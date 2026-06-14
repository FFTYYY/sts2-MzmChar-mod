using System.Collections.Generic;
using Godot;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace MzmChar.Game;

/// <summary>
/// 演奏会聚光灯 VFX 运行时管理器（三段帧动画版）。
///
/// **视觉部分**：`pack/MzmChar/vfx/concert_spotlight.tscn` —— SpriteFrames 含三段：
///   - intro (30 帧, 0.7s, loop=false)：锥光从屏顶 ease-out cubic 延伸到角色头顶
///   - idle  (18 帧, 1.4s loop)：持续 dim 状态 + 两频叠加 flicker（烘焙到帧 intensity）
///   - outro (18 帧, 0.6s, loop=false)：alpha ease-in quad 淡出
///
/// 全部由 `tools/gen_concert_spotlight_frames.py` PIL/numpy 渲染。改参数 → 重跑 → dotnet build。
///
/// 本类的状态机：
///   Spawn       → cone.Play("intro") + 0.7s 后切 "idle"
///                 同时 shadow modulate.a 渐入 (Tween, Sine.InOut, 0.5s, 0.25s delay)
///                 0.7s 后 cone modulate.a Tween 1.0→0.7 (Quad.Out, 0.3s) 转持续 dim
///   Hold        → idle 循环，modulate.a 0.7
///   Despawn     → cone.Play("outro") + 0.6s 后 QueueFree
///                 同时 shadow modulate.a 渐出 (Tween, Quad.In, 0.5s)
///                 outro 帧自带 alpha 烘焙淡出，不需要 cone modulate tween
///
/// 联机：ConcertPower hooks per-client 跑 → SpawnFor/DespawnFor 各 client 渲染各自的 VFX。
/// </summary>
public static class ConcertSpotlightVfx
{
    private const string ScenePath = "res://MzmChar/vfx/concert_spotlight.tscn";

    // ============================== 调参区 ==============================
    // 必须跟 Python 里的 INTRO/OUTRO_DURATION_S 一致
    private const float IntroDuration = 0.7f;
    private const float OutroDuration = 0.6f;

    // cone modulate dim transition (intro 完成后过渡到 hold)
    private const float ConeDimDuration = 0.3f;
    private const float ConeAlphaHold   = 0.45f;

    // shadow
    private const float ShadowFadeInDuration = 0.5f;
    private const float ShadowFadeInDelay    = 0.25f;
    private const float ShadowFadeOutDuration = 0.5f;
    private const float ShadowAlphaHold = 0.7f;

    // 位置 offset —— 全部从 .tscn 里节点 Position 读，编辑器拖即可
    //   - Cone.Position   = "锥光 landing peak 相对角色 GlobalPosition 的偏移"
    //                       例：(0, -110) → landing peak 在角色锚点上方 110px（头顶）
    //   - Shadow.Position = "影子中心相对角色 GlobalPosition 的偏移"
    //                       例：(0, 40)   → 影子在角色锚点下方 40px
    // 改完保存 .tscn，dotnet build 重启游戏，不用动 C# 不用 Python。
    // 必须跟 Python 里的 LANDING_PEAK 一致 —— 决定锥光 texture 里"最亮处"在 v 哪个位置
    private const float ConeLandingV = 0.85f;
    // ====================================================================

    private class Instance
    {
        public Control Root = null!;
        public AnimatedSprite2D Cone = null!;
        public Sprite2D Shadow = null!;
    }

    private static readonly Dictionary<Creature, Instance> _active = new();
    private static PackedScene? _cachedScene;

    private static PackedScene? LoadScene()
    {
        if (_cachedScene != null && GodotObject.IsInstanceValid(_cachedScene))
            return _cachedScene;
        try { _cachedScene = GD.Load<PackedScene>(ScenePath); }
        catch { _cachedScene = null; }
        return _cachedScene;
    }

    /// <summary>
    /// 预加载 PackedScene 及其引用的所有 PNG（66 帧 + shadow）。
    /// 由 ModEntry.OnModLoaded 在游戏启动时调一次 —— 避免第一次进演奏会触发 SpawnFor 时
    /// 同步读 66 个 PNG + 解码引起的明显卡顿。
    /// </summary>
    public static void Preload()
    {
        LoadScene();
    }

