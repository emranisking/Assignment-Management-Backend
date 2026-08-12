using AssignmentManagement.Application.Common.Interfaces;
using AssignmentManagement.Application.Enrollments.DTOs;
using AssignmentManagement.Application.Enrollments.Interfaces;
using AssignmentManagement.Application.Enrollments.Messages;
using AssignmentManagement.Common.Constants;
using AssignmentManagement.Common.Exceptions;
using AssignmentManagement.Common.Models;
using AssignmentManagement.Domain.Entities;
using AssignmentManagement.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace AssignmentManagement.Application.Enrollments.Services;

public class EnrollmentService : IEnrollmentService
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IMessagePublisher _publisher;
    private readonly IEnrollmentProcessor _processor;
    private readonly EnrollmentOptions _options;
    private readonly ICacheService _cache;

    public EnrollmentService(
        IAppDbContext db,
        ICurrentUser currentUser,
        IMessagePublisher publisher,
        IEnrollmentProcessor processor,
        EnrollmentOptions options,
        ICacheService cache)
    {
        _db = db;
        _currentUser = currentUser;
        _publisher = publisher;
        _processor = processor;
        _options = options;
        _cache = cache;
    }

    public async Task<EnrollmentRequestResponse> CreateRequestAsync(long classId, CancellationToken ct = default)
    {
        var studentId = _currentUser.RequireUserId();

        // Fast fail-fast checks. Authoritative checks still happen inside the locked worker transaction.
        var cls = await _db.Classes.AsNoTracking().FirstOrDefaultAsync(c => c.Id == classId, ct)
                  ?? throw new NotFoundException("Class", classId);

        if (cls.Status != ClassStatus.Open)
            throw new BusinessException("This class is not open for enrollment.");
        if (cls.EnrollmentDeadline <= DateTime.UtcNow)
            throw new BusinessException("The enrollment deadline has passed.");

        var alreadyEnrolled = await _db.Enrollments.AnyAsync(
            e => e.ClassId == classId && e.StudentId == studentId && e.Status == EnrollmentStatus.Active, ct);
        if (alreadyEnrolled)
            throw new BusinessException("You are already enrolled in this class.");

        var request = new EnrollmentRequest
        {
            StudentId = studentId,
            ClassId = classId,
            Status = EnrollmentRequestStatus.Pending
        };
        _db.EnrollmentRequests.Add(request);
        await _db.SaveChangesAsync(ct);

        if (_options.UseAsyncProcessing)
        {
            _publisher.Publish(new EnrollmentMessage
            {
                RequestId = request.Id,
                StudentId = studentId,
                ClassId = classId
            }, _options.QueueName);
        }
        else
        {
            // No broker configured: process inline so the flow still works end to end.
            await _processor.ProcessAsync(request.Id, ct);
            await _cache.InvalidateGroupAsync(ApplicationConstants.Cache.ClassPrefix, ct);
            request = await _db.EnrollmentRequests.AsNoTracking()
                .FirstAsync(r => r.Id == request.Id, ct);
        }

        return Map(request, "Your enrollment request has been received and is being processed.");
    }

    public async Task<EnrollmentRequestResponse> GetRequestAsync(long requestId, CancellationToken ct = default)
    {
        var request = await _db.EnrollmentRequests.AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == requestId, ct)
            ?? throw new NotFoundException("EnrollmentRequest", requestId);

        if (_currentUser.Role == UserRole.Student && request.StudentId != _currentUser.UserId)
            throw new ForbiddenException();

        return Map(request, StatusMessage(request.Status));
    }

    public async Task<PaginationResponse<EnrollmentRequestResponse>> GetMyRequestsAsync(
        PaginationRequest page, CancellationToken ct = default)
    {
        var studentId = _currentUser.RequireUserId();
        var query = _db.EnrollmentRequests.AsNoTracking()
            .Where(r => r.StudentId == studentId)
            .OrderByDescending(r => r.Id);

        var total = await query.CountAsync(ct);
        var items = await query.Skip(page.Skip).Take(page.PageSize).ToListAsync(ct);
        return new PaginationResponse<EnrollmentRequestResponse>(
            items.Select(r => Map(r, StatusMessage(r.Status))), total, page.Page, page.PageSize);
    }

    public async Task<IEnumerable<EnrollmentResponse>> GetMyEnrollmentsAsync(CancellationToken ct = default)
    {
        var studentId = _currentUser.RequireUserId();
        return await _db.Enrollments.AsNoTracking()
            .Where(e => e.StudentId == studentId && e.Status == EnrollmentStatus.Active)
            .Include(e => e.Class).ThenInclude(c => c!.Course)
            .OrderByDescending(e => e.Id)
            .Select(e => new EnrollmentResponse
            {
                Id = e.Id,
                ClassId = e.ClassId,
                ClassName = e.Class!.Name,
                CourseCode = e.Class!.Course!.Code,
                Status = e.Status,
                EnrolledAt = e.CreatedAt
            })
            .ToListAsync(ct);
    }

    public async Task DropAsync(long enrollmentId, CancellationToken ct = default)
    {
        var studentId = _currentUser.RequireUserId();
        var enrollment = await _db.Enrollments.FirstOrDefaultAsync(e => e.Id == enrollmentId, ct)
                         ?? throw new NotFoundException("Enrollment", enrollmentId);

        if (enrollment.StudentId != studentId)
            throw new ForbiddenException();
        if (enrollment.Status == EnrollmentStatus.Dropped)
            throw new BusinessException("This enrollment is already dropped.");

        var relational = _db.Database.IsRelational();
        var tx = relational ? await _db.Database.BeginTransactionAsync(ct) : null;
        try
        {
            if (relational)
                await _db.Database.ExecuteSqlRawAsync(
                    "SELECT \"Id\" FROM \"Classes\" WHERE \"Id\" = {0} FOR UPDATE",
                    new object[] { enrollment.ClassId }, ct);

            var cls = await _db.Classes.FirstOrDefaultAsync(c => c.Id == enrollment.ClassId, ct);
            enrollment.Status = EnrollmentStatus.Dropped;
            if (cls is not null && cls.EnrolledCount > 0) cls.EnrolledCount -= 1;

            await _db.SaveChangesAsync(ct);
            if (tx is not null) await tx.CommitAsync(ct);
        }
        catch
        {
            if (tx is not null) await tx.RollbackAsync(ct);
            throw;
        }
        finally
        {
            if (tx is not null) await tx.DisposeAsync();
        }

        await _cache.InvalidateGroupAsync(ApplicationConstants.Cache.ClassPrefix, ct);
    }

    private static string StatusMessage(EnrollmentRequestStatus status) => status switch
    {
        EnrollmentRequestStatus.Pending => "Your enrollment request is pending.",
        EnrollmentRequestStatus.Processing => "Your enrollment request is being processed.",
        EnrollmentRequestStatus.Approved => "You have been enrolled successfully.",
        EnrollmentRequestStatus.Rejected => "Your enrollment request was rejected.",
        _ => string.Empty
    };

    private static EnrollmentRequestResponse Map(EnrollmentRequest r, string message) => new()
    {
        RequestId = r.Id,
        ClassId = r.ClassId,
        Status = r.Status,
        Reason = r.FailureReason,
        CreatedAt = r.CreatedAt,
        ProcessedAt = r.ProcessedAt,
        Message = message
    };
}
