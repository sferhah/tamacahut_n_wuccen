using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.Packaging;

public static class PptxHelper
{
    public static Book ExtractJson(string presentationFile)
    {
        Book book = new Book();

        // Open the presentation as read-only.
        using PresentationDocument presentationDocument = PresentationDocument.Open(presentationFile, false);
        // Verify that the presentation document exists.
        if (presentationDocument == null)
        {
            throw new ArgumentNullException("presentationDocument");
        }

        // Get the presentation part of the presentation document.
        var presentationPart = presentationDocument.PresentationPart;

        // Verify that the presentation part and presentation exist.
        if (presentationPart == null || presentationPart.Presentation == null)
        {
            return book;
        }

        // Get the Presentation object from the presentation part.
        var presentation = presentationPart.Presentation;

        // Verify that the slide ID list exists.
        if (presentation.SlideIdList == null)
        {
            return book;
        }

        // Get the collection of slide IDs from the slide ID list.
        DocumentFormat.OpenXml.OpenXmlElementList slideIds = presentation.SlideIdList.ChildElements;

        Tale? previousTale = null;
        // If the slide ID is in range...
        for (int i = 0; i < slideIds.Count; i++)
        {
            // Get the specified slide part from the relationship ID.
            SlidePart slidePart = (SlidePart)presentationPart.GetPartById(((DocumentFormat.OpenXml.Presentation.SlideId)slideIds[i]).RelationshipId);

            var table = slidePart.Slide.Descendants<DocumentFormat.OpenXml.Drawing.Table>().First();

            var cells = table.Descendants<DocumentFormat.OpenXml.Drawing.TableCell>();

            if (cells.Count() != 9)
            {
                throw new ArgumentNullException("slidePart");
            }

            var tale = new Tale();
            bool sourceStartsWithPilcrow = false;
            bool translationStartsWithPilcrow = false;

            int j = 0;
            foreach (var cell in cells)
            {
                switch (j)
                {
                    case 3:
                        tale.Source.Title = string.Join(string.Empty, cell.ParseParagraphs(true));
                        break;
                    case 6:
                        tale.Source.Paragraphs = cell.ParseParagraphs(false);
                        sourceStartsWithPilcrow = cell.StartsWithPilcrow();
                        break;
                    case 5:
                        tale.Translation.Title = string.Join(string.Empty, cell.ParseParagraphs(true));
                        break;
                    case 8:
                        tale.Translation.Paragraphs = cell.ParseParagraphs(false);
                        translationStartsWithPilcrow = cell.StartsWithPilcrow();
                        break;
                }

                j++;
            }

            if (string.IsNullOrWhiteSpace(tale.Source.Title)
                && string.IsNullOrWhiteSpace(tale.Translation.Title)
                && previousTale != null)
            {
                if (sourceStartsWithPilcrow)
                {
                    previousTale.Source.Paragraphs.AddRange(tale.Source.Paragraphs);
                }
                else
                {
                    // merge the first paragraph with the last paragraph of the previous tale
                    if (previousTale.Source.Paragraphs.Count > 0 && tale.Source.Paragraphs.Count > 0)
                    {
                        previousTale.Source.Paragraphs[previousTale.Source.Paragraphs.Count - 1] += "\n" + tale.Source.Paragraphs[0];
                        tale.Source.Paragraphs.RemoveAt(0);
                        previousTale.Source.Paragraphs.AddRange(tale.Source.Paragraphs);
                    }
                }

                if (translationStartsWithPilcrow)
                {
                    previousTale.Translation.Paragraphs.AddRange(tale.Translation.Paragraphs);
                }
                else
                {
                    // merge the first paragraph with the last paragraph of the previous tale
                    if (previousTale.Translation.Paragraphs.Count > 0 && tale.Translation.Paragraphs.Count > 0)
                    {
                        previousTale.Translation.Paragraphs[previousTale.Translation.Paragraphs.Count - 1] += "\n" + tale.Translation.Paragraphs[0];
                        tale.Translation.Paragraphs.RemoveAt(0);
                        previousTale.Translation.Paragraphs.AddRange(tale.Translation.Paragraphs);
                    }
                }
            }
            else
            {
                book.Tales.Add(tale);
                previousTale = tale;
            }
        }

        book.Tales.Validate();
        book.Tales.ApplyTemplates();

        return book;
    }


    public static bool StartsWithPilcrow(this TableCell pptxCell)
        => pptxCell.Descendants<Paragraph>().FirstOrDefault()?
        .Descendants<Text>().FirstOrDefault()?.Text.TrimStart().StartsWith('¶') ?? false;

    public static List<string> ParseParagraphs(this TableCell pptxCell, bool disableMarkup)
        => pptxCell.Descendants<Paragraph>().ParseParagraphs(disableMarkup);

    public static List<string> ParseParagraphs(this IEnumerable<Paragraph> pptxParagraphs, bool disableMarkup)
    {
        var paragraphs = new List<List<string>>();
        List<string>? currentParagraph = null;

        foreach (var pptxParagraph in pptxParagraphs)
        {
            // Retrieve the text and immediately split it at the ¶ character.
            var rawLine = pptxParagraph.ParseLine(disableMarkup);
            var subLines = rawLine.Split('¶');

            for (int i = 0; i < subLines.Length; i++)
            {
                var cleanLine = subLines[i].Trim();

                // If the line (or sub-line) is empty, this marks a break (null paragraph)
                if (string.IsNullOrWhiteSpace(cleanLine))
                {
                    currentParagraph = null;
                    continue;
                }

                // If we start a segment AFTER a '¶' (i.e., i > 0), 
                // we force the creation of a new paragraph
                if (i > 0)
                {
                    currentParagraph = null;
                }

                if (currentParagraph == null)
                {
                    paragraphs.Add(currentParagraph = new List<string>());
                }

                currentParagraph.Add(cleanLine);
            }
        }

        return paragraphs
            .Where(x => x.Any())
            .Select(x => string.Join("\n", x))
            .ToList();
    }

