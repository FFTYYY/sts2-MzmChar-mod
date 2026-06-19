using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace MzmChar.Game;

/// <summary>
/// 「监视」buff（监控卡的 Mu 面授予）：本回合每当 Owner 获得 1 点格挡，同时获得 Amount 点活力。
/// 回合结束自移除。**Counter 叠层** —— 多次施加 Amount 累加，每次格挡触发对应倍数 Vigor。
///
/// Hook 选 `AfterBlockGained(creature, amount, props, cardSource)`（PowerModel 基类 virtual，
/// IL-verified vanilla `JuggernautPower` / `BeaconOfHopePower` 都 override 这个）。
/// `amount` 是本次 gain 的**增量**（vanilla BeaconOfHopePower IL: `amount * 0.5m` 给队友 → 证实 delta）。
/// 不 filter ValueProp（vanilla JuggernautPower 也不 filter）。
///
/// ctx：AfterBlockGained 不带 ctx，沿用 `PerformancePassionPower` 同款做法自己 new
/// `HookPlayerChoiceContext`。
/// </summary>
public class SurveillanceBuffPower : CustomPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override string? CustomPackedIconPath => "res://MzmChar/powers/surveillance_buff.png";
    public override string? CustomBigIconPath    => "res://MzmChar/powers/surveillance_buff.png";

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get { yield return HoverTipFactory.FromPower<VigorPower>(); }
    }

    public override async Task AfterBlockGained(Creature creature, decimal amount, ValueProp props, CardModel? cardSource)
    {
        if (Owner == null) return;
        if (creature != Owner) return;
        if (amount <= 0) return;

        Flash();
        PlayerChoiceContext? ctx = null;
        if (Owner.Player != null)
            ctx = new HookPlayerChoiceContext(this, Owner.Player.NetId, CombatState, GameActionType.Combat);
        // 每点格挡触发 Amount 点 vigor → 总 vigor = blockGained × stack
        await Sts2Compat.PowerApply<VigorPower>(ctx, Owner, (int)amount * (int)Amount, Owner, null, false);
    }

    // 回合结束自移除（参考 TempStrengthPower）
    public override async Task AfterSideTurnEnd(PlayerChoiceContext ctx, CombatSide side, IEnumerable<Creature> participants)
    {
        if (Owner == null) return;
        if (side != Owner.Side) return;
        await PowerCmd.Remove<SurveillanceBuffPower>(Owner);
    }

    // arg 2 = DumbHoverTip / 关键字 hover（不能写 {Amount}，vanilla 不注入）
    // arg 3 = power 实活 hover（带 Amount 当前层数）
    public override List<(string, string)>? Localization => LocManager.Instance.Language switch
    {
        "zhs" => new PowerLoc("盯",
            "本回合内，你每获得1点[gold]格挡[/gold]，同时获得[gold]活力[/gold]。",
            "本回合内，你每获得1点[gold]格挡[/gold]，同时获得{Amount}点[gold]活力[/gold]。"),
        _ => new PowerLoc("Watching",
            "This turn, for each 1 [gold]Block[/gold] you gain, also gain [gold]Vigor[/gold].",
            "This turn, for each 1 [gold]Block[/gold] you gain, also gain {Amount} [gold]Vigor[/gold]."),
    };
}
