using MemoryGame.Pages;

namespace MemoryGame;

public partial class AppShell : Shell
{
	public AppShell()
	{
		InitializeComponent();

		// Реєструємо маршрути додаткових екранів: так на них можна перейти за назвою через GoToAsync.
		Routing.RegisterRoute(nameof(LevelPage), typeof(LevelPage));
		Routing.RegisterRoute(nameof(GamePage), typeof(GamePage));
	}
}
