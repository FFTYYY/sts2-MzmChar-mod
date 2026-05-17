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
/// 宣泄：1 费白色攻击。
///   小墨：造成 5/7 点伤害 2 次
///   小睦：获得 4/6 临时力量
/// </summary>
[Pool(typeof(MzmCharCardPool))]
public class Catharsis : MzmCharBaseCard
{
    public override string PortraitPath => "res://MzmChar/cards/catharsis.png";

    private readonly List<DynamicVar> _vars = new()
    {
        new DamageVar(5, ValueProp.Move),
        new DynamicVar("Hits", 2m),
        new DynamicVar("TempStr", 4m),
    };
    protected override IEnumerable<DynamicVar> CanonicalVars => _vars;

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get { yield return HoverTipFactory.FromPower<StrengthPower>(); }
    }

    public Catharsis() : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy) { }

    public override TargetType TargetType =>
        !IsCanonical && Owner != null && Forms.IsMutsumiForm(Owner)
            ? TargetType.Self
            : TargetType.AnyEnemy;

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(2);      // 3 → 5
        DynamicVars["TempStr"].UpgradeValueBy(2);  // 4 → 6
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        if (Forms.IsMortisForm(Owner))
        {
            int hits = (int)DynamicVars["Hits"].BaseValue;
            // 单 AttackCommand + WithHitCount → 力量/活力 modifier 算一次但应用每次 hit
            // 若 loop 多次单独 Execute，活力等"用一次消失"的 buff 会在第一次后耗尽
            if (play.Target != null)
            {
                await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                    .FromCard(this).Targeting(play.Target).WithHitCount(hits).Execute(ctx);
                CombatCounters.StruckByMortisThisTurn[play.Target] += hits;
            }
            await CombatCounters.BumpMortisCard(ctx, Owner);
        }
        else
        {
            await PlayCast();
            var str = DynamicVars["TempStr"].BaseValue;
            await Sts2Compat.PowerApply<StrengthPower>(ctx, Owner.Creature, str, Owner.Creature, this, false);
            await Sts2Compat.PowerApply<TempStrengthPower>(ctx, Owner.Creature, str, Owner.Creature, this, true);
            await CombatCounters.BumpMutsumiCard(ctx, Owner);
        }
    }

    public override List<(string, string)>? Localization => LocManager.Instance.Language switch
    {
        "zhs" => new CardLoc("宣泄",
            "{MuSec}{MuOpen}小睦{MuClose}：本回合获得{TempStr:diff()}点[gold]力量[/gold]。{MuSecEnd}\n" +
            "{MoSec}{MoOpen}小墨{MoClose}：造成{Damage:diff()}点伤害{Hits}次。{MoSecEnd}"),
        _ => new CardLoc("Catharsis",
            "{MuSec}{MuOpen}Mu{MuClose}: This turn gain {TempStr:diff()} [gold]Strength[/gold].{MuSecEnd}\n" +
            "{MoSec}{MoOpen}Mo{MoClose}: Deal {Damage:diff()} damage {Hits} times.{MoSecEnd}"),
    };
}
