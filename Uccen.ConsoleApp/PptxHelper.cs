using DocumentFormat.OpenXml.Drawing;
using System.Text;

namespace ConsoleApp2;

public static class PptxHelper
{
    public static bool StartsWithPilcrow(this TableCell pptxCell)
        => pptxCell.Descendants<Paragraph>().FirstOrDefault()?
        .Descendants<Text>().FirstOrDefault()?.Text.TrimStart().StartsWith('¶') ?? false;

    public static List<string> ParseParagraphs(this TableCell pptxCell)
        => pptxCell.Descendants<Paragraph>().ParseParagraphs();   

    public static List<string> ParseParagraphs(this IEnumerable<Paragraph> pptxParagraphs)
    {
        var paragraphs = new List<List<string>>();
        List<string>? currentParagraph = null;

        foreach (var pptxParagraph in pptxParagraphs)
        {
            // Retrieve the text and immediately split it at the ¶ character.
            var rawLine = pptxParagraph.ParseLine();
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

    public static string ParseLine(this Paragraph pptxParagraph)
    {
        StringBuilder builder = new StringBuilder();
        List<bool> italics = new List<bool>();        

        foreach (var text in pptxParagraph.Descendants<Text>())
        {
            if (!string.IsNullOrWhiteSpace(text.Text))
            {
                italics.Add(text.IsItalic());
            }

            builder.Append(text.Text.Replace("\u00A0", " ").Replace("’", "'")
                .Replace("-", "\u2011")
                .Replace(" ?", "\u00A0?")
                .Replace(" !", "\u00A0!")
                .Replace(" :", "\u00A0:")
                .Replace(" ;", "\u00A0;"));
        }

        var res = builder.ToString();

        return italics.All(x => x) && res.Length > 0 ? $"''{res}''" : res;
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
}
