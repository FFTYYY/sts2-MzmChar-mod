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
///   小睦：本回合获得 3/4 力量，给目标施加 2/3 易伤。
///   小墨：造成 4/7 伤害 2 次。
/// </summary>
[Pool(typeof(MzmCharCardPool))]
public class StairsTumble : MzmCharBaseCard
{
    public override string PortraitPath => "res://MzmChar/cards/stairs_tumble.png";

    private readonly List<DynamicVar> _vars = new()
    {
        new DamageVar(4, ValueProp.Move),
        new DynamicVar("MuStr", 3m),
        new PowerVar<VulnerablePower>(2),
        new DynamicVar("Hits", 2m),
    };
    protected override IEnumerable<DynamicVar> CanonicalVars => _vars;

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromPower<StrengthPower>();
            yield return HoverTipFactory.FromPower<VulnerablePower>();
        }
    }

    public StairsTumble() : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy) { }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3);               // 4 → 7
        DynamicVars["MuStr"].UpgradeValueBy(1);             // 3 → 4
        DynamicVars["VulnerablePower"].UpgradeValueBy(1);   // 2 → 3
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
            var str = DynamicVars["MuStr"].BaseValue;
            await Sts2Compat.PowerApply<StrengthPower>(ctx, Owner.Creature, str, Owner.Creature, this, false);
            await Sts2Compat.PowerApply<TempStrengthPower>(ctx, Owner.Creature, str, Owner.Creature, this, true);
            if (play.Target != null)
                await Sts2Compat.PowerApply<VulnerablePower>(ctx, play.Target,
                    DynamicVars["VulnerablePower"].BaseValue, Owner.Creature, this, false);
        }
    }

    public override List<(string, string)>? Localization => LocManager.Instance.Language switch
    {
        "zhs" => new CardLoc("楼梯打滚",
            "{MuSec}{MuOpen}小睦{MuClose}：本回合获得{MuStr:diff()}点[gold]力量[/gold]。施加{VulnerablePower:diff()}层[gold]易伤[/gold]。{MuSecEnd}\n" +
            "{MoSec}{MoOpen}小墨{MoClose}：造成{Damage:diff()}点伤害{Hits}次。{MoSecEnd}"),
        _ => new CardLoc("Stairs Tumble",
            "{MuSec}{MuOpen}Mu{MuClose}: This turn gain {MuStr:diff()} [gold]Strength[/gold]; apply {VulnerablePower:diff()} [gold]Vulnerable[/gold].{MuSecEnd}\n" +
            "{MoSec}{MoOpen}Mo{MoClose}: Deal {Damage:diff()} damage {Hits} times.{MoSecEnd}"),
    };
}
