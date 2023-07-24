﻿#pragma warning disable CS8632

using Microsoft.Maui.Graphics;
using static SBSimulatorMaui.SBExtention;

namespace SBSimulatorMaui;

// カスタム特性のテスト。
// CustomAbility クラスを継承して実装する。 

/// <summary>
/// カスタム特性の作成時に使用するクラスです。
/// </summary>
internal abstract class CustomAbility : Ability
{
    public override sealed List<string> Name => CustomName;
    public abstract List<string> CustomName { get; }
}

/// <summary>
/// マジックミラー。状態異常を跳ね返す。
/// </summary>
internal class MagicMirror : CustomAbility
{
    public override AbilityType Type => AbilityType.Received;
    public override List<string> CustomName => new() { "mm", "MM", "マジックミラー", "まじっくみらー", "magicmirror", "MagicMirror", "MAGICMIRROR" };
    public override string Description => "状態異常を受けず、そのまま跳ね返す";
    public override string ImgFile => "mirror.gif";
    public override void Execute(Contract c)
    {
        if (c is AttackContract ac && ac.PoisonFlag && ac.Receiver.State.HasFlag(Player.PlayerState.Poison))
        {
            ac.Receiver.DePoison();
            ac.Actor.Poison();
            ac.Message.Add($"{ac.Receiver.Name} は{Player.PlayerState.Poison.StateToString()}を跳ね返した！", Notice.PoisonHeal, 1);
            ac.Message.Add($"{ac.Actor.Name} は{Player.PlayerState.Poison.StateToString()}を受けた！", Notice.Poison, 0);
        }
        if (c is SeedContract sc && sc.SeedFlag && sc.Receiver.State.HasFlag(Player.PlayerState.Seed))
        {
            sc.Receiver.DeSeed();
            sc.Actor.Seed();
            sc.Message.Add($"{sc.Receiver.Name} は{Player.PlayerState.Seed.StateToString()}を跳ね返した！", Notice.InvokeBufInfo);
            sc.Message.Add($"{sc.Actor.Name} は{Player.PlayerState.Seed.StateToString()}を植え付けられた！", Notice.Seed, 0);
        }
    }
    public override string ToString() => "マジックミラー";
}

/// <summary>
/// てんねん。バフによる能力上昇補正を無視する。
/// </summary>
internal class Tennen : CustomAbility
{
    public override AbilityType Type => AbilityType.MtpCalced;
    public override List<string> CustomName => new() { "ua", "UA", "てんねん", "天然", "unaware", "Unaware", "UNAWARE" };
    public override string Description => "相手の能力の変化を無視する";
    public override string ImgFile => "unaware.gif";
    public override void Execute(Contract c)
    {
        if (c is not AttackContract ac) return;
        if (ac.Actor.Ability.Type.HasFlag(Type))
        {
            ac.MtpDmg = Math.Max(ac.Actor.ATK, 1);
            return;
        }
        ac.MtpDmg = 1 / ac.Receiver.DEF;
    }
    public override string ToString() => "てんねん";
}

/// <summary>
/// ふしぎなまもり。こうかばつぐん以外のダメージを無効化する。
/// </summary>
internal class WonderGuard : CustomAbility
{
    public override AbilityType Type => AbilityType.PropCalced;
    public override List<string> CustomName => new() { "wg", "WG", "ふしぎなまもり", "不思議な守り", "wonderguard", "WonderGuard", "WONDERGUARD" };
    public override string Description => "こうかばつぐんの技しか当たらない不思議な力";
    public override string ImgFile => "shield.gif";
    public override void Execute(Contract c)
    {
        if (c is not AttackContract ac) return;
        if (ac.Receiver.Ability is not WonderGuard) return;
        var prop = ac.Word.CalcAmp(ac.Receiver.CurrentWord);
        ac.PropDmg = prop;
        if (ac.Receiver.Ability is not WonderGuard) return;
        if (prop is < 2 && ac.Receiver.CurrentWord.Type1 != WordType.Empty)
        {
            ac.PropDmg = 0;
            return;
        }
    }
    public override string ToString() => "ふしぎなまもり";
}

