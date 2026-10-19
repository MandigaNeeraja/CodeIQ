using CodeIQ.Domain.Enums;

namespace CodeIQ.Domain.Entities;

public class CandidateAttempt
{
    public Guid Id { get; set; }
    public Guid AssessmentId { get; set; }
    /// <summary>Secret token for candidate session (in URL after registration).</summary>
    public string AccessToken { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public AttemptStatus Status { get; set; } = AttemptStatus.Registered;
    public DateTime RegisteredAtUtc { get; set; }
    public DateTime? StartedAtUtc { get; set; }
    public DateTime? SubmittedAtUtc { get; set; }
    public DateTime? ExpiresAtUtc { get; set; }
    public int TotalScore { get; set; }
    public int MaxScore { get; set; }

    public Assessment Assessment { get; set; } = null!;
    public ICollection<QuestionSubmission> Submissions { get; set; } = new List<QuestionSubmission>();
}
