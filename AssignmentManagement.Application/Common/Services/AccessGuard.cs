using AssignmentManagement.Application.Common.Interfaces;
using AssignmentManagement.Common.Exceptions;
using AssignmentManagement.Domain.Entities;
using AssignmentManagement.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace AssignmentManagement.Application.Common.Services;

public class AccessGuard : IAccessGuard
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;

    public AccessGuard(IAppDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Class> RequireManageableClassAsync(long classId, CancellationToken ct = default)
    {
        var cls = await _db.Classes.FirstOrDefaultAsync(c => c.Id == classId, ct)
                  ?? throw new NotFoundException("Class", classId);

        if (_currentUser.Role == UserRole.Admin) return cls;

        if (_currentUser.Role == UserRole.Teacher && cls.TeacherId == _currentUser.UserId)
            return cls;

        throw new ForbiddenException("You are not assigned to this class.");
    }

    public async Task RequireEnrolledAsync(long classId, CancellationToken ct = default)
    {
        var studentId = _currentUser.RequireUserId();
        var enrolled = await _db.Enrollments.AnyAsync(
            e => e.ClassId == classId && e.StudentId == studentId && e.Status == EnrollmentStatus.Active, ct);
        if (!enrolled)
            throw new ForbiddenException("You are not enrolled in this class.");
    }

    public async Task<Assignment> RequireManageableAssignmentAsync(long assignmentId, CancellationToken ct = default)
    {
        var assignment = await _db.Assignments.Include(a => a.Class)
            .FirstOrDefaultAsync(a => a.Id == assignmentId, ct)
            ?? throw new NotFoundException("Assignment", assignmentId);

        if (_currentUser.Role == UserRole.Admin) return assignment;

        if (_currentUser.Role == UserRole.Teacher && assignment.Class!.TeacherId == _currentUser.UserId)
            return assignment;

        throw new ForbiddenException("You do not own this assignment.");
    }
}
