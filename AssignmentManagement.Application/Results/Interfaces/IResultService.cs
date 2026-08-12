using AssignmentManagement.Application.Results.DTOs;

namespace AssignmentManagement.Application.Results.Interfaces;

public interface IResultService
{
    Task<IEnumerable<ResultResponse>> GetMyResultsAsync(CancellationToken ct = default);
    Task<IEnumerable<ClassResultRowResponse>> GetClassResultsAsync(long classId, CancellationToken ct = default);
}
