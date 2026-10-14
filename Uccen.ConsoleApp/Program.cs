// See https://aka.ms/new-console-template for more information
using ConsoleApp2;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Unicode;


var tales = ExtractJson(@"uccen.pptx");

await File.WriteAllTextAsync(GetPath(), JsonSerializer.Serialize(tales, new JsonSerializerOptions
{
    Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
    IgnoreReadOnlyProperties = true,
    WriteIndented = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
}));

Console.WriteLine("Hello, World!");

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

    KabyleTale? previousTale = null;
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

        var tale = new KabyleTale();        

        int j = 0;
        foreach (var cell in cells)
        {
            switch (j)
            {
                case 3:
                    tale.Kabyle.Title = string.Join(string.Empty, cell.ParseParagraphs());
                    break;
                case 6:
                    tale.Kabyle.Paragraphs = cell.ParseParagraphs();
                    break;
                case 5:
                    tale.French.Title = string.Join(string.Empty, cell.ParseParagraphs());
                    break;
                case 8:
                    tale.French.Paragraphs = cell.ParseParagraphs();
                    break;
            }

            j++;
        }

        if (string.IsNullOrWhiteSpace(tale.Kabyle.Title) 
            && string.IsNullOrWhiteSpace(tale.French.Title)
            && previousTale != null)
        {
            previousTale.Kabyle.Paragraphs.AddRange(tale.Kabyle.Paragraphs);
            previousTale.French.Paragraphs.AddRange(tale.French.Paragraphs);
        }
        else
        {
            tales.Add(tale);
            previousTale = tale;
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

    return Path.Combine(path, @"Uccen.SvelteApp\public\data\tamacahut_n_wuccen.json");
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