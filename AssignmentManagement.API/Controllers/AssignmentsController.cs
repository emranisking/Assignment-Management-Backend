using AssignmentManagement.Application.Assignments.DTOs;
using AssignmentManagement.Application.Assignments.Interfaces;
using AssignmentManagement.Application.Submissions.DTOs;
using AssignmentManagement.Application.Submissions.Interfaces;
using AssignmentManagement.Common.Constants;
using AssignmentManagement.Common.Exceptions;
using AssignmentManagement.Common.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssignmentManagement.API.Controllers;

[Route("api/assignments")]
[Authorize]
public class AssignmentsController : BaseApiController
{
    private readonly IAssignmentService _assignments;
    private readonly ISubmissionService _submissions;

    public AssignmentsController(IAssignmentService assignments, ISubmissionService submissions)
    {
        _assignments = assignments;
        _submissions = submissions;
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetById(long id, CancellationToken ct)
        => Success(await _assignments.GetByIdAsync(id, ct));

    [HttpPut("{id:long}")]
    [Authorize(Roles = ApplicationConstants.Roles.Admin + "," + ApplicationConstants.Roles.Teacher)]
    public async Task<IActionResult> Update(long id, UpdateAssignmentRequest request, CancellationToken ct)
        => Success(await _assignments.UpdateAsync(id, request, ct), "Assignment updated.");

    [HttpPatch("{id:long}/publish")]
    [Authorize(Roles = ApplicationConstants.Roles.Admin + "," + ApplicationConstants.Roles.Teacher)]
    public async Task<IActionResult> Publish(long id, CancellationToken ct)
        => Success(await _assignments.PublishAsync(id, ct), "Assignment published.");

    [HttpPatch("{id:long}/publish-results")]
    [Authorize(Roles = ApplicationConstants.Roles.Admin + "," + ApplicationConstants.Roles.Teacher)]
    public async Task<IActionResult> PublishResults(long id, CancellationToken ct)
        => Success(await _assignments.PublishResultsAsync(id, ct), "Results published.");

    [HttpDelete("{id:long}")]
    [Authorize(Roles = ApplicationConstants.Roles.Admin + "," + ApplicationConstants.Roles.Teacher)]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        await _assignments.DeleteAsync(id, ct);
        return Success("Assignment deleted.");
    }

    // ----- Submissions within an assignment -----

    [HttpGet("{assignmentId:long}/submissions")]
    [Authorize(Roles = ApplicationConstants.Roles.Admin + "," + ApplicationConstants.Roles.Teacher)]
    public async Task<IActionResult> GetSubmissions(long assignmentId, [FromQuery] PaginationRequest page, CancellationToken ct)
        => Success(await _submissions.GetByAssignmentAsync(assignmentId, page, ct));

    [HttpPost("{assignmentId:long}/submissions")]
    [Authorize(Roles = ApplicationConstants.Roles.Student)]
    [RequestSizeLimit(20_000_000)]
    public async Task<IActionResult> Submit(long assignmentId, IFormFile file, CancellationToken ct)
    {
        var upload = ToUpload(file);
        return Success(await _submissions.SubmitAsync(assignmentId, upload, ct), "Submission uploaded.");
    }

    internal static SubmissionFileUpload ToUpload(IFormFile? file)
    {
        if (file is null || file.Length == 0)
            throw new ValidationAppException("A PDF file is required (form field name: 'file').");

        return new SubmissionFileUpload
        {
            Content = file.OpenReadStream(),
            FileName = file.FileName,
            ContentType = file.ContentType,
            Length = file.Length
        };
    }
}
