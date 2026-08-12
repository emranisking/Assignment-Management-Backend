using AssignmentManagement.Application.Classes.DTOs;
using AssignmentManagement.Application.Classes.Interfaces;
using AssignmentManagement.Application.Common.Interfaces;
using AssignmentManagement.Common.Constants;
using AssignmentManagement.Common.Exceptions;
using AssignmentManagement.Common.Models;
using AssignmentManagement.Domain.Entities;
using AssignmentManagement.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace AssignmentManagement.Application.Classes.Services;

public class ClassService : IClassService
{
    private readonly IAppDbContext _db;
    private readonly ICacheService _cache;
    private readonly ICurrentUser _currentUser;
    private const string Group = ApplicationConstants.Cache.ClassPrefix;

    public ClassService(IAppDbContext db, ICacheService cache, ICurrentUser currentUser)
    {
        _db = db;
        _cache = cache;
        _currentUser = currentUser;
    }

    public async Task<PaginationResponse<ClassResponse>> GetAllAsync(
        PaginationRequest page, long? courseId, CancellationToken ct = default)
    {
        var key = await _cache.BuildVersionedKeyAsync(
            Group, $"list-p{page.Page}-s{page.PageSize}-c{courseId}", ct);

        return await _cache.GetOrSetAsync(key, async () =>
        {
            var query = _db.Classes.AsNoTracking()
                .Include(c => c.Course)
                .Include(c => c.Teacher)
                .AsQueryable();

            if (courseId.HasValue) query = query.Where(c => c.CourseId == courseId.Value);

            query = query.OrderByDescending(c => c.Id);
            var total = await query.CountAsync(ct);
            var items = await query.Skip(page.Skip).Take(page.PageSize).ToListAsync(ct);

            return new PaginationResponse<ClassResponse>(
                items.Select(Map), total, page.Page, page.PageSize);
        }, ct: ct);
    }

    public async Task<ClassResponse> GetByIdAsync(long id, CancellationToken ct = default)
    {
        var entity = await LoadWithRelationsAsync(id, ct);
        return Map(entity);
    }

    public async Task<ClassResponse> CreateAsync(CreateClassRequest request, CancellationToken ct = default)
    {
        var courseExists = await _db.Courses.AnyAsync(c => c.Id == request.CourseId, ct);
        if (!courseExists) throw new NotFoundException("Course", request.CourseId);

        if (request.TeacherId.HasValue)
            await EnsureTeacherAsync(request.TeacherId.Value, ct);

        if (request.EnrollmentDeadline <= DateTime.UtcNow)
            throw new BusinessException("Enrollment deadline must be in the future.");

        var entity = new Class
        {
            CourseId = request.CourseId,
            TeacherId = request.TeacherId,
            Name = request.Name.Trim(),
            DayOfWeek = request.DayOfWeek,
            StartTime = ParseTime(request.StartTime, nameof(request.StartTime)),
            EndTime = ParseTime(request.EndTime, nameof(request.EndTime)),
            Capacity = request.Capacity,
            EnrolledCount = 0,
            EnrollmentDeadline = DateTime.SpecifyKind(request.EnrollmentDeadline, DateTimeKind.Utc),
            Status = ClassStatus.Open
        };

        if (entity.EndTime <= entity.StartTime)
            throw new BusinessException("End time must be after start time.");

        _db.Classes.Add(entity);
        await _db.SaveChangesAsync(ct);
        await _cache.InvalidateGroupAsync(Group, ct);
        return Map(await LoadWithRelationsAsync(entity.Id, ct));
    }

