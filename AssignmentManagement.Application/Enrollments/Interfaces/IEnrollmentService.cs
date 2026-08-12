using AssignmentManagement.Application.Enrollments.DTOs;
using AssignmentManagement.Common.Models;

namespace AssignmentManagement.Application.Enrollments.Interfaces;

public interface IEnrollmentService
{
    Task<EnrollmentRequestResponse> CreateRequestAsync(long classId, CancellationToken ct = default);
    Task<EnrollmentRequestResponse> GetRequestAsync(long requestId, CancellationToken ct = default);
    Task<PaginationResponse<EnrollmentRequestResponse>> GetMyRequestsAsync(PaginationRequest page, CancellationToken ct = default);
    Task<IEnumerable<EnrollmentResponse>> GetMyEnrollmentsAsync(CancellationToken ct = default);
    Task DropAsync(long enrollmentId, CancellationToken ct = default);
}
