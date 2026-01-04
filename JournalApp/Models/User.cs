using SQLite;

namespace JournalApp.Models;

// User account info
[Table("Users")]
public class User
{
    [PrimaryKey, AutoIncrement]
    public int UserID { get; set; }

    // Name shown in the app
    [MaxLength(100), NotNull]
    public string UserName { get; set; } = string.Empty;

    // Optional email
    [MaxLength(200)]
    public string? Email { get; set; }

    // light or dark theme
    [MaxLength(20)]
    public string ThemePreference { get; set; } = "light";

    // When account was created
    [NotNull]
    public DateTime CreatedAt { get; set; }
}
