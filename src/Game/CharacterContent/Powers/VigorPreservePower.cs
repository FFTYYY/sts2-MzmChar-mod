using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace MzmChar.Game;

/// <summary>
/// 「活力保留」buff：Amount 表示接下来还有多少次攻击造成伤害时，[gold]活力[/gold]不会被消耗。
/// 实现：Harmony prefix patch <see cref="VigorPower.AfterAttack"/> —— 当攻击者拥有本 buff 且
/// Amount &gt; 0，跳过原 AfterAttack（即跳过活力消耗），并将本 buff Amount-1。
///
/// **重要细节**（IL-verified）：vanilla `VigorPower.BeforeAttack` 检查 `Data.commandToModify` 非 null
/// 时**跳过设置**（line 13-19），AfterAttack 也**从不清空** `commandToModify`。vanilla 依赖
/// Amount 减到 0 → power 移除 → 下次 Apply 触发 `InitInternalData` 创建新 Data。
///
/// 我们跳过 AfterAttack 的 ModifyAmount，Amount 保持，power 不移除 → `Data.commandToModify` 永远
/// 指向第一次被 preserve 的攻击 command → 后续攻击 BeforeAttack 跳过、ModifyDamageAdditive
/// 命令 mismatch 不加活力、AfterAttack 命令 mismatch 也不消耗活力。
///
/// 所以 preserve 触发时必须**反射清掉** `VigorPower._internalData.commandToModify`，让下次
/// BeforeAttack 能正常 setup 新 command。
/// </summary>
public class VigorPreservePower : CustomPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override string? CustomPackedIconPath => "res://MzmChar/powers/vigor_preserve.png";
    public override string? CustomBigIconPath => "res://MzmChar/powers/vigor_preserve.png";

    public override List<(string, string)>? Localization => LocManager.Instance.Language switch
    {
        "zhs" => new PowerLoc("活力保留",
            "下一次攻击造成伤害时，[gold]活力[/gold]不会消失。",
            "接下来{Amount}次攻击造成伤害时，[gold]活力[/gold]不会消失。"),
        _ => new PowerLoc("Vigor Preserve",
            "For your next attacks, [gold]Vigor[/gold] is not consumed.",
            "For your next {Amount} attacks, [gold]Vigor[/gold] is not consumed."),
    };
}

[HarmonyPatch(typeof(VigorPower), "AfterAttack")]
public static class VigorPower_AfterAttack_PreservePatch
{
    // PowerModel._internalData 是 object。VigorPower 的 Data 是私有 nested 类 VigorPower+Data。
    private static readonly FieldInfo InternalDataField =
        AccessTools.Field(typeof(PowerModel), "_internalData");
    private static readonly FieldInfo CommandToModifyField =
        AccessTools.Field(typeof(VigorPower).GetNestedType("Data", BindingFlags.NonPublic)!, "commandToModify");

    [HarmonyPrefix]
    static bool Prefix(VigorPower __instance, AttackCommand command, ref Task __result)
    {
        var attacker = command?.Attacker;
        if (attacker == null) return true;
        if (attacker != __instance.Owner) return true;
        var preserve = attacker.GetPower<VigorPreservePower>();
        if (preserve == null || preserve.Amount <= 0) return true;

        // 跳过原 AfterAttack（活力不消耗），消耗一层保留，并清掉 VigorPower 内部 commandToModify
        __result = ConsumePreserveAsync(preserve, __instance);
        return false;
    }

    static async Task ConsumePreserveAsync(VigorPreservePower preserve, VigorPower vigor)
    {
        // 清掉 VigorPower._internalData.commandToModify —— 让下次 BeforeAttack 能 setup 新 command
        // 否则之后的攻击都拿不到活力加成、也不消耗活力（vigor stuck forever）
        var data = InternalDataField.GetValue(vigor);
        if (data != null) CommandToModifyField.SetValue(data, null);

        // 消耗一层 preserve buff
        // 必须用 async PowerCmd.Remove 而不是 sync SetAmount —— 否则 Amount=0 时 power 不会自动消失
        if (preserve.Amount <= 1)
            await PowerCmd.Remove<VigorPreservePower>(preserve.Owner);
        else
            preserve.SetAmount(preserve.Amount - 1, false);
    }
}
