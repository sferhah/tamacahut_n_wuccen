using System.Text;
using SkiaSharp;
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
    private Block MakeBlock(string? text, string fontFamily, bool isTitle)
    {
        double gap = isTitle ? _o.TitleGap : _o.ParagraphGap;
        if (string.IsNullOrWhiteSpace(text)) return new Block { Text = null, GapAfter = gap };

        var clean = Normalize(text);
        bool rtl = IsRtl(clean);

        var tb = new TextBlock
        {
            MaxWidth = (float)_textW,
            BaseDirection = rtl ? TextDirection.RTL : TextDirection.LTR,
            Alignment = isTitle ? TextAlignment.Center : rtl ? TextAlignment.Right : TextAlignment.Left,
        };
        tb.AddText(clean, new Style
        {
            FontFamily = fontFamily,
            FontSize = (float)(isTitle ? _o.TitleSize : _o.BodySize),
            FontWeight = isTitle ? 700 : 400,
            LineHeight = (float)_o.LineSpacing,
            TextColor = SKColors.Black,
        });

        return new Block { Text = tb, GapAfter = gap };
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
            for (int i = 0; i < tale.Source.Paragraphs.Count; i++)
                pairs.Add((MakeBlock(tale.Source.Paragraphs[i], _o.SourceFont, false),
                           MakeBlock(tale.Translation.Paragraphs[i], _o.TranslationFont, false)));

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
        }

        if (!cur.IsEmpty) spreads.Add(cur);
        return spreads;
    }

    // 3) Draw to PDF.
    public void Render(IEnumerable<Tale> tales, string outputPath)
    {
        var spreads = Paginate(tales);

        using var stream = File.Create(outputPath);
        using var doc = SKDocument.CreatePdf(stream);
        int pageNumber = 0;

        if (_o.LeadingBlankPage)
        {
            doc.BeginPage((float)_pageW, (float)_pageH);
            doc.EndPage();
            pageNumber++;
        }

        foreach (var s in spreads)
        {
            var (left, right) = _o.SourceOnLeft ? (s.Source, s.Translation) : (s.Translation, s.Source);
            DrawPage(doc, left, isLeftPage: true, ++pageNumber);
            DrawPage(doc, right, isLeftPage: false, ++pageNumber);
        }

        doc.Close();
        Console.WriteLine($"{spreads.Count} spreads, {pageNumber} pages -> {outputPath}");
    }

    private void DrawPage(SKDocument doc, List<PlacedPiece> pieces, bool isLeftPage, int pageNumber)
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
}
