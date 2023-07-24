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
        ShowMessage($"��ǂ肬�̌p���^�[������ {(SwInfiniteSeed.IsToggled ? "����" : $"{Player.MaxSeedTurn}�^�[��")} �ɕύX���܂����B");
    }

    private void SwAbilChange_Toggled(object sender, ToggledEventArgs e)
    {
        if (isBeforeInit) return;
        MatchUpPage.Mode.IsAbilChangeable = SwAbilChange.IsToggled;
        SBAudioManager.PlaySound("concent");
        ShowMessage($"�Ƃ������̕ύX��{(SwAbilChange.IsToggled ? $"�L���ɂ��܂����B(��� {Player.MaxAbilChange}�� �܂�)" : "�����ɂ��܂���")}");
    }

    private void SwInfiniteCure_Toggled(object sender, ToggledEventArgs e)
    {
        if (isBeforeInit) return;
        MatchUpPage.Mode.IsCureInfinite = !SwInfiniteCure.IsToggled;
        SBAudioManager.PlaySound("concent");
        ShowMessage($"��Ã^�C�v�̒P��ŉ񕜉\�ȉ񐔂� {(!SwInfiniteCure.IsToggled ? "����" : $"{Player.MaxCureCount}��")} �ɕύX���܂����B");
    }
}