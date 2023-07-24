using Plugin.Maui.Audio;

namespace SBSimulatorMaui;

public partial class MainPage : ContentPage
{
    public MainPage()
    {
        InitializeComponent();
        Init();
    }

    private async void Init()
    {
        await SBAudioManager.Init();
        SBAudioManager.PlaySound(SBOptions.MainBgm);
        LblTitle.FontFamily = "MPlus1pBlack";
    }

    private async void BtnStart_Clicked(object sender, EventArgs e)
    {
        SBAudioManager.PlaySound("pera");
        await Shell.Current.GoToAsync(nameof(MatchUpPage));
    }

    private async void BtnOption_Clicked(object sender, EventArgs e)
    {
        SBAudioManager.PlaySound("pera");
        await Shell.Current.GoToAsync(nameof(MainOptionPage));
    }

    private async void BtnOther_Clicked(object sender, EventArgs e)
    {
        SBAudioManager.PlaySound("pera");
        await Shell.Current.GoToAsync(nameof(InfoPage));
    }

    private async void BtnOnline_Clicked(object sender, EventArgs e)
    {
        SBAudioManager.PlaySound("pera");
        await Shell.Current.GoToAsync(nameof(OnlineMatchUpPage));
    }
}