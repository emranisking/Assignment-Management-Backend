using AssignmentManagement.Application.Common.Interfaces;
using AssignmentManagement.Application.Courses.DTOs;
using AssignmentManagement.Application.Courses.Interfaces;
using AssignmentManagement.Common.Constants;
using AssignmentManagement.Common.Exceptions;
using AssignmentManagement.Common.Models;
using AssignmentManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AssignmentManagement.Application.Courses.Services;

public class CourseService : ICourseService
{
    private readonly IAppDbContext _db;
    private readonly ICacheService _cache;
    private const string Group = ApplicationConstants.Cache.CoursePrefix;

    public CourseService(IAppDbContext db, ICacheService cache)
    {
        _db = db;
        _cache = cache;
    }

    public async Task<PaginationResponse<CourseResponse>> GetAllAsync(
        PaginationRequest page, string? search, CancellationToken ct = default)
    {
        var key = await _cache.BuildVersionedKeyAsync(
            Group, $"list-p{page.Page}-s{page.PageSize}-q{search}", ct);

        return await _cache.GetOrSetAsync(key, async () =>
        {
            var query = _db.Courses.AsNoTracking().AsQueryable();
            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                query = query.Where(c => c.Code.ToLower().Contains(s) || c.Name.ToLower().Contains(s));
            }

            query = query.OrderBy(c => c.Code);
            var total = await query.CountAsync(ct);
            var items = await query.Skip(page.Skip).Take(page.PageSize)
                .Select(c => Map(c)).ToListAsync(ct);

            return new PaginationResponse<CourseResponse>(items, total, page.Page, page.PageSize);
        }, ct: ct);
    }

    public async Task<CourseResponse> GetByIdAsync(long id, CancellationToken ct = default)
    {
        var key = await _cache.BuildVersionedKeyAsync(Group, $"id-{id}", ct);
        return await _cache.GetOrSetAsync(key, async () =>
        {
            var course = await _db.Courses.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id, ct)
                         ?? throw new NotFoundException("Course", id);
            return Map(course);
        }, ct: ct);
    }

    public async Task<CourseResponse> CreateAsync(CreateCourseRequest request, CancellationToken ct = default)
    {
        var code = request.Code.Trim().ToUpperInvariant();
        if (await _db.Courses.AnyAsync(c => c.Code == code, ct))
            throw new BusinessException($"Course code '{code}' already exists.", 409);

        var course = new Course
        {
            Code = code,
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            CreditHours = request.CreditHours
        };

        _db.Courses.Add(course);
        await _db.SaveChangesAsync(ct);
        await _cache.InvalidateGroupAsync(Group, ct);
        return Map(course);
    }

    public async Task<CourseResponse> UpdateAsync(long id, UpdateCourseRequest request, CancellationToken ct = default)
    {
        var course = await _db.Courses.FirstOrDefaultAsync(c => c.Id == id, ct)
                     ?? throw new NotFoundException("Course", id);

        course.Name = request.Name.Trim();
        course.Description = request.Description?.Trim();
        course.CreditHours = request.CreditHours;
        await _db.SaveChangesAsync(ct);
        await _cache.InvalidateGroupAsync(Group, ct);
        return Map(course);
    }

    public async Task DeleteAsync(long id, CancellationToken ct = default)
    {
        var course = await _db.Courses.Include(c => c.Classes)
            .FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw new NotFoundException("Course", id);

        if (course.Classes.Any())
            throw new BusinessException("Cannot delete a course that still has classes.");

        _db.Courses.Remove(course);
        await _db.SaveChangesAsync(ct);
        await _cache.InvalidateGroupAsync(Group, ct);
    }

    private static CourseResponse Map(Course c) => new()
    {
        Id = c.Id,
        Code = c.Code,
        Name = c.Name,
        Description = c.Description,
        CreditHours = c.CreditHours,
        CreatedAt = c.CreatedAt
    };
}
