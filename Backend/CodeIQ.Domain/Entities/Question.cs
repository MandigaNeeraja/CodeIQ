using CodeIQ.Domain.Enums;

namespace CodeIQ.Domain.Entities;

public class Question
{
    public Guid Id { get; set; }
    public Guid AssessmentId { get; set; }
    public int OrderIndex { get; set; }
    public string Title { get; set; } = string.Empty;
    public string ProblemStatement { get; set; } = string.Empty;
    public SupportedLanguage DefaultLanguage { get; set; } = SupportedLanguage.Python3;
    public string StarterCode { get; set; } = string.Empty;
    public int Points { get; set; } = 100;
    public int TimeLimitSeconds { get; set; } = 5;
    public Guid? SourceQuestionBankItemId { get; set; }

    public Assessment Assessment { get; set; } = null!;
    public ICollection<TestCase> TestCases { get; set; } = new List<TestCase>();
}
