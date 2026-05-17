using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
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
/// Mortis：2/1 费蓝色能力。每切换一次人格，对全体敌人造成 6/10 伤害。
/// </summary>
[Pool(typeof(MzmCharCardPool))]
public class MortisCard : MzmCharBaseCard
{
    public override string PortraitPath => "res://MzmChar/cards/mortis_card.png";

    private readonly List<DynamicVar> _vars = new()
    {
        new DynamicVar("Dmg", 6m),
    };
    protected override IEnumerable<DynamicVar> CanonicalVars => _vars;

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get { yield return HoverTipFactory.FromPower<MortisCardPower>(); }
    }

    public MortisCard() : base(2, CardType.Power, CardRarity.Uncommon, TargetType.Self) { }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);              // 2 → 1
        DynamicVars["Dmg"].UpgradeValueBy(4);  // 6 → 10
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await PlayCast();
        await PowerCmd.Apply<MortisCardPower>(Owner.Creature,
            DynamicVars["Dmg"].BaseValue, Owner.Creature, this, false);
        if (Forms.IsMortisForm(Owner)) await CombatCounters.BumpMortisCard(ctx, Owner);
        else await CombatCounters.BumpMutsumiCard(ctx, Owner);
    }

    public override List<(string, string)>? Localization => LocManager.Instance.Language switch
    {
        "zhs" => new CardLoc("Mortis",
            "每切换一次人格，对全体敌人造成{Dmg:diff()}点伤害。"),
        _ => new CardLoc("Mortis",
            "Per persona switch, deal {Dmg:diff()} damage to ALL enemies."),
    };
}
