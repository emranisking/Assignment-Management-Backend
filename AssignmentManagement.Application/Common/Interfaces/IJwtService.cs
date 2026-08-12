using AssignmentManagement.Domain.Entities;

namespace AssignmentManagement.Application.Common.Interfaces;

public interface IJwtService
{
    string GenerateToken(User user, out DateTime expiresAtUtc);
}
