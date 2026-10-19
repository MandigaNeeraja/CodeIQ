namespace CodeIQ.Application.Options;

public class SeedAdminOptions
{
    public const string SectionName = "SeedAdmin";
    public string Email { get; set; } = "admin@codeiq.local";
    public string Password { get; set; } = "Admin@123";
    public string DisplayName { get; set; } = "Platform Admin";
}
