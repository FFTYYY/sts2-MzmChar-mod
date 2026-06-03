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
using MegaCrit.Sts2.Core.ValueProps;

namespace MzmChar.Game;

/// <summary>
/// 拨弦：1 费白色攻击。获得 1 演艺热情。
///   小墨：造成 8/12 点伤害
///   小睦：获得 4/6 点活力
/// </summary>
[Pool(typeof(MzmCharCardPool))]
public class Pluck : MzmCharBaseCard
{
    public override string PortraitPath => "res://MzmChar/cards/pluck.png";

    private readonly List<DynamicVar> _vars = new()
    {
        new DamageVar(8, ValueProp.Move),
        new PowerVar<VigorPower>(4),
    };
    protected override IEnumerable<DynamicVar> CanonicalVars => _vars;

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromPower<PerformancePassionPower>();
            yield return HoverTipFactory.FromPower<VigorPower>();
        }
    }

    public Pluck() : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy) { }

    // Mu 现在自给活力，不需要目标
    public override TargetType TargetType =>
        !IsCanonical && Owner != null && Forms.IsMutsumiForm(Owner)
            ? TargetType.Self
            : TargetType.AnyEnemy;

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(4);           // 8 → 12
        DynamicVars["VigorPower"].UpgradeValueBy(2);    // 4 → 6
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await Sts2Compat.PowerApply<PerformancePassionPower>(ctx, Owner.Creature, 1, Owner.Creature, this, false);
        if (Forms.IsMortisForm(Owner))
        {
            if (play.Target != null)
            {
                await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                    .FromCard(this).Targeting(play.Target).Execute(ctx);
            }
        }
        else
        {
            await PlayCast();
            await Sts2Compat.PowerApply<VigorPower>(ctx, Owner.Creature,
                DynamicVars["VigorPower"].BaseValue, Owner.Creature, this, false);
        }
    }

    public override List<(string, string)>? Localization => LocManager.Instance.Language switch
    {
        "zhs" => new CardLoc("拨弦",
            "获得1点[gold]演艺热情[/gold]。\n" +
            "{MuSec}{MuOpen}小睦{MuClose}：获得{VigorPower:diff()}点[gold]活力[/gold]。{MuSecEnd}\n" +
            "{MoSec}{MoOpen}小墨{MoClose}：造成{Damage:diff()}点伤害。{MoSecEnd}"),
        _ => new CardLoc("Pluck",
            "Gain 1 [gold]Performance Passion[/gold].\n" +
            "{MuSec}{MuOpen}Mu{MuClose}: Gain {VigorPower:diff()} [gold]Vigor[/gold].{MuSecEnd}\n" +
            "{MoSec}{MoOpen}Mo{MoClose}: Deal {Damage:diff()} damage.{MoSecEnd}"),
    };
}
