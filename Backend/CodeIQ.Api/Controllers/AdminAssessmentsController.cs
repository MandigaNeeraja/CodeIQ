using CodeIQ.Application.Abstractions;
using CodeIQ.Application.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CodeIQ.Api.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/admin/assessments")]
public sealed class AdminAssessmentsController : ControllerBase
{
    private readonly IAssessmentService _assessmentService;
    private readonly IQuestionBankService _questionBank;
    private readonly IAdminDashboardService _dashboard;

    public AdminAssessmentsController(
        IAssessmentService assessmentService,
        IQuestionBankService questionBank,
        IAdminDashboardService dashboard)
    {
        _assessmentService = assessmentService;
        _questionBank = questionBank;
        _dashboard = dashboard;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AssessmentSummaryDto>>> List(CancellationToken cancellationToken)
    {
        var items = await _assessmentService.ListAsync(cancellationToken);
        return Ok(items);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AssessmentDetailDto>> Get(Guid id, CancellationToken cancellationToken)
    {
        var item = await _assessmentService.GetByIdAsync(id, cancellationToken);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    public async Task<ActionResult<AssessmentDetailDto>> Create([FromBody] CreateAssessmentRequest request, CancellationToken cancellationToken)
    {
        var created = await _assessmentService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<AssessmentDetailDto>> Update(Guid id, [FromBody] UpdateAssessmentRequest request, CancellationToken cancellationToken)
    {
        var updated = await _assessmentService.UpdateAsync(id, request, cancellationToken);
        return updated is null ? NotFound() : Ok(updated);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await _assessmentService.DeleteAsync(id, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }

    [HttpPost("{assessmentId:guid}/questions")]
    public async Task<ActionResult<QuestionAdminDto>> CreateQuestion(
        Guid assessmentId,
        [FromBody] UpsertQuestionRequest request,
        CancellationToken cancellationToken)
    {
        var question = await _assessmentService.UpsertQuestionAsync(assessmentId, null, request, cancellationToken);
        return question is null ? NotFound() : Ok(question);
    }

    [HttpPut("{assessmentId:guid}/questions/{questionId:guid}")]
    public async Task<ActionResult<QuestionAdminDto>> UpdateQuestion(
        Guid assessmentId,
        Guid questionId,
        [FromBody] UpsertQuestionRequest request,
        CancellationToken cancellationToken)
    {
        var question = await _assessmentService.UpsertQuestionAsync(assessmentId, questionId, request, cancellationToken);
        return question is null ? NotFound() : Ok(question);
    }

    [HttpDelete("{assessmentId:guid}/questions/{questionId:guid}")]
    public async Task<IActionResult> DeleteQuestion(Guid assessmentId, Guid questionId, CancellationToken cancellationToken)
    {
        var deleted = await _assessmentService.DeleteQuestionAsync(assessmentId, questionId, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }

    [HttpGet("{id:guid}/results")]
    public async Task<ActionResult<IReadOnlyList<AttemptResultDto>>> Results(Guid id, CancellationToken cancellationToken)
    {
        var results = await _assessmentService.GetResultsAsync(id, cancellationToken);
        return Ok(results);
    }

    [HttpGet("{id:guid}/results/export")]
    public async Task<IActionResult> ExportResults(Guid id, CancellationToken cancellationToken)
    {
        var csv = await _assessmentService.ExportResultsCsvAsync(id, cancellationToken);
        return File(System.Text.Encoding.UTF8.GetBytes(csv), "text/csv", "results.csv");
    }

    [HttpGet("{id:guid}/live")]
    public async Task<ActionResult<AssessmentLiveMonitorDto>> Live(Guid id, CancellationToken cancellationToken)
    {
        var monitor = await _dashboard.GetLiveMonitorAsync(id, cancellationToken);
        return monitor is null ? NotFound() : Ok(monitor);
    }

    [HttpGet("{id:guid}/analytics")]
    public async Task<ActionResult<AssessmentAnalyticsDto>> Analytics(Guid id, CancellationToken cancellationToken)
    {
        var analytics = await _dashboard.GetAnalyticsAsync(id, cancellationToken);
        return analytics is null ? NotFound() : Ok(analytics);
    }

    [HttpPost("{assessmentId:guid}/questions/from-bank")]
    public async Task<ActionResult<AddFromQuestionBankResponse>> AddFromBank(
        Guid assessmentId,
        [FromBody] AddFromQuestionBankRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _questionBank.AddToAssessmentAsync(assessmentId, request, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }
}
