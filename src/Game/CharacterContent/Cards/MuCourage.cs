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
/// 勇气：1 费蓝色攻击。
///   小墨：获得 7 格挡（卡型仍 Attack，不真打伤害）
///   小睦：造成 4 点伤害 2 次
/// 升级：添加[gold]保留[/gold]。
/// </summary>
[Pool(typeof(MzmCharCardPool))]
public class MuCourage : MzmCharBaseCard
{
    public override string PortraitPath => "res://MzmChar/cards/mzmchar_courage.png";

    private readonly List<DynamicVar> _vars = new()
    {
        new BlockVar(7, ValueProp.Move),
        new DamageVar(4, ValueProp.Move),
        new DynamicVar("Hits", 2m),
    };
    protected override IEnumerable<DynamicVar> CanonicalVars => _vars;

    public MuCourage() : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy) { }

    public override TargetType TargetType =>
        !IsCanonical && Owner != null && Forms.IsMortisForm(Owner)
            ? TargetType.Self   // Mo 获得格挡，不要目标
            : TargetType.AnyEnemy;  // Mu 打伤害，要目标

    protected override void OnUpgrade() { AddKeyword(CardKeyword.Retain); }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        if (Forms.IsMortisForm(Owner))
        {
            await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block.BaseValue, ValueProp.Move, play, false);
            await CombatCounters.BumpMortisCard(ctx, Owner);
        }
        else
        {
            int hits = (int)DynamicVars["Hits"].BaseValue;
            // WithHitCount —— 让活力 / 力量 modifier 应用到每次 hit
            if (play.Target != null)
            {
                await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                    .FromCard(this).Targeting(play.Target).WithHitCount(hits).Execute(ctx);
            }
            await CombatCounters.BumpMutsumiCard(ctx, Owner);
        }
    }

    public override List<(string, string)>? Localization => LocManager.Instance.Language switch
    {
        "zhs" => new CardLoc("勇气",
            "{MuSec}{MuOpen}小睦{MuClose}：造成{Damage:diff()}点伤害{Hits}次。{MuSecEnd}\n" +
            "{MoSec}{MoOpen}小墨{MoClose}：获得{Block:diff()}点[gold]格挡[/gold]。{MoSecEnd}"),
        _ => new CardLoc("Courage",
            "{MuSec}{MuOpen}Mu{MuClose}: Deal {Damage:diff()} damage {Hits} times.{MuSecEnd}\n" +
            "{MoSec}{MoOpen}Mo{MoClose}: Gain {Block:diff()} [gold]Block[/gold].{MoSecEnd}"),
    };
}
