using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;

namespace MzmChar.Game;

/// <summary>
/// 演奏关键字的 tooltip 默认在 hover tips 末尾（vanilla CardModel.get_HoverTips IL-verified：keyword tips 在 ExtraHoverTips 后追加）。
/// 我们希望它放在 hover 列表首位（与卡描述顶部位置一致）。Postfix 找到 Perform 的 tip 并移到首位。
/// </summary>
[HarmonyPatch(typeof(CardModel), "HoverTips", MethodType.Getter)]
internal static class PerformTipOrderPatch
{
    [HarmonyPostfix]
    static void Postfix(CardModel __instance, ref IEnumerable<IHoverTip> __result)
    {
        if (__instance?.Keywords == null) return;
        if (!__instance.Keywords.Contains(MzmCharKeywords.Perform)) return;

        // 通过比对 Title 找到 Perform 关键字的 tip。每次重新算 Title，因为 locale 可能切换。
        // HoverTip 是 struct → 用 is pattern；Title 类型是 string? (nullable annotated)，用 var 推断避免 CS8600
        if (HoverTipFactory.FromKeyword(MzmCharKeywords.Perform) is not HoverTip performTip) return;
        var targetTitle = performTip.Title;

        var list = __result.ToList();
        int idx = list.FindIndex(t => t is HoverTip ht && ht.Title == targetTitle);
        if (idx > 0)
        {
            var tip = list[idx];
            list.RemoveAt(idx);
            list.Insert(0, tip);
            __result = list;
        }
    }
}
