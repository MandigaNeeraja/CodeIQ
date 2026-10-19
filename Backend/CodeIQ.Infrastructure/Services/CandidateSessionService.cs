using System.Security.Cryptography;
using System.Text.Json;
using CodeIQ.Application.Abstractions;
using CodeIQ.Application.Common;
using CodeIQ.Application.Dtos;
using CodeIQ.Domain.Entities;
using CodeIQ.Domain.Enums;
using CodeIQ.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CodeIQ.Infrastructure.Services;

public sealed class CandidateSessionService : ICandidateSessionService
{
    private readonly AppDbContext _db;
    private readonly ICodeRunnerClient _runner;

    public CandidateSessionService(AppDbContext db, ICodeRunnerClient runner)
    {
        _db = db;
        _runner = runner;
    }

    public async Task<PublicAssessmentDto?> GetPublicAssessmentAsync(string slug, CancellationToken cancellationToken = default)
    {
        var normalized = slug.Trim().ToLowerInvariant();
        var assessment = await _db.Assessments
            .AsNoTracking()
            .Include(x => x.Questions)
            .FirstOrDefaultAsync(x => x.Slug == normalized && x.IsPublished, cancellationToken);

        if (assessment is null)
        {
            return null;
        }

        return new PublicAssessmentDto(
            assessment.Id,
            assessment.Title,
            assessment.Description,
            assessment.DurationMinutes,
            assessment.Questions.Count,
            IsRegistrationOpen(assessment, DateTime.UtcNow));
    }

    public async Task<RegisterCandidateResponse> RegisterAsync(
        string slug,
        RegisterCandidateRequest request,
        CancellationToken cancellationToken = default)
    {
        var normalized = slug.Trim().ToLowerInvariant();
        var assessment = await _db.Assessments
            .FirstOrDefaultAsync(x => x.Slug == normalized && x.IsPublished, cancellationToken)
            ?? throw new InvalidOperationException("Assessment is not available.");

        var now = DateTime.UtcNow;
        if (!IsRegistrationOpen(assessment, now))
        {
            throw new InvalidOperationException("Registration is closed for this assessment.");
        }

        var email = request.Email.Trim().ToLowerInvariant();
        var existing = await _db.CandidateAttempts
            .FirstOrDefaultAsync(x => x.AssessmentId == assessment.Id && x.Email == email, cancellationToken);

        if (existing is not null)
        {
            if (existing.Status == AttemptStatus.InProgress
                && existing.ExpiresAtUtc is not null
                && DateTime.UtcNow > AsUtc(existing.ExpiresAtUtc.Value))
            {
                existing.Status = AttemptStatus.Expired;
                existing.SubmittedAtUtc ??= now;
            }

            if (existing.Status is AttemptStatus.Submitted or AttemptStatus.Expired)
            {
                existing.Status = AttemptStatus.Registered;
                existing.StartedAtUtc = null;
                existing.ExpiresAtUtc = null;
                existing.SubmittedAtUtc = null;
                existing.TotalScore = 0;
                await _db.QuestionSubmissions
                    .Where(s => s.AttemptId == existing.Id)
                    .ExecuteDeleteAsync(cancellationToken);
                await _db.SaveChangesAsync(cancellationToken);
            }

            return new RegisterCandidateResponse(
                existing.Id,
                existing.AccessToken,
                $"/session/{existing.AccessToken}");
        }

        var attempt = new CandidateAttempt
        {
            Id = Guid.NewGuid(),
            AssessmentId = assessment.Id,
            AccessToken = GenerateAccessToken(),
            FullName = request.FullName.Trim(),
            Email = email,
            Status = AttemptStatus.Registered,
            RegisteredAtUtc = now,
            MaxScore = await _db.Questions.Where(q => q.AssessmentId == assessment.Id).SumAsync(q => q.Points, cancellationToken),
        };

        _db.CandidateAttempts.Add(attempt);
        await _db.SaveChangesAsync(cancellationToken);

        return new RegisterCandidateResponse(
            attempt.Id,
            attempt.AccessToken,
            $"/session/{attempt.AccessToken}");
    }

