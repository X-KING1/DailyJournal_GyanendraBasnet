using SQLite;

namespace JournalApp.Models;

// Tags to categorize journal entries
[Table("Tags")]
public class Tag
{
    [PrimaryKey, AutoIncrement]
    public int TagID { get; set; }

    // Tag name like Work, Health, Travel
    [MaxLength(50), NotNull]
    public string TagName { get; set; } = string.Empty;

    // Group tags by category
    [MaxLength(50)]
    public string? Category { get; set; }

    // Hex color for display
    [MaxLength(10)]
    public string? Color { get; set; }

    // True for default tags
    public bool IsPreDefined { get; set; } = false;

    // When tag was made
    public DateTime CreatedAt { get; set; }
}
