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
/// 脑内霸凌：2 费白色攻击。
///   小睦：2/4 回合内给目标挂[gold]二重易伤[/gold]（有易伤时受伤再增加 50%）。
///   小墨：造成 10 伤害；2/4 回合内给目标挂[gold]二重虚弱[/gold]（有虚弱时伤害再减少 25%）。
/// </summary>
[Pool(typeof(MzmCharCardPool))]
public class BrainBully : MzmCharBaseCard
{
    public override string PortraitPath => "res://MzmChar/cards/brain_bully.png";

    private readonly List<DynamicVar> _vars = new()
    {
        new DamageVar(10, ValueProp.Move),
        new PowerVar<VulnerableDoublePower>(2),
        new PowerVar<WeakDoublePower>(2),
    };
    protected override IEnumerable<DynamicVar> CanonicalVars => _vars;

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            foreach (var t in FormTooltips.Both()) yield return t;
            yield return HoverTipFactory.FromPower<VulnerableDoublePower>();
            yield return HoverTipFactory.FromPower<WeakDoublePower>();
            yield return HoverTipFactory.FromPower<VulnerablePower>();
            yield return HoverTipFactory.FromPower<WeakPower>();
        }
    }

    public BrainBully() : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy) { }

    protected override void OnUpgrade()
    {
        DynamicVars["VulnerableDoublePower"].UpgradeValueBy(2);  // 2 → 4
        DynamicVars["WeakDoublePower"].UpgradeValueBy(2);        // 2 → 4
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        if (Forms.IsMortisForm(Owner))
        {
            if (play.Target != null)
            {
                await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                    .FromCard(this).Targeting(play.Target).Execute(ctx);
                await Sts2Compat.PowerApply<WeakDoublePower>(ctx, play.Target,
                    DynamicVars["WeakDoublePower"].BaseValue, Owner.Creature, this, false);
            }
        }
        else
        {
            await PlayCast();
            if (play.Target != null)
                await Sts2Compat.PowerApply<VulnerableDoublePower>(ctx, play.Target,
                    DynamicVars["VulnerableDoublePower"].BaseValue, Owner.Creature, this, false);
        }
    }

    public override List<(string, string)>? Localization => LocManager.Instance.Language switch
    {
        "zhs" => new CardLoc("脑内霸凌",
            "{MuSec}{MuOpen}小睦{MuClose}：{VulnerableDoublePower:diff()}回合内，若目标有[gold]易伤[/gold]，则其受到的伤害额外增加50%。{MuSecEnd}\n" +
            "{MoSec}{MoOpen}小墨{MoClose}：造成{Damage:diff()}点伤害。{WeakDoublePower:diff()}回合内，若目标有[gold]虚弱[/gold]，则其造成的伤害额外减少25%。{MoSecEnd}"),
        _ => new CardLoc("Brain Bully",
            "{MuSec}{MuOpen}Mu{MuClose}: For {VulnerableDoublePower:diff()} turns, if the target has [gold]Vulnerable[/gold], they take an additional 50% damage.{MuSecEnd}\n" +
            "{MoSec}{MoOpen}Mo{MoClose}: Deal {Damage:diff()} damage. For {WeakDoublePower:diff()} turns, if the target has [gold]Weak[/gold], they deal an additional 25% less damage.{MoSecEnd}"),
    };
}
