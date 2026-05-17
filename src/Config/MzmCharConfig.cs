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
    [ConfigSection("音频设置")]
    [ConfigHoverTip]
    public static bool EnableCustomBgm { get; set; } = false;

    public MzmCharConfig() : base() { }
}
