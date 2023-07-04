#pragma warning disable CS8632

using System.Text.RegularExpressions;
using Umayadia.Kana;
using static SBSimulatorMaui.Word;

namespace SBSimulatorMaui;
// Program クラスで行っている処理を抽出したクラス。
// 文字列で入力し、アノテーション付き文字列で出力する。
/// <summary>
/// バトル単位の処理を管理するクラスです。
/// </summary>
public class Battle
{
    /// <summary>
    /// 一人目のプレイヤー情報
    /// </summary>
    public Player Player1 { get; init; } = new();
    /// <summary>
    /// 二人目のプレイヤー情報
    /// </summary>
    public Player Player2 { get; init; } = new();
    /// <summary>
    /// １ターン前の<see cref="CurrentPlayer"/>の情報
    /// </summary>
    public Player PreActor { get; set; } = new();
    /// <summary>
    /// １ターン前の<see cref="OtherPlayer"/>の情報
    /// </summary>
    public Player PreReceiver { get; set; } = new();
    /// <summary>
    /// <see cref="Player1"/>がターンを持っているかどうかを表すフラグ
    /// </summary>
    public bool IsPlayer1sTurn { get; private set; } = true;
    /// <summary>
    /// 経過したターン数
    /// </summary>
    public int TurnNum { get; private set; } = 1;
    /// <summary>
    /// 使用済みの単語名を保持するリスト
    /// </summary>
    public List<string> UsedWords { get; set; } = new();
    /// <summary>
    /// 入力処理を行うハンドラー
    /// </summary>
    public required Func<Order>? In { get; set; }
    /// <summary>
    /// 出力処理を行うハンドラー
    /// </summary>
    public required Func<List<AnnotatedString>, Task>? Out { get; set; }
    /// <summary>
    /// 出力する情報を保存するバッファー
    /// </summary>
    /// <returns>アノテーション付き文字列のリスト</returns>
    public List<AnnotatedString> Buffer { get; private set; } = new();
    public required Action<Order, CancellationTokenSource>? OnShowOrdered { get; set; }
    public required Action<Order, CancellationTokenSource>? OnResetOrdered { get; set; }
    public required Action<Order, CancellationTokenSource>? OnExitOrdered { get; set; }
    public required Action<Order, CancellationTokenSource>? OnHelpOrdered { get; set; }
    public required Action<Order, CancellationTokenSource>? OnAddOrdered { get; set; }
    public required Action<Order, CancellationTokenSource>? OnRemoveOrdered { get; set; }
    public required Action<Order, CancellationTokenSource>? OnSearchOrdered { get; set; }
    /// <summary>
    /// リセット時の処理を行うハンドラー
    /// </summary>
    public required Action<CancellationTokenSource>? OnReset { get; set; }
    /// <summary>
    /// コマンド列とハンドラーを紐づける辞書
    /// </summary>
    Dictionary<OrderType, Action<Order, CancellationTokenSource>> OrderFunctions = new();
    /// <summary>
    /// アクションを行う側のプレイヤー
    /// </summary>
    public Player CurrentPlayer => IsPlayer1sTurn ? Player1 : Player2;
    /// <summary>
    /// アクションを受ける側のプレイヤー
    /// </summary>
    public Player OtherPlayer => IsPlayer1sTurn ? Player2 : Player1;
    public OrderType CurrentOrderType { get; private set; } = OrderType.None;
    /// <summary>
    /// やどりぎが永続するかどうかを表すフラグです。
    /// </summary>
    public bool IsSeedInfinite { get; set; } = false;
    /// <summary>
    /// 医療タイプの単語による回復が無限に使用可能かどうかを表すフラグです。
    /// </summary>
    public bool IsCureInfinite { get; set; } = false;
    /// <summary>
    /// とくせいの変更が可能かどうかを表すフラグです。
    /// </summary>
    public bool IsAbilChangeable { get; set; } = true;
    /// <summary>
    /// ストリクト モードが有効かどうかを表すフラグです。
    /// </summary>
    public bool IsStrict { get; set; } = true;
    /// <summary>
    /// タイプ推論が有効かどうかを表すフラグです。
    /// </summary>
    public bool IsInferable { get; set; } = true;
    /// <summary>
    /// カスタムとくせいが使用可能かどうかを表すフラグです。
    /// </summary>
    public bool IsCustomAbilUsable { get; set; } = true;
    /// <summary>
    /// CPUの行動に待ち時間を設けるかを表すフラグです。
    /// </summary>
    public bool IsCPUDelayEnabled { get; set; } = true;
    public bool IsBeforeInit { get; private set; } = true;

