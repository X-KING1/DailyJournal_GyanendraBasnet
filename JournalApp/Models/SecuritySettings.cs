using SQLite;

namespace JournalApp.Models;

// Password/PIN settings for app security
[Table("SecuritySettings")]
public class SecuritySettings
{
    [PrimaryKey, AutoIncrement]
    public int SettingID { get; set; }

    // Which user
    [Indexed, NotNull]
    public int UserID { get; set; }

    // Password hash stored securely
    [MaxLength(256)]
    public string? HashedPIN { get; set; }

    // Is password lock on or off
    public bool IsEnabled { get; set; } = false;

    // Tracks wrong password tries
    public int FailedAttempts { get; set; } = 0;

    // Last time settings changed
    [NotNull]
    public DateTime UpdatedAt { get; set; }
}
