using CodeIQ.Application.Abstractions;
using CodeIQ.Application.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace CodeIQ.Api.Controllers;

[ApiController]
[Route("api/public")]
public sealed class PublicAssessmentsController : ControllerBase
{
    private readonly ICandidateSessionService _sessions;

    public PublicAssessmentsController(ICandidateSessionService sessions)
    {
        _sessions = sessions;
    }

    [HttpGet("assessments/{slug}")]
    public async Task<ActionResult<PublicAssessmentDto>> GetAssessment(string slug, CancellationToken cancellationToken)
    {
        var assessment = await _sessions.GetPublicAssessmentAsync(slug, cancellationToken);
        return assessment is null ? NotFound() : Ok(assessment);
    }

    [HttpPost("assessments/{slug}/register")]
    public async Task<ActionResult<RegisterCandidateResponse>> Register(
        string slug,
        [FromBody] RegisterCandidateRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await _sessions.RegisterAsync(slug, request, cancellationToken);
            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("sessions/{accessToken}")]
    public async Task<ActionResult<CandidateSessionDto>> GetSession(string accessToken, CancellationToken cancellationToken)
    {
        var session = await _sessions.GetSessionAsync(accessToken, cancellationToken);
        return session is null ? NotFound() : Ok(session);
    }

    [HttpPost("sessions/{accessToken}/start")]
    public async Task<ActionResult<CandidateSessionDto>> Start(string accessToken, CancellationToken cancellationToken)
    {
        try
        {
            var session = await _sessions.StartAttemptAsync(accessToken, cancellationToken);
            return session is null ? NotFound() : Ok(session);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("sessions/{accessToken}/questions/{questionId:guid}/run")]
    public async Task<ActionResult<RunCodeResponse>> RunSamples(
        string accessToken,
        Guid questionId,
        [FromBody] RunCodeRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _sessions.RunSamplesAsync(accessToken, questionId, request, cancellationToken);
            return result is null ? NotFound() : Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("sessions/{accessToken}/questions/{questionId:guid}/submit")]
    public async Task<ActionResult<CandidateSubmitQuestionResponse>> Submit(
        string accessToken,
        Guid questionId,
        [FromBody] SubmitQuestionRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _sessions.SubmitQuestionAsync(accessToken, questionId, request, cancellationToken);
            if (result is null)
            {
                return NotFound();
            }

            return Ok(new CandidateSubmitQuestionResponse(
                result.PassedTests,
                result.TotalTests,
                result.IsFinal,
                result.Results));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("sessions/{accessToken}/finalize")]
    public async Task<ActionResult<CandidateSessionDto>> Finalize(string accessToken, CancellationToken cancellationToken)
    {
        var session = await _sessions.FinalizeAttemptAsync(accessToken, cancellationToken);
        return session is null ? NotFound() : Ok(session);
    }
}
