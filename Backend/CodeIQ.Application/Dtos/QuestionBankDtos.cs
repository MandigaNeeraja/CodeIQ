using CodeIQ.Domain.Enums;

namespace CodeIQ.Application.Dtos;

public sealed record QuestionBankSummaryDto(
    Guid Id,
    string Slug,
    string Title,
    string Category,
    QuestionDifficulty Difficulty,
    string Tags,
    int DefaultPoints,
    int TestCaseCount,
    SupportedLanguage DefaultLanguage);

public sealed record QuestionBankDetailDto(
    Guid Id,
    string Slug,
    string Title,
    string Category,
    QuestionDifficulty Difficulty,
    string Tags,
    string ProblemStatement,
    SupportedLanguage DefaultLanguage,
    string StarterCode,
    int DefaultPoints,
    int TimeLimitSeconds,
    IReadOnlyList<TestCaseAdminDto> TestCases);

public sealed record AddFromQuestionBankRequest(IReadOnlyList<Guid> QuestionBankItemIds);

public sealed record AddFromQuestionBankResponse(IReadOnlyList<QuestionAdminDto> AddedQuestions);
