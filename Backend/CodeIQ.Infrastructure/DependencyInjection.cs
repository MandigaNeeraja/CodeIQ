using CodeIQ.Application.Abstractions;
using CodeIQ.Infrastructure.Data;
using CodeIQ.Infrastructure.Runner;
using CodeIQ.Infrastructure.Security;
using CodeIQ.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CodeIQ.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlite(configuration.GetConnectionString("Default") ?? "Data Source=codeiq.db"));

        services.AddHttpClient<ICodeRunnerClient, CodeRunnerClient>();
        services.AddSingleton<JwtTokenService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IAssessmentService, AssessmentService>();
        services.AddScoped<IQuestionBankService, QuestionBankService>();
        services.AddScoped<IAdminDashboardService, AdminDashboardService>();
        services.AddScoped<ICandidateSessionService, CandidateSessionService>();

        return services;
    }
}
