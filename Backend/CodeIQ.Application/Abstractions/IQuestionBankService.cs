using CodeIQ.Application.Dtos;

namespace CodeIQ.Application.Abstractions;

public interface IQuestionBankService
{
    Task<IReadOnlyList<QuestionBankSummaryDto>> ListAsync(
        string? category,
        string? difficulty,
        string? search,
        CancellationToken cancellationToken = default);

    Task<QuestionBankDetailDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<AddFromQuestionBankResponse?> AddToAssessmentAsync(
        Guid assessmentId,
        AddFromQuestionBankRequest request,
        CancellationToken cancellationToken = default);
}
