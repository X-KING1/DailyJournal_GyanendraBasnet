using SQLite;

namespace JournalApp.Models;

// Mood options for journal entries
[Table("Moods")]
public class Mood
{
    [PrimaryKey, AutoIncrement]
    public int MoodID { get; set; }

    // e.g. Happy, Sad, Anxious
    [MaxLength(50), NotNull]
    public string MoodName { get; set; } = string.Empty;

    // Positive, Neutral, or Negative
    [MaxLength(20), NotNull]
    public string Category { get; set; } = "Neutral";

    // Emoji to show with mood
    [MaxLength(10)]
    public string? Emoji { get; set; }

    // True if its a default system mood
    public bool IsPreDefined { get; set; } = false;
}
