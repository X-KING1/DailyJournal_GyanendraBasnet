using JournalApp.Models;

namespace JournalApp.Helpers;

// Helper functions for mood colors and styles
public static class MoodHelper
{
    // Get hex color for mood type
    public static string GetCategoryColor(MoodCategory category)
    {
        return category switch
        {
            MoodCategory.Positive => "#10b981", // green
            MoodCategory.Neutral => "#3b82f6",  // blue
            MoodCategory.Negative => "#ef4444", // red
            _ => "#6b7280" // gray default
        };
    }

    // Get CSS class for progress bars
    public static string GetProgressClass(MoodCategory category)
    {
        return category switch
        {
            MoodCategory.Positive => "progress-fill-positive",
            MoodCategory.Neutral => "progress-fill-neutral",
            MoodCategory.Negative => "progress-fill-negative",
            _ => "progress-fill-neutral"
        };
    }
    
    // Same as above but takes string input
    public static string GetCategoryColorFromString(string category)
    {
        return category switch
        {
            "Positive" => "#10b981",
            "Neutral" => "#3b82f6",
            "Negative" => "#ef4444",
            _ => "#6b7280"
        };
    }
    
    // Progress class from string category
    public static string GetProgressClassFromString(string category)
    {
        return category switch
        {
            "Positive" => "progress-fill-positive",
            "Neutral" => "progress-fill-neutral",
            "Negative" => "progress-fill-negative",
            _ => "progress-fill-neutral"
        };
    }
}
