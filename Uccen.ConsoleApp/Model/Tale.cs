
using System.Globalization;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

public class Tale
{
    public TaleContent Source { get; set; } = new TaleContent();
    public TaleContent Translation { get; set; } = new TaleContent();
    [JsonIgnore]
    public string? IllustrationSvgPath { get; set; }
}

public static class TaleXt
{
    public static DateTime Date { get; set; } = DateTime.Now;

    public static void Validate(this IEnumerable<Tale> tales)
    {
        foreach (var tale in tales)
        {
            tale.Validate();
        }
    }

    public static void Validate(this Tale tale)
    {
        if (tale.Source.Paragraphs.Count != tale.Translation.Paragraphs.Count)
            throw new InvalidDataException(
                $"Tale '{tale.Source.Title}': {tale.Source.Paragraphs.Count} source vs " +
                $"{tale.Translation.Paragraphs.Count} translation paragraphs.");
    }

    public static void ApplyTemplates(this IEnumerable<Tale> tales)
    {
        foreach (var tale in tales)
        {
            tale.ApplyTemplates();
        }
    }

    private static readonly Regex UpdatedAtRegex = new(
        @"\{\{updated_at(?:\|([^}]+))?\}\}",
        RegexOptions.IgnoreCase | RegexOptions.Compiled
    );

    private static readonly CultureInfo DefaultCulture = new("fr-FR");

    public static void ApplyTemplates(this Tale tale)
    {
        if (tale is null) return;

        DateTime date = TaleXt.Date;

        if (tale.Source?.Paragraphs is not null)
        {
            tale.Source.Paragraphs = ProcessParagraphs(tale.Source.Paragraphs, date);
        }

        if (tale.Translation?.Paragraphs is not null)
        {
            tale.Translation.Paragraphs = ProcessParagraphs(tale.Translation.Paragraphs, date);
        }
    }

    private static List<string> ProcessParagraphs(List<string> paragraphs, DateTime updatedAt)
    {
        return paragraphs.Select(p => ApplyTokens(p, updatedAt)).ToList();
    }

    private static string ApplyTokens(string s, DateTime updatedAt)
    {
        if (string.IsNullOrEmpty(s))
            return s;

        if (string.Equals(s, "{{empty}}", StringComparison.OrdinalIgnoreCase))
            return string.Empty;

        s = s.Replace("{{Indent}}", "\u2003", StringComparison.OrdinalIgnoreCase)
             .Replace("{{إزاحة}}", "\u2003", StringComparison.OrdinalIgnoreCase);

        return ApplyDate(s, updatedAt);
    }

    public static string ApplyDate(string s, DateTime? updatedAt = null, CultureInfo? culture = null)
    {
        if (string.IsNullOrEmpty(s) || !s.Contains("{{updated_at", StringComparison.OrdinalIgnoreCase))
            return s;

        DateTime date = updatedAt ?? DateTime.Now;
        CultureInfo targetCulture = culture ?? DefaultCulture;

        return UpdatedAtRegex.Replace(s, match =>
        {
            string format = match.Groups[1].Success ? match.Groups[1].Value.Trim() : "yyyy-MM-dd";
            return date.ToString(format, targetCulture);
        });
    }
}
