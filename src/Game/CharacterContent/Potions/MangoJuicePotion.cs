using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace MzmChar.Game;

/// <summary>
/// 芒果汁（罕见）：获得 12 点活力。
/// </summary>
[Pool(typeof(MzmCharPotionPool))]
public class MangoJuicePotion : CustomPotionModel
{
    public override PotionRarity Rarity => PotionRarity.Uncommon;
    public override PotionUsage Usage => PotionUsage.CombatOnly;
    public override TargetType TargetType => TargetType.Self;

    public override string? CustomPackedImagePath   => "res://MzmChar/potions/mango_juice.png";
    public override string? CustomPackedOutlinePath => "res://MzmChar/potions/mango_juice.png";

    public override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get { yield return HoverTipFactory.FromPower<VigorPower>(); }
    }

    protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature? target)
    {
        if (Owner == null) return;
        var c = Owner.Creature;
        await Sts2Compat.PowerApply<VigorPower>(choiceContext, c, 12, c, null, false);
    }

    public override List<(string, string)>? Localization => LocManager.Instance.Language switch
    {
        "zhs" => new PotionLoc(
            Title:       "芒果汁",
            Description: "获得12点[gold]活力[/gold]。"),
        _ => new PotionLoc(
            Title:       "Mango Juice",
            Description: "Gain 12 [gold]Vigor[/gold]."),
    };
}
