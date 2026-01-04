using SQLite;
using JournalApp.Models;
using Microsoft.Extensions.Logging;

namespace JournalApp.Services;

// Handles all database read/write operations
public class DatabaseService : IDatabaseService
{
    private readonly ILogger<DatabaseService> _logger;
    private SQLiteAsyncConnection? _database;
    private readonly string _dbPath;

    public DatabaseService(ILogger<DatabaseService> logger)
    {
        _logger = logger;
        _dbPath = Path.Combine(FileSystem.AppDataDirectory, "journal.db");
    }

    #region Database Initialization

    public async Task InitializeDatabaseAsync()
    {
        try
        {
            if (_database != null)
                return;

            _database = new SQLiteAsyncConnection(_dbPath);
            
            // Create all 7 tables
            await _database.CreateTableAsync<User>();
            await _database.CreateTableAsync<SecuritySettings>();
            await _database.CreateTableAsync<JournalEntry>();
            await _database.CreateTableAsync<Mood>();
            await _database.CreateTableAsync<Tag>();
            await _database.CreateTableAsync<JournalEntryMood>();
            await _database.CreateTableAsync<JournalEntryTag>();
            
            // Seed default data
            await SeedDefaultMoodsAsync();
            await SeedDefaultTagsAsync();
            
            // Create default user if none exists
            var user = await GetCurrentUserAsync();
            if (user == null)
            {
                await CreateUserAsync(new User 
                { 
                    UserName = "Default User",
                    CreatedAt = DateTime.Now
                });
            }
            
            _logger.LogInformation("Database ready at: " + _dbPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database setup failed");
            throw new InvalidOperationException("Could not set up database", ex);
        }
    }

    #endregion

    #region User Operations

    public async Task<User?> GetCurrentUserAsync()
    {
        await InitializeDatabaseAsync();
        return await _database!.Table<User>().FirstOrDefaultAsync();
    }

    public async Task<int> CreateUserAsync(User user)
    {
        await InitializeDatabaseAsync();
        user.CreatedAt = DateTime.Now;
        return await _database!.InsertAsync(user);
    }

    public async Task<int> UpdateUserAsync(User user)
    {
        await InitializeDatabaseAsync();
        return await _database!.UpdateAsync(user);
    }

    #endregion

    #region Journal Entry Operations

    public async Task<JournalEntry?> GetEntryByDateAsync(DateTime date)
    {
        try
        {
            await InitializeDatabaseAsync();
            
            var startOfDay = date.Date;
            var endOfDay = startOfDay.AddDays(1);
            
            return await _database!.Table<JournalEntry>()
                .Where(e => e.Date >= startOfDay && e.Date < endOfDay)
                .FirstOrDefaultAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Could not get entry for " + date.ToShortDateString());
            throw;
        }
    }

    public async Task<JournalEntry?> GetEntryByIdAsync(int entryId)
    {
        await InitializeDatabaseAsync();
        return await _database!.Table<JournalEntry>()
            .Where(e => e.EntryID == entryId)
            .FirstOrDefaultAsync();
    }

    public async Task<List<JournalEntry>> GetAllEntriesAsync()
    {
        try
        {
            await InitializeDatabaseAsync();
            return await _database!.Table<JournalEntry>()
                .OrderByDescending(e => e.Date)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Could not get entries");
            throw;
        }
    }

    public async Task<List<JournalEntry>> GetEntriesByDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        try
        {
            await InitializeDatabaseAsync();
            return await _database!.Table<JournalEntry>()
                .Where(e => e.Date >= startDate && e.Date <= endDate)
                .OrderByDescending(e => e.Date)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Could not get entries for date range");
            throw;
        }
    }

