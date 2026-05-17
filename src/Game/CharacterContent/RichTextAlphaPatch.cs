using HarmonyLib;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.RichTextTags;
using Godot;

namespace MzmChar.Game;

/// <summary>
/// vanilla 的 RichTextGold / RichTextBlue / ... 等色 tag effect 用 `charFx.Color = StsColors.<color>`
/// **绝对赋值**，把外层 `[color=#RRGGBBAA]` 设的 alpha 直接丢了——导致我们的 inactive 形态 wrap
/// 包不住 `[gold]X[/gold]`：外层半透明，内层 gold 又是 100% opaque。
///
/// 修法：Prefix 每个色 tag 的 _ProcessCustomFX，先取 vanilla 想用的色，再用 charFx.Color.A
/// 覆盖目标色的 alpha → 继承外层 alpha。Gold 还要保留 "diff() 绿色" 的特殊分支。
///
/// IL-probe verified（_scratch/probe）：8 个色 tag effect 全部用同款 absolute-set 模式。
/// </summary>
internal static class RichTextAlphaPatch
{
    /// <summary>把 target 色的 alpha 替换为 incoming.A，即"继承外层 alpha"。</summary>
    private static Color WithIncomingAlpha(Color target, Color incoming)
    {
        target.A = incoming.A;
        return target;
    }

    [HarmonyPatch(typeof(RichTextGold), "_ProcessCustomFX")]
    private static class GoldFix
    {
        static bool Prefix(CharFXTransform charFx, ref bool __result)
        {
            var c = charFx.Color;
            // 保留 :diff() 绿色（vanilla 原行为：检测 green → 不改色）
            if (c == StsColors.green) { __result = true; return false; }
            charFx.Color = WithIncomingAlpha(StsColors.gold, c);
            __result = true;
            return false;
        }
    }

    [HarmonyPatch(typeof(RichTextBlue), "_ProcessCustomFX")]
    private static class BlueFix
    {
        static bool Prefix(CharFXTransform charFx, ref bool __result)
        {
            charFx.Color = WithIncomingAlpha(StsColors.blue, charFx.Color);
            __result = true;
            return false;
        }
    }

    [HarmonyPatch(typeof(RichTextRed), "_ProcessCustomFX")]
    private static class RedFix
    {
        static bool Prefix(CharFXTransform charFx, ref bool __result)
        {
            charFx.Color = WithIncomingAlpha(StsColors.red, charFx.Color);
            __result = true;
            return false;
        }
    }

    [HarmonyPatch(typeof(RichTextGreen), "_ProcessCustomFX")]
    private static class GreenFix
    {
        static bool Prefix(CharFXTransform charFx, ref bool __result)
        {
            charFx.Color = WithIncomingAlpha(StsColors.green, charFx.Color);
            __result = true;
            return false;
        }
    }

    [HarmonyPatch(typeof(RichTextAqua), "_ProcessCustomFX")]
    private static class AquaFix
    {
        static bool Prefix(CharFXTransform charFx, ref bool __result)
        {
            charFx.Color = WithIncomingAlpha(StsColors.aqua, charFx.Color);
            __result = true;
            return false;
        }
    }

    [HarmonyPatch(typeof(RichTextOrange), "_ProcessCustomFX")]
    private static class OrangeFix
    {
        static bool Prefix(CharFXTransform charFx, ref bool __result)
        {
            charFx.Color = WithIncomingAlpha(StsColors.orange, charFx.Color);
            __result = true;
            return false;
        }
    }

    [HarmonyPatch(typeof(RichTextPink), "_ProcessCustomFX")]
    private static class PinkFix
    {
        static bool Prefix(CharFXTransform charFx, ref bool __result)
        {
            charFx.Color = WithIncomingAlpha(StsColors.pink, charFx.Color);
            __result = true;
            return false;
        }
    }

    [HarmonyPatch(typeof(RichTextPurple), "_ProcessCustomFX")]
    private static class PurpleFix
    {
        static bool Prefix(CharFXTransform charFx, ref bool __result)
        {
            charFx.Color = WithIncomingAlpha(StsColors.purple, charFx.Color);
            __result = true;
            return false;
        }
    }
}
