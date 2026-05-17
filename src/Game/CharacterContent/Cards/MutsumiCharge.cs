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
/// 睦头人出击！：0 费基础攻击。
///   基础：造成 0 点伤害 2 次。
///   升级：造成 0 点伤害 2 次，获得 0 点格挡 2 次。
/// 用 WithHitCount(2) 让力量 / 活力 modifier 算一次但应用每次 hit（同 Catharsis Mo 分支）。
/// </summary>
[Pool(typeof(MzmCharCardPool))]
public class MutsumiCharge : MzmCharBaseCard
{
    public override string PortraitPath => "res://MzmChar/cards/mutsumi_charge.png";

    private readonly List<DynamicVar> _vars = new()
    {
        new DamageVar(0, ValueProp.Move),
        new BlockVar(0, ValueProp.Move),
        new DynamicVar("Hits", 2m),
    };
    protected override IEnumerable<DynamicVar> CanonicalVars => _vars;

    private readonly HashSet<CardTag> _tags = new() { CardTag.Strike };
    protected override HashSet<CardTag> CanonicalTags => _tags;

    // 初始牌：禁止被「发现」类抽到（与 MuStrike/MuDefend 同款理由）
    public override bool CanBeGeneratedInCombat => false;
    public override bool CanBeGeneratedByModifiers => false;

    public MutsumiCharge() : base(0, CardType.Attack, CardRarity.Basic, TargetType.AnyEnemy) { }

    protected override void OnUpgrade()
    {
        // 升级仅"额外获得 0 格挡 ×2" —— 描述里多一条 clause（IsUpgraded gated），数值不变
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        int hits = (int)DynamicVars["Hits"].BaseValue;
        if (play.Target != null)
        {
            await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                .FromCard(this).Targeting(play.Target).WithHitCount(hits).Execute(ctx);
            if (Forms.IsMortisForm(Owner))
                CombatCounters.StruckByMortisThisTurn[play.Target] += hits;
        }

        if (IsUpgraded)
        {
            // 升级才生效；多次 GainBlock 让力量 / 敏捷 modifier 应用到每次格挡
            for (int i = 0; i < hits; i++)
            {
                await CreatureCmd.GainBlock(Owner.Creature,
                    DynamicVars.Block.BaseValue, ValueProp.Move, play, false);
            }
        }

        if (Forms.IsMortisForm(Owner)) await CombatCounters.BumpMortisCard(ctx, Owner);
        else                            await CombatCounters.BumpMutsumiCard(ctx, Owner);
    }

    public override List<(string, string)>? Localization => LocManager.Instance.Language switch
    {
        "zhs" => new CardLoc("睦头人出击！",
            "造成{Damage:diff()}点伤害{Hits}次。{IfUpgraded:show:获得{Block:diff()}点[gold]格挡[/gold]{Hits}次。|}"),
        _ => new CardLoc("Mutsumi, Charge!",
            "Deal {Damage:diff()} damage {Hits} times.{IfUpgraded:show: Gain {Block:diff()} [gold]Block[/gold] {Hits} times.|}"),
    };
}
