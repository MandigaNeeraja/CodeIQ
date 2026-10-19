using CodeIQ.Application.Abstractions;
using CodeIQ.Application.Dtos;
using CodeIQ.Domain.Entities;
using CodeIQ.Domain.Enums;
using CodeIQ.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CodeIQ.Infrastructure.Services;

public sealed class QuestionBankService : IQuestionBankService
{
    private readonly AppDbContext _db;

    public QuestionBankService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<QuestionBankSummaryDto>> ListAsync(
        string? category,
        string? difficulty,
        string? search,
        CancellationToken cancellationToken = default)
    {
        var query = _db.QuestionBankItems.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(category))
        {
            query = query.Where(x => x.Category == category);
        }

        if (!string.IsNullOrWhiteSpace(difficulty) &&
            Enum.TryParse<QuestionDifficulty>(difficulty, true, out var diff))
        {
            query = query.Where(x => x.Difficulty == diff);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLowerInvariant();
            query = query.Where(x =>
                x.Title.ToLower().Contains(term) ||
                x.Tags.ToLower().Contains(term) ||
                x.Category.ToLower().Contains(term));
        }

        return await query
            .OrderBy(x => x.Category)
            .ThenBy(x => x.Difficulty)
            .ThenBy(x => x.Title)
            .Select(x => new QuestionBankSummaryDto(
                x.Id,
                x.Slug,
                x.Title,
                x.Category,
                x.Difficulty,
                x.Tags,
                x.DefaultPoints,
                x.TestCases.Count,
                x.DefaultLanguage))
            .ToListAsync(cancellationToken);
    }

    public async Task<QuestionBankDetailDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var item = await _db.QuestionBankItems
            .AsNoTracking()
            .Include(x => x.TestCases.OrderBy(t => t.OrderIndex))
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (item is null)
        {
            return null;
        }

        return new QuestionBankDetailDto(
            item.Id,
            item.Slug,
            item.Title,
            item.Category,
            item.Difficulty,
            item.Tags,
            item.ProblemStatement,
            item.DefaultLanguage,
            item.StarterCode,
            item.DefaultPoints,
            item.TimeLimitSeconds,
            item.TestCases
                .Select(t => new TestCaseAdminDto(t.Id, t.Input, t.ExpectedOutput, t.IsSample, t.OrderIndex))
                .ToList());
    }

    public async Task<AddFromQuestionBankResponse?> AddToAssessmentAsync(
        Guid assessmentId,
        AddFromQuestionBankRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.QuestionBankItemIds.Count == 0)
        {
            throw new InvalidOperationException("Select at least one question from the bank.");
        }

        var assessment = await _db.Assessments
            .Include(x => x.Questions)
            .FirstOrDefaultAsync(x => x.Id == assessmentId, cancellationToken);

        if (assessment is null)
        {
            return null;
        }

        var bankItems = await _db.QuestionBankItems
            .Include(x => x.TestCases)
            .Where(x => request.QuestionBankItemIds.Contains(x.Id))
            .ToListAsync(cancellationToken);

        if (bankItems.Count == 0)
        {
            throw new InvalidOperationException("No matching question bank items found.");
        }

        var order = assessment.Questions.Count;
        var added = new List<QuestionAdminDto>();

        foreach (var bank in bankItems.OrderBy(x => x.Title))
        {
            if (assessment.Questions.Any(q => q.SourceQuestionBankItemId == bank.Id))
            {
                continue;
            }

            var question = new Question
            {
                Id = Guid.NewGuid(),
                AssessmentId = assessmentId,
                OrderIndex = order++,
                Title = bank.Title,
                ProblemStatement = bank.ProblemStatement,
                DefaultLanguage = bank.DefaultLanguage,
                StarterCode = bank.StarterCode,
                Points = bank.DefaultPoints,
                TimeLimitSeconds = bank.TimeLimitSeconds,
                SourceQuestionBankItemId = bank.Id,
            };

            foreach (var tc in bank.TestCases.OrderBy(x => x.OrderIndex))
            {
                question.TestCases.Add(new TestCase
                {
                    Id = Guid.NewGuid(),
                    QuestionId = question.Id,
                    Input = tc.Input,
                    ExpectedOutput = tc.ExpectedOutput,
                    IsSample = tc.IsSample,
                    OrderIndex = tc.OrderIndex,
                });
            }

            _db.Questions.Add(question);
            added.Add(MapQuestion(question));
        }

        assessment.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        return new AddFromQuestionBankResponse(added);
    }

    private static QuestionAdminDto MapQuestion(Question question) =>
        new(
            question.Id,
            question.OrderIndex,
            question.Title,
            question.ProblemStatement,
            question.DefaultLanguage,
            question.StarterCode,
            question.Points,
            question.TimeLimitSeconds,
            question.SourceQuestionBankItemId,
            question.TestCases
                .OrderBy(x => x.OrderIndex)
                .Select(x => new TestCaseAdminDto(x.Id, x.Input, x.ExpectedOutput, x.IsSample, x.OrderIndex))
                .ToList());
}