/// <summary>
/// がんじょう。即死するダメージを受けたときに、体力１を残して耐える。
/// </summary>
internal class Ganjou : CustomAbility
{
    public override AbilityType Type => AbilityType.ActionBegin | AbilityType.Received;
    public override List<string> CustomName => new() { "st", "ST", "がんじょう", "頑丈", "sturdy", "Sturdy", "STURDY" };
    public override string Description => "HPが満タンのとき、技を受けても一撃で倒されることがない";
    public override string ImgFile => "ganjou.gif";
    bool _invokeFlag = false;
    public override void Execute(Contract c)
    {
        if (c is not AttackContract ac) return;
        if (ac.Receiver.HP <= 0 && ac.Args.PreActor.HP == ac.Args.PreActor.MaxHP && ac.State == AbilityType.ActionBegin)
        {
            ac.Receiver.HP = 1;
            _invokeFlag = true;
        }
        if (_invokeFlag && ac.State == AbilityType.Received)
        {
            ac.Message.Add($"{ac.Receiver.Name} はこうげきをこらえた！", Notice.InvokeBufInfo);
            _invokeFlag = false;
        }
    }
    public override string ToString() => "がんじょう";
}
/// <summary>
/// 「最強の特性」
/// </summary>
internal class God : CustomAbility
{
    public override AbilityType Type => AbilityType.AmpDecided | AbilityType.CritDecided | AbilityType.ViolenceUsed | AbilityType.ActionEnd | AbilityType.Received;
    public override List<string> CustomName => new() { "gd", "GD", "神", "かみ", "カミ", "god", "God", "GOD" };

    // ほんとは本家のテキスト入れたかったけど流石に長すぎる
    public override string Description => "「最強の特性」";
    public override string ImgFile => "god.gif";
    public override void Execute(Contract c)
    {
        if (c is not AttackContract ac) return;
        if (ac.State == AbilityType.AmpDecided)
        {
            ac.AmpDmg = 2 * Math.Max(0, ac.Actor.CurrentWord.Length - 5) + 1;
        }
        if (ac.State == AbilityType.CritDecided)
        {
            ac.CritFlag = true;
        }
        if (ac.State == AbilityType.ViolenceUsed)
        {
            if (ac.Actor.TryChangeATK(Buf, ac.Word))
            {
                ac.Message.Add($"{ac.Actor.Name} の攻撃が下がった！(現在{ac.Actor.ATK,0:0.0#}倍)", Notice.Debuf);
                return;
            }
            ac.Message.Add($"{ac.Actor.Name} の攻撃はもう下がらない！", Notice.Caution);
        }
        if (ac.State == AbilityType.ActionEnd)
        {
            if (ac.Actor.TryChangeATK(4, ac.Word))
            {
                ac.Message.Add($"{ac.Actor.Name} の攻撃がぐーんぐーんと上がった！！！！(現在{ac.Actor.ATK,0:0.0#}倍)", Notice.Buf);
                return;
            }
            ac.Message.Add($"{ac.Actor.Name} の攻撃はもう上がらない！", Notice.Caution);
        }
        if (ac.State == AbilityType.Received)
        {
            ac.Receiver.TryChangeATK(100, ac.Receiver.CurrentWord);
            ac.Message.Add($"{ac.Receiver.Name} はダメージを受けて攻撃がぐぐーんぐーんと上がった！！！！！ (現在{ac.Receiver.ATK,0:0.0#}倍)", Notice.InvokeBufInfo);
            ac.Receiver.TryChangeDEF(100, ac.Receiver.CurrentWord);
            ac.Message.Add($"{ac.Receiver.Name} はダメージを受けて防御がぐぐーんぐーんと上がった！！！１！ (現在{ac.Receiver.DEF,0:0.0#}倍)", Notice.InvokeBufInfo);
        }
    }
    public override string ToString() => "神";
}

