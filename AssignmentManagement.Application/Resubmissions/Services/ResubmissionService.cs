using AssignmentManagement.Application.Common.Interfaces;
using AssignmentManagement.Application.Resubmissions.DTOs;
using AssignmentManagement.Application.Resubmissions.Interfaces;
using AssignmentManagement.Common.Exceptions;
using AssignmentManagement.Common.Models;
using AssignmentManagement.Domain.Entities;
using AssignmentManagement.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace AssignmentManagement.Application.Resubmissions.Services;

public class ResubmissionService : IResubmissionService
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IAccessGuard _guard;

    public ResubmissionService(IAppDbContext db, ICurrentUser currentUser, IAccessGuard guard)
    {
        _db = db;
        _currentUser = currentUser;
        _guard = guard;
    }

    public async Task<ResubmissionResponse> CreateAsync(
        long submissionId, CreateResubmissionRequest request, CancellationToken ct = default)
    {
        var studentId = _currentUser.RequireUserId();

        var submission = await _db.Submissions.FirstOrDefaultAsync(s => s.Id == submissionId, ct)
                         ?? throw new NotFoundException("Submission", submissionId);

        if (submission.StudentId != studentId)
            throw new ForbiddenException();
        if (submission.Status != SubmissionStatus.Graded)
            throw new BusinessException("You can only request a resubmission after the submission has been graded.");

        var duplicate = await _db.ResubmissionRequests.AnyAsync(
            r => r.SubmissionId == submissionId && r.Status == ResubmissionRequestStatus.Pending, ct);
        if (duplicate)
            throw new BusinessException("You already have a pending resubmission request for this submission.");

        var entity = new ResubmissionRequest
        {
            SubmissionId = submissionId,
            StudentId = studentId,
            Reason = request.Reason.Trim(),
            Status = ResubmissionRequestStatus.Pending
        };
        _db.ResubmissionRequests.Add(entity);
        await _db.SaveChangesAsync(ct);
        return await LoadAsync(entity.Id, ct);
    }

    public async Task<PaginationResponse<ResubmissionResponse>> GetAllAsync(
        PaginationRequest page, CancellationToken ct = default)
    {
        var query = _db.ResubmissionRequests.AsNoTracking()
            .Include(r => r.Student)
            .Include(r => r.Submission).ThenInclude(s => s!.Assignment).ThenInclude(a => a!.Class)
            .AsQueryable();

        switch (_currentUser.Role)
        {
            case UserRole.Student:
                query = query.Where(r => r.StudentId == _currentUser.UserId);
                break;
            case UserRole.Teacher:
                query = query.Where(r => r.Submission!.Assignment!.Class!.TeacherId == _currentUser.UserId);
                break;
        }

        query = query.OrderByDescending(r => r.Id);
        var total = await query.CountAsync(ct);
        var items = await query.Skip(page.Skip).Take(page.PageSize).ToListAsync(ct);
        return new PaginationResponse<ResubmissionResponse>(
            items.Select(Map), total, page.Page, page.PageSize);
    }

    public Task<ResubmissionResponse> ApproveAsync(long id, string? note, CancellationToken ct = default)
        => DecideAsync(id, ResubmissionRequestStatus.Approved, note, ct);

    public Task<ResubmissionResponse> RejectAsync(long id, string? note, CancellationToken ct = default)
        => DecideAsync(id, ResubmissionRequestStatus.Rejected, note, ct);

    private async Task<ResubmissionResponse> DecideAsync(
        long id, ResubmissionRequestStatus status, string? note, CancellationToken ct)
    {
        var entity = await _db.ResubmissionRequests
            .Include(r => r.Submission)
            .FirstOrDefaultAsync(r => r.Id == id, ct)
            ?? throw new NotFoundException("ResubmissionRequest", id);

        // Only the owning teacher (or admin) may decide.
        await _guard.RequireManageableAssignmentAsync(entity.Submission!.AssignmentId, ct);

        if (entity.Status != ResubmissionRequestStatus.Pending)
            throw new BusinessException("This resubmission request has already been processed.");

        entity.Status = status;
        entity.DecisionNote = note?.Trim();
        entity.ProcessedAt = DateTime.UtcNow;

        if (status == ResubmissionRequestStatus.Approved)
            entity.Submission!.Status = SubmissionStatus.ResubmissionRequested;

        await _db.SaveChangesAsync(ct);
        return await LoadAsync(entity.Id, ct);
    }

    private async Task<ResubmissionResponse> LoadAsync(long id, CancellationToken ct)
    {
        var entity = await _db.ResubmissionRequests.AsNoTracking()
            .Include(r => r.Student)
            .Include(r => r.Submission).ThenInclude(s => s!.Assignment)
            .FirstAsync(r => r.Id == id, ct);
        return Map(entity);
    }

    private static ResubmissionResponse Map(ResubmissionRequest r) => new()
    {
        Id = r.Id,
        SubmissionId = r.SubmissionId,
        StudentId = r.StudentId,
        StudentName = r.Student?.Name ?? string.Empty,
        AssignmentId = r.Submission?.AssignmentId ?? 0,
        AssignmentTitle = r.Submission?.Assignment?.Title ?? string.Empty,
        Reason = r.Reason,
        Status = r.Status,
        DecisionNote = r.DecisionNote,
        CreatedAt = r.CreatedAt,
        ProcessedAt = r.ProcessedAt
    };
}
