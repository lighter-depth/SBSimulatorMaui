using System.Text.RegularExpressions;

namespace SBSimulatorMaui;

public partial class NameSettingPage : ContentPage
{
    public static PlayerSelector Selector { get; internal set; } = PlayerSelector.None;
    public NameSettingPage()
	{
		InitializeComponent();
        CmdBtnBack.Command = new Command(async () =>
        {
            while (Navigation.NavigationStack.Count > 2)
            {
                Navigation.RemovePage(Navigation.NavigationStack[1]);
            }
            SBAudioManager.PlaySound("pera");
            await Shell.Current.GoToAsync($"../{(Selector == PlayerSelector.None ? nameof(OnlineMatchUpPage) : nameof(MatchUpPage))}", false);
        });
    }

    private void BtnNameRegister_Clicked(object sender, EventArgs e)
    {
        var name = NameEntry.Text;
        if (string.IsNullOrWhiteSpace(name)) return;
        if (name.Length > 8)
        {
            ShowMessage("‚W•¶ŽšˆÈ“à‚É‚µ‚Ä‚­‚¾‚³‚¢");
            return;
        }
        if (!new Regex("^[\u3040-\u30FF]+$").IsMatch(name))
        {
            ShowMessage("‚Ð‚ç‚ª‚È‚©ƒJƒ^ƒJƒi‚É‚µ‚Ä‚­‚¾‚³‚¢");
            return;
        }
        ShowMessage("“o˜^‚µ‚Ü‚µ‚½");
        SetName(name);
    }

    private void Entry_Completed(object sender, EventArgs e)
    {
        BtnNameRegister_Clicked(sender, e);
    }
    private void ShowMessage(string message)
    {
        LblNameChanged.Text = message;
        LblNameChanged.Opacity = 1;
    }
    private static void SetName(string name)
    {
        if (Selector == PlayerSelector.None) OnlineMatchUpPage.PlayerName = name;
        if (Selector == PlayerSelector.Player1) MatchUpPage.Player1Name = name;
        if (Selector == PlayerSelector.Player2) MatchUpPage.Player2Name = name;
    }
}