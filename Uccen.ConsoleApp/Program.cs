// See https://aka.ms/new-console-template for more information
using ConsoleApp2;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;
using Uccen.Transliteration;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Unicode;


//using (PresentationDocument doc = PresentationDocument.Open(@"C:\Test\input.pptx", true))
//{
//    if (doc.MainDocumentPart?.Document.Body != null)
//    {
//        var paras = doc.MainDocumentPart.Document.Body.Elements<Paragraph>();

//        foreach (var para in paras)
//        {
//            foreach (var run in para.Elements<Run>())
//            {
//                foreach (var text in run.Elements<Text>())
//                {
//                    if (text.Text.Contains("Chacal"))
//                    {
//                        text.Text = text.Text.Replace("Chacal", "uccen");
//                    }
//                }
//            }
//        }
//    }
//}

//GetAllTextInPPTX(@"C:\Test\Presentation1.pptx");

TransliterationService.GetTransliterations();

var test = TransliterationService.Transliterate("add-is", "Arabic");
var tales = ExtractJson(@"uccen.pptx");

await File.WriteAllTextAsync(GetPath(), JsonSerializer.Serialize(tales, new JsonSerializerOptions
{
    Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
    IgnoreReadOnlyProperties = true,
    WriteIndented = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
}));

Console.WriteLine("Hello, World!");


static void GetAllTextInPPTX(string presentationFile)
{
    // Open the presentation as read-only.
    using PresentationDocument presentationDocument = PresentationDocument.Open(presentationFile, true);
    // Verify that the presentation document exists.
    if (presentationDocument == null)
    {
        throw new ArgumentNullException("presentationDocument");
    }

    // Get the presentation part of the presentation document.
    PresentationPart presentationPart = presentationDocument.PresentationPart;

    // Verify that the presentation part and presentation exist.
    if (presentationPart == null || presentationPart.Presentation == null)
    {
        return;
    }
    // Get the Presentation object from the presentation part.
    Presentation presentation = presentationPart.Presentation;

    // Verify that the slide ID list exists.
    if (presentation.SlideIdList == null)
    {
        return;
    }

    // Get the collection of slide IDs from the slide ID list.
    DocumentFormat.OpenXml.OpenXmlElementList slideIds =
        presentation.SlideIdList.ChildElements;

    // If the slide ID is in range...
    for (int i = 0; i < slideIds.Count; i++)
    {
        // Get the relationship ID of the slide.
        string slidePartRelationshipId = (slideIds[i] as SlideId).RelationshipId;
        // Get the specified slide part from the relationship ID.
        SlidePart slidePart = (SlidePart)presentationPart.GetPartById(slidePartRelationshipId);         

        var table = slidePart.Slide.Descendants<DocumentFormat.OpenXml.Drawing.Table>().First();

        var cells = table.Descendants<DocumentFormat.OpenXml.Drawing.TableCell>();

        if (cells.Count() != 9)
        {
            throw new ArgumentNullException("slidePart");
        }

        int j = 0;
        foreach (var cell in cells)
        {
            if (j % 3 == 0) // first column
            {
                foreach (var paragraph in cell.Descendants<DocumentFormat.OpenXml.Drawing.Paragraph>())
                {
                    var pp = paragraph.ChildElements.First<DocumentFormat.OpenXml.Drawing.ParagraphProperties>();
                    if (pp == null)
                    {
                        pp = new DocumentFormat.OpenXml.Drawing.ParagraphProperties();
                        paragraph.InsertBefore(pp, paragraph.First());
                    }

                    pp.Append(new DocumentFormat.OpenXml.Drawing.RightToLeft());

                    foreach (var text in paragraph.Descendants<DocumentFormat.OpenXml.Drawing.Text>())
                    {
                        text.Text = TransliterationService.Transliterate(text.Text, "Arabic");
                    }
                }
            }

            j++;
        }      
    }
}

static List<KabyleTale> ExtractJson(string presentationFile)
{
    List<KabyleTale> tales = new List<KabyleTale>();

    // Open the presentation as read-only.
    using PresentationDocument presentationDocument = PresentationDocument.Open(presentationFile, false);
    // Verify that the presentation document exists.
    if (presentationDocument == null)
    {
        throw new ArgumentNullException("presentationDocument");
    }

    // Get the presentation part of the presentation document.
    PresentationPart presentationPart = presentationDocument.PresentationPart;

    // Verify that the presentation part and presentation exist.
    if (presentationPart == null || presentationPart.Presentation == null)
    {
        return tales;
    }

    // Get the Presentation object from the presentation part.
    Presentation presentation = presentationPart.Presentation;

    // Verify that the slide ID list exists.
    if (presentation.SlideIdList == null)
    {
        return tales;
    }

    // Get the collection of slide IDs from the slide ID list.
    DocumentFormat.OpenXml.OpenXmlElementList slideIds = presentation.SlideIdList.ChildElements;

    // If the slide ID is in range...
    for (int i = 0; i < slideIds.Count; i++)
    {
        // Get the specified slide part from the relationship ID.
        SlidePart slidePart = (SlidePart)presentationPart.GetPartById(((SlideId)slideIds[i]).RelationshipId);
        
        var table = slidePart.Slide.Descendants<DocumentFormat.OpenXml.Drawing.Table>().First();

        var cells = table.Descendants<DocumentFormat.OpenXml.Drawing.TableCell>();

        if (cells.Count() != 9)
        {
            throw new ArgumentNullException("slidePart");
        }

        var forcast = new KabyleTale();
        tales.Add(forcast);

        int j = 0;
        foreach (var cell in cells)
        {
            switch (j)
            {
                case 3:
                    forcast.Kabyle.Title = string.Join(string.Empty, cell.ParseParagraphs());
                    break;
                case 6:
                    forcast.Kabyle.Paragraphs = cell.ParseParagraphs();
                    break;
                case 5:
                    forcast.French.Title = string.Join(string.Empty, cell.ParseParagraphs());
                    break;
                case 8:
                    forcast.French.Paragraphs = cell.ParseParagraphs();
                    break;
            }

            j++;
        }

        
    }

    return tales;
}

static string GetPath()
{
    string path = Directory.GetParent(System.Reflection.Assembly.GetExecutingAssembly().Location).FullName;

    while (!Directory.GetFiles(path).Any(x => x.EndsWith(".sln")))
    {
        path = Directory.GetParent(path).FullName;
    }

    return Path.Combine(path, @"Uccen.BlazorApp\wwwroot\data\tamacahut_n_wuccen.json");
}

public class KabyleTale
{
    public KabyleTaleContent Kabyle { get; set; } = new KabyleTaleContent();
    public KabyleTaleContent French { get; set; } = new KabyleTaleContent();
}

public class KabyleTaleContent
{
    public string? Title { get; set; }
    public List<string> Paragraphs { get; set; } = new List<string>();    
}