using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Vfx;

namespace MzmChar.Game;

/// <summary>
/// Vanilla scene bug 兜底：<c>NHorizontalLinesVfx._Ready</c> 第一行 <c>GetParent&lt;Control&gt;()</c>
/// 在 parent 不是 <c>Control</c> 时直接 InvalidCast；CSharpInstanceBridge 接住后走 <c>PushError</c>
/// 打 log（不致命，但 PlaySequence 等串行流程会被中断）。
/// vanilla 自家 <c>NGrandFinaleVfx</c> 的 .tscn 把这个节点挂在 <c>NParticlesContainer</c>（Node2D）
/// 下，每次播都踩。本 Prefix 在 parent 不是 Control 时跳过整个 _Ready，让节点保持 inert：
///   - parent == Control（Whirlwind 等正常用法）：行为不变
///   - parent != Control（GrandFinale 等 vanilla 误用）：跳过初始化，不抛、不打 log
/// </summary>
[HarmonyPatch(typeof(NHorizontalLinesVfx), "_Ready")]
internal static class NHorizontalLinesVfxReadyPatch
{
    [HarmonyPrefix]
    private static bool Prefix(NHorizontalLinesVfx __instance)
    {
        return __instance.GetParent() is Control;
    }
}
