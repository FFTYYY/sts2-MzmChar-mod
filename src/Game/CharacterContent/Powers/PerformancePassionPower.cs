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
///
/// 实现参考 vanilla `PaelsEye` relic（IL-verified）：
///   1. override `ShouldTakeExtraTurn(player)` 返回 `Amount &gt;= 5`
///   2. override `AfterTakingExtraTurn(player)` 在新回合开始前 apply ConcertPower + 自移除
///
/// beta 兼容：AfterTakingExtraTurn 不带 ctx，但 beta 的 PowerCmd.Apply 必须有 ctx。
/// 我们自己构造一个 HookPlayerChoiceContext（vanilla 的 Hook.* 内部也是这么造的）。
/// 必须在 AfterTakingExtraTurn 里 apply（不是 AfterPlayerTurnStartEarly），
/// 否则错过本回合的 ModifyHandDraw / AfterPlayerTurnStartEarly 这两个 hook iteration。
///
/// ⚠️ AfterTakingExtraTurn 必须 guard `Amount >= Threshold`：vanilla
/// `Hook.AfterTakingExtraTurn` 对**所有** listeners 广播，不只触发源那个。
/// 如果玩家同时有 PaelsEye（PaelsEye 在玩家放置回合触发 extra turn），我们这个 hook
/// 会被错误调用——若不 guard 就会在 PP 未到 5 时也 apply ConcertPower + 清零 PP。
/// 见 report_44。
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
        // 必须跟 ShouldTakeExtraTurn 的条件一致 —— 否则其它 listener（如 PaelsEye）触发 extra turn
        // 时我们也会被广播调到，错误 apply ConcertPower + 清零 PP。见 report_44。
        if (Amount < Threshold) return;
        Flash();

        PlayerChoiceContext? ctx = null;
#if BETA
        ctx = new HookPlayerChoiceContext(this, player.NetId, CombatState, GameActionType.Combat);
#endif
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
