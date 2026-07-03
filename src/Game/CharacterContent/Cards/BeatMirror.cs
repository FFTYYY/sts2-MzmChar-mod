using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Cards.Variables;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace MzmChar.Game;

/// <summary>
/// 砸碎镜子：0 费白色攻击。
///   小睦：施加 1/2 易伤，获得 3 活力。
///   小墨：造成 5/8 伤害，施加 1 层虚弱。
/// </summary>
[Pool(typeof(MzmCharCardPool))]
public class BeatMirror : MzmCharBaseCard
{
    public override string PortraitPath => "res://MzmChar/cards/beat_mirror.png";

    private readonly List<DynamicVar> _vars = new()
    {
        new DamageVar(5, ValueProp.Move),                    // Mo damage
        new PowerVar<VigorPower>(3),
        new PowerVar<VulnerablePower>(1),                    // Mu Vulnerable
        new PowerVar<WeakPower>(1),                          // Mo Weak
    };
    protected override IEnumerable<DynamicVar> CanonicalVars => _vars;

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromPower<VigorPower>();
            yield return HoverTipFactory.FromPower<VulnerablePower>();
            yield return HoverTipFactory.FromPower<WeakPower>();
        }
    }

    public BeatMirror() : base(0, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy) { }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3);                  // Mo: 5 → 8
        DynamicVars["VulnerablePower"].UpgradeValueBy(1);      // Mu vuln: 1 → 2
        // Vigor 升级不再 +2，base 3 保持
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        if (Forms.IsMortisForm(Owner))
        {
            if (play.Target != null)
            {
                await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                    .FromCardCompat(this, play).Targeting(play.Target).Execute(ctx);
                await Sts2Compat.PowerApply<WeakPower>(ctx, play.Target, DynamicVars["WeakPower"].BaseValue, Owner.Creature, this, false);
            }
        }
        else
        {
            await PlayCast();
            if (play.Target != null)
                await Sts2Compat.PowerApply<VulnerablePower>(ctx, play.Target,
                    DynamicVars["VulnerablePower"].BaseValue, Owner.Creature, this, false);
            await Sts2Compat.PowerApply<VigorPower>(ctx, Owner.Creature,
                DynamicVars["VigorPower"].BaseValue, Owner.Creature, this, false);
        }
    }

    public override List<(string, string)>? Localization => LocManager.Instance.Language switch
    {
        "zhs" => new CardLoc("砸碎镜子",
            "{MuSec}{MuOpen}小睦{MuClose}：获得{VigorPower:diff()}点[gold]活力[/gold]。施加{VulnerablePower:diff()}层[gold]易伤[/gold]。{MuSecEnd}\n" +
            "{MoSec}{MoOpen}小墨{MoClose}：造成{Damage:diff()}点伤害。施加{WeakPower}层[gold]虚弱[/gold]。{MoSecEnd}"),
        _ => new CardLoc("Shatter Mirror",
            "{MuSec}{MuOpen}Mu{MuClose}: Apply {VulnerablePower:diff()} [gold]Vulnerable[/gold]; gain {VigorPower:diff()} [gold]Vigor[/gold].{MuSecEnd}\n" +
            "{MoSec}{MoOpen}Mo{MoClose}: Deal {Damage:diff()} damage; apply {WeakPower} [gold]Weak[/gold].{MoSecEnd}"),
    };
}
