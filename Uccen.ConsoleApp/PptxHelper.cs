using System.Text;

namespace ConsoleApp2;
public static class PptxHelper
{
    public static List<string> ParseParagraphs(this DocumentFormat.OpenXml.Drawing.TableCell pptxCell)
        => pptxCell.Descendants<DocumentFormat.OpenXml.Drawing.Paragraph>().ParseParagraphs();

    public static List<string> ParseParagraphs(this IEnumerable<DocumentFormat.OpenXml.Drawing.Paragraph> pptxParagraphs)
    {
        var lines = pptxParagraphs.Select(x => x.ParseLine()).ToList();

        var paragraphs = new List<List<string>>();
        List<string>? paragraph = null;

        foreach (string v in lines.Select(x => x.Trim()))
        {
            if (paragraph == null)
            {
                paragraphs.Add(paragraph = new List<string>());
            }

            if (string.IsNullOrWhiteSpace(v))
            {
                paragraph = null;
            }
            else
            {
                paragraph.Add(v);
            }
        }

        return paragraphs.Where(x => x.Any()).Select(x=> string.Join("\n", x)).ToList();
    }

    public static string ParseLine(this DocumentFormat.OpenXml.Drawing.Paragraph pptxParagraph)
    {
        StringBuilder builder = new StringBuilder();
        foreach (var text in pptxParagraph.Descendants<DocumentFormat.OpenXml.Drawing.Text>())
        {
            builder.Append(text.Text.Replace("\u00A0", " ").Replace("’", "'"));
        }
        return builder.ToString();
    }
}
