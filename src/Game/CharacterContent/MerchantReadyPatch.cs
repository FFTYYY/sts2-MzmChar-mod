using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;

namespace MzmChar.Game;

/// <summary>
/// vanilla <c>NMerchantCharacter._Ready</c> 无条件用第一个子节点构造 <c>MegaSprite</c>，
/// 我们 merchant.tscn 的 Visuals 不是 SpineSprite → ctor 抛 <c>MegaSpineBinding.ValidateBoundObject</c>。
/// prefix 检测：非 SpineSprite 子节点 → return false 跳过 vanilla _Ready（AnimatedSprite2D 用
/// <c>autoplay</c> 自播 idle_loop，不需要 vanilla 的 spine anim）。
/// </summary>
[HarmonyPatch]
internal static class MerchantReadyPatch
{
    [HarmonyPatch(typeof(NMerchantCharacter), "_Ready")]
    [HarmonyPrefix]
    private static bool _Ready_Prefix(NMerchantCharacter __instance)
    {
        if (__instance.GetChildCount() == 0) return true;
        var child = __instance.GetChild(0);
        if (child == null) return true;
        // SpineSprite 类型在独立 Spine runtime DLL，用 type name 字符串比对（最低耦合）
        return child.GetType().Name == "SpineSprite";
    }
}
