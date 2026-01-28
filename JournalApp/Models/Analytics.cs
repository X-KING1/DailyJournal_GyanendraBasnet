namespace JournalApp.Models;

// Analytics data for dashboard
public class JournalAnalytics
{
    public Dictionary<MoodCategory, int> MoodDistribution { get; set; } = new();
    public string? MostFrequentMood { get; set; }
    public Dictionary<string, int> TagFrequency { get; set; } = new();
    public Dictionary<string, int> CategoryBreakdown { get; set; } = new();
    public Dictionary<DateTime, double> WordCountTrends { get; set; } = new();
    public StreakInfo StreakInfo { get; set; } = new();
}
