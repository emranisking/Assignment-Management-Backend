using AssignmentManagement.Application.Users.DTOs;
using AssignmentManagement.Common.Models;
using AssignmentManagement.Domain.Enums;

namespace AssignmentManagement.Application.Users.Interfaces;

public interface IUserService
{
    Task<PaginationResponse<UserResponse>> GetAllAsync(PaginationRequest page, UserRole? role, CancellationToken ct = default);
    Task<UserResponse> GetByIdAsync(long id, CancellationToken ct = default);
    Task<UserResponse> CreateAsync(CreateUserRequest request, CancellationToken ct = default);
    Task<UserResponse> UpdateAsync(long id, UpdateUserRequest request, CancellationToken ct = default);
    Task SetActiveAsync(long id, bool isActive, CancellationToken ct = default);
}
