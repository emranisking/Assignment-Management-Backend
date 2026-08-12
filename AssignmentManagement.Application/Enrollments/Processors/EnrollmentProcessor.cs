using AssignmentManagement.Application.Common.Interfaces;
using AssignmentManagement.Application.Enrollments.Interfaces;
using AssignmentManagement.Domain.Entities;
using AssignmentManagement.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AssignmentManagement.Application.Enrollments.Processors;

public class EnrollmentProcessor : IEnrollmentProcessor
{
    private readonly IAppDbContext _db;
    private readonly ILogger<EnrollmentProcessor> _logger;

    // Acquires a row-level lock on the target class for the duration of the transaction.
    // Executed with ExecuteSqlRaw so we don't depend on FromSql column mapping.
    private const string LockClassSql = "SELECT \"Id\" FROM \"Classes\" WHERE \"Id\" = {0} FOR UPDATE";

    public EnrollmentProcessor(IAppDbContext db, ILogger<EnrollmentProcessor> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task ProcessAsync(long requestId, CancellationToken ct = default)
    {
        var request = await _db.EnrollmentRequests.FirstOrDefaultAsync(r => r.Id == requestId, ct);
        if (request is null)
        {
            _logger.LogWarning("Enrollment request {RequestId} not found; skipping.", requestId);
            return;
        }

        // Idempotency: if a redelivered message hits an already-finished request, do nothing.
        if (request.Status is EnrollmentRequestStatus.Approved or EnrollmentRequestStatus.Rejected)
        {
            _logger.LogInformation("Enrollment request {RequestId} already {Status}; ignoring.",
                requestId, request.Status);
            return;
        }

        var relational = _db.Database.IsRelational();
        var tx = relational ? await _db.Database.BeginTransactionAsync(ct) : null;
        try
        {
            if (relational)
                await _db.Database.ExecuteSqlRawAsync(LockClassSql, new object[] { request.ClassId }, ct);

            var cls = await _db.Classes.FirstOrDefaultAsync(c => c.Id == request.ClassId, ct);

            string? rejectReason = Validate(cls, request);
            if (rejectReason is not null)
            {
                Reject(request, rejectReason);
            }
            else
            {
                var alreadyEnrolled = await _db.Enrollments.AnyAsync(
                    e => e.ClassId == request.ClassId && e.StudentId == request.StudentId, ct);

                if (alreadyEnrolled)
                {
                    Reject(request, "You are already enrolled in this class.");
                }
                else
                {
                    _db.Enrollments.Add(new Enrollment
                    {
                        ClassId = request.ClassId,
                        StudentId = request.StudentId,
                        Status = EnrollmentStatus.Active
                    });

                    cls!.EnrolledCount += 1;
                    request.Status = EnrollmentRequestStatus.Approved;
                    request.FailureReason = null;
                    request.ProcessedAt = DateTime.UtcNow;
                }
            }

            await _db.SaveChangesAsync(ct);
            if (tx is not null) await tx.CommitAsync(ct);
        }
        catch (DbUpdateException ex)
        {
            // The UNIQUE(StudentId, ClassId) constraint is the final guard against duplicates.
            if (tx is not null) await tx.RollbackAsync(ct);
            _logger.LogWarning(ex, "Unique/DB conflict for request {RequestId}; marking as duplicate.", requestId);
            await MarkRejectedInNewScopeAsync(requestId, "You are already enrolled in this class.", ct);
        }
        catch (Exception ex)
        {
            if (tx is not null) await tx.RollbackAsync(ct);
            _logger.LogError(ex, "Unexpected error processing enrollment request {RequestId}.", requestId);
            throw; // let the caller decide whether to NACK/redeliver
        }
        finally
        {
            if (tx is not null) await tx.DisposeAsync();
        }
    }

    private static string? Validate(Class? cls, EnrollmentRequest request)
    {
        if (cls is null) return "Class no longer exists.";
        if (cls.Status != ClassStatus.Open) return "This class is not open for enrollment.";
        if (cls.EnrollmentDeadline <= DateTime.UtcNow) return "The enrollment deadline has passed.";
        if (cls.EnrolledCount >= cls.Capacity) return "Class is full.";
        return null;
    }

    private static void Reject(EnrollmentRequest request, string reason)
    {
        request.Status = EnrollmentRequestStatus.Rejected;
        request.FailureReason = reason;
        request.ProcessedAt = DateTime.UtcNow;
    }

    private async Task MarkRejectedInNewScopeAsync(long requestId, string reason, CancellationToken ct)
    {
        var request = await _db.EnrollmentRequests.FirstOrDefaultAsync(r => r.Id == requestId, ct);
        if (request is null || request.Status == EnrollmentRequestStatus.Approved) return;
        Reject(request, reason);
        await _db.SaveChangesAsync(ct);
    }
}
