using HarmonyLib;
using Godot;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace MzmChar.Game;

/// <summary>
/// BaseLib 非 spine 路径的动画系统缺口修复：vanilla 的 &lt;TriggerAnim&gt;d__28 状态机调
/// SetAnimationTrigger("Attack"/"Cast"/"Hit") + CustomScaledWait 后直接 return，**没有自动
/// 调 SetAnimationTrigger("Idle") 切回去**。spine 路径靠 CreatureAnimator.AnimState 状态机
/// 自动转移；非 spine 路径走 BaseLib SendTriggerToOtherAnimators Prefix 只调
/// PlayCustomAnimation(["attack", "Attack"]) → AnimatedSprite2D.Play("attack")，没接
/// animation_finished 信号 → 播完停在最后一帧。
///
/// 修法：Postfix NCreatureVisuals._Ready，若 %Visuals 是 AnimatedSprite2D 且 SpriteFrames
/// 含 "idle" 动画，则 connect AnimationFinished 信号，任何非 idle / 非 die 动画播完自动
/// Play("idle")。die 不切（死了应停在最后一帧）。
///
/// IL-verified（_scratch/probe）：BaseLib 非 spine 路径在 anim 触发后无回 idle 逻辑；
/// CustomCharacterModel.SetupCustomAnimationStates 默认 `ldnull / ret`（spine 专用）。
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
            if (!GodotObject.IsInstanceValid(anim)) return;
            var current = anim.Animation;
            if (current == IdleAnim || current == DieAnim) return;
            anim.Play(IdleAnim);
        };
    }
}
