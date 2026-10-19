using CodeIQ.Application.Abstractions;
using CodeIQ.Application.Common;
using CodeIQ.Application.Dtos;
using CodeIQ.Domain.Entities;
using CodeIQ.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CodeIQ.Infrastructure.Services;

public sealed class AssessmentService : IAssessmentService
{
    private readonly AppDbContext _db;

    public AssessmentService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<AssessmentSummaryDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        return await _db.Assessments
            .AsNoTracking()
            .OrderByDescending(x => x.UpdatedAtUtc)
            .Select(x => new AssessmentSummaryDto(
                x.Id,
                x.Title,
                x.Slug,
                x.IsPublished,
                x.DurationMinutes,
                x.Questions.Count,
                x.Attempts.Count,
                x.UpdatedAtUtc))
            .ToListAsync(cancellationToken);
    }

    public async Task<AssessmentDetailDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var assessment = await _db.Assessments
            .AsNoTracking()
            .Include(x => x.Questions.OrderBy(q => q.OrderIndex))
            .ThenInclude(q => q.TestCases.OrderBy(t => t.OrderIndex))
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        return assessment is null ? null : MapDetail(assessment);
    }

    public async Task<AssessmentDetailDto> CreateAsync(CreateAssessmentRequest request, CancellationToken cancellationToken = default)
    {
        var baseSlug = string.IsNullOrWhiteSpace(request.Slug)
            ? SlugHelper.FromTitle(request.Title)
            : SlugHelper.FromTitle(request.Slug);

        var slug = SlugHelper.EnsureUnique(baseSlug, s => _db.Assessments.Any(x => x.Slug == s));
        var now = DateTime.UtcNow;
        var entity = new Assessment
        {
            Id = Guid.NewGuid(),
            Title = request.Title.Trim(),
            Description = request.Description.Trim(),
            Slug = slug,
            DurationMinutes = Math.Clamp(request.DurationMinutes, 5, 480),
            OpensAtUtc = request.OpensAtUtc,
            ClosesAtUtc = request.ClosesAtUtc,
            IsPublished = false,
            CandidateInstructions = request.CandidateInstructions?.Trim() ?? string.Empty,
            ShuffleQuestions = false,
            AllowCodeRun = true,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
        };

        _db.Assessments.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return MapDetail(entity);
    }

    public async Task<AssessmentDetailDto?> UpdateAsync(Guid id, UpdateAssessmentRequest request, CancellationToken cancellationToken = default)
    {
        var entity = await _db.Assessments
            .Include(x => x.Questions)
            .ThenInclude(q => q.TestCases)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (entity is null)
        {
            return null;
        }

        var slug = SlugHelper.FromTitle(request.Slug);
        if (!string.Equals(slug, entity.Slug, StringComparison.Ordinal))
        {
            slug = SlugHelper.EnsureUnique(slug, s => _db.Assessments.Any(x => x.Slug == s && x.Id != id));
        }

        entity.Title = request.Title.Trim();
        entity.Description = request.Description.Trim();
        entity.Slug = slug;
        entity.DurationMinutes = Math.Clamp(request.DurationMinutes, 5, 480);
        entity.OpensAtUtc = request.OpensAtUtc;
        entity.ClosesAtUtc = request.ClosesAtUtc;
        entity.IsPublished = request.IsPublished;
        entity.CandidateInstructions = request.CandidateInstructions.Trim();
        entity.ShuffleQuestions = request.ShuffleQuestions;
        entity.AllowCodeRun = request.AllowCodeRun;
        entity.UpdatedAtUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
        return MapDetail(entity);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _db.Assessments.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null)
        {
            return false;
        }

        await _db.CandidateAttempts
            .Where(x => x.AssessmentId == id)
            .ExecuteDeleteAsync(cancellationToken);

        _db.Assessments.Remove(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<QuestionAdminDto?> UpsertQuestionAsync(
        Guid assessmentId,
        Guid? questionId,
        UpsertQuestionRequest request,
        CancellationToken cancellationToken = default)
    {
        var assessment = await _db.Assessments
            .Include(x => x.Questions)
            .ThenInclude(q => q.TestCases)
            .FirstOrDefaultAsync(x => x.Id == assessmentId, cancellationToken);

        if (assessment is null)
        {
            return null;
        }

        Question question;
        if (questionId is null)
        {
            question = new Question
            {
                Id = Guid.NewGuid(),
                AssessmentId = assessmentId,
            };
            _db.Questions.Add(question);
        }
        else
        {
            question = assessment.Questions.FirstOrDefault(x => x.Id == questionId.Value)
                ?? throw new InvalidOperationException("Question not found.");
        }

        question.OrderIndex = request.OrderIndex;
        question.Title = request.Title.Trim();
        question.ProblemStatement = request.ProblemStatement.Trim();
        question.DefaultLanguage = request.DefaultLanguage;
        question.StarterCode = request.StarterCode;
        question.Points = Math.Max(1, request.Points);
        question.TimeLimitSeconds = Math.Clamp(request.TimeLimitSeconds, 1, 30);

        var incomingIds = request.TestCases
            .Select(x => x.Id)
            .Where(id => id is not null && id.Value != Guid.Empty)
            .Select(id => id!.Value)
            .ToHashSet();

        var toRemove = question.TestCases.Where(x => !incomingIds.Contains(x.Id)).ToList();
        _db.TestCases.RemoveRange(toRemove);

        foreach (var tc in request.TestCases.OrderBy(x => x.OrderIndex))
        {
            var hasExistingId = tc.Id is not null && tc.Id.Value != Guid.Empty;
            TestCase entity;
            if (hasExistingId)
            {
                entity = question.TestCases.FirstOrDefault(x => x.Id == tc.Id!.Value)
                    ?? throw new InvalidOperationException("Test case not found.");
            }
            else
            {
                entity = new TestCase
                {
                    Id = Guid.NewGuid(),
                    QuestionId = question.Id,
                };
                question.TestCases.Add(entity);
            }

            entity.Input = tc.Input;
            entity.ExpectedOutput = tc.ExpectedOutput;
            entity.IsSample = tc.IsSample;
            entity.OrderIndex = tc.OrderIndex;
        }

        assessment.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        return MapQuestion(question);
    }

    public async Task<bool> DeleteQuestionAsync(Guid assessmentId, Guid questionId, CancellationToken cancellationToken = default)
    {
        var question = await _db.Questions
            .FirstOrDefaultAsync(x => x.Id == questionId && x.AssessmentId == assessmentId, cancellationToken);
        if (question is null)
        {
            return false;
        }

        _db.Questions.Remove(question);
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<IReadOnlyList<AttemptResultDto>> GetResultsAsync(Guid assessmentId, CancellationToken cancellationToken = default)
    {
        var assessment = await _db.Assessments
            .AsNoTracking()
            .Include(x => x.Questions)
            .Include(x => x.Attempts)
            .ThenInclude(a => a.Submissions)
            .FirstOrDefaultAsync(x => x.Id == assessmentId, cancellationToken);

        if (assessment is null)
        {
            return Array.Empty<AttemptResultDto>();
        }

        var maxByQuestion = assessment.Questions.ToDictionary(x => x.Id, x => x.Points);

        return assessment.Attempts
            .OrderByDescending(x => x.TotalScore)
            .ThenBy(x => x.SubmittedAtUtc)
            .Select(attempt =>
            {
                var questionResults = assessment.Questions
                    .OrderBy(q => q.OrderIndex)
                    .Select(q =>
                    {
                        var final = attempt.Submissions
                            .Where(s => s.QuestionId == q.Id && s.IsFinal)
                            .OrderByDescending(s => s.SubmittedAtUtc)
                            .FirstOrDefault();

                        return new QuestionResultDto(
                            q.Id,
                            q.Title,
                            final?.Score ?? 0,
                            q.Points,
                            final?.PassedTests ?? 0,
                            final?.TotalTests ?? q.TestCases.Count,
                            final?.Language,
                            final is not null);
                    })
                    .ToList();

                return new AttemptResultDto(
                    attempt.Id,
                    attempt.FullName,
                    attempt.Email,
                    attempt.Status,
                    attempt.TotalScore,
                    attempt.MaxScore,
                    attempt.StartedAtUtc,
                    attempt.SubmittedAtUtc,
                    questionResults);
            })
            .ToList();
    }

    public async Task<string> ExportResultsCsvAsync(Guid assessmentId, CancellationToken cancellationToken = default)
    {
        var results = await GetResultsAsync(assessmentId, cancellationToken);
        var lines = new List<string> { "FullName,Email,Status,TotalScore,MaxScore,StartedAtUtc,SubmittedAtUtc" };
        foreach (var r in results)
        {
            lines.Add(string.Join(',',
                Csv(r.FullName),
                Csv(r.Email),
                r.Status,
                r.TotalScore,
                r.MaxScore,
                r.StartedAtUtc?.ToString("O") ?? "",
                r.SubmittedAtUtc?.ToString("O") ?? ""));
        }

        return string.Join(Environment.NewLine, lines);
    }

    private static string Csv(string value) =>
        $"\"{value.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";

    private static AssessmentDetailDto MapDetail(Assessment assessment)
    {
        var questions = assessment.Questions
            .OrderBy(x => x.OrderIndex)
            .Select(MapQuestion)
            .ToList();

        return new AssessmentDetailDto(
            assessment.Id,
            assessment.Title,
            assessment.Description,
            assessment.Slug,
            assessment.DurationMinutes,
            assessment.OpensAtUtc,
            assessment.ClosesAtUtc,
            assessment.IsPublished,
            assessment.CandidateInstructions,
            assessment.ShuffleQuestions,
            assessment.AllowCodeRun,
            $"/test/{assessment.Slug}",
            questions);
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
