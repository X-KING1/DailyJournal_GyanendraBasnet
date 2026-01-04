using SQLite;

namespace JournalApp.Models;

// Main journal entry - stores one entry per day
[Table("JournalEntries")]
public class JournalEntry
{
    [PrimaryKey, AutoIncrement]
    public int EntryID { get; set; }

    // Links to user who wrote this
    [Indexed, NotNull]
    public int UserID { get; set; }

    // Entry date - only one entry per day allowed
    [Indexed, NotNull]
    public DateTime Date { get; set; }

    // Optional title for the entry
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    // Main journal content (HTML from editor)
    public string Content { get; set; } = string.Empty;

    // Tracks word count for stats
    public int WordCount { get; set; }

    // Auto-set when entry is created
    [NotNull]
    public DateTime CreatedAt { get; set; }

    // Updated whenever entry changes
    [NotNull]
    public DateTime UpdatedAt { get; set; }
}
