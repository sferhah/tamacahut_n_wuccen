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
        var tales = PptxHelper.ExtractJson(GetFullPathWithinProject(@$"Resources\pptx\{fileName}"));
        var languageCode = GetLanguageCode(fileName);
        var svelteDataPath = GetSvelteDataPath(languageCode);

        await File.WriteAllTextAsync(svelteDataPath, JsonSerializer.Serialize(tales, new JsonSerializerOptions
        {
            Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
            IgnoreReadOnlyProperties = true,
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        }));

        File.Copy(svelteDataPath, GetFullPathWithinProject(@$"Resources\json\tamacahut_n_wuccen{languageCode}.json"), true);

        Console.WriteLine($"JSON files generated successfully for: {fileName}");
    }
}

static async Task GenerateBilingualPdf()
{
    await GenerateJsonFiles();
    return;

    if (!ExecuteNpmTest())
    {
        Console.WriteLine("Failed to execute npm test.");
        return;
    }

    var languages = new[]
    {
        new { Name = "", Font = "Consolas" }, // .fra
        new { Name = ".eng", Font = "Consolas" },
        new { Name = ".arb", Font = GetFullPathWithinProject(@"Resources\fonts\NotoNaskhArabic-VariableFont_wght.ttf") },
        new { Name = ".arq", Font = GetFullPathWithinProject(@"Resources\fonts\NotoNaskhArabic-VariableFont_wght.ttf") }
    }.ToList();

    var scripts = new[]
    {
        new { Name = "", Font = "Consolas" },
        new { Name = "_tfng", Font = GetFullPathWithinProject(@"Resources\fonts\NotoNaskhArabic-VariableFont_wght.ttf") },
        new { Name = "_arab", Font = GetFullPathWithinProject(@"Resources\fonts\NotoNaskhArabic-VariableFont_wght.ttf") },
        /*new { Name = "_hebr", Font = GetFullPathWithinProject(@"Resources\fonts\NotoNaskhArabic-VariableFont_wght.ttf") },
        new { Name = "_syrc", Font = GetFullPathWithinProject(@"Resources\fonts\NotoNaskhArabic-VariableFont_wght.ttf") },
        new { Name = "_armi", Font = GetFullPathWithinProject(@"Resources\fonts\NotoNaskhArabic-VariableFont_wght.ttf") },
        new { Name = "_phnx", Font = GetFullPathWithinProject(@"Resources\fonts\NotoNaskhArabic-VariableFont_wght.ttf") },
        new { Name = "_nbat", Font = GetFullPathWithinProject(@"Resources\fonts\NotoNaskhArabic-VariableFont_wght.ttf") },
        new { Name = "_sarb", Font = GetFullPathWithinProject(@"Resources\fonts\NotoNaskhArabic-VariableFont_wght.ttf") },
        new { Name = "_cyrl", Font = GetFullPathWithinProject(@"Resources\fonts\NotoNaskhArabic-VariableFont_wght.ttf") },
        new { Name = "_grek", Font = GetFullPathWithinProject(@"Resources\fonts\NotoNaskhArabic-VariableFont_wght.ttf") },*/
    }.ToList();

    foreach(var language in languages)
    {
        foreach (var script in scripts)
        {   
            var jsonFile = GetFullPathWithinProject(@$"Resources\json\tamacahut_n_wuccen{script.Name}{language.Name}.json");
            var pdfFile = GetFullPathWithinProject(@$"Resources\pdf\tamacahut_n_wuccen{script.Name}{language.Name}.pdf");

            var args = new string[]
            {
                 jsonFile,
                 pdfFile,
                 script.Font,
                 language.Font,
            };

            PdfHelper.GenerateBilingualPdf(args);
        }
    }
}


static bool ExecuteNpmTest()
{   
    string projectDirectory = GetSveltePath();

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

static string GetSvelteDataPath(string languageCode)
{
    return Path.Combine(GetSveltePath(), $@"public\data\tamacahut_n_wuccen{languageCode}.json");
}

static string GetSveltePath()
{
    string path = Directory.GetParent(System.Reflection.Assembly.GetExecutingAssembly().Location).FullName;

    while (!Directory.GetFiles(path).Any(x => x.EndsWith(".sln")))
    {
        path = Directory.GetParent(path).FullName;
    }

    return Path.Combine(path, $@"Uccen.SvelteApp");
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
