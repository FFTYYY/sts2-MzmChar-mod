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
/// 拨弦：1 费白色攻击。获得演艺热情。
///   小墨：造成 8/10 点伤害
///   小睦：施加 2/3 层易伤 + 本回合获得 2 点力量
/// </summary>
[Pool(typeof(MzmCharCardPool))]
public class Pluck : MzmCharBaseCard
{
    public override string PortraitPath => "res://MzmChar/cards/pluck.png";

    private readonly List<DynamicVar> _vars = new()
    {
        new DamageVar(8, ValueProp.Move),
        new PowerVar<VulnerablePower>(2),
        new DynamicVar("MuStr", 2m),
    };
    protected override IEnumerable<DynamicVar> CanonicalVars => _vars;

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromPower<PerformancePassionPower>();
            yield return HoverTipFactory.FromPower<VulnerablePower>();
            yield return HoverTipFactory.FromPower<StrengthPower>();
        }
    }

    public Pluck() : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy) { }

    public override TargetType TargetType =>
        !IsCanonical && Owner != null && Forms.IsMutsumiForm(Owner)
            ? TargetType.AnyEnemy   // 小睦施加易伤，还是要选目标
            : TargetType.AnyEnemy;

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(2);           // 8 → 10
        DynamicVars["VulnerablePower"].UpgradeValueBy(1); // 2 → 3
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await PowerCmd.Apply<PerformancePassionPower>(Owner.Creature, 1, Owner.Creature, this, false);
        if (Forms.IsMortisForm(Owner))
        {
            if (play.Target != null)
            {
                await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                    .FromCard(this).Targeting(play.Target).Execute(ctx);
                CombatCounters.StruckByMortisThisTurn[play.Target]++;
            }
            await CombatCounters.BumpMortisCard(ctx, Owner);
        }
        else
        {
            await PlayCast();
            if (play.Target != null)
                await PowerCmd.Apply<VulnerablePower>(play.Target, DynamicVars["VulnerablePower"].BaseValue, Owner.Creature, this, false);
            var str = DynamicVars["MuStr"].BaseValue;
            await PowerCmd.Apply<StrengthPower>(Owner.Creature, str, Owner.Creature, this, false);
            await PowerCmd.Apply<TempStrengthPower>(Owner.Creature, str, Owner.Creature, this, true);
            await CombatCounters.BumpMutsumiCard(ctx, Owner);
        }
    }

    public override List<(string, string)>? Localization => LocManager.Instance.Language switch
    {
        "zhs" => new CardLoc("拨弦",
            "获得1点[gold]演艺热情[/gold]。\n" +
            "{MuSec}{MuOpen}小睦{MuClose}：施加{VulnerablePower:diff()}层[gold]易伤[/gold]。本回合获得{MuStr}点[gold]力量[/gold]。{MuSecEnd}\n" +
            "{MoSec}{MoOpen}小墨{MoClose}：造成{Damage:diff()}点伤害。{MoSecEnd}"),
        _ => new CardLoc("Pluck",
            "Gain 1 [gold]Performance Passion[/gold].\n" +
            "{MuSec}{MuOpen}Mu{MuClose}: Apply {VulnerablePower:diff()} [gold]Vulnerable[/gold]; this turn gain {MuStr} [gold]Strength[/gold].{MuSecEnd}\n" +
            "{MoSec}{MoOpen}Mo{MoClose}: Deal {Damage:diff()} damage.{MoSecEnd}"),
    };
}
