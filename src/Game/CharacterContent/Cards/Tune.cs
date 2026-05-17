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
/// 调音：1 费白色技能。获得演艺热情。
///   小墨：施加 2/3 层虚弱
///   小睦：获得 8/12 点格挡
/// </summary>
[Pool(typeof(MzmCharCardPool))]
public class Tune : MzmCharBaseCard
{
    public override string PortraitPath => "res://MzmChar/cards/tune.png";

    private readonly List<DynamicVar> _vars = new()
    {
        new BlockVar(8, ValueProp.Move),
        new PowerVar<WeakPower>(2),
    };
    protected override IEnumerable<DynamicVar> CanonicalVars => _vars;

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromPower<PerformancePassionPower>();
            yield return HoverTipFactory.FromPower<WeakPower>();
        }
    }

    public Tune() : base(1, CardType.Skill, CardRarity.Common, TargetType.AnyEnemy) { }

    // 小墨施虚弱要目标，小睦获格挡不要
    public override TargetType TargetType =>
        !IsCanonical && Owner != null && Forms.IsMutsumiForm(Owner)
            ? TargetType.Self
            : TargetType.AnyEnemy;

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(4);              // 8 → 12
        DynamicVars["WeakPower"].UpgradeValueBy(1);       // 2 → 3
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await PlayCast();
        await Sts2Compat.PowerApply<PerformancePassionPower>(ctx, Owner.Creature, 1, Owner.Creature, this, false);
        if (Forms.IsMortisForm(Owner))
        {
            if (play.Target != null)
                await Sts2Compat.PowerApply<WeakPower>(ctx, play.Target, DynamicVars["WeakPower"].BaseValue, Owner.Creature, this, false);
            await CombatCounters.BumpMortisCard(ctx, Owner);
        }
        else
        {
            await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block.BaseValue, ValueProp.Move, play, false);
            await CombatCounters.BumpMutsumiCard(ctx, Owner);
        }
    }

    public override List<(string, string)>? Localization => LocManager.Instance.Language switch
    {
        "zhs" => new CardLoc("调音",
            "获得1点[gold]演艺热情[/gold]。\n" +
            "{MuSec}{MuOpen}小睦{MuClose}：获得{Block:diff()}点[gold]格挡[/gold]。{MuSecEnd}\n" +
            "{MoSec}{MoOpen}小墨{MoClose}：施加{WeakPower:diff()}层[gold]虚弱[/gold]。{MoSecEnd}"),
        _ => new CardLoc("Tune",
            "Gain 1 [gold]Performance Passion[/gold].\n" +
            "{MuSec}{MuOpen}Mu{MuClose}: Gain {Block:diff()} [gold]Block[/gold].{MuSecEnd}\n" +
            "{MoSec}{MoOpen}Mo{MoClose}: Apply {WeakPower:diff()} [gold]Weak[/gold].{MoSecEnd}"),
    };
}
