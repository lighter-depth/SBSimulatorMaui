﻿#pragma warning disable CS8632

using static SBSimulatorMaui.Word;
using static SBSimulatorMaui.SBExtention;
using System.Reflection;
using System.Diagnostics.CodeAnalysis;

namespace SBSimulatorMaui;

/// <summary>
/// プレイヤーがターンを先攻するかどうかを決定します。
/// </summary>
public enum Proceeds
{
    /// <summary>
    /// プレイヤーが先攻かどうかはランダムに決定されます。
    /// </summary>
    Random,
    /// <summary>
    /// プレイヤーは必ず先攻になります。
    /// </summary>
    True,
    /// <summary>
    /// プレイヤーは必ず後攻になります。
    /// </summary>
    False
}
/// <summary>
/// <see cref="CPUPlayer"/>クラスのインスタンスを作成するファクトリ クラスです。
/// </summary>
internal class CPUManager
{
    public static List<CPUPlayer> CPUPlayers => _cpuPlayers ??= GetCPUPlayers();

    static List<CPUPlayer> _cpuPlayers;
    static List<CPUPlayer> GetCPUPlayers()
    {
        var result = new List<CPUPlayer>();
        var subClasses = Assembly.GetAssembly(typeof(CPUPlayer))?.GetTypes()
            .Where(x => x.IsSubclassOf(typeof(CPUPlayer)) && !x.IsAbstract).ToArray() ?? Array.Empty<Type>();
        foreach (var i in subClasses) result.Add(Activator.CreateInstance(i) as CPUPlayer);
        return result;
    }
    /// <summary>
    /// 文字列からCPUを生成します。
    /// </summary>
    /// <param name="name">生成に使用する文字列</param>
    /// <returns>文字列から推論された<see cref="CPUPlayer"/>クラスのインスタンス</returns>
    public static CPUPlayer? Create(string name)
    {
        foreach (var i in CPUPlayers)
        {
            if (i.ReferedName.Contains(name))
            {
                i.Name = i.CPUName;
                i.Ability = i.FirstAbility;
                return i.Clone() as CPUPlayer;
            }
        }
        return null;
    }
}

