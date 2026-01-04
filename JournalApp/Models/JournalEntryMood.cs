using SQLite;

namespace JournalApp.Models;

// Links entries to moods (many-to-many relationship)
[Table("JournalEntryMoods")]
public class JournalEntryMood
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    // Which entry
    [Indexed, NotNull]
    public int EntryID { get; set; }

    // Which mood
    [Indexed, NotNull]
    public int MoodID { get; set; }

    // Primary or Secondary mood
    [MaxLength(20), NotNull]
    public string Type { get; set; } = "Primary";
}
