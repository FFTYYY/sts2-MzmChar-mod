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
/// 人偶之心：2/1 费金色能力。每打出能力牌获得 1 点力量。
/// </summary>
[Pool(typeof(MzmCharCardPool))]
public class DollHeart : MzmCharBaseCard
{
    public override string PortraitPath => "res://MzmChar/cards/doll_heart.png";

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get { yield return HoverTipFactory.FromPower<DollHeartPower>(); }
    }

    public DollHeart() : base(2, CardType.Power, CardRarity.Rare, TargetType.Self) { }

    protected override void OnUpgrade() { EnergyCost.UpgradeBy(-1); }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await PlayCast();
        await Sts2Compat.PowerApply<DollHeartPower>(ctx, Owner.Creature, 1, Owner.Creature, this, false);
        if (Forms.IsMortisForm(Owner)) await CombatCounters.BumpMortisCard(ctx, Owner);
        else                            await CombatCounters.BumpMutsumiCard(ctx, Owner);
    }

    public override List<(string, string)>? Localization => LocManager.Instance.Language switch
    {
        "zhs" => new CardLoc("人偶之心",
            "每当你打出[gold]能力[/gold]牌，获得1点[gold]力量[/gold]。"),
        _ => new CardLoc("Doll Heart",
            "Whenever you play a [gold]Power[/gold] card, gain 1 [gold]Strength[/gold]."),
    };
}