    public async Task<CandidateSessionDto?> GetSessionAsync(string accessToken, CancellationToken cancellationToken = default)
    {
        var attempt = await LoadAttempt(accessToken, cancellationToken: cancellationToken);
        if (attempt is null)
        {
            return null;
        }

        await ExpireIfNeeded(attempt, cancellationToken);
        return MapSession(attempt);
    }

    public async Task<CandidateSessionDto?> StartAttemptAsync(string accessToken, CancellationToken cancellationToken = default)
    {
        var attempt = await LoadAttempt(accessToken, tracking: true, cancellationToken);
        if (attempt is null)
        {
            return null;
        }

        var now = DateTime.UtcNow;
        if (!IsRegistrationOpen(attempt.Assessment, now))
        {
            throw new InvalidOperationException("This assessment is not open.");
        }

        if (attempt.Status == AttemptStatus.Submitted || attempt.Status == AttemptStatus.Expired)
        {
            throw new InvalidOperationException("This attempt is already closed.");
        }

        if (attempt.Status == AttemptStatus.Registered)
        {
            attempt.Status = AttemptStatus.InProgress;
            attempt.StartedAtUtc = now;
            attempt.ExpiresAtUtc = now.AddMinutes(attempt.Assessment.DurationMinutes);
            await _db.SaveChangesAsync(cancellationToken);
        }

        return MapSession(attempt);
    }

    public async Task<RunCodeResponse?> RunSamplesAsync(
        string accessToken,
        Guid questionId,
        RunCodeRequest request,
        CancellationToken cancellationToken = default)
    {
        var attempt = await LoadAttempt(accessToken, tracking: true, cancellationToken);
        if (attempt is null)
        {
            return null;
        }

        await EnsureAttemptActiveAsync(attempt, cancellationToken);
        if (!attempt.Assessment.AllowCodeRun)
        {
            throw new InvalidOperationException("Sample runs are disabled for this assessment.");
        }

        var question = attempt.Assessment.Questions.FirstOrDefault(x => x.Id == questionId)
            ?? throw new InvalidOperationException("Question not found.");

        var samples = question.TestCases.Where(x => x.IsSample).OrderBy(x => x.OrderIndex).ToList();
        var results = await ExecuteTestCases(request.Language, request.SourceCode, question.TimeLimitSeconds, samples, cancellationToken);
        return new RunCodeResponse(results, results.Count(x => x.Passed), results.Count);
    }

    public async Task<SubmitQuestionResponse?> SubmitQuestionAsync(
        string accessToken,
        Guid questionId,
        SubmitQuestionRequest request,
        CancellationToken cancellationToken = default)
    {
        var attempt = await LoadAttempt(accessToken, tracking: true, cancellationToken);
        if (attempt is null)
        {
            return null;
        }

        await EnsureAttemptActiveAsync(attempt, cancellationToken);
        var question = attempt.Assessment.Questions.FirstOrDefault(x => x.Id == questionId)
            ?? throw new InvalidOperationException("Question not found.");

        var testCases = question.TestCases.OrderBy(x => x.OrderIndex).ToList();
        var results = await ExecuteTestCases(request.Language, request.SourceCode, question.TimeLimitSeconds, testCases, cancellationToken);
        var passed = results.Count(x => x.Passed);
        var total = results.Count;
        var score = total == 0 ? 0 : (int)Math.Round(question.Points * (passed / (double)total));

        var submission = new QuestionSubmission
        {
            Id = Guid.NewGuid(),
            AttemptId = attempt.Id,
            QuestionId = question.Id,
            Language = request.Language,
            SourceCode = request.SourceCode,
            IsFinal = request.Finalize,
            Score = score,
            PassedTests = passed,
            TotalTests = total,
            SubmittedAtUtc = DateTime.UtcNow,
            ResultJson = JsonSerializer.Serialize(results),
        };

        if (request.Finalize)
        {
            var previousFinal = attempt.Submissions.Where(x => x.QuestionId == question.Id && x.IsFinal).ToList();
            foreach (var prev in previousFinal)
            {
                prev.IsFinal = false;
            }
        }

        _db.QuestionSubmissions.Add(submission);
        RecalculateAttemptScore(attempt);
        await _db.SaveChangesAsync(cancellationToken);

        return new SubmitQuestionResponse(score, question.Points, passed, total, request.Finalize, results);
    }

