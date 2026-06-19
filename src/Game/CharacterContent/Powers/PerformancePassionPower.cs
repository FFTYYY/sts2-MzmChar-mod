using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace MzmChar.Game;

/// <summary>
/// 「演艺热情」buff：Amount 达到 5 时，当前回合结束后立刻开启额外回合。
/// 参考 vanilla <c>PaelsEye</c>：
///   1. <c>ShouldTakeExtraTurn</c> 返回 <c>Amount &gt;= 5</c>
///   2. <c>AfterTakingExtraTurn</c> 在新回合开始前 apply ConcertPower + 自移除
/// <c>AfterTakingExtraTurn</c> 不带 ctx 但 <c>PowerCmd.Apply</c> 需要 ctx，
/// 自己 new <c>HookPlayerChoiceContext</c>。
/// 必须 guard <c>Amount &gt;= Threshold</c>：<c>Hook.AfterTakingExtraTurn</c> 广播给所有 listeners
/// （PaelsEye 触发 extra turn 时也会调到我们）。
/// </summary>
public class PerformancePassionPower : CustomPowerModel
{
    private const int Threshold = 5;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override string? CustomPackedIconPath => "res://MzmChar/powers/performance_passion.png";
    public override string? CustomBigIconPath => "res://MzmChar/powers/performance_passion.png";

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get { yield return HoverTipFactory.FromPower<ConcertPower>(); }
    }

    public override bool ShouldTakeExtraTurn(Player player)
    {
        if (Owner?.Player != player) return false;
        return Amount >= Threshold;
    }

    public override async Task AfterTakingExtraTurn(Player player)
    {
        if (Owner?.Player != player) return;
        // 跟 ShouldTakeExtraTurn 条件一致（Hook 广播给所有 listeners）
        if (Amount < Threshold) return;
        Flash();

        PlayerChoiceContext? ctx = new HookPlayerChoiceContext(this, player.NetId, CombatState, GameActionType.Combat);
        await Sts2Compat.PowerApply<ConcertPower>(ctx, player.Creature, 1, player.Creature, null, false);
        await PowerCmd.Remove<PerformancePassionPower>(Owner);
    }

    public override List<(string, string)>? Localization => LocManager.Instance.Language switch
    {
        "zhs" => new PowerLoc("演艺热情",
            "积累的演奏热情。达到5点时，在本回合结束后开启一个额外的回合，并进入[gold]演奏会[/gold]。",
            "积累的演奏热情。达到5点时，在本回合结束后开启一个额外的回合，并进入[gold]演奏会[/gold]。"),
        _ => new PowerLoc("Performance Passion",
            "Accumulated performance passion. At 5, enter [gold]Concert[/gold].",
            "Accumulated performance passion. At 5, enter [gold]Concert[/gold]."),
    };
}
