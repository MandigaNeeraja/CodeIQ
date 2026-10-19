using CodeIQ.Application.Dtos;

namespace CodeIQ.Application.Abstractions;

public interface IAdminDashboardService
{
    Task<AdminDashboardOverviewDto> GetOverviewAsync(CancellationToken cancellationToken = default);
    Task<AssessmentLiveMonitorDto?> GetLiveMonitorAsync(Guid assessmentId, CancellationToken cancellationToken = default);
    Task<AssessmentAnalyticsDto?> GetAnalyticsAsync(Guid assessmentId, CancellationToken cancellationToken = default);
}
