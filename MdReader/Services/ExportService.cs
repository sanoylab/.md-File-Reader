using Markdig;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Xceed.Words.NET;

namespace MdReader.Services;

public class ExportService
{
    private readonly MarkdownPipeline _markdownPipeline;

    public ExportService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
        _markdownPipeline = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .Build();
    }

    // Convert markdown to HTML
    public string MarkdownToHtml(string markdown)
    {
        return Markdown.ToHtml(markdown, _markdownPipeline);
    }

    // Export to PDF
    public byte[] ExportToPdf(string markdown, string title)
    {
        try
        {
            using var stream = new MemoryStream();
            
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(12).FontFamily("Arial"));

                    page.Content()
                        .Column(column =>
                        {
                            column.Item().Text(title).FontSize(20).Bold();
                            column.Item().PaddingBottom(10);
                            column.Item().PaddingTop(10);

                            // Parse markdown and convert to PDF elements
                            var lines = markdown.Split('\n');
                            var inCodeBlock = false;
                            var codeBlockContent = new List<string>();

                            foreach (var line in lines)
                            {
                                var trimmed = line.Trim();

                                if (trimmed.StartsWith("```"))
                                {
                                    if (inCodeBlock && codeBlockContent.Count > 0)
                                    {
                                        column.Item().Padding(5).Background(Colors.Grey.Lighten3)
                                            .Text(string.Join("\n", codeBlockContent))
                                            .FontFamily("Courier New").FontSize(10);
                                        codeBlockContent.Clear();
                                    }
                                    inCodeBlock = !inCodeBlock;
                                    continue;
                                }

                                if (inCodeBlock)
                                {
                                    codeBlockContent.Add(trimmed);
                                    continue;
                                }

                                if (trimmed.StartsWith("# "))
                                {
                                    column.Item().PaddingTop(10).Text(trimmed.Substring(2)).FontSize(18).Bold();
                                }
                                else if (trimmed.StartsWith("## "))
                                {
                                    column.Item().PaddingTop(8).Text(trimmed.Substring(3)).FontSize(16).Bold();
                                }
                                else if (trimmed.StartsWith("### "))
                                {
                                    column.Item().PaddingTop(6).Text(trimmed.Substring(4)).FontSize(14).Bold();
                                }
                                else if (trimmed.StartsWith("- ") || trimmed.StartsWith("* "))
                                {
                                    column.Item().PaddingLeft(10).Text("• " + trimmed.Substring(2));
                                }
                                else if (!string.IsNullOrWhiteSpace(trimmed))
                                {
                                    column.Item().Text(trimmed);
                                }
                                else
                                {
                                    column.Item().Height(5);
                                }
                            }

                            if (inCodeBlock && codeBlockContent.Count > 0)
                            {
                                column.Item().Padding(5).Background(Colors.Grey.Lighten3)
                                    .Text(string.Join("\n", codeBlockContent))
                                    .FontFamily("Courier New").FontSize(10);
                            }
                        });
                });
            });

            document.GeneratePdf(stream);
            return stream.ToArray();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                "Failed to export PDF. Error: " + ex.Message, ex);
        }
    }

    // Export to Word
    public byte[] ExportToWord(string markdown, string title)
    {
        using var stream = new MemoryStream();
        using var doc = DocX.Create(stream);

        // Add title with larger font
        var titlePara = doc.InsertParagraph(title);
        titlePara.FontSize(18).Bold();
        doc.InsertParagraph();

        // Convert markdown to plain text with basic formatting
        var html = MarkdownToHtml(markdown);
        
        // Simple approach: convert HTML to plain text and add paragraphs
        var lines = markdown.Split('\n');
        var inCodeBlock = false;

        foreach (var line in lines)
        {
            var trimmed = line.Trim();

            if (trimmed.StartsWith("```"))
            {
                inCodeBlock = !inCodeBlock;
                continue;
            }

            if (inCodeBlock)
            {
                var codePara = doc.InsertParagraph(trimmed);
                codePara.Font("Courier New").FontSize(10);
                continue;
            }

            // Remove markdown syntax and add as plain text
            var text = trimmed;
            if (text.StartsWith("# "))
            {
                text = text.Substring(2);
                var para = doc.InsertParagraph(text);
                para.FontSize(16).Bold();
            }
            else if (text.StartsWith("## "))
            {
                text = text.Substring(3);
                var para = doc.InsertParagraph(text);
                para.FontSize(14).Bold();
            }
            else if (text.StartsWith("### "))
            {
                text = text.Substring(4);
                var para = doc.InsertParagraph(text);
                para.FontSize(12).Bold();
            }
            else if (text.StartsWith("- ") || text.StartsWith("* "))
            {
                text = "• " + text.Substring(2);
                doc.InsertParagraph(text);
            }
            else if (!string.IsNullOrWhiteSpace(text))
            {
                doc.InsertParagraph(text);
            }
            else
            {
                doc.InsertParagraph();
            }
        }

        doc.Save();
        return stream.ToArray();
    }
}

