using AssignmentManagement.Application.Common.Interfaces;
using AssignmentManagement.Application.TeacherApplications.DTOs;
using AssignmentManagement.Application.TeacherApplications.Interfaces;
using AssignmentManagement.Common.Exceptions;
using AssignmentManagement.Common.Models;
using AssignmentManagement.Domain.Entities;
using AssignmentManagement.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace AssignmentManagement.Application.TeacherApplications.Services;

public class TeacherApplicationService : ITeacherApplicationService
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;

    public TeacherApplicationService(IAppDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<TeacherApplicationResponse> ApplyAsync(
        CreateTeacherApplicationRequest request, CancellationToken ct = default)
    {
        var teacherId = _currentUser.RequireUserId();

        var courseExists = await _db.Courses.AnyAsync(c => c.Id == request.CourseId, ct);
        if (!courseExists) throw new NotFoundException("Course", request.CourseId);

        var duplicate = await _db.TeacherCourseApplications.AnyAsync(
            a => a.TeacherId == teacherId
                 && a.CourseId == request.CourseId
                 && a.Status == CourseApplicationStatus.Pending, ct);
        if (duplicate)
            throw new BusinessException("You already have a pending application for this course.");

        var app = new TeacherCourseApplication
        {
            TeacherId = teacherId,
            CourseId = request.CourseId,
            Status = CourseApplicationStatus.Pending
        };

        _db.TeacherCourseApplications.Add(app);
        await _db.SaveChangesAsync(ct);
        return await GetByIdAsync(app.Id, ct);
    }

    public async Task<PaginationResponse<TeacherApplicationResponse>> GetAllAsync(
        PaginationRequest page, CancellationToken ct = default)
    {
        var query = _db.TeacherCourseApplications.AsNoTracking()
            .Include(a => a.Teacher)
            .Include(a => a.Course)
            .AsQueryable();

        // Teachers see only their own applications; Admin sees everything.
        if (_currentUser.Role == UserRole.Teacher)
            query = query.Where(a => a.TeacherId == _currentUser.UserId);

        query = query.OrderByDescending(a => a.Id);
        var total = await query.CountAsync(ct);
        var items = await query.Skip(page.Skip).Take(page.PageSize).ToListAsync(ct);
        return new PaginationResponse<TeacherApplicationResponse>(
            items.Select(Map), total, page.Page, page.PageSize);
    }

    public async Task<TeacherApplicationResponse> GetByIdAsync(long id, CancellationToken ct = default)
    {
        var app = await _db.TeacherCourseApplications.AsNoTracking()
            .Include(a => a.Teacher)
            .Include(a => a.Course)
            .FirstOrDefaultAsync(a => a.Id == id, ct)
            ?? throw new NotFoundException("TeacherCourseApplication", id);

        if (_currentUser.Role == UserRole.Teacher && app.TeacherId != _currentUser.UserId)
            throw new ForbiddenException();

        return Map(app);
    }

    public Task<TeacherApplicationResponse> ApproveAsync(long id, string? note, CancellationToken ct = default)
        => DecideAsync(id, CourseApplicationStatus.Approved, note, ct);

    public Task<TeacherApplicationResponse> RejectAsync(long id, string? note, CancellationToken ct = default)
        => DecideAsync(id, CourseApplicationStatus.Rejected, note, ct);

    private async Task<TeacherApplicationResponse> DecideAsync(
        long id, CourseApplicationStatus status, string? note, CancellationToken ct)
    {
        var app = await _db.TeacherCourseApplications.FirstOrDefaultAsync(a => a.Id == id, ct)
                  ?? throw new NotFoundException("TeacherCourseApplication", id);

        if (app.Status != CourseApplicationStatus.Pending)
            throw new BusinessException("This application has already been processed.");

        app.Status = status;
        app.DecisionNote = note?.Trim();
        app.ProcessedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return await GetByIdAsync(app.Id, ct);
    }

    private static TeacherApplicationResponse Map(TeacherCourseApplication a) => new()
    {
        Id = a.Id,
        TeacherId = a.TeacherId,
        TeacherName = a.Teacher?.Name ?? string.Empty,
        CourseId = a.CourseId,
        CourseCode = a.Course?.Code ?? string.Empty,
        Status = a.Status,
        DecisionNote = a.DecisionNote,
        CreatedAt = a.CreatedAt,
        ProcessedAt = a.ProcessedAt
    };
}
