// See https://aka.ms/new-console-template for more information
using ConsoleApp2;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Unicode;



//ForceRtl(GetFullPathWithinProject("uccen.arb.pptx"));
//return;

await GenerateJsonFiles();

//GenerateBilingualPdf();

static async Task GenerateJsonFiles()
{
    string[] fileNames = ["uccen.pptx", "uccen.eng.pptx", "uccen.arb.pptx", "uccen.arq.pptx"];

    foreach (var fileName in fileNames)
    {
        var tales = ExtractJson(fileName);
        var languageCode = GetLanguageCode(fileName);

        await File.WriteAllTextAsync(GetPath(languageCode), JsonSerializer.Serialize(tales, new JsonSerializerOptions
        {
            Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
            IgnoreReadOnlyProperties = true,
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        }));

        Console.WriteLine($"JSON files generated successfully for: {fileName}");
    }
}

static void ForceRtl(string presentationFile)
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
    Presentation presentation = presentationPart.Presentation;

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
        SlidePart slidePart = (SlidePart)presentationPart.GetPartById(((SlideId)slideIds[i]).RelationshipId);

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

static List<Tale> ExtractJson(string presentationFile)
{
    List<Tale> tales = new List<Tale>();

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

    Tale? previousTale = null;
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

        var tale = new Tale();
        bool sourceStartsWithPilcrow = false;
        bool translationStartsWithPilcrow = false;

        int j = 0;
        foreach (var cell in cells)
        {
            switch (j)
            {
                case 3:
                    tale.Source.Title = string.Join(string.Empty, cell.ParseParagraphs());
                    break;
                case 6:
                    tale.Source.Paragraphs = cell.ParseParagraphs();
                    sourceStartsWithPilcrow = cell.StartsWithPilcrow();
                    break;
                case 5:
                    tale.Translation.Title = string.Join(string.Empty, cell.ParseParagraphs());                    
                    break;
                case 8:
                    tale.Translation.Paragraphs = cell.ParseParagraphs();
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
            tales.Add(tale);
            previousTale = tale;
        }
    }

    return tales;
}


static string GetLanguageCode(string fileName)
{
    var parts = fileName.Split('.');
    return parts.Length > 2 ? "." + parts[1] : string.Empty;
}

static string GetPath(string languageCode = "")
{
    string path = Directory.GetParent(System.Reflection.Assembly.GetExecutingAssembly().Location).FullName;

    while (!Directory.GetFiles(path).Any(x => x.EndsWith(".sln")))
    {
        path = Directory.GetParent(path).FullName;
    }

    return Path.Combine(path, $@"Uccen.SvelteApp\public\data\tamacahut_n_wuccen{languageCode}.json");
}

static string GetFullPathWithinProject(string filename)
{
    string path = Directory.GetParent(System.Reflection.Assembly.GetExecutingAssembly().Location).FullName;

    while (!Directory.GetFiles(path).Any(x => x.EndsWith(".csproj")))
    {
        path = Directory.GetParent(path).FullName;
    }

    return Path.Combine(path, filename);
}

static string GetOutputPath()
{
    string path = Directory.GetParent(System.Reflection.Assembly.GetExecutingAssembly().Location).FullName;

    while (!Directory.GetFiles(path).Any(x => x.EndsWith(".sln")))
    {
        path = Directory.GetParent(path).FullName;
    }

    return Path.Combine(path, "Uccen.ConsoleApp", "tamacahut_n_wuccen.pdf");
}

static void GenerateBilingualPdf()
{
    QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
    QuestPDF.Settings.UseSystemFonts = true;

    // Read JSON file
    string jsonPath = GetFullPathWithinProject("tamacahut_n_wuccen.json");
    string jsonContent = File.ReadAllText(jsonPath);
    var tales = JsonSerializer.Deserialize<List<Tale>>(jsonContent) ?? new List<Tale>();

    if (tales.Count == 0)
    {
        Console.WriteLine("No tales found in JSON file.");
        return;
    }

    // Generate PDF
    var pdfPath = GetOutputPath();
    Document.Create(container =>
    {
        container.Page(page =>
        {
            // A5 page size: 148mm x 210mm
            page.Size(148, 210, Unit.Millimetre);
            page.Margin(20); // uniform margin in mm (2cm = 20mm)
            page.MarginTop(20);
            page.MarginBottom(20);
            page.MarginLeft(20);
            page.MarginRight(15);
            page.DefaultTextStyle(x => x.FontSize(11).LineHeight(1.5f));

            page.Content().Column(column =>
            {
                for (int i = 0; i < tales.Count; i++)
                {
                    var tale = tales[i];

                    // Render Source content
                    column.Item().RenderTaleContent(tale.Source);

                    // Page break after source
                    column.Item().PageBreak();

                    // Render Translation content
                    column.Item().RenderTaleContent(tale.Translation);

                    // Page break after translation (unless it's the last tale)
                    if (i < tales.Count - 1)
                    {
                        column.Item().PageBreak();
                    }
                }
            });
        });
    }).GeneratePdf(pdfPath);

    Console.WriteLine($"PDF generated successfully: {pdfPath}");
}

static class TaleContentExtensions
{
    public static void RenderTaleContent(this IContainer container, TaleContent content)
    {
        container.Column(column =>
        {
            // Render title
            if (!string.IsNullOrWhiteSpace(content.Title))
            {
                column.Item().Text(content.Title)
                    .FontSize(16)
                    .Bold();
            }

            // Render paragraphs
            foreach (var paragraph in content.Paragraphs)
            {
                if (!string.IsNullOrWhiteSpace(paragraph))
                {
                    column.Item().Text(paragraph)
                        .FontSize(11)
                        .LineHeight(1.5f);
                }
            }
        });
    }
}

public class Tale
{
    public TaleContent Source { get; set; } = new TaleContent();
    public TaleContent Translation { get; set; } = new TaleContent();
}

public class TaleContent
{   
    public string? Title { get; set; }
    public List<string> Paragraphs { get; set; } = new List<string>();    
}