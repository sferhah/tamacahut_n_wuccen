using SkiaSharp;
using Svg.Skia;
using System.Text;
using System.Text.RegularExpressions;
using Topten.RichTextKit;
// ---------- engine ----------
public sealed class FacingPagesRenderer
{
    private readonly LayoutOptions _o;
    private readonly double _pageW, _pageH, _textW, _textH;

    public FacingPagesRenderer(LayoutOptions options)
    {
        _o = options;
        _pageW = Mm(_o.PageWidthMm);
        _pageH = Mm(_o.PageHeightMm);
        _textW = _pageW - Mm(_o.InnerMm) - Mm(_o.OuterMm);
        _textH = _pageH - Mm(_o.TopMm) - Mm(_o.BottomMm);
    }

    private static double Mm(double mm) => mm * 72.0 / 25.4; // PDF units are points

    // ----- direction detection: first strong character decides -----
    private static bool IsRtl(string text)
    {
        foreach (var ch in text)
        {
            if ((ch >= '֐' && ch <= 'ࣿ') ||   // Hebrew, Arabic, Syriac, Thaana, NKo, Arabic Ext.
                (ch >= 'יִ' && ch <= '﷿') ||   // Hebrew & Arabic presentation forms A
                (ch >= 'ﹰ' && ch <= '﻿'))     // Arabic presentation forms B
                return true;
            if (char.IsLetter(ch)) return false;
        }
        return false;
    }

    // NFC: letter + combining mark -> precomposed letter where one exists.
    // Soft hyphens are dropped: this layout never hyphenates.
    private static string Normalize(string text) =>
        text.Normalize(NormalizationForm.FormC).Replace("­", "");

    // 1) Lay out each paragraph with the real fonts.
    private Block MakeBlock(string? text, string fontFamily, bool isTitle, bool centerContent = false)
    {
        double gap = isTitle ? _o.TitleGap : _o.ParagraphGap;
        if (string.IsNullOrWhiteSpace(text)) return new Block { Text = null, GapAfter = gap };

        var clean = Normalize(text);
        bool rtl = IsRtl(clean);

        var tb = new TextBlock
        {
            MaxWidth = (float)_textW,
            BaseDirection = rtl ? TextDirection.RTL : TextDirection.LTR,
            Alignment = isTitle || centerContent ? TextAlignment.Center : rtl ? TextAlignment.Right : TextAlignment.Left,
        };
        AddStyledText(tb, clean, new Style
        {
            FontFamily = fontFamily,
            FontSize = (float)(isTitle ? _o.TitleSize : _o.BodySize),
            FontWeight = isTitle ? 700 : 400,
            LineHeight = (float)_o.LineSpacing,
            TextColor = SKColors.Black,
        });

        return new Block { Text = tb, GapAfter = gap };
    }

    // ----- ''text'' -> italic (two apostrophes on each side, as in wiki markup) -----
    // Single apostrophes (l'homme, don't) are ordinary text, also inside an italic span.
    private static readonly Regex ItalicSpan = new(@"''(.+?)''", RegexOptions.Compiled);

    /// <summary>Adds text to the block; spans written as ''like this'' become italic (markers removed).</summary>
    private static void AddStyledText(TextBlock tb, string text, Style style)
    {
        var italic = new Style
        {
            FontFamily = style.FontFamily,
            FontSize = style.FontSize,
            FontWeight = style.FontWeight,
            LineHeight = style.LineHeight,
            TextColor = style.TextColor,
            FontItalic = true,
        };

        int pos = 0;
        foreach (Match m in ItalicSpan.Matches(text))
        {
            if (m.Index > pos) tb.AddText(text[pos..m.Index], style);
            tb.AddText(m.Groups[1].Value, italic);
            pos = m.Index + m.Length;
        }
        if (pos < text.Length) tb.AddText(text[pos..], style);
    }

