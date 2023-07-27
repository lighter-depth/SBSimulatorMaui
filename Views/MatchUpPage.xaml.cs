namespace SBSimulatorMaui;

public partial class MatchUpPage : ContentPage
{
    public static string Player1Name { get; internal set; } = "Ç∂Ç‘ÇÒ";
    public static Ability Player1Ability { get; internal set; } = new Debugger();
    public static string Player2Name { get; internal set; } = "Ç†Ç¢Çƒ";
    public static Ability Player2Ability { get;  internal set; } = new Debugger();
    public static string Player1Kind => cpuNames[pkrPlayer1Index];
    public static string Player2Kind => cpuNames[pkrPlayer2Index];
    public static bool CustomFlag { get; internal set; } = false;
    readonly bool isPickerChangingIsFirst = true;
    static int modePkrIndex = 0;
    static int pkrPlayer1Index = 0;
    static int pkrPlayer2Index = 0;
    static readonly List<string> cpuNames = new();
    public static Mode Mode { get; internal set; } = new();
	public MatchUpPage()
	{
		InitializeComponent();
        CmdBtnBack.Command = new Command(async () =>
        {
            SBAudioManager.PlaySound("pera");
            await Shell.Current.GoToAsync($"../../{nameof(MainPage)}", false);
        });
        ModePicker.ItemsSource = ModeFactory.ModeNames;
        if (CustomFlag) modePkrIndex = ModePicker.ItemsSource.Count - 1;
        ModePicker.SelectedIndex = modePkrIndex;
        var cpuItems = new List<string> { "êlä‘" };
        foreach (var i in CPUManager.CPUPlayers)
        {
            cpuItems.Add($"CPU({i.CPUName})");
        }
        cpuItems.ForEach(cpuNames.Add);
        PkrPlayer1.ItemsSource = cpuItems;
        PkrPlayer1.SelectedIndex = pkrPlayer1Index;
        PkrPlayer2.ItemsSource = cpuItems;
        PkrPlayer2.SelectedIndex = pkrPlayer2Index;
        isPickerChangingIsFirst = false;
    }
    private async void BattleStartBtn_Clicked(object sender, EventArgs e)
    {
        BattlePage.InitialPlayer1 = PkrPlayer1.SelectedIndex == 0 ? new Player(Player1Name, Player1Ability) : CPUManager.Create(Player1Name);
        BattlePage.InitialPlayer2 = PkrPlayer2.SelectedIndex == 0 ? new Player(Player2Name, Player2Ability) : CPUManager.Create(Player2Name);
        BattlePage.Mode = Mode;
        BattleLoadingPage.IsOnline = false;
        await Shell.Current.GoToAsync(BattleLoadingPage.IsExecuted ? nameof(BattlePage) : nameof(BattleLoadingPage));
    }

    private void ModePicker_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (isPickerChangingIsFirst) return;
        CustomFlag = false;
        SBAudioManager.PlaySound("concent");
        modePkrIndex = ModePicker.SelectedIndex;
        Mode = ModeFactory.Create(ModeFactory.ModeNames[ModePicker.SelectedIndex]).Mode;
        if (ModePicker.SelectedIndex == ModeFactory.ModeNames.Length - 1) Mode = new();
    }

    private async void BtnPlayer1AbilitySetting_Clicked(object sender, EventArgs e)
    {
        SBAudioManager.PlaySound("pera");
        MakeUpAbilityPage.Selector = PlayerSelector.Player1;
        await Shell.Current.GoToAsync(nameof(MakeUpAbilityPage));
    }

    private async void BtnPlayer2AbilitySetting_Clicked(object sender, EventArgs e)
    {
        SBAudioManager.PlaySound("pera");
        MakeUpAbilityPage.Selector = PlayerSelector.Player2;
        await Shell.Current.GoToAsync(nameof(MakeUpAbilityPage));
    }

    private void ContentPage_Loaded(object sender, EventArgs e)
    {
        RefreshPlayerInfo();
    }

    private async void BtnPlayer1NameSetting_Clicked(object sender, EventArgs e)
    {
        SBAudioManager.PlaySound("pera");
        NameSettingPage.Selector = PlayerSelector.Player1;
        await Shell.Current.GoToAsync(nameof(NameSettingPage));
    }

    private async void BtnPlayer2NameSetting_Clicked(object sender, EventArgs e)
    {
        SBAudioManager.PlaySound("pera");
        NameSettingPage.Selector = PlayerSelector.Player2;
        await Shell.Current.GoToAsync(nameof(NameSettingPage));
    }

    private void PkrPlayer1_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (!isPickerChangingIsFirst) SBAudioManager.PlaySound("concent");
        pkrPlayer1Index = PkrPlayer1.SelectedIndex;
        if (PkrPlayer1.SelectedIndex == 0)
        {
            BtnPlayer1NameSetting.IsEnabled = true;
            BtnPlayer1AbilitySetting.IsEnabled = true;
            return;
        }
        BtnPlayer1NameSetting.IsEnabled = false;
        BtnPlayer1AbilitySetting.IsEnabled = false;
        var cpu = CPUManager.CPUPlayers[PkrPlayer1.SelectedIndex - 1];
        Player1Name = cpu.CPUName;
        Player1Ability = cpu.FirstAbility;
        RefreshPlayerInfo();
    }
    private void PkrPlayer2_SelectedIndexChanged(object sender, EventArgs e)
    {
        if(!isPickerChangingIsFirst) SBAudioManager.PlaySound("concent");
        pkrPlayer2Index = PkrPlayer2.SelectedIndex;
        if (PkrPlayer2.SelectedIndex == 0)
        {
            BtnPlayer2NameSetting.IsEnabled = true;
            BtnPlayer2AbilitySetting.IsEnabled = true;
            return;
        }
        BtnPlayer2NameSetting.IsEnabled = false;
        BtnPlayer2AbilitySetting.IsEnabled = false;
        var cpu = CPUManager.CPUPlayers[PkrPlayer2.SelectedIndex - 1];
        Player2Name = cpu.CPUName;
        Player2Ability = cpu.FirstAbility;
        RefreshPlayerInfo();
    }
    private void RefreshPlayerInfo()
    {
        LblPlayer1Name.Text = Player1Name;
        ImgPlayer1Ability.Source = Player1Ability.ImgFile;
        LblPlayer2Name.Text = Player2Name;
        ImgPlayer2Ability.Source = Player2Ability.ImgFile;
    }


    private async void BtnPlayer1Detail_Clicked(object sender, EventArgs e)
    {
        SBAudioManager.PlaySound("pera");
        PlayerMakeUpPage.Selector = PlayerSelector.Player1;
        await Shell.Current.GoToAsync(nameof(PlayerMakeUpPage));
    }

    private async void BtnPlayer2Detail_Clicked(object sender, EventArgs e)
    {
        SBAudioManager.PlaySound("pera");
        PlayerMakeUpPage.Selector = PlayerSelector.Player2;
        await Shell.Current.GoToAsync(nameof(PlayerMakeUpPage));
    }

    private async void BtnModeDetail_Clicked(object sender, EventArgs e)
    {
        SBAudioManager.PlaySound("pera");
        await Shell.Current.GoToAsync(nameof(ModeMakeUpPage));
    }
}