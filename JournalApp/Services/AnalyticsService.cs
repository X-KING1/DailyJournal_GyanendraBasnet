using JournalApp.Models;
using Microsoft.Extensions.Logging;

namespace JournalApp.Services;

// Calculates stats from journal entries
public class AnalyticsService : IAnalyticsService
{
    private readonly IDatabaseService _databaseService;
    private readonly ILogger<AnalyticsService> _logger;

    public AnalyticsService(IDatabaseService databaseService, ILogger<AnalyticsService> logger)
    {
        _databaseService = databaseService;
        _logger = logger;
    }

    public async Task<JournalAnalytics> GetAnalyticsAsync(DateTime? startDate = null, DateTime? endDate = null)
    {
        try
        {
            var entries = startDate.HasValue || endDate.HasValue
                ? await _databaseService.GetEntriesByDateRangeAsync(
                    startDate ?? DateTime.MinValue, 
                    endDate ?? DateTime.MaxValue)
                : await _databaseService.GetAllEntriesAsync();

            var analytics = new JournalAnalytics
            {
                MoodDistribution = await CalculateMoodDistributionAsync(entries),
                MostFrequentMood = await CalculateMostFrequentMoodAsync(entries),
                TagFrequency = await CalculateTagFrequencyAsync(entries),
                CategoryBreakdown = new Dictionary<string, int>(), // Categories now via tags
                WordCountTrends = CalculateWordCountTrends(entries),
                StreakInfo = await CalculateStreakAsync()
            };

            return analytics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Could not calculate analytics");
            throw;
        }
    }

    private async Task<Dictionary<MoodCategory, int>> CalculateMoodDistributionAsync(List<JournalEntry> entries)
    {
        var distribution = new Dictionary<MoodCategory, int>
        {
            { MoodCategory.Positive, 0 },
            { MoodCategory.Neutral, 0 },
            { MoodCategory.Negative, 0 }
        };

        var allMoods = await _databaseService.GetAllMoodsAsync();

        foreach (var entry in entries)
        {
            var primaryMood = await _databaseService.GetPrimaryMoodForEntryAsync(entry.EntryID);
            if (primaryMood != null)
            {
                if (Enum.TryParse<MoodCategory>(primaryMood.Category, out var category))
                {
                    distribution[category]++;
                }
            }
        }

        return distribution;
    }

    private async Task<string?> CalculateMostFrequentMoodAsync(List<JournalEntry> entries)
    {
        if (entries.Count == 0) return null;

        var moodCounts = new Dictionary<string, int>();

        foreach (var entry in entries)
        {
            var primaryMood = await _databaseService.GetPrimaryMoodForEntryAsync(entry.EntryID);
            if (primaryMood != null)
            {
                if (!moodCounts.TryAdd(primaryMood.MoodName, 1))
                    moodCounts[primaryMood.MoodName]++;
            }
        }

        return moodCounts.OrderByDescending(kvp => kvp.Value).FirstOrDefault().Key;
    }

    private async Task<Dictionary<string, int>> CalculateTagFrequencyAsync(List<JournalEntry> entries)
    {
        var tagFrequency = new Dictionary<string, int>();

        foreach (var entry in entries)
        {
            var tags = await _databaseService.GetTagsByEntryIdAsync(entry.EntryID);
            foreach (var tag in tags)
            {
                if (!tagFrequency.TryAdd(tag.TagName, 1))
                    tagFrequency[tag.TagName]++;
            }
        }

        return tagFrequency.OrderByDescending(kvp => kvp.Value)
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }

    private static Dictionary<DateTime, double> CalculateWordCountTrends(List<JournalEntry> entries)
    {
        return entries
            .GroupBy(e => e.Date.Date)
            .OrderBy(g => g.Key)
            .ToDictionary(g => g.Key, g => g.Average(e => e.WordCount));
    }

    public async Task<StreakInfo> CalculateStreakAsync()
    {
        try
        {
            var entries = await _databaseService.GetAllEntriesAsync();
            var entryDates = entries.Select(e => e.Date.Date).OrderByDescending(d => d).ToList();

            if (entryDates.Count == 0)
            {
                return new StreakInfo
                {
                    CurrentStreak = 0,
                    LongestStreak = 0,
                    TotalEntries = 0,
                    MissedDays = []
                };
            }

            var currentStreak = CalculateCurrentStreak(entryDates);
            var longestStreak = CalculateLongestStreak(entryDates);
            var missedDays = CalculateMissedDays(entryDates);

            return new StreakInfo
            {
                CurrentStreak = currentStreak,
                LongestStreak = longestStreak,
                TotalEntries = entries.Count,
                MissedDays = missedDays
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Could not calculate streak");
            throw;
        }
    }

    private static int CalculateCurrentStreak(List<DateTime> entryDates)
    {
        var today = DateTime.Today;
        var streak = 0;

        if (!entryDates.Contains(today) && !entryDates.Contains(today.AddDays(-1)))
            return 0;

        var checkDate = entryDates.Contains(today) ? today : today.AddDays(-1);

        while (entryDates.Contains(checkDate))
        {
            streak++;
            checkDate = checkDate.AddDays(-1);
        }

        return streak;
    }

    private static int CalculateLongestStreak(List<DateTime> entryDates)
    {
        if (entryDates.Count == 0) return 0;

        var maxStreak = 1;
        var currentStreak = 1;

        for (int i = 0; i < entryDates.Count - 1; i++)
        {
            if ((entryDates[i] - entryDates[i + 1]).Days == 1)
            {
                currentStreak++;
                maxStreak = Math.Max(maxStreak, currentStreak);
            }
            else
            {
                currentStreak = 1;
            }
        }

        return maxStreak;
    }

    private static List<DateTime> CalculateMissedDays(List<DateTime> entryDates)
    {
        if (entryDates.Count == 0) return [];

        var missedDays = new List<DateTime>();
        var startDate = entryDates.Min();
        var endDate = DateTime.Today;

        for (var date = startDate; date <= endDate; date = date.AddDays(1))
        {
            if (!entryDates.Contains(date))
            {
                missedDays.Add(date);
            }
        }

        return missedDays;
    }
}
