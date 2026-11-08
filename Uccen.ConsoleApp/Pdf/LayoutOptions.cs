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

    public double SourceBodySize { get; set; } = 10.5;
    public double TranslationBodySize { get; set; } = 10.5;
    public double SourceTitleSize { get; set; } = 15;
    public double TranslationTitleSize { get; set; } = 15;
    public double SourceLineSpacing { get; set; } = 1.35;   // multiple of font size
    public double TranslationLineSpacing { get; set; } = 1.35;
    public double SourceParagraphGap { get; set; } = 6;     // points after each paragraph
    public double TranslationParagraphGap { get; set; } = 6;
    public double SourceTitleGap { get; set; } = 14;        // points after the title
    public double TranslationTitleGap { get; set; } = 14;

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
    // LayoutOptions
    /// <summary>Arabic-style book: bound on the right, so the first page of each spread is the RIGHT page.</summary>
    public bool RightToLeftBook { get; set; } = false;

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

    // ----- back cover (last page) -----
    public string? BackCoverText { get; set; }          // e.g. a short summary (source language)
    public string? BackCoverTranslation { get; set; }   // optional, drawn below
    public string? BackCoverSvgPath { get; set; }
    public double BackCoverImageWidth { get; set; } = 0.6;  // fraction of the width between cover margins
    public double BackCoverTop { get; set; } = 0.14;        // where the content starts (fraction of page height)

    /// <summary>For print: pad with blank pages so the total is a multiple of this (4 for a folded booklet). 0 = off.</summary>
    public int PageMultiple { get; set; } = 0;

    /// <summary>Font size of the back cover text, in points.</summary>
    public double BackCoverTextSize { get; set; } = 11.5;

    public bool HasBackCover => !string.IsNullOrWhiteSpace(BackCoverText) ||
                                !string.IsNullOrWhiteSpace(BackCoverTranslation) ||
                                !string.IsNullOrWhiteSpace(BackCoverSvgPath);

    

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
