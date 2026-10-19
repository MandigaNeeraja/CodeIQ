using CodeIQ.Domain.Enums;

namespace CodeIQ.Application.Dtos;

public sealed record AssessmentSummaryDto(
    Guid Id,
    string Title,
    string Slug,
    bool IsPublished,
    int DurationMinutes,
    int QuestionCount,
    int AttemptCount,
    DateTime UpdatedAtUtc);

public sealed record AssessmentDetailDto(
    Guid Id,
    string Title,
    string Description,
    string Slug,
    int DurationMinutes,
    DateTime? OpensAtUtc,
    DateTime? ClosesAtUtc,
    bool IsPublished,
    string CandidateInstructions,
    bool ShuffleQuestions,
    bool AllowCodeRun,
    string InvitePath,
    IReadOnlyList<QuestionAdminDto> Questions);

public sealed record CreateAssessmentRequest(
    string Title,
    string Description,
    string? Slug,
    int DurationMinutes,
    DateTime? OpensAtUtc,
    DateTime? ClosesAtUtc,
    string? CandidateInstructions);

public sealed record UpdateAssessmentRequest(
    string Title,
    string Description,
    string Slug,
    int DurationMinutes,
    DateTime? OpensAtUtc,
    DateTime? ClosesAtUtc,
    bool IsPublished,
    string CandidateInstructions,
    bool ShuffleQuestions,
    bool AllowCodeRun);

public sealed record QuestionAdminDto(
    Guid Id,
    int OrderIndex,
    string Title,
    string ProblemStatement,
    SupportedLanguage DefaultLanguage,
    string StarterCode,
    int Points,
    int TimeLimitSeconds,
    Guid? SourceQuestionBankItemId,
    IReadOnlyList<TestCaseAdminDto> TestCases);

public sealed record UpsertQuestionRequest(
    string Title,
    string ProblemStatement,
    SupportedLanguage DefaultLanguage,
    string StarterCode,
    int Points,
    int TimeLimitSeconds,
    int OrderIndex,
    IReadOnlyList<UpsertTestCaseRequest> TestCases);

public sealed record TestCaseAdminDto(
    Guid Id,
    string Input,
    string ExpectedOutput,
    bool IsSample,
    int OrderIndex);

public sealed record UpsertTestCaseRequest(
    Guid? Id,
    string Input,
    string ExpectedOutput,
    bool IsSample,
    int OrderIndex);

public sealed record AttemptResultDto(
    Guid AttemptId,
    string FullName,
    string Email,
    AttemptStatus Status,
    int TotalScore,
    int MaxScore,
    DateTime? StartedAtUtc,
    DateTime? SubmittedAtUtc,
    IReadOnlyList<QuestionResultDto> Questions);

public sealed record QuestionResultDto(
    Guid QuestionId,
    string Title,
    int Score,
    int MaxScore,
    int PassedTests,
    int TotalTests,
    SupportedLanguage? Language,
    bool HasFinalSubmission);
