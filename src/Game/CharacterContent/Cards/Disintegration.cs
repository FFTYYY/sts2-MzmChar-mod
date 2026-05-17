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
/// 人格解体：1/0 费金色能力。每切换一次人格，随机给牌组中 1 张牌添加「虚无」。
/// </summary>
[Pool(typeof(MzmCharCardPool))]
public class Disintegration : MzmCharBaseCard
{
    public override string PortraitPath => "res://MzmChar/cards/disintegration.png";

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get { yield return HoverTipFactory.FromPower<DisintegrationPower>(); }
    }

    public Disintegration() : base(1, CardType.Power, CardRarity.Rare, TargetType.Self) { }

    protected override void OnUpgrade() { EnergyCost.UpgradeBy(-1); }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await PlayCast();
        await PowerCmd.Apply<DisintegrationPower>(Owner.Creature, 1, Owner.Creature, this, false);
        if (Forms.IsMortisForm(Owner)) await CombatCounters.BumpMortisCard(ctx, Owner);
        else                            await CombatCounters.BumpMutsumiCard(ctx, Owner);
    }

    public override List<(string, string)>? Localization => LocManager.Instance.Language switch
    {
        "zhs" => new CardLoc("人格解体",
            "你每切换一次人格，随机给牌组中一张牌添加[gold]虚无[/gold]。"),
        _ => new CardLoc("Disintegration",
            "Whenever you switch personas, add [gold]Ethereal[/gold] to a random card in your deck."),
    };
}
