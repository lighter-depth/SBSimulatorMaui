﻿#pragma warning disable CS8632
using System.Text;
using static SBSimulatorMaui.Word;

namespace SBSimulatorMaui;
/// <summary>
/// プレイヤーの情報を管理するクラスです。
/// </summary>
public class Player
{
    #region instance properties

    /// <summary>
    /// 名前
    /// </summary>
    public string Name { get; internal set; } = "じぶん";
    /// <summary>
    /// 呼び出し元の<see cref="Battle"/>クラスの情報
    /// </summary>
    public Battle? Parent { get; private set; }

    /// <summary>
    /// 残り体力
    /// </summary>
    public int HP
    {
        get => _hp;
        internal set => _hp = Math.Min(MaxHP, Math.Max(0, value));
    }
    int _hp;

    /// <summary>
    /// 攻撃力
    /// </summary>
    public double ATK => playerBufCap[_atkIndex];
    int _atkIndex = 6;

    /// <summary>
    /// 攻撃力を指定するインデックス値
    /// </summary>
    public int ATKIndex
    {
        get => _atkIndex;
        internal set => _atkIndex = Math.Min(playerBufCap.Length - 1, Math.Max(0, value));
    }

    /// <summary>
    /// 防御力
    /// </summary>
    public double DEF => playerBufCap[_defIndex];
    int _defIndex = 6;

    /// <summary>
    /// 防御力を指定するインデックス値
    /// </summary>
    public int DEFIndex
    {
        get => _defIndex;
        internal set => _defIndex = Math.Min(playerBufCap.Length - 1, Math.Max(0, value));
    }

    /// <summary>
    /// 使用している単語の情報
    /// </summary>
    public Word CurrentWord { get; internal set; } = new Word(string.Empty, WordType.Empty);

    /// <summary>
    /// とくせいの情報
    /// </summary>
    public Ability Ability { get; internal set; } = new Debugger();

    /// <summary>
    /// 食べ物タイプの単語を使用した回数
    /// </summary>
    public int FoodCount { get; private set; } = 0;

    /// <summary>
    /// 医療タイプの単語を使用した回数
    /// </summary>
    public int CureCount { get; private set; } = 0;

    /// <summary>
    /// 状態異常の情報
    /// </summary>
    public PlayerState State { get; private set; } = PlayerState.Normal;
    public Luck Luck { get; internal set; } = Luck.Normal;
    /// <summary>
    /// プレイヤーが先攻するかどうか
    /// </summary>
    public virtual Proceeds Proceeding { get; internal set; } = Proceeds.Random;

    /// <summary>
    /// どく状態によるダメージ蓄積の値
    /// </summary>
    public int PoisonDmg { get; private set; } = 0;

    /// <summary>
    /// 最大体力の値
    /// </summary>
    public int MaxHP
    {
        get => _maxHP;
        set
        {
            _maxHP = value;
            HP = Math.Min(HP, _maxHP);
        }
    }
    int _maxHP = 60;

    /// <summary>
    /// 残りとくせい変更回数
    /// </summary>
    public int RemainingAbilChangingCount => MaxAbilChange - _abilChangedCount - 1;
    /// <summary>
    /// 使用した動物タイプの情報を保持するリスト
    /// </summary>
    public List<BredString> BrdBuf { get; set; } = new();
    #endregion

    #region static properties

    /// <summary>
    /// とくせいを変更可能な回数
    /// </summary>
    public static int MaxAbilChange { get; set; } = 3;

    /// <summary>
    /// 医療タイプを使用可能な回数
    /// </summary>
    public static int MaxCureCount { get; set; } = 5;

    /// <summary>
    /// やどりぎによるダメージ
    /// </summary>
    public static int SeedDmg { get; set; } = 5;

    /// <summary>
    /// やどりぎの継続するターン数
    /// </summary>
    public static int MaxSeedTurn { get; set; } = 4;

    /// <summary>
    /// 食べ物タイプを使用可能な回数
    /// </summary>
    public static int MaxFoodCount { get; set; } = 6;

    /// <summary>
    /// 急所によるダメージの倍率
    /// </summary>
    public static double CritDmg { get; set; } = 1.5;

    /// <summary>
    /// ほけんにより上昇する攻撃力のインデックス値
    /// </summary>
    public static int InsBufQty { get; set; } = 3;
    #endregion

    #region private fields
    /// <summary>
    /// やどりぎを植えられてから経過したターン数
    /// </summary>
    private int _seedCount = 0;

    /// <summary>
    /// とくせいを変更した回数
    /// </summary>
    internal protected int _abilChangedCount = 0;

