using AssignmentManagement.Application.Assignments.Interfaces;
using AssignmentManagement.Application.Assignments.Services;
using AssignmentManagement.Application.Authentication.Interfaces;
using AssignmentManagement.Application.Authentication.Services;
using AssignmentManagement.Application.Classes.Interfaces;
using AssignmentManagement.Application.Classes.Services;
using AssignmentManagement.Application.Common.Interfaces;
using AssignmentManagement.Application.Common.Services;
using AssignmentManagement.Application.Courses.Interfaces;
using AssignmentManagement.Application.Courses.Services;
using AssignmentManagement.Application.Enrollments.Interfaces;
using AssignmentManagement.Application.Enrollments.Processors;
using AssignmentManagement.Application.Enrollments.Services;
using AssignmentManagement.Application.Resubmissions.Interfaces;
using AssignmentManagement.Application.Resubmissions.Services;
using AssignmentManagement.Application.Results.Interfaces;
using AssignmentManagement.Application.Results.Services;
using AssignmentManagement.Application.Submissions.Interfaces;
using AssignmentManagement.Application.Submissions.Services;
using AssignmentManagement.Application.TeacherApplications.Interfaces;
using AssignmentManagement.Application.TeacherApplications.Services;
using AssignmentManagement.Application.Users.Interfaces;
using AssignmentManagement.Application.Users.Services;
using Microsoft.Extensions.DependencyInjection;

namespace AssignmentManagement.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IAccessGuard, AccessGuard>();

        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<ICourseService, CourseService>();
        services.AddScoped<IClassService, ClassService>();
        services.AddScoped<ITeacherApplicationService, TeacherApplicationService>();
        services.AddScoped<IEnrollmentService, EnrollmentService>();
        services.AddScoped<IEnrollmentProcessor, EnrollmentProcessor>();
        services.AddScoped<IAssignmentService, AssignmentService>();
        services.AddScoped<ISubmissionService, SubmissionService>();
        services.AddScoped<IResubmissionService, ResubmissionService>();
        services.AddScoped<IResultService, ResultService>();

        return services;
    }
}
