using AssignmentManagement.Application.Resubmissions.DTOs;
using AssignmentManagement.Application.Resubmissions.Interfaces;
using AssignmentManagement.Common.Constants;
using AssignmentManagement.Common.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssignmentManagement.API.Controllers;

[ApiController]
[Produces("application/json")]
[Authorize]
public class ResubmissionsController : BaseApiController
{
    private readonly IResubmissionService _service;
    public ResubmissionsController(IResubmissionService service) => _service = service;

    [HttpPost("api/submissions/{submissionId:long}/resubmission-requests")]
    [Authorize(Roles = ApplicationConstants.Roles.Student)]
    public async Task<IActionResult> Create(long submissionId, CreateResubmissionRequest request, CancellationToken ct)
        => Success(await _service.CreateAsync(submissionId, request, ct), "Resubmission requested.");

    [HttpGet("api/resubmission-requests")]
    public async Task<IActionResult> GetAll([FromQuery] PaginationRequest page, CancellationToken ct)
        => Success(await _service.GetAllAsync(page, ct));

    [HttpPatch("api/resubmission-requests/{id:long}/approve")]
    [Authorize(Roles = ApplicationConstants.Roles.Admin + "," + ApplicationConstants.Roles.Teacher)]
    public async Task<IActionResult> Approve(long id, ResubmissionDecisionRequest request, CancellationToken ct)
        => Success(await _service.ApproveAsync(id, request.Note, ct), "Resubmission approved.");

    [HttpPatch("api/resubmission-requests/{id:long}/reject")]
    [Authorize(Roles = ApplicationConstants.Roles.Admin + "," + ApplicationConstants.Roles.Teacher)]
    public async Task<IActionResult> Reject(long id, ResubmissionDecisionRequest request, CancellationToken ct)
        => Success(await _service.RejectAsync(id, request.Note, ct), "Resubmission rejected.");
}
