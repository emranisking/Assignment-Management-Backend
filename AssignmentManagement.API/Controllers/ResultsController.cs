using AssignmentManagement.Application.Results.Interfaces;
using AssignmentManagement.Common.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssignmentManagement.API.Controllers;

[ApiController]
[Produces("application/json")]
[Authorize]
public class ResultsController : BaseApiController
{
    private readonly IResultService _results;
    public ResultsController(IResultService results) => _results = results;

    [HttpGet("api/results/me")]
    [Authorize(Roles = ApplicationConstants.Roles.Student)]
    public async Task<IActionResult> GetMyResults(CancellationToken ct)
        => Success(await _results.GetMyResultsAsync(ct));

    [HttpGet("api/classes/{classId:long}/results")]
    [Authorize(Roles = ApplicationConstants.Roles.Admin + "," + ApplicationConstants.Roles.Teacher)]
    public async Task<IActionResult> GetClassResults(long classId, CancellationToken ct)
        => Success(await _results.GetClassResultsAsync(classId, ct));
}
