using AssignmentManagement.Application.Enrollments.Interfaces;
using AssignmentManagement.Common.Constants;
using AssignmentManagement.Common.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssignmentManagement.API.Controllers;

/// <summary>Read side of enrollment: request status + a student's own enrollments.</summary>
[ApiController]
[Produces("application/json")]
[Authorize]
public class EnrollmentsController : BaseApiController
{
    private readonly IEnrollmentService _enrollments;
    public EnrollmentsController(IEnrollmentService enrollments) => _enrollments = enrollments;

    [HttpGet("api/enrollment-requests/{requestId:long}")]
    public async Task<IActionResult> GetRequest(long requestId, CancellationToken ct)
        => Success(await _enrollments.GetRequestAsync(requestId, ct));

    [HttpGet("api/enrollment-requests")]
    [Authorize(Roles = ApplicationConstants.Roles.Student)]
    public async Task<IActionResult> GetMyRequests([FromQuery] PaginationRequest page, CancellationToken ct)
        => Success(await _enrollments.GetMyRequestsAsync(page, ct));

    [HttpGet("api/enrollments/me")]
    [Authorize(Roles = ApplicationConstants.Roles.Student)]
    public async Task<IActionResult> GetMyEnrollments(CancellationToken ct)
        => Success(await _enrollments.GetMyEnrollmentsAsync(ct));

    [HttpDelete("api/enrollments/{enrollmentId:long}")]
    [Authorize(Roles = ApplicationConstants.Roles.Student)]
    public async Task<IActionResult> Drop(long enrollmentId, CancellationToken ct)
    {
        await _enrollments.DropAsync(enrollmentId, ct);
        return Success("Enrollment dropped.");
    }
}
