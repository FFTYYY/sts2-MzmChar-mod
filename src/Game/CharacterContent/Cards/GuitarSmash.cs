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
/// 吉他打击：2/1 费攻击。双形态。升级仅 -1 费，数值不变。（曾用名「强力和弦」/「吉他重击」）
///   小睦：对目标施加 3 层虚弱，本回合获得 4 力量
///   小墨：造成 15 伤害，抽 2 张
/// </summary>
[Pool(typeof(MzmCharCardPool))]
public class GuitarSmash : MzmCharBaseCard
{
    public override string PortraitPath => "res://MzmChar/cards/guitar_smash.png";

    private readonly List<DynamicVar> _vars = new()
    {
        new DamageVar(15, ValueProp.Move),                   // Mo damage
        new DynamicVar("MuStr", 4m),                          // Mu: 本回合 4/6 力量
        new CardsVar(2),
        new PowerVar<WeakPower>(3),
    };
    protected override IEnumerable<DynamicVar> CanonicalVars => _vars;

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromPower<WeakPower>();
            yield return HoverTipFactory.FromPower<StrengthPower>();
        }
    }

    public GuitarSmash() : base(2, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy) { }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);                           // 2 → 1，数值不变
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        if (Forms.IsMortisForm(Owner))
        {
            if (play.Target != null)
            {
                await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                    .FromCard(this).Targeting(play.Target).Execute(ctx);
                CombatCounters.StruckByMortisThisTurn[play.Target]++;
            }
            await CardPileCmd.Draw(ctx, DynamicVars.Cards.BaseValue, Owner, false);
            await CombatCounters.BumpMortisCard(ctx, Owner);
        }
        else
        {
            await PlayCast();
            if (play.Target != null)
                await PowerCmd.Apply<WeakPower>(play.Target,
                    DynamicVars["WeakPower"].BaseValue, Owner.Creature, this, false);
            var str = DynamicVars["MuStr"].BaseValue;
            await PowerCmd.Apply<StrengthPower>(Owner.Creature, str, Owner.Creature, this, false);
            await PowerCmd.Apply<TempStrengthPower>(Owner.Creature, str, Owner.Creature, this, true);
            await CombatCounters.BumpMutsumiCard(ctx, Owner);
        }
    }

    public override List<(string, string)>? Localization => LocManager.Instance.Language switch
    {
        "zhs" => new CardLoc("吉他打击",
            "{MuSec}{MuOpen}小睦{MuClose}：施加{WeakPower}层[gold]虚弱[/gold]。本回合获得{MuStr:diff()}点[gold]力量[/gold]。{MuSecEnd}\n" +
            "{MoSec}{MoOpen}小墨{MoClose}：造成{Damage:diff()}点伤害。抽{Cards}张牌。{MoSecEnd}"),
        _ => new CardLoc("Guitar Strike",
            "{MuSec}{MuOpen}Mu{MuClose}: Apply {WeakPower} [gold]Weak[/gold] to the target; this turn gain {MuStr:diff()} [gold]Strength[/gold].{MuSecEnd}\n" +
            "{MoSec}{MoOpen}Mo{MoClose}: Deal {Damage:diff()} damage. Draw {Cards}.{MoSecEnd}"),
    };
}