    /// <summary>
    /// バフの数値リテラルを管理する配列
    /// </summary>
    private static readonly double[] playerBufCap = new[] { 0.25, 0.28571429, 0.33333333, 0.4, 0.5, 0.66666666, 1.0, 1.5, 2.0, 2.5, 3.0, 3.5, 4.0 };
    #endregion

    #region constructors
    public Player(string name, Ability abil) => (Name, Ability, HP) = (name, abil, MaxHP);
    public Player() : this("じぶん", new Debugger()) { }
    #endregion

    #region enums

    /// <summary>
    /// 状態異常を管理するフラグ
    /// </summary>
    [Flags]
    public enum PlayerState
    {
        /// <summary>
        /// 状態異常になっていない状態を表します。
        /// </summary>
        Normal = 0,
        /// <summary>
        /// どく状態を表します。
        /// </summary>
        Poison = 1,
        /// <summary>
        /// やどりぎ状態を表します。
        /// </summary>
        Seed = 2
    }
    #endregion

    #region methods and change status

    /// <summary>
    /// とくせいを変更します。
    /// </summary>
    /// <param name="abil">変更先のとくせい</param>
    /// <returns>変更が成功したかを表すフラグ</returns>
    public bool TryChangeAbil(Ability abil)
    {
        if (_abilChangedCount < MaxAbilChange - 1)
        {
            Ability = abil;
            _abilChangedCount++;
            return true;
        }
        return false;
    }

    /// <summary>
    /// プレイヤーの状態を文字列として出力します。
    /// </summary>
    /// <returns>プレイヤーの状態を表す文字列</returns>
    public string GetStatusString()
    {
        var seedTurn = State.HasFlag(PlayerState.Seed) ? MaxSeedTurn - _seedCount : 0;
        var currentWordString = CurrentWord.Name == string.Empty ? "(なし)" : CurrentWord.ToString();
        return $"{Name}:\n"
             + $"         HP:   {HP}/{MaxHP},    残り食べ物回数:    {MaxFoodCount - FoodCount}回,          状態: [{State.StateToString()}]\n\n"
             + $"         ATK:  {ATK}倍,      残り医療回数:    {MaxCureCount - CureCount}回,        現在の単語: {currentWordString}\n\n"
             + $"         DEF:  {DEF}倍,      毒のダメージ: {PoisonDmg}ダメージ,        とくせい: {Ability.ToString()}\n\n"
             + $"         残りとくせい変更回数: {MaxAbilChange - _abilChangedCount - 1}回,    残りやどりぎターン: {seedTurn}ターン\n\n";
    }

    /// <summary>
    /// プレイヤーを毒状態にします。
    /// </summary>
    public void Poison()
    {
        State |= PlayerState.Poison;
        PoisonDmg = 0;
    }

    /// <summary>
    /// プレイヤーの毒状態を解除します。
    /// </summary>
    public void DePoison()
    {
        State &= ~PlayerState.Poison;
        PoisonDmg = 0;
    }

    /// <summary>
    /// プレイヤーをやどりぎ状態にします。
    /// </summary>
    public void Seed()
    {
        State |= PlayerState.Seed;
        _seedCount = 0;
    }

    /// <summary>
    /// プレイヤーのやどりぎ状態を解除します。
    /// </summary>
    public void DeSeed()
    {
        State &= ~PlayerState.Seed;
        _seedCount = 0;
    }

    /// <summary>
    /// プレイヤーに毒状態によるダメージを与えます。
    /// </summary>
    public void TakePoisonDmg()
    {
        // NOTE: ダメージ算出はかりうむ式。
        PoisonDmg += (int)(MaxHP * 0.0625);
        HP -= PoisonDmg;
    }

    /// <summary>
    /// 指定したプレイヤーにやどりぎ状態によるダメージを与え、体力を回復します。
    /// </summary>
    /// <param name="other">ダメージを与える大正のプレイヤー</param>
    public void TakeSeedDmg(Player other)
    {
        HP -= SeedDmg;
        var otherHPResult = other.HP + SeedDmg;
        if (otherHPResult > other.MaxHP) other.HP = other.MaxHP;
        else other.HP = otherHPResult;
        if (Parent?.IsSeedInfinite == false) _seedCount++;
        if (_seedCount > MaxSeedTurn) State &= ~PlayerState.Seed;
    }

    /// <summary>
    /// 体力を回復します。
    /// </summary>
    /// <param name="isCure">回復時の単語が医療タイプかどうかを表すフラグ</param>
    public void Heal(bool isCure)
    {
        if (isCure)
        {
            var resultHPCure = HP + 40;
            HP = resultHPCure <= MaxHP ? resultHPCure : MaxHP;
            if (Parent?.IsCureInfinite == false) CureCount++;
            return;
        }
        var resultHPFood = HP + 20;
        HP = resultHPFood <= MaxHP ? resultHPFood : MaxHP;
        FoodCount++;
    }

