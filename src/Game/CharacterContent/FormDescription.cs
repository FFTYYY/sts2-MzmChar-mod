using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;

namespace MzmChar.Game;

/// <summary>
/// 卡牌 / power 描述里「当前形态高亮」的颜色 token。
///
/// 用法：
/// 1. loc 模板里把「[gold]小睦[/gold]」改成 `{MuOpen}小睦{MuClose}`，「[gold]小墨[/gold]」改成 `{MoOpen}小墨{MoClose}`
/// 2. 卡牌 override `AddExtraArgsToDescription`：
///    ```
///    protected override void AddExtraArgsToDescription(LocString description) {
///        base.AddExtraArgsToDescription(description);
///        FormDescription.AddTokens(this, description);
///    }
///    ```
///    （MzmCharBaseCard 已经加了，新卡只要继承它就自动有）
/// 3. 每次卡 render 都会调一次 → token 实时反映当前形态
///
/// 颜色：
///   - 当前形态 (active) → `[gold]X[/gold]`（金色高亮）
///   - 非当前形态 (inactive) → 空 wrapper，渲染成默认文字色（白色）
/// </summary>
public static class FormDescription
{
    // 用整段 open/close 而不是只替 color name —— 这样 inactive 的能完全不加 tag，
    // 用文字默认色（如果只替 color name，inactive 就得有个真实颜色 tag，没法回到 default）
    private const string MuActiveOpen = "[blue]";
    private const string MuActiveClose = "[/blue]";
    private const string MoActiveOpen = "[blue]";
    private const string MoActiveClose = "[/blue]";
    private const string InactiveOpen = "";
    private const string InactiveClose = "";

    // Section-level wrappers：把整段 inactive 形态的描述用半透明白色包起来
    // Godot RichTextLabel 标准 BBCode `[color=#RRGGBBAA]` 支持 alpha
    // MegaRichTextLabel 继承 Godot.RichTextLabel 且 BbcodeEnabled=true → 应原生支持
    private const string InactiveSectionOpen = "[color=#FFFFFF80]";   // 50% alpha 白
    private const string InactiveSectionClose = "[/color]";

    public static void AddTokens(CardModel card, LocString description)
    {
        // Canonical model（卡库 / preview 用）不能访 Owner —— 会崩 CanonicalModelException
        // 这种情况下默认 Mu form active
        bool muActive = card.IsCanonical || card.Owner == null || Forms.IsMutsumiForm(card.Owner);

        description.Add("MuOpen", muActive ? MuActiveOpen : InactiveOpen);
        description.Add("MuClose", muActive ? MuActiveClose : InactiveClose);
        description.Add("MoOpen", muActive ? InactiveOpen : MoActiveOpen);
        description.Add("MoClose", muActive ? InactiveClose : MoActiveClose);

        // 整段半透明：active 形态段是空 wrapper，inactive 段套半透明
        description.Add("MuSec", muActive ? "" : InactiveSectionOpen);
        description.Add("MuSecEnd", muActive ? "" : InactiveSectionClose);
        description.Add("MoSec", muActive ? InactiveSectionOpen : "");
        description.Add("MoSecEnd", muActive ? InactiveSectionClose : "");
    }
}
