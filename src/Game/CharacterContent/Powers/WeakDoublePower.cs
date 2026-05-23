using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace MzmChar.Game;

/// <summary>
/// 「虚弱翻倍」debuff：当 owner 攻击时，自身[gold]虚弱[/gold]带来的减伤翻倍。
/// </summary>
public class WeakDoublePower : CustomPowerModel
{
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override string? CustomPackedIconPath => "res://MzmChar/powers/weak_double.png";
    public override string? CustomBigIconPath    => "res://MzmChar/powers/weak_double.png";

    // 0.106: AfterTurnEnd(ctx, side) → AfterSideTurnEnd(ctx, side, participants)
#if BETA
    public override async Task AfterSideTurnEnd(PlayerChoiceContext ctx, CombatSide side, IEnumerable<Creature> participants)
#else
    public override async Task AfterTurnEnd(PlayerChoiceContext ctx, CombatSide side)
#endif
    {
        if (side != Owner.Side) return;
        Flash();
        if (Amount <= 1)
            await PowerCmd.Remove<WeakDoublePower>(Owner);
        else
            SetAmount(Amount - 1, false);
    }

    public override List<(string, string)>? Localization => LocManager.Instance.Language switch
    {
        "zhs" => new PowerLoc("虚弱翻倍",
            "目标身上的[gold]虚弱[/gold]效果翻倍。",
            "{Amount}回合内，目标身上的[gold]虚弱[/gold]效果翻倍。"),
        _ => new PowerLoc("Weak x2",
            "[gold]Weak[/gold] effect on this target is doubled.",
            "For {Amount} turns, [gold]Weak[/gold] effect on this target is doubled."),
    };
}

[HarmonyPatch(typeof(WeakPower), nameof(WeakPower.ModifyDamageMultiplicative))]
public static class WeakPower_DoublePatch
{
    [HarmonyPostfix]
    static void Postfix(Creature dealer, ref decimal __result)
    {
        if (dealer == null) return;
        if (!dealer.HasPower<WeakDoublePower>()) return;
        // ModifyDamageMultiplicative 返回的是**乘数**（weak 默认 0.75 = -25%）。
        // "翻倍" = penalty 加倍：penalty = 1 - result，新 penalty = 2 * penalty → new_result = 1 - 2 * (1 - result) = 2 * result - 1
        // weak 0.75 → 0.5 (-50% 伤害)
        __result = 2m * __result - 1m;
    }
}
