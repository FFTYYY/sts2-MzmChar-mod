using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;

namespace MzmChar.Game;

/// <summary>
/// 「传话筒」小睦版 buff：回合开始时额外抽1张牌（计入 ExtraDrawsThisTurnPower）。
/// 应用时根据当时形态决定是 Mu/Mo 版本（看 Megaphone 卡的 OnPlay）。
/// </summary>
public class MegaphoneMutsumiPower : CustomPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override string? CustomPackedIconPath => "res://MzmChar/powers/megaphone_mu.png";
    public override string? CustomBigIconPath => "res://MzmChar/powers/megaphone_mu.png";

    // 参考 vanilla MachineLearningPower 的 ModifyHandDraw 模式 —— 走自然抽牌 pipeline，
    // 而不是 AfterPlayerTurnStart 里手动 CardPileCmd.Draw。好处：
    //   1. 跟 vanilla 抽牌数显示一致（HUD 上显示的"本回合抽牌数"已含 Amount）
    //   2. 跟其他 ModifyHandDraw power（vanilla MachineLearning / Demesne 等）协同正常累加
    //   3. 跟 vanilla mind_rot / scrutiny 等"少抽牌" debuff 抵消正常
    public override decimal ModifyHandDraw(MegaCrit.Sts2.Core.Entities.Players.Player player, decimal count)
    {
        if (Owner == null || player.Creature != Owner) return count;
        return count + Amount;
    }

    // AfterPlayerTurnStart 只负责更新我们自己的计数器（FightForBody / 争夺身体 用）+ Flash
    public override async Task AfterPlayerTurnStart(PlayerChoiceContext ctx, Player player)
    {
        if (player.Creature != Owner) return;
        Flash();
        CombatCounters.ExtraDrawsThisTurn[player] += Amount;
        await Task.CompletedTask;
    }

    public override List<(string, string)>? Localization => LocManager.Instance.Language switch
    {
        "zhs" => new PowerLoc("传话筒（睦）",
            "回合开始时，额外抽牌。",
            "回合开始时，额外抽{Amount}张牌。"),
        _ => new PowerLoc("Megaphone (Mu)",
            "At the start of your turn, draw extra cards.",
            "At the start of your turn, draw {Amount} extra cards."),
    };
}
