using AssignmentManagement.Application.Users.DTOs;
using AssignmentManagement.Application.Users.Interfaces;
using AssignmentManagement.Common.Constants;
using AssignmentManagement.Common.Models;
using AssignmentManagement.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssignmentManagement.API.Controllers;

[Route("api/users")]
[Authorize(Roles = ApplicationConstants.Roles.Admin)]
public class UsersController : BaseApiController
{
    private readonly IUserService _users;
    public UsersController(IUserService users) => _users = users;

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] PaginationRequest page, [FromQuery] UserRole? role, CancellationToken ct)
        => Success(await _users.GetAllAsync(page, role, ct));

    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetById(long id, CancellationToken ct)
        => Success(await _users.GetByIdAsync(id, ct));

    [HttpPost]
    public async Task<IActionResult> Create(CreateUserRequest request, CancellationToken ct)
        => Success(await _users.CreateAsync(request, ct), "User created.");

    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(long id, UpdateUserRequest request, CancellationToken ct)
        => Success(await _users.UpdateAsync(id, request, ct), "User updated.");

    [HttpPatch("{id:long}/activate")]
    public async Task<IActionResult> Activate(long id, CancellationToken ct)
    {
        await _users.SetActiveAsync(id, true, ct);
        return Success("User activated.");
    }

    [HttpPatch("{id:long}/deactivate")]
    public async Task<IActionResult> Deactivate(long id, CancellationToken ct)
    {
        await _users.SetActiveAsync(id, false, ct);
        return Success("User deactivated.");
    }
}
