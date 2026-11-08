using System.Text.Json;
using Topten.RichTextKit;


// Each font argument is an installed family name ("Calibri") or a path
// ("C:\Fonts\MyFont.ttf" or "C:\Fonts\MyFont.ttf|C:\Fonts\MyFont-Bold.ttf").
public class PdfHelperArgs
{
    required public string JsonFilePath { get; set; }
    required public string PdfFilePath { get; set; }
    required public string SourceFont { get; set; }
    required public string LanguageFont { get; set; }
    public double SourceBodySize { get; set; } = 10.5;
    public double TranslationBodySize { get; set; } = 10.5;
    public bool RightToLeftBook { get; set; }
    public double SourceLineSpacing { get; set; } = 1.35;
    public double TranslationLineSpacing { get; set; } = 1.35;
}

public static class PdfHelper
{
    public static void GenerateBilingualPdf(PdfHelperArgs args)
    {

        var mapper = new FileFontMapper();
        FontMapper.Default = mapper;

        string Font(string arg) => FileFontMapper.IsPath(arg) ? mapper.Register(arg) : arg;

        var options = new LayoutOptions
        {
            SourceFont = Font(args.SourceFont),
            TranslationFont = Font(args.LanguageFont),
            RightToLeftBook = args.RightToLeftBook,
            SourceBodySize = args.SourceBodySize,
            TranslationBodySize = args.TranslationBodySize,
            SourceLineSpacing = args.SourceLineSpacing,
            TranslationLineSpacing = args.TranslationLineSpacing,
        };

        var json = File.ReadAllText(args.JsonFilePath);
        var book = JsonSerializer.Deserialize<Book>(json,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                    ?? new Book();

        var cover = book.Tales.FirstOrDefault();

        if (cover?.Source.Title == "#0")
        {
            (string? sourceAuthor, string? sourceTitle) = GetAuthorAndTitle(cover.Source.Paragraphs);
            (string? translationAuthor, string? translationTitle) = GetAuthorAndTitle(cover.Translation.Paragraphs);

            options.CoverAuthor = translationAuthor;

            options.CoverTitle = sourceTitle;
            options.CoverSubtitle = translationTitle;

            options.CoverSvgPath = PathHelper.GetSvelteImagePath(@"0");            

            book.Tales.RemoveAt(0);
        }


        foreach (var tale in book.Tales.Where(t => t.Source.Title?.StartsWith("#") ?? false))
        {
            var fileName = tale.Source.Title!.Skip(1).FirstOrDefault().ToString();
            tale.IllustrationSvgPath = PathHelper.GetSvelteImagePath(fileName);
        }

        new FacingPagesRenderer(options).Render(book, args.PdfFilePath).PatchWithPdfSharp(args.PdfFilePath);        
    }

    public static (string? author, string? title) GetAuthorAndTitle(List<string> paragraphs)
    {
        return (author: paragraphs.FirstOrDefault(), title: paragraphs.Skip(1).FirstOrDefault());
    }
}