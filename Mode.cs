#pragma warning disable CS8632
using static SBSimulatorMaui.Proceeds;

namespace SBSimulatorMaui;

/// <summary>
/// バトルの初期設定を行うクラスです。
/// </summary>
public class Mode
{
    /// <summary>
    /// インスタンスがストーリーモードであるかどうかを表すフラグ
    /// </summary>
    public bool IsStoryMode = false;
    /// <summary>
    /// <see cref="Battle.Player1"/>がターンを先攻するかどうかを表すフラグ
    /// </summary>
    public Proceeds Player1Proceeds { get; set; } = 0;
    /// <summary>
    /// <see cref="Battle.Player2"/>がターンを先攻するかどうかを表すフラグ
    /// </summary>
    public Proceeds Player2Proceeds { get; set; } = 0;
    /// <summary>
    /// <see cref="Battle.Player1"/>の最大HP
    /// </summary>
    public int Player1MaxHP { get; set; } = 60;
    /// <summary>
    /// <see cref="Battle.Player2"/>の最大HP
    /// </summary>
    public int Player2MaxHP { get; set; } = 60;
    public Luck Player1Luck { get; set; } = Luck.Normal;
    public Luck Player2Luck { get; set; } = Luck.Normal;
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
    /// <summary>
    /// とくせいを変更可能な回数
    /// </summary>
    public int MaxAbilChange { get; set; } = 3;
    /// <summary>
    /// 医療タイプを使用可能な回数
    /// </summary>
    public int MaxCureCount { get; set; } = 5;
    /// <summary>
    /// 食べ物タイプを使用可能な回数
    /// </summary>
    public int MaxFoodCount { get; set; } = 6;
    /// <summary>
    /// やどりぎによるダメージ
    /// </summary>
    public int SeedDmg { get; set; } = 5;
    /// <summary>
    /// やどりぎの継続するターン数
    /// </summary>
    public int MaxSeedTurn { get; set; } = 4;
    /// <summary>
    /// 急所によるダメージの倍率
    /// </summary>
    public double CritDmg { get; set; } = 1.5;
    /// <summary>
    /// ほけんにより上昇する攻撃力のインデックス値
    /// </summary>
    public int InsBufQty { get; set; } = 3;
    /// <summary>
    /// ストーリー モードにおけるCPUの名前を管理します。
    /// </summary>
    public string StoryName { get; set; } = "じぶん";
    public Mode(Proceeds player1Proceeds, Proceeds player2Proceeds, int player1MaxHP, int player2MaxHP, bool isSeedInfinite, bool isCureInfinite, bool isAbilChangeable, int maxAbilChange, int maxCureCount, int maxFoodCount, int seedDmg, int maxSeedTurn, double critDmg, int insBufQty)
    {
        Player1Proceeds = player1Proceeds;
        Player2Proceeds = player2Proceeds;
        Player1MaxHP = player1MaxHP;
        Player2MaxHP = player2MaxHP;
        IsSeedInfinite = isSeedInfinite;
        IsCureInfinite = isCureInfinite;
        IsAbilChangeable = isAbilChangeable;
        MaxAbilChange = maxAbilChange;
        MaxCureCount = maxCureCount;
        MaxFoodCount = maxFoodCount;
        SeedDmg = seedDmg;
        MaxSeedTurn = maxSeedTurn;
        CritDmg = critDmg;
        InsBufQty = insBufQty;
    }
    public Mode(int player1MaxHP, int player2MaxHP, bool isSeedInfinite, bool isCureInfinite, bool isAbilChangeable)
        : this(0, 0, player1MaxHP, player2MaxHP, isSeedInfinite, isCureInfinite, isAbilChangeable, 3, 5, 6, 5, 4, 1.5, 3) { }
    public Mode(Proceeds player1Proceeds, Proceeds player2Proceeds, int player1MaxHP, int player2MaxHP, bool isAbilChangeable, string storyName)
        : this(player1Proceeds, player2Proceeds, player1MaxHP, player2MaxHP, false, false, isAbilChangeable, 3, 5, 6, 5, 4, 1.5, 3) => (StoryName, IsStoryMode) = (storyName, true);
    public Mode()
        : this(60, 60, false, false, true) { }
    /// <summary>
    /// インスタンスの情報を<see cref="Battle"/>クラスのオブジェクトに登録します。
    /// </summary>
    /// <param name="b">登録先の<see cref="Battle"/>クラスのインスタンス</param>
    public void Set(Battle b)
    {
        b.Player1.Proceeding = Player1Proceeds;
        b.Player2.Proceeding = Player2Proceeds;
        b.Player1.MaxHP = Player1MaxHP;
        b.Player2.MaxHP = Player2MaxHP;
        b.Player1.Luck = Player1Luck;
        b.Player2.Luck = Player2Luck;
        b.IsSeedInfinite = IsSeedInfinite;
        b.IsCureInfinite = IsCureInfinite;
        b.IsAbilChangeable = IsAbilChangeable;
        b.IsStrict = IsStrict;
        b.IsInferable = IsInferable;
        b.IsCustomAbilUsable = IsCustomAbilUsable;
        b.IsCPUDelayEnabled = IsCPUDelayEnabled;
        Player.MaxAbilChange = MaxAbilChange;
        Player.MaxCureCount = MaxCureCount;
        Player.MaxFoodCount = MaxFoodCount;
        Player.SeedDmg = SeedDmg;
        Player.MaxSeedTurn = MaxSeedTurn;
        Player.CritDmg = CritDmg;
        Player.InsBufQty = InsBufQty;
        if (!b.IsBeforeInit) return;
        b.Player1.HP = Player1MaxHP;
        b.Player2.HP = Player2MaxHP;
    }
}
/// <summary>
/// <see cref="Mode"/>クラスのインスタンスを作成するファクトリ クラスです。
/// </summary>
class ModeFactory
{
    public static string[] ModeNames => new[] 
    {
        "デフォルト", "クラシック", "やどりぎ時代", "ストーリー #1", "ストーリー #2", "ストーリー #3", "ストーリー #4", "ストーリー #5", "ストーリー #6", "ストーリー #7",
        "ストーリー #8", "ストーリー #9", "ストーリー #10", "ストーリー #11", "ストーリー #12", "ストーリー #13", "カスタム"
    };
    /// <summary>
    /// 文字列からモードを生成します。
    /// </summary>
    /// <param name="name">生成に使用する文字列</param>
    /// <returns>文字列から推論された<see cref="Mode"/>クラスのインスタンス</returns>
    public static (Mode? Mode, string? ModeName) Create(string? name)
    {
        return name?.ToUpper() switch
        {
            "D" or "DEFAULT" or "デフォルト" => (new(), "Default"),
            "1" or "P1" or "PLAYER1PROCEEDS"or "DEFAULTPLAYER1PROCEEDS" => (new(True, False, 60, 60, false, false, true, 3, 5, 6, 5, 4, 1.5, 3), "DefaultPlayer1Proceeds"),
            "2" or "P2" or "PLAYER2PROCEEDS" or "DEFAULTPLAYER2PROCEEDS" => (new(False, True, 60, 60, false, false, true, 3, 5, 6, 5, 4, 1.5, 3), "DefaultPlayer2Proceeds"),
            "C" or "CLASSIC" or "クラシック" => (new(50, 50, true, true, false), "Classic"),
            "S" or "AOS" or "AGEOFSEED" or "やどりぎ時代" => (new(60, 60, true, false, true), "AgeOfSeed"),
            "S1" or "STORY1" or "ストーリー #1" => (new(False, True, 40, 40, false, "あいて"), "Story1"),
            "S2" or "STORY2" or "ストーリー #2" => (new(False, True, 40, 40, false, "うらないし"), "Story2"),
            "S3" or "STORY3" or "ストーリー #3" => (new(False, True, 50, 50, true, "いたまえ"), "Story3"),
            "S4" or "STORY4" or "ストーリー #4" => (new(True, False, 40, 40, true, "ごうとう"), "Story4"),
            "S5" or "STORY5" or "ストーリー #5" => (new(False, True, 40, 40, true, "めんせつかん"), "Story5"),
            "S6" or "STORY6" or "ストーリー #6" => (new(True, False, 40, 40, true, "めんせつかん"), "Story6"),
            "S7" or "STORY7" or "ストーリー #7" => (new(False, True, 40, 40, true, "ゾンビ"), "Story7"),
            "S8" or "STORY8" or "ストーリー #8" => (new(False, True, 40, 40, true, "そう"), "Story8"),
            "S9" or "STORY9" or "ストーリー #9" => (new(False, True, 50, 50, true, "スイカ"), "Story9"),
            "S10" or "SA" or "STORY10" or "STORYA" or "ストーリー #10" => (new(False, True, 40, 40, true, "ちきゅう"), "Story10"),
            "S11" or "SB" or "STORY11" or "STORYB" or "ストーリー #11" => (new(False, True, 40, 40, true, "つよし"), "Story11"),
            "S12" or "SC" or "STORY12" or "STORYC" or "ストーリー #12" => (new(False, True, 40, 40, true, "ひな"), "Story12"),
            "S13" or "SD" or "STORY13" or "STORYD" or "ストーリー #13" => (new(False, True, 100, 100, true, "あに"), "Story13"),
            _ => (null, null)
        };
    }
}

