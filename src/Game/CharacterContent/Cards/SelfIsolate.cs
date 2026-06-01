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
/// 自闭：0 费技能（无消耗）。
///   小睦：获得 5/8 格挡
///   小墨：获得 1 费，对自己施加 1 层虚弱，进入小睦。升级后去掉自施虚弱（仍然 1 费，不再加多）。
/// </summary>
[Pool(typeof(MzmCharCardPool))]
public class SelfIsolate : MzmCharBaseCard
{
    public override string PortraitPath => "res://MzmChar/cards/self_isolate.png";

    private readonly List<DynamicVar> _vars = new()
    {
        new BlockVar(5, ValueProp.Move),
        new EnergyVar(1),
        new PowerVar<WeakPower>(1),
    };
    protected override IEnumerable<DynamicVar> CanonicalVars => _vars;

    private readonly HashSet<CardKeyword> _keywords = new();
    public override IEnumerable<CardKeyword> CanonicalKeywords => _keywords;

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            foreach (var t in FormTooltips.EnterMu()) yield return t;
            yield return HoverTipFactory.FromPower<WeakPower>();
        }
    }

    public SelfIsolate() : base(0, CardType.Skill, CardRarity.Common, TargetType.Self) { }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(3);   // Mu: 5 → 8
        // 升级后去掉自施虚弱
        DynamicVars["WeakPower"].UpgradeValueBy(-1);  // 1 → 0
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        if (Forms.IsMortisForm(Owner))
        {
            await PlayerCmd.GainEnergy(DynamicVars.Energy.IntValue, Owner);
            var weak = (int)DynamicVars["WeakPower"].BaseValue;
            if (weak > 0)
                await Sts2Compat.PowerApply<WeakPower>(ctx, Owner.Creature, weak, Owner.Creature, this, false);
            await Forms.EnterMutsumi(Owner, this, ctx);
        }
        else
        {
            await CreatureCmd.GainBlock(Owner.Creature,
                DynamicVars.Block.BaseValue, ValueProp.Move, play, false);
        }
    }

    public override List<(string, string)>? Localization => LocManager.Instance.Language switch
    {
        "zhs" => new CardLoc("自闭",
            "{MuSec}{MuOpen}小睦{MuClose}：获得{Block:diff()}点[gold]格挡[/gold]。{MuSecEnd}\n" +
            "{MoSec}{MoOpen}小墨{MoClose}：获得{Energy:energyIcons()}。{IfUpgraded:show:|对自己施加{WeakPower}层[gold]虚弱[/gold]。}[gold]进入小睦[/gold]。{MoSecEnd}"),
        _ => new CardLoc("Self-Isolate",
            "{MuSec}{MuOpen}Mu{MuClose}: Gain {Block:diff()} [gold]Block[/gold].{MuSecEnd}\n" +
            "{MoSec}{MoOpen}Mo{MoClose}: Gain {Energy:energyIcons()}.{IfUpgraded:show:| Apply {WeakPower} [gold]Weak[/gold] to yourself.} [gold]Enter Mu[/gold].{MoSecEnd}"),
    };
}