    public char NextChar => OtherPlayer.CurrentWord.LastChar;
    static readonly Action<Order, CancellationTokenSource> emptyDelegate = (o, c) => { };
    CancellationTokenSource cts = new();
    public static Battle Empty => new(new(), new())
    {
        In = () => new(),
        Out = async a => await Task.Delay(0),
        OnShowOrdered = emptyDelegate,
        OnResetOrdered = emptyDelegate,
        OnHelpOrdered = emptyDelegate,
        OnExitOrdered = emptyDelegate,
        OnAddOrdered = emptyDelegate,
        OnRemoveOrdered = emptyDelegate,
        OnSearchOrdered = emptyDelegate,
        OnReset = c => { }
    };

    public Battle(Player p1, Player p2) => (Player1, Player2) = (p1, p2);
    public Battle() : this(new(), new()) { }

    public void Dispose()
    {
        OnReset(cts);
    }

    public async void Run()
    {
        Initialize();
        IsBeforeInit = false;
        await Out(Buffer);
        cts = new CancellationTokenSource();
        while (!cts.IsCancellationRequested)
        {
            Buffer.Clear();

            // 入力処理、CPU かどうかを判定
            var order = CurrentPlayer is not CPUPlayer cpu ? await Task.Run(() => In()) : await cpu.ExecuteAsync();
            CurrentOrderType = order.Type;
            if (order.Type is OrderType.None)
            {
                await Out(Buffer);
                continue;
            }

            // 辞書 OrderFunctions からコマンド名に合致するハンドラーを取り出す
            if (OrderFunctions.TryGetValue(order.Type, out var func))
                func(order, cts);
            else
                OnDefault(order, cts);

            // 出力処理
            await Out(Buffer);

        }
    }
    private void Initialize()
    {
        IsPlayer1sTurn = InitIsP1sTurn();
        Buffer.Add($"{CurrentPlayer.Name} のターンです", Notice.General);
        Buffer.Add($"{Player1.Name}: {Player1.HP}/{Player1.MaxHP},     {Player2.Name}: {Player2.HP}/{Player2.MaxHP}", Notice.LogInfo);
        OrderFunctions = new()
        {
            [OrderType.Error] = OnErrorOrdered,
            [OrderType.Change] = OnChangeOrdered,
            [OrderType.EnablerOption] = OnEnablerOptionOrdered,
            [OrderType.NumOption] = OnNumOptionOrdered,
            [OrderType.PlayerNumOption] = OnPlayerNumOptionOrdered,
            [OrderType.PlayerStringOption] = OnPlayerStringOptionOrdered,
            [OrderType.ModeOption] = OnModeOptionOrdered,
            [OrderType.Show] = OnShowOrdered,
            [OrderType.AI] = OnAIOrdered,
            [OrderType.Reset] = OnResetOrdered,
            [OrderType.Exit] = OnExitOrdered,
            [OrderType.Help] = OnHelpOrdered,
            [OrderType.Add] = OnAddOrdered,
            [OrderType.Remove] = OnRemoveOrdered,
            [OrderType.Search] = OnSearchOrdered
        };
    }
    /// <summary>
    /// 先攻・後攻の設定を行います。
    /// </summary>
    /// <returns><see cref="Player1"/>が先攻するかどうかを表すフラグ</returns>
    private bool InitIsP1sTurn()
    {
        var randomFlag = new Random().Next(2) == 0;
        var p1TPA = Player1.Proceeding;
        var p2TPA = Player2.Proceeding;
        if (p1TPA == p2TPA) return randomFlag;
        if (p1TPA == Proceeds.True || p2TPA == Proceeds.False) return true;
        if (p1TPA == Proceeds.False || p2TPA == Proceeds.True) return false;
        return randomFlag;
    }