    /// <summary>
    /// プレイヤーの攻撃力を変更します。
    /// </summary>
    /// <param name="arg">変更する攻撃力のインデックス値</param>
    /// <param name="word">攻撃力変更に使用する単語</param>
    /// <returns>変更が成功したかを表すフラグ</returns>
    public bool TryChangeATK(int arg, Word word)
    {
        var resultIndex = ATKIndex + arg;
        if (resultIndex < 0 || resultIndex == playerBufCap.Length - 1) return false;
        else if (resultIndex >= playerBufCap.Length - 1) ATKIndex = playerBufCap.Length - 1;
        else ATKIndex = resultIndex;
        CurrentWord = word;
        return true;
    }

    /// <summary>
    /// プレイヤーの防御力を変更します。
    /// </summary>
    /// <param name="arg">変更する防御力のインデックス値</param>
    /// <param name="word">防御力変更に使用する単語</param>
    /// <returns>変更が成功したかを表すフラグ</returns>
    public bool TryChangeDEF(int arg, Word word)
    {
        var resultIndex = DEFIndex + arg;
        if (resultIndex < 0 || resultIndex == playerBufCap.Length - 1) return false;
        else if (resultIndex >= playerBufCap.Length - 1) DEFIndex = playerBufCap.Length - 1;
        else DEFIndex = resultIndex;
        CurrentWord = word;
        return true;
    }

    /// <summary>
    /// とくせい「かくめい」を発動します。
    /// </summary>
    /// <param name="other">発動対象のプレイヤー</param>
    public void Rev(Player other)
    {
        var thisDifATK = ATKIndex - 6;
        var thisDifDEF = DEFIndex - 6;
        var otherDifATK = other.ATKIndex - 6;
        var otherDifDEF = other.DEFIndex - 6;
        ATKIndex = 6 - thisDifATK;
        DEFIndex = 6 - thisDifDEF;
        other.ATKIndex = 6 - otherDifATK;
        other.DEFIndex = 6 - otherDifDEF;
    }

    /// <summary>
    /// とくせい「たいふういっか」を発動します。
    /// </summary>
    /// <param name="other">発動対象のプレイヤー</param>
    /// <remarks>ある偉人は、民の前に立ちこう言った。「天で話にならねぇよ...」</remarks>
    public void WZ(Player other)
    {
        ATKIndex = 6;
        DEFIndex = 6;
        other.ATKIndex = 6;
        other.DEFIndex = 6;
    }

    /// <summary>
    /// プレイヤーの情報を複製します。
    /// </summary>
    /// <returns>複製された情報</returns>
    public Player Clone()
    {
        return (Player)MemberwiseClone();
    }
    /// <summary>
    /// <see cref="Battle"/>クラスの情報を登録します。
    /// </summary>
    /// <param name="parent">登録する<see cref="Battle"/>クラスのインスタンス</param>
    public void Register(Battle parent)
    {
        Parent = parent;
    }
    public string Serialize()
    {
        var sb = new StringBuilder();
        sb.Append(Name);
        foreach (var i in new[]
        {
            HP.ToString(), MaxHP.ToString(), ATKIndex.ToString(), DEFIndex.ToString(),
            CurrentWord.Serialize(), AbilityManager.Serialize(Ability.GetType()),
            ((int)State).ToString(), ((int)Proceeding).ToString()
        })
        {
            sb.Append('#' + i);
        }
        return sb.ToString();
    }
    public static Player Deserialize(string textData)
    {
        try
        {
            var data = textData.Split('#');
            var player = new Player
            {
                Name = data[0],
                HP = int.Parse(data[1]),
                MaxHP = int.Parse(data[2]),
                ATKIndex = int.Parse(data[3]),
                DEFIndex = int.Parse(data[4]),
                CurrentWord = Word.Deserialize(data[5]),
                Ability = AbilityManager.Deserialize(data[6]),
                State = (PlayerState)int.Parse(data[7]),
                Proceeding = (Proceeds)int.Parse(data[8]),
            };
            return player;
        }
        catch 
        {
            throw; 
        }
    }
    public void Sync(Player playerData)
    {
        Ability = playerData.Ability;
    }
    #endregion

    #region minor classes

    /// <summary>
    /// とくせい「はんしょく」の情報管理に用いる補助クラス。
    /// </summary>
    public class BredString
    {
        /// <summary>
        /// 単語の名前
        /// </summary>
        public string Name { get; init; } = string.Empty;
        /// <summary>
        /// 単語が使用された回数
        /// </summary>
        public int Rep { get; private set; } = 0;
        public BredString(string name) => (Name, Rep) = (name, 0);
        public void Increment() => Rep++;
    }
    #endregion
}
public enum Luck
{
    Normal,
    Lucky,
    UnLucky
}
