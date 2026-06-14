using System;
using Godot;

namespace MzmChar.Game;

/// <summary>
/// mod 内部 trace / warn 走这里。必须用 <c>Godot.GD.Print</c> 而非 <c>Console.WriteLine</c>，
/// 后者在 Godot mono runtime 下不进玩家 log 文件。前缀固定 <c>[MzmChar]</c> 方便 grep。
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
