using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Screens.RunHistoryScreen;

namespace MzmChar.Game;

/// <summary>
/// Run history 头像 (<c>NRunHistoryPlayerIcon._icon TextureRect</c>) 父 layout 比例 ~1:1.5，
/// 1:1 <c>button_small.png</c> 在那里会被竖向拉长。Postfix 改成 KeepAspectCentered，
/// 让图按原比例居中。Prepare 检查字段存在性兜底，万一 vanilla 改字段名整个 patch 优雅禁用。
/// 只影响我们 mod 的头像（通过 ResourcePath 判断）。
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
