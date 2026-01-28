namespace JournalApp.Models;

// Streak tracking
public class StreakInfo
{
    public int CurrentStreak { get; set; }
    public int LongestStreak { get; set; }
    public int TotalEntries { get; set; }
    public List<DateTime> MissedDays { get; set; } = [];
}
