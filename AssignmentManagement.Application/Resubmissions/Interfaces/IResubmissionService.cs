using AssignmentManagement.Application.Resubmissions.DTOs;
using AssignmentManagement.Common.Models;

namespace AssignmentManagement.Application.Resubmissions.Interfaces;

public interface IResubmissionService
{
    Task<ResubmissionResponse> CreateAsync(long submissionId, CreateResubmissionRequest request, CancellationToken ct = default);
    Task<PaginationResponse<ResubmissionResponse>> GetAllAsync(PaginationRequest page, CancellationToken ct = default);
    Task<ResubmissionResponse> ApproveAsync(long id, string? note, CancellationToken ct = default);
    Task<ResubmissionResponse> RejectAsync(long id, string? note, CancellationToken ct = default);
}