/// <summary>
/// コンピューターにより行動ルーチンを決定するプレイヤーを表すスーパークラスです。
/// </summary>
internal abstract class CPUPlayer : Player
{
    /// <summary>
    /// CPUのプレイヤーに設定する名前
    /// </summary>
    public abstract string CPUName { get; set; }
    /// <summary>
    /// CPUのプレイヤーを生成するときに参照する名前
    /// </summary>
    public abstract List<string> ReferedName { get; }
    /// <summary>
    /// CPUの初期とくせい
    /// </summary>
    public virtual Ability FirstAbility => new Debugger();
    static readonly int _millisecondsDelay = 1000;
    public CPUPlayer(string name, Ability ability) : base(name, ability) { }
    public CPUPlayer() : base() { }
    /// <summary>
    /// CPUの思考ルーチンを実行します。
    /// </summary>
    /// <returns><see cref="Battle"/>クラスへの命令を表す文字列</returns>
    public async Task<Order> ExecuteAsync()
    {
            var timer = Parent?.IsCPUDelayEnabled == true ? Task.Delay(_millisecondsDelay) : Task.Run(() => { });
            var result = await Task.Run(() => Execute());
            await timer;
            return result;
    }
    /// <summary>
    /// CPUの思考ルーチンを実行します。
    /// </summary>
    /// <returns><see cref="Battle"/>クラスへの命令を表す文字列</returns>
    public abstract Order Execute();
    /// <summary>
    /// 指定した条件に合う単語を検索します。
    /// </summary>
    /// <param name="startChar">最初の文字の指定</param>
    /// <param name="type">タイプの指定</param>
    /// <param name="word">出力された<see cref="Word"/>クラスのインスタンス</param>
    /// <returns>単語が見つかったかどうかを表すフラグ</returns>
    public bool TryWordSearchByType(char startChar, WordType type, [NotNullWhen(true)]out Word? word)
    {
        word = null;
        if (Parent is null) return false;
        word = new();
        var resultWords = SBDictionary.TypedWords.Where(x => x.Key[0] == startChar && x.Value.Contains(type) && !Parent.UsedWords.Contains(x.Key)).ToDictionary(p => p.Key, p => p.Value);
        var random = SBOptions.Random.Next(resultWords.Count);
        if (resultWords.Count != 0 && Parent.TryInferWordTypes(resultWords.ElementAt(random).Key, out var wordTemp))
        {
            word = wordTemp;
            return true;
        }
        return false;
    }
    /// <summary>
    /// 指定した条件に合う単語を検索します。
    /// </summary>
    /// <param name="startChar">最初の文字の指定</param>
    /// <param name="type1">タイプ1の指定</param>
    /// <param name="type2">タイプ1の指定</param>
    /// <param name="word">出力された<see cref="Word"/>クラスのインスタンス</param>
    /// <returns>単語が見つかったかどうかを表すフラグ</returns>
    public bool TryWordSearchByType(char startChar, WordType type1, WordType type2, [NotNullWhen(true)] out Word? word)
    {
        word = null;
        if (Parent is null) return false;
        word = new();
        var resultWords = SBDictionary.TypedWords.Where(x => x.Key[0] == startChar && x.Value.Contains(type1) && x.Value.Contains(type2) && !Parent.UsedWords.Contains(x.Key)).ToDictionary(p => p.Key, p => p.Value);
        var random = SBOptions.Random.Next(resultWords.Count);
        if (resultWords.Count != 0 && Parent.TryInferWordTypes(resultWords.ElementAt(random).Key, out var wordTemp))
        {
            word = wordTemp;
            return true;
        }
        return false;
    }
    /// <summary>
    /// 指定した条件に合う単語を探します。
    /// </summary>
    /// <param name="pred">条件の指定</param>
    /// <param name="word">出力された<see cref="Word"/>クラスのインスタンス</param>
    /// <returns>単語が見つかったかどうかを表すフラグ</returns>
    public bool TryWordSearchByName(Predicate<string> pred, [NotNullWhen(true)]out Word? word)
    {
        word = null;
        if (Parent is null) return false;
        var resultWords = SBDictionary.PerfectNameDic.Where(x => pred(x) && !Parent.UsedWords.Contains(x)).ToList();
        var random = SBOptions.Random.Next(resultWords.Count);
        if (resultWords.Count != 0 && Parent.TryInferWordTypes(resultWords[random], out var wordTemp))
        {
            word = wordTemp;
            return true;
        }
        return false;
    }
    /// <summary>
    /// 指定した条件に合うタイプ付きの単語を探します。
    /// </summary>
    /// <param name="pred">条件の指定</param>
    /// <param name="word">出力された<see cref="Word"/>クラスのインスタンス</param>
    /// <returns>単語が見つかったかどうかを表すフラグ</returns>
    public bool TrySearchTypedWord(Predicate<string> pred, [NotNullWhen(true)] out Word? word)
    {
        word = null;
        if (Parent is null) return false;
        var resultWords = SBDictionary.TypedWords.Keys.Where(x => pred(x) && !Parent.UsedWords.Contains(x)).ToList();
        var random = SBOptions.Random.Next(resultWords.Count);
        if (resultWords.Count != 0 && Parent.TryInferWordTypes(resultWords[random], out var wordTemp))
        {
            word = wordTemp;
            return true;
        }
        return false;
    }
    // 8倍弱点以上をつける単語の検索。
    // 挙動は後々改善予定。
    public bool TrySearchKillWord(char startChar, Word prev, [NotNullWhen(true)] out Word? word)
    {
        word = null;
        var resultTypeList = new List<Word>();
        var resultList = new List<Word>();
        if(Parent is null) return false;
        for(var i = 0; i < NUMBER_OF_TYPES; i++)
        {
            for(var j = 0; j < NUMBER_OF_TYPES; j++)
            {
                var w = new Word(string.Empty, (WordType)i, (WordType)j);
                if (w.CalcAmp(prev) >= 8 * Parent.CurrentPlayer.ATK / Parent.OtherPlayer.DEF) resultTypeList.Add(w);
            }
        }
        if (resultTypeList.Count == 0) return false;
        foreach(var i in resultTypeList)
        {
            if (TryWordSearchByType(startChar, i.Type1, i.Type2, out var wordTemp))
                resultList.Add(wordTemp);
        }
        if(resultList.Count > 0)
        {
            word = resultList[SBOptions.Random.Next(resultList.Count)];
            return true;
        }
        return false;
    }
    /// <summary>
    /// 無条件に単語を検索します。
    /// </summary>
    /// <param name="startchar">最初の文字の指定</param>
    /// <returns>出力された<see cref="Word"/>クラスのインスタンス</returns>
    public Word FindSomeWord(char startchar)
    {
        if (Parent is null) return new();
        var result = new Word();
        foreach (var i in SBDictionary.TypedWords.Keys)
        {
            if (i?[0] == startchar && !Parent.UsedWords.Contains(i))
            {
                Parent?.TryInferWordTypes(i, out result);
                return result;
            }
        }
        foreach (var i in SBDictionary.NoTypeWords)
        {
            if (i?[0] == startchar && !Parent.UsedWords.Contains(i))
            {
                result = new(i, this, Parent.OtherPlayer, WordType.Empty);
                return result;
            }
        }
        return result;
    }
    /// <summary>
    /// 単語探索時に参照する最初の文字を取得します。
    /// </summary>
    /// <returns>最初の文字</returns>
    public char GetStartChar()
    {
        return Parent is null ? GetRandomChar()
             : string.IsNullOrWhiteSpace(Parent.OtherPlayer.CurrentWord.Name) ? GetRandomChar()
             : Parent.OtherPlayer.CurrentWord.LastChar;
    }
    /// <summary>
    /// 現在相手が場に出している単語を取得します。
    /// </summary>
    /// <returns>相手が現在使用している単語の情報</returns>
    public Word GetLastWord()
    {
        return Parent is null ? new()
            : Parent.OtherPlayer.CurrentWord;
    }
}
/// <summary>
/// いたまえのAIです。
/// </summary>
internal class Itamae : CPUPlayer
{
    public override Ability FirstAbility => new Ikasui();
    public override string CPUName { get; set; } = "いたまえ";
    public override List<string> ReferedName => new() { "いたまえ", "s3", "S3" };
    public override Order Execute()
    {
        var startchar = GetStartChar();
        if (TryWordSearchByType(startchar, WordType.Food, out var wordFood))
        {
            return new(wordFood.Name);
        }
        return new(FindSomeWord(startchar).Name);
    }
}
/// <summary>
/// つよしのAIです。
/// </summary>
internal class Tsuyoshi : CPUPlayer
{
    public override Ability FirstAbility => new Kakumei();
    public override string CPUName { get; set; } = "つよし";
    public override List<string> ReferedName => new() { "つよし", "s11", "S11", "sb", "SB" };
    public int ViolenceUsed { get; private set; } = 0;
    public bool PlayUsed { get; private set; } = false;
    public Tsuyoshi(string name, Ability ability) : base(name, ability) { }
    public Tsuyoshi() : base() { }
    public override Order Execute()
    {
        var startchar = GetStartChar();
        if (ViolenceUsed < 2 && TryWordSearchByType(startchar, WordType.Violence, out var wordViolence))
        {
            ViolenceUsed++;
            return new(wordViolence.Name);
        }
        if (ViolenceUsed >= 2 && !PlayUsed && TryWordSearchByType(startchar, WordType.Play, out var wordPlay))
        {
            PlayUsed = true;
            return new(wordPlay.Name);
        }
        if (ViolenceUsed >= 2 && PlayUsed && TryWordSearchByType(startchar, WordType.Violence, out var wordViolenceAttacker))
        {
            return new(wordViolenceAttacker.Name);
        }
        return new(FindSomeWord(startchar).Name);
    }
}
/// <summary>
/// あにのAIです。
/// </summary>
internal class Ani : CPUPlayer
{
    public override Ability FirstAbility => new Oremoji();
    public override string CPUName { get; set; } = "あに";
    public override List<string> ReferedName => new() { "あに", "s13", "S13", "sd", "SD" };
    public Ani(string name, Ability ability) : base(name, ability) { }
    public Ani() : base() { }
    public override Order Execute()
    {
        for (var i = 30; i > 0; i--) if (TrySearchTypedWord(x => x[0] == GetStartChar() && x.Length >= i, out var word)) return new(word.Name);
        return new(FindSomeWord(GetStartChar()).Name);
    }
}
/// <summary>
/// ぬ攻めを行うAiです。
/// </summary>
internal class NuzemeAI : CPUPlayer
{
    public override Ability FirstAbility => new Oremoji();
    public override string CPUName { get; set; } = "ぬぜめマン";
    public override List<string> ReferedName => new() { "ぬぜめ", "ぬぜめマン", "nz" };
    public NuzemeAI(string name, Ability ability) : base(name, ability) { }
    public NuzemeAI() : base() { }
    // HACK: 探索のアルゴリズムを改善したい。
    public override Order Execute()
    {
        if (Parent is null) return new();
        var startchar = GetStartChar();
        var word = GetLastWord();
        if(TrySearchKillWord(startchar, Parent.OtherPlayer.CurrentWord, out var killWord))
        {
            return new(killWord.Name);
        }
        if (TryWordSearchByName(x => x.Length > 6 && x[0] == startchar && x[^1] == 'ぬ', out var wordNuzeme7))
        {
            return new(wordNuzeme7.Name);
        }
        if(TryWordSearchByName(x => x.Length == 6 && x[0] == startchar && x[^1] == 'ぬ', out var wordNuzeme6))
        {
            return new(wordNuzeme6.Name);
        }
        if (TryWordSearchByName(x => x.Length > 0 && x[0] == startchar && x[^1] == 'ぬ', out var wordNuzeme))
        {
            return new(wordNuzeme.Name);
        }
        if (TryWordSearchByName(x => x.Length > 6 && x[0] == startchar && x[^1] == 'ぐ', out var wordGuzeme7))
        {
            return new(wordGuzeme7.Name);
        }
        if (TryWordSearchByName(x => x.Length == 6 && x[0] == startchar && x[^1] == 'ぐ', out var wordGuzeme6))
        {
            return new(wordGuzeme6.Name);
        }
        if (TryWordSearchByName(x => x.Length > 0 && x[0] == startchar && x[^1] == 'ぐ', out var wordGuzeme))
        {
            return new(wordGuzeme.Name);
        }
        return new(FindSomeWord(startchar).Name);
    }
}
