using Windows.Storage.Pickers;

namespace SBSimulatorMaui;

public partial class OnlineMatchUpPage : ContentPage
{
    public static string PlayerName { get; internal set; } = "‚¶‚Ô‚ñ";
    public static Ability PlayerAbility { get; internal set; } = new Debugger();
    public OnlineMatchUpPage()
    {
        InitializeComponent();
        CmdBtnBack.Command = new Command(async () =>
        {
            SBAudioManager.PlaySound("pera");
            await Shell.Current.GoToAsync($"../../{nameof(MainPage)}", false);
        });
    }

    private void BtnBackToMenu_Clicked(object sender, EventArgs e) => CmdBtnBack.Command.Execute(null);

    private async void BtnPlayerNameSetting_Clicked(object sender, EventArgs e)
    {
        SBAudioManager.PlaySound("pera");
        NameSettingPage.Selector = PlayerSelector.None;
        await Shell.Current.GoToAsync(nameof(NameSettingPage));
    }

    private async void BtnPlayerAbilitySetting_Clicked(object sender, EventArgs e)
    {
        SBAudioManager.PlaySound("pera");
        MakeUpAbilityPage.Selector = PlayerSelector.None;
        await Shell.Current.GoToAsync(nameof(MakeUpAbilityPage));
    }

    private void ContentPage_Loaded(object sender, EventArgs e) => RefreshPlayerInfo();
    private void RefreshPlayerInfo()
    {
        LblPlayerName.Text = PlayerName;
        ImgPlayerAbility.Source = PlayerAbility.ImgFile;
    }

    private async void BattleStartBtn_Clicked(object sender, EventArgs e)
    {
        Server.Initialize();
        OnlineBattlePage.InitialPlayer = new(PlayerName, PlayerAbility);
        BattleLoadingPage.IsOnline = true;
        await Shell.Current.GoToAsync(BattleLoadingPage.IsExecuted ? nameof(OnlineBattlePage) : nameof(BattleLoadingPage));
    }
}