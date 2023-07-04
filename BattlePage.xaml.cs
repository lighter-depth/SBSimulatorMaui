using Microsoft.Maui.Controls.Shapes;
using SkiaSharp;
using SkiaSharp.Views.Maui.Controls;
using System;

namespace SBSimulatorMaui;

public partial class BattlePage : ContentPage
{
    public static Mode Mode { get; internal set; } = new();
    public static Player Player1 { get; internal set; } = new("じぶん", new Debugger());
    public static Player Player2 { get; internal set; } = new("あいて", new Debugger());
    Battle Battle;
    Order OrderBuffer = new();
    readonly Action<Order, CancellationTokenSource> EmptyDelegate = (o, c) => { };
    static readonly Dictionary<Notice, string> SoundDic = new()
    {
        [Notice.Buf] = "up",
        [Notice.Debuf] = "down",
        [Notice.RevInfo] = "pera",
        [Notice.EffectiveProp] = "effective",
        [Notice.MidDmgProp] = "middmg",
        [Notice.NonEffectiveProp] = "noneffective",
        [Notice.Heal] = "heal",
        [Notice.Poison] = "poison",
        [Notice.PoisonDmg] = "poison",
        [Notice.PoisonHeal] = "poison_heal",
        [Notice.Seed] = "seeded",
        [Notice.SeedDmg] = "seed_damage",
        [Notice.InvokeBufInfo] = "up"
    };
    bool isOrdered = false;
    bool deadFlag = false;
    bool isBeforeInit = true;
    bool isPkrPlayerLuckInitializing = false;
    string orderedAbilityName = string.Empty;
    PlayerSelector currentPlayerOptionModalSelector = PlayerSelector.Player1;

    public BattlePage()
    {
        InitializeComponent();
        InitPlayerNameText();
        InitBtnBackBehavior();
        InitOptionModal();
        InitBattle();
        InitBattleTexts();
        InitAbilityModal();
        isBeforeInit = false;
        Battle.Run();
    }
    private void InitPlayerNameText()
    {
        LblAllyName.Text = Player1.Name;
        LblFoeName.Text = Player2.Name;
        if (Player1 is CPUPlayer) BdrAllyCPU.IsVisible = true;
        if (Player2 is CPUPlayer) BdrFoeCPU.IsVisible = true;
    }
    private void InitBtnBackBehavior()
    {
        CmdBtnBack.Command = new Command(async () =>
        {
            SBAudioManager.StopSound(SBOptions.BattleBgm);
            Battle.Dispose();
            SBAudioManager.CancelAudio();
            while (Navigation.NavigationStack.Count > 2)
            {
                Navigation.RemovePage(Navigation.NavigationStack[1]);
            }
            await Shell.Current.GoToAsync($"../{nameof(MatchUpPage)}", false);

            // horizon 再生開始用の遅延
            await Task.Delay(500);
            SBAudioManager.PlaySound(SBOptions.MainBgm);
        });
    }
    private void InitBattle()
    {
        Battle = new(Player1.Clone(), Player2.Clone())
        {
            In = In,
            Out = Out,
            OnShowOrdered = EmptyDelegate,
            OnResetOrdered = EmptyDelegate,
            OnHelpOrdered = EmptyDelegate,
            OnAddOrdered = EmptyDelegate,
            OnExitOrdered = EmptyDelegate,
            OnRemoveOrdered = EmptyDelegate,
            OnSearchOrdered = EmptyDelegate,
            OnReset = cts => cts.Cancel()
        };
        Battle.Player1.Register(Battle);
        Battle.Player2.Register(Battle);
        Mode.Set(Battle);
    }
    private async void InitBattleTexts()
    {
        // この遅延がないと初期化がうまくいかない
        await Task.Delay(50);

        ReloadHPTexts();
        LblAuxInfo.Text = Battle.IsPlayer1sTurn ? $"{Battle.Player1.Name} のターンです" : $"{Battle.Player2.Name} のターンです";
    }
    private void ReloadHPTexts()
    {
        LblAllyHP.Text = $"{Battle.Player1.HP}/{Battle.Player1.MaxHP}";
        LblFoeHP.Text = $"{Battle.Player2.HP}/{Battle.Player2.MaxHP}";
    }
    private void InitAbilityModal()
    {
        var abilities = SBOptions.AllowCustomAbility ? AbilityManager.Abilities : AbilityManager.CanonAbilities;
        foreach (var i in abilities)
        {
            var bdrFrame = new Border
            {
                StyleId = i.ToString(),
                Background = Color.FromArgb("EFE8E8"),
                WidthRequest = 70,
                HeightRequest = 75,
                Stroke = Colors.Transparent,
                StrokeShape = new RoundRectangle { CornerRadius = 10 },
                Margin = new Thickness(1, 1, 1, 1)
            };
            var grid = new Grid
            {
                VerticalOptions = LayoutOptions.Start,
                WidthRequest = 70,
                HeightRequest = 75
            };
            var btn = new ImageButton
            {
                WidthRequest = 60,
                HeightRequest = 60,
                Source = i.ImgFile,
                Background = Colors.Transparent,
                VerticalOptions = LayoutOptions.Start
            };
            var bdrName = new Border
            {
                VerticalOptions = LayoutOptions.End,
                Background = Colors.White,
                Stroke = Colors.Transparent,
                StrokeShape = new RoundRectangle { CornerRadius = 20 }
            };
            var lbl = new Label
            {
                VerticalOptions = LayoutOptions.Center,
                HorizontalOptions = LayoutOptions.Center,
                HorizontalTextAlignment = TextAlignment.Center,
                FontFamily = "MPlus1pRegular",
                FontSize = 10,
                Text = i.ToString(),
                LineBreakMode = LineBreakMode.NoWrap
            };
            bdrName.Content = lbl;
            grid.Add(btn);
            grid.Add(bdrName);
            bdrFrame.Content = grid;
            FlexChangingAbilityModal.Add(bdrFrame);
        }
        PkrPlayerAbilityModal.ItemsSource = new List<string> { "現在のプレイヤー", "自分", "相手" };
        PkrPlayerAbilityModal.SelectedIndex = 0;
        var index = 0;
        foreach (var i in FlexChangingAbilityModal)
        {
            var bdr = i as Border;
            var grid = bdr.Content as Grid;
            var btn = grid[0] as ImageButton;
            var ability = abilities[index];
            btn.Clicked += (sender, e) =>
            {
                var selector = PkrPlayerAbilityModal.SelectedIndex switch
                {
                    1 => PlayerSelector.Player1,
                    2 => PlayerSelector.Player2,
                    _ => Battle.IsPlayer1sTurn ? PlayerSelector.Player1 : PlayerSelector.Player2
                };
                OrderChangeAbility(ability.ToString(), selector);
            };
            index++;
        }
        ReloadAbilityModalTexts();
    }
    private void InitOptionModal()
    {
        SwInfiniteSeed.IsToggled = Mode.IsSeedInfinite;
        SwInfiniteCure.IsToggled = Mode.IsCureInfinite;
        SwAbilChange.IsToggled = Mode.IsAbilChangeable;
        MaxCureEntry.Text = Mode.MaxCureCount.ToString();
        CritEntry.Text = Mode.CritDmg.ToString();
        MaxSeedEntry.Text = Mode.MaxSeedTurn.ToString();
        MaxAbilEntry.Text = Mode.MaxAbilChange.ToString();
        SeedDmgEntry.Text = Mode.SeedDmg.ToString();
        MaxFoodEntry.Text = Mode.MaxFoodCount.ToString();
        InsBufEntry.Text = Mode.InsBufQty.ToString();
        MaxSeedTurnSetter.IsVisible = !SwInfiniteSeed.IsToggled;
        MaxAbilChangeSetter.IsVisible = SwAbilChange.IsToggled;
        MaxCureCountSetter.IsVisible = !SwInfiniteCure.IsToggled;
        PkrPlayerLuck.ItemsSource = new List<string> { "幸運", "普通", "不運" };
        PkrPlayerLuck.SelectedIndex = 1;
    }

