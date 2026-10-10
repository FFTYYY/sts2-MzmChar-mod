using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Audio;
using MegaCrit.Sts2.Core.Rooms;
using MzmChar.Config;

namespace MzmChar.Game;

/// <summary>
/// 战斗 BGM 替换 + 智能调度。
///
/// ## 调度规则（2026-10-10 重写）
/// 原则一：**我们的曲子永不硬切断**，总是放完整首再决定去向。
/// 原则二：播我们的曲子时，战斗内正常音量、战斗外压低音量；交还给 vanilla 后音量不归我们管。
///
///   战斗开始（且该战斗启用了替换）→ 若没在播：停掉 vanilla 音乐并随机起一首（正常音量）；
///                                    若已在播：**不换曲**，只把音量抬回正常
///   战斗结束                      → 音量淡到压低档，**继续播**
///   一首播完（Finished）          → 还在**同一场**战斗中：**循环这一首**（一场战斗自始至终一首曲子）
///                                    已经换了一场战斗：随机 roll 新的一首
///                                    不在战斗中：停掉我们的，调 vanilla 的 UpdateMusic() 交还
///
/// 「同一场战斗」用 <c>CombatManager.CurrentCombatId</c> 判定：曲子起播时记下当时的战斗 id，
/// 播完时比对。跨战斗的情形来自上面那条「已在播就不换曲」—— 战斗 A 的曲子被带进了战斗 B，
/// 等它放完才轮到 B 自己的随机曲。
///
/// 「战斗内」= **启用了替换的**战斗内。例如没开 boss 替换时，boss 战对本调度器等同于战斗外：
/// 我们的曲子保持压低继续播，vanilla 自己放 boss 曲，放完我们的就悄悄退出（此时不抢回音乐）。
///
/// ## 关键实现约束（都是 IL 验证过的）
/// - 流必须 <c>Loop = false</c>，否则 <c>Finished</c> 永不触发，整套调度无从谈起
/// - player 必须挂在 **SceneTree.Root** 下。挂 NCombatRoom.CombatVfxContainer 会随战斗房间一起销毁，
///   战斗一结束就没了，做不到「战斗后继续播」
/// - 交还用 <c>NRunMusicController.UpdateMusic()</c>：它重新解析当前 Act 的曲目并 update_music，
///   且**不会回调 PlayCustomMusic**（无递归风险）。vanilla 自己只在 NRun._Ready 和换 Act 时调它，
///   战斗结束不会自动重启音乐 —— 所以中间这段时间音乐是我们独占的，不会重叠
/// - vanilla 的 <c>StopMusic()</c> 由玩家死亡（CreatureCmd.Kill）和退回主菜单（NPauseMenu.CloseToMenu）
///   触发，是我们唯一该**硬停**的时机
///
/// 三个开关（Settings → Mods → 若叶睦角色）：
///   - <c>EnableCustomBgm</c>          非 boss 战
///   - <c>EnableCustomBgmBoss</c>      boss 战（默认关）
///   - <c>UseBgmForAllCharacters</c>   非若叶睦角色也替换（默认关）
/// </summary>
[HarmonyPatch]
internal static class CustomBgmPatch
{
    private const string AudioDir = "res://MzmChar/audio/";
    // 注入 EncounterModel.CustomBgm 的标记，跟 vanilla boss FMOD event 区分
    private const string DummyBgmKey = "mzm:custom_bgm";

    private const float BaseVolumeDb = -6f;    // 正常档（相对用户滑条）
    private const float DuckOffsetDb = -14f;   // 战斗外在正常档基础上再压低
    private const float DuckFadeSeconds = 1.2f;
    private const float RaiseFadeSeconds = 0.8f;
    private const float TrackFadeInSeconds = 1.0f;   // 起新曲时淡入
    private const float VanillaFadeInSeconds = 1.5f; // 交还后让 vanilla 淡入
    private const float SilentDb = -60f;

    private static AudioStreamPlayer? _player;
    private static Tween? _volTween;
    private static string? _currentPath;