    public static string ParseLine(this Paragraph paragraph, bool disableMarkup)
    {
        var bulletChar = paragraph.ParagraphProperties?
            .GetFirstChild<CharacterBullet>()?
            .Char?
            .Value;

        string bullet =
            bulletChar == "-"
                ? "— "
                : string.IsNullOrEmpty(bulletChar)
                    ? string.Empty
                    : $"{bulletChar} ";

        var runs = paragraph.Descendants<Text>()
            .Select(t => (
                IsItalic: !string.IsNullOrWhiteSpace(t.Text) && t.IsItalic(),
                IsBold: !string.IsNullOrWhiteSpace(t.Text) && t.IsBold(),
                Text: t.Text ?? string.Empty
            ))
            .ToList();

        var content = string.Concat(runs.Select(r => r.Text));

        if (!string.IsNullOrWhiteSpace(content))
        {
            var nonEmptyRuns = runs
                .Where(r => !string.IsNullOrWhiteSpace(r.Text))
                .ToList();

            if (nonEmptyRuns.Count > 0)
            {
                bool allItalic = nonEmptyRuns.All(r => r.IsItalic);
                bool allBold = nonEmptyRuns.All(r => r.IsBold);

                if (allItalic || allBold)
                {
                    content = ApplyWikiMarkup(content, allBold, allItalic, disableMarkup);
                }
                else
                {
                    content = string.Concat(
                        runs.Select(r => ApplyWikiMarkup(r.Text, r.IsBold, r.IsItalic, disableMarkup))
                    );
                }
            }
        }

        return (bullet + content)
            .Replace("\u00A0", " ")
            .Replace("’", "'")
            .Replace("-", "\u2011")
            .Replace(" ?", "\u00A0?")
            .Replace(" !", "\u00A0!")
            .Replace(" :", "\u00A0:")
            .Replace(" ;", "\u00A0;");
    }

    private static string ApplyWikiMarkup(string text, bool bold, bool italic, bool disableMarkup)
    {
        if (disableMarkup)
        {
            return text;
        }

        if (string.IsNullOrEmpty(text))
        {
            return text;
        }

        if (bold && italic)
        {
            return $"'''''{text}'''''";
        }

        if (bold)
        {
            return $"'''{text}'''";
        }

        if (italic)
        {
            return $"''{text}''";
        }

        return text;
    }


    public static bool IsItalic(this Text text)
    {   
        if (text.Parent is Run run && run.RunProperties?.Italic?.Value == true)
        {
            return true;
        }

        if (text.Parent is EndParagraphRunProperties endPr && endPr.Italic?.Value == true)
        {
            return true;
        }

        return false;
    }

    public static bool IsBold(this Text text)
    {
        if (text.Parent is Run run && run.RunProperties?.Bold?.Value == true)
        {
            return true;
        }

        if (text.Parent is EndParagraphRunProperties endPr && endPr.Bold?.Value == true)
        {
            return true;
        }

        return false;
    }

    public static void ForceRtl(string presentationFile)
    {
        // Open the presentation as read-write.
        using PresentationDocument presentationDocument = PresentationDocument.Open(presentationFile, true);
        // Verify that the presentation document exists.
        if (presentationDocument == null)
        {
            throw new ArgumentNullException("presentationDocument");
        }

        // Get the presentation part of the presentation document.
        var presentationPart = presentationDocument.PresentationPart;

        // Verify that the presentation part and presentation exist.
        if (presentationPart == null || presentationPart.Presentation == null)
        {
            return;
        }

        // Get the Presentation object from the presentation part.
        var presentation = presentationPart.Presentation;

        // Verify that the slide ID list exists.
        if (presentation.SlideIdList == null)
        {
            return;
        }

        // Get the collection of slide IDs from the slide ID list.
        DocumentFormat.OpenXml.OpenXmlElementList slideIds = presentation.SlideIdList.ChildElements;


        // If the slide ID is in range...
        for (int i = 0; i < slideIds.Count; i++)
        {
            // Get the specified slide part from the relationship ID.
            SlidePart slidePart = (SlidePart)presentationPart.GetPartById(((DocumentFormat.OpenXml.Presentation.SlideId)slideIds[i]).RelationshipId);

            var table = slidePart.Slide.Descendants<DocumentFormat.OpenXml.Drawing.Table>().First();

            var cells = table.Descendants<DocumentFormat.OpenXml.Drawing.TableCell>();

            if (cells.Count() != 9)
            {
                throw new ArgumentNullException("slidePart");
            }

            var tale = new Tale();

            int j = 0;
            foreach (var cell in cells)
            {
                switch (j)
                {
                    case 5:
                        cell.ForceRtl();
                        break;
                    case 8:
                        cell.ForceRtl(true);
                        break;
                }

                j++;
            }

            slidePart.Slide.Save();
        }

        presentationDocument.Save();

    }

    public static void ForceRtl(this TableCell tableCell, bool forceRightAlignment = false)
    {
        foreach (var paragraph in tableCell.Descendants<Paragraph>())
        {
            var pPr = paragraph.ParagraphProperties;
            if (pPr == null)
            {
                pPr = new ParagraphProperties();
                paragraph.PrependChild(pPr);
            }

            pPr.RightToLeft = true;
            if (forceRightAlignment)
            {
                pPr.Alignment = TextAlignmentTypeValues.Right;
            }
        }
    }
}
