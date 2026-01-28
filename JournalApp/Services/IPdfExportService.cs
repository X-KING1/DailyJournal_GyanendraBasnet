namespace JournalApp.Services;

// Export journal to PDF
public interface IPdfExportService
{
    Task<string> ExportToPdfAsync(DateTime startDate, DateTime endDate, string fileName);
    Task<string> ExportToPdfAtPathAsync(DateTime startDate, DateTime endDate, string fullPath);
}
