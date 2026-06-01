using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
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
/// 雨中流泪：2 费攻击（蓝色，rare）。
/// 共同：本场战斗每切换过一次人格，本回合获得 1/2 力量（一次性结算）。
///   小睦：施加 1 层易伤，[gold]进入小墨[/gold]。
///   小墨：造成 5 点伤害。
/// </summary>
[Pool(typeof(MzmCharCardPool))]
public class CryInRain : MzmCharBaseCard
{
    public override string PortraitPath => "res://MzmChar/cards/cry_in_rain.png";

    private readonly List<DynamicVar> _vars;

    public CryInRain() : base(2, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
    {
        _vars = new List<DynamicVar>
        {
            new DamageVar(5, ValueProp.Move),                    // Mo damage（不升级）
            new PowerVar<VulnerablePower>(1),                    // Mu vuln（不升级）
            new DynamicVar("StrPerSwitch", 1m),                  // 1 / 2 升级
            // 实算：本场切换次数 × StrPerSwitch
            new LambdaVar("StrTotal", card =>
            {
                if (card.Owner == null) return 0;
                int sw = CombatCounters.GetPersonaSwitchesThisCombat(card.Owner);
                return sw * (int)card.DynamicVars["StrPerSwitch"].BaseValue;
            }),
        };
    }

    protected override IEnumerable<DynamicVar> CanonicalVars => _vars;

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            foreach (var t in FormTooltips.EnterMo()) yield return t;
            yield return HoverTipFactory.FromPower<StrengthPower>();
            yield return HoverTipFactory.FromPower<VulnerablePower>();
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars["StrPerSwitch"].UpgradeValueBy(1);   // 1 → 2
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        // 共同效果：本回合 +N 力量（N = switches × StrPerSwitch）
        int sw = CombatCounters.GetPersonaSwitchesThisCombat(Owner);
        int strGain = sw * (int)DynamicVars["StrPerSwitch"].BaseValue;
        if (strGain > 0)
        {
            await Sts2Compat.PowerApply<StrengthPower>(ctx, Owner.Creature, strGain, Owner.Creature, this, false);
            await Sts2Compat.PowerApply<TempStrengthPower>(ctx, Owner.Creature, strGain, Owner.Creature, this, true);
        }

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
            if (play.Target != null)
                await Sts2Compat.PowerApply<VulnerablePower>(ctx, play.Target,
                    DynamicVars["VulnerablePower"].BaseValue, Owner.Creature, this, false);
            await Forms.EnterMortis(Owner, this, ctx);
        }
    }

    public override List<(string, string)>? Localization => LocManager.Instance.Language switch
    {
        "zhs" => new CardLoc("雨中流泪",
            "本场战斗每切换过一次人格，本回合获得{StrPerSwitch:diff()}点[gold]力量[/gold]（{StrTotal}力量）。\n" +
            "{MuSec}{MuOpen}小睦{MuClose}：施加{VulnerablePower}层[gold]易伤[/gold]。[gold]进入小墨[/gold]。{MuSecEnd}\n" +
            "{MoSec}{MoOpen}小墨{MoClose}：造成{Damage:diff()}点伤害。{MoSecEnd}"),
        _ => new CardLoc("Crying in the Rain",
            "Per persona switch this combat, gain {StrPerSwitch:diff()} [gold]Strength[/gold] this turn (total {StrTotal}).\n" +
            "{MuSec}{MuOpen}Mu{MuClose}: Apply {VulnerablePower} [gold]Vulnerable[/gold]; [gold]Enter Mo[/gold].{MuSecEnd}\n" +
            "{MoSec}{MoOpen}Mo{MoClose}: Deal {Damage:diff()} damage.{MoSecEnd}"),
    };
}
