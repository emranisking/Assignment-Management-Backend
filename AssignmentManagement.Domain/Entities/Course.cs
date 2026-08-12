using AssignmentManagement.Common.Models;

namespace AssignmentManagement.Domain.Entities;

public class Course : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int CreditHours { get; set; } = 3;

    public ICollection<Class> Classes { get; set; } = new List<Class>();
}
