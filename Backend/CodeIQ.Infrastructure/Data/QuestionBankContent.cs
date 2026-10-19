namespace CodeIQ.Infrastructure.Data;

internal static class QuestionBankContent
{
    internal static string Format(
        string problem,
        string inputFormat,
        string outputFormat,
        string constraints,
        params (string title, string sampleInput, string sampleOutput, string explanation)[] samples)
    {
        var lines = new List<string>
        {
            "Problem",
            "-------",
            problem.Trim(),
            "",
            "Input Format",
            "------------",
            inputFormat.Trim(),
            "",
            "Output Format",
            "-------------",
            outputFormat.Trim(),
            "",
            "Constraints",
            "-----------",
            constraints.Trim(),
        };

        for (var i = 0; i < samples.Length; i++)
        {
            var (title, sampleInput, sampleOutput, explanation) = samples[i];
            var label = string.IsNullOrWhiteSpace(title) ? $"Sample Case {i}" : title;
            lines.Add("");
            lines.Add($"{label} — Input");
            lines.Add("----------------");
            lines.Add(string.IsNullOrEmpty(sampleInput) ? "(none)" : sampleInput.TrimEnd());
            lines.Add("");
            lines.Add($"{label} — Output");
            lines.Add("-----------------");
            lines.Add(sampleOutput.TrimEnd());
            if (!string.IsNullOrWhiteSpace(explanation))
            {
                lines.Add("");
                lines.Add("Explanation");
                lines.Add("-----------");
                lines.Add(explanation.Trim());
            }
        }

        lines.Add("");
        lines.Add("Note: Additional hidden test cases are evaluated on submit. Score = points × (passed tests ÷ total tests).");
        return string.Join('\n', lines);
    }
}