    /// <summary>当前是否处于「启用了替换的战斗」中 —— 决定音量档位与播完后的去向。</summary>
    private static bool _combatActive;

    /// <summary>我们是否停掉过 vanilla 的音乐且它还没自己恢复。只有这种情况交还时才需要抢回来。</summary>
    private static bool _vanillaStopped;

    /// <summary>当前这首曲子起播时所属的战斗 id（装箱存放，只用于相等比较）。决定播完是循环还是换曲。</summary>
    private static object? _trackCombatKey;

    /// <summary>交还 vanilla 时驱动其 BGM 总线淡入的 tween。</summary>
    private static Tween? _vanillaFadeTween;

    /// <summary>true 表示 SetBgmVol 是我们自己为了淡入调的，postfix 应当忽略。</summary>
    private static bool _selfDrivingBgmVol;

    private static List<string>? _bgmPaths;
    private static readonly Dictionary<string, AudioStream> _streamCache = new();
    private static int _bgmBusIndex = -2;

    // 游戏的 BGM/Master 音量滑条走 FMOD 总线（NAudioManager.SetBgmVol -> _audioNode.Call），
    // 跟 Godot AudioServer 是两套系统；不手动同步过来，我们的 AudioStreamPlayer 听不见滑条变化。
    private static float _bgmLinearVol = 1f;
    private static float _masterLinearVol = 1f;

    // ───────────────────────── 适用性判定 ─────────────────────────

    /// <summary>若叶睦是否在本局中（读 NRunMusicController._runState.Players[].Character）。</summary>
    private static bool IsOurCharInRun(NRunMusicController? controller)
    {
        controller ??= NRun.Instance?.RunMusicController;
        if (controller == null) return false;
        var runState = AccessTools.Field(typeof(NRunMusicController), "_runState")?.GetValue(controller);
        if (runState == null) return false;
        if (AccessTools.Property(runState.GetType(), "Players")?.GetValue(runState)
            is not System.Collections.IEnumerable players) return false;
        foreach (var p in players)
        {
            var ch = AccessTools.Property(p.GetType(), "Character")?.GetValue(p);
            if (ch is MutsumiCharacter) return true;
        }
        return false;
    }

    /// <summary>本局是否该由我们接管 BGM：若叶睦在场，或用户开了「所有角色都用」。</summary>
    private static bool BgmApplies(NRunMusicController? controller) =>
        MzmCharConfig.UseBgmForAllCharacters || IsOurCharInRun(controller);

    /// <summary>根据房间类型选用对应开关。</summary>
    private static bool ShouldUseCustomBgm(RoomType roomType) =>
        roomType == RoomType.Boss ? MzmCharConfig.EnableCustomBgmBoss : MzmCharConfig.EnableCustomBgm;

    // ───────────────────────── vanilla 入口 ─────────────────────────

    // vanilla EncounterModel.HasBgm 对非 boss 遭遇默认 false，PlayCustomMusic 根本不会被调用。
    // 强制 HasBgm=true + CustomBgm=<marker>，把调用链引到我们这里。
    [HarmonyPatch(typeof(EncounterModel), "get_HasBgm")]
    [HarmonyPostfix]
    private static void HasBgm_Postfix(EncounterModel __instance, ref bool __result)
    {
        if (!ShouldUseCustomBgm(__instance.RoomType)) return;
        if (!BgmApplies(null)) return;
        __result = true;
    }

    [HarmonyPatch(typeof(EncounterModel), "get_CustomBgm")]
    [HarmonyPostfix]
    private static void CustomBgm_Postfix(EncounterModel __instance, ref string __result)
    {
        if (!ShouldUseCustomBgm(__instance.RoomType)) return;
        if (!BgmApplies(null)) return;
        __result = DummyBgmKey;
    }

