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
/// 监控：1 费蓝色技能。
///   小睦：获得等同于自身[gold]格挡[/gold]值的[gold]活力[/gold]。[gold]进入小墨[/gold]。
///         （升级后：下回合开始时格挡不消失）
///   小墨：对随机敌人造成 5/7 伤害，本回合获得 3/4 敏捷。[gold]进入小睦[/gold]。
/// </summary>
[Pool(typeof(MzmCharCardPool))]
public class Surveillance : MzmCharBaseCard
{
    public override string PortraitPath => "res://MzmChar/cards/surveillance.png";

    private readonly List<DynamicVar> _vars = new()
    {
        new DamageVar(5, ValueProp.Move),                    // Mo 伤害
        new DynamicVar("MoDex", 3m),
        // 实算：Mu 获得的活力 = 当前格挡（不带 modifier kind —— 活力本身不被 buff 修饰）
        new LambdaVar("MuVigorGain", card =>
        {
            if (card.Owner?.Creature == null) return 0;
            return card.Owner.Creature.Block;
        }),
    };
    protected override IEnumerable<DynamicVar> CanonicalVars => _vars;

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            foreach (var t in FormTooltips.BothEnter()) yield return t;
            yield return HoverTipFactory.FromPower<VigorPower>();
            yield return HoverTipFactory.FromPower<DexterityPower>();
            yield return HoverTipFactory.FromPower<BlockRetainTurnPower>();
        }
    }

    public Surveillance() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(2);              // Mo 5 → 7
        DynamicVars["MoDex"].UpgradeValueBy(1);            // Dex 3 → 4
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        if (Forms.IsMortisForm(Owner))
        {
            var cs = Owner.Creature.CombatState;
            if (cs != null && cs.HittableEnemies.Count > 0)
            {
                await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                    .FromCard(this).TargetingRandomOpponents(cs, true).Execute(ctx);
            }
            var dex = DynamicVars["MoDex"].BaseValue;
            await Sts2Compat.PowerApply<DexterityPower>(ctx, Owner.Creature, dex, Owner.Creature, this, false);
            await Sts2Compat.PowerApply<TempDexterityPower>(ctx, Owner.Creature, dex, Owner.Creature, this, true);
            await Forms.EnterMutsumi(Owner, this, ctx);
        }
        else
        {
            int blockAmt = Owner.Creature.Block;
            if (blockAmt > 0)
                await Sts2Compat.PowerApply<VigorPower>(ctx, Owner.Creature, blockAmt, Owner.Creature, this, false);

            // 升级后：下回合开始时格挡不消失
            if (IsUpgraded)
                await Sts2Compat.PowerApply<BlockRetainTurnPower>(ctx, Owner.Creature, 1, Owner.Creature, this, false);

            await Forms.EnterMortis(Owner, this, ctx);
        }
    }

    public override List<(string, string)>? Localization => LocManager.Instance.Language switch
    {
        "zhs" => new CardLoc("监控",
            "{MuSec}{MuOpen}小睦{MuClose}：获得等同于自身[gold]格挡[/gold]值的[gold]活力[/gold]（{MuVigorGain}[gold]活力[/gold]）。" +
            "{IfUpgraded:show:下回合开始时，[gold]格挡[/gold]不会消失。|}" +
            "[gold]进入小墨[/gold]。{MuSecEnd}\n" +
            "{MoSec}{MoOpen}小墨{MoClose}：对随机敌人造成{Damage:diff()}点伤害。本回合获得{MoDex:diff()}点[gold]敏捷[/gold]。[gold]进入小睦[/gold]。{MoSecEnd}"),
        _ => new CardLoc("Surveillance",
            "{MuSec}{MuOpen}Mu{MuClose}: Gain [gold]Vigor[/gold] equal to your current [gold]Block[/gold] ({MuVigorGain} [gold]Vigor[/gold]). " +
            "{IfUpgraded:show:[gold]Block[/gold] is not removed at start of next turn. |}" +
            "[gold]Enter Mo[/gold].{MuSecEnd}\n" +
            "{MoSec}{MoOpen}Mo{MoClose}: Deal {Damage:diff()} damage to a random enemy; this turn gain {MoDex:diff()} [gold]Dexterity[/gold]. [gold]Enter Mu[/gold].{MoSecEnd}"),
    };
}
