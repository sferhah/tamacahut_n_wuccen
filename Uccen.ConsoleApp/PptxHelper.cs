using System.Text;

namespace ConsoleApp2;

public static class PptxHelper
{
    public static bool StartsWithPilcrow(this DocumentFormat.OpenXml.Drawing.TableCell pptxCell)
        => pptxCell.Descendants<DocumentFormat.OpenXml.Drawing.Paragraph>().FirstOrDefault()?
        .Descendants<DocumentFormat.OpenXml.Drawing.Text>().FirstOrDefault()?.Text.TrimStart().StartsWith('¶') ?? false;

    public static List<string> ParseParagraphs(this DocumentFormat.OpenXml.Drawing.TableCell pptxCell)
        => pptxCell.Descendants<DocumentFormat.OpenXml.Drawing.Paragraph>().ParseParagraphs();   

    public static List<string> ParseParagraphs(this IEnumerable<DocumentFormat.OpenXml.Drawing.Paragraph> pptxParagraphs)
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

    public static string ParseLine(this DocumentFormat.OpenXml.Drawing.Paragraph pptxParagraph)
    {
        StringBuilder builder = new StringBuilder();
        foreach (var text in pptxParagraph.Descendants<DocumentFormat.OpenXml.Drawing.Text>())
        {
            builder.Append(text.Text.Replace("\u00A0", " ").Replace("’", "'").Replace("-", "\u2011").Replace(" ?", "\u00A0?").Replace(" !", "\u00A0!").Replace(" :", "\u00A0:"));
        }
        return builder.ToString();
    }
}