    [HarmonyPatch(typeof(NRunMusicController), "PlayCustomMusic")]
    [HarmonyPrefix]
    private static bool PlayCustomMusic_Prefix(NRunMusicController __instance, string customMusic)
    {
        if (!BgmApplies(__instance)) return true;

        // vanilla 子类 override CustomBgm 返回 FMOD event 时基类 postfix 不触发；
        // 用非空 customMusic 兜底识别 boss/elite。
        bool ours = customMusic == DummyBgmKey
                    || (!string.IsNullOrEmpty(customMusic) && MzmCharConfig.EnableCustomBgmBoss);
        if (!ours)
        {
            // 这场战斗不归我们管（例如没开 boss 替换的 boss 战）。
            // 对调度器而言等同于「战斗外」：我们的曲子保持压低继续放完，之后不抢音乐。
            _combatActive = false;
            _vanillaStopped = false;   // vanilla 马上要放自己的曲子
            ApplyVolume(fade: true);
            return true;
        }

        _combatActive = true;
        KillVanillaFade();   // 若上一轮交还的淡入还没走完，先把总线拨回用户值再接管
        StopVanillaMusicViaProxy(__instance);
        _vanillaStopped = true;

        if (IsPlaying())
            ApplyVolume(fade: true);   // 已在播 -> 不换曲，只把音量抬回正常
        else
            StartNextTrack();

        return false;
    }

    // ───────────────────────── 战斗结束 ─────────────────────────

    // 胜利（EndCombatInternal）与败北（LoseCombat）都要接，否则有一条路径漏掉压低。
    //
    // v0.110 起 EndCombatInternal 有 0 参 + 1 参(CombatTurnState) 两个重载：0 参只是转发壳，
    // 真实胜利路径 CheckWinCondition 直调 1 参版（IL-verified）。属性写法遇重载歧义时 Harmony
    // 回退绑 0 参壳 -> prefix 不触发。TargetMethod 选参数最多的重载：stable v0.107 只有 0 参，
    // beta 选 1 参真路径（0 参壳转发进 1 参，两条入口都覆盖）。
    [HarmonyPatch]
    private static class EndCombatInternalPatch
    {
        private static System.Reflection.MethodBase TargetMethod() =>
            typeof(CombatManager)
                .GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic |
                            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Static)
                .Where(m => m.Name == "EndCombatInternal")
                .OrderByDescending(m => m.GetParameters().Length)
                .First();

