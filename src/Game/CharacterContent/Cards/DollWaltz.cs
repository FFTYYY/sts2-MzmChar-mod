using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace MzmChar.Game;

/// <summary>
/// 人偶们的轮舞：X 费金色技能。造成 0 点伤害 floor(1.5X) 次，获得 0 点格挡 X 次。
/// 升级：攻击次数变 2X。
/// X 费：override HasEnergyCostX + ResolveEnergyXValue（参考 Whirlwind）
/// </summary>
[Pool(typeof(MzmCharCardPool))]
public class DollWaltz : MzmCharBaseCard
{
    public override string PortraitPath => "res://MzmChar/cards/doll_waltz.png";

    protected override bool HasEnergyCostX => true;

    // base=0 的伤害/格挡：用 DamageVar/BlockVar 让显示侧走 :diff() (PreviewValue=0+Str)
    private readonly List<DynamicVar> _vars = new()
    {
        new DamageVar(0, ValueProp.Move),
        new BlockVar(0, ValueProp.Move),
    };
    protected override IEnumerable<DynamicVar> CanonicalVars => _vars;

    protected override void AddExtraArgsToDescription(MegaCrit.Sts2.Core.Localization.LocString description)
    {
        base.AddExtraArgsToDescription(description);
        // X 预览 = 当前可用能量。canonical（卡库）时 Owner getter 直接抛 → 必须先短路
        int x = IsCanonical ? 0 : (Owner?.PlayerCombatState?.Energy ?? 0);
        int atkMult = IsUpgraded ? (2 * x) : (int)(1.5m * x);
        description.Add("XAttack", (decimal)atkMult);
        description.Add("XBlock", (decimal)x);
    }

    public DollWaltz() : base(0, CardType.Attack, CardRarity.Rare, TargetType.AllEnemies) { }

    protected override void OnUpgrade() { /* IsUpgraded 控制攻击次数倍率 */ }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        int x = ResolveEnergyXValue();
        if (x <= 0)
        {
            await Bump(ctx); return;
        }
        int attackHits = IsUpgraded ? (2 * x) : (int)(1.5m * x);
        int blockHits = x;

        var cs = Owner.Creature.CombatState;
        // 单 AttackCommand + WithHitCount → 力量/活力 等 modifier 算一次但应用到每次 hit
        // （如果 loop 多次单独 Execute，活力会在第一次后被消耗，后续 hit 没活力加成）
        if (cs != null && cs.HittableEnemies.Count > 0 && attackHits > 0)
        {
            await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                .FromCard(this).TargetingAllOpponents(cs)
                .WithHitCount(attackHits).Execute(ctx);
            if (Forms.IsMortisForm(Owner))
                foreach (var e in cs.HittableEnemies)
                    CombatCounters.StruckByMortisThisTurn[e] += attackHits;
        }
        for (int i = 0; i < blockHits; i++)
        {
            // GainBlock 不像 attack 那样有多次累计 modifier 的概念——敏捷加成每次都重新计算
            await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block.BaseValue, ValueProp.Move, play, false);
        }
        await Bump(ctx);
    }

    private async Task Bump(PlayerChoiceContext ctx)
    {
        if (Forms.IsMortisForm(Owner)) await CombatCounters.BumpMortisCard(ctx, Owner);
        else await CombatCounters.BumpMutsumiCard(ctx, Owner);
    }

    public override List<(string, string)>? Localization => LocManager.Instance.Language switch
    {
        "zhs" => new CardLoc("人偶们的轮舞",
            "对全体敌人造成{Damage:diff()}点伤害{IfUpgraded:show:2X|1.5X}次（{XAttack}次）。获得{Block:diff()}点[gold]格挡[/gold]X次（{XBlock}次）。"),
        _ => new CardLoc("Doll Waltz",
            "Deal {Damage:diff()} damage to ALL enemies {IfUpgraded:show:2X|1.5X}({XAttack}) times. Gain {Block:diff()} [gold]Block[/gold] X({XBlock}) times."),
    };
}
