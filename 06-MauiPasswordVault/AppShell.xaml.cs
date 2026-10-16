using PassVault.Pages;

namespace PassVault;

public partial class AppShell : Shell
{
	public AppShell()
	{
		InitializeComponent();

		// Екран додавання/редагування відкривається за назвою через GoToAsync
		Routing.RegisterRoute(nameof(EntryPage), typeof(EntryPage));
	}
}
