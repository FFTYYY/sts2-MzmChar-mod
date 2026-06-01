using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;

namespace MzmChar.Game;

/// <summary>
/// 小睦形态标记 buff。本身没效果 —— 卡牌通过 Forms.IsMutsumiForm(player) 判断使用哪份效果。
/// 默认形态规则：没 buff、或两个 buff 都有时，都视为小睦（在 Forms.IsMortisForm 里实现）。
///
/// 额外：内挂 CombatCounters 的 Before/AfterCardPlayed bump hook，下沉自
/// SecondPersonaRelic（原来挂遗物上，导致队友拿到 MzmChar 卡却没被 bump 计数）。
/// 现在 form power 上挂 hook → 任何持有 form power 的 creature（包括 HeartResonance
/// 给队友 EnterMutsumi 之后的队友）出牌都会自动 bump counter。
/// 跟 MortisFormPower 写一样的代码（form 切换 Remove 一个 Apply 另一个，两个 power 都需要）。
/// 内部 CombatCounters.OnBeforeCardPlayed 自带 owner filter（cardPlay.Card.Owner != owner 就跳过），
/// 不会处理非 owner 的卡。
/// </summary>
public class MutsumiFormPower : CustomPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    public override string? CustomPackedIconPath => "res://MzmChar/powers/mu_form.png";
    public override string? CustomBigIconPath    => "res://MzmChar/powers/mu_form.png";

    public override Task BeforeCardPlayed(CardPlay cardPlay)
    {
        if (Owner?.Player != null) CombatCounters.OnBeforeCardPlayed(Owner.Player, cardPlay);
        return Task.CompletedTask;
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext ctx, CardPlay cardPlay)
    {
        if (Owner?.Player != null) await CombatCounters.OnAfterCardPlayed(ctx, Owner.Player, cardPlay);
    }

    public override List<(string, string)>? Localization => LocManager.Instance.Language switch
    {
        "zhs" => new PowerLoc("小睦", "你是小睦。", "你是小睦。"),
        _     => new PowerLoc("Little Mu", "You are Mutsumi.", "You are Mutsumi."),
    };
}
