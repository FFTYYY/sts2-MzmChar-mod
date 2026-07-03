using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;

namespace MzmChar.Game;

/// <summary>
/// 「伤害减半」buff：owner 受到的伤害减半。Amount = 剩余回合数。
/// 效果通过 <see cref="HalfDamageMultiplicativePatch"/>（AbstractModel.ModifyDamageMultiplicative 的
/// Harmony prefix）实现 —— 用 Harmony 而非 override 是为了跨 v0.107/v0.108 单 dll 兼容：v0.107 base 是 5 参、
/// v0.108 是 6 参，override 必须编译期绑一个签名，Harmony 按参数名匹配可以跨版本工作。
/// </summary>
public class HalfDamagePower : CustomPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override string? CustomPackedIconPath => "res://MzmChar/powers/half_damage.png";
    public override string? CustomBigIconPath    => "res://MzmChar/powers/half_damage.png";

    // 在玩家**下回合开始**时减层（不是回合结束）—— 否则 1 层情况下：
    // 玩家打出下跪 → 玩家回合结束 → 减层到 0 → buff 消失 → 敌人回合还没来就没保护了
    // 改成 AfterPlayerTurnStartEarly：本回合结束 → 敌人回合（伤害减半生效）→ 玩家下回合开始 → 减层
    public override async Task AfterPlayerTurnStartEarly(PlayerChoiceContext ctx, Player player)
    {
        if (player.Creature != Owner) return;
        Flash();
        if (Amount <= 1)
            await PowerCmd.Remove<HalfDamagePower>(Owner);
        else
            SetAmount(Amount - 1, false);
    }

    public override List<(string, string)>? Localization => LocManager.Instance.Language switch
    {
        "zhs" => new PowerLoc("伤害减半",
            "受到的伤害减半。",
            "{Amount}回合内，受到的伤害减半。"),
        _ => new PowerLoc("Damage Halved",
            "Incoming damage is halved.",
            "For {Amount} turns, incoming damage is halved."),
    };
}

/// <summary>
/// AbstractModel.ModifyDamageMultiplicative prefix：仅当 __instance 是 HalfDamagePower + target 就是它 owner
/// 时短路返回 0.5m；其它情况正常跑 vanilla base（identity 1m）。
///
/// 用 Harmony 而不是 override：override 需要编译期匹配一个具体签名，跨 v0.107（5 参）/ v0.108（6 参）
/// 就没法用同一份 dll。Harmony 按参数名匹配，<c>target</c> 和 <c>__instance</c> 两个 vanilla 版本都有。
/// </summary>
[HarmonyPatch(typeof(AbstractModel), "ModifyDamageMultiplicative")]
internal static class HalfDamageMultiplicativePatch
{
    [HarmonyPrefix]
    private static bool Prefix(AbstractModel __instance, Creature? target, ref decimal __result)
    {
        if (__instance is HalfDamagePower hdp && target == hdp.Owner)
        {
            __result = 0.5m;
            return false;  // 跳过 base
        }
        return true;  // 走 vanilla base（返 1m）
    }
}
