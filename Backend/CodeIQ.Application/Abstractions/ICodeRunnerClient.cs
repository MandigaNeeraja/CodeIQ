using CodeIQ.Domain.Enums;

namespace CodeIQ.Application.Abstractions;

public sealed record RunnerExecuteResult(
    string Stdout,
    string Stderr,
    int ExitCode,
    int TimeMs,
    string? CompileError);

public interface ICodeRunnerClient
{
    Task<RunnerExecuteResult> ExecuteAsync(
        SupportedLanguage language,
        string source,
        string stdin,
        int timeoutSeconds,
        CancellationToken cancellationToken = default);
}
