namespace SBSimulatorMaui;

public partial class InfoPage : ContentPage
{
	public InfoPage()
	{
		InitializeComponent();
        CmdBtnBack.Command = new Command(async () =>
        {
            SBAudioManager.PlaySound("pera");
            await Shell.Current.GoToAsync($"../../{nameof(MainPage)}", false);
        });
        LblVersion.Text = SBOptions.Version;
    }

    private async void BtnYouTube_Clicked(object sender, EventArgs e)
    {
        await Browser.OpenAsync("https://www.youtube.com/@lighter_depth");
    }

    private async void BtnTwitter_Clicked(object sender, EventArgs e)
    {
        await Browser.OpenAsync("https://twitter.com/lighter_depth");
    }

    private async void BtnOriginal_Clicked(object sender, EventArgs e)
    {
        await Browser.OpenAsync("http://siritori-battle.net");
    }
}