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
/// 不和谐音 (Disharmony)：MutsumiCharge 的「先古」版本，由古老牙齿（ArchaicTooth）转化得到。
/// 0 费攻击，对全体敌人生效。
///   基础：对全体敌人造成 0 点伤害 2 次，获得 0 点格挡。重放 2。
///   升级：伤害 0 → 2，格挡 0 → 2（hits / 重放数不变）。
///
/// 「重放」走 vanilla `CardModel.BaseReplayCount`（IL-verified setter）。
/// 描述里不写"重放 N" —— framework 自动追加。
/// </summary>
[Pool(typeof(MzmCharCardPool))]
public class Disharmony : MzmCharBaseCard
{
    public override string PortraitPath => "res://MzmChar/cards/disharmony.png";

    // 由古老牙齿转化得到的特殊卡，不应该被印牌/变化牌机制随机产生
    public override bool CanBeGeneratedInCombat => false;
    public override bool CanBeGeneratedByModifiers => false;

    private readonly List<DynamicVar> _vars = new()
    {
        new DamageVar(0, ValueProp.Move),
        new BlockVar(0, ValueProp.Move),
        new DynamicVar("DmgHits", 2m),
        // BlockHits 保留作为可调字段。当前固定为 1 → loc 文案省略 "N 次"。
        // 如果以后改成多次，记得把 loc 里 "获得 X 点格挡" 后面加回 "{BlockHits} 次"。
        new DynamicVar("BlockHits", 1m),
    };
    protected override IEnumerable<DynamicVar> CanonicalVars => _vars;

    public Disharmony() : base(0, CardType.Attack, CardRarity.Ancient, TargetType.AllEnemies)
    {
        SetDefaultReplayCount(2);  // 直接写 field，绕开 set_BaseReplayCount 的 AssertMutable
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(2);   // 0 → 2
        DynamicVars.Block.UpgradeValueBy(2);    // 0 → 2
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        int dmgHits = (int)DynamicVars["DmgHits"].BaseValue;
        int blockHits = (int)DynamicVars["BlockHits"].BaseValue;

        var cs = Owner.Creature.CombatState;
        if (cs != null && cs.HittableEnemies.Count > 0)
        {
            await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                .FromCard(this).TargetingAllOpponents(cs).WithHitCount(dmgHits).Execute(ctx);
            if (Forms.IsMortisForm(Owner))
            {
                foreach (var e in cs.HittableEnemies)
                    CombatCounters.StruckByMortisThisTurn[e] += dmgHits;
            }
        }

        for (int i = 0; i < blockHits; i++)
        {
            await CreatureCmd.GainBlock(Owner.Creature,
                DynamicVars.Block.BaseValue, ValueProp.Move, play, false);
        }

        if (Forms.IsMortisForm(Owner)) await CombatCounters.BumpMortisCard(ctx, Owner);
        else                            await CombatCounters.BumpMutsumiCard(ctx, Owner);
    }

    public override List<(string, string)>? Localization => LocManager.Instance.Language switch
    {
        "zhs" => new CardLoc("不和谐音",
            "对全体敌人造成{Damage:diff()}点伤害{DmgHits}次。获得{Block:diff()}点[gold]格挡[/gold]。"),
        _ => new CardLoc("Disharmony",
            "Deal {Damage:diff()} damage to ALL enemies {DmgHits} times. Gain {Block:diff()} [gold]Block[/gold]."),
    };
}
