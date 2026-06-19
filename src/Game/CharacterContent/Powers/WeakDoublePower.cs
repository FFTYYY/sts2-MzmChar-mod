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
/// 「二重虚弱」debuff：若 owner 身上有[gold]虚弱[/gold]，weak 的乘数加性再减 0.25。
/// 加性而非乘性 → Debilitate 把 weak 改成 0.5 后再 −0.25 = 0.25，不会破到 0。
/// </summary>
public class WeakDoublePower : CustomPowerModel
{
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override string? CustomPackedIconPath => "res://MzmChar/powers/weak_double.png";
    public override string? CustomBigIconPath    => "res://MzmChar/powers/weak_double.png";

    public override async Task AfterSideTurnEnd(PlayerChoiceContext ctx, CombatSide side, IEnumerable<Creature> participants)
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
        "zhs" => new PowerLoc("二重虚弱",
            "若目标身上有[gold]虚弱[/gold]，则目标造成的伤害额外减少25%。",
            "{Amount}回合内，若目标身上有[gold]虚弱[/gold]，则目标造成的伤害额外减少25%。"),
        _ => new PowerLoc("Double Weak",
            "If this target has [gold]Weak[/gold], they deal an additional 25% less damage.",
            "For {Amount} turns, if this target has [gold]Weak[/gold], they deal an additional 25% less damage."),
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
        // __result == 1m 表示 weak 没生效（不是攻击 / 非本 dealer）—— 我们也不该生效
        if (__result >= 1m) return;
        // 加性减 0.25：weak 0.75 → 0.5 (50% 减伤)；weak+Debilitate 0.5 → 0.25 (75% 减伤)
        __result -= 0.25m;
        if (__result < 0m) __result = 0m;
    }
}
