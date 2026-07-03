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
///   小睦：施加[gold]监视[/gold]（本回合每次获得格挡同时获等量活力）。
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
    };
    protected override IEnumerable<DynamicVar> CanonicalVars => _vars;

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            // Mu 不再进入 Mo，只有 Mo→Mu 切换 → 仅 EnterMu
            foreach (var t in FormTooltips.EnterMu()) yield return t;
            yield return HoverTipFactory.FromPower<SurveillanceBuffPower>();
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
                    .FromCardCompat(this, play).TargetingRandomOpponents(cs, true).Execute(ctx);
            }
            var dex = DynamicVars["MoDex"].BaseValue;
            await Sts2Compat.PowerApply<DexterityPower>(ctx, Owner.Creature, dex, Owner.Creature, this, false);
            await Sts2Compat.PowerApply<TempDexterityPower>(ctx, Owner.Creature, dex, Owner.Creature, this, true);
            await Forms.EnterMutsumi(Owner, this, ctx);
        }
        else
        {
            await Sts2Compat.PowerApply<SurveillanceBuffPower>(ctx, Owner.Creature, 1, Owner.Creature, this, false);

            // 升级后：下回合开始时格挡不消失
            if (IsUpgraded)
                await Sts2Compat.PowerApply<BlockRetainTurnPower>(ctx, Owner.Creature, 1, Owner.Creature, this, false);

            // 不再进入小墨
        }
    }

    public override List<(string, string)>? Localization => LocManager.Instance.Language switch
    {
        "zhs" => new CardLoc("盯——",
            "{MuSec}{MuOpen}小睦{MuClose}：本回合内，你获得[gold]格挡[/gold]时，同时获得等量的[gold]活力[/gold]。" +
            "{IfUpgraded:show:下回合开始时，[gold]格挡[/gold]不会消失。|}{MuSecEnd}\n" +
            "{MoSec}{MoOpen}小墨{MoClose}：对随机敌人造成{Damage:diff()}点伤害。本回合获得{MoDex:diff()}点[gold]敏捷[/gold]。[gold]进入小睦[/gold]。{MoSecEnd}"),
        _ => new CardLoc("Surveillance",
            "{MuSec}{MuOpen}Mu{MuClose}: This turn, whenever you gain [gold]Block[/gold], gain that much [gold]Vigor[/gold]." +
            "{IfUpgraded:show: [gold]Block[/gold] is not removed at start of next turn.|}{MuSecEnd}\n" +
            "{MoSec}{MoOpen}Mo{MoClose}: Deal {Damage:diff()} damage to a random enemy. This turn, gain {MoDex:diff()} [gold]Dexterity[/gold]. [gold]Enter Mu[/gold].{MoSecEnd}"),
    };
}