    public async Task<int> InsertEntryAsync(JournalEntry entry)
    {
        try
        {
            await InitializeDatabaseAsync();
            
            // Check if already exists
            var existing = await GetEntryByDateAsync(entry.Date);
            if (existing != null)
            {
                throw new InvalidOperationException(
                    "You already have an entry for " + entry.Date.ToString("yyyy-MM-dd") + ". Edit that one instead.");
            }

            // Set user if not set
            if (entry.UserID == 0)
            {
                var user = await GetCurrentUserAsync();
                entry.UserID = user?.UserID ?? 1;
            }

            entry.CreatedAt = DateTime.Now;
            entry.UpdatedAt = DateTime.Now;
            
            var result = await _database!.InsertAsync(entry);
            _logger.LogInformation("Created entry for " + entry.Date.ToShortDateString());
            return entry.EntryID;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Could not save entry");
            throw;
        }
    }

    public async Task<int> UpdateEntryAsync(JournalEntry entry)
    {
        try
        {
            await InitializeDatabaseAsync();
            entry.UpdatedAt = DateTime.Now;
            
            var result = await _database!.UpdateAsync(entry);
            _logger.LogInformation("Updated entry for " + entry.Date.ToShortDateString());
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Could not update entry");
            throw;
        }
    }

    public async Task<int> DeleteEntryAsync(int entryId)
    {
        try
        {
            await InitializeDatabaseAsync();
            
            var entry = await GetEntryByIdAsync(entryId);
            if (entry == null)
            {
                _logger.LogWarning("Entry with ID " + entryId + " not found");
                return 0;
            }
            
            // Delete junction table records first
            await _database!.ExecuteAsync("DELETE FROM JournalEntryMoods WHERE EntryID = ?", entryId);
            await _database!.ExecuteAsync("DELETE FROM JournalEntryTags WHERE EntryID = ?", entryId);
            
            var result = await _database!.DeleteAsync(entry);
            _logger.LogInformation("Deleted entry with ID " + entryId);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Could not delete entry");
            throw;
        }
    }

    public async Task<List<JournalEntry>> SearchEntriesAsync(string searchTerm)
    {
        try
        {
            await InitializeDatabaseAsync();
            
            if (string.IsNullOrWhiteSpace(searchTerm))
                return await GetAllEntriesAsync();

            var allEntries = await _database!.Table<JournalEntry>().ToListAsync();
            
            return allEntries
                .Where(e => (e.Title?.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ?? false) || 
                           (e.Content?.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ?? false))
                .OrderByDescending(e => e.Date)
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Search failed");
            throw;
        }
    }

    public async Task<List<JournalEntry>> FilterEntriesAsync(
        DateTime? startDate, 
        DateTime? endDate,
        List<int>? moodIds, 
        List<int>? tagIds)
    {
        try
        {
            await InitializeDatabaseAsync();
            var entries = await GetAllEntriesAsync();

            if (startDate.HasValue)
                entries = entries.Where(e => e.Date >= startDate.Value).ToList();
            
            if (endDate.HasValue)
                entries = entries.Where(e => e.Date <= endDate.Value).ToList();

            if (moodIds != null && moodIds.Count > 0)
            {
                var entryIdsWithMoods = await _database!.Table<JournalEntryMood>()
                    .Where(em => moodIds.Contains(em.MoodID))
                    .ToListAsync();
                var validEntryIds = entryIdsWithMoods.Select(em => em.EntryID).Distinct().ToList();
                entries = entries.Where(e => validEntryIds.Contains(e.EntryID)).ToList();
            }

            if (tagIds != null && tagIds.Count > 0)
            {
                var entryIdsWithTags = await _database!.Table<JournalEntryTag>()
                    .Where(et => tagIds.Contains(et.TagID))
                    .ToListAsync();
                var validEntryIds = entryIdsWithTags.Select(et => et.EntryID).Distinct().ToList();
                entries = entries.Where(e => validEntryIds.Contains(e.EntryID)).ToList();
            }

            return entries.OrderByDescending(e => e.Date).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Filter failed");
            throw;
        }
    }

    #endregion

    #region Mood Operations

    public async Task<List<Mood>> GetAllMoodsAsync()
    {
        await InitializeDatabaseAsync();
        return await _database!.Table<Mood>().ToListAsync();
    }