    // 2) Paginate pairs of blocks into spreads, keeping them in lock-step.
    public List<Spread> Paginate(IEnumerable<Tale> tales)
    {
        var spreads = new List<Spread>();
        var cur = new Spread();
        double yS = 0, yT = 0;

        void NewSpread()
        {
            spreads.Add(cur);
            cur = new Spread();
            yS = yT = 0;
        }

        // How many lines of b, starting at line `start`, fit below height y.
        int LinesThatFit(double y, Block b, int start)
        {
            int n = 0;
            while (start + n < b.LineCount && b.Bottom(start + n) - b.Top(start) <= _textH - y + 0.01) n++;
            return n;
        }

        // Place up to n lines of b at y; returns the new y.
        double Place(List<PlacedPiece> page, Block b, int first, int n, double y)
        {
            if (n == 0) return y;
            page.Add(new PlacedPiece(b, first, n, y));
            return y + b.Bottom(first + n - 1) - b.Top(first);
        }

        foreach (var tale in tales)
        {
            tale.Validate(); // throws if source/translation paragraph counts differ

            if (_o.NewSpreadPerTale && !cur.IsEmpty) NewSpread();

            var pairs = new List<(Block S, Block T)>
            {
                (MakeBlock(tale.Source.Title, _o.SourceFont, true),
                 MakeBlock(tale.Translation.Title, _o.TranslationFont, true))
            };
            bool centerContent = tale.Source.Title?.StartsWith("#", StringComparison.Ordinal) == true
                              || tale.Translation.Title?.StartsWith("#", StringComparison.Ordinal) == true;
            for (int i = 0; i < tale.Source.Paragraphs.Count; i++)
                pairs.Add((MakeBlock(tale.Source.Paragraphs[i], _o.SourceFont, false, centerContent),
                           MakeBlock(tale.Translation.Paragraphs[i], _o.TranslationFont, false, centerContent)));

            foreach (var (S, T) in pairs)
            {
                if (S.LineCount == 0 && T.LineCount == 0) continue;

                if (_o.AlignParagraphTops) yS = yT = Math.Max(yS, yT);

                // Keep paragraphs whole: if either side doesn't fit completely in the space
                // left on this spread, move BOTH sides to the next spread.
                if (!cur.IsEmpty && (LinesThatFit(yS, S, 0) < S.LineCount || LinesThatFit(yT, T, 0) < T.LineCount))
                    NewSpread();

                // A pair only splits when a paragraph is taller than a whole empty page;
                // then both sides continue on the next spread.
                int iS = 0, iT = 0;
                while (true)
                {
                    int nS = LinesThatFit(yS, S, iS);
                    int nT = LinesThatFit(yT, T, iT);

                    yS = Place(cur.Source, S, iS, nS, yS); iS += nS;
                    yT = Place(cur.Translation, T, iT, nT, yT); iT += nT;

                    if (iS == S.LineCount && iT == T.LineCount) break;
                    NewSpread();
                }

                if (S.LineCount > 0) yS += S.GapAfter;
                if (T.LineCount > 0) yT += T.GapAfter;
            }

            if (!string.IsNullOrWhiteSpace(tale.IllustrationSvgPath))
            {
                using var svg = new SKSvg();
                var picture = svg.Load(tale.IllustrationSvgPath)
                              ?? throw new InvalidDataException($"Cannot load SVG: {tale.IllustrationSvgPath}");
                var bounds = picture.CullRect;

                if (bounds.Width > 0 && bounds.Height > 0)
                {
                    // Center the illustration across the full spread while keeping it
                    // within the text area and available vertical space of one page.
                    double scale = Math.Min(2 * _textW / bounds.Width, _textH / bounds.Height);
                    double width = bounds.Width * scale;
                    double height = bounds.Height * scale;
                    double y = Math.Max(yS, yT);

                    if (y + height > _textH + 0.01 && !cur.IsEmpty)
                    {
                        NewSpread();
                        y = 0;
                    }

                    cur.Illustrations.Add(new PlacedIllustration(
                        tale.IllustrationSvgPath,
                        (_pageW * 2 - width) / 2,
                        y,
                        width,
                        height));
                    yS = yT = y + height;
                }
            }
        }

        if (!cur.IsEmpty) spreads.Add(cur);
        return spreads;
    }

