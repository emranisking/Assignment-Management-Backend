using AssignmentManagement.Application.Submissions.DTOs;
using AssignmentManagement.Application.Submissions.Interfaces;
using AssignmentManagement.Common.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssignmentManagement.API.Controllers;

[Route("api/submissions")]
[Authorize]
public class SubmissionsController : BaseApiController
{
    private readonly ISubmissionService _submissions;
    public SubmissionsController(ISubmissionService submissions) => _submissions = submissions;

    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetById(long id, CancellationToken ct)
        => Success(await _submissions.GetByIdAsync(id, ct));

    [HttpGet("{id:long}/versions")]
    public async Task<IActionResult> GetVersions(long id, CancellationToken ct)
        => Success(await _submissions.GetVersionsAsync(id, ct));

    [HttpGet("{id:long}/download")]
    public async Task<IActionResult> Download(long id, [FromQuery] int? version, CancellationToken ct)
    {
        var result = await _submissions.DownloadAsync(id, version, ct);
        return File(result.Content, result.ContentType, result.FileName);
    }

    [HttpPost("{id:long}/versions")]
    [Authorize(Roles = ApplicationConstants.Roles.Student)]
    [RequestSizeLimit(20_000_000)]
    public async Task<IActionResult> AddVersion(long id, IFormFile file, CancellationToken ct)
    {
        var upload = AssignmentsController.ToUpload(file);
        return Success(await _submissions.AddVersionAsync(id, upload, ct), "New version uploaded.");
    }

    [HttpPost("{id:long}/grade")]
    [Authorize(Roles = ApplicationConstants.Roles.Admin + "," + ApplicationConstants.Roles.Teacher)]
    public async Task<IActionResult> Grade(long id, GradeSubmissionRequest request, CancellationToken ct)
        => Success(await _submissions.GradeAsync(id, request, ct), "Submission graded.");
}
