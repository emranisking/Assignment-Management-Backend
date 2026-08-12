using AssignmentManagement.Application.Common.Interfaces;
using AssignmentManagement.Application.Submissions.DTOs;
using AssignmentManagement.Application.Submissions.Interfaces;
using AssignmentManagement.Common.Constants;
using AssignmentManagement.Common.Exceptions;
using AssignmentManagement.Common.Models;
using AssignmentManagement.Domain.Entities;
using AssignmentManagement.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace AssignmentManagement.Application.Submissions.Services;

public class SubmissionService : ISubmissionService
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IAccessGuard _guard;
    private readonly IFileStorageService _storage;

    public SubmissionService(
        IAppDbContext db, ICurrentUser currentUser, IAccessGuard guard, IFileStorageService storage)
    {
        _db = db;
        _currentUser = currentUser;
        _guard = guard;
        _storage = storage;
    }

    public async Task<SubmissionResponse> SubmitAsync(long assignmentId, SubmissionFileUpload upload, CancellationToken ct = default)
    {
        ValidateFile(upload);
        var studentId = _currentUser.RequireUserId();

        var assignment = await _db.Assignments.FirstOrDefaultAsync(a => a.Id == assignmentId, ct)
                         ?? throw new NotFoundException("Assignment", assignmentId);

        await _guard.RequireEnrolledAsync(assignment.ClassId, ct);

        if (assignment.Status == AssignmentStatus.Draft)
            throw new BusinessException("This assignment is not published yet.");
        if (assignment.Status == AssignmentStatus.Closed || assignment.Deadline <= DateTime.UtcNow)
            throw new BusinessException("The submission deadline has passed.");

        var existing = await _db.Submissions
            .FirstOrDefaultAsync(s => s.AssignmentId == assignmentId && s.StudentId == studentId, ct);
        if (existing is not null)
            throw new BusinessException("You have already submitted. Request a resubmission to upload a new version.");

        var stored = await _storage.SaveAsync(
            upload.Content, upload.FileName, upload.ContentType,
            $"submissions/assignment-{assignmentId}/student-{studentId}", ct);

        var submission = new Submission
        {
            AssignmentId = assignmentId,
            StudentId = studentId,
            Status = SubmissionStatus.Submitted,
            CurrentVersion = 1
        };
        submission.Versions.Add(new SubmissionVersion
        {
            VersionNumber = 1,
            FileName = stored.FileName,
            FilePath = stored.FilePath,
            FileSize = stored.FileSize,
            ContentType = stored.ContentType,
            SubmittedAt = DateTime.UtcNow
        });

        _db.Submissions.Add(submission);
        await _db.SaveChangesAsync(ct);
        return await LoadResponseAsync(submission.Id, ct);
    }

    public async Task<SubmissionResponse> AddVersionAsync(long submissionId, SubmissionFileUpload upload, CancellationToken ct = default)
    {
        ValidateFile(upload);
        var studentId = _currentUser.RequireUserId();

        var submission = await _db.Submissions.Include(s => s.Assignment)
            .FirstOrDefaultAsync(s => s.Id == submissionId, ct)
            ?? throw new NotFoundException("Submission", submissionId);

        if (submission.StudentId != studentId)
            throw new ForbiddenException();

        if (submission.Status != SubmissionStatus.ResubmissionRequested)
            throw new BusinessException("A new version can only be uploaded after a resubmission is approved.");

        var stored = await _storage.SaveAsync(
            upload.Content, upload.FileName, upload.ContentType,
            $"submissions/assignment-{submission.AssignmentId}/student-{studentId}", ct);

        var nextVersion = submission.CurrentVersion + 1;
        _db.SubmissionVersions.Add(new SubmissionVersion
        {
            SubmissionId = submission.Id,
            VersionNumber = nextVersion,
            FileName = stored.FileName,
            FilePath = stored.FilePath,
            FileSize = stored.FileSize,
            ContentType = stored.ContentType,
            SubmittedAt = DateTime.UtcNow
        });

        submission.CurrentVersion = nextVersion;
        submission.Status = SubmissionStatus.Resubmitted;
        // Clear the previous grade so the teacher re-grades the new version.
        submission.Marks = null;
        submission.Feedback = null;
        submission.GradedAt = null;
        submission.GradedById = null;

        await _db.SaveChangesAsync(ct);
        return await LoadResponseAsync(submission.Id, ct);
    }

    public async Task<PaginationResponse<SubmissionResponse>> GetByAssignmentAsync(
        long assignmentId, PaginationRequest page, CancellationToken ct = default)
    {
        await _guard.RequireManageableAssignmentAsync(assignmentId, ct);

        var query = _db.Submissions.AsNoTracking()
            .Where(s => s.AssignmentId == assignmentId)
            .Include(s => s.Student)
            .OrderBy(s => s.Student!.Name);

        var total = await query.CountAsync(ct);
        var items = await query.Skip(page.Skip).Take(page.PageSize).ToListAsync(ct);
        return new PaginationResponse<SubmissionResponse>(
            items.Select(Map), total, page.Page, page.PageSize);
    }

    public async Task<SubmissionResponse> GetByIdAsync(long submissionId, CancellationToken ct = default)
    {
        var submission = await LoadWithAccessAsync(submissionId, ct);
        return Map(submission);
    }

    public async Task<IEnumerable<SubmissionVersionResponse>> GetVersionsAsync(long submissionId, CancellationToken ct = default)
    {
        await LoadWithAccessAsync(submissionId, ct);
        return await _db.SubmissionVersions.AsNoTracking()
            .Where(v => v.SubmissionId == submissionId)
            .OrderBy(v => v.VersionNumber)
            .Select(v => new SubmissionVersionResponse
            {
                Id = v.Id,
                VersionNumber = v.VersionNumber,
                FileName = v.FileName,
                FileSize = v.FileSize,
                ContentType = v.ContentType,
                SubmittedAt = v.SubmittedAt
            })
            .ToListAsync(ct);
    }

    public async Task<DownloadResult> DownloadAsync(long submissionId, int? versionNumber, CancellationToken ct = default)
    {
        var submission = await LoadWithAccessAsync(submissionId, ct);
        var target = versionNumber ?? submission.CurrentVersion;

        var version = await _db.SubmissionVersions.AsNoTracking()
            .FirstOrDefaultAsync(v => v.SubmissionId == submissionId && v.VersionNumber == target, ct)
            ?? throw new NotFoundException($"Version {target} of submission", submissionId);

        var stream = await _storage.OpenReadAsync(version.FilePath, ct);
        return new DownloadResult
        {
            Content = stream,
            FileName = version.FileName,
            ContentType = version.ContentType
        };
    }

    public async Task<SubmissionResponse> GradeAsync(long submissionId, GradeSubmissionRequest request, CancellationToken ct = default)
    {
        var submission = await _db.Submissions.Include(s => s.Assignment)
            .FirstOrDefaultAsync(s => s.Id == submissionId, ct)
            ?? throw new NotFoundException("Submission", submissionId);

        await _guard.RequireManageableAssignmentAsync(submission.AssignmentId, ct);

        if (request.Marks > submission.Assignment!.MaxMarks)
            throw new BusinessException(
                $"Marks cannot exceed the assignment maximum of {submission.Assignment.MaxMarks}.");

        submission.Marks = request.Marks;
        submission.Feedback = request.Feedback?.Trim();
        submission.Status = SubmissionStatus.Graded;
        submission.GradedAt = DateTime.UtcNow;
        submission.GradedById = _currentUser.UserId;

        await _db.SaveChangesAsync(ct);
        return await LoadResponseAsync(submission.Id, ct);
    }

    private async Task<Submission> LoadWithAccessAsync(long submissionId, CancellationToken ct)
    {
        var submission = await _db.Submissions.Include(s => s.Assignment).ThenInclude(a => a!.Class)
            .FirstOrDefaultAsync(s => s.Id == submissionId, ct)
            ?? throw new NotFoundException("Submission", submissionId);

        var isOwnerStudent = _currentUser.Role == UserRole.Student && submission.StudentId == _currentUser.UserId;
        var isManagingTeacher = _currentUser.Role == UserRole.Teacher
                                && submission.Assignment!.Class!.TeacherId == _currentUser.UserId;
        var isAdmin = _currentUser.Role == UserRole.Admin;

        if (!isOwnerStudent && !isManagingTeacher && !isAdmin)
            throw new ForbiddenException();

        return submission;
    }

    private async Task<SubmissionResponse> LoadResponseAsync(long id, CancellationToken ct)
    {
        var submission = await _db.Submissions.AsNoTracking()
            .Include(s => s.Student)
            .FirstAsync(s => s.Id == id, ct);
        return Map(submission);
    }

    private static void ValidateFile(SubmissionFileUpload upload)
    {
        if (upload.Length <= 0)
            throw new ValidationAppException("The uploaded file is empty.");
        if (upload.Length > ApplicationConstants.Files.MaxSubmissionBytes)
            throw new ValidationAppException("The file exceeds the maximum allowed size (15 MB).");

        var isPdf = string.Equals(upload.ContentType, ApplicationConstants.Files.AllowedContentType, StringComparison.OrdinalIgnoreCase)
                    || upload.FileName.EndsWith(ApplicationConstants.Files.AllowedExtension, StringComparison.OrdinalIgnoreCase);
        if (!isPdf)
            throw new ValidationAppException("Only PDF files are accepted.");
    }

    private static SubmissionResponse Map(Submission s) => new()
    {
        Id = s.Id,
        AssignmentId = s.AssignmentId,
        StudentId = s.StudentId,
        StudentName = s.Student?.Name ?? string.Empty,
        Status = s.Status,
        CurrentVersion = s.CurrentVersion,
        Marks = s.Marks,
        Feedback = s.Feedback,
        GradedAt = s.GradedAt,
        CreatedAt = s.CreatedAt
    };
}
