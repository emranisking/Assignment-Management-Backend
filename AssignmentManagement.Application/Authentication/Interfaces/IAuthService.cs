using AssignmentManagement.Application.Authentication.DTOs;

namespace AssignmentManagement.Application.Authentication.Interfaces;

public interface IAuthService
{
    Task<UserProfileResponse> RegisterStudentAsync(RegisterRequest request, CancellationToken ct = default);
    Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken ct = default);
    Task<UserProfileResponse> GetCurrentAsync(CancellationToken ct = default);
}
