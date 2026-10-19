using System.Text.RegularExpressions;

namespace CodeIQ.Application.Common;

public static partial class SlugHelper
{
    public static string FromTitle(string title)
    {
        var slug = title.Trim().ToLowerInvariant();
        slug = NonAlphanumeric().Replace(slug, "-");
        slug = MultipleHyphens().Replace(slug, "-").Trim('-');
        return string.IsNullOrWhiteSpace(slug) ? Guid.NewGuid().ToString("N")[..8] : slug;
    }

    public static string EnsureUnique(string baseSlug, Func<string, bool> exists)
    {
        if (!exists(baseSlug))
        {
            return baseSlug;
        }

        for (var i = 2; i < 1000; i++)
        {
            var candidate = $"{baseSlug}-{i}";
            if (!exists(candidate))
            {
                return candidate;
            }
        }

        return $"{baseSlug}-{Guid.NewGuid():N}"[..Math.Min(baseSlug.Length + 9, 64)];
    }

    [GeneratedRegex("[^a-z0-9]+", RegexOptions.Compiled)]
    private static partial Regex NonAlphanumeric();

    [GeneratedRegex("-{2,}", RegexOptions.Compiled)]
    private static partial Regex MultipleHyphens();
}
