namespace CodeIQ.Application.Options;

public class JwtOptions
{
    public const string SectionName = "Jwt";
    public string Issuer { get; set; } = "CodeIQ";
    public string Audience { get; set; } = "CodeIQ";
    public string Secret { get; set; } = string.Empty;
    public int ExpirationMinutes { get; set; } = 480;
}
