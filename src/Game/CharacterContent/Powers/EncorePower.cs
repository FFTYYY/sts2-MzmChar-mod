using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace MzmChar.Game;

/// <summary>
/// 「安可」buff（Encore 卡应用）：每当一个[gold]演奏会[/gold]回合结束时，获得 Amount 点[gold]演艺热情[/gold]。
///
/// 实现关键（沿用 CelebrationBanquetRelic 同款 flag 模式）：
///   probe out_concert_remove.txt 确认：`PowerCmd.Remove&lt;T&gt;` 走 `RemoveInternal + AfterRemoved`，
///   **不**触发全局 `Hook.AfterPowerAmountChanged`。`AfterRemoved` 是 power 自身的 instance hook，
///   外部 power 无法跨实例直接监听 ConcertPower 正→0。
///
/// 所以监听 ConcertPower 0→正（PowerCmd.Apply 走 hook，amount 是 delta）置 [SavedProperty] flag。
/// `AfterSideTurnEnd / AfterTurnEnd` 检查 flag → +Amount PP + 重置 flag。
///
/// 连续 concert：每次 ConcertPower 被 apply 都 0→1（上一个已 remove），flag 重置成 true → 每个
/// concert turn end 都触发 +Amount PP。Counter 叠层（多张 Encore = Amount 累加）。
/// </summary>
public class EncorePower : CustomPowerModel
{
    [SavedProperty]
    private bool ConcertActiveThisTurn { get; set; }

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override string? CustomPackedIconPath => "res://MzmChar/powers/encore.png";
    public override string? CustomBigIconPath    => "res://MzmChar/powers/encore.png";

    // Counter 层数上限 —— 用户要求：每次演奏会结束最多获得 2 点演艺热情
    private const int MaxStack = 2;

    /// <summary>
    /// 首次创建时 clamp。注意：Counter stack 模式下，对已存在 power 再 Apply 只 bump Amount，
    /// **不调** AfterApplied —— 所以多张 Encore 叠加的 clamp 由 AfterPowerAmountChanged (self) 负责。
    /// </summary>
    public override Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        if (Amount > MaxStack)
            SetAmount(MaxStack, false);
        return Task.CompletedTask;
    }

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromPower<ConcertPower>();
            yield return HoverTipFactory.FromPower<PerformancePassionPower>();
        }
    }

    // stable: AfterPowerAmountChanged(PowerModel, decimal, Creature, CardModel)
    // beta:   AfterPowerAmountChanged(PlayerChoiceContext, PowerModel, decimal, Creature?, CardModel?)
    // body 不使用 ctx，两签名共用
#if BETA
    public override Task AfterPowerAmountChanged(
        PlayerChoiceContext ctx, PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
#else
    public override Task AfterPowerAmountChanged(
        PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
#endif
    {
        if (Owner == null) return Task.CompletedTask;
        if (power.Owner != Owner) return Task.CompletedTask;

        // 1) 监听 ConcertPower 0→正
        if (power is ConcertPower)
        {
            if (amount <= 0) return Task.CompletedTask;
            if (power.Amount != amount) return Task.CompletedTask;  // 等价 oldAmount == 0
            ConcertActiveThisTurn = true;
            return Task.CompletedTask;
        }

        // 2) 监听自己 Amount 变化（Counter stack 多次 apply 触发这个，不是 AfterApplied）→ clamp
        //    silent=true 避免 Flash + 二次 hook 触发递归
        if (power == this && Amount > MaxStack)
        {
            SetAmount(MaxStack, true);
        }

        return Task.CompletedTask;
    }

#if BETA
    public override async Task AfterSideTurnEnd(PlayerChoiceContext ctx, CombatSide side, IEnumerable<Creature> participants)
#else
    public override async Task AfterTurnEnd(PlayerChoiceContext ctx, CombatSide side)
#endif
    {
        if (Owner == null) return;
        if (side != Owner.Side) return;
        if (!ConcertActiveThisTurn) return;
        ConcertActiveThisTurn = false;
        if (Amount <= 0) return;

        Flash();
        await Sts2Compat.PowerApply<PerformancePassionPower>(ctx, Owner, (int)Amount, Owner, null, false);
    }

    // 战斗开始重置 flag（残留的 saved state 不应跨战斗）
    public override Task BeforeCombatStart()
    {
        ConcertActiveThisTurn = false;
        return Task.CompletedTask;
    }

    public override List<(string, string)>? Localization => LocManager.Instance.Language switch
    {
        "zhs" => new PowerLoc("安可",
            "[gold]演奏会[/gold]回合结束时，获得[gold]演艺热情[/gold]。",
            "[gold]演奏会[/gold]回合结束时，获得{Amount}点[gold]演艺热情[/gold]。"),
        _ => new PowerLoc("Encore",
            "At the end of a [gold]Concert[/gold] turn, gain [gold]Performance Passion[/gold].",
            "At the end of a [gold]Concert[/gold] turn, gain {Amount} [gold]Performance Passion[/gold]."),
    };
}
