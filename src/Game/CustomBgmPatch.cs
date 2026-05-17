using System;
using System.Collections.Generic;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Audio;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Rooms;
using MzmChar.Config;

namespace MzmChar.Game;

/// <summary>
/// 战斗 BGM 替换：若叶睦在场且非 boss 战时，把 vanilla 区域 / 战斗曲静音，从 pack/MzmChar/audio/
/// 随机抽一首 mp3 循环播放；boss 战保留 vanilla 专属曲。战斗结束 / 败北时渐弱停止。
/// 通过 Settings → Mods → 若叶睦角色 → 启用自定义战斗 BGM 开关。
/// </summary>
[HarmonyPatch]
internal static class CustomBgmPatch
{
    private const string AudioDir = "res://MzmChar/audio/";
    // 注入 EncounterModel.CustomBgm 的标记，跟 vanilla boss FMOD event 区分
    private const string DummyBgmKey = "mzm:custom_bgm";
    private const float FadeOutSeconds = 1.5f;
    private const float StartVolumeDb = -6f;

    private static AudioStreamPlayer? _player;
    private static List<string>? _bgmPaths;
    private static readonly Dictionary<string, AudioStream> _streamCache = new();
    private static int _bgmBusIndex = -2;

    private static bool IsOurCharInCombat(NRunMusicController? controller)
    {
        controller ??= NRun.Instance?.RunMusicController;
        if (controller == null) return false;
        var runState = AccessTools.Field(typeof(NRunMusicController), "_runState")?.GetValue(controller);
        if (runState == null) return false;
        var players = AccessTools.Property(runState.GetType(), "Players")?.GetValue(runState)
            as System.Collections.IEnumerable;
        if (players == null) return false;
        foreach (var p in players)
        {
            var ch = AccessTools.Property(p.GetType(), "Character")?.GetValue(p);
            if (ch is MutsumiCharacter) return true;
        }
        return false;
    }

    // vanilla EncounterModel.HasBgm defaults to false for non-boss encounters, so PlayCustomMusic
    // is never invoked. Force HasBgm=true and CustomBgm=<marker> so the call chain reaches us.
    [HarmonyPatch(typeof(EncounterModel), "get_HasBgm")]
    [HarmonyPostfix]
    private static void HasBgm_Postfix(EncounterModel __instance, ref bool __result)
    {
        if (!MzmCharConfig.EnableCustomBgm) return;
        if (!IsOurCharInCombat(null)) return;
        if (__instance.RoomType == RoomType.Boss) return;
        __result = true;
    }

    [HarmonyPatch(typeof(EncounterModel), "get_CustomBgm")]
    [HarmonyPostfix]
    private static void CustomBgm_Postfix(EncounterModel __instance, ref string __result)
    {
        if (!MzmCharConfig.EnableCustomBgm) return;
        if (!IsOurCharInCombat(null)) return;
        if (__instance.RoomType == RoomType.Boss) return;
        __result = DummyBgmKey;
    }

    [HarmonyPatch(typeof(NRunMusicController), "PlayCustomMusic")]
    [HarmonyPrefix]
    private static bool PlayCustomMusic_Prefix(NRunMusicController __instance, string customMusic)
    {
        if (!MzmCharConfig.EnableCustomBgm) return true;
        if (!IsOurCharInCombat(__instance)) return true;
        // Real FMOD events (e.g. vanilla boss themes) pass through.
        if (customMusic != DummyBgmKey) return true;
        StopVanillaMusicViaProxy(__instance);
        StartCustom();
        return false;
    }

    // Hook both victory (EndCombatInternal) and defeat (LoseCombat) so the fade-out always runs.
    // Skip the config gate: if music is playing it should stop regardless of toggle state.
    [HarmonyPatch(typeof(CombatManager), "EndCombatInternal")]
    [HarmonyPrefix]
    private static void EndCombatInternal_Prefix()
    {
        if (_player == null) return;
        FadeOutAndStop(FadeOutSeconds);
    }

    [HarmonyPatch(typeof(CombatManager), "LoseCombat")]
    [HarmonyPrefix]
    private static void LoseCombat_Prefix()
    {
        if (_player == null) return;
        FadeOutAndStop(FadeOutSeconds);
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

    private static void StartCustom()
    {
        StopCustomImmediate();

        var paths = GetBgmPaths();
        if (paths.Count == 0) { ModEntry.Log($"[Bgm] no audio files in {AudioDir}"); return; }

        // 不用 player.RunState.Rng（那是 deterministic 用的）；BGM 是本地体验
        var pick = paths[(int)(GD.Randi() % (uint)paths.Count)];

        if (!_streamCache.TryGetValue(pick, out var stream))
        {
            try { stream = GD.Load<AudioStream>(pick); }
            catch (Exception e) { ModEntry.Log($"[Bgm] Load fail {pick}: {e.Message}"); return; }
            if (stream == null) { ModEntry.Log($"[Bgm] Load returned null: {pick}"); return; }
            if (stream is AudioStreamMP3 mp3) mp3.Loop = true;
            else if (stream is AudioStreamOggVorbis ogg) ogg.Loop = true;
            else if (stream is AudioStreamWav wav) wav.LoopMode = AudioStreamWav.LoopModeEnum.Forward;
            _streamCache[pick] = stream;
        }

        var container = NCombatRoom.Instance?.CombatVfxContainer;
        if (container == null) { ModEntry.Log("[Bgm] no CombatVfxContainer; cannot start"); return; }

        _player = new AudioStreamPlayer
        {
            Stream = stream,
            Bus = ResolveBgmBusName(),
            VolumeDb = StartVolumeDb,
        };
        container.AddChild(_player);
        _player.Play();
        ModEntry.Log($"[Bgm] playing {pick} on bus '{_player.Bus}' ({paths.Count} choices)");
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
                // packed 模式 source 文件被剥离，只剩 .import sidecar → strip 后缀拿 canonical 路径
                string? logical = null;
                if (file.EndsWith(".import"))
                    logical = file[..^".import".Length];
                else if (file.EndsWith(".mp3") || file.EndsWith(".ogg") || file.EndsWith(".wav"))
                    logical = file;

                if (logical != null) found.Add(AudioDir + logical);
            }
        }
        else
        {
            ModEntry.Log($"[Bgm] DirAccess.Open({AudioDir}) failed");
        }

        _bgmPaths = new List<string>(found);
        _bgmPaths.Sort();
        return _bgmPaths;
    }

    private static void StopCustomImmediate()
    {
        if (_player != null && GodotObject.IsInstanceValid(_player))
        {
            _player.Stop();
            _player.QueueFree();
        }
        _player = null;
    }

    private static void FadeOutAndStop(float seconds)
    {
        if (_player == null || !GodotObject.IsInstanceValid(_player)) { _player = null; return; }

        var p = _player;
        _player = null;

        try
        {
            var tween = p.CreateTween();
            tween.TweenProperty(p, (NodePath)"volume_db", -80.0, seconds);
            tween.TweenCallback(Callable.From(() =>
            {
                if (GodotObject.IsInstanceValid(p))
                {
                    p.Stop();
                    p.QueueFree();
                }
            }));
        }
        catch (Exception e)
        {
            ModEntry.Log($"[Bgm] fade err: {e.Message}; stop immediately");
            if (GodotObject.IsInstanceValid(p)) { p.Stop(); p.QueueFree(); }
        }
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
