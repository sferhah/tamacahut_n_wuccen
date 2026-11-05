using SkiaSharp;
using Topten.RichTextKit;
// ---------- fonts loaded from file paths ----------
/// <summary>
/// Lets a font be given as "Regular.ttf" or "Regular.ttf|Bold.ttf" instead of an installed
/// family name. Characters missing from the file still fall back to installed fonts.
/// </summary>
public sealed class FileFontMapper : FontMapper
{
    private readonly Dictionary<string, (SKTypeface Regular, SKTypeface? Bold)> _fonts =
        new(StringComparer.OrdinalIgnoreCase);

    /// <summary>True if the argument looks like a path rather than a family name.</summary>
    public static bool IsPath(string arg) =>
        arg.Contains('\\') || arg.Contains('/') ||
        arg.Split('|')[0].Trim().EndsWith(".ttf", StringComparison.OrdinalIgnoreCase) ||
        arg.Split('|')[0].Trim().EndsWith(".otf", StringComparison.OrdinalIgnoreCase);

    /// <summary>Loads the file(s) and returns the family name to use in a Style.</summary>
    public string Register(string spec)
    {
        var parts = spec.Split('|');
        var regular = Load(parts[0]);
        var bold = parts.Length > 1 && parts[1].Trim().Length > 0 ? Load(parts[1]) : null;

        string key = $"file-font-{_fonts.Count}";
        _fonts[key] = (regular, bold);
        return key;
    }

    private static SKTypeface Load(string path)
    {
        path = path.Trim();
        if (!File.Exists(path)) throw new FileNotFoundException("Font file not found", path);
        return SKTypeface.FromFile(path) ?? throw new InvalidDataException($"Cannot load font: {path}");
    }

    public override SKTypeface TypefaceFromStyle(IStyle style, bool ignoreFontVariants)
    {
        if (style.FontFamily is not null && _fonts.TryGetValue(style.FontFamily, out var f))
            return style.FontWeight >= 600 && f.Bold is not null ? f.Bold : f.Regular;

        return base.TypefaceFromStyle(style, ignoreFontVariants); // installed font by name
    }
}
