using System.Collections.Generic;
using System.Linq;
using Godot;

namespace MzmChar.Game;

/// <summary>
/// 卡图风格。BaseLib 的 SimpleModConfig 对 enum 属性会自动渲染成**下拉菜单**
/// （见 BaseLib <c>SimpleModConfig.GenerateOptionFromProperty</c> → <c>CreateDropdownOption</c>）。
///
/// ⚠️ 枚举值会被序列化进存档：**只能往后追加，不要改名、插队或删除**，
/// 否则老存档里的设置会指到别的风格。
/// </summary>
public enum CardArtStyleId
{
    /// <summary>默认卡图（pack/MzmChar/cards/）。</summary>
    Default = 0,

    /// <summary>儿童简笔画卡图（pack/MzmChar/cards_child/）。图集尚未制作，因此该选项当前自动隐藏。</summary>
    ChildDrawing = 1,
}

/// <summary>
/// 卡图风格 → 资源目录的映射，外加「这套图集到底有没有装进包里」的探测。
///
/// 设计要点：**图集不存在时，设置项自动隐藏、路径自动回落默认**。
/// 所以登记一个还没画的风格是安全的 —— 等哪天把图丢进 pack，下拉框自己就出现了。
///
/// 加一套新风格：
///   1. 把图放进 <c>pack/MzmChar/cards_&lt;xxx&gt;/</c>，文件名与 <c>cards/</c> **逐一同名**；
///   2. <see cref="CardArtStyleId"/> 末尾追加枚举值，并在 <see cref="Dirs"/> 里补一行；
///   3. 两份 settings_ui.json 里加 <c>MZMCHAR-CARD_ART_STYLE.&lt;枚举值名&gt;</c> 作为下拉项显示名。
/// 93 张卡的代码一行都不用动 —— 它们的 PortraitPath 统一走 <see cref="MzmCharBaseCard.Art"/>。
/// </summary>
public static class CardArtStyles
{
    private static readonly Dictionary<CardArtStyleId, string> Dirs = new()
    {
        [CardArtStyleId.Default] = "cards",
        [CardArtStyleId.ChildDrawing] = "cards_child",
    };

    /// <summary>探测用的代表文件：每套图集都必然包含它（初始卡「打击」）。</summary>
    private const string ProbeCard = "strike";

    private static Dictionary<CardArtStyleId, bool>? _available;

    private static Dictionary<CardArtStyleId, bool> Available =>
        _available ??= Dirs.ToDictionary(
            kv => kv.Key,
            kv => ResourceLoader.Exists($"res://MzmChar/{kv.Value}/{ProbeCard}.png"));

    /// <summary>实际装进包里的图集数量。只有 &gt;1 时切换才有意义（设置项据此显隐）。</summary>
    public static int AvailableCount => Available.Count(kv => kv.Value);

    /// <summary>
    /// 当前生效的资源目录名。选中的风格若不存在（图集没做 / 老存档指向已移除的风格），
    /// 安全回落到默认图集，不会出现白图或崩溃。
    /// </summary>
    public static string CurrentDir
    {
        get
        {
            var id = MzmChar.Config.MzmCharConfig.CardArtStyle;
            if (Dirs.TryGetValue(id, out var dir) && Available.TryGetValue(id, out var ok) && ok)
                return dir;
            return Dirs[CardArtStyleId.Default];
        }
    }
}
