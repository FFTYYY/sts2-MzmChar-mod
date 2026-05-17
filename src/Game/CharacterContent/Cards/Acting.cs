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
/// 演技：1 费白色技能。抽 2/3 张牌，获得 1 层「转换人格」（升级后还本回合获得 2 力量 + 2 敏捷）。
///   小睦：进入小墨。
///   小墨：进入小睦。
/// </summary>
[Pool(typeof(MzmCharCardPool))]
public class Acting : MzmCharBaseCard
{
    public override string PortraitPath => "res://MzmChar/cards/acting.png";

    private readonly List<DynamicVar> _vars = new()
    {
        new CardsVar(2),
        new DynamicVar("TempStr", 0m),
        new DynamicVar("TempDex", 0m),
    };
    protected override IEnumerable<DynamicVar> CanonicalVars => _vars;

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            foreach (var t in FormTooltips.BothEnter()) yield return t;
            yield return HoverTipFactory.FromPower<StrengthPower>();
            yield return HoverTipFactory.FromPower<DexterityPower>();
            yield return HoverTipFactory.FromPower<TransformPersonaPower>();
        }
    }

    public Acting() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self) { }

    protected override void OnUpgrade()
    {
        DynamicVars.Cards.UpgradeValueBy(1);            // 抽牌 2 → 3
        DynamicVars["TempStr"].UpgradeValueBy(2);       // 0 → 2
        DynamicVars["TempDex"].UpgradeValueBy(2);       // 0 → 2
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await CardPileCmd.Draw(ctx, DynamicVars.Cards.BaseValue, Owner, false);

        var str = DynamicVars["TempStr"].BaseValue;
        var dex = DynamicVars["TempDex"].BaseValue;
        if (str > 0)
        {
            await PowerCmd.Apply<StrengthPower>(Owner.Creature, str, Owner.Creature, this, false);
            await PowerCmd.Apply<TempStrengthPower>(Owner.Creature, str, Owner.Creature, this, true);
        }
        if (dex > 0)
        {
            await PowerCmd.Apply<DexterityPower>(Owner.Creature, dex, Owner.Creature, this, false);
            await PowerCmd.Apply<TempDexterityPower>(Owner.Creature, dex, Owner.Creature, this, true);
        }

        await PowerCmd.Apply<TransformPersonaPower>(Owner.Creature, 1, Owner.Creature, this, false);

        if (Forms.IsMutsumiForm(Owner))
        {
            await CombatCounters.BumpMutsumiCard(ctx, Owner);
            await Forms.EnterMortis(Owner, this, ctx);
        }
        else
        {
            await CombatCounters.BumpMortisCard(ctx, Owner);
            await Forms.EnterMutsumi(Owner, this, ctx);
        }
    }

    public override List<(string, string)>? Localization => LocManager.Instance.Language switch
    {
        "zhs" => new CardLoc("演技",
            "抽{Cards:diff()}张牌。获得1层[gold]转换人格[/gold]。{IfUpgraded:show:本回合获得{TempStr}点[gold]力量[/gold]和{TempDex}点[gold]敏捷[/gold]。|}\n" +
            "{MuSec}{MuOpen}小睦{MuClose}：[gold]进入小墨[/gold]。{MuSecEnd}\n" +
            "{MoSec}{MoOpen}小墨{MoClose}：[gold]进入小睦[/gold]。{MoSecEnd}"),
        _ => new CardLoc("Acting",
            "Draw {Cards:diff()}. Gain 1 [gold]Transform Persona[/gold].{IfUpgraded:show: This turn gain {TempStr} [gold]Strength[/gold] and {TempDex} [gold]Dexterity[/gold].|}\n" +
            "{MuSec}{MuOpen}Mu{MuClose}: [gold]Enter Mo[/gold].{MuSecEnd}\n" +
            "{MoSec}{MoOpen}Mo{MoClose}: [gold]Enter Mu[/gold].{MoSecEnd}"),
    };
}
