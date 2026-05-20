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
/// 独白：1 费蓝色攻击。
///   小墨：对全体敌人造成 20/25 伤害
///   小睦：获得 7/9 临时力量
/// 演奏 / 虚无 / 消耗。
/// </summary>
[Pool(typeof(MzmCharCardPool))]
public class MuMonologue : MzmCharBaseCard
{
    public override string PortraitPath => "res://MzmChar/cards/mzmchar_monologue.png";

    private readonly List<DynamicVar> _vars = new()
    {
        new DamageVar(20, ValueProp.Move),
        new DynamicVar("TempStr", 7m),
    };
    protected override IEnumerable<DynamicVar> CanonicalVars => _vars;

    private readonly HashSet<CardKeyword> _keywords = new()
    {
        CardKeyword.Ethereal, CardKeyword.Exhaust, MzmCharKeywords.Perform,
    };
    public override IEnumerable<CardKeyword> CanonicalKeywords => _keywords;

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromPower<PerformancePassionPower>();
            yield return HoverTipFactory.FromPower<ConcertPower>();
            yield return HoverTipFactory.FromPower<StrengthPower>();
        }
    }

    public MuMonologue() : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AllEnemies) { }

    // 4 状态：
    //   非演奏会 → 演艺热情 (Self)
    //   演奏会 Mu → 临时力量 (Self)
    //   演奏会 Mo → AOE 伤害 (AllEnemies)
    public override TargetType TargetType =>
        IsInConcert() && !IsCanonical && Owner != null && !Forms.IsMutsumiForm(Owner)
            ? TargetType.AllEnemies
            : TargetType.Self;

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(5);      // 20 → 25
        DynamicVars["TempStr"].UpgradeValueBy(2);  // 7 → 9
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        if (!IsInConcert())
        {
            await PlayCast();
            await Sts2Compat.PowerApply<PerformancePassionPower>(ctx, Owner.Creature, 1, Owner.Creature, this, false);
        }
        else if (Forms.IsMortisForm(Owner))
        {
            var cs = Owner.Creature.CombatState;
            if (cs != null && cs.HittableEnemies.Count > 0)
            {
                await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                    .FromCard(this).TargetingAllOpponents(cs).Execute(ctx);
                foreach (var e in cs.HittableEnemies)
                    CombatCounters.StruckByMortisThisTurn[e]++;
            }
        }
        else
        {
            await PlayCast();
            var str = DynamicVars["TempStr"].BaseValue;
            await Sts2Compat.PowerApply<StrengthPower>(ctx, Owner.Creature, str, Owner.Creature, this, false);
            await Sts2Compat.PowerApply<TempStrengthPower>(ctx, Owner.Creature, str, Owner.Creature, this, true);
        }
        if (Forms.IsMortisForm(Owner)) await CombatCounters.BumpMortisCard(ctx, Owner);
        else                            await CombatCounters.BumpMutsumiCard(ctx, Owner);
    }

    public override List<(string, string)>? Localization => LocManager.Instance.Language switch
    {
        "zhs" => new CardLoc("独白",
            "{ShowRealEffect:show:{MuSec}{MuOpen}小睦{MuClose}：本回合获得{TempStr:diff()}点[gold]力量[/gold]。{MuSecEnd}\n{MoSec}{MoOpen}小墨{MoClose}：对全体敌人造成{Damage:diff()}点伤害。{MoSecEnd}|非演奏会回合，获得1点[gold]演艺热情[/gold]。}"),
        _ => new CardLoc("Monologue",
            "{ShowRealEffect:show:{MuSec}{MuOpen}Mu{MuClose}: This turn gain {TempStr:diff()} [gold]Strength[/gold].{MuSecEnd}\n{MoSec}{MoOpen}Mo{MoClose}: Deal {Damage:diff()} damage to ALL enemies.{MoSecEnd}|Outside concert, gain 1 [gold]Performance Passion[/gold].}"),
    };
}
