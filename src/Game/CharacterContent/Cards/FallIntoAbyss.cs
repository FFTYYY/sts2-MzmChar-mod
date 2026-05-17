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
/// 坠入深渊：0 费金色能力。抽 2/3 张，获得 2 费，获得 12/20 活力。本战斗无法再进入小睦。
/// </summary>
[Pool(typeof(MzmCharCardPool))]
public class FallIntoAbyss : MzmCharBaseCard
{
    public override string PortraitPath => "res://MzmChar/cards/fall_into_abyss.png";

    private readonly List<DynamicVar> _vars = new()
    {
        new CardsVar(2),
        new EnergyVar(2),
        new PowerVar<VigorPower>(12),
    };
    protected override IEnumerable<DynamicVar> CanonicalVars => _vars;

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            foreach (var t in FormTooltips.EnterMu()) yield return t;
            yield return HoverTipFactory.FromPower<VigorPower>();
            yield return HoverTipFactory.FromPower<FallIntoAbyssPower>();
        }
    }

    public FallIntoAbyss() : base(0, CardType.Power, CardRarity.Rare, TargetType.Self) { }

    protected override void OnUpgrade()
    {
        DynamicVars.Cards.UpgradeValueBy(1);                  // 2 → 3
        DynamicVars["VigorPower"].UpgradeValueBy(8);          // 12 → 20
        // 能量保持 2 不变（不升级）
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await PlayCast();
        await CardPileCmd.Draw(ctx, DynamicVars.Cards.BaseValue, Owner, false);
        await PlayerCmd.GainEnergy(DynamicVars.Energy.IntValue, Owner);
        await Sts2Compat.PowerApply<VigorPower>(ctx, Owner.Creature,
            DynamicVars["VigorPower"].BaseValue, Owner.Creature, this, false);
        await Sts2Compat.PowerApply<FallIntoAbyssPower>(ctx, Owner.Creature, 1, Owner.Creature, this, false);
        if (Forms.IsMortisForm(Owner)) await CombatCounters.BumpMortisCard(ctx, Owner);
        else await CombatCounters.BumpMutsumiCard(ctx, Owner);
    }

    public override List<(string, string)>? Localization => LocManager.Instance.Language switch
    {
        "zhs" => new CardLoc("坠入深渊",
            "抽{Cards:diff()}张牌。获得{Energy:energyIcons()}。获得{VigorPower:diff()}点[gold]活力[/gold]。本场战斗无法再[gold]进入小睦[/gold]。"),
        _ => new CardLoc("Fall Into Abyss",
            "Draw {Cards:diff()}; gain {Energy:energyIcons()}; gain {VigorPower:diff()} [gold]Vigor[/gold]. This combat you cannot enter [gold]Mu[/gold]."),
    };
}
