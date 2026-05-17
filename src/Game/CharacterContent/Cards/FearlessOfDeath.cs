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
/// 无惧死亡：2 费蓝色能力。本场战斗下次死亡时以 1/20 点生命复活。
/// </summary>
[Pool(typeof(MzmCharCardPool))]
public class FearlessOfDeath : MzmCharBaseCard
{
    public override string PortraitPath => "res://MzmChar/cards/fearless_of_death.png";

    private readonly List<DynamicVar> _vars = new()
    {
        new DynamicVar("ReviveHp", 1m),
    };
    protected override IEnumerable<DynamicVar> CanonicalVars => _vars;

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get { yield return HoverTipFactory.FromPower<FearlessOfDeathPower>(); }
    }

    public FearlessOfDeath() : base(2, CardType.Power, CardRarity.Uncommon, TargetType.Self) { }

    protected override void OnUpgrade()
    {
        DynamicVars["ReviveHp"].UpgradeValueBy(19);  // 1 → 20
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await PlayCast();
        await Sts2Compat.PowerApply<FearlessOfDeathPower>(ctx, Owner.Creature,
            DynamicVars["ReviveHp"].BaseValue, Owner.Creature, this, false);
        if (Forms.IsMortisForm(Owner)) await CombatCounters.BumpMortisCard(ctx, Owner);
        else await CombatCounters.BumpMutsumiCard(ctx, Owner);
    }

    public override List<(string, string)>? Localization => LocManager.Instance.Language switch
    {
        "zhs" => new CardLoc("无惧死亡",
            "本场战斗中，下次死亡时以{ReviveHp:diff()}点生命复活。"),
        _ => new CardLoc("Fearless of Death",
            "On your next death this combat, revive with {ReviveHp:diff()} HP."),
    };
}
