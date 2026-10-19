using System.Net.Http.Json;
using System.Text.Json.Serialization;
using CodeIQ.Application.Abstractions;
using CodeIQ.Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using CodeIQ.Application.Options;

namespace CodeIQ.Infrastructure.Runner;

public sealed class CodeRunnerClient : ICodeRunnerClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<CodeRunnerClient> _logger;

    public CodeRunnerClient(HttpClient httpClient, IOptions<RunnerOptions> options, ILogger<CodeRunnerClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _httpClient.BaseAddress = new Uri(options.Value.BaseUrl.TrimEnd('/') + "/");
    }

    public async Task<RunnerExecuteResult> ExecuteAsync(
        SupportedLanguage language,
        string source,
        string stdin,
        int timeoutSeconds,
        CancellationToken cancellationToken = default)
    {
        var payload = new ExecuteRequestDto
        {
            Language = language.ToRunnerToken(),
            Source = source,
            Stdin = stdin,
            TimeoutSeconds = timeoutSeconds,
        };

        try
        {
            using var response = await _httpClient.PostAsJsonAsync("execute", payload, cancellationToken);
            response.EnsureSuccessStatusCode();
            var body = await response.Content.ReadFromJsonAsync<ExecuteResponseDto>(cancellationToken: cancellationToken);
            if (body is null)
            {
                throw new InvalidOperationException("Runner returned empty body.");
            }

            return new RunnerExecuteResult(
                body.Stdout,
                body.Stderr,
                body.ExitCode,
                body.TimeMs,
                body.CompileError);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Code runner request failed.");
            var message = IsRunnerUnreachable(ex)
                ? "Code execution service is not running. Start it with: cd codeiq-runner && docker compose up --build"
                : ex.Message;
            return new RunnerExecuteResult("", message, -1, 0, message);
        }
    }

    private static bool IsRunnerUnreachable(Exception ex)
    {
        for (var current = ex; current is not null; current = current.InnerException)
        {
            if (current is HttpRequestException or System.Net.Sockets.SocketException)
            {
                return true;
            }
        }

        return ex.Message.Contains("actively refused", StringComparison.OrdinalIgnoreCase)
            || ex.Message.Contains("connection refused", StringComparison.OrdinalIgnoreCase);
    }

    private sealed class ExecuteRequestDto
    {
        [JsonPropertyName("language")]
        public string Language { get; set; } = string.Empty;

        [JsonPropertyName("source")]
        public string Source { get; set; } = string.Empty;

        [JsonPropertyName("stdin")]
        public string Stdin { get; set; } = string.Empty;

        [JsonPropertyName("timeoutSeconds")]
        public int TimeoutSeconds { get; set; }
    }

    private sealed class ExecuteResponseDto
    {
        [JsonPropertyName("stdout")]
        public string Stdout { get; set; } = string.Empty;

        [JsonPropertyName("stderr")]
        public string Stderr { get; set; } = string.Empty;

        [JsonPropertyName("exitCode")]
        public int ExitCode { get; set; }

        [JsonPropertyName("timeMs")]
        public int TimeMs { get; set; }

        [JsonPropertyName("compileError")]
        public string? CompileError { get; set; }
    }
}
