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
/// 失控：1 费白色攻击。
///   小墨：造成 9/12 伤害
///   小睦：获得 5/7 层活力
/// 之后随机进入小睦或小墨。
/// </summary>
[Pool(typeof(MzmCharCardPool))]
public class OutOfControl : MzmCharBaseCard
{
    public override string PortraitPath => "res://MzmChar/cards/out_of_control.png";

    private readonly List<DynamicVar> _vars = new()
    {
        new DamageVar(9, ValueProp.Move),
        new PowerVar<VigorPower>(5),
    };
    protected override IEnumerable<DynamicVar> CanonicalVars => _vars;

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            foreach (var t in FormTooltips.BothEnter()) yield return t;
            yield return HoverTipFactory.FromPower<VigorPower>();
        }
    }

    public OutOfControl() : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy) { }

    public override TargetType TargetType =>
        !IsCanonical && Owner != null && Forms.IsMutsumiForm(Owner)
            ? TargetType.Self
            : TargetType.AnyEnemy;

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3);          // 9 → 12
        DynamicVars["VigorPower"].UpgradeValueBy(2);   // 5 → 7
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        // 1. 本形态的主效果
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
            await Sts2Compat.PowerApply<VigorPower>(ctx, Owner.Creature, DynamicVars["VigorPower"].BaseValue, Owner.Creature, this, false);
        }

        // 2. 主效果之后随机进入小睦或小墨
        var rng = Owner.RunState?.Rng?.CombatCardSelection;
        bool goMu = rng != null ? (rng.NextInt(0, 2) == 0) : true;
        if (goMu) await Forms.EnterMutsumi(Owner, this, ctx);
        else       await Forms.EnterMortis(Owner, this, ctx);
    }

    public override List<(string, string)>? Localization => LocManager.Instance.Language switch
    {
        "zhs" => new CardLoc("失控",
            "{MuSec}{MuOpen}小睦{MuClose}：获得{VigorPower:diff()}点[gold]活力[/gold]。{MuSecEnd}\n" +
            "{MoSec}{MoOpen}小墨{MoClose}：造成{Damage:diff()}点伤害。{MoSecEnd}\n" +
            "之后随机[gold]进入小睦[/gold]或[gold]进入小墨[/gold]。"),
        _ => new CardLoc("Out of Control",
            "{MuSec}{MuOpen}Mu{MuClose}: Gain {VigorPower:diff()} [gold]Vigor[/gold].{MuSecEnd}\n" +
            "{MoSec}{MoOpen}Mo{MoClose}: Deal {Damage:diff()} damage.{MoSecEnd}\n" +
            "Then randomly [gold]Enter Mu[/gold] or [gold]Enter Mo[/gold]."),
    };
}
