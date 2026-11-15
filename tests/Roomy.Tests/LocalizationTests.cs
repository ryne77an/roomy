using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Roomy.Core;

namespace Roomy.Tests;

public sealed class LocalizationTests
{
    /// <summary>Repo root, derived from this file's compile-time path (works locally and in CI).</summary>
    static string RepoRoot([CallerFilePath] string thisFile = "") =>
        Path.GetFullPath(Path.Combine(Path.GetDirectoryName(thisFile)!, "..", ".."));

    [Fact]
    public void BothLanguagesDefineTheSameKeys()
    {
        var en = Loc.Table("en").Keys.ToHashSet(StringComparer.Ordinal);
        var ru = Loc.Table("ru").Keys.ToHashSet(StringComparer.Ordinal);
        Assert.Empty(en.Except(ru).Order());
        Assert.Empty(ru.Except(en).Order());
    }

    [Fact]
    public void NoStringIsEmptyAndPlaceholdersMatch()
    {
        var en = Loc.Table("en");
        var ru = Loc.Table("ru");
        var problems = new List<string>();
        foreach (var (key, value) in en)
        {
            if (string.IsNullOrWhiteSpace(value)) problems.Add($"en:{key} is empty");
            if (string.IsNullOrWhiteSpace(ru[key])) problems.Add($"ru:{key} is empty");
            if (!Placeholders(value).SetEquals(Placeholders(ru[key])))
                problems.Add($"{key}: en uses {{{string.Join("},{", Placeholders(value))}}} but ru uses {{{string.Join("},{", Placeholders(ru[key]))}}}");
        }
        Assert.Empty(problems);
    }

    static HashSet<string> Placeholders(string text) =>
        Regex.Matches(text, @"\{(\d+)(?:[,:][^}]*)?\}").Select(m => m.Groups[1].Value).ToHashSet(StringComparer.Ordinal);

    [Theory]
    [InlineData(1, 0)] [InlineData(2, 1)] [InlineData(4, 1)] [InlineData(5, 2)]
    [InlineData(11, 2)] [InlineData(12, 2)] [InlineData(14, 2)] [InlineData(21, 0)]
    [InlineData(22, 1)] [InlineData(25, 2)] [InlineData(101, 0)] [InlineData(111, 2)]
    [InlineData(1001, 0)] [InlineData(1002, 1)]
    public void RussianPluralsFollowTheRules(long count, int expected) =>
        Assert.Equal(expected, Loc.PluralIndex("ru", count));

    [Theory]
    [InlineData(1, 0)]
    [InlineData(2, 2)]
    [InlineData(0, 2)]
    public void EnglishPluralsOnlyHaveOneAndMany(long count, int expected) =>
        Assert.Equal(expected, Loc.PluralIndex("en", count));

    [Fact]
    public void LanguageSelectionFallsBackToTheOsLanguage()
    {
        var osIsRussian = System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "ru";
        var expected = osIsRussian ? "ru" : "en";

        Assert.Equal("ru", Loc.Pick("ru"));
        Assert.Equal("ru", Loc.Pick("ru-RU"));
        Assert.Equal("en", Loc.Pick("en-GB"));
        // Unknown or missing language: follow the Windows display language.
        Assert.Equal(expected, Loc.Pick("de"));
        Assert.Equal(expected, Loc.Pick(null));
        Assert.Equal(expected, Loc.Pick("  "));
    }

    [Fact]
    public void EveryKeyUsedByCodeAndXamlExists()
    {
        var root = RepoRoot();
        var known = Loc.Keys.ToHashSet(StringComparer.Ordinal);
        var missing = new SortedSet<string>(StringComparer.Ordinal);
        foreach (var file in Directory.EnumerateFiles(Path.Combine(root, "src"), "*", SearchOption.AllDirectories))
        {
            if (!file.EndsWith(".cs", StringComparison.OrdinalIgnoreCase) &&
                !file.EndsWith(".xaml", StringComparison.OrdinalIgnoreCase)) continue;
            if (file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}") ||
                file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")) continue;

            var text = File.ReadAllText(file);
            foreach (Match m in Regex.Matches(text, "Loc\\.T\\(\"([A-Za-z0-9_]+)\""))
                if (!known.Contains(m.Groups[1].Value)) missing.Add($"{Path.GetFileName(file)}: {m.Groups[1].Value}");
            foreach (Match m in Regex.Matches(text, @"loc:Tr\s+([A-Za-z0-9_]+)"))
                if (!known.Contains(m.Groups[1].Value)) missing.Add($"{Path.GetFileName(file)}: {m.Groups[1].Value}");
        }
        Assert.Empty(missing);
    }
}
