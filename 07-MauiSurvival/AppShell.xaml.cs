using Survival.Pages;

namespace Survival;

public partial class AppShell : Shell
{
	public AppShell()
	{
		InitializeComponent();

		// Кожен екран гри - окрема сторінка; на неї можна перейти за назвою через GoToAsync
		Routing.RegisterRoute(nameof(HomePage), typeof(HomePage));
		Routing.RegisterRoute(nameof(LocationsPage), typeof(LocationsPage));
		Routing.RegisterRoute(nameof(LocationPage), typeof(LocationPage));
		Routing.RegisterRoute(nameof(InventoryPage), typeof(InventoryPage));
		Routing.RegisterRoute(nameof(SettingsPage), typeof(SettingsPage));
		Routing.RegisterRoute(nameof(ScenePage), typeof(ScenePage));
	}
}
