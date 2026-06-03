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
/// 楼梯打滚：1 费白色攻击。
///   小睦：获得 5/7 点活力，给目标施加 1/2 易伤。
///   小墨：造成 4/7 伤害 2 次。
/// </summary>
[Pool(typeof(MzmCharCardPool))]
public class StairsTumble : MzmCharBaseCard
{
    public override string PortraitPath => "res://MzmChar/cards/stairs_tumble.png";

    private readonly List<DynamicVar> _vars = new()
    {
        new DamageVar(4, ValueProp.Move),
        new PowerVar<VigorPower>(5),
        new PowerVar<VulnerablePower>(1),
        new DynamicVar("Hits", 2m),
    };
    protected override IEnumerable<DynamicVar> CanonicalVars => _vars;

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromPower<VigorPower>();
            yield return HoverTipFactory.FromPower<VulnerablePower>();
        }
    }

    public StairsTumble() : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy) { }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3);               // 4 → 7
        DynamicVars["VigorPower"].UpgradeValueBy(2);        // 5 → 7
        DynamicVars["VulnerablePower"].UpgradeValueBy(1);   // 1 → 2
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        if (Forms.IsMortisForm(Owner))
        {
            int hits = (int)DynamicVars["Hits"].BaseValue;
            int dmg = (int)DynamicVars.Damage.BaseValue;
            if (play.Target != null)
            {
                // 单 AttackCommand + WithHitCount → vigor / strength 等 modifier 算一次但应用于每次 hit；
                // VigorPower.AfterAttack 也只 fire 一次（消耗一次活力）
                await DamageCmd.Attack(dmg).FromCard(this).Targeting(play.Target).WithHitCount(hits).Execute(ctx);
            }
        }
        else
        {
            await PlayCast();
            await Sts2Compat.PowerApply<VigorPower>(ctx, Owner.Creature,
                DynamicVars["VigorPower"].BaseValue, Owner.Creature, this, false);
            if (play.Target != null)
                await Sts2Compat.PowerApply<VulnerablePower>(ctx, play.Target,
                    DynamicVars["VulnerablePower"].BaseValue, Owner.Creature, this, false);
        }
    }

    public override List<(string, string)>? Localization => LocManager.Instance.Language switch
    {
        "zhs" => new CardLoc("楼梯打滚",
            "{MuSec}{MuOpen}小睦{MuClose}：获得{VigorPower:diff()}点[gold]活力[/gold]。施加{VulnerablePower:diff()}层[gold]易伤[/gold]。{MuSecEnd}\n" +
            "{MoSec}{MoOpen}小墨{MoClose}：造成{Damage:diff()}点伤害{Hits}次。{MoSecEnd}"),
        _ => new CardLoc("Stairs Tumble",
            "{MuSec}{MuOpen}Mu{MuClose}: Gain {VigorPower:diff()} [gold]Vigor[/gold]; apply {VulnerablePower:diff()} [gold]Vulnerable[/gold].{MuSecEnd}\n" +
            "{MoSec}{MoOpen}Mo{MoClose}: Deal {Damage:diff()} damage {Hits} times.{MoSecEnd}"),
    };
}
