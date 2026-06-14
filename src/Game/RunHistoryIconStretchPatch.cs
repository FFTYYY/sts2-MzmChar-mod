using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Screens.RunHistoryScreen;

namespace MzmChar.Game;

/// <summary>
/// Run history 的玩家头像 TextureRect 用 stretch=Scale，父 layout 给的格子比例 ~1:1.5，
/// 所以 1:1 的 button_small.png 在那里被竖向拉长。我们没法改 vanilla scene，
/// 但可以在 LoadRun 之后把那个 TextureRect 的 StretchMode 改成 KeepAspectCentered，
/// 让图按原比例居中 (头像不变形，最多上下留点透明)。
/// 只对我们的角色生效 (通过 ResourcePath 判断)，不影响 vanilla / 其它 mod。
/// </summary>
[HarmonyPatch(typeof(NRunHistoryPlayerIcon), "LoadRun")]
internal static class RunHistoryIconStretchPatch
{
    static void Postfix(NRunHistoryPlayerIcon __instance)
    {
        var icon = AccessTools.Field(typeof(NRunHistoryPlayerIcon), "_icon")
            .GetValue(__instance) as TextureRect;
        if (icon?.Texture?.ResourcePath is { } path && path.Contains("MzmChar/"))
        {
            icon.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
        }
    }
}
