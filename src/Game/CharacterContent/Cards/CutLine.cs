using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;

namespace MzmChar.Game;

/// <summary>
/// 剪切线：1 费蓝色能力。回合结束时，每有 1 层「回合结束失去力量」获得 1 格挡。
/// </summary>
[Pool(typeof(MzmCharCardPool))]
public class CutLine : MzmCharBaseCard
{
    public override string PortraitPath => "res://MzmChar/cards/cut_line.png";

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromPower<CutLinePower>();
            yield return HoverTipFactory.FromPower<TempStrengthPower>();
        }
    }

    public CutLine() : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self) { }

    protected override void OnUpgrade() { AddKeyword(CardKeyword.Innate); }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await PlayCast();
        await Sts2Compat.PowerApply<CutLinePower>(ctx, Owner.Creature, 1, Owner.Creature, this, false);
        if (Forms.IsMortisForm(Owner)) await CombatCounters.BumpMortisCard(ctx, Owner);
        else await CombatCounters.BumpMutsumiCard(ctx, Owner);
    }

    public override List<(string, string)>? Localization => LocManager.Instance.Language switch
    {
        "zhs" => new CardLoc("剪切线",
            "回合结束时，你每有1层[gold]若叶睦的临时力量[/gold]，就获得1点[gold]格挡[/gold]。"),
        _ => new CardLoc("Cut Line",
            "At turn end, gain 1 [gold]Block[/gold] per stack of [gold]Wakaba's Temp Strength[/gold]."),
    };
}