        [HarmonyPrefix]
        private static void Prefix() => OnCombatEnded();
    }

    [HarmonyPatch(typeof(CombatManager), "LoseCombat")]
    [HarmonyPrefix]
    private static void LoseCombat_Prefix() => OnCombatEnded();

    /// <summary>战斗结束：只压低音量，**不停**。放完那一首再由 OnTrackFinished 决定去向。</summary>
    private static void OnCombatEnded()
    {
        _combatActive = false;
        if (IsPlaying()) ApplyVolume(fade: true);
    }

    // vanilla 决定把所有音乐停掉（玩家死亡 CreatureCmd.Kill / 退回主菜单 NPauseMenu.CloseToMenu
    // / 控制器离开场景树）—— 这是唯一该硬停我们的时机，否则曲子会一路放到主菜单。
    [HarmonyPatch(typeof(NRunMusicController), "StopMusic")]
    [HarmonyPrefix]
    private static void StopMusic_Prefix()
    {
        _combatActive = false;
        _vanillaStopped = false;
        KillVanillaFade();   // 把 BGM 总线拨回用户值，别让淡入中途被打断后留在低音量
        StopImmediate();
    }

    // ───────────────────────── 音量 ─────────────────────────

    // 两个滑条都会调对应 setter（包括启动时载入持久化设置那次），挂 postfix 就够。
    [HarmonyPatch(typeof(NAudioManager), "SetBgmVol")]
    [HarmonyPostfix]
    private static void SetBgmVol_Postfix(float volume)
    {
        // 交还时我们会自己驱动 SetBgmVol 做淡入 —— 那些斜坡值不是用户设置，别记下来
        if (_selfDrivingBgmVol) return;

        _bgmLinearVol = volume;
        KillVanillaFade(restore: false);   // 用户动了滑条 → 放弃淡入，以用户值为准
        ApplyVolume(fade: false);
    }

    [HarmonyPatch(typeof(NAudioManager), "SetMasterVol")]
    [HarmonyPostfix]
    private static void SetMasterVol_Postfix(float volume)
    {
        _masterLinearVol = volume;
        ApplyVolume(fade: false);
    }

    /// <summary>当前应有的音量：用户滑条 + 基础档 +（战斗外再压低）。</summary>
    private static float TargetVolumeDb()
    {
        var linear = Math.Max(_bgmLinearVol * _masterLinearVol, 0.0001f);
        var db = BaseVolumeDb + (float)(20.0 * Math.Log10(linear));
        return _combatActive ? db : db + DuckOffsetDb;
    }

    /// <summary>把音量调到目标档。fade=true 平滑过渡（战斗开始/结束），false 立即（滑条变化）。</summary>
    private static void ApplyVolume(bool fade)
    {
        if (!IsPlaying()) return;
        var p = _player!;
        var target = TargetVolumeDb();

        if (!fade)
        {
            KillVolTween();
            p.VolumeDb = target;
            return;
        }
        FadeTo(target, _combatActive ? RaiseFadeSeconds : DuckFadeSeconds);
    }

    /// <summary>把我们自己的 player 平滑推到指定音量。</summary>
    private static void FadeTo(float targetDb, float seconds)
    {
        if (!IsPlaying()) return;
        var p = _player!;
        KillVolTween();
        try
        {
            _volTween = p.CreateTween();
            _volTween.TweenProperty(p, (NodePath)"volume_db", targetDb, seconds);
        }
        catch (Exception e)
        {
            ModEntry.Log($"[Bgm] volume tween err: {e.Message}");
            p.VolumeDb = targetDb;
        }
    }

    private static void KillVolTween()
    {
        if (_volTween != null && GodotObject.IsInstanceValid(_volTween)) _volTween.Kill();
        _volTween = null;
    }

    // ───────────────────────── 播放 ─────────────────────────

    private static bool IsPlaying() => _player != null && GodotObject.IsInstanceValid(_player);

    /// <summary>当前战斗 id（装箱）。拿不到就返回 null —— 调用方按「同一场」保守处理。</summary>
    private static object? CurrentCombatKey()
    {
        try
        {
            var cm = CombatManager.Instance;
            if (cm == null) return null;
            var id = cm.CurrentCombatId;
            return id.HasValue ? id.Value : null;
        }
        catch { return null; }
    }

    /// <summary>随机起下一首（尽量不与当前这首重复），音量按当前档位。</summary>
    private static void StartNextTrack()
    {
        var paths = GetBgmPaths();
        if (paths.Count == 0) { ModEntry.Log($"[Bgm] no audio files in {AudioDir}"); return; }

        // 不用 RunState.Rng（那是 deterministic 同步用的）；BGM 是纯本地体验
        var pick = paths[(int)(GD.Randi() % (uint)paths.Count)];
        if (paths.Count > 1 && pick == _currentPath)                   // 避免连播同一首
            pick = paths[(paths.IndexOf(pick) + 1) % paths.Count];

        var stream = LoadStream(pick);
        if (stream == null) return;

        var root = (Engine.GetMainLoop() as SceneTree)?.Root;
        if (root == null) { ModEntry.Log("[Bgm] no SceneTree root; cannot start"); return; }

        StopImmediate();

        _currentPath = pick;
        _trackCombatKey = CurrentCombatKey();   // 记下归属的战斗，播完时据此决定循环还是换曲
        _player = new AudioStreamPlayer
        {
            Name = "MzmCharBgm",
            Stream = stream,
            Bus = ResolveBgmBusName(),
            VolumeDb = SilentDb,                // 从静音起步，下面淡入，避免突然炸响
        };
        // 挂在 SceneTree 根下，这样战斗房间销毁也不影响它继续播
        root.AddChild(_player);
        _player.Finished += OnTrackFinished;
        _player.Play();
        FadeTo(TargetVolumeDb(), TrackFadeInSeconds);
        ModEntry.Log($"[Bgm] playing {pick} (combat={_combatActive}, {paths.Count} choices)");
    }

    /// <summary>一首放完：同场战斗循环、换了战斗才换曲、不在战斗则交还 vanilla。</summary>
    private static void OnTrackFinished()
    {
        if (_combatActive)
        {
            // 还是起播时那场战斗 → 循环同一首（一场战斗自始至终一首曲子）。
            // 取不到战斗 id 时也按「同一场」处理：宁可继续循环，也不要莫名其妙换曲。
            if (IsPlaying() && Equals(_trackCombatKey, CurrentCombatKey()))
            {
                _player!.Play();
                return;
            }
            StartNextTrack();   // 已经是另一场战斗了（曲子是从上一场带过来的）→ 随机换一首
            return;
        }

        ModEntry.Log("[Bgm] track finished out of combat -> hand back to vanilla");
        StopImmediate();
        if (_vanillaStopped)
        {
            _vanillaStopped = false;
            RestoreVanillaMusic();
        }
        // 否则 vanilla 自己正放着（例如没开替换的 boss 战），不要抢
    }

    /// <summary>交还：让 vanilla 按当前 Act 重新解析并播放它自己的音乐，并淡入（不要突然炸响）。</summary>
    private static void RestoreVanillaMusic()
    {
        try
        {
            var controller = NRunMusicController.Instance ?? NRun.Instance?.RunMusicController;
            if (controller == null || !GodotObject.IsInstanceValid(controller)) return;
            // _runState 为空说明没有进行中的 run（主菜单等），此时 UpdateMusic 会 NRE
            if (AccessTools.Field(typeof(NRunMusicController), "_runState")?.GetValue(controller) == null) return;

            FadeVanillaMusicIn(VanillaFadeInSeconds);   // 先把总线压到 0，再随 UpdateMusic 一起爬上来
            controller.UpdateMusic();
        }
        catch (Exception e)
        {
            ModEntry.Log($"[Bgm] restore vanilla err: {e.Message}");
            KillVanillaFade();   // 兜底：绝不把 BGM 总线留在 0
        }
    }

    // ── vanilla BGM 总线淡入 ────────────────────────────────────
    // NAudioManager.SetBgmVol 只是 _audioNode.Call(_setBgmVolume, pow(v)) —— **不写持久化**
    // （调用方只有音量滑条 UI 和启动载入设置，IL 验证过），所以可以临时驱动它做淡入。
    // 但必须保证任何路径都恢复到用户值，否则会把玩家的 BGM 永久静音。

    /// <summary>直接设 vanilla BGM 总线音量。期间屏蔽自己的 postfix，免得把斜坡值当成用户设置。</summary>
    private static void SetVanillaBgmVolume(float linear)
    {
        try
        {
            var am = NAudioManager.Instance;
            if (am == null || !GodotObject.IsInstanceValid(am)) return;
            _selfDrivingBgmVol = true;
            am.SetBgmVol(linear);
        }
        catch (Exception e) { ModEntry.Log($"[Bgm] set vanilla vol err: {e.Message}"); }
        finally { _selfDrivingBgmVol = false; }
    }

    private static void FadeVanillaMusicIn(float seconds)
    {
        KillVanillaFade(restore: false);
        var target = _bgmLinearVol;
        try
        {
            if (Engine.GetMainLoop() is not SceneTree tree) { SetVanillaBgmVolume(target); return; }
            SetVanillaBgmVolume(0f);
            _vanillaFadeTween = tree.CreateTween();
            _vanillaFadeTween.TweenMethod(Callable.From<float>(SetVanillaBgmVolume), 0f, target, seconds);
            // 收尾强制归位：即使 tween 精度有偏差也要落在用户值上
            _vanillaFadeTween.TweenCallback(Callable.From(() => SetVanillaBgmVolume(_bgmLinearVol)));
        }
        catch (Exception e)
        {
            ModEntry.Log($"[Bgm] vanilla fade err: {e.Message}; restore volume");
            SetVanillaBgmVolume(target);
        }
    }

    /// <summary>停掉淡入。restore=true 时把总线拨回用户值（用户自己动滑条的情况传 false）。</summary>
    private static void KillVanillaFade(bool restore = true)
    {
        bool had = _vanillaFadeTween != null;
        if (had && GodotObject.IsInstanceValid(_vanillaFadeTween)) _vanillaFadeTween!.Kill();
        _vanillaFadeTween = null;
        if (had && restore) SetVanillaBgmVolume(_bgmLinearVol);
    }

    private static AudioStream? LoadStream(string path)
    {
        if (_streamCache.TryGetValue(path, out var cached)) return cached;
        AudioStream? stream;
        try { stream = GD.Load<AudioStream>(path); }
        catch (Exception e) { ModEntry.Log($"[Bgm] Load fail {path}: {e.Message}"); return null; }
        if (stream == null) { ModEntry.Log($"[Bgm] Load returned null: {path}"); return null; }

        // ⚠️ 必须关闭循环：循环流永远不触发 Finished，整套「放完再决定」的调度就失效了
        if (stream is AudioStreamMP3 mp3) mp3.Loop = false;
        else if (stream is AudioStreamOggVorbis ogg) ogg.Loop = false;
        else if (stream is AudioStreamWav wav) wav.LoopMode = AudioStreamWav.LoopModeEnum.Disabled;

        _streamCache[path] = stream;
        return stream;
    }

    private static List<string> GetBgmPaths()
    {
        if (_bgmPaths != null) return _bgmPaths;

        var found = new HashSet<string>();
        var dir = DirAccess.Open(AudioDir);
        if (dir != null)
        {
            dir.IncludeNavigational = false;
            dir.IncludeHidden = false;
            foreach (var file in dir.GetFiles())
            {
                // packed 模式 source 文件被剥离，只剩 .import sidecar -> strip 后缀拿 canonical 路径
                string? logical = null;
                if (file.EndsWith(".import")) logical = file[..^".import".Length];
                else if (file.EndsWith(".mp3") || file.EndsWith(".ogg") || file.EndsWith(".wav")) logical = file;
                if (logical != null) found.Add(AudioDir + logical);
            }
        }
        else
        {
            ModEntry.Log($"[Bgm] DirAccess.Open({AudioDir}) failed");
        }

        _bgmPaths = found.ToList();
        _bgmPaths.Sort();
        return _bgmPaths;
    }

    /// <summary>硬停：只在 vanilla 要停掉一切音乐、或换曲前复用节点时调用。</summary>
    private static void StopImmediate()
    {
        KillVolTween();
        if (_player != null && GodotObject.IsInstanceValid(_player))
        {
            _player.Finished -= OnTrackFinished;
            _player.Stop();
            _player.QueueFree();
        }
        _player = null;
    }

    private static void StopVanillaMusicViaProxy(NRunMusicController controller)
    {
        try
        {
            var proxy = AccessTools.Field(typeof(NRunMusicController), "_proxy")?.GetValue(controller) as GodotObject;
            var stopName = AccessTools.Field(typeof(NRunMusicController), "_stopMusic")?.GetValue(null) as StringName;
            if (proxy != null && stopName != null && GodotObject.IsInstanceValid(proxy))
                proxy.Call(stopName);
        }
        catch (Exception e) { ModEntry.Log($"[Bgm] StopVanilla err: {e.Message}"); }
    }

    private static StringName ResolveBgmBusName()
    {
        if (_bgmBusIndex == -2)
        {
            foreach (var candidate in new[] { "Bgm", "BGM", "Music" })
            {
                var idx = AudioServer.GetBusIndex(candidate);
                if (idx >= 0) { _bgmBusIndex = idx; return candidate; }
            }
            _bgmBusIndex = -1;
        }
        return _bgmBusIndex >= 0 ? AudioServer.GetBusName(_bgmBusIndex) : "Master";
    }
}
