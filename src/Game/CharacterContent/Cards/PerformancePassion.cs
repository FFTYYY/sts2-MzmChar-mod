using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Cards.Variables;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace MzmChar.Game;

/// <summary>
/// 演艺热情：1 费蓝色技能。抽 2/3 张。获得 1 演艺热情。
/// </summary>
[Pool(typeof(MzmCharCardPool))]
public class PerformancePassion : MzmCharBaseCard
{
    public override string PortraitPath => "res://MzmChar/cards/passion_card.png";

    private readonly List<DynamicVar> _vars = new() { new CardsVar(2) };
    protected override IEnumerable<DynamicVar> CanonicalVars => _vars;

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get { yield return HoverTipFactory.FromPower<PerformancePassionPower>(); }
    }

    public PerformancePassion() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self) { }

    protected override void OnUpgrade() { DynamicVars.Cards.UpgradeValueBy(1); /* 2→3 */ }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await PlayCast();
        await CardPileCmd.Draw(ctx, DynamicVars.Cards.BaseValue, Owner, false);
        await PowerCmd.Apply<PerformancePassionPower>(Owner.Creature, 1, Owner.Creature, this, false);
        if (Forms.IsMortisForm(Owner)) await CombatCounters.BumpMortisCard(ctx, Owner);
        else                            await CombatCounters.BumpMutsumiCard(ctx, Owner);
    }

    public override List<(string, string)>? Localization => LocManager.Instance.Language switch
    {
        "zhs" => new CardLoc("演艺热情",
            "抽{Cards:diff()}张牌。获得1点[gold]演艺热情[/gold]。"),
        _ => new CardLoc("Performance Passion",
            "Draw {Cards:diff()}. Gain 1 [gold]Performance Passion[/gold]."),
    };
}
