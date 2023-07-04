namespace SBSimulatorMaui;

public partial class MainOptionPage : ContentPage
{
    readonly bool isBeforeInit = true;
	public MainOptionPage()
	{
		InitializeComponent();
        CmdBtnBack.Command = new Command(async () =>
        {
            SBAudioManager.PlaySound("pera");
            await Shell.Current.GoToAsync($"../../{nameof(MainPage)}", false);
        });
        SwCustomAbil.IsToggled = SBOptions.AllowCustomAbility;
        PkrMainBgm.ItemsSource = new[] { "�唭��", "�d�]���E�ɂ�", "����䂯�����_�[�����h" };
        PkrMainBgm.SelectedIndex = SBOptions.MainBgm switch
        {
            "horizon" => 0,
            "denno" => 1,
            _ => 2
        };
        PkrBattleBgm.ItemsSource = new[] { "�I�[�o�[�t���[", "�j���j���E��", "�X�[�p�[�E�g���C��" };
        PkrBattleBgm.SelectedIndex = SBOptions.BattleBgm switch
        {
            "overflow" => 0,
            "ninja" => 1,
            _ => 2
        };
        isBeforeInit = false;
    }

    private void PkrMainBgm_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (isBeforeInit) return;
        SBAudioManager.StopSound(SBOptions.MainBgm);
        SBAudioManager.PlaySound("concent");
        SBOptions.MainBgm = PkrMainBgm.SelectedIndex switch
        {
            0 => "horizon",
            1 => "denno",
            _ => "wonderland"
        };
        SBAudioManager.PlaySound(SBOptions.MainBgm);
    }
    private static int BattleBgmToIndex(string bgmName)
    {
        return bgmName switch
        {
            "overflow" => 0,
            "ninja" => 1,
            _ => 2
        };
    }

    private void PkrBattleBgm_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (isBeforeInit) return;
        SBAudioManager.PlaySound("concent");
        SBOptions.BattleBgm = PkrBattleBgm.SelectedIndex switch
        {
            0 => "overflow",
            1 => "ninja",
            _ => "last"
        };
    }

    private void SwCustomAbil_Toggled(object sender, ToggledEventArgs e)
    {
        if (isBeforeInit) return;
        SBAudioManager.PlaySound("concent");
        SBOptions.AllowCustomAbility = SwCustomAbil.IsToggled;
        if (SBOptions.AllowCustomAbility) return;
        if (MatchUpPage.Player1Ability is CustomAbility) MatchUpPage.Player1Ability = new Debugger();
        if (MatchUpPage.Player2Ability is CustomAbility) MatchUpPage.Player2Ability = new Debugger();
    }
}