using PdfSharp;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
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
    private static readonly Regex ImageMarkup = new(@"\{\{Image\|([^|]+)\|([^|]*)\|(\d+(?:\.\d+)?)px\}\}", RegexOptions.Compiled);
    private sealed record ImageCaptionRequest(string Path, string Caption, double WidthPixels, bool IsSource);

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

    private static bool ContainsArabicScript(string text) => text.EnumerateRunes().Any(rune =>
        (rune.Value >= 0x0600 && rune.Value <= 0x06FF) ||
        (rune.Value >= 0x0750 && rune.Value <= 0x077F) ||
        (rune.Value >= 0x0870 && rune.Value <= 0x089F) ||
        (rune.Value >= 0x08A0 && rune.Value <= 0x08FF) ||
        (rune.Value >= 0xFB50 && rune.Value <= 0xFDFF) ||
        (rune.Value >= 0xFE70 && rune.Value <= 0xFEFF) ||
        (rune.Value >= 0x10EC0 && rune.Value <= 0x10EFF) ||
        (rune.Value >= 0x1EE00 && rune.Value <= 0x1EEFF));

    // NFC: letter + combining mark -> precomposed letter where one exists.
    // Soft hyphens are dropped: this layout never hyphenates.
    private static string Normalize(string text) =>
        text.Normalize(NormalizationForm.FormC).Replace("­", "");

    // 1) Lay out each paragraph with the real fonts.
    private Block MakeBlock(string? text, string fontFamily, bool isSource, bool isTitle, bool centerContent = false, double? maxWidth = null, double? bodySize = null, double? lineSpacing = null)
    {
        double gap = isSource
            ? isTitle ? _o.SourceTitleGap : _o.SourceParagraphGap
            : isTitle ? _o.TranslationTitleGap : _o.TranslationParagraphGap;
        if (string.IsNullOrWhiteSpace(text)) return new Block { Text = null, GapAfter = gap };

        var clean = Normalize(text);
        bool rtl = IsRtl(clean);

        var tb = new TextBlock
        {
            MaxWidth = (float)(maxWidth ?? _textW),
            BaseDirection = rtl ? TextDirection.RTL : TextDirection.LTR,
            Alignment = isTitle || centerContent ? TextAlignment.Center : rtl ? TextAlignment.Right : TextAlignment.Left,
        };
        AddStyledText(tb, clean, new Style
        {
            FontFamily = fontFamily,
            FontSize = (float)(isTitle
                ? isSource ? _o.SourceTitleSize : _o.TranslationTitleSize
                : bodySize ?? (isSource ? _o.SourceBodySize : _o.TranslationBodySize)),
            FontWeight = isTitle ? 700 : 400,
            LineHeight = (float)(lineSpacing ?? (isSource ? _o.SourceLineSpacing : _o.TranslationLineSpacing)),
            TextColor = SKColors.Black,
        });

        return new Block { Text = tb, GapAfter = gap };
    }

    // ----- Wiki-style emphasis: ''italic'', '''bold''', and '''''bold italic''''' -----
    // Single apostrophes (l'homme, don't) are ordinary text.
    private static readonly Regex EmphasisSpan = new(@"'''''(.+?)'''''|'''(.+?)'''|''(.+?)''", RegexOptions.Compiled);

    /// <summary>Adds text with wiki-style emphasis markers removed.</summary>
    private static void AddStyledText(TextBlock tb, string text, Style style)
    {
        int pos = 0;
        foreach (Match m in EmphasisSpan.Matches(text))
        {
            if (m.Index > pos) tb.AddText(text[pos..m.Index], style);

            bool bold = m.Groups[1].Success || m.Groups[2].Success;
            bool italic = m.Groups[1].Success || m.Groups[3].Success;
            string content = m.Groups[1].Success ? m.Groups[1].Value
                : m.Groups[2].Success ? m.Groups[2].Value
                : m.Groups[3].Value;

            var emphasisStyle = new Style
            {
                FontFamily = style.FontFamily,
                FontSize = style.FontSize,
                FontWeight = bold ? 700 : style.FontWeight,
                LineHeight = style.LineHeight,
                TextColor = style.TextColor,
                FontItalic = italic,
                BackgroundColor = italic && ContainsArabicScript(content)
                    ? new SKColor(245, 245, 245)
                    : style.BackgroundColor,
            };
            tb.AddText(content, emphasisStyle);
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

        Spread NewSpread()
        {
            spreads.Add(cur);
            cur = new Spread();
            yS = yT = 0;
            return cur;
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

            var pairs = new List<(Block S, Block T, ImageCaptionRequest? Image)>
            {
                (MakeBlock(tale.Source.Title, _o.SourceFont, true, true),
                 MakeBlock(tale.Translation.Title, _o.TranslationFont, false, true), null)
            };
            bool centerContent = tale.Source.Title?.StartsWith("#", StringComparison.Ordinal) == true
                              || tale.Translation.Title?.StartsWith("#", StringComparison.Ordinal) == true;
            for (int i = 0; i < tale.Source.Paragraphs.Count; i++)
            {
                string sourceParagraph = tale.Source.Paragraphs[i];
                string translationParagraph = tale.Translation.Paragraphs[i];
                var sourceMatches = ImageMarkup.Matches(sourceParagraph);
                var translationMatches = ImageMarkup.Matches(translationParagraph);
                if (sourceMatches.Count > 1 || translationMatches.Count > 1)
                    throw new InvalidDataException("A paragraph can contain only one image markup.");
                if (sourceMatches.Count > 0 && translationMatches.Count > 0)
                    throw new InvalidDataException("An aligned paragraph pair cannot contain image markup on both sides.");

                if (sourceMatches.Count == 0 && translationMatches.Count == 0)
                {
                    pairs.Add((MakeBlock(sourceParagraph, _o.SourceFont, true, false, centerContent),
                               MakeBlock(translationParagraph, _o.TranslationFont, false, false, centerContent), null));
                    continue;
                }

                bool imageIsSource = sourceMatches.Count > 0;
                var match = imageIsSource ? sourceMatches[0] : translationMatches[0];
                var image = ParseImageMarkup(match, imageIsSource);
                string imageParagraph = imageIsSource ? sourceParagraph : translationParagraph;
                string otherParagraph = imageIsSource ? translationParagraph : sourceParagraph;
                string before = imageParagraph[..match.Index];
                string after = imageParagraph[(match.Index + match.Length)..];

                string sourcePrefix = imageIsSource ? before : otherParagraph;
                string translationPrefix = imageIsSource ? otherParagraph : before;
                if (!string.IsNullOrWhiteSpace(sourcePrefix) || !string.IsNullOrWhiteSpace(translationPrefix))
                    pairs.Add((MakeBlock(sourcePrefix, _o.SourceFont, true, false, centerContent),
                               MakeBlock(translationPrefix, _o.TranslationFont, false, false, centerContent), null));

                pairs.Add((MakeBlock(null, _o.SourceFont, true, false),
                           MakeBlock(null, _o.TranslationFont, false, false), image));

                string sourceSuffix = imageIsSource ? after : string.Empty;
                string translationSuffix = imageIsSource ? string.Empty : after;
                if (!string.IsNullOrWhiteSpace(sourceSuffix) || !string.IsNullOrWhiteSpace(translationSuffix))
                    pairs.Add((MakeBlock(sourceSuffix, _o.SourceFont, true, false, centerContent, bodySize: 6.0),
                               MakeBlock(translationSuffix, _o.TranslationFont, false, false, centerContent, bodySize: 6.0), null));
            }

            foreach (var (S, T, image) in pairs)
            {
                if (S.LineCount == 0 && T.LineCount == 0 && image is null) continue;

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

                if (image is not null)
                    PlaceImageCaption(cur, image, ref yS, ref yT, NewSpread);
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


    private static ImageCaptionRequest ParseImageMarkup(Match match, bool isSource)
    {
        if (!double.TryParse(match.Groups[3].Value, System.Globalization.NumberStyles.AllowDecimalPoint,
                             System.Globalization.CultureInfo.InvariantCulture, out double widthPixels) || widthPixels <= 0)
            throw new InvalidDataException($"Invalid image width in markup: {match.Value}");

        string path = match.Groups[1].Value.Trim();
        if (!Path.IsPathRooted(path))
            path = Path.Combine(PathHelper.GetSveltePath(), "public", "images", path);

        return new ImageCaptionRequest(path, match.Groups[2].Value.Trim(), widthPixels, isSource);
    }

    private void PlaceImageCaption(Spread spread, ImageCaptionRequest request, ref double yS, ref double yT,
                                  Func<Spread> newSpread)
    {
        using var svg = new SKSvg();
        var picture = svg.Load(request.Path)
                      ?? throw new InvalidDataException($"Cannot load SVG: {request.Path}");
        var bounds = picture.CullRect;
        if (bounds.Width <= 0 || bounds.Height <= 0)
            throw new InvalidDataException($"SVG has invalid dimensions: {request.Path}");

        var caption = MakeBlock(request.Caption, request.IsSource ? _o.SourceFont : _o.TranslationFont,
                                request.IsSource, isTitle: false, centerContent: true, maxWidth: _textW,
                                bodySize: _o.ImageCaptionTextSize);
        double captionHeight = caption.LineCount > 0 ? caption.Bottom(caption.LineCount - 1) - caption.Top(0) : 0;
        double captionGap = caption.LineCount > 0 ? Mm(3) : 0;
        double maxImageHeight = _textH - captionHeight - captionGap;
        if (maxImageHeight <= 0)
            throw new InvalidDataException("Image caption is too tall to fit in the page text area.");

        double requestedWidth = request.WidthPixels * 0.75;
        double scale = Math.Min(Math.Min(requestedWidth, _textW) / bounds.Width, maxImageHeight / bounds.Height);
        double width = bounds.Width * scale;
        double height = bounds.Height * scale;
        double itemHeight = height + captionGap + captionHeight;
        double y = Math.Max(yS, yT);

        if (y + itemHeight > _textH + 0.01 && !spread.IsEmpty)
        {
            spread = newSpread();
            y = 0;
        }
        if (itemHeight > _textH + 0.01)
            throw new InvalidDataException("Image and caption are too tall to fit in the page text area.");

        bool isFirstPage = request.IsSource == _o.SourceOnLeft;
        bool isLeftPage = isFirstPage != _o.RightToLeftBook;
        double pageOffset = isLeftPage ? 0 : _pageW;
        double x = pageOffset + (_pageW - width) / 2;
        spread.ImageCaptions.Add(new PlacedImageCaption(request.Path, x, y, width, height, isLeftPage,
            caption.LineCount > 0 ? caption : null, y + height + captionGap));

        double nextY = y + itemHeight + (request.IsSource ? _o.SourceParagraphGap : _o.TranslationParagraphGap);
        yS = yT = nextY;
    }

    // 3) Draw to PDF.
    public FacingPagesRenderer Render(Book book, string outputPath)
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

        int pageNumberOffset = _o.HasFlyLeaf ? pageNumber + 1 : 0;
        for (int spreadIndex = 0; spreadIndex < spreads.Count; spreadIndex++)
        {
            var s = spreads[spreadIndex];
            var (left, right) = _o.SourceOnLeft ? (s.Source, s.Translation) : (s.Translation, s.Source);
            int leftPageNumber = ++pageNumber - pageNumberOffset;
            int rightPageNumber = ++pageNumber - pageNumberOffset;
            if (_o.HasFlyLeaf && spreadIndex == 0 && !_o.SourceOnLeft)
                (leftPageNumber, rightPageNumber) = (rightPageNumber, leftPageNumber);

            DrawPage(doc, left, s.Illustrations, s.ImageCaptions, isLeftPage: !_o.RightToLeftBook, leftPageNumber);
            DrawPage(doc, right, s.Illustrations, s.ImageCaptions, isLeftPage: _o.RightToLeftBook, rightPageNumber);
        }

        if (_o.HasBackCover)
        {
            // Cover + spreads always give an odd page count, so one more page is the back of the book.
            // For print, pad with blank pages first so the total is a multiple of PageMultiple.
            int total = pageNumber + 1;
            int blanks = _o.PageMultiple > 1 ? (_o.PageMultiple - total % _o.PageMultiple) % _o.PageMultiple : 0;
            for (int i = 0; i < blanks; i++)
            {
                doc.BeginPage((float)_pageW, (float)_pageH);
                doc.EndPage();
                pageNumber++;
            }

            DrawBackCover(doc);
            pageNumber++;
        }

        doc.Close();
        Console.WriteLine($"{spreads.Count} spreads, {pageNumber} pages -> {outputPath}");

        return this;
    }

    private void DrawBackCover(SKDocument doc)
    {
        var canvas = doc.BeginPage((float)_pageW, (float)_pageH);

        double margin = Mm(_o.OuterMm) + Mm(6);
        double textWidth = _pageW - 2 * margin;

        DrawBackCoverParagraphs(canvas, _o.BackCoverTranslationParagraphs, margin, textWidth);

        doc.EndPage();
    }

    private void DrawBackCoverParagraphs(SKCanvas canvas, IEnumerable<string> paragraphs,
                                         double x, double width)
    {
        var blocks = paragraphs
            .Select(paragraph => MakeBlock(paragraph, _o.TranslationFont, isSource: false, isTitle: false,
                                           maxWidth: width, bodySize: _o.BackCoverTranslationSize,
                                           lineSpacing: _o.BackCoverTranslationLineSpacing))
            .Where(block => block.LineCount > 0)
            .ToList();

        double y = Mm(_o.TopMm);

        foreach (var block in blocks)
        {
            block.Text!.Paint(canvas, new SKPoint((float)x, (float)(y - block.Top(0))));
            y += block.Bottom(block.LineCount - 1) - block.Top(0) + block.GapAfter;
        }
    }


    public void PatchWithPdfSharp(string outputPath)
    {
        PdfSharpLicense.SetAgplLicense();
        using var pdf = PdfReader.Open(outputPath, PdfDocumentOpenMode.Modify);
        if (_o.RightToLeftBook)
        {
            pdf.ViewerPreferences.Direction = PdfReadingDirection.RightToLeft;  // << /ViewerPreferences << /Direction /R2L >> >> 
        }

        pdf.PageLayout = PdfPageLayout.TwoPageRight;                        // cover alone, then spreads 2|3, 4|5…            
    
        using var stream = new MemoryStream();
        pdf.Save(stream);
        byte[] pdfBytes = stream.ToArray();

        // Convert to Latin1 (1-to-1 byte mapping so binary streams aren't corrupted)
        string pdfText = Encoding.Latin1.GetString(pdfBytes);

        // Replace "/ModDate(D:20261126115648+01'00')" with spaces of the exact same length
        pdfText = Regex.Replace(pdfText, @"/ModDate\([^\)]+\)", match => new string(' ', match.Length));

        File.WriteAllBytes(outputPath, Encoding.Latin1.GetBytes(pdfText));
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

        bool hasCoverTextBelowImage = !string.IsNullOrWhiteSpace(_o.CoverTextBelowImage);
        double coverTextY = _pageH - Mm(_o.BottomMm) - Mm(4) - (hasCoverTextBelowImage ? 14.4 : 0);

        if (!string.IsNullOrWhiteSpace(_o.CoverSvgPath))
        {
            // Box for the image: below the title, above the bottom margin and optional caption.
            double boxTop = y + Mm(_o.CoverImageGapMm);
            double boxBottom = hasCoverTextBelowImage ? coverTextY - Mm(4) : _pageH - Mm(_o.BottomMm);
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

        if (hasCoverTextBelowImage)
            DrawCoverText(canvas, _o.CoverTextBelowImage, _o.TranslationFont, 12, 400, margin, coverTextY, width);

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
                          List<PlacedImageCaption> imageCaptions, bool isLeftPage, int pageNumber)
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

        foreach (var imageCaption in imageCaptions.Where(item => item.IsLeftPage == isLeftPage))
        {
            double pageOffset = isLeftPage ? 0 : _pageW;
            DrawIllustration(canvas, new PlacedIllustration(imageCaption.Path, imageCaption.X, imageCaption.Y,
                imageCaption.Width, imageCaption.Height), y0, pageOffset);
            if (imageCaption.Caption is not null)
                imageCaption.Caption.Text!.Paint(canvas, new SKPoint((float)((_pageW - _textW) / 2),
                    (float)(y0 + imageCaption.CaptionY - imageCaption.Caption.Top(0))));
        }

        if (_o.PageNumbers && pageNumber > 0)
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
