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
        var book = JsonSerializer.Deserialize<Book>(json,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                    ?? new Book();

        var cover = book.Tales.FirstOrDefault();

        if (cover?.Source.Title == "#0")
        {
            options.CoverTitle = cover.Source.Paragraphs.FirstOrDefault();
            options.CoverSubtitle = cover.Translation.Paragraphs.FirstOrDefault();
            //options.CoverSvgPath = book.CoverSvgImage ?? File.ReadAllText(PathHelper.GetFullPathWithinProject(@"Resources\cover.svg"));
            options.CoverSvgPath = PathHelper.GetFullPathWithinProject(@"Resources\images\cover.svg");

            book.Tales.RemoveAt(0);
        }

        new FacingPagesRenderer(options).Render(book, args[1]);
    }
}