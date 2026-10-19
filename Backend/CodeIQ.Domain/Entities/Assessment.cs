namespace CodeIQ.Domain.Entities;

public class Assessment
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    /// <summary>URL slug for candidate invite link, e.g. /test/acme-sde-2026</summary>
    public string Slug { get; set; } = string.Empty;
    public int DurationMinutes { get; set; }
    public DateTime? OpensAtUtc { get; set; }
    public DateTime? ClosesAtUtc { get; set; }
    public bool IsPublished { get; set; }
    public string CandidateInstructions { get; set; } = string.Empty;
    public bool ShuffleQuestions { get; set; }
    public bool AllowCodeRun { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    public ICollection<Question> Questions { get; set; } = new List<Question>();
    public ICollection<CandidateAttempt> Attempts { get; set; } = new List<CandidateAttempt>();
}
