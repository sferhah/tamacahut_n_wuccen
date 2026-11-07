using SkiaSharp;
using Topten.RichTextKit;
// ---------- fonts loaded from file paths ----------
/// <summary>
/// Lets a font be given as "Regular.ttf" or "Regular.ttf|Bold.ttf" instead of an installed
/// family name. Characters missing from the file still fall back to installed fonts.
/// </summary>
public sealed class FileFontMapper : FontMapper
{
    private sealed record Faces(SKTypeface Regular, SKTypeface? Bold, SKTypeface? Italic, SKTypeface? BoldItalic);

    private readonly Dictionary<string, Faces> _fonts = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>True if the argument looks like a path rather than a family name.</summary>
    public static bool IsPath(string arg) =>
        arg.Contains('\\') || arg.Contains('/') ||
        arg.Split('|')[0].Trim().EndsWith(".ttf", StringComparison.OrdinalIgnoreCase) ||
        arg.Split('|')[0].Trim().EndsWith(".otf", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Loads "regular|bold|italic|boldItalic" (only regular is required; empty parts are skipped)
    /// and returns the family name to use in a Style.
    /// </summary>
    public string Register(string spec)
    {
        var parts = spec.Split('|');
        SKTypeface? Part(int i) => parts.Length > i && parts[i].Trim().Length > 0 ? Load(parts[i]) : null;

        string key = $"file-font-{_fonts.Count}";
        _fonts[key] = new Faces(Load(parts[0]), Part(1), Part(2), Part(3));
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
        {
            bool bold = style.FontWeight >= 600;
            if (style.FontItalic)
                return (bold ? f.BoldItalic ?? f.Italic : f.Italic) ?? (bold ? f.Bold : null) ?? f.Regular;
            return (bold ? f.Bold : null) ?? f.Regular;
        }

        return base.TypefaceFromStyle(style, ignoreFontVariants); // installed font by name
    }
}