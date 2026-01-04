using Microsoft.Extensions.Logging;
using JournalApp.Services;
using Syncfusion.Blazor;
using Syncfusion.Maui.Core.Hosting;

namespace JournalApp;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        // Syncfusion license for charts
        Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense("Ngo9BigBOggjHTQxAR8/V1JGaF5cXGpCfEx0Q3xbf1x2ZFRMYVlbRXZPIiBoS35RcEViW3hfc3VQR2ZcVER2VEFf");

        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureSyncfusionCore()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            });

        // Blazor WebView for hybrid app
        builder.Services.AddMauiBlazorWebView();

        // Syncfusion charts
        builder.Services.AddSyncfusionBlazor();

        // Register services - singleton so one instance is shared everywhere
        builder.Services.AddSingleton<IDatabaseService, DatabaseService>();
        builder.Services.AddSingleton<IAnalyticsService, AnalyticsService>();
        builder.Services.AddSingleton<IPdfExportService, PdfExportService>();
        builder.Services.AddSingleton<ISecurityService, SecurityService>();

        return builder.Build();
    }
}