    // 3) Draw to PDF.
    public void Render(Book book, string outputPath)
    {
        var spreads = Paginate(book.Tales);

        using var stream = File.Create(outputPath);
        using var doc = SKDocument.CreatePdf(stream);
        int pageNumber = 0;

        // Page 1: the cover if there is one, otherwise (optionally) blank.
        // Either way the spreads stay on pages (2,3), (4,5)...
        if (_o.HasCover)
        {
            DrawCover(doc);
            pageNumber++;
        }
        else if (_o.LeadingBlankPage)
        {
            doc.BeginPage((float)_pageW, (float)_pageH);
            doc.EndPage();
            pageNumber++;
        }

        foreach (var s in spreads)
        {
            var (left, right) = _o.SourceOnLeft ? (s.Source, s.Translation) : (s.Translation, s.Source);
            DrawPage(doc, left, s.Illustrations, isLeftPage: !_o.RightToLeftBook, ++pageNumber);
            DrawPage(doc, right, s.Illustrations, isLeftPage: _o.RightToLeftBook, ++pageNumber);
        }

        doc.Close();
        Console.WriteLine($"{spreads.Count} spreads, {pageNumber} pages -> {outputPath}");
    }


    // ----- cover: title, optional subtitle, then the SVG centred in the remaining space -----
    private void DrawCover(SKDocument doc)
    {
        var canvas = doc.BeginPage((float)_pageW, (float)_pageH);

        double margin = Mm(_o.OuterMm);       // symmetric margins: the cover isn't bound like a spread
        double width = _pageW - 2 * margin;
        double y = _pageH * _o.CoverTitleTop;

        // Draw the author independently so the existing title/subtitle/image positions stay unchanged.
        DrawCoverText(canvas, _o.CoverAuthor, _o.TranslationFont, 12, 400, margin, _pageH * 0.04, width);

        y = DrawCoverText(canvas, _o.CoverTitle, _o.SourceFont, _o.CoverTitleSize, 700, margin, y, width);
        if (!string.IsNullOrWhiteSpace(_o.CoverSubtitle))
            y = DrawCoverText(canvas, _o.CoverSubtitle, _o.TranslationFont, _o.CoverSubtitleSize, 400,
                              margin, y + Mm(4), width);

        if (!string.IsNullOrWhiteSpace(_o.CoverSvgPath))
        {
            // Box for the image: below the title, above the bottom margin.
            double boxTop = y + Mm(_o.CoverImageGapMm);
            double boxBottom = _pageH - Mm(_o.BottomMm);
            double boxW = _pageW * _o.CoverImageMaxWidth;   // full page width, no margins
            double boxH = boxBottom - boxTop;

            // Fits a w x h image into the box, centred; returns its rectangle on the page.
            SKRect Fit(float w, float h)
            {
                double scale = Math.Min(boxW / w, boxH / h);
                double x = (_pageW - w * scale) / 2;
                double top = boxTop + (boxH - h * scale) / 2;
                return SKRect.Create((float)x, (float)top, (float)(w * scale), (float)(h * scale));
            }

            if (_o.CoverSvgPath.EndsWith(".svg", StringComparison.OrdinalIgnoreCase))
            {
                // Simple SVGs: drawn as vectors. Heavy filters/masks may not render; use a PNG then.
                using var svg = new SKSvg();
                var picture = svg.Load(_o.CoverSvgPath)
                              ?? throw new InvalidDataException($"Cannot load SVG: {_o.CoverSvgPath}");
                var r = picture.CullRect;

                if (r.Width > 0 && r.Height > 0 && boxH > 0)
                {
                    var dest = Fit(r.Width, r.Height);
                    canvas.Save();
                    canvas.Translate(dest.Left, dest.Top);
                    canvas.Scale(dest.Width / r.Width);
                    canvas.Translate(-r.Left, -r.Top);
                    canvas.DrawPicture(picture);
                    canvas.Restore();
                }
            }
            else
            {
                // PNG / JPG: embedded at full resolution (transparency is kept).
                using var image = SKImage.FromEncodedData(_o.CoverSvgPath)
                                  ?? throw new InvalidDataException($"Cannot load image: {_o.CoverSvgPath}");
                if (boxH > 0)
                {   
                    canvas.DrawImage(image, Fit(image.Width, image.Height), new SKSamplingOptions(SKCubicResampler.Mitchell));
                }
            }
        }

        doc.EndPage();
    }

