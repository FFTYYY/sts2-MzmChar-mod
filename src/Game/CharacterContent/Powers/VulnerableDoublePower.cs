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
/// 「易伤翻倍」debuff：在目标身上时，[gold]易伤[/gold]带来的伤害加成翻倍。
/// Amount = 剩余回合数。owner.Side 的回合结束时 -1，归零则自动移除。
/// </summary>
public class VulnerableDoublePower : CustomPowerModel
{
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override string? CustomPackedIconPath => "res://MzmChar/powers/vulnerable_double.png";
    public override string? CustomBigIconPath    => "res://MzmChar/powers/vulnerable_double.png";

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
            await PowerCmd.Remove<VulnerableDoublePower>(Owner);
        else
            SetAmount(Amount - 1, false);
    }

    public override List<(string, string)>? Localization => LocManager.Instance.Language switch
    {
        "zhs" => new PowerLoc("易伤翻倍",
            "目标身上的[gold]易伤[/gold]效果翻倍。",
            "{Amount}回合内，目标身上的[gold]易伤[/gold]效果翻倍。"),
        _ => new PowerLoc("Vulnerable x2",
            "[gold]Vulnerable[/gold] effect on this target is doubled.",
            "For {Amount} turns, [gold]Vulnerable[/gold] effect on this target is doubled."),
    };
}

[HarmonyPatch(typeof(VulnerablePower), nameof(VulnerablePower.ModifyDamageMultiplicative))]
public static class VulnerablePower_DoublePatch
{
    [HarmonyPostfix]
    static void Postfix(Creature target, ref decimal __result)
    {
        if (target == null) return;
        if (!target.HasPower<VulnerableDoublePower>()) return;
        // ModifyDamageMultiplicative 返回的是**乘数**（vuln 默认 1.5 = +50%）。
        // "翻倍" = bonus 加倍：bonus = result - 1，新 bonus = 2 * bonus → new_result = 1 + 2 * (result - 1) = 2 * result - 1
        // vuln 1.5 → 2.0 (+100% 伤害)
        __result = 2m * __result - 1m;
    }
}
