using AssignmentManagement.Application.Classes.DTOs;
using AssignmentManagement.Common.Models;
using AssignmentManagement.Domain.Enums;

namespace AssignmentManagement.Application.Classes.Interfaces;

public interface IClassService
{
    Task<PaginationResponse<ClassResponse>> GetAllAsync(PaginationRequest page, long? courseId, CancellationToken ct = default);
    Task<ClassResponse> GetByIdAsync(long id, CancellationToken ct = default);
    Task<ClassResponse> CreateAsync(CreateClassRequest request, CancellationToken ct = default);
    Task<ClassResponse> UpdateAsync(long id, UpdateClassRequest request, CancellationToken ct = default);
    Task<ClassResponse> AssignTeacherAsync(long id, long teacherId, CancellationToken ct = default);
    Task<ClassResponse> SetStatusAsync(long id, ClassStatus status, CancellationToken ct = default);
    Task<IEnumerable<ClassStudentResponse>> GetStudentsAsync(long id, CancellationToken ct = default);
}