    /// <summary>
    /// デフォルトで実行されるハンドラーです。単語の種別に応じて<see cref="Contract"/>を生成し処理します。
    /// </summary>
    public void OnDefault(Order order, CancellationTokenSource cts)
    {
        Word? word;

        // 単語をタイプ推論し、生成する。

        // タイプ推論が成功したかどうかを表すフラグ。
        bool isInferSuccessed;

        // タイプ推論が有効な場合。推論できない場合は無属性。
        if (IsInferable && order.TypeParam is null)
        {
            var name = KanaConverter.ToHiragana(order.Body).Replace('ヴ', 'ゔ');
            isInferSuccessed = TryInferWordTypes(name, out Word wordtemp);
            word = wordtemp;
        }
        // タイプ推論が無効な場合。手動で入力されたタイプを参照し、単語に変換する。
        // 単語の書式が不正な場合には、isInferSuccessed を false に設定する。
        else
        {
            var type1 = order.TypeParam?.Length > 0 ? order.TypeParam?[0].CharToType() : WordType.Empty;
            var type2 = order.TypeParam?.Length > 0 ? order.TypeParam?.Length > 1 ? order.TypeParam?[1].CharToType() : WordType.Empty : WordType.Empty;
            word = new Word(order.Body, CurrentPlayer, OtherPlayer, type1 ?? WordType.Empty, type2 ?? WordType.Empty);
            isInferSuccessed = word.Name.IsWild() || new Regex("^[ぁ-ゔゟァ-ヴー]*$").IsMatch(word.Name);
        }

        // ContractType の決定。
        var ct = word.IsBuf ? ContractType.Buf
               : word.IsHeal ? ContractType.Heal
               : word.IsSeed ? ContractType.Seed
               : ContractType.Attack;
        var c = Contract.Create(ct, CurrentPlayer, OtherPlayer, word, this, new ContractArgs(PreActor, PreReceiver) { IsInferSuccessed = isInferSuccessed });
        c.Execute();

        // Contract の情報をバッファーに追加する。
        Buffer.Add(ct switch
        {
            ContractType.Attack => $"{CurrentPlayer.Name} は単語 {word} で攻撃した！",
            ContractType.Buf => $"{CurrentPlayer.Name} は単語 {word} で能力を高めた！",
            ContractType.Heal => $"{CurrentPlayer.Name} は単語 {word} を使った！",
            ContractType.Seed => $"{CurrentPlayer.Name} は単語 {word} でやどりぎを植えた！",
            _ => throw new ArgumentException($"ContractType \"{ct}\" has not been implemented.")
        }, Notice.LogActionInfo);

        Buffer.AddMany(c.Message);

        // プレイヤーが死んだかどうかの判定、ターンの交代。
        if (c.DeadFlag)
        {
            Out(Buffer);
            OnReset(cts);
        }
        if (c.IsBodyExecuted) ToggleTurn();
    }
    public void OnErrorOrdered(Order order, CancellationTokenSource cts)
    {
        Buffer.Add(order?.ErrorMessage ?? "入力が不正です", Notice.Warn);
    }

