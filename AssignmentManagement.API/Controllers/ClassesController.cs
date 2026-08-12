using AssignmentManagement.Application.Assignments.DTOs;
using AssignmentManagement.Application.Assignments.Interfaces;
using AssignmentManagement.Application.Classes.DTOs;
using AssignmentManagement.Application.Classes.Interfaces;
using AssignmentManagement.Application.Enrollments.DTOs;
using AssignmentManagement.Application.Enrollments.Interfaces;
using AssignmentManagement.Common.Constants;
using AssignmentManagement.Common.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssignmentManagement.API.Controllers;

[Route("api/classes")]
[Authorize]
public class ClassesController : BaseApiController
{
    private readonly IClassService _classes;
    private readonly IAssignmentService _assignments;
    private readonly IEnrollmentService _enrollments;

    public ClassesController(IClassService classes, IAssignmentService assignments, IEnrollmentService enrollments)
    {
        _classes = classes;
        _assignments = assignments;
        _enrollments = enrollments;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] PaginationRequest page, [FromQuery] long? courseId, CancellationToken ct)
        => Success(await _classes.GetAllAsync(page, courseId, ct));

    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetById(long id, CancellationToken ct)
        => Success(await _classes.GetByIdAsync(id, ct));

    [HttpPost]
    [Authorize(Roles = ApplicationConstants.Roles.Admin)]
    public async Task<IActionResult> Create(CreateClassRequest request, CancellationToken ct)
        => Success(await _classes.CreateAsync(request, ct), "Class created.");

    [HttpPut("{id:long}")]
    [Authorize(Roles = ApplicationConstants.Roles.Admin)]
    public async Task<IActionResult> Update(long id, UpdateClassRequest request, CancellationToken ct)
        => Success(await _classes.UpdateAsync(id, request, ct), "Class updated.");

    [HttpPatch("{id:long}/assign-teacher")]
    [Authorize(Roles = ApplicationConstants.Roles.Admin)]
    public async Task<IActionResult> AssignTeacher(long id, AssignTeacherRequest request, CancellationToken ct)
        => Success(await _classes.AssignTeacherAsync(id, request.TeacherId, ct), "Teacher assigned.");

    [HttpPatch("{id:long}/status")]
    [Authorize(Roles = ApplicationConstants.Roles.Admin)]
    public async Task<IActionResult> SetStatus(long id, UpdateClassStatusRequest request, CancellationToken ct)
        => Success(await _classes.SetStatusAsync(id, request.Status, ct), "Class status updated.");

    [HttpGet("{id:long}/students")]
    [Authorize(Roles = ApplicationConstants.Roles.Admin + "," + ApplicationConstants.Roles.Teacher)]
    public async Task<IActionResult> GetStudents(long id, CancellationToken ct)
        => Success(await _classes.GetStudentsAsync(id, ct));

    // ----- Nested: assignments within a class -----

    [HttpGet("{classId:long}/assignments")]
    public async Task<IActionResult> GetAssignments(long classId, [FromQuery] PaginationRequest page, CancellationToken ct)
        => Success(await _assignments.GetByClassAsync(classId, page, ct));

    [HttpPost("{classId:long}/assignments")]
    [Authorize(Roles = ApplicationConstants.Roles.Admin + "," + ApplicationConstants.Roles.Teacher)]
    public async Task<IActionResult> CreateAssignment(long classId, CreateAssignmentRequest request, CancellationToken ct)
        => Success(await _assignments.CreateAsync(classId, request, ct), "Assignment created.");

    // ----- Nested: student enrollment request (async, returns 202) -----

    [HttpPost("{classId:long}/enrollment-requests")]
    [Authorize(Roles = ApplicationConstants.Roles.Student)]
    public async Task<IActionResult> RequestEnrollment(long classId, CancellationToken ct)
    {
        var result = await _enrollments.CreateRequestAsync(classId, ct);
        return StatusCode(StatusCodes.Status202Accepted, ApiResponse<EnrollmentRequestResponse>.Ok(result, result.Message));
    }
}
