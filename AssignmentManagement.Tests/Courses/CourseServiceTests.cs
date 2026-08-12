using AssignmentManagement.Application.Courses.DTOs;
using AssignmentManagement.Application.Courses.Services;
using AssignmentManagement.Common.Exceptions;
using AssignmentManagement.Common.Models;
using AssignmentManagement.Tests.Common;
using Xunit;

namespace AssignmentManagement.Tests.Courses;

public class CourseServiceTests
{
    private static CourseService NewService(out AssignmentManagement.Infrastructure.Persistence.AppDbContext db)
    {
        db = TestHelpers.NewInMemoryDb();
        return new CourseService(db, new NoOpCacheService());
    }

    [Fact]
    public async Task Create_Then_Get_Works()
    {
        var service = NewService(out _);
        var created = await service.CreateAsync(new CreateCourseRequest
        {
            Code = "cse220", Name = "Data Structures", CreditHours = 3
        });

        Assert.True(created.Id > 0);
        Assert.Equal("CSE220", created.Code); // normalized to upper-case

        var fetched = await service.GetByIdAsync(created.Id);
        Assert.Equal("Data Structures", fetched.Name);
    }

    [Fact]
    public async Task Create_DuplicateCode_Throws()
    {
        var service = NewService(out _);
        await service.CreateAsync(new CreateCourseRequest { Code = "CSE101", Name = "Intro" });

        await Assert.ThrowsAsync<BusinessException>(() =>
            service.CreateAsync(new CreateCourseRequest { Code = "cse101", Name = "Dup" }));
    }

    [Fact]
    public async Task GetAll_Paginates()
    {
        var service = NewService(out _);
        for (var i = 1; i <= 5; i++)
            await service.CreateAsync(new CreateCourseRequest { Code = $"C{i:000}", Name = $"Course {i}" });

        var page = await service.GetAllAsync(new PaginationRequest { Page = 1, PageSize = 2 }, null);
        Assert.Equal(5, page.TotalItems);
        Assert.Equal(3, page.TotalPages);
        Assert.Equal(2, System.Linq.Enumerable.Count(page.Items));
    }
}
