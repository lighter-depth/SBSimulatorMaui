namespace SBSimulatorMaui;

public partial class AppShell : Shell
{
	public AppShell()
	{
		InitializeComponent();
        Routing.RegisterRoute(nameof(MainPage), typeof(MainPage));
        Routing.RegisterRoute(nameof(MainOptionPage), typeof(MainOptionPage));
        Routing.RegisterRoute(nameof(InfoPage), typeof(InfoPage));
        Routing.RegisterRoute(nameof(MatchUpPage), typeof(MatchUpPage));
        Routing.RegisterRoute(nameof(MakeUpAbilityPage), typeof(MakeUpAbilityPage));
        Routing.RegisterRoute(nameof(PlayerMakeUpPage), typeof(PlayerMakeUpPage));
        Routing.RegisterRoute(nameof(ModeMakeUpPage), typeof(ModeMakeUpPage));
        Routing.RegisterRoute(nameof(NameSettingPage), typeof(NameSettingPage));
        Routing.RegisterRoute(nameof(BattlePage), typeof(BattlePage));
        Routing.RegisterRoute(nameof(BattleLoadingPage), typeof(BattleLoadingPage));
        Routing.RegisterRoute(nameof(OnlineMatchUpPage), typeof(OnlineMatchUpPage));
        Routing.RegisterRoute(nameof(OnlineBattlePage), typeof(OnlineBattlePage));
    }
}