    public async Task<List<Mood>> GetMoodsByEntryIdAsync(int entryId)
    {
        await InitializeDatabaseAsync();
        
        var entryMoods = await _database!.Table<JournalEntryMood>()
            .Where(em => em.EntryID == entryId)
            .ToListAsync();
        
        var moodIds = entryMoods.Select(em => em.MoodID).ToList();
        var allMoods = await _database!.Table<Mood>().ToListAsync();
        
        return allMoods.Where(m => moodIds.Contains(m.MoodID)).ToList();
    }

    public async Task<int> AddMoodAsync(Mood mood)
    {
        await InitializeDatabaseAsync();
        return await _database!.InsertAsync(mood);
    }

    public async Task SetEntryMoodsAsync(int entryId, int primaryMoodId, List<int>? secondaryMoodIds)
    {
        await InitializeDatabaseAsync();
        
        // Remove existing moods for this entry
        await _database!.ExecuteAsync("DELETE FROM JournalEntryMoods WHERE EntryID = ?", entryId);
        
        // Add primary mood
        await _database!.InsertAsync(new JournalEntryMood 
        { 
            EntryID = entryId, 
            MoodID = primaryMoodId, 
            Type = "Primary" 
        });
        
        // Add secondary moods (max 2)
        if (secondaryMoodIds != null)
        {
            foreach (var moodId in secondaryMoodIds.Take(2))
            {
                await _database!.InsertAsync(new JournalEntryMood 
                { 
                    EntryID = entryId, 
                    MoodID = moodId, 
                    Type = "Secondary" 
                });
            }
        }
    }

    public async Task<Mood?> GetPrimaryMoodForEntryAsync(int entryId)
    {
        await InitializeDatabaseAsync();
        
        var entryMood = await _database!.Table<JournalEntryMood>()
            .Where(em => em.EntryID == entryId && em.Type == "Primary")
            .FirstOrDefaultAsync();
        
        if (entryMood == null) return null;
        
        return await _database!.Table<Mood>()
            .Where(m => m.MoodID == entryMood.MoodID)
            .FirstOrDefaultAsync();
    }

    public async Task<List<Mood>> GetSecondaryMoodsForEntryAsync(int entryId)
    {
        await InitializeDatabaseAsync();
        
        var entryMoods = await _database!.Table<JournalEntryMood>()
            .Where(em => em.EntryID == entryId && em.Type == "Secondary")
            .ToListAsync();
        
        var moodIds = entryMoods.Select(em => em.MoodID).ToList();
        var allMoods = await _database!.Table<Mood>().ToListAsync();
        
        return allMoods.Where(m => moodIds.Contains(m.MoodID)).ToList();
    }

    #endregion

    #region Tag Operations

    public async Task<List<Tag>> GetAllTagsAsync()
    {
        await InitializeDatabaseAsync();
        return await _database!.Table<Tag>().ToListAsync();
    }

    public async Task<List<Tag>> GetTagsByEntryIdAsync(int entryId)
    {
        await InitializeDatabaseAsync();
        
        var entryTags = await _database!.Table<JournalEntryTag>()
            .Where(et => et.EntryID == entryId)
            .ToListAsync();
        
        var tagIds = entryTags.Select(et => et.TagID).ToList();
        var allTags = await _database!.Table<Tag>().ToListAsync();
        
        return allTags.Where(t => tagIds.Contains(t.TagID)).ToList();
    }

    public async Task<int> AddTagAsync(Tag tag)
    {
        await InitializeDatabaseAsync();
        tag.CreatedAt = DateTime.Now;
        return await _database!.InsertAsync(tag);
    }

    public async Task SetEntryTagsAsync(int entryId, List<int> tagIds)
    {
        await InitializeDatabaseAsync();
        
        // Remove existing tags for this entry
        await _database!.ExecuteAsync("DELETE FROM JournalEntryTags WHERE EntryID = ?", entryId);
        
        // Add new tags
        foreach (var tagId in tagIds)
        {
            await _database!.InsertAsync(new JournalEntryTag 
            { 
                EntryID = entryId, 
                TagID = tagId 
            });
        }
    }

    #endregion

    #region Security Operations

    public async Task<SecuritySettings?> GetSecuritySettingsAsync(int userId)
    {
        await InitializeDatabaseAsync();
        return await _database!.Table<SecuritySettings>()
            .Where(s => s.UserID == userId)
            .FirstOrDefaultAsync();
    }

