using CodeIQ.Domain.Enums;

namespace CodeIQ.Application.Dtos;

public sealed record AdminDashboardOverviewDto(
    int TotalAssessments,
    int PublishedAssessments,
    int TotalCandidates,
    int ActiveAttempts,
    int QuestionBankCount,
    IReadOnlyList<AssessmentSummaryDto> RecentAssessments);

public sealed record LiveAttemptDto(
    Guid AttemptId,
    string FullName,
    string Email,
    AttemptStatus Status,
    DateTime? StartedAtUtc,
    DateTime? ExpiresAtUtc,
    int AnsweredQuestions,
    int TotalQuestions,
    int CurrentScore,
    int MaxScore);

public sealed record AssessmentLiveMonitorDto(
    Guid AssessmentId,
    string Title,
    int InProgressCount,
    int RegisteredCount,
    int SubmittedCount,
    IReadOnlyList<LiveAttemptDto> Attempts);

public sealed record AssessmentAnalyticsDto(
    Guid AssessmentId,
    string Title,
    int TotalAttempts,
    int SubmittedAttempts,
    double AverageScore,
    int MaxPossibleScore,
    int QuestionCount,
    IReadOnlyList<QuestionAnalyticsDto> PerQuestion);

public sealed record QuestionAnalyticsDto(
    Guid QuestionId,
    string Title,
    int AttemptsWithFinalSubmission,
    double AverageScore,
    int MaxScore);
