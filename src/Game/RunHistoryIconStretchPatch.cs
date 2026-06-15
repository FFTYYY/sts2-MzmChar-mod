using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Screens.RunHistoryScreen;

namespace MzmChar.Game;

/// <summary>
/// beta vanilla: Run history 头像走 A 路径 (NRunHistoryPlayerIcon._icon TextureRect, stretch=Scale)，
/// 父 layout 比例 ~1:1.5，1:1 button_small.png 在那里会被竖向拉长。这里 postfix 改成
/// KeepAspectCentered，让图按原比例居中（头像不变形，最多上下留点透明）。
/// stable vanilla: 走 B 路径 (AddChild character.Icon scene)，没有 _icon 字段，
/// Prepare 返回 false 直接禁用整个 patch；Postfix 再 try/catch 兜底。
/// 只影响我们 mod 的头像 (通过 ResourcePath 判断)。
/// </summary>
[HarmonyPatch(typeof(NRunHistoryPlayerIcon), "LoadRun")]
internal static class RunHistoryIconStretchPatch
{
    static bool Prepare()
    {
        return AccessTools.Field(typeof(NRunHistoryPlayerIcon), "_icon") != null;
    }

    static void Postfix(NRunHistoryPlayerIcon __instance)
    {
        try
        {
            var f = AccessTools.Field(typeof(NRunHistoryPlayerIcon), "_icon");
            if (f?.GetValue(__instance) is not TextureRect icon) return;
            if (icon.Texture?.ResourcePath is { } path && path.Contains("MzmChar/"))
            {
                icon.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
            }
        }
        catch { /* vanilla 改字段名时静默跳过 */ }
    }
}
