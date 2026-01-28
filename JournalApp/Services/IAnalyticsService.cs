using JournalApp.Models;

namespace JournalApp.Services;

// Stats and charts data
public interface IAnalyticsService
{
    Task<JournalAnalytics> GetAnalyticsAsync(DateTime? startDate = null, DateTime? endDate = null);
    Task<StreakInfo> CalculateStreakAsync();
}
