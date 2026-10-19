using CodeIQ.Domain.Enums;

namespace CodeIQ.Domain.Entities;

public class QuestionBankItem
{
    public Guid Id { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public QuestionDifficulty Difficulty { get; set; } = QuestionDifficulty.Easy;
    public string Tags { get; set; } = string.Empty;
    public string ProblemStatement { get; set; } = string.Empty;
    public SupportedLanguage DefaultLanguage { get; set; } = SupportedLanguage.Python3;
    public string StarterCode { get; set; } = string.Empty;
    public int DefaultPoints { get; set; } = 100;
    public int TimeLimitSeconds { get; set; } = 5;
    public DateTime CreatedAtUtc { get; set; }

    public ICollection<QuestionBankTestCase> TestCases { get; set; } = new List<QuestionBankTestCase>();
}
