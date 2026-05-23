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
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace MzmChar.Game;

/// <summary>
/// 收起真心：1 费蓝色技能。下回合开始时获得 2/3 费。
///   小墨：进入小睦
///   小睦：获得 5/7 格挡
/// </summary>
[Pool(typeof(MzmCharCardPool))]
public class HideHeart : MzmCharBaseCard
{
    public override string PortraitPath => "res://MzmChar/cards/hide_heart.png";

    private readonly List<DynamicVar> _vars = new()
    {
        new EnergyVar(2),
        new BlockVar(5, ValueProp.Move),
    };
    protected override IEnumerable<DynamicVar> CanonicalVars => _vars;

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            foreach (var t in FormTooltips.EnterMu()) yield return t;
            yield return HoverTipFactory.FromPower<EnergyNextTurnPower>();
        }
    }

    public HideHeart() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }

    protected override void OnUpgrade()
    {
        DynamicVars.Energy.UpgradeValueBy(1);   // 2 → 3
        DynamicVars.Block.UpgradeValueBy(2);    // 5 → 7
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await Sts2Compat.PowerApply<EnergyNextTurnPower>(ctx, Owner.Creature, DynamicVars.Energy.BaseValue, Owner.Creature, this, false);
        if (Forms.IsMortisForm(Owner))
        {
            await Forms.EnterMutsumi(Owner, this, ctx);
        }
        else
        {
            await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block.BaseValue, ValueProp.Move, play, false);
        }
    }

    public override List<(string, string)>? Localization => LocManager.Instance.Language switch
    {
        "zhs" => new CardLoc("收起真心",
            "下回合开始时，获得{Energy:energyIcons()}。\n" +
            "{MuSec}{MuOpen}小睦{MuClose}：获得{Block:diff()}点[gold]格挡[/gold]。{MuSecEnd}\n" +
            "{MoSec}{MoOpen}小墨{MoClose}：[gold]进入小睦[/gold]。{MoSecEnd}"),
        _ => new CardLoc("Hide the Heart",
            "Next turn, gain {Energy:energyIcons()}.\n" +
            "{MuSec}{MuOpen}Mu{MuClose}: Gain {Block:diff()} [gold]Block[/gold].{MuSecEnd}\n" +
            "{MoSec}{MoOpen}Mo{MoClose}: [gold]Enter Mu[/gold].{MoSecEnd}"),
    };
}
