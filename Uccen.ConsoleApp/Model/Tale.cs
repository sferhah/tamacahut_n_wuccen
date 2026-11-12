
using System.Text.Json.Serialization;

public class Tale
{
    public TaleContent Source { get; set; } = new TaleContent();
    public TaleContent Translation { get; set; } = new TaleContent();
    [JsonIgnore]
    public string? IllustrationSvgPath { get; set; }
}

public static class TaleXt
{
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

    public static void ApplyTemplates(this Tale tale)
    {
        tale.Source.Paragraphs = tale.Source.Paragraphs.Select(s => string.Equals(s, "{{empty}}", StringComparison.OrdinalIgnoreCase) ? string.Empty : s).ToList();
        tale.Translation.Paragraphs = tale.Translation.Paragraphs.Select(s => string.Equals(s, "{{empty}}", StringComparison.OrdinalIgnoreCase) ? string.Empty : s).ToList();

        tale.Source.Paragraphs = tale.Source.Paragraphs.Select(s => s.Replace("{{Indent}}", "\u2003", StringComparison.OrdinalIgnoreCase)).ToList();
        tale.Translation.Paragraphs = tale.Translation.Paragraphs.Select(s => s.Replace("{{Indent}}", "\u2003", StringComparison.OrdinalIgnoreCase)).ToList();


        tale.Source.Paragraphs = tale.Source.Paragraphs.Select(s => s.Replace("{{إزاحة}}", "\u2003", StringComparison.OrdinalIgnoreCase)).ToList();
        tale.Translation.Paragraphs = tale.Translation.Paragraphs.Select(s => s.Replace("{{إزاحة}}", "\u2003", StringComparison.OrdinalIgnoreCase)).ToList();
    }
}