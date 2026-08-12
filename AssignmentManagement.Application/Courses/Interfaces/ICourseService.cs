using AssignmentManagement.Application.Courses.DTOs;
using AssignmentManagement.Common.Models;

namespace AssignmentManagement.Application.Courses.Interfaces;

public interface ICourseService
{
    Task<PaginationResponse<CourseResponse>> GetAllAsync(PaginationRequest page, string? search, CancellationToken ct = default);
    Task<CourseResponse> GetByIdAsync(long id, CancellationToken ct = default);
    Task<CourseResponse> CreateAsync(CreateCourseRequest request, CancellationToken ct = default);
    Task<CourseResponse> UpdateAsync(long id, UpdateCourseRequest request, CancellationToken ct = default);
    Task DeleteAsync(long id, CancellationToken ct = default);
}
