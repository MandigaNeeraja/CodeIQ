using CodeIQ.Application.Dtos;

namespace CodeIQ.Application.Abstractions;

public interface ICandidateSessionService
{
    Task<PublicAssessmentDto?> GetPublicAssessmentAsync(string slug, CancellationToken cancellationToken = default);
    Task<RegisterCandidateResponse> RegisterAsync(string slug, RegisterCandidateRequest request, CancellationToken cancellationToken = default);
    Task<CandidateSessionDto?> GetSessionAsync(string accessToken, CancellationToken cancellationToken = default);
    Task<CandidateSessionDto?> StartAttemptAsync(string accessToken, CancellationToken cancellationToken = default);
    Task<RunCodeResponse?> RunSamplesAsync(string accessToken, Guid questionId, RunCodeRequest request, CancellationToken cancellationToken = default);
    Task<SubmitQuestionResponse?> SubmitQuestionAsync(string accessToken, Guid questionId, SubmitQuestionRequest request, CancellationToken cancellationToken = default);
    Task<CandidateSessionDto?> FinalizeAttemptAsync(string accessToken, CancellationToken cancellationToken = default);
}
