namespace SBSimulatorMaui;

public partial class OnlineBattlePage : ContentPage
{
    public static Player InitialPlayer { get; internal set; } = new();
    bool isBattleBegan = false;
    readonly CancellationTokenSource Cancellation = new();
    readonly Action<Order, CancellationTokenSource> EmptyDelegate = (o, c) => { };
    Battle Battle;
    Order OrderBuffer = new();
    Room PreviousRoom = Room.Empty;
    bool isOrdered = false;
    bool isBeforeInit = true;
    bool deadFlag = false;
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
                await WaitUntilFoeActionAsync(ct);
            }
            LblServerState.Text = Server.IsHost ? "\ud83d\udfe2 ホスト" : "\ud83d\udfe2 クライアント";
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
        await ShowInitialCharAsync();
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
    private void ReloadHPTexts()
    {
        LblAllyHP.Text = $"{Battle.Player1.HP}/{Battle.Player1.MaxHP}";
        LblFoeHP.Text = $"{Battle.Player2.HP}/{Battle.Player2.MaxHP}";
    }
    private async Task<Order> In()
    {
        return await Task.Run(async () =>
        {
            while (true)
            {
                if (isOrdered) break;
            }
            isOrdered = false;
            await Server.ForceReadAsync();
            //SyncPlayerInfo();
            return OrderBuffer;
        });
    }
    private async Task Out(List<AnnotatedString> list)
    {
        if(!Battle.IsPlayer1sTurn) await OutPlayerInfoAsync();
        if (Battle.CurrentOrderType == OrderType.Action) await OutActionOrder(list);
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
            await WaitUntilFoeActionAsync(Cancellation.Token); 
        }
    }

    private async Task WaitUntilFoeActionAsync(CancellationToken ct)
    {
        PreviousRoom = Server.CurrentRoom with { };
        do
        {
            try
            {
                await Task.Run(async () =>
                {
                    while (!ct.IsCancellationRequested)
                    {
                        await Task.Delay(3000, ct);
                        await Server.ForceReadAsync();
                        if (Server.IsHost
                            && PreviousRoom.Player2Info != Server.CurrentRoom.Player2Info
                         || !Server.IsHost
                            && PreviousRoom.Player1Info != Server.CurrentRoom.Player1Info) break;
                    }
                }, ct);
            }
            catch { return; }
            if (ct.IsCancellationRequested) return;
            await Server.ForceReadAsync();
            var player2info = GetPlayerInfo().Player2Skl;
            var player2WordName = player2info.CurrentWord.Name;
            if (string.IsNullOrWhiteSpace(player2WordName)) continue;
            SetOrder(new(player2WordName));
        }
        while (false);
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
        PaintWordSurface(wordLbl);
        await wordGrid.FadeTo(1, 200, Easing.Linear);
        if (!word.IsEmpty) SBAudioManager.PlaySound(TypeToSymbol(word.Type1));
    }
    private void PaintWordSurface(Label wordLbl)
    {
        if (isBeforeInit) return;
        const double DEFAULT_FONT_SIZE = 52;
        var name = Battle.OtherPlayer.CurrentWord.Name;
        var fontSize = name.Length < 10 ? 1 : (double)9 / name.Length;
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
    private void SyncPlayerInfo()
    {
        var (player1Skl, player2Skl) = GetPlayerInfo();
        Battle.Player1.Sync(player1Skl);
        Battle.Player2.Sync(player2Skl);
    }
    private async Task ShowInitialCharAsync()
    {
        LblInitialChar.Text = Server.CurrentRoom.InitialChar;
        await BdrInitialChar.FadeTo(1, 250, Easing.Linear);
    }

    private void WordEntry_Completed(object sender, EventArgs e) 
    {
        BtnRegisterWord_Clicked(sender, e); 
    }

    private void BtnSituation_Clicked(object sender, EventArgs e)
    {

    }

    private void BtnAbility_Clicked(object sender, EventArgs e)
    {

    }

    private void BtnEscape_Clicked(object sender, EventArgs e)
    {

    }
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
        await Server.OverwriteAsync(player1Info, player2Info);
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

}