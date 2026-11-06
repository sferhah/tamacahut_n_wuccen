using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Unicode;


//PptxHelper.ForceRtl(GetFullPathWithinProject("uccen.arb.pptx"));
//return;

//await GenerateJsonFiles();
GenerateBilingualPdf();

static async Task GenerateJsonFiles()
{
    string[] fileNames = ["uccen.pptx", "uccen.eng.pptx", "uccen.arb.pptx", "uccen.arq.pptx"];

    foreach (var fileName in fileNames)
    {
        var tales = PptxHelper.ExtractJson(GetFullPathWithinProject($"Resources\\pptx\\{fileName}"));
        var languageCode = GetLanguageCode(fileName);

        await File.WriteAllTextAsync(GetSvelteDataPath(languageCode), JsonSerializer.Serialize(tales, new JsonSerializerOptions
        {
            Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
            IgnoreReadOnlyProperties = true,
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        }));

        Console.WriteLine($"JSON files generated successfully for: {fileName}");
    }
}

static void GenerateBilingualPdf()
{   
    var languageCode = ".arq";

    var args = new string[]
    {
         GetSvelteDataPath(languageCode),
         Path.ChangeExtension(GetSvelteDataPath(languageCode), "pdf"),         
         "Consolas",          // source
         GetFullPathWithinProject(@"Resources\fonts\NotoNaskhArabic-VariableFont_wght.ttf"), // translation
    };

    PdfHelper.GenerateBilingualPdf(args);
}

static string GetLanguageCode(string fileName)
{
    var parts = fileName.Split('.');
    return parts.Length > 2 ? "." + parts[1] : string.Empty;
}

static string GetSvelteDataPath(string languageCode = "")
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
