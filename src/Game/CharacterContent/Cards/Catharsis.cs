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
///   小墨：造成 7/9 点伤害 2 次
///   小睦：本回合获得 6 点力量（升级后额外获得 4 点活力）
/// </summary>
[Pool(typeof(MzmCharCardPool))]
public class Catharsis : MzmCharBaseCard
{
    public override string PortraitPath => "res://MzmChar/cards/catharsis.png";

    private readonly List<DynamicVar> _vars = new()
    {
        new DamageVar(7, ValueProp.Move),
        new DynamicVar("Hits", 2m),
        new DynamicVar("TempStr", 6m),         // Mu: 本回合 6 力量（无升级）
        new PowerVar<VigorPower>(0),            // Mu: 升级后 +4 活力
    };
    protected override IEnumerable<DynamicVar> CanonicalVars => _vars;

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromPower<StrengthPower>();
            yield return HoverTipFactory.FromPower<VigorPower>();
        }
    }

    public Catharsis() : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy) { }

    public override TargetType TargetType =>
        !IsCanonical && Owner != null && Forms.IsMutsumiForm(Owner)
            ? TargetType.Self
            : TargetType.AnyEnemy;

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(2);              // 7 → 9
        DynamicVars["VigorPower"].UpgradeValueBy(4);       // 0 → 4
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
            }
        }
        else
        {
            await PlayCast();
            var str = DynamicVars["TempStr"].BaseValue;
            await Sts2Compat.PowerApply<StrengthPower>(ctx, Owner.Creature, str, Owner.Creature, this, false);
            await Sts2Compat.PowerApply<TempStrengthPower>(ctx, Owner.Creature, str, Owner.Creature, this, true);

            var vigor = DynamicVars["VigorPower"].BaseValue;
            if (vigor > 0)
                await Sts2Compat.PowerApply<VigorPower>(ctx, Owner.Creature, vigor, Owner.Creature, this, false);
        }
    }

    public override List<(string, string)>? Localization => LocManager.Instance.Language switch
    {
        "zhs" => new CardLoc("宣泄",
            "{MuSec}{MuOpen}小睦{MuClose}：本回合获得{TempStr}点[gold]力量[/gold]。{IfUpgraded:show:获得{VigorPower}点[gold]活力[/gold]。|}{MuSecEnd}\n" +
            "{MoSec}{MoOpen}小墨{MoClose}：造成{Damage:diff()}点伤害{Hits}次。{MoSecEnd}"),
        _ => new CardLoc("Catharsis",
            "{MuSec}{MuOpen}Mu{MuClose}: This turn gain {TempStr} [gold]Strength[/gold].{IfUpgraded:show: Gain {VigorPower} [gold]Vigor[/gold].|}{MuSecEnd}\n" +
            "{MoSec}{MoOpen}Mo{MoClose}: Deal {Damage:diff()} damage {Hits} times.{MoSecEnd}"),
    };
}
