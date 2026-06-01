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
/// 镜子药水（普通）：本回合获得 4 力量 + 4 敏捷，并镜像切换形态（Mu↔Mo）。
/// 力量/敏捷的「本回合」语义 = Apply Str + TempStr 组合（vanilla 不存在 "TempStr only"，TempStr 在回合结束时
/// 等量扣 Str），参考 Acting.cs。
/// </summary>
[Pool(typeof(MzmCharPotionPool))]
public class MirrorPotion : CustomPotionModel
{
    public override PotionRarity Rarity => PotionRarity.Common;
    public override PotionUsage Usage => PotionUsage.CombatOnly;
    public override TargetType TargetType => TargetType.Self;

    public override string? CustomPackedImagePath   => "res://MzmChar/potions/mirror_potion.png";
    public override string? CustomPackedOutlinePath => "res://MzmChar/potions/mirror_potion.png";

    public override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromPower<StrengthPower>();
            yield return HoverTipFactory.FromPower<DexterityPower>();
            foreach (var t in FormTooltips.BothEnter()) yield return t;
        }
    }

    protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature? target)
    {
        if (Owner == null) return;
        var c = Owner.Creature;

        await Sts2Compat.PowerApply<StrengthPower>(choiceContext, c, 4, c, null, false);
        await Sts2Compat.PowerApply<TempStrengthPower>(choiceContext, c, 4, c, null, true);
        await Sts2Compat.PowerApply<DexterityPower>(choiceContext, c, 4, c, null, false);
        await Sts2Compat.PowerApply<TempDexterityPower>(choiceContext, c, 4, c, null, true);

        if (Forms.IsMutsumiForm(Owner))
            await Forms.EnterMortis(Owner, null, choiceContext);
        else
            await Forms.EnterMutsumi(Owner, null, choiceContext);
    }

    public override List<(string, string)>? Localization => LocManager.Instance.Language switch
    {
        "zhs" => new PotionLoc(
            Title:       "镜子药水",
            Description: "本回合获得4点[gold]力量[/gold]和4点[gold]敏捷[/gold]。小睦：[gold]进入小墨[/gold]；小墨：[gold]进入小睦[/gold]。"),
        _ => new PotionLoc(
            Title:       "Mirror Potion",
            Description: "Gain 4 [gold]Strength[/gold] and 4 [gold]Dexterity[/gold] this turn. If in Mu form: [gold]Enter Mo[/gold]; if in Mo form: [gold]Enter Mu[/gold]."),
    };
}
