using System.Diagnostics;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Unicode;

//PptxHelper.ForceRtl(GetFullPathWithinProject("uccen.arb.pptx"));
//return;

TaleXt.Date = new DateTime(2026, 11, 26);

await GenerateBilingualPdf();

static async Task GenerateJsonFiles()
{
    string[] fileNames = ["uccen.pptx", "uccen.eng.pptx", "uccen.arb.pptx", "uccen.arq.pptx"];

    Book? previousBook = null;

    foreach (var fileName in fileNames)
    {
        var book = PptxHelper.ExtractJson(PathHelper.GetFullPathWithinProject(@$"Resources\pptx\{fileName}"));

        if (previousBook != null)
        {
            for (int i = 0; i < book.Tales.Count; i++)
            {
                var tale = book.Tales[i];
                var previousTale = previousBook.Tales[i];

                if (tale.Source.Title != previousTale.Source.Title)
                {
                    throw new Exception();
                }

                for (int j = 0; j < tale.Source.Paragraphs.Count; j++)
                {
                    if (tale.Source.Paragraphs[j] != previousTale.Source.Paragraphs[j])
                    {
                        throw new Exception($"Paragraph mismatch in tale \n{tale.Source.Paragraphs[j]}\n!=\n{previousTale.Source.Paragraphs[j]}");
                    }
                }
            }
        }

        previousBook = book;

        var languageCode = GetLanguageCode(fileName);
        var svelteDataPath = PathHelper.GetSvelteDataPath(languageCode);

        await File.WriteAllTextAsync(svelteDataPath, JsonSerializer.Serialize(book, new JsonSerializerOptions
        {
            Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
            IgnoreReadOnlyProperties = true,
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        }));

        File.Copy(svelteDataPath, PathHelper.GetFullPathWithinProject(@$"Resources\json\tamacahut_n_wuccen{languageCode}.json"), true);

        Console.WriteLine($"JSON files generated successfully for: {fileName}");
    }
}

static async Task GenerateBilingualPdf()
{
    await GenerateJsonFiles();
    //return;

    // Check the transliteration first, then generate the transliterated JSON files and the mapping table.
    if (!RunNpmScript("test") || !RunNpmScript("generate"))
    {
        return;
    }

    UpdateReadMe();    

    var pdfFolder = PathHelper.GetFullPathWithinProject(@$"Resources\pdf");
    var di = new DirectoryInfo(pdfFolder);

    foreach (FileInfo file in di.GetFiles())
    {
        file.Delete();
    }

    var arSize = 14.0;
    var arLineSpacing = 1.0;
    var arFont = PathHelper.GetFullPathWithinProject(@"Resources\fonts\NotoNaskhArabic-Regular.ttf")
        + "|"
        + PathHelper.GetFullPathWithinProject(@"Resources\fonts\NotoNaskhArabic-Bold.ttf");
    var arBackCoverSize = 13.0;
    var arBackCoverLineSpacing = 0.9;

    var latnSize = 11.5; // "Noto Sans"
    var latnFont = "Cambria"; // "Noto Sans"

    var scripts = new[]
    {
        new { Name = "", BodySize = latnSize, LineSpacing = 1.35, Font = latnFont },
        new { Name = "_tfng", BodySize = 10.5, LineSpacing = 1.35, Font = "Aptos" },
        new { Name = "_arab", BodySize = arSize, LineSpacing = arLineSpacing, Font = arFont },
        /*new { Name = "_hebr", BodySize = 10.5, LineSpacing = 1.35, Font = PathHelper.GetFullPathWithinProject(@"Resources\fonts\NotoSansHebrew-VariableFont_wdth,wght.ttf") },
        new { Name = "_syrc", BodySize = 10.5, LineSpacing = 1.35, Font = PathHelper.GetFullPathWithinProject(@"Resources\fonts\NotoNaskhArabic-VariableFont_wght.ttf") },
        new { Name = "_armi", BodySize = 10.5, LineSpacing = 1.35, Font = PathHelper.GetFullPathWithinProject(@"Resources\fonts\NotoNaskhArabic-VariableFont_wght.ttf") },
        new { Name = "_phnx", BodySize = 10.5, LineSpacing = 1.35, Font = PathHelper.GetFullPathWithinProject(@"Resources\fonts\NotoNaskhArabic-VariableFont_wght.ttf") },
        new { Name = "_nbat", BodySize = 10.5, LineSpacing = 1.35, Font = PathHelper.GetFullPathWithinProject(@"Resources\fonts\NotoNaskhArabic-VariableFont_wght.ttf") },
        new { Name = "_sarb", BodySize = 10.5, LineSpacing = 1.35, Font = PathHelper.GetFullPathWithinProject(@"Resources\fonts\NotoNaskhArabic-VariableFont_wght.ttf") },
        new { Name = "_cyrl", BodySize = 10.5, LineSpacing = 1.35, Font = PathHelper.GetFullPathWithinProject(@"Resources\fonts\NotoNaskhArabic-VariableFont_wght.ttf") },
        new { Name = "_grek", BodySize = 10.5, LineSpacing = 1.35, Font = PathHelper.GetFullPathWithinProject(@"Resources\fonts\NotoNaskhArabic-VariableFont_wght.ttf") },*/
    }.ToList();

    var translations = new[]
    {
        new { Name = "", IsRtl = false, BodySize = latnSize, LineSpacing = 1.35, Font = latnFont, BackCoverSize = 11.5, BackCoverLineSpacing = 1.35 }, // .fra
        new { Name = ".eng", IsRtl = false, BodySize = latnSize, LineSpacing = 1.35, Font = latnFont, BackCoverSize = 11.5, BackCoverLineSpacing = 1.35 },
        new { Name = ".arb", IsRtl = true, BodySize = arSize, LineSpacing = arLineSpacing, Font = arFont, BackCoverSize = arBackCoverSize, BackCoverLineSpacing = arBackCoverLineSpacing },
        new { Name = ".arq", IsRtl = true, BodySize = arSize, LineSpacing = arLineSpacing, Font = arFont, BackCoverSize = arBackCoverSize, BackCoverLineSpacing = arBackCoverLineSpacing }
    }.ToList();

    foreach (var translation in translations)
    {
        foreach (var script in scripts)
        {   
            var jsonFilePath = PathHelper.GetFullPathWithinProject(@$"Resources\json\tamacahut_n_wuccen{script.Name}{translation.Name}.json");
            var pdfFilePath = PathHelper.GetFullPathWithinProject(@$"Resources\pdf\tamacahut_n_wuccen{script.Name}{translation.Name}.pdf");

            var args = new PdfHelperArgs
            {
                JsonFilePath = jsonFilePath,
                PdfFilePath = pdfFilePath,
                SourceFont = script.Font,
                LanguageFont = translation.Font,
                RightToLeftBook = translation.IsRtl,
                SourceBodySize = script.BodySize,
                TranslationBodySize = translation.BodySize,
                SourceLineSpacing = script.LineSpacing,
                TranslationLineSpacing = translation.LineSpacing,

                BackCoverTranslationSize = translation.BackCoverSize,
                BackCoverTranslationLineSpacing = translation.BackCoverLineSpacing,

                PageMultiple = 4, // booklet
            };

            PdfHelper.GenerateBilingualPdf(args);
        }
    }
}

