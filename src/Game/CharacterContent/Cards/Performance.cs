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
/// 表演：1 费白色攻击。
///   小睦：对目标施加 2 层虚弱、1 层易伤（升级后 3 / 2），进入小墨。
///   小墨：造成 9/13 伤害。
/// </summary>
[Pool(typeof(MzmCharCardPool))]
public class Performance : MzmCharBaseCard
{
    public override string PortraitPath => "res://MzmChar/cards/performance.png";

    private readonly List<DynamicVar> _vars = new()
    {
        new DamageVar(9, ValueProp.Move),
        new PowerVar<WeakPower>(2),
        new PowerVar<VulnerablePower>(1),
    };
    protected override IEnumerable<DynamicVar> CanonicalVars => _vars;

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            foreach (var t in FormTooltips.EnterMo()) yield return t;
            yield return HoverTipFactory.FromPower<VulnerablePower>();
            yield return HoverTipFactory.FromPower<WeakPower>();
        }
    }

    public Performance() : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy) { }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(4);              // Mo 9 → 13
        DynamicVars["WeakPower"].UpgradeValueBy(1);        // 2 → 3
        DynamicVars["VulnerablePower"].UpgradeValueBy(1);  // 1 → 2
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        if (Forms.IsMortisForm(Owner))
        {
            if (play.Target != null)
            {
                await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                    .FromCard(this).Targeting(play.Target).Execute(ctx);
                CombatCounters.StruckByMortisThisTurn[play.Target]++;
            }
            await CombatCounters.BumpMortisCard(ctx, Owner);
        }
        else
        {
            if (play.Target != null)
            {
                await PowerCmd.Apply<WeakPower>(play.Target,
                    DynamicVars["WeakPower"].BaseValue, Owner.Creature, this, false);
                await PowerCmd.Apply<VulnerablePower>(play.Target,
                    DynamicVars["VulnerablePower"].BaseValue, Owner.Creature, this, false);
            }
            await CombatCounters.BumpMutsumiCard(ctx, Owner);
            await Forms.EnterMortis(Owner, this, ctx);
        }
    }

    public override List<(string, string)>? Localization => LocManager.Instance.Language switch
    {
        "zhs" => new CardLoc("表演",
            "{MuSec}{MuOpen}小睦{MuClose}：施加{WeakPower:diff()}层[gold]虚弱[/gold]和{VulnerablePower:diff()}层[gold]易伤[/gold]。[gold]进入小墨[/gold]。{MuSecEnd}\n" +
            "{MoSec}{MoOpen}小墨{MoClose}：造成{Damage:diff()}点伤害。{MoSecEnd}"),
        _ => new CardLoc("Performance",
            "{MuSec}{MuOpen}Mu{MuClose}: Apply {WeakPower:diff()} [gold]Weak[/gold] and {VulnerablePower:diff()} [gold]Vulnerable[/gold]; [gold]Enter Mo[/gold].{MuSecEnd}\n" +
            "{MoSec}{MoOpen}Mo{MoClose}: Deal {Damage:diff()} damage.{MoSecEnd}"),
    };
}