    public async Task<int> SaveSecuritySettingsAsync(SecuritySettings settings)
    {
        await InitializeDatabaseAsync();
        settings.UpdatedAt = DateTime.Now;
        
        var existing = await GetSecuritySettingsAsync(settings.UserID);
        if (existing != null)
        {
            settings.SettingID = existing.SettingID;
            return await _database!.UpdateAsync(settings);
        }
        return await _database!.InsertAsync(settings);
    }

    #endregion

    #region Seed Data

    public async Task SeedDefaultMoodsAsync()
    {
        await InitializeDatabaseAsync();
        
        var existingMoods = await _database!.Table<Mood>().CountAsync();
        if (existingMoods > 0) return;

        var defaultMoods = new List<Mood>
        {
            // Positive moods
            new() { MoodName = "Happy", Category = "Positive", Emoji = "😊", IsPreDefined = true },
            new() { MoodName = "Excited", Category = "Positive", Emoji = "🎉", IsPreDefined = true },
            new() { MoodName = "Grateful", Category = "Positive", Emoji = "🙏", IsPreDefined = true },
            new() { MoodName = "Peaceful", Category = "Positive", Emoji = "😌", IsPreDefined = true },
            new() { MoodName = "Loved", Category = "Positive", Emoji = "❤️", IsPreDefined = true },
            new() { MoodName = "Confident", Category = "Positive", Emoji = "💪", IsPreDefined = true },
            
            // Neutral moods
            new() { MoodName = "Calm", Category = "Neutral", Emoji = "😐", IsPreDefined = true },
            new() { MoodName = "Tired", Category = "Neutral", Emoji = "😴", IsPreDefined = true },
            new() { MoodName = "Thoughtful", Category = "Neutral", Emoji = "🤔", IsPreDefined = true },
            new() { MoodName = "Busy", Category = "Neutral", Emoji = "📋", IsPreDefined = true },
            
            // Negative moods
            new() { MoodName = "Sad", Category = "Negative", Emoji = "😢", IsPreDefined = true },
            new() { MoodName = "Anxious", Category = "Negative", Emoji = "😰", IsPreDefined = true },
            new() { MoodName = "Stressed", Category = "Negative", Emoji = "😫", IsPreDefined = true },
            new() { MoodName = "Angry", Category = "Negative", Emoji = "😠", IsPreDefined = true },
            new() { MoodName = "Lonely", Category = "Negative", Emoji = "😔", IsPreDefined = true }
        };

        foreach (var mood in defaultMoods)
        {
            await _database!.InsertAsync(mood);
        }
        
        _logger.LogInformation("Seeded " + defaultMoods.Count + " default moods");
    }

    public async Task SeedDefaultTagsAsync()
    {
        await InitializeDatabaseAsync();
        
        var existingTags = await _database!.Table<Tag>().CountAsync();
        if (existingTags > 0) return;

        var defaultTags = new List<Tag>
        {
            new() { TagName = "Work", Category = "Professional", Color = "#3B82F6", IsPreDefined = true },
            new() { TagName = "Health", Category = "Wellness", Color = "#10B981", IsPreDefined = true },
            new() { TagName = "Travel", Category = "Lifestyle", Color = "#F59E0B", IsPreDefined = true },
            new() { TagName = "Fitness", Category = "Wellness", Color = "#EF4444", IsPreDefined = true },
            new() { TagName = "Family", Category = "Personal", Color = "#EC4899", IsPreDefined = true },
            new() { TagName = "Friends", Category = "Personal", Color = "#8B5CF6", IsPreDefined = true },
            new() { TagName = "Learning", Category = "Growth", Color = "#06B6D4", IsPreDefined = true },
            new() { TagName = "Finance", Category = "Professional", Color = "#84CC16", IsPreDefined = true }
        };

        foreach (var tag in defaultTags)
        {
            tag.CreatedAt = DateTime.Now;
            await _database!.InsertAsync(tag);
        }
        
        _logger.LogInformation("Seeded " + defaultTags.Count + " default tags");
    }

    #endregion
}