    /// <summary>
    /// とくせいを変更する際に実行されるハンドラーです。
    /// </summary>
    public void OnChangeOrdered(Order order, CancellationTokenSource cts)
    {
        // 「変更可能な特性」設定の確認
        if (!IsAbilChangeable)
        {
            Buffer.Add("設定「変更可能な特性」がオフになっています。", Notice.Warn);
            return;
        }
        var player = PlayerSelectorToPlayerOrDefault(order.Selector);
        if (player is null)
        {
            Buffer.Add("条件に一致するプレイヤーが見つかりませんでした。", Notice.Warn);
            return;
        }
        var nextAbil = AbilityManager.Create(order.Body, IsCustomAbilUsable);
        if (nextAbil is null)
        {
            Buffer.Add($"入力 {order.Body} に対応するとくせいが見つかりませんでした。", Notice.Warn);
            return;
        }
        if (nextAbil == player.Ability)
        {
            Buffer.Add("既にそのとくせいになっている！", Notice.Warn);
            return;
        }
        if (player.TryChangeAbil(nextAbil))
        {
            Buffer.Add($"{player.Name} はとくせいを {nextAbil.ToString()} に変更しました", Notice.SystemInfo);
        }
        else
            Buffer.Add($"{player.Name} はもう特性を変えられない！", Notice.Caution);

    }
    public void OnEnablerOptionOrdered(Order order, CancellationTokenSource cts)
    {
        var enabler = order.Enabler;
        if (order.Option == Options.InfiniteSeed)
        {
            IsSeedInfinite = enabler;
            Buffer.Add($"やどりぎの継続ターン数を {(enabler ? "無限" : $"{Player.MaxSeedTurn}ターン")} に変更しました。", Notice.SettingInfo);
            return;
        }
        if (order.Option == Options.InfiniteCure)
        {
            IsCureInfinite = enabler;
            Buffer.Add($"医療タイプの単語で回復可能な回数を {(enabler ? "無限" : $"{Player.MaxCureCount}回")} に変更しました。", Notice.SettingInfo);
            return;
        }
        if (order.Option == Options.AbilChange)
        {
            IsAbilChangeable = enabler;
            Buffer.Add($"とくせいの変更を{(enabler ? $"有効にしました。(上限 {Player.MaxAbilChange}回 まで)" : "無効にしました")}", Notice.SettingInfo);
            return;
        }
        if (order.Option == Options.Strict)
        {
            IsStrict = enabler;
            Buffer.Add($"ストリクト モードを{(enabler ? "有効" : "無効")}にしました。", Notice.SettingInfo);
            return;
        }
        if (order.Option == Options.Infer)
        {
            IsInferable = true;
            Buffer.Add($"タイプの推論を{(enabler ? "有効" : "無効")}にしました。", Notice.SettingInfo);
            return;
        }
        if (order.Option == Options.CustomAbil)
        {
            IsCustomAbilUsable = enabler;
            Buffer.Add($"カスタム特性を{(enabler ? "有効" : "無効")}にしました。", Notice.SettingInfo);
            if (enabler) return;
            if (Player1.Ability is CustomAbility)
            {
                Buffer.Add($"特性が見つかりません。{Player1.Name} の特性をデバッガーに設定します。", Notice.Caution);
                Player1.Ability = new Debugger();
            }
            if (Player2.Ability is CustomAbility)
            {
                Buffer.Add($"特性が見つかりません。{Player2.Name} の特性をデバッガーに設定します。", Notice.Caution);
                Player2.Ability = new Debugger();
            }
            return;
        }
        if (order.Option == Options.CPUDelay)
        {
            IsCPUDelayEnabled = enabler;
            Buffer.Add($"CPUの待ち時間を{(enabler ? "有効" : "無効")}にしました。", Notice.SettingInfo);
            return;
        }
    }
    public void OnNumOptionOrdered(Order order, CancellationTokenSource cts)
    {
        var intParam = (int)order.Param[0];
        if (order.Option == Options.SetAbilCount)
        {
            Player.MaxAbilChange = intParam;
            Buffer.Add($"とくせいの変更回数上限を {Player.MaxAbilChange}回 に設定しました。", Notice.SettingInfo);
            return;
        }
        if (order.Option == Options.SetMaxCureCount)
        {
            Player.MaxCureCount = intParam;
            Buffer.Add($"医療タイプの単語による回復の回数上限を {Player.MaxCureCount}回 に設定しました。", Notice.SettingInfo);
            return;
        }
        if (order.Option == Options.SetMaxFoodCount)
        {
            Player.MaxFoodCount = intParam;
            Buffer.Add($"食べ物タイプの単語による回復の回数上限を {Player.MaxFoodCount}回 に設定しました。", Notice.SettingInfo);
            return;
        }
        if (order.Option == Options.SetSeedDmg)
        {
            Player.SeedDmg = intParam;
            Buffer.Add($"やどりぎによるダメージを {Player.SeedDmg} に設定しました。", Notice.SettingInfo);
            return;
        }
        if (order.Option == Options.SetMaxSeedTurn)
        {
            Player.MaxSeedTurn = intParam;
            Buffer.Add($"やどりぎの継続ターン数を {Player.MaxSeedTurn}ターン に設定しました。", Notice.SettingInfo);
            return;
        }
        if (order.Option == Options.SetCritDmgMultiplier)
        {
            Player.CritDmg = order.Param[0];
            Buffer.Add($"急所によるダメージ倍率を {Player.CritDmg}倍 に設定しました。", Notice.SettingInfo);
            return;
        }
        if (order.Option == Options.SetInsBufQty)
        {
            Player.InsBufQty = intParam;
            Buffer.Add($"ほけん発動による攻撃力の変化を {Player.InsBufQty} 段階 に設定しました。", Notice.SettingInfo);
            return;
        }
    }
    private void OnPlayerNumOptionOrdered(Order order, CancellationTokenSource cts)
    {
        var player = PlayerSelectorToPlayerOrDefault(order.Selector);
        if (player is null)
        {
            Buffer.Add("条件に一致するプレイヤーが見つかりませんでした。", Notice.Warn);
            return;
        }
        if (order.Option == Options.SetMaxHP)
        {
            player.MaxHP = (int)order.Param[0];
            if (player.HP > player.MaxHP) player.HP = player.MaxHP;
            Buffer.Add($"{player.Name} の最大HPを {player.MaxHP} に設定しました。", Notice.SettingInfo);
            return;
        }
        if(order.Option == Options.SetHP)
        {
            player.HP = Math.Max(Math.Min((int)order.Param[0], player.MaxHP), 1);
            Buffer.Add($"{player.Name} のHPを {player.HP} に設定しました。", Notice.SettingInfo);
            return;
        }
        if(order.Option == Options.SetATK)
        {
            player.ATKIndex = (int)order.Param[0];
            Buffer.Add($"{player.Name} の攻撃を {player.ATKIndex - 6} に設定しました。", Notice.SettingInfo);
            return;
        }
        if (order.Option == Options.SetDEF)
        {
            player.DEFIndex = (int)order.Param[0];
            Buffer.Add($"{player.Name} の防御を {player.DEFIndex - 6} に設定しました。", Notice.SettingInfo);
            return;
        }
        if(order.Option == Options.SetCurrentWord)
        {

            if (!TryInferWordTypes(order.Body, out var word)) 
            { 
                Buffer.Add("辞書にない単語です", Notice.Warn);
                return;
            }
            player.CurrentWord = word;
            Buffer.Add($"{player.Name} の単語を {player.CurrentWord.Name} に設定しました。", Notice.SettingInfo);
            return;
        }
    }
    private void OnPlayerStringOptionOrdered(Order order, CancellationTokenSource cts) 
    {
        var player = PlayerSelectorToPlayerOrDefault(order.Selector);
        if (player is null)
        {
            Buffer.Add("条件に一致するプレイヤーが見つかりませんでした。", Notice.Warn); 
            return;
        }
        if(order.Option == Options.SetLuck)
        {
            var luck = order.Body.ToLower() switch
            {
                "max" or "l" or "lucky" => Luck.Lucky,
                "min" or "u" or "unlucky" => Luck.UnLucky,
                _ => Luck.Normal
            };
            player.Luck = luck;
            Buffer.Add($"{player.Name} の運を {luck switch 
            {
                Luck.Lucky => "最大",
                Luck.UnLucky => "最小",
                _ => "通常"
            }} に設定しました。", Notice.SettingInfo);
        }
    }
    private void OnModeOptionOrdered(Order order, CancellationTokenSource cts)
    {
        if (order.Option == Options.SetMode)
        {
            var (mode, modeName) = ModeFactory.Create(order.Body);
            if (mode is null)
            {
                Buffer.Add($"モード {order.Body} が見つかりません。", Notice.Warn);
                return;
            }
            mode.Set(this);
            Buffer.Add($"モードを {modeName} に設定しました。", Notice.SettingInfo);
            return;
        }
    }
    private void OnAIOrdered(Order order, CancellationTokenSource cts)
    {
        Buffer.Add("「AIの変更」機能は開発中です", Notice.SystemInfo);
    }

