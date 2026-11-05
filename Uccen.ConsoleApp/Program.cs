using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Unicode;


//PptxHelper.ForceRtl(GetFullPathWithinProject("uccen.arb.pptx"));
//return;

await GenerateJsonFiles();

args = new string[]
{
     @"C:\Users\user\source\repos\tamacahut_n_wuccen\Uccen.SvelteApp\public\data\tamacahut_n_wuccen.json",
     @"C:\Users\user\source\repos\tamacahut_n_wuccen\Uccen.SvelteApp\public\data\tamacahut_n_wuccen.pdf",
     "Calibri",          // source (Kabyle) font
     "Sakkal Majalla",   // translation (Arabic) font
};


args = new string[]
{
     @"C:\Users\user\source\repos\tamacahut_n_wuccen\Uccen.SvelteApp\public\data\tamacahut_n_wuccen.arq.json",
     @"C:\Users\user\source\repos\tamacahut_n_wuccen\Uccen.SvelteApp\public\data\tamacahut_n_wuccen.arq.pdf",
     "Calibri",          // source (Kabyle) font
     @"C:\Users\user\source\repos\tamacahut_n_wuccen\Uccen.ConsoleApp\Resources\fonts\NotoNaskhArabic-VariableFont_wght.ttf",   // translation (Arabic) font
};

//PdfHelper.GenerateBilingualPdf(args);

static async Task GenerateJsonFiles()
{
    string[] fileNames = ["uccen.pptx", "uccen.eng.pptx", "uccen.arb.pptx", "uccen.arq.pptx"];

    foreach (var fileName in fileNames)
    {
        var tales = PptxHelper.ExtractJson(GetFullPathWithinProject($"Resources\\pptx\\{fileName}"));
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

static async Task GenerateBilingualPdf()
{
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
