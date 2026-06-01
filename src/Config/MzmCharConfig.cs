using BaseLib.Config;

namespace MzmChar.Config;

/// <summary>
/// Mod 设置：在游戏 Settings → Mods → Mzm Character 里显示成可勾选项。
/// 持久化、UI 都是 BaseLib 自动处理；patch 入口直接读 static property 即可。
///
/// 本地化 keys 见 pack/MzmChar/localization/{zhs,eng}/settings_ui.json
/// 格式：MZMCHAR-&lt;PROPERTY_AS_SCREAMING_SNAKE&gt;.title / .hover.title / .hover.desc
/// </summary>
public class MzmCharConfig : SimpleModConfig
{
    /// <summary>非 boss 战斗启用自定义 BGM（保留原有 property name，旧用户存档的开关状态延续）</summary>
    [ConfigSection("音频设置")]
    [ConfigHoverTip]
    public static bool EnableCustomBgm { get; set; } = false;

    /// <summary>boss 战也启用自定义 BGM（新选项；默认关，让原版 boss 主题曲优先）</summary>
    [ConfigHoverTip]
    public static bool EnableCustomBgmBoss { get; set; } = false;

    public MzmCharConfig() : base() { }
}
