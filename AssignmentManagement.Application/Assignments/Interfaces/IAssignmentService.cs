using AssignmentManagement.Application.Assignments.DTOs;
using AssignmentManagement.Common.Models;

namespace AssignmentManagement.Application.Assignments.Interfaces;

public interface IAssignmentService
{
    Task<AssignmentResponse> CreateAsync(long classId, CreateAssignmentRequest request, CancellationToken ct = default);
    Task<PaginationResponse<AssignmentResponse>> GetByClassAsync(long classId, PaginationRequest page, CancellationToken ct = default);
    Task<AssignmentResponse> GetByIdAsync(long id, CancellationToken ct = default);
    Task<AssignmentResponse> UpdateAsync(long id, UpdateAssignmentRequest request, CancellationToken ct = default);
    Task<AssignmentResponse> PublishAsync(long id, CancellationToken ct = default);
    Task<AssignmentResponse> PublishResultsAsync(long id, CancellationToken ct = default);
    Task DeleteAsync(long id, CancellationToken ct = default);
}
