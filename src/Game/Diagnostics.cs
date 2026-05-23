using System;
using Godot;

namespace MzmChar.Game;

/// <summary>
/// 诊断日志 helper —— 所有 mod 内部 trace / warn 走这里。
///
/// **必须**用 `Godot.GD.Print` 而非 `Console.WriteLine`：
///   - `Console.WriteLine` 在 Godot mono runtime 下指向 game stdout，但 **不进** 玩家 log 文件
///     （`%AppData%/Roaming/SlayTheSpire2/logs/godot_*.log`）
///   - `GD.Print` 是 Godot 标准 logger，走 OS::print → 自动写到 log 文件 + Output panel
///   - BaseLib 的 `[BaseLib]` 前缀日志能在玩家 log 里看到，就是用 Godot logger
///
/// 历史教训（report_47 调查多人同死卡死时发现的）：
/// 旧实现用 Console.WriteLine，开发时本机能在 godot.exe terminal 看到，部署后玩家 log
/// 完全无 `[MzmChar]` 字样，所有死亡链诊断点等于白埋。这次重写后玩家发的 log 能 grep
/// `[MzmChar]` 直接定位。
///
/// 前缀固定 `[MzmChar]` 让玩家发 log 时一眼能从 vanilla / 其它 mod 日志里 grep 出来。
/// 频率：death anim 链 / Forms 切换 / hidden power apply 等关键路径打点。
/// 量大的位置（如每帧 / 每次 BumpMu/MoCard）加 `if-cond` 限制。
/// </summary>
internal static class Diag
{
    private const string Prefix = "[MzmChar]";

    /// <summary>常规 trace。量大的位置加 `if-cond` 才调。</summary>
    public static void Trace(string msg)
    {
        GD.Print($"{Prefix} {msg}");
    }

    /// <summary>异常/异常分支警告。</summary>
    public static void Warn(string msg)
    {
        GD.PushWarning($"{Prefix} {msg}");
    }

    /// <summary>例外抓捕，记录但不抛。</summary>
    public static void Exception(string where, Exception ex)
    {
        GD.PushError($"{Prefix} [EXC] {where}: {ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}");
    }
}
