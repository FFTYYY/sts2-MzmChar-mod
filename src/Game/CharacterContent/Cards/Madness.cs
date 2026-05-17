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
/// 疯癫：2 费蓝色技能。
///   小睦：额外获得 2 点能量，进入小墨
///   小墨：抽 3/4 张牌，进入小睦
/// </summary>
[Pool(typeof(MzmCharCardPool))]
public class Madness : MzmCharBaseCard
{
    public override string PortraitPath => "res://MzmChar/cards/madness.png";

    private readonly List<DynamicVar> _vars = new()
    {
        new CardsVar(3),     // Mo 抽 3/4
        new EnergyVar(2),    // Mu 获得 2 能量（不升级）
    };
    protected override IEnumerable<DynamicVar> CanonicalVars => _vars;

    // 双形态都「进入X」 → 双挂 form tip
    protected override IEnumerable<IHoverTip> ExtraHoverTips => FormTooltips.BothEnter();

    public Madness() : base(2, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }

    protected override void OnUpgrade()
    {
        DynamicVars.Cards.UpgradeValueBy(1);   // 3 → 4
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        if (Forms.IsMortisForm(Owner))
        {
            await CardPileCmd.Draw(ctx, DynamicVars.Cards.BaseValue, Owner, false);
            await CombatCounters.BumpMortisCard(ctx, Owner);
            await Forms.EnterMutsumi(Owner, this, ctx);
        }
        else
        {
            await PlayerCmd.GainEnergy(DynamicVars.Energy.IntValue, Owner);
            await CombatCounters.BumpMutsumiCard(ctx, Owner);
            await Forms.EnterMortis(Owner, this, ctx);
        }
    }

    public override List<(string, string)>? Localization => LocManager.Instance.Language switch
    {
        "zhs" => new CardLoc("疯癫",
            "{MuSec}{MuOpen}小睦{MuClose}：额外获得{Energy:energyIcons()}。[gold]进入小墨[/gold]。{MuSecEnd}\n" +
            "{MoSec}{MoOpen}小墨{MoClose}：抽{Cards:diff()}张牌。[gold]进入小睦[/gold]。{MoSecEnd}"),
        _ => new CardLoc("Madness",
            "{MuSec}{MuOpen}Mu{MuClose}: Gain an additional {Energy:energyIcons()}; [gold]Enter Mo[/gold].{MuSecEnd}\n" +
            "{MoSec}{MoOpen}Mo{MoClose}: Draw {Cards:diff()}; [gold]Enter Mu[/gold].{MoSecEnd}"),
    };
}
