using CodeIQ.Application.Dtos;

namespace CodeIQ.Application.Abstractions;

public interface IAssessmentService
{
    Task<IReadOnlyList<AssessmentSummaryDto>> ListAsync(CancellationToken cancellationToken = default);
    Task<AssessmentDetailDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<AssessmentDetailDto> CreateAsync(CreateAssessmentRequest request, CancellationToken cancellationToken = default);
    Task<AssessmentDetailDto?> UpdateAsync(Guid id, UpdateAssessmentRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<QuestionAdminDto?> UpsertQuestionAsync(Guid assessmentId, Guid? questionId, UpsertQuestionRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteQuestionAsync(Guid assessmentId, Guid questionId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AttemptResultDto>> GetResultsAsync(Guid assessmentId, CancellationToken cancellationToken = default);
    Task<string> ExportResultsCsvAsync(Guid assessmentId, CancellationToken cancellationToken = default);
}