    private void BtnRegisterWord_Clicked(object sender, EventArgs e)
    {
        SetOrder(new(WordEntry.Text));
        WordEntry.Text = string.Empty;
    }
    private void WordEntry_Completed(object sender, EventArgs e)
    {
        BtnRegisterWord_Clicked(sender, e);
    }
    private Order In()
    {
        while (true)
        {
            if (isOrdered) break;
        }
        isOrdered = false;
        return OrderBuffer;
    }
    private async Task Out(List<AnnotatedString> list)
    {
        ReloadSituationModal();
        ReloadPlayerOptionModal(currentPlayerOptionModalSelector);
        SetBdrStrokeAbilityModal();
        if (Battle.CurrentOrderType == OrderType.Action) await OutActionOrder(list);
        if (Battle.CurrentOrderType == OrderType.Change) OutChangeOrder(list);
        if (Battle.CurrentOrderType is OrderType.EnablerOption
                                    or OrderType.ModeOption
                                    or OrderType.NumOption
                                    or OrderType.PlayerNumOption
                                    or OrderType.PlayerStringOption) await OutOptionOrder(list);
    }

    private async Task OutActionOrder(List<AnnotatedString> list)
    {
        LblEntryHider.Text = string.Empty;
        if (deadFlag) return;
        WordEntry.IsEnabled = false;
        BtnRegisterWord.IsEnabled = false;
        WordEntry.Opacity = 0;
        BdrWordEntry.Opacity = 0;
        BtnRegisterWord.Opacity = 0;
        LblAuxInfo.Opacity = 0;
        LblAuxInfo.Text = Battle.IsPlayer1sTurn ? $"{Battle.Player1.Name} のターンです" : $"{Battle.Player2.Name} のターンです";
        if (list.Select(x => x.Notice).Contains(Notice.DeathInfo)) deadFlag = true;
        foreach (var i in list)
            if (i.Notice == Notice.Warn)
            {
                LblAuxInfo.Opacity = 1;
                LblAuxInfo.Text = i.Text;
                WordEntry.IsEnabled = true;
                BtnRegisterWord.IsEnabled = true;
                WordEntry.Opacity = 1;
                BdrWordEntry.Opacity = 1;
                BtnRegisterWord.Opacity = 1;
                WordEntry.Focus();
                return;
            }
        var msgList = GetMessages(list);
        if (!(deadFlag ^ Battle.IsPlayer1sTurn)) await ShowWordAsync(AllyWord, GfxAllyWord, ImgAllyType1, BdrAllyType1, LblAllyType1, ImgAllyType2, BdrAllyType2, LblAllyType2);
        else await ShowWordAsync(FoeWord, GfxFoeWord, ImgFoeType1, BdrFoeType1, LblFoeType1, ImgFoeType2, BdrFoeType2, LblFoeType2);
        BoxEntryHider.Opacity = 1;
        LblEntryHider.Opacity = 1;

        // 単語表示 → メッセージ表示 のテンポを整えるための遅延
        await Task.Delay(900);

        await ShowMessageAsync(LblEntryHider, msgList);

        if (!deadFlag && Battle.CurrentPlayer is not CPUPlayer)
        {
            BoxEntryHider.Opacity = 0;
            LblEntryHider.Opacity = 0;
            LblAuxInfo.Opacity = 1;
            WordEntry.IsEnabled = true;
            BtnRegisterWord.IsEnabled = true;
            WordEntry.Opacity = 1;
            BdrWordEntry.Opacity = 1;
            BtnRegisterWord.Opacity = 1;
            WordEntry.Placeholder = $"「{Battle.NextChar}」からはじまることば";
            WordEntry.Focus();
        }
    }
    private void OutChangeOrder(List<AnnotatedString> list)
    {
        foreach (var i in list)
            if (i.Notice is Notice.Warn or Notice.Caution)
            {
                ShowLblChangedAbilityModal(i.Text);
                return;
            }
        SetBdrStrokeAbilityModal();
        ShowLblChangedAbilityModal("とくせいを変更した！");
        if (!isBeforeInit) SBAudioManager.PlaySound("concent");
        ReloadAbilityModalTexts();
    }
    private async Task OutOptionOrder(List<AnnotatedString> list)
    {
        await UpdateAllHPBarsAsync(Battle.Player1.HP, Battle.Player2.HP, 0);
        var result = new List<string>();
        foreach (var i in list)
        {
            if (i.Notice is Notice.Warn or Notice.Caution)
            {
                ShowLblSetOptionModal(i.Text);
                return;
            }
            if (i.Notice is Notice.SettingInfo) result.Add(i.Text);
        }
        if (result.Count > 0) ShowLblSetOptionModal(result[0]);
        if (!isBeforeInit) SBAudioManager.PlaySound("concent");
    }
    private void ReloadAbilityModalTexts()
    {
        LblCountAbilityModal.Text = "タップしてとくせいを変える";
        if (Battle.IsAbilChangeable) LblCountAbilityModal.Text +=
                $"(あと{PkrPlayerAbilityModal.SelectedIndex switch
                {
                    1 => Battle.Player1.RemainingAbilChangingCount,
                    2 => Battle.Player2.RemainingAbilChangingCount,
                    _ => Battle.IsPlayer1sTurn ? Battle.Player1.RemainingAbilChangingCount : Battle.Player2.RemainingAbilChangingCount
                }}回)";
        LblAllyAbilityNameAbilityModal.Text = Battle.Player1.Ability.ToString();
        LblFoeAbilityNameAbilityModal.Text = Battle.Player2.Ability.ToString();
        LblAllyAbilityDescAbilityModal.Text = Battle.Player1.Ability.Description;
        LblFoeAbilityDescAbilityModal.Text = Battle.Player2.Ability.Description;
    }

    private static List<AnnotatedString> GetMessages(List<AnnotatedString> list)
    {
        var filter = new List<Notice>()
        {
            Notice.HPUpdated, Notice.NoDmgProp, Notice.NonEffectiveProp, Notice.MidDmgProp, Notice.EffectiveProp, Notice.CritInfo,
            Notice.PoisonHeal,Notice.Heal, Notice.Buf, Notice.Debuf, Notice.RevInfo,Notice.Poison, Notice.PoisonDmg, Notice.Seed,
            Notice.SeedDmg, Notice.InvokeBufInfo, Notice.Caution, Notice.DeathInfo
        };
        return list.Where(x => filter.Contains(x.Notice)).ToList();
    }
    private async Task ShowMessageAsync(Label label, List<AnnotatedString> msgs)
    {
        var tasks = new List<Task>();
        foreach (var msg in msgs)
        {
            if (!msg.IsInvisible) label.Text = msg.Text;
            if (SoundDic.TryGetValue(msg.Notice, out var soundName)) SBAudioManager.PlaySound(soundName);
            if (msg.Notice == Notice.HPUpdated)
            {
                tasks.Add(UpdateAllHPBarsAsync(msg.Params[0], msg.Params[1], 800));
                continue;
            }
            if (msg.Notice == Notice.Buf)
            {
                var selector = !Battle.IsPlayer1sTurn ? PlayerSelector.Player1 : PlayerSelector.Player2;
                tasks.Add(ShowEffectAsync(selector, "red.gif"));
            }
            if (msg.Notice == Notice.Debuf)
            {
                var selector = !Battle.IsPlayer1sTurn ? PlayerSelector.Player1 : PlayerSelector.Player2;
                tasks.Add(ShowEffectAsync(selector, "blue.gif"));
            }
            if (msg.Notice == Notice.Heal)
            {
                var selector = !Battle.IsPlayer1sTurn ? PlayerSelector.Player1 : PlayerSelector.Player2;
                tasks.Add(ShowEffectAsync(selector, "heal.gif"));
            }
            if (msg.Notice == Notice.InvokeBufInfo)
            {
                var selector = !Battle.IsPlayer1sTurn ? PlayerSelector.Player2 : PlayerSelector.Player1;
                tasks.Add(ShowEffectAsync(selector, "red.gif"));
            }
            if (msg.Notice == Notice.Poison)
            {
                if (!Battle.IsPlayer1sTurn) (msg.Params[0] == 0 ? BdrAllyPoison : BdrFoePoison).Opacity = 1;
                else (msg.Params[0] == 0 ? BdrFoePoison : BdrAllyPoison).Opacity = 1;
            }
            if (msg.Notice == Notice.PoisonHeal)
            {
                if (!Battle.IsPlayer1sTurn) (msg.Params[0] == 0 ? BdrAllyPoison : BdrFoePoison).Opacity = 0;
                else (msg.Params[0] == 0 ? BdrFoePoison : BdrAllyPoison).Opacity = 0;
            }
            if (msg.Notice == Notice.DeathInfo)
            {
                await Task.Delay(500);
                SBAudioManager.PlaySound("end");
                SBAudioManager.StopSound("overflow");
            }
            tasks.Add(Task.Delay(900));
            await Task.WhenAll(tasks);
        }
    }
    private async Task UpdateAllHPBarsAsync(int player1HP, int player2HP, uint length)
    {
        LblAllyHP.Text = $"{player1HP}/{Battle.Player1.MaxHP}";
        LblFoeHP.Text = $"{player2HP}/{Battle.Player2.MaxHP}";
        await Task.WhenAll(AnimateHPBarAsync(AllyHPBarRemain, AllyHPBarAll, (double)player1HP / Battle.Player1.MaxHP, length),
                           AnimateHPBarAsync(FoeHPBarRemain, FoeHPBarAll, (double)player2HP / Battle.Player2.MaxHP, length));
    }
    private static async Task AnimateHPBarAsync(BoxView hpRemain, BoxView hpAll, double prop, uint length)
    {
        await hpRemain.ScaleXTo(hpAll.ScaleX * 2 * prop, length, Easing.CubicOut);
        if (prop > 0.5) hpRemain.Color = hpRemain.BackgroundColor = Color.FromArgb("00DB0E");
        if (prop <= 0.5) hpRemain.Color = hpRemain.BackgroundColor = Color.FromArgb("E1B740");
        if (prop <= 0.2) hpRemain.Color = hpRemain.BackgroundColor = Color.FromArgb("B84731");
    }
    private async Task ShowWordAsync(Grid wordGrid, SKCanvasView wordGfx, Image type1Img, Border type1Bdr, Label type1Lbl, Image type2Img, Border type2Bdr, Label type2Lbl)
    {
        await wordGrid.FadeTo(0, 200, Easing.Linear);
        wordGfx.InvalidateSurface();
        type1Img.Opacity = 1;
        type1Bdr.Opacity = 1;
        type1Lbl.Opacity = 1;
        type2Img.Opacity = 1;
        type2Bdr.Opacity = 1;
        type2Lbl.Opacity = 1;
        wordGrid.SetColumnSpan(type1Img, 1);
        wordGrid.SetColumnSpan(type1Bdr, 1);
        var word = Battle.OtherPlayer.CurrentWord;
        type1Lbl.Text = word.Type1.TypeToString();
        type2Lbl.Text = word.Type2.TypeToString();
        type1Bdr.WidthRequest = type1Lbl.Text.Length * 20 + 10;
        type2Bdr.WidthRequest = type2Lbl.Text.Length * 20 + 10;
        if (word.IsEmpty)
        {
            type1Img.Opacity = 0;
            type1Bdr.Opacity = 0;
            type1Lbl.Opacity = 0;
            type2Img.Opacity = 0;
            type2Bdr.Opacity = 0;
            type2Lbl.Opacity = 0;
        }
        else if (word.IsSingleTyped)
        {
            wordGrid.SetColumnSpan(type1Img, 2);
            type1Img.Source = TypeToImg(word.Type1);
            wordGrid.SetColumnSpan(type1Bdr, 2);
            type2Img.Opacity = 0;
            type2Lbl.Opacity = 0;
            type2Bdr.Opacity = 0;
        }
        else
        {
            type1Img.Source = TypeToImg(word.Type1);
            type2Img.Source = TypeToImg(word.Type2);
        }
        await wordGrid.FadeTo(1, 200, Easing.Linear);
        if (!word.IsEmpty) SBAudioManager.PlaySound(TypeToSymbol(word.Type1));
    }
    static string TypeToImg(WordType type)
    {
        return TypeToSymbol(type) + ".gif";
    }
    static string TypeToSymbol(WordType type)
    {
        return type switch
        {
            WordType.Normal => "normal",
            WordType.Animal => "animal",
            WordType.Plant => "plant",
            WordType.Place => "place",
            WordType.Emote => "emote",
            WordType.Art => "art",
            WordType.Food => "food",
            WordType.Violence => "violence",
            WordType.Health => "health",
            WordType.Body => "body",
            WordType.Mech => "mech",
            WordType.Science => "science",
            WordType.Time => "time",
            WordType.Person => "person",
            WordType.Work => "work",
            WordType.Cloth => "cloth",
            WordType.Society => "society",
            WordType.Play => "play",
            WordType.Bug => "bug",
            WordType.Math => "math",
            WordType.Insult => "insult",
            WordType.Religion => "religion",
            WordType.Sports => "sports",
            WordType.Weather => "weather",
            WordType.Tale => "tale",
            _ => null
        };
    }

    private async Task ShowEffectAsync(PlayerSelector selector, string imgName)
    {
        if (selector == PlayerSelector.Player1)
        {
            ImgAllyEffectInner.Source = imgName;
            ImgAllyEffectOuter.Source = imgName;
            AllyEffect.Opacity = 1;
            await Task.Delay(1000);
            AllyEffect.Opacity = 0;
        }
        if (selector == PlayerSelector.Player2)
        {
            ImgFoeEffectInner.Source = imgName;
            ImgFoeEffectOuter.Source = imgName;
            FoeEffect.Opacity = 1;
            await Task.Delay(1000);
            FoeEffect.Opacity = 0;
        }
    }

    private void GfxAllyWord_PaintSurface(object sender, SkiaSharp.Views.Maui.SKPaintSurfaceEventArgs e) => PaintWordSurface(sender, e);

    private void GfxFoeWord_PaintSurface(object sender, SkiaSharp.Views.Maui.SKPaintSurfaceEventArgs e) => PaintWordSurface(sender, e);
    private void PaintWordSurface(object sender, SkiaSharp.Views.Maui.SKPaintSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
        canvas.Clear();
        var width = e.Info.Width;
        var height = e.Info.Height;
        var name = Battle.OtherPlayer.CurrentWord.Name;
        var scaleX = name.Length < 10 ? 1 : (float)9 / name.Length;
        using var stream = FileSystem.Current.OpenAppPackageFileAsync("MPLUS1p-Bold.ttf").Result;
        using var skPaint = new SKPaint
        {
            TextSize = 92,
            Color = new SKColor(42, 42, 42),
            Typeface = SKTypeface.FromStream(stream),
            TextAlign = SKTextAlign.Center,
            TextScaleX = scaleX
        };
        canvas.DrawText(name, width / 2, height / 1.4f, skPaint);
    }
    private async void ContentPage_Loaded(object sender, EventArgs e)
    {
        await Task.Delay(500);
        SBAudioManager.StopSound(SBOptions.MainBgm);
        SBAudioManager.PlaySound("start");
        await Task.Delay(500);
        SBAudioManager.PlaySound(SBOptions.BattleBgm);
    }

    private async void BtnSituation_Clicked(object sender, EventArgs e) => await ShowModal(SituationModal);
    private void ReloadSituationModal()
    {
        LblAllyNameSituationModal.Text = Battle.Player1.Name;
        LblFoeNameSituationModal.Text = Battle.Player2.Name;
        LblAllyATKSituationModal.Text = $"{Battle.Player1.ATK,0:0.0#}倍";
        LblAllyDEFSituationModal.Text = $"{Battle.Player1.DEF,0:0.0#}倍";
        LblFoeATKSituationModal.Text = $"{Battle.Player2.ATK,0:0.0#}倍";
        LblFoeDEFSituationModal.Text = $"{Battle.Player2.DEF,0:0.0#}倍";
        LblOtherPlayersWordNameSituationModal.Text = Battle.OtherPlayer.CurrentWord.Name == string.Empty ? string.Empty : $"{Battle.OtherPlayer.CurrentWord.Name} の弱点";
        SetFlexWordTypes();
    }
    private void SetFlexWordTypes()
    {
        FlexWordTypes.Clear();
        var types = Word.GetWeakTypes(Battle.OtherPlayer.CurrentWord);
        if (types.Count == 0) return;
        foreach (var i in types)
        {
            var img = new Image
            {
                Source = TypeToImg(i),
                WidthRequest = 80
            };
            var lbl = new Label
            {
                Text = i.TypeToString(),
                FontFamily = "MPlus1pRegular",
                FontSize = 12,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Start,
                VerticalTextAlignment = TextAlignment.Start,
                Margin = new Thickness(0, -10, 0, 0)
            };
            var vsl = new VerticalStackLayout { Spacing = 0 };
            vsl.Add(img);
            vsl.Add(lbl);
            FlexWordTypes.Add(vsl);
        }
    }
    private async void BtnCloseSituationModal_Clicked(object sender, EventArgs e) => await HideModal(SituationModal);
    private async void BtnAbility_Clicked(object sender, EventArgs e) => await ShowModal(AbilityModal);

    private async void BtnCloseAbilityModal_Clicked(object sender, EventArgs e) => await HideModal(AbilityModal);
    private void OrderChangeAbility(string abilityName, PlayerSelector selector)
    {
        orderedAbilityName = abilityName;
        SetOrder(new(OrderType.Change, abilityName, selector));
    }

    private void PkrPlayerAbilityModal_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (!isBeforeInit) SBAudioManager.PlaySound("concent");
        SetBdrStrokeAbilityModal();
        LblCountAbilityModal.Text = "タップしてとくせいを変える";
        if (Battle.IsAbilChangeable) LblCountAbilityModal.Text +=
        $"(あと{PkrPlayerAbilityModal.SelectedIndex switch
        {
            1 => Battle.Player1.RemainingAbilChangingCount,
            2 => Battle.Player2.RemainingAbilChangingCount,
            _ => Battle.IsPlayer1sTurn ? Battle.Player1.RemainingAbilChangingCount : Battle.Player2.RemainingAbilChangingCount
        }}回)";
    }
    private void SetBdrStrokeAbilityModal()
    {
        var abilityName = PkrPlayerAbilityModal.SelectedIndex switch
        {
            1 => Battle.Player1.Ability.ToString(),
            2 => Battle.Player2.Ability.ToString(),
            _ => Battle.IsPlayer1sTurn ? Battle.Player1.Ability.ToString() : Battle.Player2.Ability.ToString()
        };
        foreach (var i in FlexChangingAbilityModal)
        {
            var bdr = i as Border;
            bdr.Stroke = Colors.Transparent;
            if (bdr.StyleId == abilityName)
            {
                bdr.Stroke = Colors.Black;
            }
        }
    }
    private async void ShowLblChangedAbilityModal(string text)
    {
        LblChangedAbilityModal.Text = text;
        LblChangedAbilityModal.Opacity = 1;
        await Task.Delay(1500);
        LblChangedAbilityModal.Opacity = 0;
    }

    private async void BtnEscape_Clicked(object sender, EventArgs e) => await ShowModal(EscapeModal);

    private async void BtnEndBattle_Clicked(object sender, EventArgs e)
    {
        SBAudioManager.StopSound(SBOptions.BattleBgm);
        Battle.Dispose();
        SBAudioManager.CancelAudio();
        while (Navigation.NavigationStack.Count > 2)
        {
            Navigation.RemovePage(Navigation.NavigationStack[1]);
        }
        await Shell.Current.GoToAsync($"../{nameof(MatchUpPage)}", false);

        // bgm 再生開始用の遅延
        await Task.Delay(500);
        SBAudioManager.PlaySound(SBOptions.MainBgm);
    }

    private async void BtnCloseEscapeModal_Clicked(object sender, EventArgs e) => await HideModal(EscapeModal);

    private async void BtnBattleOption_Clicked(object sender, EventArgs e)
    {
        LblSetOptionModal.Opacity = 0;
        await ShowModal(OptionModal);
    }

    private async void BtnCloseOptionModal_Clicked(object sender, EventArgs e) => await HideModal(OptionModal);
    private async Task ShowModal(Border modal)
    {
        SBAudioManager.PlaySound("pera");
        modal.IsVisible = true;
        await modal.FadeTo(1, 200, Easing.Linear);
        CmdBtnBack.Command = new Command(async () => await HideModal(modal));
    }
    private async Task HideModal(Border modal)
    {
        SBAudioManager.PlaySound("pera");
        await modal.FadeTo(0, 200, Easing.Linear);
        modal.IsVisible = false;
        InitBtnBackBehavior();
    }

    private void SwInfiniteSeed_Toggled(object sender, ToggledEventArgs e)
    {
        MaxSeedTurnSetter.IsVisible = !SwInfiniteSeed.IsToggled;
        if (isBeforeInit) return;
        SetOrder(new(Options.InfiniteSeed, SwInfiniteSeed.IsToggled));
    }

    private void SwAbilChange_Toggled(object sender, ToggledEventArgs e)
    {
        MaxAbilChangeSetter.IsVisible = SwAbilChange.IsToggled;
        if (isBeforeInit) return;
        SetOrder(new(Options.AbilChange, SwAbilChange.IsToggled));
    }

    private void SwInfiniteCure_Toggled(object sender, ToggledEventArgs e)
    {
        MaxCureCountSetter.IsVisible = !SwInfiniteCure.IsToggled;
        if (isBeforeInit) return;
        SetOrder(new(Options.InfiniteCure, SwInfiniteCure.IsToggled));
    }
    private static void HideOptionCounterBorders(Border plus, Border minus, Border plus10, Border minus10)
    {
        plus.IsEnabled = minus.IsEnabled = plus10.IsEnabled = minus10.IsEnabled = false;
        plus.Opacity = minus.Opacity = plus10.Opacity = minus10.Opacity = 0;
    }
    private static void ShowOptionCounterBorders(Border plus, Border minus, Border plus10, Border minus10)
    {
        plus.IsEnabled = minus.IsEnabled = plus10.IsEnabled = minus10.IsEnabled = true;
        plus.Opacity = minus.Opacity = plus10.Opacity = minus10.Opacity = 1;
    }
    private void ShowLblSetOptionModal(string text)
    {
        LblSetOptionModal.Text = text;
        LblSetOptionModal.Opacity = 1;
    }

    #region  main option handlers

    private void MaxCureEntry_Focused(object sender, FocusEventArgs e) => HideOptionCounterBorders(BdrMaxCurePlus, BdrMaxCureMinus, BdrMaxCurePlus10, BdrMaxCureMinus10);

    private void MaxCureEntry_Unfocused(object sender, FocusEventArgs e) => ShowOptionCounterBorders(BdrMaxCurePlus, BdrMaxCureMinus, BdrMaxCurePlus10, BdrMaxCureMinus10);

    private void MaxCureEntry_Completed(object sender, EventArgs e)
    {
        var maxCureCount = int.TryParse(MaxCureEntry.Text, out var maxCure) ? maxCure : Player.MaxCureCount;
        SetOrder(new(Options.SetMaxCureCount, Math.Max(0, maxCureCount)));
        MaxCureEntry.Text = Math.Max(0, maxCureCount).ToString();
    }
    private void SetOrder(Order order)
    {
        OrderBuffer = order;
        isOrdered = true;
    }
    private void ModifyIntEntryCounter(Entry entry, Options option, int dif)
    {
        var currentNum = int.Parse(entry.Text);
        var resultNum = Math.Max(0, currentNum + dif);
        if (resultNum != currentNum) SetOrder(new(option, resultNum));
        if (!deadFlag) entry.Text = resultNum.ToString();
    }
    private void ModifySignedEntryBorder(Entry entry, Options option, int dif)
    {
        var currentNum = int.Parse(entry.Text);
        var resultNum = currentNum + dif;
        if (resultNum != currentNum) SetOrder(new(option, resultNum));
        if (!deadFlag) entry.Text = resultNum.ToString();
    }
    private void ModifyDoubleEntryCounter(Entry entry, Options option, int dif)
    {
        var currentNum = double.Parse(entry.Text);
        var resultNum = Math.Max(0, currentNum + dif);
        if (resultNum != currentNum) SetOrder(new(option, resultNum));
        if (!deadFlag) entry.Text = resultNum.ToString();
    }

    private void BtnMaxCurePlus_Clicked(object sender, EventArgs e) => ModifyIntEntryCounter(MaxCureEntry, Options.SetMaxCureCount, 1);

    private void BtnMaxCureMinus_Clicked(object sender, EventArgs e) => ModifyIntEntryCounter(MaxCureEntry, Options.SetMaxCureCount, -1);

    private void BtnMaxCurePlus10_Clicked(object sender, EventArgs e) => ModifyIntEntryCounter(MaxCureEntry, Options.SetMaxCureCount, 10);

    private void BtnMaxCureMinus10_Clicked(object sender, EventArgs e) => ModifyIntEntryCounter(MaxCureEntry, Options.SetMaxCureCount, -10);

    private void CritEntry_Focused(object sender, FocusEventArgs e) => HideOptionCounterBorders(BdrCritPlus, BdrCritMinus, BdrCritPlus10, BdrCritMinus10);

    private void CritEntry_Unfocused(object sender, FocusEventArgs e) => ShowOptionCounterBorders(BdrCritPlus, BdrCritMinus, BdrCritPlus10, BdrCritMinus10);

    private void CritEntry_Completed(object sender, EventArgs e)
    {
        var crit = double.TryParse(CritEntry.Text, out var critVal) ? critVal : Player.CritDmg;
        SetOrder(new(Options.SetCritDmgMultiplier, Math.Max(0, crit)));
        CritEntry.Text = Math.Max(0, crit).ToString();
    }

    private void BtnCritPlus_Clicked(object sender, EventArgs e) => ModifyDoubleEntryCounter(CritEntry, Options.SetCritDmgMultiplier, 1);

    private void BtnCritMinus_Clicked(object sender, EventArgs e) => ModifyDoubleEntryCounter(CritEntry, Options.SetCritDmgMultiplier, -1);

    private void BtnCritPlus10_Clicked(object sender, EventArgs e) => ModifyDoubleEntryCounter(CritEntry, Options.SetCritDmgMultiplier, 10);

    private void BtnCritMinus10_Clicked(object sender, EventArgs e) => ModifyDoubleEntryCounter(CritEntry, Options.SetCritDmgMultiplier, -10);

    private void MaxSeedEntry_Focused(object sender, FocusEventArgs e) => HideOptionCounterBorders(BdrMaxSeedPlus, BdrMaxSeedMinus, BdrMaxSeedPlus10, BdrMaxSeedMinus10);

    private void MaxSeedEntry_Unfocused(object sender, FocusEventArgs e) => ShowOptionCounterBorders(BdrMaxSeedPlus, BdrMaxSeedMinus, BdrMaxSeedPlus10, BdrMaxSeedMinus10);

    private void MaxSeedEntry_Completed(object sender, EventArgs e)
    {
        var maxSeedTurn = int.TryParse(MaxSeedEntry.Text, out var maxSeed) ? maxSeed : Player.MaxSeedTurn;
        SetOrder(new(Options.SetMaxSeedTurn, Math.Max(0, maxSeedTurn)));
        MaxSeedEntry.Text = Math.Max(0, maxSeedTurn).ToString();
    }

    private void BtnMaxSeedPlus_Clicked(object sender, EventArgs e) => ModifyIntEntryCounter(MaxSeedEntry, Options.SetMaxSeedTurn, 1);

    private void BtnMaxSeedMinus_Clicked(object sender, EventArgs e) => ModifyIntEntryCounter(MaxSeedEntry, Options.SetMaxSeedTurn, -1);

    private void BtnMaxSeedPlus10_Clicked(object sender, EventArgs e) => ModifyIntEntryCounter(MaxSeedEntry, Options.SetMaxSeedTurn, 10);

    private void BtnMaxSeedMinus10_Clicked(object sender, EventArgs e) => ModifyIntEntryCounter(MaxSeedEntry, Options.SetMaxSeedTurn, -10);

    private void MaxAbilEntry_Focused(object sender, FocusEventArgs e) => HideOptionCounterBorders(BdrMaxAbilPlus, BdrMaxAbilMinus, BdrMaxAbilPlus10, BdrMaxAbilMinus10);

    private void MaxAbilEntry_Unfocused(object sender, FocusEventArgs e) => ShowOptionCounterBorders(BdrMaxAbilPlus, BdrMaxAbilMinus, BdrMaxAbilPlus10, BdrMaxAbilMinus10);

    private void MaxAbilEntry_Completed(object sender, EventArgs e)
    {
        var maxAbilChange = int.TryParse(MaxAbilEntry.Text, out var maxAbil) ? maxAbil : Player.MaxAbilChange;
        SetOrder(new(Options.SetAbilCount, Math.Max(0, maxAbilChange)));
        MaxAbilEntry.Text = Math.Max(0, maxAbilChange).ToString();
    }

    private void BtnMaxAbilPlus_Clicked(object sender, EventArgs e) => ModifyIntEntryCounter(MaxAbilEntry, Options.SetAbilCount, 1);

    private void BtnMaxAbilMinus_Clicked(object sender, EventArgs e) => ModifyIntEntryCounter(MaxAbilEntry, Options.SetAbilCount, -1);

    private void BtnMaxAbilPlus10_Clicked(object sender, EventArgs e) => ModifyIntEntryCounter(MaxAbilEntry, Options.SetAbilCount, 10);

    private void BtnMaxAbilMinus10_Clicked(object sender, EventArgs e) => ModifyIntEntryCounter(MaxAbilEntry, Options.SetAbilCount, -10);

    private void SeedDmgEntry_Focused(object sender, FocusEventArgs e) => HideOptionCounterBorders(BdrSeedDmgPlus, BdrSeedDmgMinus, BdrSeedDmgPlus10, BdrSeedDmgMinus10);

    private void SeedDmgEntry_Unfocused(object sender, FocusEventArgs e) => ShowOptionCounterBorders(BdrSeedDmgPlus, BdrSeedDmgMinus, BdrSeedDmgPlus10, BdrSeedDmgMinus10);

    private void SeedDmgEntry_Completed(object sender, EventArgs e)
    {
        var seedDmg = int.TryParse(SeedDmgEntry.Text, out var dmg) ? dmg : Player.SeedDmg;
        SetOrder(new(Options.SetSeedDmg, Math.Max(0, seedDmg)));
        SeedDmgEntry.Text = Math.Max(0, seedDmg).ToString();
    }

    private void BtnSeedDmgPlus_Clicked(object sender, EventArgs e) => ModifyIntEntryCounter(SeedDmgEntry, Options.SetSeedDmg, 1);

    private void BtnSeedDmgMinus_Clicked(object sender, EventArgs e) => ModifyIntEntryCounter(SeedDmgEntry, Options.SetSeedDmg, -1);

    private void BtnSeedDmgPlus10_Clicked(object sender, EventArgs e) => ModifyIntEntryCounter(SeedDmgEntry, Options.SetSeedDmg, 10);

    private void BtnSeedDmgMinus10_Clicked(object sender, EventArgs e) => ModifyIntEntryCounter(SeedDmgEntry, Options.SetSeedDmg, -10);

    private void MaxFoodEntry_Focused(object sender, FocusEventArgs e) => HideOptionCounterBorders(BdrMaxFoodPlus, BdrMaxFoodMinus, BdrMaxFoodPlus10, BdrMaxFoodMinus10);

    private void MaxFoodEntry_Unfocused(object sender, FocusEventArgs e) => ShowOptionCounterBorders(BdrMaxFoodPlus, BdrMaxFoodMinus, BdrMaxFoodPlus10, BdrMaxFoodMinus10);

    private void MaxFoodEntry_Completed(object sender, EventArgs e)
    {
        var maxFoodCount = int.TryParse(MaxFoodEntry.Text, out var maxFood) ? maxFood : Player.MaxFoodCount;
        SetOrder(new(Options.SetMaxFoodCount, Math.Max(0, maxFoodCount)));
        MaxFoodEntry.Text = Math.Max(0, maxFoodCount).ToString();
    }

    private void BtnMaxFoodPlus_Clicked(object sender, EventArgs e) => ModifyIntEntryCounter(MaxFoodEntry, Options.SetMaxFoodCount, 1);

    private void BtnMaxFoodMinus_Clicked(object sender, EventArgs e) => ModifyIntEntryCounter(MaxFoodEntry, Options.SetMaxFoodCount, -1);

    private void BtnMaxFoodPlus10_Clicked(object sender, EventArgs e) => ModifyIntEntryCounter(MaxFoodEntry, Options.SetMaxFoodCount, 10);

    private void BtnMaxFoodMinus10_Clicked(object sender, EventArgs e) => ModifyIntEntryCounter(MaxFoodEntry, Options.SetMaxFoodCount, -10);

    private void InsBufEntry_Focused(object sender, FocusEventArgs e) => HideOptionCounterBorders(BdrInsBufPlus, BdrInsBufMinus, BdrInsBufPlus10, BdrInsBufMinus10);

    private void InsBufEntry_Unfocused(object sender, FocusEventArgs e) => ShowOptionCounterBorders(BdrInsBufPlus, BdrInsBufMinus, BdrInsBufPlus10, BdrInsBufMinus10);

    private void InsBufEntry_Completed(object sender, EventArgs e)
    {
        var insBufQty = int.TryParse(InsBufEntry.Text, out var buf) ? buf : Player.InsBufQty;
        SetOrder(new(Options.SetInsBufQty, insBufQty));
        InsBufEntry.Text = insBufQty.ToString();
    }

    private void BtnInsBufPlus_Clicked(object sender, EventArgs e) => ModifySignedEntryBorder(InsBufEntry, Options.SetInsBufQty, 1);

    private void BtnInsBufMinus_Clicked(object sender, EventArgs e) => ModifySignedEntryBorder(InsBufEntry, Options.SetInsBufQty, -1);

    private void BtnInsBufPlus10_Clicked(object sender, EventArgs e) => ModifySignedEntryBorder(InsBufEntry, Options.SetInsBufQty, 10);

    private void BtnInsBufMinus10_Clicked(object sender, EventArgs e) => ModifySignedEntryBorder(InsBufEntry, Options.SetInsBufQty, -10);

    private async void BtnRevertOptionModal_Clicked(object sender, EventArgs e)
    {
        Mode.Set(Battle);
        ShowLblSetOptionModal("オプションをデフォルトに戻しました");
        isBeforeInit = true;
        InitOptionModal();
        SBAudioManager.PlaySound("concent");
        isBeforeInit = false;
        ReloadPlayerOptionModal(currentPlayerOptionModalSelector);
        await UpdateAllHPBarsAsync(Battle.Player1.HP, Battle.Player2.HP, 0);
    }

    private async void BtnOpenPlayer1Setting_Clicked(object sender, EventArgs e) => await ShowPlayerOptionModal(PlayerSelector.Player1);

    private async void BtnOpenPlayer2Setting_Clicked(object sender, EventArgs e) => await ShowPlayerOptionModal(PlayerSelector.Player2);
    private async Task ShowPlayerOptionModal(PlayerSelector selector)
    {
        currentPlayerOptionModalSelector = selector;
        SBAudioManager.PlaySound("pera");
        PlayerOptions.IsVisible = true;
        ReloadPlayerOptionModal(selector);
        await PlayerOptions.FadeTo(1, 200, Easing.Linear);
        MainOptions.IsVisible = false;
    }
    #endregion
    private async void BtnBackOption_Clicked(object sender, EventArgs e)
    {
        SBAudioManager.PlaySound("pera");
        MainOptions.IsVisible = true;
        await PlayerOptions.FadeTo(0, 200, Easing.Linear);
        PlayerOptions.IsVisible = false;
    }
    private void ReloadPlayerOptionModal(PlayerSelector selector)
    {
        var player = selector == PlayerSelector.Player1 ? Battle.Player1 : Battle.Player2;
        LblPlayerNameOptionModal.Text = player.Name;
        ImgCurrentAbilityOptionModal.Source = player.Ability.ImgFile;
        HPSettingEntry.Text = player.HP.ToString();
        MaxHPEntry.Text = player.MaxHP.ToString();
        LblATKViewer.Text = FormatStatusNumber(player.ATKIndex - 6);
        ATKSlider.Value = player.ATKIndex - 6;
        LblDEFViewer.Text = FormatStatusNumber(player.DEFIndex - 6);
        DEFSlider.Value = player.DEFIndex - 6;
        isPkrPlayerLuckInitializing = true;
        PkrPlayerLuck.SelectedIndex = player.Luck switch
        {
            Luck.Lucky => 0,
            Luck.Normal => 1,
            _ => 2
        };
        isPkrPlayerLuckInitializing = false;
    }

    private void HPSettingEntry_Focused(object sender, FocusEventArgs e) => HideOptionCounterBorders(BdrHPSettingPlus, BdrHPSettingMinus, BdrHPSettingPlus10, BdrHPSettingMinus10);

    private void HPSettingEntry_Unfocused(object sender, FocusEventArgs e) => ShowOptionCounterBorders(BdrHPSettingPlus, BdrHPSettingMinus, BdrHPSettingPlus10, BdrHPSettingMinus10);

    private void HPSettingEntry_Completed(object sender, EventArgs e)
    {
        var player = Battle.PlayerSelectorToPlayerOrDefault(currentPlayerOptionModalSelector);
        var hp = int.TryParse(HPSettingEntry.Text, out var hpTmp) ? hpTmp : player.HP;
        var hpResult = Math.Min(player.MaxHP, Math.Max(hp, 0));
        SetOrder(new(Options.SetHP, currentPlayerOptionModalSelector, hpResult));
        HPSettingEntry.Text = hpResult.ToString();
    }

    private void BtnHPSettingPlus_Clicked(object sender, EventArgs e)
    {
        var player = Battle.PlayerSelectorToPlayerOrDefault(currentPlayerOptionModalSelector);
        var hpResult = Math.Min(player.MaxHP, player.HP + 1);
        if (hpResult != player.HP) SetOrder(new(Options.SetHP, currentPlayerOptionModalSelector, hpResult));
        HPSettingEntry.Text = hpResult.ToString();
    }

    private void BtnHPSettingMinus_Clicked(object sender, EventArgs e)
    {
        var player = Battle.PlayerSelectorToPlayerOrDefault(currentPlayerOptionModalSelector);
        var hpResult = Math.Max(0, player.HP - 1);
        if (hpResult != player.HP) SetOrder(new(Options.SetHP, currentPlayerOptionModalSelector, hpResult));
        HPSettingEntry.Text = hpResult.ToString();
    }

    private void BtnHPSettingPlus10_Clicked(object sender, EventArgs e)
    {
        var player = Battle.PlayerSelectorToPlayerOrDefault(currentPlayerOptionModalSelector);
        var hpResult = Math.Min(player.MaxHP, player.HP + 10);
        if (hpResult != player.HP) SetOrder(new(Options.SetHP, currentPlayerOptionModalSelector, hpResult));
        HPSettingEntry.Text = hpResult.ToString();
    }

    private void BtnHPSettingMinus10_Clicked(object sender, EventArgs e)
    {
        var player = Battle.PlayerSelectorToPlayerOrDefault(currentPlayerOptionModalSelector);
        var hpResult = Math.Max(0, player.HP - 10);
        if (hpResult != player.HP) SetOrder(new(Options.SetHP, currentPlayerOptionModalSelector, hpResult));
        HPSettingEntry.Text = hpResult.ToString();
    }

    private void MaxHPEntry_Focused(object sender, FocusEventArgs e) => HideOptionCounterBorders(BdrMaxHPPlus, BdrMaxHPMinus, BdrMaxHPPlus10, BdrMaxHPMinus10);

    private void MaxHPEntry_Unfocused(object sender, FocusEventArgs e) => ShowOptionCounterBorders(BdrMaxHPPlus, BdrMaxHPMinus, BdrMaxHPPlus10, BdrMaxHPMinus10);

    private void MaxHPEntry_Completed(object sender, EventArgs e)
    {
        var player = Battle.PlayerSelectorToPlayerOrDefault(currentPlayerOptionModalSelector);
        var maxHP = int.TryParse(MaxHPEntry.Text, out var maxHPTmp) ? maxHPTmp : player.MaxHP;
        var maxHPResult = Math.Max(maxHP, 0);
        SetOrder(new(Options.SetMaxHP, currentPlayerOptionModalSelector, maxHPResult));
        MaxHPEntry.Text = maxHPResult.ToString();
        HPSettingEntry.Text = Math.Min(maxHPResult, player.HP).ToString();
    }

    private void BtnMaxHPPlus_Clicked(object sender, EventArgs e)
    {
        var player = Battle.PlayerSelectorToPlayerOrDefault(currentPlayerOptionModalSelector);
        SetOrder(new(Options.SetMaxHP, currentPlayerOptionModalSelector, player.MaxHP + 1));
        MaxHPEntry.Text = (player.MaxHP + 1).ToString();
        HPSettingEntry.Text = Math.Min(player.MaxHP + 1, player.HP).ToString();
    }

    private void BtnMaxHPMinus_Clicked(object sender, EventArgs e)
    {
        var player = Battle.PlayerSelectorToPlayerOrDefault(currentPlayerOptionModalSelector);
        var maxHPResult = Math.Max(1, player.MaxHP - 1);
        if (maxHPResult != player.MaxHP) SetOrder(new(Options.SetMaxHP, currentPlayerOptionModalSelector, maxHPResult));
        MaxHPEntry.Text = maxHPResult.ToString();
        HPSettingEntry.Text = Math.Min(maxHPResult, player.HP).ToString();
    }

    private void BtnMaxHPPlus10_Clicked(object sender, EventArgs e)
    {
        var player = Battle.PlayerSelectorToPlayerOrDefault(currentPlayerOptionModalSelector);
        SetOrder(new(Options.SetMaxHP, currentPlayerOptionModalSelector, player.MaxHP + 10));
        MaxHPEntry.Text = (player.MaxHP + 10).ToString();
        HPSettingEntry.Text = Math.Min(player.MaxHP + 10, player.HP).ToString();
    }

    private void BtnMaxHPMinus10_Clicked(object sender, EventArgs e)
    {
        var player = Battle.PlayerSelectorToPlayerOrDefault(currentPlayerOptionModalSelector);
        var maxHPResult = Math.Max(1, player.MaxHP - 10);
        if (maxHPResult != player.MaxHP) SetOrder(new(Options.SetMaxHP, currentPlayerOptionModalSelector, maxHPResult));
        MaxHPEntry.Text = maxHPResult.ToString();
        HPSettingEntry.Text = Math.Min(maxHPResult, player.HP).ToString();
    }

    private static string FormatStatusNumber(int num)
    {
        var sign = num > 0 ? "+" : string.Empty;
        return sign + num.ToString();
    }

    private void ATKSlider_DragCompleted(object sender, EventArgs e)
    {
        var atk = (int)Math.Round(ATKSlider.Value) + 6;
        SetOrder(new(Options.SetATK, currentPlayerOptionModalSelector, atk));
    }

    private void ATKSlider_ValueChanged(object sender, ValueChangedEventArgs e)
    {
        LblATKViewer.Text = FormatStatusNumber((int)Math.Round(ATKSlider.Value));
    }

    private void BtnATKPlus_Clicked(object sender, EventArgs e)
    {
        var valueTmp = ATKSlider.Value;
        ATKSlider.Value++;
        if (ATKSlider.Value != valueTmp) SetOrder(new(Options.SetATK, currentPlayerOptionModalSelector, (int)Math.Round(ATKSlider.Value) + 6));
    }

    private void BtnATKMinus_Clicked(object sender, EventArgs e)
    {
        var valueTmp = ATKSlider.Value;
        ATKSlider.Value--;
        if (ATKSlider.Value != valueTmp) SetOrder(new(Options.SetATK, currentPlayerOptionModalSelector, (int)Math.Round(ATKSlider.Value) + 6));
    }

    private void DEFSlider_DragCompleted(object sender, EventArgs e)
    {
        var def = (int)Math.Round(DEFSlider.Value) + 6;
        SetOrder(new(Options.SetDEF, currentPlayerOptionModalSelector, def));
    }

    private void DEFSlider_ValueChanged(object sender, ValueChangedEventArgs e)
    {
        LblDEFViewer.Text = FormatStatusNumber((int)Math.Round(DEFSlider.Value));
    }

    private void BtnDEFPlus_Clicked(object sender, EventArgs e)
    {
        var valueTmp = DEFSlider.Value;
        DEFSlider.Value++;
        if (DEFSlider.Value != valueTmp) SetOrder(new(Options.SetDEF, currentPlayerOptionModalSelector, (int)Math.Round(DEFSlider.Value) + 6));
    }

    private void BtnDEFMinus_Clicked(object sender, EventArgs e)
    {
        var valueTmp = DEFSlider.Value;
        DEFSlider.Value--;
        if (DEFSlider.Value != valueTmp) SetOrder(new(Options.SetDEF, currentPlayerOptionModalSelector, (int)Math.Round(DEFSlider.Value) + 6));
    }

    private void PkrPlayerLuck_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (isBeforeInit || isPkrPlayerLuckInitializing) return;
        var luck = PkrPlayerLuck.SelectedIndex switch
        {
            0 => "lucky",
            1 => "normal",
            _ => "unlucky"
        };
        SetOrder(new(Options.SetLuck, luck, currentPlayerOptionModalSelector));
        SBAudioManager.PlaySound("concent");
    }
}