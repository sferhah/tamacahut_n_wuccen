// Facing-page (bilingual) PDF generator with right-to-left support (Arabic, Hebrew).
//
// Same pagination rules as the PDFsharp version:
//   - paragraph i of the source and of the translation always start on the same spread,
//   - a paragraph pair that doesn't fit moves, whole, to the next spread on both sides,
//   - paragraph tops are aligned across the two pages.
//
// Text is laid out by RichTextKit (on SkiaSharp + HarfBuzz), which provides:
//   - Arabic shaping (joined letter forms, ligatures, diacritics),
//   - the Unicode bidi algorithm (RTL text with embedded numbers / Latin words),
//   - Unicode line breaking (U+2011, U+00A0 etc. never break),
//   - automatic per-character font fallback to installed fonts, like a browser.
//
// Usage:
//   dotnet run -- tales.json output.pdf [source font] [translation font]
// A font is an installed family name or a path, optionally "regular|bold":
//   dotnet run -- tales.json out.pdf "Calibri" "Segoe UI"
//   dotnet run -- tales.json out.pdf "C:\Fonts\a.ttf|C:\Fonts\a-bold.ttf" "C:\Fonts\arabic.ttf"

// ---------- settings ----------
using Topten.RichTextKit;

public class LayoutOptions
{
    public double PageWidthMm { get; set; } = 148;   // A5
    public double PageHeightMm { get; set; } = 210;
    public double TopMm { get; set; } = 18;
    public double BottomMm { get; set; } = 22;        // leaves room for page number
    public double InnerMm { get; set; } = 20;         // binding side
    public double OuterMm { get; set; } = 14;

    public string SourceFont { get; set; } = "Calibri";
    public string TranslationFont { get; set; } = "Calibri";

    public double BodySize { get; set; } = 10.5;
    public double TitleSize { get; set; } = 15;
    public double LineSpacing { get; set; } = 1.35;   // multiple of font size
    public double ParagraphGap { get; set; } = 6;     // points after each paragraph
    public double TitleGap { get; set; } = 14;        // points after the title

    /// <summary>Re-align both sides so paragraph i starts at the same height on both pages.</summary>
    public bool AlignParagraphTops { get; set; } = true;

    /// <summary>Each tale starts on a new spread.</summary>
    public bool NewSpreadPerTale { get; set; } = true;

    /// <summary>Insert a blank page 1 so spreads are pages (2,3), (4,5)...</summary>
    public bool LeadingBlankPage { get; set; } = true;

    /// <summary>true: source on the left (even) page. false: translation on the left,
    /// e.g. for an Arabic-first edition.</summary>
    public bool SourceOnLeft { get; set; } = true;

    public bool PageNumbers { get; set; } = true;

    // ----- cover (page 1). Cover content replaces the leading blank page. -----
    public string? CoverAuthor { get; set; }
    public string? CoverTitle { get; set; }
    /// <summary>Optional second line, e.g. the title in the translation language.</summary>
    public string? CoverSubtitle { get; set; }
    public string? CoverSvgPath { get; set; }
    public double CoverTitleSize { get; set; } = 26;
    public double CoverSubtitleSize { get; set; } = 18;
    /// <summary>Where the title starts, as a fraction of the page height from the top.</summary>
    public double CoverTitleTop { get; set; } = 0.16;
    /// <summary>Maximum image width, as a fraction of the width between the margins.</summary>
    public double CoverImageMaxWidth { get; set; } = 0.85;
    /// <summary>Space between the title block and the image.</summary>
    public double CoverImageGapMm { get; set; } = 8;
    public bool HasCover => !string.IsNullOrWhiteSpace(CoverAuthor)
                         || !string.IsNullOrWhiteSpace(CoverTitle)
                         || !string.IsNullOrWhiteSpace(CoverSvgPath);

    // LayoutOptions
    /// <summary>Arabic-style book: bound on the right, so the first page of each spread is the RIGHT page.</summary>
    public bool RightToLeftBook { get; set; } = false;

}

// ---------- layout primitives ----------

/// <summary>One laid-out paragraph (or title). Text is null for an empty paragraph.</summary>
public sealed class Block
{
    public TextBlock? Text { get; init; }
    public double GapAfter { get; init; }

    public int LineCount => Text?.Lines.Count ?? 0;
    public double Top(int line) => Text!.Lines[line].YCoord;
    public double Bottom(int line) => Text!.Lines[line].YCoord + Text!.Lines[line].Height;
}

/// <summary>A run of lines from one block, placed at height Y on a page.</summary>
public sealed record PlacedPiece(Block Block, int FirstLine, int LineCount, double Y);

/// <summary>An SVG illustration placed across a facing-page spread.</summary>
public sealed record PlacedIllustration(string Path, double X, double Y, double Width, double Height);

public sealed class Spread
{
    public List<PlacedPiece> Source { get; } = new();
    public List<PlacedPiece> Translation { get; } = new();
    public List<PlacedIllustration> Illustrations { get; } = new();
    public bool IsEmpty => Source.Count == 0 && Translation.Count == 0 && Illustrations.Count == 0;
}