    public async Task<CandidateSessionDto?> FinalizeAttemptAsync(string accessToken, CancellationToken cancellationToken = default)
    {
        var attempt = await LoadAttempt(accessToken, tracking: true, cancellationToken);
        if (attempt is null)
        {
            return null;
        }

        if (attempt.Status is AttemptStatus.Submitted or AttemptStatus.Expired)
        {
            return MapSession(attempt);
        }

        attempt.Status = AttemptStatus.Submitted;
        attempt.SubmittedAtUtc = DateTime.UtcNow;
        RecalculateAttemptScore(attempt);
        await _db.SaveChangesAsync(cancellationToken);
        return MapSession(attempt);
    }

    private async Task<CandidateAttempt?> LoadAttempt(
        string accessToken,
        bool tracking = false,
        CancellationToken cancellationToken = default)
    {
        var query = _db.CandidateAttempts
            .AsSplitQuery()
            .Include(x => x.Assessment)
            .ThenInclude(a => a.Questions.OrderBy(q => q.OrderIndex))
            .ThenInclude(q => q.TestCases.OrderBy(t => t.OrderIndex))
            .Include(x => x.Submissions)
            .AsQueryable();

        if (!tracking)
        {
            query = query.AsNoTracking();
        }

        return await query.FirstOrDefaultAsync(x => x.AccessToken == accessToken, cancellationToken);
    }

    private async Task ExpireIfNeeded(CandidateAttempt attempt, CancellationToken cancellationToken)
    {
        if (attempt.Status != AttemptStatus.InProgress || attempt.ExpiresAtUtc is null)
        {
            return;
        }

        if (DateTime.UtcNow <= AsUtc(attempt.ExpiresAtUtc.Value))
        {
            return;
        }

        var tracked = await _db.CandidateAttempts.FirstOrDefaultAsync(x => x.Id == attempt.Id, cancellationToken);
        if (tracked is null || tracked.Status != AttemptStatus.InProgress)
        {
            return;
        }

        tracked.Status = AttemptStatus.Expired;
        tracked.SubmittedAtUtc ??= DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        attempt.Status = tracked.Status;
    }

    private async Task EnsureAttemptActiveAsync(CandidateAttempt attempt, CancellationToken cancellationToken)
    {
        if (attempt.Status == AttemptStatus.Expired)
        {
            throw new InvalidOperationException("Time limit exceeded. This attempt is closed.");
        }

        if (attempt.Status != AttemptStatus.InProgress)
        {
            throw new InvalidOperationException("Start the test before submitting code.");
        }

        if (attempt.ExpiresAtUtc is not null && DateTime.UtcNow > AsUtc(attempt.ExpiresAtUtc.Value))
        {
            attempt.Status = AttemptStatus.Expired;
            attempt.SubmittedAtUtc ??= DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
            throw new InvalidOperationException("Time limit exceeded. This attempt is closed.");
        }
    }

