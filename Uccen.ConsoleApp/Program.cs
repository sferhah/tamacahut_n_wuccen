using System.Diagnostics;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Unicode;

//PptxHelper.ForceRtl(GetFullPathWithinProject("uccen.arb.pptx"));
//return;

await GenerateBilingualPdf();

static async Task GenerateJsonFiles()
{
    string[] fileNames = ["uccen.pptx", "uccen.eng.pptx", "uccen.arb.pptx", "uccen.arq.pptx"];   

    foreach (var fileName in fileNames)
    {
        var book = PptxHelper.ExtractJson(PathHelper.GetFullPathWithinProject(@$"Resources\pptx\{fileName}"));
        book.Tales = book.Tales.Where(t => !t.Source.Title?.StartsWith("#ignore", StringComparison.OrdinalIgnoreCase) ?? false).ToList();

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

    if (!ExecuteNpmTest())
    {
        Console.WriteLine("Failed to execute npm test.");
        return;
    }

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
        new { Name = "", IsRtl = false, BodySize = latnSize, LineSpacing = 1.35, Font = latnFont }, // .fra
        new { Name = ".eng", IsRtl = false, BodySize = latnSize, LineSpacing = 1.35, Font = latnFont },
        new { Name = ".arb", IsRtl = true, BodySize = arSize, LineSpacing = arLineSpacing, Font = arFont },
        new { Name = ".arq", IsRtl = true, BodySize = arSize, LineSpacing = arLineSpacing, Font = arFont }
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
                PageMultiple = 4, // booklet
            };

            PdfHelper.GenerateBilingualPdf(args);
        }
    }
}


static bool ExecuteNpmTest()
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
    string arguments = isWindows ? "/c npm run test" : "run test";

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
        Console.WriteLine("Running 'npm run test'...");

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
                Console.WriteLine("\n--- Errors Encountered ---");
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(error);
                Console.ResetColor();
            }
            else
            {
                Console.WriteLine("\nNo errors encountered during the test run.");
                return true;
            }

            Console.WriteLine($"\nProcess exited with code: {process.ExitCode}");
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
