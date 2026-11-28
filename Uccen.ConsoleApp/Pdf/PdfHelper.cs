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
    public double BackCoverTranslationSize { get; set; } = 11.5;
    public double BackCoverTranslationLineSpacing { get; set; } = 1.35;
    public double ImageCaptionTextSize { get; set; } = 6.0;
    public bool RightToLeftBook { get; set; }
    public double SourceLineSpacing { get; set; } = 1.35;
    public double TranslationLineSpacing { get; set; } = 1.35;
    public int PageMultiple { get; set; } = 0;
}

public static class PdfHelper
{
    public static void GenerateBilingualPdf(PdfHelperArgs args)
    {
        // One mapper per document (not the global FontMapper.Default), so PDFs can be generated in parallel.
        var mapper = new FileFontMapper();

        string Font(string arg) => FileFontMapper.IsPath(arg) ? mapper.Register(arg) : arg;

        var options = new LayoutOptions
        {
            FontMapper = mapper,
            SourceFont = Font(args.SourceFont),
            TranslationFont = Font(args.LanguageFont),
            RightToLeftBook = args.RightToLeftBook,
            SourceBodySize = args.SourceBodySize,
            TranslationBodySize = args.TranslationBodySize,
            BackCoverTranslationSize = args.BackCoverTranslationSize,
            BackCoverTranslationLineSpacing = args.BackCoverTranslationLineSpacing,
            ImageCaptionTextSize = args.ImageCaptionTextSize,
            SourceLineSpacing = args.SourceLineSpacing,
            TranslationLineSpacing = args.TranslationLineSpacing,
            PageMultiple = args.PageMultiple,
        };

        var json = File.ReadAllText(args.JsonFilePath);
        var book = JsonSerializer.Deserialize<Book>(json,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                    ?? new Book();

        var cover = book.Tales.FirstOrDefault();

        if (cover?.Source.Title == "#0")
        {
            (string? sourceAuthor, string? sourceTitle, string? textBelow) = GetAuthorAndTitle(cover.Source.Paragraphs);
            (string? translationAuthor, string? translationTitle, string? translationTextBelow) = GetAuthorAndTitle(cover.Translation.Paragraphs);

            options.CoverAuthor = translationAuthor;

            options.CoverTitle = sourceTitle;
            options.CoverSubtitle = translationTitle;

            options.CoverSvgPath = PathHelper.GetSvelteImagePath(@"0");

            options.CoverTextBelowImage = translationTextBelow;

            book.Tales.Remove(cover);
        }

        var flyleaf = book.Tales.FirstOrDefault();

        if (flyleaf != null && flyleaf.Source.Paragraphs.All(p => string.IsNullOrEmpty(p)))
        {
            flyleaf.Translation.Title = flyleaf.Source.Title + " - " + flyleaf.Translation.Title;
            flyleaf.Source.Title = string.Empty;
            options.HasFlyLeaf = true;
        }


        var backCover = book.Tales.LastOrDefault();

        if (backCover?.Translation.Title?.Trim() == "Ω")
        {
            options.BackCoverTranslationParagraphs = backCover.Translation.Paragraphs;
            book.Tales.Remove(backCover);
        }

        foreach (var tale in book.Tales.Where(t => t.Source.Title?.StartsWith("#") ?? false))
        {
            var fileName = tale.Source.Title!.Skip(1).FirstOrDefault().ToString();
            tale.IllustrationSvgPath = PathHelper.GetSvelteImagePath(fileName);
        }

        new FacingPagesRenderer(options).Render(book, args.PdfFilePath).PatchWithPdfSharp(args.PdfFilePath);        
    }

    public static (string? author, string? title, string? textBelow) GetAuthorAndTitle(List<string> paragraphs)
    {
        return (author: paragraphs.FirstOrDefault(), title: paragraphs.Skip(1).FirstOrDefault(), textBelow: paragraphs.Skip(2).FirstOrDefault());
    }
}