using CodeIQ.Domain.Enums;

namespace CodeIQ.Application.Dtos;

public sealed record PublicAssessmentDto(
    Guid Id,
    string Title,
    string Description,
    int DurationMinutes,
    int QuestionCount,
    bool RegistrationOpen);

public sealed record RegisterCandidateRequest(string FullName, string Email);

public sealed record RegisterCandidateResponse(
    Guid AttemptId,
    string AccessToken,
    string SessionPath);

public sealed record CandidateSessionDto(
    Guid AttemptId,
    Guid AssessmentId,
    string AssessmentTitle,
    AttemptStatus Status,
    DateTime? StartedAtUtc,
    DateTime? ExpiresAtUtc,
    int DurationMinutes,
    string CandidateInstructions,
    bool AllowCodeRun,
    IReadOnlyList<CandidateQuestionDto> Questions);

public sealed record CandidateQuestionDto(
    Guid Id,
    int OrderIndex,
    string Title,
    string ProblemStatement,
    SupportedLanguage DefaultLanguage,
    string StarterCode,
    int Points,
    int TimeLimitSeconds,
    IReadOnlyList<SampleTestCaseDto> SampleTestCases,
    CandidateQuestionProgressDto? Progress);

public sealed record SampleTestCaseDto(string Input, string ExpectedOutput);

public sealed record CandidateQuestionProgressDto(
    SupportedLanguage Language,
    string SourceCode,
    bool IsFinal,
    int? Score);

public sealed record RunCodeRequest(SupportedLanguage Language, string SourceCode);

public sealed record RunCodeResponse(
    IReadOnlyList<TestRunResultDto> Results,
    int Passed,
    int Total);

public sealed record TestRunResultDto(
    Guid? TestCaseId,
    bool IsSample,
    bool Passed,
    string Input,
    string ExpectedOutput,
    string ActualOutput,
    string? Stderr,
    int TimeMs,
    string? CompileError);

public sealed record SubmitQuestionRequest(
    SupportedLanguage Language,
    string SourceCode,
    bool Finalize);

public sealed record SubmitQuestionResponse(
    int Score,
    int MaxScore,
    int PassedTests,
    int TotalTests,
    bool IsFinal,
    IReadOnlyList<TestRunResultDto> Results);

/// <summary>Returned to candidates — scores are withheld until admin review.</summary>
public sealed record CandidateSubmitQuestionResponse(
    int PassedTests,
    int TotalTests,
    bool IsFinal,
    IReadOnlyList<TestRunResultDto> Results);
