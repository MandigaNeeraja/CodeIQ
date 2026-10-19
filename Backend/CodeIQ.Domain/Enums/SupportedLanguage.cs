namespace CodeIQ.Domain.Enums;

public enum SupportedLanguage
{
    Python3 = 0,
    Cpp17 = 1,
    C = 2,
    JavaScript = 3,
}

public static class SupportedLanguageExtensions
{
    public static string ToRunnerToken(this SupportedLanguage language) => language switch
    {
        SupportedLanguage.Python3 => "python3",
        SupportedLanguage.Cpp17 => "cpp17",
        SupportedLanguage.C => "c",
        SupportedLanguage.JavaScript => "javascript",
        _ => throw new ArgumentOutOfRangeException(nameof(language)),
    };
}
