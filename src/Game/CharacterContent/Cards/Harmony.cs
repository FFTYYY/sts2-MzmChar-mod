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
/// 和声：1 费攻击。抽 1 张。
///   小睦：获得 8/11 格挡
///   小墨：造成 8/11 伤害
/// </summary>
[Pool(typeof(MzmCharCardPool))]
public class Harmony : MzmCharBaseCard
{
    public override string PortraitPath => "res://MzmChar/cards/harmony.png";

    private readonly List<DynamicVar> _vars = new()
    {
        new DamageVar(8, ValueProp.Move),
        new BlockVar(8, ValueProp.Move),
        new CardsVar(1),
    };
    protected override IEnumerable<DynamicVar> CanonicalVars => _vars;

    public Harmony() : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy) { }

    // 动态目标：小睦自给格挡不要目标，小墨打伤害要目标。
    // 两道短路必须都有：
    //   1. !IsCanonical：canonical 不能访问 Owner（get_Owner 内部 AssertMutable 会抛）
    //   2. Owner != null：NInspectCardScreen 等渲染 mutable 预览实例时 Owner 可以是 null
    // 都不满足时 fallback AnyEnemy（卡库 / 详情页 / 战斗外的"理论上"目标类型）
    public override TargetType TargetType =>
        !IsCanonical && Owner != null && Forms.IsMutsumiForm(Owner)
            ? TargetType.Self
            : TargetType.AnyEnemy;

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3);                // 8 → 11
        DynamicVars.Block.UpgradeValueBy(3);                 // 8 → 11
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await CardPileCmd.Draw(ctx, DynamicVars.Cards.BaseValue, Owner, false);

        if (Forms.IsMortisForm(Owner))
        {
            if (play.Target != null)
            {
                await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                    .FromCard(this).Targeting(play.Target).Execute(ctx);
            }
        }
        else
        {
            await CreatureCmd.GainBlock(Owner.Creature,
                DynamicVars.Block.BaseValue, ValueProp.Move, play, false);
        }
    }

    public override List<(string, string)>? Localization => LocManager.Instance.Language switch
    {
        "zhs" => new CardLoc("和声",
            "抽{Cards}张牌。\n" +
            "{MuSec}{MuOpen}小睦{MuClose}：获得{Block:diff()}点[gold]格挡[/gold]。{MuSecEnd}\n" +
            "{MoSec}{MoOpen}小墨{MoClose}：造成{Damage:diff()}点伤害。{MoSecEnd}"),
        _ => new CardLoc("Harmony",
            "Draw {Cards}.\n" +
            "{MuSec}{MuOpen}Mu{MuClose}: Gain {Block:diff()} [gold]Block[/gold].{MuSecEnd}\n" +
            "{MoSec}{MoOpen}Mo{MoClose}: Deal {Damage:diff()} damage.{MoSecEnd}"),
    };
}
