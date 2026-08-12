using AssignmentManagement.Application.Courses.DTOs;
using AssignmentManagement.Application.Courses.Interfaces;
using AssignmentManagement.Common.Constants;
using AssignmentManagement.Common.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssignmentManagement.API.Controllers;

[Route("api/courses")]
[Authorize]
public class CoursesController : BaseApiController
{
    private readonly ICourseService _courses;
    public CoursesController(ICourseService courses) => _courses = courses;

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] PaginationRequest page, [FromQuery] string? search, CancellationToken ct)
        => Success(await _courses.GetAllAsync(page, search, ct));

    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetById(long id, CancellationToken ct)
        => Success(await _courses.GetByIdAsync(id, ct));

    [HttpPost]
    [Authorize(Roles = ApplicationConstants.Roles.Admin)]
    public async Task<IActionResult> Create(CreateCourseRequest request, CancellationToken ct)
        => Success(await _courses.CreateAsync(request, ct), "Course created.");

    [HttpPut("{id:long}")]
    [Authorize(Roles = ApplicationConstants.Roles.Admin)]
    public async Task<IActionResult> Update(long id, UpdateCourseRequest request, CancellationToken ct)
        => Success(await _courses.UpdateAsync(id, request, ct), "Course updated.");

    [HttpDelete("{id:long}")]
    [Authorize(Roles = ApplicationConstants.Roles.Admin)]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        await _courses.DeleteAsync(id, ct);
        return Success("Course deleted.");
    }
}