    /// <summary>Draws centred text (RTL-aware) and returns the y just below it.</summary>
    private double DrawCoverText(SKCanvas canvas, string? text, string font, double size, int weight,
                                 double x, double y, double width)
    {
        if (string.IsNullOrWhiteSpace(text)) return y;

        var clean = Normalize(text);
        var tb = new TextBlock
        {
            MaxWidth = (float)width,
            Alignment = TextAlignment.Center,
            BaseDirection = IsRtl(clean) ? TextDirection.RTL : TextDirection.LTR,
        };
        AddStyledText(tb, clean, new Style
        {
            FontFamily = font,
            FontSize = (float)size,
            FontWeight = weight,
            LineHeight = 1.2f,
            TextColor = SKColors.Black,
        });
        tb.Paint(canvas, new SKPoint((float)x, (float)y));
        return y + tb.MeasuredHeight;
    }

    private void DrawPage(SKDocument doc, List<PlacedPiece> pieces, List<PlacedIllustration> illustrations,
                          bool isLeftPage, int pageNumber)
    {
        var canvas = doc.BeginPage((float)_pageW, (float)_pageH);

        // Mirrored margins: inner (binding) margin is on the right of a left page and vice versa.
        double x0 = isLeftPage ? Mm(_o.OuterMm) : Mm(_o.InnerMm);
        double y0 = Mm(_o.TopMm);

        foreach (var p in pieces)
        {
            var tb = p.Block.Text!;
            double top = p.Block.Top(p.FirstLine);
            double height = p.Block.Bottom(p.FirstLine + p.LineCount - 1) - top;
            bool whole = p.FirstLine == 0 && p.LineCount == p.Block.LineCount;

            canvas.Save();
            if (!whole) // only part of the paragraph belongs on this page: clip to those lines
                canvas.ClipRect(new SKRect(0, (float)(y0 + p.Y), (float)_pageW, (float)(y0 + p.Y + height)));
            tb.Paint(canvas, new SKPoint((float)x0, (float)(y0 + p.Y - top)));
            canvas.Restore();
        }

        foreach (var illustration in illustrations)
            DrawIllustration(canvas, illustration, y0, isLeftPage ? 0 : _pageW);

        if (_o.PageNumbers)
        {
            var num = new TextBlock
            {
                MaxWidth = (float)_textW,
                Alignment = isLeftPage ? TextAlignment.Left : TextAlignment.Right,
            };
            num.AddText(pageNumber.ToString(), new Style
            {
                FontFamily = _o.SourceFont,
                FontSize = 8.5f,
                TextColor = SKColors.Gray,
            });
            num.Paint(canvas, new SKPoint((float)x0, (float)(_pageH - Mm(_o.BottomMm) + Mm(8))));
        }

        doc.EndPage();
    }

    private void DrawIllustration(SKCanvas canvas, PlacedIllustration illustration, double y0, double pageOffset)
    {
        using var svg = new SKSvg();
        var picture = svg.Load(illustration.Path)
                      ?? throw new InvalidDataException($"Cannot load SVG: {illustration.Path}");
        var bounds = picture.CullRect;
        if (bounds.Width <= 0 || bounds.Height <= 0) return;

        canvas.Save();
        canvas.Translate((float)(illustration.X - pageOffset), (float)(y0 + illustration.Y));
        canvas.Scale((float)(illustration.Width / bounds.Width), (float)(illustration.Height / bounds.Height));
        canvas.Translate(-bounds.Left, -bounds.Top);
        canvas.DrawPicture(picture);
        canvas.Restore();
    }


}
