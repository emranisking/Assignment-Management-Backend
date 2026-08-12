using AssignmentManagement.Application.Common.Interfaces;
using AssignmentManagement.Application.Results.DTOs;
using AssignmentManagement.Application.Results.Interfaces;
using AssignmentManagement.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace AssignmentManagement.Application.Results.Services;

public class ResultService : IResultService
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IAccessGuard _guard;

    public ResultService(IAppDbContext db, ICurrentUser currentUser, IAccessGuard guard)
    {
        _db = db;
        _currentUser = currentUser;
        _guard = guard;
    }

    public async Task<IEnumerable<ResultResponse>> GetMyResultsAsync(CancellationToken ct = default)
    {
        var studentId = _currentUser.RequireUserId();

        // Only assignments whose results the teacher has published are visible to students.
        return await _db.Submissions.AsNoTracking()
            .Where(s => s.StudentId == studentId
                        && s.Assignment!.ResultsPublished
                        && s.Status == SubmissionStatus.Graded)
            .Include(s => s.Assignment).ThenInclude(a => a!.Class).ThenInclude(c => c!.Course)
            .OrderByDescending(s => s.GradedAt)
            .Select(s => new ResultResponse
            {
                AssignmentId = s.AssignmentId,
                AssignmentTitle = s.Assignment!.Title,
                ClassId = s.Assignment!.ClassId,
                ClassName = s.Assignment!.Class!.Name,
                CourseCode = s.Assignment!.Class!.Course!.Code,
                MaxMarks = s.Assignment!.MaxMarks,
                Marks = s.Marks,
                Feedback = s.Feedback,
                SubmissionStatus = s.Status,
                GradedAt = s.GradedAt
            })
            .ToListAsync(ct);
    }

    public async Task<IEnumerable<ClassResultRowResponse>> GetClassResultsAsync(long classId, CancellationToken ct = default)
    {
        await _guard.RequireManageableClassAsync(classId, ct);

        return await _db.Submissions.AsNoTracking()
            .Where(s => s.Assignment!.ClassId == classId)
            .Include(s => s.Student)
            .Include(s => s.Assignment)
            .OrderBy(s => s.Student!.Name)
            .Select(s => new ClassResultRowResponse
            {
                StudentId = s.StudentId,
                StudentName = s.Student!.Name,
                AssignmentId = s.AssignmentId,
                AssignmentTitle = s.Assignment!.Title,
                MaxMarks = s.Assignment!.MaxMarks,
                Marks = s.Marks,
                Status = s.Status
            })
            .ToListAsync(ct);
    }
}
