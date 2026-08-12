using AssignmentManagement.Application.Authentication.DTOs;
using AssignmentManagement.Application.Authentication.Interfaces;
using AssignmentManagement.Application.Common.Interfaces;
using AssignmentManagement.Common.Exceptions;
using AssignmentManagement.Domain.Entities;
using AssignmentManagement.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace AssignmentManagement.Application.Authentication.Services;

public class AuthService : IAuthService
{
    private readonly IAppDbContext _db;
    private readonly IPasswordHasher _hasher;
    private readonly IJwtService _jwt;
    private readonly ICurrentUser _currentUser;

    public AuthService(IAppDbContext db, IPasswordHasher hasher, IJwtService jwt, ICurrentUser currentUser)
    {
        _db = db;
        _hasher = hasher;
        _jwt = jwt;
        _currentUser = currentUser;
    }

    public async Task<UserProfileResponse> RegisterStudentAsync(RegisterRequest request, CancellationToken ct = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var exists = await _db.Users.AnyAsync(u => u.Email == email, ct);
        if (exists)
            throw new BusinessException("A user with this email already exists.", 409);

        var user = new User
        {
            Name = request.Name.Trim(),
            Email = email,
            PasswordHash = _hasher.Hash(request.Password),
            Role = UserRole.Student,
            IsActive = true
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);

        return Map(user);
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email, ct);

        if (user is null || !_hasher.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedAppException("Invalid email or password.");

        if (!user.IsActive)
            throw new ForbiddenException("This account is deactivated.");

        var token = _jwt.GenerateToken(user, out var expiresAt);

        return new LoginResponse
        {
            AccessToken = token,
            ExpiresAtUtc = expiresAt,
            User = Map(user)
        };
    }

    public async Task<UserProfileResponse> GetCurrentAsync(CancellationToken ct = default)
    {
        var id = _currentUser.RequireUserId();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id, ct)
                   ?? throw new NotFoundException("User", id);
        return Map(user);
    }

    private static UserProfileResponse Map(User user) => new()
    {
        Id = user.Id,
        Name = user.Name,
        Email = user.Email,
        Role = user.Role,
        IsActive = user.IsActive
    };
}
