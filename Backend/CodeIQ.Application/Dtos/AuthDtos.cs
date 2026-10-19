namespace CodeIQ.Application.Dtos;

public sealed record LoginRequest(string Email, string Password);

public sealed record LoginResponse(string Token, string Email, string DisplayName, DateTime ExpiresAtUtc);