static void UpdateReadMe()
{
    var jsonMappingFilePath = PathHelper.GetFullPathWithinProject(@$"Resources\json\transliteration_table.json");
    var rawRows = JsonSerializer.Deserialize<List<List<string>>>(File.ReadAllText(jsonMappingFilePath),
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                ?? new List<List<string>>();

    string table = ToMarkdownTable(rawRows);

    string readmePath = Path.Combine(PathHelper.GetSlnPath(), @"README.md");

    string readme = File.ReadAllText(readmePath, Encoding.UTF8);   

    string startTag = "<!-- TABLE_START -->";
    string endTag = "<!-- TABLE_END -->";

    int startIndex = readme.IndexOf(startTag, StringComparison.Ordinal);
    int endIndex = readme.IndexOf(endTag, StringComparison.Ordinal);

    if (startIndex != -1 && endIndex != -1)
    {
        int replaceStart = startIndex + startTag.Length;
        string updatedReadme = readme
            .Remove(replaceStart, endIndex - replaceStart)
            .Insert(replaceStart, Environment.NewLine + table);

        File.WriteAllText(readmePath, updatedReadme, Encoding.UTF8);
    }
}

static string ToMarkdownTable(List<List<string>> rawRows)
{
    if (rawRows == null || rawRows.Count == 0)
        return string.Empty;

    var sb = new StringBuilder();

    // 1. Header row
    sb.AppendLine("| " + string.Join(" | ", rawRows[0]) + " |");

    // 2. Separator row
    sb.AppendLine("| " + string.Join(" | ", rawRows[0].Select(_ => ":---")) + " |");

    // 3. Data rows
    foreach (var row in rawRows.Skip(1))
    {
        sb.AppendLine("| " + string.Join(" | ", row) + " |");
    }

    return sb.ToString();
}


/// <summary>Runs `npm run &lt;scriptName&gt;` in the Svelte project; true when it exits with code 0.</summary>
static bool RunNpmScript(string scriptName)
{
    string projectDirectory = PathHelper.GetSveltePath();

    if (!Directory.Exists(projectDirectory))
    {
        Console.WriteLine("The specified directory does not exist.");
        return false;
    }

    // 2. Adjust the command depending on the Operating System
    bool isWindows = OperatingSystem.IsWindows();
    string fileName = isWindows ? "cmd.exe" : "npm";
    string arguments = isWindows ? $"/c npm run {scriptName}" : $"run {scriptName}";

    // 3. Configure the process settings
    ProcessStartInfo startInfo = new ProcessStartInfo
    {
        FileName = fileName,
        Arguments = arguments,
        WorkingDirectory = projectDirectory, // Directs npm to execute inside this folder
        RedirectStandardOutput = true,      // Captures the test results text
        RedirectStandardError = true,       // Captures any error messages
        UseShellExecute = false,            // Required to redirect streams
        CreateNoWindow = true               // Hides the command prompt window
    };

    try
    {
        Console.WriteLine($"Running 'npm run {scriptName}'...");

        // 4. Start the process
        using (Process process = Process.Start(startInfo))
        {
            if (process == null)
            {
                Console.WriteLine("Failed to start the process.");
                return false;
            }

            // Read the output streams
            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();

            process.WaitForExit();

            // 5. Display the results in the C# console
            Console.WriteLine("\n--- NPM Output ---");
            Console.WriteLine(output);

            if (!string.IsNullOrEmpty(error))
            {
                Console.WriteLine("\n--- NPM Errors ---");
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(error);
                Console.ResetColor();
            }

            if (process.ExitCode == 0)
            {
                Console.WriteLine($"\n'npm run {scriptName}' succeeded.");
                return true;
            }

            Console.WriteLine($"\n'npm run {scriptName}' failed with exit code: {process.ExitCode}");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"An error occurred: {ex.Message}");        
    }

    return false;
}



static string GetLanguageCode(string fileName)
{
    var parts = fileName.Split('.');
    return parts.Length > 2 ? "." + parts[1] : string.Empty;
}
