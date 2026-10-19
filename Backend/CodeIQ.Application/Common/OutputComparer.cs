namespace CodeIQ.Application.Common;

public static class OutputComparer
{
    public static bool Matches(string expected, string actual)
    {
        var normalizedExpected = Normalize(expected);
        var normalizedActual = Normalize(actual);
        return string.Equals(normalizedExpected, normalizedActual, StringComparison.Ordinal);
    }

    private static string Normalize(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        var lines = value.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
        while (lines.Length > 0 && string.IsNullOrWhiteSpace(lines[^1]))
        {
            Array.Resize(ref lines, lines.Length - 1);
        }

        for (var i = 0; i < lines.Length; i++)
        {
            lines[i] = lines[i].TrimEnd();
        }

        return string.Join('\n', lines);
    }
}
