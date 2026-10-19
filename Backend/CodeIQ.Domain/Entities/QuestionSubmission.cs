using CodeIQ.Domain.Enums;

namespace CodeIQ.Domain.Entities;

public class QuestionSubmission
{
    public Guid Id { get; set; }
    public Guid AttemptId { get; set; }
    public Guid QuestionId { get; set; }
    public SupportedLanguage Language { get; set; }
    public string SourceCode { get; set; } = string.Empty;
    public bool IsFinal { get; set; }
    public int Score { get; set; }
    public int PassedTests { get; set; }
    public int TotalTests { get; set; }
    public DateTime SubmittedAtUtc { get; set; }
    public string? ResultJson { get; set; }

    public CandidateAttempt Attempt { get; set; } = null!;
    public Question Question { get; set; } = null!;
}
