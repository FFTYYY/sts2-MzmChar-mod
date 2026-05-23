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
using MegaCrit.Sts2.Core.Models.Powers;

namespace MzmChar.Game;

/// <summary>
/// 燃烧：1 费蓝色能力。+2/+3 力量（直接，不产生独立 power）。
/// </summary>
[Pool(typeof(MzmCharCardPool))]
public class MuBurn : MzmCharBaseCard
{
    public override string PortraitPath => "res://MzmChar/cards/mzmchar_burn.png";

    private readonly List<DynamicVar> _vars = new()
    {
        new PowerVar<StrengthPower>(2),
    };
    protected override IEnumerable<DynamicVar> CanonicalVars => _vars;

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get { yield return HoverTipFactory.FromPower<StrengthPower>(); }
    }

    public MuBurn() : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self) { }

    protected override void OnUpgrade() { DynamicVars["StrengthPower"].UpgradeValueBy(1); /* 2→3 */ }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await PlayCast();
        await Sts2Compat.PowerApply<StrengthPower>(ctx, Owner.Creature, DynamicVars["StrengthPower"].BaseValue, Owner.Creature, this, false);
    }

    public override List<(string, string)>? Localization => LocManager.Instance.Language switch
    {
        "zhs" => new CardLoc("燃烧",
            "获得{StrengthPower:diff()}点[gold]力量[/gold]。"),
        _ => new CardLoc("Burn",
            "Gain {StrengthPower:diff()} [gold]Strength[/gold]."),
    };
}
