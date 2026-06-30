using System.Reflection;
using BaseLib.Config;
using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;
using MzmChar.Config;
using MzmChar.Game;

namespace MzmChar;

/// <summary>
/// Mod 入口。BaseLib 已经处理了几乎所有 modding 基础设施 —— 这里只需要：
///   1. 创建 Harmony 实例并 PatchAll 我们 assembly 里的 [HarmonyPatch] 类（如果有的话）
///   2. 角色 / 卡 / 遗物 / 池子等内容通过继承 BaseLib 的 Custom*Model 自动注册，无需手动调
///      —— 它们的 ctor 在 ModelDb.Init 反射构造时自己向 BaseLib 登记。
/// </summary>
[ModInitializer(nameof(OnModLoaded))]
public static class ModEntry
{
    public const string ModId   = "MzmChar";
    public const string Version = "0.2.5";
    public static HarmonyLib.Harmony? Harmony { get; private set; }

    public static void OnModLoaded()
    {
        Log($"Loading {ModId} v{Version} ...");

        Harmony = new HarmonyLib.Harmony($"com.yongyi.{ModId}");
        Harmony.PatchAll(Assembly.GetExecutingAssembly());

        // 注册 mod 设置：游戏 Settings → Mods 里会出现 Mzm Character 子菜单
        // 持久化 / UI 全由 BaseLib 自动处理；属性是 static，patch 入口直接读 MzmCharConfig.X
        ModConfigRegistry.Register(ModId, new MzmCharConfig());

        // 预加载形态 SpriteFrames（含几十 MB atlas）—— 避免战斗中第一次切形态时同步加载卡顿
        Forms.Preload();
        // 预加载演奏会聚光灯 VFX（PackedScene + 66 帧锥光 + 1 影子）—— 避免第一次进 concert 卡顿
        ConcertSpotlightVfx.Preload();

        Log($"{ModId} loaded — character/cards/relic auto-registered via BaseLib.");
    }

    internal static void Log(string msg)
    {
        // 游戏会把 stdout 写进 %AppData%\Roaming\SlayTheSpire2\logs\godot.log
        System.Console.WriteLine($"[{ModId}] {msg}");
    }
}
