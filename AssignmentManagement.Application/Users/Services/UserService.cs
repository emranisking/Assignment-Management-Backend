using AssignmentManagement.Application.Common.Interfaces;
using AssignmentManagement.Application.Users.DTOs;
using AssignmentManagement.Application.Users.Interfaces;
using AssignmentManagement.Common.Constants;
using AssignmentManagement.Common.Exceptions;
using AssignmentManagement.Common.Models;
using AssignmentManagement.Domain.Entities;
using AssignmentManagement.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace AssignmentManagement.Application.Users.Services;

public class UserService : IUserService
{
    private readonly IAppDbContext _db;
    private readonly IPasswordHasher _hasher;
    private readonly ICacheService _cache;

    public UserService(IAppDbContext db, IPasswordHasher hasher, ICacheService cache)
    {
        _db = db;
        _hasher = hasher;
        _cache = cache;
    }

    public async Task<PaginationResponse<UserResponse>> GetAllAsync(
        PaginationRequest page, UserRole? role, CancellationToken ct = default)
    {
        var key = await _cache.BuildVersionedKeyAsync(
            ApplicationConstants.Cache.UserPrefix, $"p{page.Page}-s{page.PageSize}-r{role}", ct);

        return await _cache.GetOrSetAsync(key, async () =>
        {
            var query = _db.Users.AsNoTracking().OrderByDescending(u => u.Id).AsQueryable();
            if (role.HasValue) query = query.Where(u => u.Role == role.Value);

            var total = await query.CountAsync(ct);
            var items = await query.Skip(page.Skip).Take(page.PageSize)
                .Select(u => Map(u)).ToListAsync(ct);

            return new PaginationResponse<UserResponse>(items, total, page.Page, page.PageSize);
        }, ct: ct);
    }

    public async Task<UserResponse> GetByIdAsync(long id, CancellationToken ct = default)
    {
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id, ct)
                   ?? throw new NotFoundException("User", id);
        return Map(user);
    }

    public async Task<UserResponse> CreateAsync(CreateUserRequest request, CancellationToken ct = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        if (await _db.Users.AnyAsync(u => u.Email == email, ct))
            throw new BusinessException("A user with this email already exists.", 409);

        var user = new User
        {
            Name = request.Name.Trim(),
            Email = email,
            PasswordHash = _hasher.Hash(request.Password),
            Role = request.Role,
            IsActive = true
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);
        await _cache.InvalidateGroupAsync(ApplicationConstants.Cache.UserPrefix, ct);
        return Map(user);
    }

    public async Task<UserResponse> UpdateAsync(long id, UpdateUserRequest request, CancellationToken ct = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id, ct)
                   ?? throw new NotFoundException("User", id);

        user.Name = request.Name.Trim();
        user.IsActive = request.IsActive;
        await _db.SaveChangesAsync(ct);
        await _cache.InvalidateGroupAsync(ApplicationConstants.Cache.UserPrefix, ct);
        return Map(user);
    }

    public async Task SetActiveAsync(long id, bool isActive, CancellationToken ct = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id, ct)
                   ?? throw new NotFoundException("User", id);
        user.IsActive = isActive;
        await _db.SaveChangesAsync(ct);
        await _cache.InvalidateGroupAsync(ApplicationConstants.Cache.UserPrefix, ct);
    }

    private static UserResponse Map(User u) => new()
    {
        Id = u.Id,
        Name = u.Name,
        Email = u.Email,
        Role = u.Role,
        IsActive = u.IsActive,
        CreatedAt = u.CreatedAt
    };
}
