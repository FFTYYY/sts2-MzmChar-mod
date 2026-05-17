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
/// 月之森制服：1 费蓝色能力。如果你以小睦开始回合，获得 1/2 点临时敏捷。
/// </summary>
[Pool(typeof(MzmCharCardPool))]
public class MoonForestUniform : MzmCharBaseCard
{
    public override string PortraitPath => "res://MzmChar/cards/moon_forest.png";

    private readonly List<DynamicVar> _vars = new()
    {
        new PowerVar<MoonForestPower>(1),
    };
    protected override IEnumerable<DynamicVar> CanonicalVars => _vars;

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get { yield return HoverTipFactory.FromPower<MoonForestPower>(); }
    }

    public MoonForestUniform() : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self) { }

    protected override void OnUpgrade() { DynamicVars["MoonForestPower"].UpgradeValueBy(1); /* 1→2 */ }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await PlayCast();
        await PowerCmd.Apply<MoonForestPower>(Owner.Creature, DynamicVars["MoonForestPower"].BaseValue, Owner.Creature, this, false);
        if (Forms.IsMortisForm(Owner)) await CombatCounters.BumpMortisCard(ctx, Owner);
        else await CombatCounters.BumpMutsumiCard(ctx, Owner);
    }

    public override List<(string, string)>? Localization => LocManager.Instance.Language switch
    {
        "zhs" => new CardLoc("月之森制服",
            "以[gold]小睦[/gold]开始回合时，本回合获得{MoonForestPower:diff()}点[gold]敏捷[/gold]。"),
        _ => new CardLoc("Moon Forest Uniform",
            "If you start your turn as [gold]Mu[/gold], gain {MoonForestPower:diff()} [gold]Dexterity[/gold] this turn."),
    };
}
