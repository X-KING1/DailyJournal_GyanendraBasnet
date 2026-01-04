using SQLite;

namespace JournalApp.Models;

// Links entries to tags (many-to-many relationship)
[Table("JournalEntryTags")]
public class JournalEntryTag
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    // Which entry
    [Indexed, NotNull]
    public int EntryID { get; set; }

    // Which tag
    [Indexed, NotNull]
    public int TagID { get; set; }
}
