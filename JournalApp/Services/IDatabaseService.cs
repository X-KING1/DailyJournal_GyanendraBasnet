using JournalApp.Models;

namespace JournalApp.Services;

// All database operations
public interface IDatabaseService
{
    // Setup
    Task InitializeDatabaseAsync();
    
    // User stuff
    Task<User?> GetCurrentUserAsync();
    Task<int> CreateUserAsync(User user);
    Task<int> UpdateUserAsync(User user);
    
    // Journal entries
    Task<JournalEntry?> GetEntryByDateAsync(DateTime date);
    Task<JournalEntry?> GetEntryByIdAsync(int entryId);
    Task<List<JournalEntry>> GetAllEntriesAsync();
    Task<List<JournalEntry>> GetEntriesByDateRangeAsync(DateTime startDate, DateTime endDate);
    Task<int> InsertEntryAsync(JournalEntry entry);
    Task<int> UpdateEntryAsync(JournalEntry entry);
    Task<int> DeleteEntryAsync(int entryId);
    Task<List<JournalEntry>> SearchEntriesAsync(string searchTerm);
    Task<List<JournalEntry>> FilterEntriesAsync(DateTime? startDate, DateTime? endDate, 
        List<int>? moodIds, List<int>? tagIds);
    
    // Mood handling
    Task<List<Mood>> GetAllMoodsAsync();
    Task<List<Mood>> GetMoodsByEntryIdAsync(int entryId);
    Task<int> AddMoodAsync(Mood mood);
    Task SetEntryMoodsAsync(int entryId, int primaryMoodId, List<int>? secondaryMoodIds);
    Task<Mood?> GetPrimaryMoodForEntryAsync(int entryId);
    Task<List<Mood>> GetSecondaryMoodsForEntryAsync(int entryId);
    
    // Tag handling
    Task<List<Tag>> GetAllTagsAsync();
    Task<List<Tag>> GetTagsByEntryIdAsync(int entryId);
    Task<int> AddTagAsync(Tag tag);
    Task SetEntryTagsAsync(int entryId, List<int> tagIds);
    
    // Security
    Task<SecuritySettings?> GetSecuritySettingsAsync(int userId);
    Task<int> SaveSecuritySettingsAsync(SecuritySettings settings);
    
    // Load default data
    Task SeedDefaultMoodsAsync();
    Task SeedDefaultTagsAsync();
}
