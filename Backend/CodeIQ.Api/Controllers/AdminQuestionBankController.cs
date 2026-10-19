using CodeIQ.Application.Abstractions;
using CodeIQ.Application.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CodeIQ.Api.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/admin/question-bank")]
public sealed class AdminQuestionBankController : ControllerBase
{
    private readonly IQuestionBankService _questionBank;

    public AdminQuestionBankController(IQuestionBankService questionBank)
    {
        _questionBank = questionBank;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<QuestionBankSummaryDto>>> List(
        [FromQuery] string? category,
        [FromQuery] string? difficulty,
        [FromQuery] string? search,
        CancellationToken cancellationToken)
    {
        return Ok(await _questionBank.ListAsync(category, difficulty, search, cancellationToken));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<QuestionBankDetailDto>> Get(Guid id, CancellationToken cancellationToken)
    {
        var item = await _questionBank.GetByIdAsync(id, cancellationToken);
        return item is null ? NotFound() : Ok(item);
    }
}