/// <summary>
/// 人体タイプの言葉を使った時、５分の１の確率で「確定」する。
/// </summary>
internal class Kakutei : CustomAbility
{
    public override AbilityType Type => AbilityType.PropCalced | AbilityType.CritDecided | AbilityType.ActionExecuted | AbilityType.ActionEnd;
    public override List<string> CustomName => new() { "dc", "DC", "かくてい", "確定", "kakutei", "Kakutei", "KAKUTEI" };
    public override string Description => "人体タイプの言葉を使った時、「確定」することがある";
    public override string ImgFile => "kakutei.gif";
    const int KAKUTEI = 12140000;
    bool _ketsunaanaFlag = false;
    public override void Execute(Contract c)
    {
        if (c is not AttackContract ac) return;
        if (ac.Actor.Ability is not Kakutei) return;
        if(ac.State == AbilityType.PropCalced && ac.Actor.CurrentWord.ContainsType(WordType.Body) && RandomFlag(5, ac.Actor.Luck))
        {
            ac.PropDmg = KAKUTEI;
            _ketsunaanaFlag = true;
        }
        if(ac.State == AbilityType.CritDecided && _ketsunaanaFlag && ac.PropDmg == KAKUTEI)
        {
            ac.CritFlag = true;
        }
        if(ac.State == AbilityType.ActionExecuted && _ketsunaanaFlag && ac.PropDmg == KAKUTEI) 
        {
            ac.Message.RemoveAt(ac.Message.Count - 1);
            ac.Message.RemoveAt(ac.Message.Count - 1);
        }
        if(ac.State == AbilityType.ActionEnd && _ketsunaanaFlag && ac.PropDmg == KAKUTEI)
        {
            ac.Message.Add("一撃必殺！", Notice.EffectiveProp);
            _ketsunaanaFlag = false;
        }
    }
    public override string ToString() => "かくてい";
}
internal class GouyokunaTsubo : CustomAbility, ISingleTypedBufAbility
{
    public override AbilityType Type => AbilityType.ActionBegin;
    public override List<string> CustomName => new() { "pg", "PG", "ごうよくなつぼ", "強欲な壺", "gouyokunatsubo", "GouyokunaTsubo", "GOUYOKUNATSUBO" };
    public override string Description => "感情タイプの言葉を使うと、ダメージを与える代わりにとくせいの変更上限が１増える";
    public override string ImgFile => "tsubo.gif";
    public WordType BufType => WordType.Emote; 
    public override void Execute(Contract c) 
    {
        if (c is not BufContract bc) return;
        if (bc.Actor.CurrentWord.ContainsType(BufType))
        {
            bc.Actor._abilChangedCount--;
            bc.Message.Add($"{bc.Actor.Name} のとくせい変更上限が増えた！(残り{bc.Actor.RemainingAbilChangingCount}回)", Notice.Buf);
        }
    }
    public override string ToString() => "ごうよくなつぼ";
}
internal class AutoAim : CustomAbility
{
    public override AbilityType Type => AbilityType.ContractBegin | AbilityType.ContractEnd;
    public override List<string> CustomName => new() { "aa", "AA", "おーとえいむ", "オートエイム", "autoaim", "AutoAim", "AUTOAIM" };
    public override string Description => "乱数のあるダメージが必ず最高値で出る";
    public override string ImgFile => "autoaim.gif";
    public override void Execute(Contract c)
    {
        if(c.State == AbilityType.ContractBegin) c.Actor.Luck = Luck.Lucky;
        if (c.State == AbilityType.ContractEnd) c.Actor.Luck = Luck.Normal;
    }
    public override string ToString() => "オートエイム";
}