    /// <summary>
    /// 文字列のタイプを推論し、単語を出力します。
    /// </summary>
    /// <param name="name">推論元の文字列</param>
    /// <param name="word">出力される文字</param>
    /// <returns>タイプ推論が成功したかを表すフラグ</returns>
    public bool TryInferWordTypes(string name, out Word word)
    {
        if (name.IsWild())
        {
            word = new Word(name, CurrentPlayer, OtherPlayer, WordType.Empty);
            return true;
        }
        if (SBDictionary.TypedWords.TryGetValue(name, out var types))
        {
            var type1 = types[0];
            var type2 = types.Count > 1 ? types[1] : WordType.Empty;
            word = new Word(name, CurrentPlayer, OtherPlayer, type1, type2);
            return true;
        }
        if (SBDictionary.NoTypeWords.Contains(name) || SBDictionary.NoTypeWordEx.Contains(name))
        {
            word = new Word(name, CurrentPlayer, OtherPlayer, WordType.Empty);
            return true;
        }
        word = new Word();
        return false;
    }

    /// <summary>
    /// ひとつ前のプレイヤー情報を更新し、ターンを交替します。
    /// </summary>
    public void ToggleTurn()
    {
        PreActor = CurrentPlayer.Clone();
        PreReceiver = OtherPlayer.Clone();
        IsPlayer1sTurn = !IsPlayer1sTurn;
        TurnNum++;
        if (TurnNum > 1)
        {
            Buffer.Add($"{CurrentPlayer.Name} のターンです", Notice.General);
            Buffer.Add($"{Player1.Name}: {Player1.HP}/{Player1.MaxHP},     {Player2.Name}: {Player2.HP}/{Player2.MaxHP}", Notice.LogInfo);
        }
    }
    internal Player? PlayerSelectorToPlayerOrDefault(PlayerSelector selector)
    {
        return selector switch
        {
            PlayerSelector.Player1 => Player1,
            PlayerSelector.Player2 => Player2,
            _ => null
        };
    }
}
