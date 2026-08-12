using AssignmentManagement.Application.Assignments.DTOs;
using AssignmentManagement.Application.Assignments.Interfaces;
using AssignmentManagement.Application.Common.Interfaces;
using AssignmentManagement.Common.Constants;
using AssignmentManagement.Common.Exceptions;
using AssignmentManagement.Common.Models;
using AssignmentManagement.Domain.Entities;
using AssignmentManagement.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace AssignmentManagement.Application.Assignments.Services;

public class AssignmentService : IAssignmentService
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IAccessGuard _guard;
    private readonly ICacheService _cache;
    private const string Group = ApplicationConstants.Cache.AssignmentPrefix;

    public AssignmentService(IAppDbContext db, ICurrentUser currentUser, IAccessGuard guard, ICacheService cache)
    {
        _db = db;
        _currentUser = currentUser;
        _guard = guard;
        _cache = cache;
    }

    public async Task<AssignmentResponse> CreateAsync(long classId, CreateAssignmentRequest request, CancellationToken ct = default)
    {
        await _guard.RequireManageableClassAsync(classId, ct);

        var assignment = new Assignment
        {
            ClassId = classId,
            Title = request.Title.Trim(),
            Description = request.Description?.Trim(),
            Deadline = DateTime.SpecifyKind(request.Deadline, DateTimeKind.Utc),
            MaxMarks = request.MaxMarks,
            Status = AssignmentStatus.Draft
        };

        _db.Assignments.Add(assignment);
        await _db.SaveChangesAsync(ct);
        await _cache.InvalidateGroupAsync(Group, ct);
        return Map(assignment);
    }

    public async Task<PaginationResponse<AssignmentResponse>> GetByClassAsync(
        long classId, PaginationRequest page, CancellationToken ct = default)
    {
        await EnsureCanViewClassAsync(classId, ct);

        var key = await _cache.BuildVersionedKeyAsync(
            Group, $"class-{classId}-p{page.Page}-s{page.PageSize}-role{_currentUser.Role}", ct);

        return await _cache.GetOrSetAsync(key, async () =>
        {
            var query = _db.Assignments.AsNoTracking().Where(a => a.ClassId == classId);

            // Students never see draft assignments.
            if (_currentUser.Role == UserRole.Student)
                query = query.Where(a => a.Status != AssignmentStatus.Draft);

            query = query.OrderByDescending(a => a.Id);
            var total = await query.CountAsync(ct);
            var items = await query.Skip(page.Skip).Take(page.PageSize).Select(a => Map(a)).ToListAsync(ct);
            return new PaginationResponse<AssignmentResponse>(items, total, page.Page, page.PageSize);
        }, ct: ct);
    }

    public async Task<AssignmentResponse> GetByIdAsync(long id, CancellationToken ct = default)
    {
        var assignment = await _db.Assignments.AsNoTracking()
            .Include(a => a.Class)
            .FirstOrDefaultAsync(a => a.Id == id, ct)
            ?? throw new NotFoundException("Assignment", id);

        await EnsureCanViewClassAsync(assignment.ClassId, ct);

        if (_currentUser.Role == UserRole.Student && assignment.Status == AssignmentStatus.Draft)
            throw new ForbiddenException("This assignment is not published yet.");

        return Map(assignment);
    }

    public async Task<AssignmentResponse> UpdateAsync(long id, UpdateAssignmentRequest request, CancellationToken ct = default)
    {
        var assignment = await _guard.RequireManageableAssignmentAsync(id, ct);
        assignment.Title = request.Title.Trim();
        assignment.Description = request.Description?.Trim();
        assignment.Deadline = DateTime.SpecifyKind(request.Deadline, DateTimeKind.Utc);
        assignment.MaxMarks = request.MaxMarks;
        await _db.SaveChangesAsync(ct);
        await _cache.InvalidateGroupAsync(Group, ct);
        return Map(assignment);
    }

    public async Task<AssignmentResponse> PublishAsync(long id, CancellationToken ct = default)
    {
        var assignment = await _guard.RequireManageableAssignmentAsync(id, ct);
        assignment.Status = AssignmentStatus.Published;
        await _db.SaveChangesAsync(ct);
        await _cache.InvalidateGroupAsync(Group, ct);
        return Map(assignment);
    }

    public async Task<AssignmentResponse> PublishResultsAsync(long id, CancellationToken ct = default)
    {
        var assignment = await _guard.RequireManageableAssignmentAsync(id, ct);
        assignment.ResultsPublished = true;
        await _db.SaveChangesAsync(ct);
        await _cache.InvalidateGroupAsync(Group, ct);
        return Map(assignment);
    }

    public async Task DeleteAsync(long id, CancellationToken ct = default)
    {
        var assignment = await _guard.RequireManageableAssignmentAsync(id, ct);
        var hasSubmissions = await _db.Submissions.AnyAsync(s => s.AssignmentId == id, ct);
        if (hasSubmissions)
            throw new BusinessException("Cannot delete an assignment that already has submissions.");

        _db.Assignments.Remove(assignment);
        await _db.SaveChangesAsync(ct);
        await _cache.InvalidateGroupAsync(Group, ct);
    }

    private async Task EnsureCanViewClassAsync(long classId, CancellationToken ct)
    {
        switch (_currentUser.Role)
        {
            case UserRole.Admin:
                return;
            case UserRole.Teacher:
                await _guard.RequireManageableClassAsync(classId, ct);
                return;
            case UserRole.Student:
                await _guard.RequireEnrolledAsync(classId, ct);
                return;
            default:
                throw new ForbiddenException();
        }
    }

    private static AssignmentResponse Map(Assignment a) => new()
    {
        Id = a.Id,
        ClassId = a.ClassId,
        Title = a.Title,
        Description = a.Description,
        Deadline = a.Deadline,
        MaxMarks = a.MaxMarks,
        Status = a.Status,
        ResultsPublished = a.ResultsPublished,
        CreatedAt = a.CreatedAt
    };
}
