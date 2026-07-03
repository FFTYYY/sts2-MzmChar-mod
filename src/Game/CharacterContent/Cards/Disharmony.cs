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
/// 0 费攻击，重放 2，对全体敌人。
///   基础：对全体敌人造成 0 点伤害 2 次。
///   升级：在基础之上，额外获得 0 点格挡。
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
        // BlockHits 0 → 1：升级后 OnUpgrade 中 +1，让升级版才有"获得格挡"步骤
        new DynamicVar("BlockHits", 0m),
    };
    protected override IEnumerable<DynamicVar> CanonicalVars => _vars;

    public Disharmony() : base(0, CardType.Attack, CardRarity.Ancient, TargetType.AllEnemies)
    {
        SetDefaultReplayCount(2);  // 直接写 field，绕开 set_BaseReplayCount 的 AssertMutable
    }

    protected override void OnUpgrade()
    {
        // 升级：额外加一次获得格挡（base 0 + 1 → 1 次）；伤害 / 重放数 / 目标全部不变
        DynamicVars["BlockHits"].UpgradeValueBy(1);
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        int dmgHits = (int)DynamicVars["DmgHits"].BaseValue;
        int blockHits = (int)DynamicVars["BlockHits"].BaseValue;

        var cs = Owner.Creature.CombatState;
        if (cs != null && cs.HittableEnemies.Count > 0)
        {
            await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                .FromCardCompat(this, play).TargetingAllOpponents(cs).WithHitCount(dmgHits).Execute(ctx);
        }

        for (int i = 0; i < blockHits; i++)
        {
            await CreatureCmd.GainBlock(Owner.Creature,
                DynamicVars.Block.BaseValue, ValueProp.Move, play, false);
        }
    }

    public override List<(string, string)>? Localization => LocManager.Instance.Language switch
    {
        "zhs" => new CardLoc("不和谐音",
            "对全体敌人造成{Damage:diff()}点伤害{DmgHits}次。{IfUpgraded:show:获得{Block:diff()}点[gold]格挡[/gold]。|}"),
        _ => new CardLoc("Disharmony",
            "Deal {Damage:diff()} damage to ALL enemies {DmgHits} times.{IfUpgraded:show: Gain {Block:diff()} [gold]Block[/gold].|}"),
    };
}
