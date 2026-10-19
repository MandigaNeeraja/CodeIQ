using CodeIQ.Application.Options;
using CodeIQ.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace CodeIQ.Infrastructure.Data;

public static class DbInitializer
{
    public static async Task InitializeAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var seedOptions = scope.ServiceProvider.GetRequiredService<IOptions<SeedAdminOptions>>().Value;

        await db.Database.MigrateAsync(cancellationToken);

        if (!await db.AdminUsers.AnyAsync(cancellationToken))
        {
            db.AdminUsers.Add(new AdminUser
            {
                Id = Guid.NewGuid(),
                Email = seedOptions.Email.Trim().ToLowerInvariant(),
                DisplayName = seedOptions.DisplayName,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(seedOptions.Password),
                CreatedAtUtc = DateTime.UtcNow,
            });
            await db.SaveChangesAsync(cancellationToken);
        }

        await QuestionBankSeeder.SeedAsync(db, cancellationToken);
    }
}
