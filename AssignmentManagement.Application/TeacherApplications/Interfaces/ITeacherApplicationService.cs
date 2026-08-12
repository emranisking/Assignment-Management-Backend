using AssignmentManagement.Application.TeacherApplications.DTOs;
using AssignmentManagement.Common.Models;

namespace AssignmentManagement.Application.TeacherApplications.Interfaces;

public interface ITeacherApplicationService
{
    Task<TeacherApplicationResponse> ApplyAsync(CreateTeacherApplicationRequest request, CancellationToken ct = default);
    Task<PaginationResponse<TeacherApplicationResponse>> GetAllAsync(PaginationRequest page, CancellationToken ct = default);
    Task<TeacherApplicationResponse> GetByIdAsync(long id, CancellationToken ct = default);
    Task<TeacherApplicationResponse> ApproveAsync(long id, string? note, CancellationToken ct = default);
    Task<TeacherApplicationResponse> RejectAsync(long id, string? note, CancellationToken ct = default);
}
