namespace CodeIQ.Domain.Entities;

public class QuestionBankTestCase
{
    public Guid Id { get; set; }
    public Guid QuestionBankItemId { get; set; }
    public string Input { get; set; } = string.Empty;
    public string ExpectedOutput { get; set; } = string.Empty;
    public bool IsSample { get; set; }
    public int OrderIndex { get; set; }

    public QuestionBankItem QuestionBankItem { get; set; } = null!;
}
