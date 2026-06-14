using System;
using HarmonyLib;
using Godot;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace MzmChar.Game;

/// <summary>
/// BaseLib 非 spine 路径动画完成后不自动回 idle（spine 走 CreatureAnimator 状态机自动转移，
/// 非 spine 走 PlayCustomAnimation 不接 animation_finished 信号 → 播完停在最后一帧）。
/// Postfix <c>NCreatureVisuals._Ready</c>：若 %Visuals 是 AnimatedSprite2D 且 SpriteFrames
/// 含 "idle"，则 connect AnimationFinished，任何非 idle / 非 die 动画播完自动 Play("idle")。
/// die 不切（死了应停在最后一帧）。
/// lambda 内 try/catch 兜底 ObjectDisposedException（多 player 同死时撞车）。
/// 不显式 disconnect：Godot 在 node Free 时自动清 signal handler。
/// </summary>
[HarmonyPatch(typeof(NCreatureVisuals), "_Ready")]
internal static class AutoReturnToIdlePatch
{
    private static readonly StringName IdleAnim = new("idle");
    private static readonly StringName DieAnim = new("die");

    [HarmonyPostfix]
    static void Postfix(NCreatureVisuals __instance)
    {
        var visualsNode = __instance.GetNodeOrNull("%Visuals");
        if (visualsNode is not AnimatedSprite2D anim) return;
        if (anim.SpriteFrames?.HasAnimation(IdleAnim) != true) return;

        anim.AnimationFinished += () =>
        {
            // GC 竞态防御：lambda 触发瞬间 anim 可能已被 vanilla 释放
            if (!GodotObject.IsInstanceValid(anim))
            {
                Diag.Trace("AutoReturnToIdle.handler: anim freed before access");
                return;
            }
            try
            {
                var current = anim.Animation;
                if (current == IdleAnim || current == DieAnim)
                {
                    Diag.Trace($"AutoReturnToIdle.handler: current={current}, no swap to idle");
                    return;
                }
                Diag.Trace($"AutoReturnToIdle.handler: current={current} → Play(idle)");
                anim.Play(IdleAnim);
            }
            catch (ObjectDisposedException ex)
            {
                Diag.Exception("AutoReturnToIdle.handler (anim freed mid-signal)", ex);
            }
        };
    }
}