    private static DateTime AsUtc(DateTime value) =>
        value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc),
        };

    private static bool IsRegistrationOpen(Assessment assessment, DateTime utcNow)
    {
        if (!assessment.IsPublished)
        {
            return false;
        }

        if (assessment.OpensAtUtc is not null && utcNow < assessment.OpensAtUtc)
        {
            return false;
        }

        if (assessment.ClosesAtUtc is not null && utcNow > assessment.ClosesAtUtc)
        {
            return false;
        }

        return true;
    }

    private async Task<IReadOnlyList<TestRunResultDto>> ExecuteTestCases(
        SupportedLanguage language,
        string source,
        int timeoutSeconds,
        IReadOnlyList<TestCase> testCases,
        CancellationToken cancellationToken)
    {
        var results = new List<TestRunResultDto>(testCases.Count);
        foreach (var testCase in testCases)
        {
            var run = await _runner.ExecuteAsync(language, source, testCase.Input, timeoutSeconds, cancellationToken);
            var actual = run.CompileError is not null ? string.Empty : FormatActualOutput(run);
            var passed = run.CompileError is null
                && run.ExitCode == 0
                && OutputComparer.Matches(testCase.ExpectedOutput, run.Stdout);

            results.Add(new TestRunResultDto(
                testCase.Id,
                testCase.IsSample,
                passed,
                testCase.IsSample ? testCase.Input : "—",
                testCase.IsSample ? testCase.ExpectedOutput : "—",
                testCase.IsSample ? actual : (passed ? "Accepted" : "Wrong Answer"),
                run.Stderr,
                run.TimeMs,
                run.CompileError));
        }

        return results;
    }

    private static string FormatActualOutput(RunnerExecuteResult run)
    {
        if (!string.IsNullOrWhiteSpace(run.Stdout))
        {
            return run.Stdout;
        }

        if (!string.IsNullOrWhiteSpace(run.Stderr))
        {
            return run.Stderr.TrimEnd();
        }

        return run.ExitCode != 0
            ? $"(no output, exit code {run.ExitCode})"
            : "(no output)";
    }

    private static void RecalculateAttemptScore(CandidateAttempt attempt)
    {
        var finalScores = attempt.Submissions
            .Where(x => x.IsFinal)
            .GroupBy(x => x.QuestionId)
            .Select(g => g.OrderByDescending(x => x.SubmittedAtUtc).First().Score);

        attempt.TotalScore = finalScores.Sum();
        attempt.MaxScore = attempt.Assessment.Questions.Sum(x => x.Points);
    }

    private static CandidateSessionDto MapSession(CandidateAttempt attempt)
    {
        var ordered = attempt.Assessment.Questions.OrderBy(x => x.OrderIndex).ToList();
        if (attempt.Assessment.ShuffleQuestions && attempt.Status != AttemptStatus.Registered)
        {
            ordered = ordered
                .OrderBy(q => HashCode.Combine(attempt.Id.GetHashCode(), q.Id.GetHashCode()))
                .ToList();
        }

        var questions = ordered
            .Select(q =>
            {
                var latest = attempt.Submissions
                    .Where(s => s.QuestionId == q.Id)
                    .OrderByDescending(s => s.SubmittedAtUtc)
                    .FirstOrDefault();

                var final = attempt.Submissions
                    .Where(s => s.QuestionId == q.Id && s.IsFinal)
                    .OrderByDescending(s => s.SubmittedAtUtc)
                    .FirstOrDefault();

                CandidateQuestionProgressDto? progress = latest is null
                    ? null
                    : new CandidateQuestionProgressDto(
                        latest.Language,
                        latest.SourceCode,
                        final is not null,
                        null);

                return new CandidateQuestionDto(
                    q.Id,
                    q.OrderIndex,
                    q.Title,
                    q.ProblemStatement,
                    q.DefaultLanguage,
                    q.StarterCode,
                    q.Points,
                    q.TimeLimitSeconds,
                    q.TestCases
                        .Where(t => t.IsSample)
                        .OrderBy(t => t.OrderIndex)
                        .Select(t => new SampleTestCaseDto(t.Input, t.ExpectedOutput))
                        .ToList(),
                    progress);
            })
            .ToList();

        return new CandidateSessionDto(
            attempt.Id,
            attempt.AssessmentId,
            attempt.Assessment.Title,
            attempt.Status,
            attempt.StartedAtUtc,
            attempt.ExpiresAtUtc,
            attempt.Assessment.DurationMinutes,
            attempt.Assessment.CandidateInstructions,
            attempt.Assessment.AllowCodeRun,
            questions);
    }

    private static string GenerateAccessToken()
    {
        Span<byte> bytes = stackalloc byte[32];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
