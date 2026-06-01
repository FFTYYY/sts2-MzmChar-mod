using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;

namespace MzmChar.Game;

/// <summary>
/// 小墨形态标记 buff。本身没效果。
/// 同 MutsumiFormPower 一样内挂 CombatCounters 的 Before/AfterCardPlayed bump hook，
/// 这样任何持有 form power 的 creature（包括队友被 HeartResonance 影响后）出牌都计数。
/// </summary>
public class MortisFormPower : CustomPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    public override string? CustomPackedIconPath => "res://MzmChar/powers/mo_form.png";
    public override string? CustomBigIconPath    => "res://MzmChar/powers/mo_form.png";

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
        "zhs" => new PowerLoc("小墨", "你是墨缇斯。", "你是墨缇斯。"),
        _     => new PowerLoc("Little Mo", "You are Mortis.", "You are Mortis."),
    };
}
