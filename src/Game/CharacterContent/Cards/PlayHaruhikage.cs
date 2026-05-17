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
/// 演奏《春日影》：1 费蓝色攻击。（原 Yearning「盼望」，user 让改回 "演奏《春日影》"）
///   小墨：对全体敌人造成 5/7 点伤害 3 次
///   小睦：获得 1 层「无实体」
/// 演奏 / 虚无 / 消耗。
/// </summary>
[Pool(typeof(MzmCharCardPool))]
public class PlayHaruhikage : MzmCharBaseCard
{
    public override string PortraitPath => "res://MzmChar/cards/play_haruhikage.png";

    private readonly List<DynamicVar> _vars = new()
    {
        new DamageVar(5, ValueProp.Move),
        new DynamicVar("Hits", 3m),
        new PowerVar<IntangiblePower>(1),
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
            yield return HoverTipFactory.FromPower<IntangiblePower>();
        }
    }

    public PlayHaruhikage() : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AllEnemies) { }

    // 4 状态：
    //   非演奏会 → 演艺热情 (Self)
    //   演奏会 Mu → 无实体 (Self)
    //   演奏会 Mo → AOE 伤害 (AllEnemies)
    public override TargetType TargetType =>
        IsInConcert() && !IsCanonical && Owner != null && !Forms.IsMutsumiForm(Owner)
            ? TargetType.AllEnemies
            : TargetType.Self;

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(2);   // 5 → 7
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
            int hits = (int)DynamicVars["Hits"].BaseValue;
            // 单 AttackCommand + WithHitCount —— 力量/活力 modifier 应用每次 hit
            if (cs != null && cs.HittableEnemies.Count > 0)
            {
                await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                    .FromCard(this).TargetingAllOpponents(cs).WithHitCount(hits).Execute(ctx);
                foreach (var e in cs.HittableEnemies)
                    CombatCounters.StruckByMortisThisTurn[e] += hits;
            }
        }
        else
        {
            await PlayCast();
            await Sts2Compat.PowerApply<IntangiblePower>(ctx, Owner.Creature, DynamicVars["IntangiblePower"].BaseValue, Owner.Creature, this, false);
        }
        if (Forms.IsMortisForm(Owner)) await CombatCounters.BumpMortisCard(ctx, Owner);
        else                            await CombatCounters.BumpMutsumiCard(ctx, Owner);
    }

    public override List<(string, string)>? Localization => LocManager.Instance.Language switch
    {
        "zhs" => new CardLoc("演奏《春日影》",
            "{ShowRealEffect:show:{MuSec}{MuOpen}小睦{MuClose}：获得{IntangiblePower}层[gold]无实体[/gold]。{MuSecEnd}\n{MoSec}{MoOpen}小墨{MoClose}：对全体敌人造成{Damage:diff()}点伤害{Hits}次。{MoSecEnd}|非演奏会回合，获得1点[gold]演艺热情[/gold]。}"),
        _ => new CardLoc("Play Haruhikage",
            "{ShowRealEffect:show:{MuSec}{MuOpen}Mu{MuClose}: Gain {IntangiblePower} [gold]Intangible[/gold].{MuSecEnd}\n{MoSec}{MoOpen}Mo{MoClose}: Deal {Damage:diff()} damage to ALL enemies {Hits} times.{MoSecEnd}|Outside concert, gain 1 [gold]Performance Passion[/gold].}"),
    };
}
