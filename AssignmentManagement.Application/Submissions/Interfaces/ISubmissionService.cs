using AssignmentManagement.Application.Submissions.DTOs;
using AssignmentManagement.Common.Models;

namespace AssignmentManagement.Application.Submissions.Interfaces;

public interface ISubmissionService
{
    Task<SubmissionResponse> SubmitAsync(long assignmentId, SubmissionFileUpload upload, CancellationToken ct = default);
    Task<SubmissionResponse> AddVersionAsync(long submissionId, SubmissionFileUpload upload, CancellationToken ct = default);
    Task<PaginationResponse<SubmissionResponse>> GetByAssignmentAsync(long assignmentId, PaginationRequest page, CancellationToken ct = default);
    Task<SubmissionResponse> GetByIdAsync(long submissionId, CancellationToken ct = default);
    Task<IEnumerable<SubmissionVersionResponse>> GetVersionsAsync(long submissionId, CancellationToken ct = default);
    Task<DownloadResult> DownloadAsync(long submissionId, int? versionNumber, CancellationToken ct = default);
    Task<SubmissionResponse> GradeAsync(long submissionId, GradeSubmissionRequest request, CancellationToken ct = default);
}
