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
/// 扭曲：0 费攻击。
///   小睦：本回合失去2敏捷（升级失 1），获得1费，进入小墨
///   小墨：造成5伤（升级 6）
///
/// 「本回合失去 N 敏捷」= 持久 -N 敏捷 + 回合结束 +N 恢复（合并到 TempDexterityPower，负 Amount 反转方向）。
/// </summary>
[Pool(typeof(MzmCharCardPool))]
public class Distort : MzmCharBaseCard
{
    public override string PortraitPath => "res://MzmChar/cards/distort.png";

    private readonly List<DynamicVar> _vars = new()
    {
        new DamageVar(5, ValueProp.Move),
        new EnergyVar(1),
        new DynamicVar("DexLoss", 2m),
    };
    protected override IEnumerable<DynamicVar> CanonicalVars => _vars;

    // Mu 分支有「进入小墨」 → 挂 EnterMo tooltip（"转换成小墨人格"）
    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            foreach (var t in FormTooltips.EnterMo()) yield return t;
            yield return HoverTipFactory.FromPower<DexterityPower>();
        }
    }

    public Distort() : base(0, CardType.Skill, CardRarity.Common, TargetType.Self) { }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(1);
        DynamicVars["DexLoss"].UpgradeValueBy(-1);
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        if (Forms.IsMortisForm(Owner))
        {
            var cs = Owner.Creature.CombatState;
            if (cs != null && cs.HittableEnemies.Count > 0)
            {
                var rng = Owner.RunState?.Rng?.CombatTargets;
                var target = rng != null ? rng.NextItem(cs.HittableEnemies) : cs.HittableEnemies[0];
                if (target != null)
                {
                    await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                        .FromCard(this).Targeting(target).Execute(ctx);
                }
            }
        }
        else
        {
            var dexLoss = DynamicVars["DexLoss"].BaseValue;
            await Sts2Compat.PowerApply<DexterityPower>(ctx, Owner.Creature, -dexLoss, Owner.Creature, this, false);
            // 负 Amount → 回合结束 +dexLoss 恢复敏捷（合并的 TempDexterityPower 负号语义）
            await Sts2Compat.PowerApply<TempDexterityPower>(ctx, Owner.Creature, -dexLoss, Owner.Creature, this, true);
            await PlayerCmd.GainEnergy(DynamicVars.Energy.IntValue, Owner);
            await Forms.EnterMortis(Owner, this, ctx);
        }
    }

    public override List<(string, string)>? Localization => LocManager.Instance.Language switch
    {
        "zhs" => new CardLoc("扭曲",
            "{MuSec}{MuOpen}小睦{MuClose}：本回合失去{DexLoss:diff()}点[gold]敏捷[/gold]。获得{Energy:energyIcons()}。[gold]进入小墨[/gold]。{MuSecEnd}\n" +
            "{MoSec}{MoOpen}小墨{MoClose}：对随机敌人造成{Damage:diff()}点伤害。{MoSecEnd}"),
        _ => new CardLoc("Distort",
            "{MuSec}{MuOpen}Mu{MuClose}: This turn lose {DexLoss:diff()} [gold]Dexterity[/gold]; gain {Energy:energyIcons()}; [gold]Enter Mo[/gold].{MuSecEnd}\n" +
            "{MoSec}{MoOpen}Mo{MoClose}: Deal {Damage:diff()} damage to a random enemy.{MoSecEnd}"),
    };
}
