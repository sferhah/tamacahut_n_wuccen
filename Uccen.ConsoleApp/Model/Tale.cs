

public class Tale
{
    public TaleContent Source { get; set; } = new TaleContent();
    public TaleContent Translation { get; set; } = new TaleContent();
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
}