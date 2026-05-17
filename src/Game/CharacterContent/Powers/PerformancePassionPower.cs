using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
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
///   1. override `ShouldTakeExtraTurn(player)` 返回 `player == Owner.Player && Amount &gt;= 5`
///      —— `Hook.ShouldTakeExtraTurn` 遍历所有 hook listener，任一返回 true 即触发
///   2. override `AfterTakingExtraTurn(player)` 做清理：
///      - 移除自己（避免下回合再触发，构成无限回合）
///      - 给玩家应用 ConcertPower（标记额外回合处于"演奏会"状态）
///
/// 阈值固定为 5。如果应用超过 5（如两张回忆中的乐队 = 10 passion），同样触发一次（不是两次）。
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
        Flash();
        // 移除自己，避免下回合 ShouldTakeExtraTurn 再返回 true 形成连环
        await PowerCmd.Remove<PerformancePassionPower>(Owner);
        // 应用「演奏会」状态到这个新开的（额外）回合
        await PowerCmd.Apply<ConcertPower>(player.Creature, 1, player.Creature, null, false);
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
