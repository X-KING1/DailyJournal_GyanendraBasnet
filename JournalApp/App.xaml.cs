using JournalApp.Services;

namespace JournalApp;

public partial class App : Application
{
	private readonly IDatabaseService _databaseService;

	public App(IDatabaseService databaseService)
	{
		InitializeComponent();
		_databaseService = databaseService;
		
		// Initialize database on app startup
		Task.Run(async () => await _databaseService.InitializeDatabaseAsync());
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		return new Window(new MainPage()) { Title = "My Journal" };
	}
}
