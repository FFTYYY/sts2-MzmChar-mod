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
    /// <summary>非 boss 战斗启用自定义 BGM。</summary>
    [ConfigSection("音频设置")]
    [ConfigHoverTip]
    public static bool EnableCustomBgm { get; set; } = false;

    /// <summary>boss 战也启用自定义 BGM。默认关，让原版 boss 主题曲优先。</summary>
    [ConfigHoverTip]
    public static bool EnableCustomBgmBoss { get; set; } = false;

    /// <summary>
    /// 非若叶睦角色也用本 mod 的战斗 BGM。默认关：只有若叶睦在场时才替换。
    /// 打开后，上面两个 BGM 开关对任意角色都生效（BGM 是纯本地效果，不影响联机同步）。
    /// </summary>
    [ConfigHoverTip]
    public static bool UseBgmForAllCharacters { get; set; } = false;

    /// <summary>
    /// 卡图风格（enum → BaseLib 自动渲染成下拉菜单）。默认用原本的卡图。
    /// 目录映射与"怎么加新风格"见 <see cref="MzmChar.Game.CardArtStyles"/>；
    /// 读取点只有一处：<see cref="MzmChar.Game.MzmCharBaseCard.Art"/>。
    /// </summary>
    [ConfigSection("卡面设置")]
    [ConfigHoverTip]
    [ConfigVisibleIf(nameof(HasAlternateCardArt))]
    public static MzmChar.Game.CardArtStyleId CardArtStyle { get; set; } = MzmChar.Game.CardArtStyleId.Default;

    /// <summary>
    /// 只有真的装了两套以上卡图时，才显示上面那个下拉框。
    /// 目前只有默认图集 → 整行自动隐藏；将来把新图集丢进 pack 后，它会自己出现。
    /// </summary>
    [ConfigIgnore]
    public static bool HasAlternateCardArt => MzmChar.Game.CardArtStyles.AvailableCount > 1;

    public MzmCharConfig() : base() { }
}
