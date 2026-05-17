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
/// 下跪：1 费蓝色技能。
///   小睦：1/2 回合内受到的伤害减半。
///   小墨：获得 4/6 层[gold]覆甲[/gold]。[gold]进入小睦[/gold]。
/// </summary>
[Pool(typeof(MzmCharCardPool))]
public class Kneel : MzmCharBaseCard
{
    public override string PortraitPath => "res://MzmChar/cards/kneel.png";

    private readonly List<DynamicVar> _vars = new()
    {
        new PowerVar<HalfDamagePower>(1),
        new PowerVar<PlatingPower>(4),
    };
    protected override IEnumerable<DynamicVar> CanonicalVars => _vars;

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            foreach (var t in FormTooltips.EnterMu()) yield return t;
            yield return HoverTipFactory.FromPower<HalfDamagePower>();
            yield return HoverTipFactory.FromPower<PlatingPower>();
        }
    }

    public Kneel() : base(2, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }

    protected override void OnUpgrade()
    {
        DynamicVars["HalfDamagePower"].UpgradeValueBy(1);  // 1 → 2
        DynamicVars["PlatingPower"].UpgradeValueBy(2);     // 4 → 6
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        if (Forms.IsMortisForm(Owner))
        {
            await PowerCmd.Apply<PlatingPower>(Owner.Creature,
                DynamicVars["PlatingPower"].BaseValue, Owner.Creature, this, false);
            await CombatCounters.BumpMortisCard(ctx, Owner);
            await Forms.EnterMutsumi(Owner, this, ctx);
        }
        else
        {
            await PowerCmd.Apply<HalfDamagePower>(Owner.Creature,
                DynamicVars["HalfDamagePower"].BaseValue, Owner.Creature, this, false);
            await CombatCounters.BumpMutsumiCard(ctx, Owner);
        }
    }

    public override List<(string, string)>? Localization => LocManager.Instance.Language switch
    {
        "zhs" => new CardLoc("下跪",
            "{MuSec}{MuOpen}小睦{MuClose}：{HalfDamagePower:diff()}回合内，受到的伤害减半。{MuSecEnd}\n" +
            "{MoSec}{MoOpen}小墨{MoClose}：获得{PlatingPower:diff()}层[gold]覆甲[/gold]。[gold]进入小睦[/gold]。{MoSecEnd}"),
        _ => new CardLoc("Kneel",
            "{MuSec}{MuOpen}Mu{MuClose}: For {HalfDamagePower:diff()} turns, incoming damage is halved.{MuSecEnd}\n" +
            "{MoSec}{MoOpen}Mo{MoClose}: Gain {PlatingPower:diff()} [gold]Plating[/gold]; [gold]Enter Mu[/gold].{MoSecEnd}"),
    };
}
