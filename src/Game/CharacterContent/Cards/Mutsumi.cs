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
/// 睦头人：1 费蓝色技能。（原名「Killkiss」）
///   小睦：获得 7 格挡 + 进入小墨（升级：额外本回合 +2 力量）。
///   小墨：对全体敌人施加 1/2 虚弱，本回合 +3 敏捷，进入小睦。
/// </summary>
[Pool(typeof(MzmCharCardPool))]
public class Mutsumi : MzmCharBaseCard
{
    public override string PortraitPath => "res://MzmChar/cards/mutsumi.png";

    private readonly List<DynamicVar> _vars = new()
    {
        new BlockVar(7, ValueProp.Move),
        new DynamicVar("MuStr", 0m),    // base 0；升级 +2
        new DynamicVar("MoDex", 3m),
        new DynamicVar("MoWeak", 1m),   // base 1；升级 +1
    };
    protected override IEnumerable<DynamicVar> CanonicalVars => _vars;

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            foreach (var t in FormTooltips.BothEnter()) yield return t;
            yield return HoverTipFactory.FromPower<StrengthPower>();
            yield return HoverTipFactory.FromPower<DexterityPower>();
            yield return HoverTipFactory.FromPower<WeakPower>();
        }
    }

    public Mutsumi() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }

    protected override void OnUpgrade()
    {
        DynamicVars["MuStr"].UpgradeValueBy(2);            // 0 → 2
        DynamicVars["MoWeak"].UpgradeValueBy(1);           // 1 → 2
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        if (Forms.IsMortisForm(Owner))
        {
            var cs = Owner.Creature.CombatState;
            if (cs != null)
            {
                foreach (var e in cs.HittableEnemies)
                    await Sts2Compat.PowerApply<WeakPower>(ctx, e, DynamicVars["MoWeak"].BaseValue, Owner.Creature, this, false);
            }
            var dex = DynamicVars["MoDex"].BaseValue;
            await Sts2Compat.PowerApply<DexterityPower>(ctx, Owner.Creature, dex, Owner.Creature, this, false);
            await Sts2Compat.PowerApply<TempDexterityPower>(ctx, Owner.Creature, dex, Owner.Creature, this, true);
            await CombatCounters.BumpMortisCard(ctx, Owner);
            await Forms.EnterMutsumi(Owner, this, ctx);
        }
        else
        {
            await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block.BaseValue, ValueProp.Move, play, false);
            var str = DynamicVars["MuStr"].BaseValue;
            if (str > 0)
            {
                await Sts2Compat.PowerApply<StrengthPower>(ctx, Owner.Creature, str, Owner.Creature, this, false);
                await Sts2Compat.PowerApply<TempStrengthPower>(ctx, Owner.Creature, str, Owner.Creature, this, true);
            }
            await CombatCounters.BumpMutsumiCard(ctx, Owner);
            await Forms.EnterMortis(Owner, this, ctx);
        }
    }

    public override List<(string, string)>? Localization => LocManager.Instance.Language switch
    {
        "zhs" => new CardLoc("睦头人",
            "{MuSec}{MuOpen}小睦{MuClose}：获得{Block:diff()}点[gold]格挡[/gold]。{IfUpgraded:show:本回合获得{MuStr:diff()}点[gold]力量[/gold]。|}[gold]进入小墨[/gold]。{MuSecEnd}\n" +
            "{MoSec}{MoOpen}小墨{MoClose}：给所有敌人施加{MoWeak:diff()}层[gold]虚弱[/gold]。本回合获得{MoDex}点[gold]敏捷[/gold]。[gold]进入小睦[/gold]。{MoSecEnd}"),
        _ => new CardLoc("Mutsumi",
            "{MuSec}{MuOpen}Mu{MuClose}: Gain {Block:diff()} [gold]Block[/gold].{IfUpgraded:show: This turn gain {MuStr:diff()} [gold]Strength[/gold].|} [gold]Enter Mo[/gold].{MuSecEnd}\n" +
            "{MoSec}{MoOpen}Mo{MoClose}: Apply {MoWeak:diff()} [gold]Weak[/gold] to ALL enemies; this turn gain {MoDex} [gold]Dexterity[/gold]. [gold]Enter Mu[/gold].{MoSecEnd}"),
    };
}
