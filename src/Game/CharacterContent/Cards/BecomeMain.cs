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
using MegaCrit.Sts2.Core.ValueProps;

namespace MzmChar.Game;

/// <summary>
/// 成为本尊：1 费金色技能。消耗（升级去除消耗）。本回合无法切换人格。
///   小睦：获得 3 费。
///   小墨：造成 15 伤害，获得 15 格挡。
/// 升级仅去除消耗，所有数值不变。
/// </summary>
[Pool(typeof(MzmCharCardPool))]
public class BecomeMain : MzmCharBaseCard
{
    public override string PortraitPath => "res://MzmChar/cards/become_main.png";

    private readonly List<DynamicVar> _vars = new()
    {
        new DamageVar(15, ValueProp.Move),
        new BlockVar("MoBlock", 15m, ValueProp.Move),
        new EnergyVar(3),
    };
    protected override IEnumerable<DynamicVar> CanonicalVars => _vars;

    private readonly HashSet<CardKeyword> _keywords = new() { CardKeyword.Exhaust };
    public override IEnumerable<CardKeyword> CanonicalKeywords => _keywords;

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromPower<NoSwitchThisTurnPower>();
        }
    }

    // Mo 走随机敌人 → 不需要玩家点目标，TargetType.Self 给纯净 UX
    public BecomeMain() : base(1, CardType.Skill, CardRarity.Rare, TargetType.Self) { }

    protected override void OnUpgrade()
    {
        RemoveKeyword(CardKeyword.Exhaust);  // 升级仅去消耗，数值不变
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await PowerCmd.Apply<NoSwitchThisTurnPower>(Owner.Creature, 1, Owner.Creature, this, false);

        if (Forms.IsMortisForm(Owner))
        {
            var cs = Owner.Creature.CombatState;
            if (cs != null && cs.HittableEnemies.Count > 0)
            {
                var rng = Owner.RunState?.Rng?.CombatTargets;
                var target = rng != null ? rng.NextItem(cs.HittableEnemies) : cs.HittableEnemies[0];
                if (target != null)
                {
                    await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                        .FromCard(this).Targeting(target).Execute(ctx);
                    CombatCounters.StruckByMortisThisTurn[target]++;
                }
            }
            await CreatureCmd.GainBlock(Owner.Creature,
                DynamicVars["MoBlock"].BaseValue, ValueProp.Move, play, false);
            await CombatCounters.BumpMortisCard(ctx, Owner);
        }
        else
        {
            await PlayCast();
            await PlayerCmd.GainEnergy(DynamicVars.Energy.IntValue, Owner);
            await CombatCounters.BumpMutsumiCard(ctx, Owner);
        }
    }

    public override List<(string, string)>? Localization => LocManager.Instance.Language switch
    {
        "zhs" => new CardLoc("成为本尊",
            "本回合无法切换人格。\n" +
            "{MuSec}{MuOpen}小睦{MuClose}：获得{Energy:energyIcons()}。{MuSecEnd}\n" +
            "{MoSec}{MoOpen}小墨{MoClose}：对随机敌人造成{Damage:diff()}点伤害。获得{MoBlock:diff()}点[gold]格挡[/gold]。{MoSecEnd}"),
        _ => new CardLoc("Become Main",
            "Cannot switch persona this turn.\n" +
            "{MuSec}{MuOpen}Mu{MuClose}: Gain {Energy:energyIcons()}.{MuSecEnd}\n" +
            "{MoSec}{MoOpen}Mo{MoClose}: Deal {Damage:diff()} damage to a random enemy; gain {MoBlock:diff()} [gold]Block[/gold].{MoSecEnd}"),
    };
}
