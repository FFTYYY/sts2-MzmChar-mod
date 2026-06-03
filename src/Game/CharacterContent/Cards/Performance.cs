using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Cards.Variables;
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
///   小睦：获得 7/10 格挡，施加 1/2 层虚弱，[gold]进入小墨[/gold]。
///   小墨：造成 10/14 点伤害，[gold]进入小睦[/gold]。
/// </summary>
[Pool(typeof(MzmCharCardPool))]
public class Performance : MzmCharBaseCard
{
    public override string PortraitPath => "res://MzmChar/cards/performance.png";

    private readonly List<DynamicVar> _vars = new()
    {
        new DamageVar(10, ValueProp.Move),
        new BlockVar(7, ValueProp.Move),
        new PowerVar<WeakPower>(1),
    };
    protected override IEnumerable<DynamicVar> CanonicalVars => _vars;

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            foreach (var t in FormTooltips.BothEnter()) yield return t;
            yield return HoverTipFactory.FromPower<WeakPower>();
        }
    }

    public Performance() : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy) { }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(4);              // 10 → 14
        DynamicVars.Block.UpgradeValueBy(3);               // 7 → 10
        DynamicVars["WeakPower"].UpgradeValueBy(1);        // 1 → 2
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        if (Forms.IsMortisForm(Owner))
        {
            if (play.Target != null)
            {
                await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                    .FromCard(this).Targeting(play.Target).Execute(ctx);
            }
            await Forms.EnterMutsumi(Owner, this, ctx);
        }
        else
        {
            await CreatureCmd.GainBlock(Owner.Creature,
                DynamicVars.Block.BaseValue, ValueProp.Move, play, false);
            if (play.Target != null)
            {
                await Sts2Compat.PowerApply<WeakPower>(ctx, play.Target,
                    DynamicVars["WeakPower"].BaseValue, Owner.Creature, this, false);
            }
            await Forms.EnterMortis(Owner, this, ctx);
        }
    }

    public override List<(string, string)>? Localization => LocManager.Instance.Language switch
    {
        "zhs" => new CardLoc("表演",
            "{MuSec}{MuOpen}小睦{MuClose}：获得{Block:diff()}点[gold]格挡[/gold]。施加{WeakPower:diff()}层[gold]虚弱[/gold]。[gold]进入小墨[/gold]。{MuSecEnd}\n" +
            "{MoSec}{MoOpen}小墨{MoClose}：造成{Damage:diff()}点伤害。[gold]进入小睦[/gold]。{MoSecEnd}"),
        _ => new CardLoc("Performance",
            "{MuSec}{MuOpen}Mu{MuClose}: Gain {Block:diff()} [gold]Block[/gold]; apply {WeakPower:diff()} [gold]Weak[/gold]; [gold]Enter Mo[/gold].{MuSecEnd}\n" +
            "{MoSec}{MoOpen}Mo{MoClose}: Deal {Damage:diff()} damage; [gold]Enter Mu[/gold].{MoSecEnd}"),
    };
}
