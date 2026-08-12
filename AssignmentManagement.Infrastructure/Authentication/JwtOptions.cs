namespace AssignmentManagement.Infrastructure.Authentication;

public class JwtOptions
{
    public string Secret { get; set; } = string.Empty;
    public string Issuer { get; set; } = "AssignmentManagement";
    public string Audience { get; set; } = "AssignmentManagement.Client";
    public int ExpiryMinutes { get; set; } = 120;
}
