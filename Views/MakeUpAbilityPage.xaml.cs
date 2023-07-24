using Microsoft.Maui.Controls.Shapes;
using Plugin.Maui.Audio;

namespace SBSimulatorMaui;

public partial class MakeUpAbilityPage : ContentPage
{
    public static PlayerSelector Selector { get; internal set; } = PlayerSelector.None;
    public MakeUpAbilityPage()
    {
        InitializeComponent();
        Init();
    }
    private void Init()
    {
        InitBtnBackBehavior();
        InitAbilityFlex();
    }
    private void InitBtnBackBehavior()
    {
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
    private void InitAbilityFlex()
    {
        var abilities = SBOptions.AllowCustomAbility && Selector != PlayerSelector.None ? AbilityManager.Abilities : AbilityManager.CanonAbilities;
        foreach (var i in abilities)
        {
            var bdr = new Border
            {
                StyleId = i.ToString(),
                BackgroundColor = Colors.White,
                Stroke = Colors.White,
                StrokeShape = new RoundRectangle { CornerRadius = 20 },
                Margin = new Thickness(5, 5),
                MaximumWidthRequest = 80,
                MaximumHeightRequest = 80
            };
            var img = new ImageButton { Source = i.ImgFile, MaximumWidthRequest = 80, MaximumHeightRequest = 80 };
            bdr.Content = img;
            AbilityFlex.Add(bdr);
        }
        var index = 0;
        foreach (var i in AbilityFlex)
        {
            var bdr = i as Border;
            var btn = bdr.Content as ImageButton;
            var ability = abilities[index];
            btn.Clicked += (sender, e) =>
            {
                ImgAbility.Source = ability.ImgFile;
                LblAbilityName.Text = ability.ToString();
                LblAbilityDesc.Text = ability.Description;
                foreach (var j in AbilityFlex)
                {
                    var b = j as Border;
                    b.Stroke = Colors.Transparent;
                }
                bdr.Stroke = Colors.Red;
                SetAbility(ability);
                ShowLblAbilityChanged();
                SBAudioManager.PlaySound("concent");
            };
            index++;
        }
    }
    private static void SetAbility(Ability ability)
    {
        if (Selector == PlayerSelector.None) OnlineMatchUpPage.PlayerAbility = ability;
        if (Selector == PlayerSelector.Player1) MatchUpPage.Player1Ability = ability;
        if (Selector == PlayerSelector.Player2) MatchUpPage.Player2Ability = ability;
    }

    private async void ContentPage_Loaded(object sender, EventArgs e)
    {
        var ability = Selector == PlayerSelector.Player1 ? MatchUpPage.Player1Ability
                    : Selector == PlayerSelector.Player2 ? MatchUpPage.Player2Ability
                    : new Debugger();
        ImgAbility.Source = ability.ImgFile;
        LblAbilityName.Text = ability.ToString();
        LblAbilityDesc.Text = ability.Description;
        await FocusAbilityAsync(ability);
    }
    private async Task FocusAbilityAsync(Ability ability)
    {
        await Task.Delay(50);
        foreach (var i in AbilityFlex)
        {
            var bdr = i as Border;
            if (bdr.StyleId == ability.ToString()) bdr.Stroke = Colors.Red;
        }
    }
    private async void ShowLblAbilityChanged()
    {
        LblAbilityChanged.Opacity = 1;
        await Task.Delay(1500);
        LblAbilityChanged.Opacity = 0;
    }
}