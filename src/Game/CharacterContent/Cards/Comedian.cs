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
/// 搞笑艺人：3/2 费金色能力卡。每切换 2 次人格，下回合获得 1 费。
/// </summary>
[Pool(typeof(MzmCharCardPool))]
public class Comedian : MzmCharBaseCard
{
    public override string PortraitPath => "res://MzmChar/cards/comedian.png";

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get { yield return HoverTipFactory.FromPower<ComedianPower>(); }
    }

    public Comedian() : base(3, CardType.Power, CardRarity.Rare, TargetType.Self) { }

    protected override void OnUpgrade() { EnergyCost.UpgradeBy(-1); }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await PlayCast();
        await Sts2Compat.PowerApply<ComedianPower>(ctx, Owner.Creature, ComedianPower.InitialAmount, Owner.Creature, this, false);
    }

    public override List<(string, string)>? Localization => LocManager.Instance.Language switch
    {
        "zhs" => new CardLoc("搞笑艺人",
            "每切换2次人格，下回合开始时获得{energyPrefix:energyIcons(1)}。"),
        _ => new CardLoc("Comedian",
            "Every 2 persona switches, gain {energyPrefix:energyIcons(1)} at the start of next turn."),
    };
}
