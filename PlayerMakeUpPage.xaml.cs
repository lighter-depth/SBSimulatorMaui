using System.Xml.Linq;

namespace SBSimulatorMaui;

public partial class PlayerMakeUpPage : ContentPage
{
    public static PlayerSelector Selector { get; internal set; } = PlayerSelector.None;
    readonly bool isBeforeInit = true;
    int maxHPBuf = 0;
    public PlayerMakeUpPage()
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
        InitTexts();
        isBeforeInit = false;
    }
    private void InitTexts()
    {
        PkrProceeds.ItemsSource = new List<string> { "ÉâÉìÉ_ÉÄ", "êÊçU", "å„çU" };
        PkrLuck.ItemsSource = new List<string> { "çKâ^", "ïÅí ", "ïsâ^" };
        LblPlayerName.Text = Selector == PlayerSelector.Player1 ? MatchUpPage.Player1Name : MatchUpPage.Player2Name;
        ImgAbility.Source = Selector == PlayerSelector.Player1 ? MatchUpPage.Player1Ability.ImgFile : MatchUpPage.Player2Ability.ImgFile;
        LblPlayerKind.Text = Selector == PlayerSelector.Player1 ? MatchUpPage.Player1Kind : MatchUpPage.Player2Kind;
        var proceeds = Selector == PlayerSelector.Player1 ? MatchUpPage.Mode.Player1Proceeds : MatchUpPage.Mode.Player2Proceeds;
        PkrProceeds.SelectedIndex = proceeds switch
        {
            Proceeds.Random => 0,
            Proceeds.True => 1,
            _ => 2
        };
        maxHPBuf = Selector == PlayerSelector.Player1 ? MatchUpPage.Mode.Player1MaxHP : MatchUpPage.Mode.Player2MaxHP;
        MaxHPEntry.Text = maxHPBuf.ToString();
        var luck = Selector == PlayerSelector.Player1 ? MatchUpPage.Mode.Player1Luck : MatchUpPage.Mode.Player2Luck;
        PkrLuck.SelectedIndex = luck switch
        {
            Luck.Lucky => 0,
            Luck.Normal => 1,
            _ => 2
        };
    }
    private void ShowMessage(string message)
    {
        LblMessage.Text = message;
        LblMessage.Opacity = 1;
    }

    private void PkrProceeds_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (isBeforeInit) return;
        SBAudioManager.PlaySound("concent");
        var proceeds = PkrProceeds.SelectedIndex switch
        {
            0 => Proceeds.Random,
            1 => Proceeds.True,
            _ => Proceeds.False
        };
        if (Selector == PlayerSelector.Player1) MatchUpPage.Mode.Player1Proceeds = proceeds;
        if (Selector == PlayerSelector.Player2) MatchUpPage.Mode.Player2Proceeds = proceeds;
        var name = LblPlayerName.Text;
        ShowMessage($"{name} ÇÃêÊçUå„çUê›íËÇ {PkrProceeds.SelectedItem as string} Ç…ê›íËÇµÇ‹ÇµÇΩ");
        MatchUpPage.CustomFlag = true;
    }

    private void MaxHPEntry_Focused(object sender, FocusEventArgs e)
    {
        BdrMaxHPPlus.IsVisible = BdrMaxHPMinus.IsVisible = BdrMaxHPPlus10.IsVisible = BdrMaxHPMinus10.IsVisible = false;
    }

    private void MaxHPEntry_Unfocused(object sender, FocusEventArgs e)
    {
        BdrMaxHPPlus.IsVisible = BdrMaxHPMinus.IsVisible = BdrMaxHPPlus10.IsVisible = BdrMaxHPMinus10.IsVisible = true;
    }
    private void SetMaxHP()
    {
        if (Selector == PlayerSelector.Player1) MatchUpPage.Mode.Player1MaxHP = maxHPBuf;
        if (Selector == PlayerSelector.Player2) MatchUpPage.Mode.Player2MaxHP = maxHPBuf;
        var name = LblPlayerName.Text;
        ShowMessage($"{name} ÇÃç≈ëÂëÃóÕÇ {maxHPBuf} Ç…ê›íËÇµÇ‹ÇµÇΩ");
        MatchUpPage.CustomFlag = true;
        SBAudioManager.PlaySound("concent");
    }

    private void MaxHPEntry_Completed(object sender, EventArgs e)
    {
        if(!int.TryParse(MaxHPEntry.Text, out var maxHP)) return;
        var maxHPResult = Math.Max(maxHP, 1);
        maxHPBuf = maxHPResult;
        MaxHPEntry.Text = maxHPBuf.ToString();
        SetMaxHP();
    }

    private void BtnMaxHPPlus_Clicked(object sender, EventArgs e)
    {
        MaxHPEntry.Text = (++maxHPBuf).ToString();
        SetMaxHP();
    }

    private void BtnMaxHPMinus_Clicked(object sender, EventArgs e)
    {
        MaxHPEntry.Text = (maxHPBuf = Math.Max(maxHPBuf - 1, 1)).ToString();
        SetMaxHP();
    }

    private void BtnMaxHPPlus10_Clicked(object sender, EventArgs e)
    {
        MaxHPEntry.Text = (maxHPBuf += 10).ToString();
        SetMaxHP();
    }

    private void BtnMaxHPMinus10_Clicked(object sender, EventArgs e)
    {
        MaxHPEntry.Text = (maxHPBuf = Math.Max(maxHPBuf - 10, 1)).ToString();
        SetMaxHP();
    }

    private void PkrLuck_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (isBeforeInit) return;
        SBAudioManager.PlaySound("concent");
        var luck = PkrLuck.SelectedIndex switch
        {
            0 => Luck.Lucky,
            1 => Luck.Normal,
            _ => Luck.UnLucky
        };
        if (Selector == PlayerSelector.Player1) MatchUpPage.Mode.Player1Luck = luck;
        if (Selector == PlayerSelector.Player2) MatchUpPage.Mode.Player2Luck = luck;
        var name = LblPlayerName.Text;
        ShowMessage($"{name} ÇÃLuckílÇ {PkrLuck.SelectedItem as string} Ç…ê›íËÇµÇ‹ÇµÇΩ");
        MatchUpPage.CustomFlag = true;
    }
}