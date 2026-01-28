using JournalApp.Models;
using Microsoft.Extensions.Logging;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using PdfColors = QuestPDF.Helpers.Colors;

namespace JournalApp.Services;

// Creates PDF files from journal entries
public class PdfExportService : IPdfExportService
{
    private readonly IDatabaseService _databaseService;
    private readonly ILogger<PdfExportService> _logger;

    public PdfExportService(IDatabaseService databaseService, ILogger<PdfExportService> logger)
    {
        _databaseService = databaseService;
        _logger = logger;
        
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public async Task<string> ExportToPdfAsync(DateTime startDate, DateTime endDate, string fileName)
    {
        try
        {
            var entries = await _databaseService.GetEntriesByDateRangeAsync(startDate, endDate);

            if (entries.Count == 0)
            {
                throw new InvalidOperationException("No entries found for those dates.");
            }

            // Prepare entry data with moods and tags
            var entryDataList = new List<EntryPdfData>();
            foreach (var entry in entries)
            {
                var primaryMood = await _databaseService.GetPrimaryMoodForEntryAsync(entry.EntryID);
                var secondaryMoods = await _databaseService.GetSecondaryMoodsForEntryAsync(entry.EntryID);
                var tags = await _databaseService.GetTagsByEntryIdAsync(entry.EntryID);

                entryDataList.Add(new EntryPdfData
                {
                    Entry = entry,
                    PrimaryMood = primaryMood,
                    SecondaryMoods = secondaryMoods,
                    Tags = tags
                });
            }

            var filePath = Path.Combine(FileSystem.AppDataDirectory, fileName);

            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.PageColor(PdfColors.White);
                    page.DefaultTextStyle(x => x.FontSize(11));

                    page.Header()
                        .Text($"Journal Entries ({startDate:MMM dd, yyyy} - {endDate:MMM dd, yyyy})")
                        .SemiBold().FontSize(20).FontColor(PdfColors.Blue.Medium);

                    page.Content()
                        .PaddingVertical(1, Unit.Centimetre)
                        .Column(column =>
                        {
                            column.Spacing(20);

                            foreach (var data in entryDataList)
                            {
                                column.Item().Element(c => ComposeEntry(c, data));
                            }
                        });

                    page.Footer()
                        .AlignCenter()
                        .Text(x =>
                        {
                            x.Span("Page ");
                            x.CurrentPageNumber();
                            x.Span(" of ");
                            x.TotalPages();
                        });
                });
            })
            .GeneratePdf(filePath);

            _logger.LogInformation("PDF saved to: " + filePath);
            return filePath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Could not create PDF");
            throw;
        }
    }

    public async Task<string> ExportToPdfAtPathAsync(DateTime startDate, DateTime endDate, string fullPath)
    {
        try
        {
            var entries = await _databaseService.GetEntriesByDateRangeAsync(startDate, endDate);

            if (entries.Count == 0)
            {
                throw new InvalidOperationException("No entries found for those dates.");
            }

            // Prepare entry data with moods and tags
            var entryDataList = new List<EntryPdfData>();
            foreach (var entry in entries)
            {
                var primaryMood = await _databaseService.GetPrimaryMoodForEntryAsync(entry.EntryID);
                var secondaryMoods = await _databaseService.GetSecondaryMoodsForEntryAsync(entry.EntryID);
                var tags = await _databaseService.GetTagsByEntryIdAsync(entry.EntryID);

                entryDataList.Add(new EntryPdfData
                {
                    Entry = entry,
                    PrimaryMood = primaryMood,
                    SecondaryMoods = secondaryMoods,
                    Tags = tags
                });
            }

            // Ensure directory exists
            var directory = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.PageColor(PdfColors.White);
                    page.DefaultTextStyle(x => x.FontSize(11));

                    page.Header()
                        .Text($"Journal Entries ({startDate:MMM dd, yyyy} - {endDate:MMM dd, yyyy})")
                        .SemiBold().FontSize(20).FontColor(PdfColors.Blue.Medium);

                    page.Content()
                        .PaddingVertical(1, Unit.Centimetre)
                        .Column(column =>
                        {
                            column.Spacing(20);

                            foreach (var data in entryDataList)
                            {
                                column.Item().Element(c => ComposeEntry(c, data));
                            }
                        });

                    page.Footer()
                        .AlignCenter()
                        .Text(x =>
                        {
                            x.Span("Page ");
                            x.CurrentPageNumber();
                            x.Span(" of ");
                            x.TotalPages();
                        });
                });
            })
            .GeneratePdf(fullPath);

            _logger.LogInformation("PDF saved to: " + fullPath);
            return fullPath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Could not create PDF");
            throw;
        }
    }

    private static void ComposeEntry(QuestPDF.Infrastructure.IContainer container, EntryPdfData data)
    {
        var entry = data.Entry;
        
        container.Border(1).BorderColor(PdfColors.Grey.Lighten2).Padding(15).Column(column =>
        {
            column.Spacing(10);

            column.Item().Row(row =>
            {
                row.RelativeItem().Text(entry.Date.ToString("dddd, MMMM dd, yyyy"))
                    .SemiBold().FontSize(14).FontColor(PdfColors.Blue.Darken2);
            });

            if (!string.IsNullOrWhiteSpace(entry.Title))
            {
                column.Item().Text(entry.Title).Bold().FontSize(16);
            }

            // Primary mood
            if (data.PrimaryMood != null)
            {
                column.Item().Text($"Mood: {data.PrimaryMood.Emoji} {data.PrimaryMood.MoodName}")
                    .FontColor(PdfColors.Grey.Darken1);
            }

            // Secondary moods
            if (data.SecondaryMoods.Count > 0)
            {
                var moodNames = string.Join(", ", data.SecondaryMoods.Select(m => $"{m.Emoji} {m.MoodName}"));
                column.Item().Text($"Also feeling: {moodNames}")
                    .FontSize(10).FontColor(PdfColors.Grey.Medium);
            }

            column.Item().Text(StripHtml(entry.Content)).FontSize(11).LineHeight(1.5f);

            // Tags
            if (data.Tags.Count > 0)
            {
                var tagNames = string.Join(", ", data.Tags.Select(t => t.TagName));
                column.Item().PaddingTop(10).Row(row =>
                {
                    row.AutoItem().Text($"Tags: {tagNames}")
                        .FontSize(9).Italic().FontColor(PdfColors.Grey.Darken1);
                });
            }

            column.Item().Text($"{entry.WordCount} words")
                .FontSize(8).FontColor(PdfColors.Grey.Medium);
        });
    }

    // Strips HTML tags and converts to clean plain text
    private static string StripHtml(string? html)
    {
        if (string.IsNullOrEmpty(html))
            return string.Empty;

        // Replace common block tags with newlines
        var text = html
            .Replace("</p>", "\n")
            .Replace("</div>", "\n")
            .Replace("</li>", "\n")
            .Replace("<br>", "\n")
            .Replace("<br/>", "\n")
            .Replace("<br />", "\n");

        // Remove all remaining HTML tags
        text = System.Text.RegularExpressions.Regex.Replace(text, "<[^>]+>", "");

        // Decode HTML entities
        text = System.Net.WebUtility.HtmlDecode(text);

        // Clean up extra whitespace and newlines
        text = System.Text.RegularExpressions.Regex.Replace(text, @"\n\s*\n", "\n\n");
        text = text.Trim();

        return text;
    }

    // Helper class for PDF entry data
    private class EntryPdfData
    {
        public JournalEntry Entry { get; set; } = null!;
        public Mood? PrimaryMood { get; set; }
        public List<Mood> SecondaryMoods { get; set; } = [];
        public List<Tag> Tags { get; set; } = [];
    }
}
