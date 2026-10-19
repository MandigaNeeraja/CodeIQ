using CodeIQ.Domain.Entities;
using CodeIQ.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeIQ.Infrastructure.Data;

public static class QuestionBankSeeder
{
    public static async Task SeedAsync(AppDbContext db, CancellationToken cancellationToken = default)
    {
        var definitions = BuildSeedItems();
        var existing = await db.QuestionBankItems
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        if (existing.Count == 0)
        {
            db.QuestionBankItems.AddRange(definitions);
            await db.SaveChangesAsync(cancellationToken);
            return;
        }

        foreach (var definition in definitions)
        {
            var item = existing.FirstOrDefault(x => x.Slug == definition.Slug);
            if (item is null)
            {
                db.QuestionBankItems.Add(definition);
                continue;
            }

            var tracked = await db.QuestionBankItems.FirstAsync(x => x.Id == item.Id, cancellationToken);
            tracked.Title = definition.Title;
            tracked.Category = definition.Category;
            tracked.Difficulty = definition.Difficulty;
            tracked.Tags = definition.Tags;
            tracked.ProblemStatement = definition.ProblemStatement;
            tracked.DefaultLanguage = definition.DefaultLanguage;
            tracked.StarterCode = definition.StarterCode;
            tracked.DefaultPoints = definition.DefaultPoints;
            tracked.TimeLimitSeconds = definition.TimeLimitSeconds;

            await db.QuestionBankTestCases
                .Where(tc => tc.QuestionBankItemId == item.Id)
                .ExecuteDeleteAsync(cancellationToken);

            foreach (var testCase in definition.TestCases)
            {
                db.QuestionBankTestCases.Add(new QuestionBankTestCase
                {
                    Id = Guid.NewGuid(),
                    QuestionBankItemId = item.Id,
                    Input = testCase.Input,
                    ExpectedOutput = testCase.ExpectedOutput,
                    IsSample = testCase.IsSample,
                    OrderIndex = testCase.OrderIndex,
                });
            }

            await db.SaveChangesAsync(cancellationToken);
        }

        await db.SaveChangesAsync(cancellationToken);
        await SyncAssessmentQuestionsFromBankAsync(db, cancellationToken);
    }

