namespace SBSimulatorMaui;

public partial class ModeMakeUpPage : ContentPage
{
    bool isBeforeInit = true;
	public ModeMakeUpPage()
	{
		InitializeComponent();
        CmdBtnBack.Command = new Command(async () =>
        {
            while (Navigation.NavigationStack.Count > 2)
            {
                Navigation.RemovePage(Navigation.NavigationStack[1]);
            }
            SBAudioManager.PlaySound("pera");
            await Shell.Current.GoToAsync($"../{nameof(MatchUpPage)}", false);
        });
        InitControls();
        isBeforeInit = false;
    }
    private void InitControls()
    {
        SwInfiniteSeed.IsToggled = MatchUpPage.Mode.IsSeedInfinite;
        SwAbilChange.IsToggled = MatchUpPage.Mode.IsAbilChangeable;
        SwInfiniteCure.IsToggled = !MatchUpPage.Mode.IsCureInfinite;
    }
    private void ShowMessage(string message)
    {
        LblMessage.Text = message;
        LblMessage.Opacity = 1;
    }

    private void SwInfiniteSeed_Toggled(object sender, ToggledEventArgs e)
    {
        if (isBeforeInit) return;
        MatchUpPage.Mode.IsSeedInfinite = SwInfiniteSeed.IsToggled;
        SBAudioManager.PlaySound("concent");
        ShowMessage($"やどりぎの継続ターン数を {(SwInfiniteSeed.IsToggled ? "無限" : $"{Player.MaxSeedTurn}ターン")} に変更しました。");
    }

    private void SwAbilChange_Toggled(object sender, ToggledEventArgs e)
    {
        if (isBeforeInit) return;
        MatchUpPage.Mode.IsAbilChangeable = SwAbilChange.IsToggled;
        SBAudioManager.PlaySound("concent");
        ShowMessage($"とくせいの変更を{(SwAbilChange.IsToggled ? $"有効にしました。(上限 {Player.MaxAbilChange}回 まで)" : "無効にしました")}");
    }

    private void SwInfiniteCure_Toggled(object sender, ToggledEventArgs e)
    {
        if (isBeforeInit) return;
        MatchUpPage.Mode.IsCureInfinite = !SwInfiniteCure.IsToggled;
        SBAudioManager.PlaySound("concent");
        ShowMessage($"医療タイプの単語で回復可能な回数を {(!SwInfiniteCure.IsToggled ? "無限" : $"{Player.MaxCureCount}回")} に変更しました。");
    }
}