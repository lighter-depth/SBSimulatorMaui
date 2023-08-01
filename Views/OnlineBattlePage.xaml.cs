using Microsoft.Maui.Controls.Shapes;

namespace SBSimulatorMaui;

public partial class OnlineBattlePage : ContentPage
{
    public static Player InitialPlayer { get; internal set; } = new();
    bool isBattleBegan = false;
    readonly CancellationTokenSource Cancellation = new();
    readonly Action<Order, CancellationTokenSource> EmptyDelegate = (o, c) => { };
    Battle Battle;
    Order OrderBuffer = new();
    bool isOrdered = false;
    bool isBeforeInit = true;
    bool deadFlag = false;
    bool omitFlag = true;
    string orderedAbilityName = string.Empty;
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
    public OnlineBattlePage()
    {
        InitializeComponent();
        InitBtnBackBehavior();
        InitServer();
    }
    private async void InitServer()
    {
        Server.InitialPlayerInfo = InitialPlayer.Serialize();
        await Task.WhenAll(Server.RunAsync(), MainAsync(Cancellation.Token));
    }
    private (Player Player1Skl, Player Player2Skl) GetPlayerInfo()
    {
        if (Cancellation.IsCancellationRequested) throw new TaskCanceledException();
        var serverPlayer1Info = Player.Deserialize(Server.CurrentRoom.Player1Info);
        var serverPlayer2Info = Player.Deserialize(Server.CurrentRoom.Player2Info);
        var player1Skl = Server.IsHost ? serverPlayer1Info : serverPlayer2Info;
        var player2Skl = Server.IsHost ? serverPlayer2Info : serverPlayer1Info;
        return (player1Skl, player2Skl);
    }
    private void InitBtnBackBehavior()
    {
        CmdBtnBack.Command = new Command(async () =>
        {
            Cancellation.Cancel();
            SBAudioManager.StopSound(SBOptions.BattleBgm);
            await Server.CancelAsync();
            if (isBattleBegan)
            {
                SBAudioManager.StopSound(SBOptions.BattleBgm);
                SBAudioManager.CancelAudio();
            }
            while (Navigation.NavigationStack.Count > 2)
            {
                Navigation.RemovePage(Navigation.NavigationStack[1]);
            }
            await Shell.Current.GoToAsync($"../{nameof(OnlineMatchUpPage)}", false);
            if (!isBattleBegan) return;
            // horizon 再生開始用の遅延
            await Task.Delay(500);
            SBAudioManager.PlaySound(SBOptions.MainBgm);
        });
    }
    private async Task MainAsync(CancellationToken ct)
    {
        try
        {
            await WaitUntilMatchingAsync(ct);
            if (ct.IsCancellationRequested) return;
            await OnMatch(ct);
            await InitBattle();
            InitAbilityModal();
            if (Server.IsHost)
            {
                ShowWordEntry();
                WordEntry.Placeholder = $"「{Server.CurrentRoom.InitialChar}」からはじまることば";
            }
            else
            {
                HideWordEntry();
                LblEntryHider.Text = string.Empty;
                LblAuxInfo.Opacity = 1;
                LblAuxInfo.Text = "相手を待っています";
                omitFlag = false;
                await WaitUntilFoeActionAsync(ct);
            }
            isBeforeInit = false;
            Battle.Run();
        }
        catch
        {
            return;
        }
    }
    private static async Task WaitUntilMatchingAsync(CancellationToken ct)
    {
        await Task.Run(() =>
        {
            while (true)
            {
                if (Server.NotificationFlag && Server.CurrentRoom.Header == RoomState.Full)
                {
                    Server.NotificationFlag = false;
                    break;
                }
            }
        }, ct);
    }
    private async Task OnMatch(CancellationToken ct)
    {
        SBAudioManager.PlaySound("start");
        SBAudioManager.StopSound(SBOptions.MainBgm);
        LblEntryHider.Text = "マッチングした！";
        BtnSituation.IsVisible = true;
        BtnAbility.IsVisible = true;
        AllyHPBar.Opacity = 1;
        FoeHPBar.Opacity = 1;
        await Task.Delay(200, ct);
        SBAudioManager.PlaySound(SBOptions.BattleBgm);
        isBattleBegan = true;
        ShowInitialChar();
        Observe();
    }
    private async void Observe()
    {
        await Task.Run(async () =>
        {
            while (!Cancellation.IsCancellationRequested)
            {
                await Task.Delay(5000);
                await Server.ForceReadAsync();
                if (Server.CurrentRoom.Header == RoomState.Empty)
                {
                    await OnFoeFleeingAsync();
                    break;
                }
            }
        });
    }
    private async Task OnFoeFleeingAsync()
    {
        Dispatcher.Dispatch(() =>
        {
            HideWordEntry();
            LblAuxInfo.Text = "相手が諦めました";
            LblEntryHider.Text = "相手との勝負に勝った！";
        });
        await Server.CancelAsync();
        Cancellation.Cancel();
    }
    private async Task InitBattle()
    {
        var (player1Skl, player2Skl) = GetPlayerInfo();
        player1Skl.Proceeding = Server.IsHost ? Proceeds.True : Proceeds.False;
        Battle = new(player1Skl, player2Skl)
        {
            In = In,
            Out = Out,
            OnShowOrdered = EmptyDelegate,
            OnResetOrdered = EmptyDelegate,
            OnExitOrdered = EmptyDelegate,
            OnHelpOrdered = EmptyDelegate,
            OnAddOrdered = EmptyDelegate,
            OnRemoveOrdered = EmptyDelegate,
            OnSearchOrdered = EmptyDelegate,
            OnReset = cts => cts.Cancel(),
        };
        Battle.Player1.Register(Battle);
        Battle.Player2.Register(Battle);
        LblAllyName.Text = Battle.Player1.Name;
        LblFoeName.Text = Battle.Player2.Name;
        await Task.Delay(50);

        ReloadHPTexts();
        SBOptions.Random = new(Server.CurrentRoom.Seed);
        LblAuxInfo.Text = Battle.IsPlayer1sTurn ? $"{Battle.Player1.Name} のターンです" : $"{Battle.Player2.Name} のターンです";
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
        var index = 0;
        foreach (var i in FlexChangingAbilityModal)
        {
            var bdr = i as Border;
            var grid = bdr.Content as Grid;
            var btn = grid[0] as ImageButton;
            var ability = abilities[index];
            btn.Clicked += (sender, e) => OrderChangeAbility(ability.ToString(), PlayerSelector.Player1);
            index++;
        }
        ReloadAbilityModalTexts();
    }
    private void ReloadHPTexts()
    {
        LblAllyHP.Text = $"{Battle.Player1.HP}/{Battle.Player1.MaxHP}";
        LblFoeHP.Text = $"{Battle.Player2.HP}/{Battle.Player2.MaxHP}";
    }
    private async Task<Order> In()
    {
        return await Task.Run(async () =>
        {
            while (true) if (isOrdered) break;
            isOrdered = false;
            await Server.ForceReadAsync();
            await SyncPlayerInfo();
            return OrderBuffer;
        });
    }
    private async Task Out(List<AnnotatedString> list)
    {
        await ReloadSituationModal();
        SetBdrStrokeAbilityModal();
        if (!Battle.IsPlayer1sTurn) await OutPlayerInfoAsync();
        if (Battle.CurrentOrderType == OrderType.Action) await OutActionOrder(list);
        if (Battle.CurrentOrderType == OrderType.Change) OutChangeOrder(list);
    }
    private void ShowWordEntry()
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
    private void HideWordEntry()
    {
        WordEntry.IsEnabled = false;
        BtnRegisterWord.IsEnabled = false;
        WordEntry.Opacity = 0;
        BdrWordEntry.Opacity = 0;
        BtnRegisterWord.Opacity = 0;
        LblAuxInfo.Opacity = 0;
        LblAuxInfo.Text = Battle.IsPlayer1sTurn ? $"{Battle.Player1.Name} のターンです" : $"{Battle.Player2.Name} のターンです";
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
        if (!(deadFlag ^ Battle.IsPlayer1sTurn)) await ShowWordAsync(AllyWord, LblAllyWord, ImgAllyType1, BdrAllyType1, LblAllyType1, ImgAllyType2, BdrAllyType2, LblAllyType2);
        else await ShowWordAsync(FoeWord, LblFoeWord, ImgFoeType1, BdrFoeType1, LblFoeType1, ImgFoeType2, BdrFoeType2, LblFoeType2);
        BoxEntryHider.Opacity = 1;
        LblEntryHider.Opacity = 1;

        // 単語表示 → メッセージ表示 のテンポを整えるための遅延
        await Task.Delay(900);

        await ShowMessageAsync(LblEntryHider, msgList);

        if (!deadFlag && Battle.IsPlayer1sTurn)
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
        if (!Battle.IsPlayer1sTurn)
        {
            HideWordEntry();
            LblAuxInfo.Opacity = 1;
            LblAuxInfo.Text = "相手を待っています";
            FireWaiting(Cancellation.Token);
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
    private async void FireWaiting(CancellationToken ct) => await WaitUntilFoeActionAsync(ct);
    private async Task WaitUntilFoeActionAsync(CancellationToken ct)
    {
        while (true)
        {
            try
            {
                await waitingLoopAsync();
            }
            catch { return; }
            if (ct.IsCancellationRequested) return;
            await Server.ForceReadAsync();
            var player2info = GetPlayerInfo().Player2Skl;
            var player2WordName = player2info.CurrentWord.Name;
            if (string.IsNullOrWhiteSpace(player2WordName)) continue;
            SetOrder(new(player2WordName));
            break;
        }
        omitFlag = true;
        async Task waitingLoopAsync()
        {
            while (!ct.IsCancellationRequested)
            {
                await Task.Delay(3000, ct);
                await Server.ForceReadAsync();
                if (IsReloadRequired()) break;
            }
        }
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
                SBAudioManager.StopSound(SBOptions.BattleBgm);
            }
            tasks.Add(Task.Delay(900));
            await Task.WhenAll(tasks);
        }
    }
    private async Task ShowWordAsync(Grid wordGrid, Label wordLbl, Image type1Img, Border type1Bdr, Label type1Lbl, Image type2Img, Border type2Bdr, Label type2Lbl)
    {
        await wordGrid.FadeTo(0, 200, Easing.Linear);
        PaintWordSurface(wordLbl);
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
    private void PaintWordSurface(Label wordLbl)
    {
        if (isBeforeInit) return;
        const double DEFAULT_FONT_SIZE = 50;
        var name = Battle.OtherPlayer.CurrentWord.Name;
        var fontSize = name.Length < 9 ? 1 : (double)9 / name.Length;
        wordLbl.Text = name;
        wordLbl.FontSize = DEFAULT_FONT_SIZE * fontSize;
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
    private async Task SyncPlayerInfo()
    {
        await Server.ForceReadAsync();
        Battle.Player2.Sync(GetPlayerInfo().Player2Skl);
    }
    private async void ShowInitialChar()
    {
        LblInitialChar.Text = Server.CurrentRoom.InitialChar;
        await BdrInitialChar.FadeTo(1, 250, Easing.Linear);
        await Task.Delay(6000);
        await BdrInitialChar.FadeTo(0, 250, Easing.Linear);
    }

    private void WordEntry_Completed(object sender, EventArgs e)
    {
        BtnRegisterWord_Clicked(sender, e);
    }

    private async void BtnSituation_Clicked(object sender, EventArgs e) => await ShowModal(SituationModal);

    private async void BtnAbility_Clicked(object sender, EventArgs e) => await ShowModal(AbilityModal);

    private async void BtnEscape_Clicked(object sender, EventArgs e) => await ShowModal(EscapeModal);
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
    private async Task OutPlayerInfoAsync()
    {
        var player1Info = Battle.Player1.Serialize();
        var player2Info = Battle.Player2.Serialize();
        var hostInfo = Server.IsHost ? player1Info : player2Info;
        var clientInfo = Server.IsHost ? player2Info : player1Info;
        await Server.OverwriteAsync(hostInfo, clientInfo);
    }

    private void BtnRegisterWord_Clicked(object sender, EventArgs e)
    {
        SetOrder(new(WordEntry.Text));
        WordEntry.Text = string.Empty;
    }
    private void SetOrder(Order order)
    {
        OrderBuffer = order;
        isOrdered = true;
    }

    private bool IsReloadRequired()
    {
        if (omitFlag)
        {
            omitFlag = false;
            return false;
        }
        var serverInfo = GetPlayerInfo().Player2Skl.Serialize();
        var clientInfo = Battle.Player2.Serialize();
        return serverInfo != clientInfo;
    }


    private async void BtnEndBattle_Clicked(object sender, EventArgs e)
    {
        SBAudioManager.StopSound(SBOptions.BattleBgm);
        await Server.CancelAsync();
        Cancellation.Cancel();
        Battle.Dispose();
        SBAudioManager.CancelAudio();
        while (Navigation.NavigationStack.Count > 2)
        {
            Navigation.RemovePage(Navigation.NavigationStack[1]);
        }
        await Shell.Current.GoToAsync($"../{nameof(OnlineMatchUpPage)}", false);

        // bgm 再生開始用の遅延
        await Task.Delay(500);
        SBAudioManager.PlaySound(SBOptions.MainBgm);
    }

    private async void BtnCloseEscapeModal_Clicked(object sender, EventArgs e) => await HideModal(EscapeModal);

    private async void BtnCloseSituationModal_Clicked(object sender, EventArgs e) => await HideModal(SituationModal);
    private async Task ReloadSituationModal()
    {
        await Server.ForceReadAsync();
        var (player1, player2) = GetPlayerInfo();
        LblAllyNameSituationModal.Text = player1.Name;
        LblFoeNameSituationModal.Text = player2.Name;
        LblAllyATKSituationModal.Text = $"{player1.ATK,0:0.0#}倍";
        LblAllyDEFSituationModal.Text = $"{player1.DEF,0:0.0#}倍";
        LblFoeATKSituationModal.Text = $"{player2.ATK,0:0.0#}倍";
        LblFoeDEFSituationModal.Text = $"{player2.DEF,0:0.0#}倍";
        LblOtherPlayersWordNameSituationModal.Text = player2.CurrentWord.Name == string.Empty ? string.Empty : $"{player2.CurrentWord.Name} の弱点";
        SetFlexWordTypes();
    }
    private void SetFlexWordTypes()
    {
        FlexWordTypes.Clear();
        var types = Word.GetWeakTypes(GetPlayerInfo().Player2Skl.CurrentWord);
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

    private async void BtnCloseAbilityModal_Clicked(object sender, EventArgs e) => await HideModal(AbilityModal);

    private void ReloadAbilityModalTexts()
    {
        LblCountAbilityModal.Text = "タップしてとくせいを変える";
        if (Battle.IsAbilChangeable) LblCountAbilityModal.Text += $"(あと{Battle.Player1.RemainingAbilChangingCount}回)";
        LblAllyAbilityNameAbilityModal.Text = Battle.Player1.Ability.ToString();
        LblFoeAbilityNameAbilityModal.Text = "ひみつ";
        LblAllyAbilityDescAbilityModal.Text = Battle.Player1.Ability.Description;
        LblFoeAbilityDescAbilityModal.Text = "相手もきみのとくせいを知らないぞ";
    }

    private void OrderChangeAbility(string abilityName, PlayerSelector selector)
    {
        orderedAbilityName = abilityName;
        SetOrder(new(OrderType.Change, abilityName, selector));
    }

    private void SetBdrStrokeAbilityModal()
    {
        var abilityName = Battle.Player1.Ability.ToString();
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
}