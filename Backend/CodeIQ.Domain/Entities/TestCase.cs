namespace CodeIQ.Domain.Entities;

public class TestCase
{
    public Guid Id { get; set; }
    public Guid QuestionId { get; set; }
    public string Input { get; set; } = string.Empty;
    public string ExpectedOutput { get; set; } = string.Empty;
    public bool IsSample { get; set; }
    public int OrderIndex { get; set; }

    public Question Question { get; set; } = null!;
}
