using System.Text.Json;
using Topten.RichTextKit;

public static class PdfHelper
{
    public static void GenerateBilingualPdf(string[] args)
    {
        if (args.Length < 2)
        {
            Console.Error.WriteLine(
                "Usage: FacingPagesSkia <tales.json> <output.pdf> [\"Source font\"] [\"Translation font\"]");
            return;
        }

        // Each font argument is an installed family name ("Calibri") or a path
        // ("C:\Fonts\MyFont.ttf" or "C:\Fonts\MyFont.ttf|C:\Fonts\MyFont-Bold.ttf").
        var mapper = new FileFontMapper();
        FontMapper.Default = mapper;

        string Font(string arg) => FileFontMapper.IsPath(arg) ? mapper.Register(arg) : arg;

        var options = new LayoutOptions();
        if (args.Length > 2) options.SourceFont = Font(args[2]);
        if (args.Length > 3) options.TranslationFont = Font(args[3]);

        var json = File.ReadAllText(args[0]);
        var tales = JsonSerializer.Deserialize<List<Tale>>(json,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                    ?? new List<Tale>();

        new FacingPagesRenderer(options).Render(tales, args[1]);        
    }
}