    public async Task<ClassResponse> UpdateAsync(long id, UpdateClassRequest request, CancellationToken ct = default)
    {
        var entity = await _db.Classes.FirstOrDefaultAsync(c => c.Id == id, ct)
                     ?? throw new NotFoundException("Class", id);

        var start = ParseTime(request.StartTime, nameof(request.StartTime));
        var end = ParseTime(request.EndTime, nameof(request.EndTime));
        if (end <= start) throw new BusinessException("End time must be after start time.");

        if (request.Capacity < entity.EnrolledCount)
            throw new BusinessException(
                $"Capacity cannot be lower than the current enrolled count ({entity.EnrolledCount}).");

        entity.Name = request.Name.Trim();
        entity.DayOfWeek = request.DayOfWeek;
        entity.StartTime = start;
        entity.EndTime = end;
        entity.Capacity = request.Capacity;
        entity.EnrollmentDeadline = DateTime.SpecifyKind(request.EnrollmentDeadline, DateTimeKind.Utc);

        await _db.SaveChangesAsync(ct);
        await _cache.InvalidateGroupAsync(Group, ct);
        return Map(await LoadWithRelationsAsync(id, ct));
    }

    public async Task<ClassResponse> AssignTeacherAsync(long id, long teacherId, CancellationToken ct = default)
    {
        var entity = await _db.Classes.FirstOrDefaultAsync(c => c.Id == id, ct)
                     ?? throw new NotFoundException("Class", id);

        await EnsureTeacherAsync(teacherId, ct);
        entity.TeacherId = teacherId;
        await _db.SaveChangesAsync(ct);
        await _cache.InvalidateGroupAsync(Group, ct);
        return Map(await LoadWithRelationsAsync(id, ct));
    }

    public async Task<ClassResponse> SetStatusAsync(long id, ClassStatus status, CancellationToken ct = default)
    {
        var entity = await _db.Classes.FirstOrDefaultAsync(c => c.Id == id, ct)
                     ?? throw new NotFoundException("Class", id);
        entity.Status = status;
        await _db.SaveChangesAsync(ct);
        await _cache.InvalidateGroupAsync(Group, ct);
        return Map(await LoadWithRelationsAsync(id, ct));
    }

    public async Task<IEnumerable<ClassStudentResponse>> GetStudentsAsync(long id, CancellationToken ct = default)
    {
        var entity = await _db.Classes.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id, ct)
                     ?? throw new NotFoundException("Class", id);

        // Resource authorization: a teacher may only view students of their own class.
        if (_currentUser.Role == UserRole.Teacher && entity.TeacherId != _currentUser.UserId)
            throw new ForbiddenException("You are not assigned to this class.");

        return await _db.Enrollments.AsNoTracking()
            .Where(e => e.ClassId == id)
            .Include(e => e.Student)
            .OrderBy(e => e.Student!.Name)
            .Select(e => new ClassStudentResponse
            {
                StudentId = e.StudentId,
                Name = e.Student!.Name,
                Email = e.Student!.Email,
                Status = e.Status,
                EnrolledAt = e.CreatedAt
            })
            .ToListAsync(ct);
    }

    private async Task<Class> LoadWithRelationsAsync(long id, CancellationToken ct)
        => await _db.Classes.AsNoTracking()
               .Include(c => c.Course)
               .Include(c => c.Teacher)
               .FirstOrDefaultAsync(c => c.Id == id, ct)
           ?? throw new NotFoundException("Class", id);

    private async Task EnsureTeacherAsync(long teacherId, CancellationToken ct)
    {
        var teacher = await _db.Users.FirstOrDefaultAsync(u => u.Id == teacherId, ct)
                      ?? throw new NotFoundException("Teacher", teacherId);
        if (teacher.Role != UserRole.Teacher)
            throw new BusinessException("The specified user is not a teacher.");
    }

    private static TimeOnly ParseTime(string value, string field)
    {
        if (!TimeOnly.TryParse(value, out var t))
            throw new ValidationAppException($"{field} must be a valid time in HH:mm format.");
        return t;
    }

    private static ClassResponse Map(Class c) => new()
    {
        Id = c.Id,
        CourseId = c.CourseId,
        CourseCode = c.Course?.Code ?? string.Empty,
        Name = c.Name,
        TeacherId = c.TeacherId,
        TeacherName = c.Teacher?.Name,
        DayOfWeek = c.DayOfWeek,
        StartTime = c.StartTime.ToString("HH:mm"),
        EndTime = c.EndTime.ToString("HH:mm"),
        Capacity = c.Capacity,
        EnrolledCount = c.EnrolledCount,
        EnrollmentDeadline = c.EnrollmentDeadline,
        Status = c.Status
    };
}
