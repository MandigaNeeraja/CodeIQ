using CodeIQ.Application.Abstractions;
using CodeIQ.Application.Dtos;
using CodeIQ.Domain.Enums;
using CodeIQ.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CodeIQ.Infrastructure.Services;

public sealed class AdminDashboardService : IAdminDashboardService
{
    private readonly AppDbContext _db;

    public AdminDashboardService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<AdminDashboardOverviewDto> GetOverviewAsync(CancellationToken cancellationToken = default)
    {
        var assessments = await _db.Assessments.AsNoTracking().ToListAsync(cancellationToken);
        var attempts = await _db.CandidateAttempts.AsNoTracking().ToListAsync(cancellationToken);

        var recent = await _db.Assessments
            .AsNoTracking()
            .OrderByDescending(x => x.UpdatedAtUtc)
            .Take(5)
            .Select(x => new AssessmentSummaryDto(
                x.Id,
                x.Title,
                x.Slug,
                x.IsPublished,
                x.DurationMinutes,
                x.Questions.Count,
                x.Attempts.Count,
                x.UpdatedAtUtc))
            .ToListAsync(cancellationToken);

        return new AdminDashboardOverviewDto(
            assessments.Count,
            assessments.Count(x => x.IsPublished),
            attempts.Count,
            attempts.Count(x => x.Status == AttemptStatus.InProgress),
            await _db.QuestionBankItems.CountAsync(cancellationToken),
            recent);
    }

    public async Task<AssessmentLiveMonitorDto?> GetLiveMonitorAsync(
        Guid assessmentId,
        CancellationToken cancellationToken = default)
    {
        var assessment = await _db.Assessments
            .AsNoTracking()
            .Include(x => x.Questions)
            .Include(x => x.Attempts)
            .ThenInclude(a => a.Submissions)
            .FirstOrDefaultAsync(x => x.Id == assessmentId, cancellationToken);

        if (assessment is null)
        {
            return null;
        }

        var now = DateTime.UtcNow;
        var rows = assessment.Attempts
            .OrderByDescending(a => a.StartedAtUtc)
            .Select(a =>
            {
                var finalQuestionIds = a.Submissions
                    .Where(s => s.IsFinal)
                    .Select(s => s.QuestionId)
                    .Distinct()
                    .Count();

                return new LiveAttemptDto(
                    a.Id,
                    a.FullName,
                    a.Email,
                    a.Status == AttemptStatus.InProgress && a.ExpiresAtUtc is not null && now > a.ExpiresAtUtc
                        ? AttemptStatus.Expired
                        : a.Status,
                    a.StartedAtUtc,
                    a.ExpiresAtUtc,
                    finalQuestionIds,
                    assessment.Questions.Count,
                    a.TotalScore,
                    a.MaxScore);
            })
            .ToList();

        return new AssessmentLiveMonitorDto(
            assessment.Id,
            assessment.Title,
            rows.Count(x => x.Status == AttemptStatus.InProgress),
            rows.Count(x => x.Status == AttemptStatus.Registered),
            rows.Count(x => x.Status is AttemptStatus.Submitted or AttemptStatus.Expired),
            rows);
    }

    public async Task<AssessmentAnalyticsDto?> GetAnalyticsAsync(
        Guid assessmentId,
        CancellationToken cancellationToken = default)
    {
        var assessment = await _db.Assessments
            .AsNoTracking()
            .Include(x => x.Questions)
            .Include(x => x.Attempts)
            .ThenInclude(a => a.Submissions)
            .FirstOrDefaultAsync(x => x.Id == assessmentId, cancellationToken);

        if (assessment is null)
        {
            return null;
        }

        var submitted = assessment.Attempts
            .Where(a => a.Status is AttemptStatus.Submitted or AttemptStatus.Expired)
            .ToList();

        var avg = submitted.Count == 0
            ? 0
            : submitted.Average(a => a.TotalScore);

        var maxPossible = assessment.Questions.Sum(q => q.Points);

        var perQuestion = assessment.Questions
            .OrderBy(q => q.OrderIndex)
            .Select(q =>
            {
                var finals = submitted
                    .SelectMany(a => a.Submissions.Where(s => s.QuestionId == q.Id && s.IsFinal))
                    .GroupBy(s => s.AttemptId)
                    .Select(g => g.OrderByDescending(s => s.SubmittedAtUtc).First())
                    .ToList();

                return new QuestionAnalyticsDto(
                    q.Id,
                    q.Title,
                    finals.Count,
                    finals.Count == 0 ? 0 : finals.Average(f => f.Score),
                    q.Points);
            })
            .ToList();

        return new AssessmentAnalyticsDto(
            assessment.Id,
            assessment.Title,
            assessment.Attempts.Count,
            submitted.Count,
            avg,
            maxPossible,
            assessment.Questions.Count,
            perQuestion);
    }
}
