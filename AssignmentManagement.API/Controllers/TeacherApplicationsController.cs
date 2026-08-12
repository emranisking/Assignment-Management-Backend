using AssignmentManagement.Application.TeacherApplications.DTOs;
using AssignmentManagement.Application.TeacherApplications.Interfaces;
using AssignmentManagement.Common.Constants;
using AssignmentManagement.Common.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssignmentManagement.API.Controllers;

[Route("api/teacher-applications")]
[Authorize]
public class TeacherApplicationsController : BaseApiController
{
    private readonly ITeacherApplicationService _service;
    public TeacherApplicationsController(ITeacherApplicationService service) => _service = service;

    [HttpPost]
    [Authorize(Roles = ApplicationConstants.Roles.Teacher)]
    public async Task<IActionResult> Apply(CreateTeacherApplicationRequest request, CancellationToken ct)
        => Success(await _service.ApplyAsync(request, ct), "Application submitted.");

    [HttpGet]
    [Authorize(Roles = ApplicationConstants.Roles.Admin + "," + ApplicationConstants.Roles.Teacher)]
    public async Task<IActionResult> GetAll([FromQuery] PaginationRequest page, CancellationToken ct)
        => Success(await _service.GetAllAsync(page, ct));

    [HttpGet("{id:long}")]
    [Authorize(Roles = ApplicationConstants.Roles.Admin + "," + ApplicationConstants.Roles.Teacher)]
    public async Task<IActionResult> GetById(long id, CancellationToken ct)
        => Success(await _service.GetByIdAsync(id, ct));

    [HttpPatch("{id:long}/approve")]
    [Authorize(Roles = ApplicationConstants.Roles.Admin)]
    public async Task<IActionResult> Approve(long id, DecisionRequest request, CancellationToken ct)
        => Success(await _service.ApproveAsync(id, request.Note, ct), "Application approved.");

    [HttpPatch("{id:long}/reject")]
    [Authorize(Roles = ApplicationConstants.Roles.Admin)]
    public async Task<IActionResult> Reject(long id, DecisionRequest request, CancellationToken ct)
        => Success(await _service.RejectAsync(id, request.Note, ct), "Application rejected.");
}
