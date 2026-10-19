using CodeIQ.Application.Abstractions;
using CodeIQ.Application.Dtos;
using CodeIQ.Infrastructure.Data;
using CodeIQ.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;

namespace CodeIQ.Infrastructure.Services;

public sealed class AuthService : IAuthService
{
    private readonly AppDbContext _db;
    private readonly JwtTokenService _jwtTokenService;

    public AuthService(AppDbContext db, JwtTokenService jwtTokenService)
    {
        _db = db;
        _jwtTokenService = jwtTokenService;
    }

    public async Task<LoginResponse?> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await _db.AdminUsers.FirstOrDefaultAsync(x => x.Email == email, cancellationToken);
        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            return null;
        }

        var (token, expires) = _jwtTokenService.CreateAdminToken(user);
        return new LoginResponse(token, user.Email, user.DisplayName, expires);
    }
}
