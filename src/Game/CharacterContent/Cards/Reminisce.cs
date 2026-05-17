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
/// 回忆：1 费蓝色攻击（AOE）。（原名「演奏《春日影》」）
///   小睦：对全体施加 1/2 虚弱 + 1/2 易伤，[gold]进入小墨[/gold]。
///   小墨：对全体造成 10/15 伤害，[gold]进入小睦[/gold]。
/// </summary>
[Pool(typeof(MzmCharCardPool))]
public class Reminisce : MzmCharBaseCard
{
    public override string PortraitPath => "res://MzmChar/cards/reminisce.png";

    private readonly List<DynamicVar> _vars = new()
    {
        new DamageVar(10, ValueProp.Move),
        new PowerVar<WeakPower>(1),
        new PowerVar<VulnerablePower>(1),
    };
    protected override IEnumerable<DynamicVar> CanonicalVars => _vars;

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            foreach (var t in FormTooltips.BothEnter()) yield return t;
            yield return HoverTipFactory.FromPower<WeakPower>();
            yield return HoverTipFactory.FromPower<VulnerablePower>();
        }
    }

    public Reminisce() : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AllEnemies) { }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(5);              // 10 → 15
        DynamicVars["WeakPower"].UpgradeValueBy(1);        // 1 → 2
        DynamicVars["VulnerablePower"].UpgradeValueBy(1);  // 1 → 2
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        var cs = Owner.Creature.CombatState;
        if (Forms.IsMortisForm(Owner))
        {
            if (cs != null)
            {
                await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                    .FromCard(this).TargetingAllOpponents(cs).Execute(ctx);
                foreach (var e in cs.HittableEnemies)
                    CombatCounters.StruckByMortisThisTurn[e]++;
            }
            await CombatCounters.BumpMortisCard(ctx, Owner);
            await Forms.EnterMutsumi(Owner, this, ctx);
        }
        else
        {
            if (cs != null)
            {
                foreach (var e in cs.HittableEnemies)
                {
                    await Sts2Compat.PowerApply<WeakPower>(ctx, e, DynamicVars["WeakPower"].BaseValue, Owner.Creature, this, false);
                    await Sts2Compat.PowerApply<VulnerablePower>(ctx, e, DynamicVars["VulnerablePower"].BaseValue, Owner.Creature, this, false);
                }
            }
            await CombatCounters.BumpMutsumiCard(ctx, Owner);
            await Forms.EnterMortis(Owner, this, ctx);
        }
    }

    public override List<(string, string)>? Localization => LocManager.Instance.Language switch
    {
        "zhs" => new CardLoc("回忆",
            "{MuSec}{MuOpen}小睦{MuClose}：对全体敌人施加{WeakPower:diff()}层[gold]虚弱[/gold]和{VulnerablePower:diff()}层[gold]易伤[/gold]。[gold]进入小墨[/gold]。{MuSecEnd}\n" +
            "{MoSec}{MoOpen}小墨{MoClose}：对全体敌人造成{Damage:diff()}点伤害。[gold]进入小睦[/gold]。{MoSecEnd}"),
        _ => new CardLoc("Reminisce",
            "{MuSec}{MuOpen}Mu{MuClose}: Apply {WeakPower:diff()} [gold]Weak[/gold] and {VulnerablePower:diff()} [gold]Vulnerable[/gold] to ALL enemies. [gold]Enter Mo[/gold].{MuSecEnd}\n" +
            "{MoSec}{MoOpen}Mo{MoClose}: Deal {Damage:diff()} damage to ALL enemies. [gold]Enter Mu[/gold].{MoSecEnd}"),
    };
}
