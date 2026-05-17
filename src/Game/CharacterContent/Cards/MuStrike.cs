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
/// 打击：1 费基础攻击。
///   小睦：获得 4/5 点活力 + 本回合 1/2 力量
///   小墨：造成 7/10 伤害
/// </summary>
[Pool(typeof(MzmCharCardPool))]
public class MuStrike : MzmCharBaseCard
{
    public override string PortraitPath => "res://MzmChar/cards/strike.png";

    // 初始卡，不应被印牌/变化牌随机产生
    public override bool CanBeGeneratedInCombat => false;
    public override bool CanBeGeneratedByModifiers => false;

    private readonly List<DynamicVar> _vars = new()
    {
        new DamageVar(7, ValueProp.Move),
        new PowerVar<VigorPower>(4),
        new DynamicVar("TempStr", 1m),
    };
    protected override IEnumerable<DynamicVar> CanonicalVars => _vars;

    private readonly HashSet<CardTag> _tags = new() { CardTag.Strike };
    protected override HashSet<CardTag> CanonicalTags => _tags;

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromPower<VigorPower>();
            yield return HoverTipFactory.FromPower<StrengthPower>();
        }
    }

    public MuStrike() : base(1, CardType.Attack, CardRarity.Basic, TargetType.AnyEnemy) { }

    // Mu 只给自己加活力+力量 → 不需要选目标
    public override TargetType TargetType =>
        !IsCanonical && Owner != null && Forms.IsMutsumiForm(Owner)
            ? TargetType.Self
            : TargetType.AnyEnemy;

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3);              // 7 → 10
        DynamicVars["VigorPower"].UpgradeValueBy(1);       // 4 → 5
        DynamicVars["TempStr"].UpgradeValueBy(1);          // 1 → 2
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        if (Forms.IsMortisForm(Owner))
        {
            if (play.Target == null) return;
            await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                .FromCard(this).Targeting(play.Target).Execute(ctx);
            CombatCounters.StruckByMortisThisTurn[play.Target]++;
            await CombatCounters.BumpMortisCard(ctx, Owner);
        }
        else
        {
            await PlayCast();
            await Sts2Compat.PowerApply<VigorPower>(ctx, Owner.Creature,
                DynamicVars["VigorPower"].BaseValue, Owner.Creature, this, false);
            var str = DynamicVars["TempStr"].BaseValue;
            await Sts2Compat.PowerApply<StrengthPower>(ctx, Owner.Creature, str, Owner.Creature, this, false);
            await Sts2Compat.PowerApply<TempStrengthPower>(ctx, Owner.Creature, str, Owner.Creature, this, true);
            await CombatCounters.BumpMutsumiCard(ctx, Owner);
        }
    }

    public override List<(string, string)>? Localization => LocManager.Instance.Language switch
    {
        "zhs" => new CardLoc("打击",
            "{MuSec}{MuOpen}小睦{MuClose}：获得{VigorPower:diff()}点[gold]活力[/gold]。本回合获得{TempStr:diff()}点[gold]力量[/gold]。{MuSecEnd}\n" +
            "{MoSec}{MoOpen}小墨{MoClose}：造成{Damage:diff()}点伤害。{MoSecEnd}"),
        _ => new CardLoc("Strike",
            "{MuSec}{MuOpen}Mu{MuClose}: Gain {VigorPower:diff()} [gold]Vigor[/gold]; this turn gain {TempStr:diff()} [gold]Strength[/gold].{MuSecEnd}\n" +
            "{MoSec}{MoOpen}Mo{MoClose}: Deal {Damage:diff()} damage.{MoSecEnd}"),
    };
}