    private static async Task SyncAssessmentQuestionsFromBankAsync(AppDbContext db, CancellationToken cancellationToken)
    {
        var bankById = await db.QuestionBankItems
            .AsNoTracking()
            .ToDictionaryAsync(x => x.Id, cancellationToken);

        var linkedQuestions = await db.Questions
            .Where(x => x.SourceQuestionBankItemId != null)
            .ToListAsync(cancellationToken);

        foreach (var question in linkedQuestions)
        {
            if (!bankById.TryGetValue(question.SourceQuestionBankItemId!.Value, out var bank))
            {
                continue;
            }

            question.Title = bank.Title;
            question.ProblemStatement = bank.ProblemStatement;
            question.DefaultLanguage = bank.DefaultLanguage;
            question.StarterCode = bank.StarterCode;
            question.Points = bank.DefaultPoints;
            question.TimeLimitSeconds = bank.TimeLimitSeconds;

            await db.TestCases
                .Where(t => t.QuestionId == question.Id)
                .ExecuteDeleteAsync(cancellationToken);

            var bankTests = await db.QuestionBankTestCases
                .AsNoTracking()
                .Where(t => t.QuestionBankItemId == bank.Id)
                .OrderBy(t => t.OrderIndex)
                .ToListAsync(cancellationToken);

            foreach (var tc in bankTests)
            {
                db.TestCases.Add(new TestCase
                {
                    Id = Guid.NewGuid(),
                    QuestionId = question.Id,
                    Input = tc.Input,
                    ExpectedOutput = tc.ExpectedOutput,
                    IsSample = tc.IsSample,
                    OrderIndex = tc.OrderIndex,
                });
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private static List<QuestionBankItem> BuildSeedItems()
    {
        var now = DateTime.UtcNow;
        var items = new List<QuestionBankItem>
        {
            Build(
                "hello-world",
                "Hello World",
                "Fundamentals",
                QuestionDifficulty.Easy,
                "implementation,warmup",
                QuestionBankContent.Format(
                    "Print the exact string Hello on standard output.",
                    "There is no input for this challenge.",
                    "Print Hello followed by a newline.",
                    "Output must match exactly (case-sensitive).",
                    ("Sample Case 0", "", "Hello", "Your program should print one line.")),
                SupportedLanguage.Python3,
                """
# Print exactly: Hello
print('Hello')
""",
                [("", "Hello\n", true), ("", "Hello\n", false), ("", "Hello\n", false)]),

            Build(
                "sum-two-numbers",
                "Sum Two Numbers",
                "Math",
                QuestionDifficulty.Easy,
                "math,basic-io",
                QuestionBankContent.Format(
                    "Given two integers, compute and print their sum.",
                    "The first line contains integer a.\nThe second line contains integer b.",
                    "Print a single integer — the value of a + b — followed by a newline.",
                    "-10^9 ≤ a, b ≤ 10^9",
                    ("Sample Case 0", "2\n3", "5", "2 + 3 = 5."),
                    ("Sample Case 1", "10\n-4", "6", "Handles negative values.")),
                SupportedLanguage.Python3,
                """
a = int(input())
b = int(input())
# Print a + b
print(a + b)
""",
                [
                    ("2\n3\n", "5\n", true),
                    ("10\n-4\n", "6\n", true),
                    ("0\n0\n", "0\n", false),
                    ("1000000000\n1000000000\n", "2000000000\n", false),
                ]),

            Build(
                "reverse-string",
                "Reverse a String",
                "Strings",
                QuestionDifficulty.Easy,
                "strings",
                QuestionBankContent.Format(
                    "Given a string s, print its reverse.",
                    "A single line containing string s (may include letters and digits).",
                    "Print the reversed string on one line.",
                    "1 ≤ |s| ≤ 10^5",
                    ("Sample Case 0", "hello", "olleh", ""),
                    ("Sample Case 1", "CodeIQ", "QIEDoC", "")),
                SupportedLanguage.Python3,
                """
s = input()
print(s[::-1])
""",
                [
                    ("hello\n", "olleh\n", true),
                    ("CodeIQ\n", "QIEDoC\n", true),
                    ("a\n", "a\n", false),
                    ("ab\n", "ba\n", false),
                ]),

            Build(
                "fizzbuzz-single",
                "FizzBuzz (Single N)",
                "Math",
                QuestionDifficulty.Medium,
                "math,logic",
                QuestionBankContent.Format(
                    "Given an integer N, apply FizzBuzz rules to N only (not a range).",
                    "One line with integer N.",
                    "Print exactly one of: FizzBuzz, Fizz, Buzz, or N (as decimal), followed by a newline.",
                    "1 ≤ N ≤ 10^5",
                    ("Sample Case 0", "15", "FizzBuzz", "Divisible by 3 and 5."),
                    ("Sample Case 1", "9", "Fizz", "Divisible by 3 only.")),
                SupportedLanguage.Python3,
                """
n = int(input())
if n % 15 == 0:
    print("FizzBuzz")
elif n % 3 == 0:
    print("Fizz")
elif n % 5 == 0:
    print("Buzz")
else:
    print(n)
""",
                [
                    ("15\n", "FizzBuzz\n", true),
                    ("9\n", "Fizz\n", true),
                    ("10\n", "Buzz\n", true),
                    ("7\n", "7\n", false),
                    ("30\n", "FizzBuzz\n", false),
                ]),

            Build(
                "palindrome-check",
                "Palindrome Check",
                "Strings",
                QuestionDifficulty.Medium,
                "strings,two-pointers",
                QuestionBankContent.Format(
                    "Determine whether a lowercase word reads the same forward and backward.",
                    "One line containing a lowercase alphabetic word w.",
                    "Print YES if w is a palindrome, otherwise print NO.",
                    "1 ≤ |w| ≤ 10^5",
                    ("Sample Case 0", "level", "YES", ""),
                    ("Sample Case 1", "hello", "NO", "")),
                SupportedLanguage.Python3,
                """
s = input().strip()
print("YES" if s == s[::-1] else "NO")
""",
                [
                    ("level\n", "YES\n", true),
                    ("hello\n", "NO\n", true),
                    ("racecar\n", "YES\n", false),
                    ("a\n", "YES\n", false),
                ]),

            Build(
                "array-max",
                "Maximum in Array",
                "Arrays",
                QuestionDifficulty.Easy,
                "arrays",
                QuestionBankContent.Format(
                    "Find the maximum element in an array of integers.",
                    "Line 1: integer N\nLine 2: N space-separated integers.",
                    "Print the maximum value.",
                    "1 ≤ N ≤ 10^5; values fit in 32-bit signed integer.",
                    ("Sample Case 0", "3\n1 5 2", "5", ""),
                    ("Sample Case 1", "1\n42", "42", "Single element.")),
                SupportedLanguage.Python3,
                """
n = int(input())
arr = list(map(int, input().split()))
print(max(arr))
""",
                [
                    ("3\n1 5 2\n", "5\n", true),
                    ("1\n42\n", "42\n", true),
                    ("4\n-1 -5 -2 -9\n", "-1\n", false),
                    ("5\n10 10 10 10 10\n", "10\n", false),
                ]),

            Build(
                "count-vowels",
                "Count Vowels",
                "Strings",
                QuestionDifficulty.Easy,
                "strings,counting",
                QuestionBankContent.Format(
                    "Count vowels (a, e, i, o, u) in a line of text, ignoring case.",
                    "One line of text (letters and spaces).",
                    "Print the vowel count as a decimal integer.",
                    "0 ≤ length ≤ 10^5",
                    ("Sample Case 0", "hello", "2", "e and o."),
                    ("Sample Case 1", "AEIOU", "5", "Case-insensitive.")),
                SupportedLanguage.Python3,
                """
s = input().lower()
print(sum(1 for c in s if c in "aeiou"))
""",
                [
                    ("hello\n", "2\n", true),
                    ("AEIOU\n", "5\n", true),
                    ("xyz\n", "0\n", false),
                    ("Programming\n", "3\n", false),
                ]),

            Build(
                "two-sum-count",
                "Two Sum (Count Pairs)",
                "Arrays",
                QuestionDifficulty.Hard,
                "arrays,hash-map",
                QuestionBankContent.Format(
                    "Count unordered index pairs (i, j) with i < j such that nums[i] + nums[j] equals target T.",
                    "Line 1: integer N\nLine 2: N integers\nLine 3: integer T",
                    "Print the number of valid pairs.",
                    "1 ≤ N ≤ 2·10^5; values and T fit in 32-bit signed integer.",
                    ("Sample Case 0", "4\n1 2 3 2\n4", "2", "Pairs (0,2) and (1,3)."),
                    ("Sample Case 1", "3\n1 1 1\n2", "3", "Three pairs with duplicate values.")),
                SupportedLanguage.Python3,
                """
n = int(input())
nums = list(map(int, input().split()))
t = int(input())
seen = {}
count = 0
for x in nums:
    need = t - x
    count += seen.get(need, 0)
    seen[x] = seen.get(x, 0) + 1
print(count)
""",
                [
                    ("4\n1 2 3 2\n4\n", "2\n", true),
                    ("3\n1 1 1\n2\n", "3\n", true),
                    ("2\n1 2\n100\n", "0\n", false),
                    ("5\n1 1 1 1 1\n2\n", "10\n", false),
                    ("6\n0 0 0 0 0 0\n0\n", "15\n", false),
                ]),
        };

        foreach (var item in items)
        {
            item.CreatedAtUtc = now;
        }

        return items;
    }

    private static QuestionBankItem Build(
        string slug,
        string title,
        string category,
        QuestionDifficulty difficulty,
        string tags,
        string statement,
        SupportedLanguage language,
        string starter,
        (string input, string output, bool sample)[] tests)
    {
        var id = Guid.NewGuid();
        var item = new QuestionBankItem
        {
            Id = id,
            Slug = slug,
            Title = title,
            Category = category,
            Difficulty = difficulty,
            Tags = tags,
            ProblemStatement = statement.Trim(),
            DefaultLanguage = language,
            StarterCode = starter.Trim(),
            DefaultPoints = difficulty switch
            {
                QuestionDifficulty.Easy => 50,
                QuestionDifficulty.Medium => 100,
                QuestionDifficulty.Hard => 150,
                _ => 100,
            },
            TimeLimitSeconds = 5,
        };

        for (var i = 0; i < tests.Length; i++)
        {
            var t = tests[i];
            item.TestCases.Add(new QuestionBankTestCase
            {
                Id = Guid.NewGuid(),
                QuestionBankItemId = id,
                Input = t.input,
                ExpectedOutput = t.output,
                IsSample = t.sample,
                OrderIndex = i,
            });
        }

        return item;
    }
}
