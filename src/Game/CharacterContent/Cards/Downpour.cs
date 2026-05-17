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
/// 暴雨：1 费白色攻击。
///   小墨：造成 10/13 点伤害
///   小睦：获得 4/6 层活力 + 2/3 临时力量
/// </summary>
[Pool(typeof(MzmCharCardPool))]
public class Downpour : MzmCharBaseCard
{
    public override string PortraitPath => "res://MzmChar/cards/downpour.png";

    private readonly List<DynamicVar> _vars = new()
    {
        new DamageVar(10, ValueProp.Move),
        new PowerVar<VigorPower>(4),
        new DynamicVar("TempStr", 2m),
    };
    protected override IEnumerable<DynamicVar> CanonicalVars => _vars;

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromPower<VigorPower>();
            yield return HoverTipFactory.FromPower<StrengthPower>();
        }
    }

    public Downpour() : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy) { }

    // 小睦不要目标
    public override TargetType TargetType =>
        !IsCanonical && Owner != null && Forms.IsMutsumiForm(Owner)
            ? TargetType.Self
            : TargetType.AnyEnemy;

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3);                // 10 → 13
        DynamicVars["VigorPower"].UpgradeValueBy(2);          // 4 → 6
        DynamicVars["TempStr"].UpgradeValueBy(1);             // 2 → 3
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
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
            await PowerCmd.Apply<VigorPower>(Owner.Creature, DynamicVars["VigorPower"].BaseValue, Owner.Creature, this, false);
            var str = DynamicVars["TempStr"].BaseValue;
            await PowerCmd.Apply<StrengthPower>(Owner.Creature, str, Owner.Creature, this, false);
            await PowerCmd.Apply<TempStrengthPower>(Owner.Creature, str, Owner.Creature, this, true);
            await CombatCounters.BumpMutsumiCard(ctx, Owner);
        }
    }

    public override List<(string, string)>? Localization => LocManager.Instance.Language switch
    {
        "zhs" => new CardLoc("暴雨",
            "{MuSec}{MuOpen}小睦{MuClose}：获得{VigorPower:diff()}层[gold]活力[/gold]。本回合获得{TempStr:diff()}点[gold]力量[/gold]。{MuSecEnd}\n" +
            "{MoSec}{MoOpen}小墨{MoClose}：造成{Damage:diff()}点伤害。{MoSecEnd}"),
        _ => new CardLoc("Downpour",
            "{MuSec}{MuOpen}Mu{MuClose}: Gain {VigorPower:diff()} [gold]Vigor[/gold]. This turn gain {TempStr:diff()} [gold]Strength[/gold].{MuSecEnd}\n" +
            "{MoSec}{MoOpen}Mo{MoClose}: Deal {Damage:diff()} damage.{MoSecEnd}"),
    };
}
