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
/// 死亡之湖：1 费蓝色能力。如果你以小墨人格开始回合，则获得 3 点临时力量。升级：固有。
/// </summary>
[Pool(typeof(MzmCharCardPool))]
public class DeathLake : MzmCharBaseCard
{
    public override string PortraitPath => "res://MzmChar/cards/death_lake.png";

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get { yield return HoverTipFactory.FromPower<DeathLakePower>(); }
    }

    public DeathLake() : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self) { }

    protected override void OnUpgrade() { AddKeyword(CardKeyword.Innate); }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await PlayCast();
        await Sts2Compat.PowerApply<DeathLakePower>(ctx, Owner.Creature, 1, Owner.Creature, this, false);
        if (Forms.IsMortisForm(Owner)) await CombatCounters.BumpMortisCard(ctx, Owner);
        else await CombatCounters.BumpMutsumiCard(ctx, Owner);
    }

    public override List<(string, string)>? Localization => LocManager.Instance.Language switch
    {
        "zhs" => new CardLoc("死亡之湖",
            "以[gold]小墨[/gold]开始回合时，本回合获得3点[gold]力量[/gold]。"),
        _ => new CardLoc("Lake of Death",
            "If you start your turn as [gold]Mo[/gold], gain 3 [gold]Strength[/gold] this turn."),
    };
}
