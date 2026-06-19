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

namespace MzmChar.Game;

/// <summary>
/// 「二重易伤」debuff：若 owner 身上有[gold]易伤[/gold]，vuln 的乘数加性再加 0.5。
/// 加性而非乘性 → vuln 1.5 → 2.0；vuln+Debilitate 2.0 → 2.5。
/// </summary>
public class VulnerableDoublePower : CustomPowerModel
{
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override string? CustomPackedIconPath => "res://MzmChar/powers/vulnerable_double.png";
    public override string? CustomBigIconPath    => "res://MzmChar/powers/vulnerable_double.png";

    public override async Task AfterSideTurnEnd(PlayerChoiceContext ctx, CombatSide side, IEnumerable<Creature> participants)
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
        "zhs" => new PowerLoc("二重易伤",
            "若目标身上有[gold]易伤[/gold]，则目标受到的伤害额外增加50%。",
            "{Amount}回合内，若目标身上有[gold]易伤[/gold]，则目标受到的伤害额外增加50%。"),
        _ => new PowerLoc("Double Vulnerable",
            "If this target has [gold]Vulnerable[/gold], they take an additional 50% damage.",
            "For {Amount} turns, if this target has [gold]Vulnerable[/gold], they take an additional 50% damage."),
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
        // __result == 1m 表示 vuln 没生效 —— 我们也不该生效
        if (__result <= 1m) return;
        // 加性加 0.5：vuln 1.5 → 2.0 (+100%)；vuln+Debilitate 2.0 → 2.5 (+150%)
        __result += 0.5m;
    }
}
