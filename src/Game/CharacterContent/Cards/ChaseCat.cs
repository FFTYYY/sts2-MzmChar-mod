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
using MegaCrit.Sts2.Core.ValueProps;

namespace MzmChar.Game;

/// <summary>
/// 猫猫：1 费白色攻击。
///   小睦：抽 2/3 张牌。
///   小墨：对随机敌人造成 3/4 点伤害 3 次（每次单独选随机敌人）。
/// </summary>
[Pool(typeof(MzmCharCardPool))]
public class ChaseCat : MzmCharBaseCard
{
    public override string PortraitPath => "res://MzmChar/cards/chase_cat.png";

    private readonly List<DynamicVar> _vars = new()
    {
        new DamageVar(3, ValueProp.Move),
        new CardsVar(2),
        new DynamicVar("Hits", 3m),
    };
    protected override IEnumerable<DynamicVar> CanonicalVars => _vars;

    // TargetType.RandomEnemy = 4（IL-verified）：无需手动选目标（与"随机伤害"语义一致），
    // 同时框架仍传 target 给 UpdateCardPreview 让 vuln/weak modifier 能算进 display。
    public ChaseCat() : base(1, CardType.Attack, CardRarity.Common, TargetType.RandomEnemy) { }

    // Mu 只抽牌 → 不需要任何瞄准箭头；Mo 是随机多次 → RandomEnemy
    public override TargetType TargetType =>
        !IsCanonical && Owner != null && Forms.IsMutsumiForm(Owner)
            ? TargetType.Self
            : TargetType.RandomEnemy;

    protected override void OnUpgrade()
    {
        DynamicVars.Cards.UpgradeValueBy(1);   // Mu 抽牌：2 → 3
        DynamicVars.Damage.UpgradeValueBy(1);  // Mo 伤害：3 → 4（hits 仍 3）
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        if (Forms.IsMortisForm(Owner))
        {
            var cs = Owner.Creature.CombatState;
            int hits = (int)DynamicVars["Hits"].BaseValue;
            int dmg = (int)DynamicVars.Damage.BaseValue;
            if (cs != null && cs.HittableEnemies.Count > 0)
            {
                // 单 AttackCommand + WithHitCount + TargetingRandomOpponents(allowDuplicates=true)
                // → 每次 hit 框架内部独立选随机敌人；vigor / strength modifier 算一次但应用到每次 hit
                await DamageCmd.Attack(dmg).FromCard(this)
                    .TargetingRandomOpponents(cs, allowDuplicates: true)
                    .WithHitCount(hits).Execute(ctx);
            }
        }
        else
        {
            await PlayCast();
            await CardPileCmd.Draw(ctx, DynamicVars.Cards.BaseValue, Owner, false);
        }
    }

    public override List<(string, string)>? Localization => LocManager.Instance.Language switch
    {
        "zhs" => new CardLoc("猫猫",
            "{MuSec}{MuOpen}小睦{MuClose}：抽{Cards:diff()}张牌。{MuSecEnd}\n" +
            "{MoSec}{MoOpen}小墨{MoClose}：对随机敌人造成{Damage:diff()}点伤害{Hits}次。{MoSecEnd}"),
        _ => new CardLoc("Chase Cat",
            "{MuSec}{MuOpen}Mu{MuClose}: Draw {Cards:diff()}.{MuSecEnd}\n" +
            "{MoSec}{MoOpen}Mo{MoClose}: Deal {Damage:diff()} damage to a random enemy, {Hits} times.{MoSecEnd}"),
    };
}