    public static void SpawnFor(Creature creature)
    {
        if (creature == null) return;

        // 清旧
        if (_active.TryGetValue(creature, out var oldInst))
        {
            _active.Remove(creature);
            if (GodotObject.IsInstanceValid(oldInst.Root))
                oldInst.Root.QueueFree();
        }

        var scene = LoadScene();
        if (scene == null) return;

        var room = NCombatRoom.Instance;
        if (room == null) return;

        var nc = room.GetCreatureNode(creature);
        if (nc == null || !GodotObject.IsInstanceValid(nc)) return;

        var container = room.CombatVfxContainer;
        if (container == null || !GodotObject.IsInstanceValid(container)) return;

        var root = scene.Instantiate<Control>();
        if (root == null) return;

        var cone = root.GetNodeOrNull<AnimatedSprite2D>("Cone");
        var shadow = root.GetNodeOrNull<Sprite2D>("Shadow");
        if (cone == null || shadow == null)
        {
            root.QueueFree();
            return;
        }

        container.AddChild(root);

        // ===== 定位（贴图尺寸 auto-derive）=====
        int coneTexHeight = 1600;  // fallback
        var sf = cone.SpriteFrames;
        if (sf != null && sf.HasAnimation("intro") && sf.GetFrameCount("intro") > 0)
        {
            var tex = sf.GetFrameTexture("intro", 0);
            if (tex != null) coneTexHeight = tex.GetHeight();
        }
        // 锥光：用 .tscn 里 Cone 节点设的 Position 作为 "landing peak 相对角色锚点的偏移"
        // 例：Cone.Position=(0,-110) → landing peak 落在角色锚点上方 110px（头顶）
        var coneOffset = cone.Position;
        cone.Position = new Vector2(
            nc.GlobalPosition.X + coneOffset.X,
            // 让 v=ConeLandingV 落在 (nc.Y + coneOffset.Y)
            // centered=true → cone.Position.Y = (那个 Y) - (ConeLandingV - 0.5) * texHeight
            nc.GlobalPosition.Y + coneOffset.Y - (ConeLandingV - 0.5f) * coneTexHeight
        );

        // 影子：用 .tscn 里 Shadow 节点设的 Position 作为相对偏移，加到角色 GlobalPosition 上
        // → 编辑器里改 Shadow 的 Position 直接生效，不用动 C#
        var shadowOffset = shadow.Position;
        shadow.Position = nc.GlobalPosition + shadowOffset;

        // ===== 开始 intro =====
        cone.Play("intro");

        // ===== Tween 编排 =====
        var tween = root.CreateTween().SetParallel(true);

        // intro 完成后切到 idle —— 独立 Tween 串行：interval(IntroDuration) → callback
        var switchTween = root.CreateTween();
        switchTween.TweenInterval(IntroDuration);
        switchTween.TweenCallback(Callable.From(() =>
        {
            if (GodotObject.IsInstanceValid(cone))
                cone.Play("idle");
        }));

        // intro 末段 cone modulate.a 1.0 → 0.7（dim transition）
        var coneDim = tween.TweenProperty(cone, "modulate:a", ConeAlphaHold, ConeDimDuration);
        coneDim.SetDelay(IntroDuration)
               .SetTrans(Tween.TransitionType.Quad)
               .SetEase(Tween.EaseType.Out);

        // shadow 渐入
        var shadowFade = tween.TweenProperty(shadow, "modulate:a", ShadowAlphaHold, ShadowFadeInDuration);
        shadowFade.SetDelay(ShadowFadeInDelay)
                  .SetTrans(Tween.TransitionType.Sine)
                  .SetEase(Tween.EaseType.InOut);

        _active[creature] = new Instance { Root = root, Cone = cone, Shadow = shadow };
    }

    public static void DespawnFor(Creature creature)
    {
        if (creature == null) return;
        if (!_active.TryGetValue(creature, out var inst)) return;
        _active.Remove(creature);

        if (!GodotObject.IsInstanceValid(inst.Root)) return;

        // ===== 切 outro 帧动画 =====
        if (GodotObject.IsInstanceValid(inst.Cone))
            inst.Cone.Play("outro");

        // ===== Tween：shadow 淡出 + outro 完成后 free root =====
        var tween = inst.Root.CreateTween().SetParallel(true);

        var fadeShadow = tween.TweenProperty(inst.Shadow, "modulate:a", 0f, ShadowFadeOutDuration);
        fadeShadow.SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.In);

        // outro 帧自带 intensity 1.0→0 烘焙，cone modulate 保持 0.7 不动 → 显示 0.7→0

        // 等 outro 跑完后 free（不能跟 fadeShadow 抢 parallel，所以独立 tween）
        tween.SetParallel(false);
        var captured = inst.Root;
        tween.TweenInterval(OutroDuration);
        tween.TweenCallback(Callable.From(() =>
        {
            if (GodotObject.IsInstanceValid(captured))
                captured.QueueFree();
        }));
    }
}
