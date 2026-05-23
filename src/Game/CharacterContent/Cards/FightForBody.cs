using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History;
using MegaCrit.Sts2.Core.Combat.History.Entries;
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
/// 争夺身体：2 费攻击（升级后 1 费）。
///   小睦：本回合每额外抽过 1 张牌，获得 4 点格挡。进入小墨。
///   小墨：本回合每额外抽过 1 张牌，造成 5 点伤害。进入小睦。
/// </summary>
[Pool(typeof(MzmCharCardPool))]
public class FightForBody : MzmCharBaseCard
{
    public override string PortraitPath => "res://MzmChar/cards/fight_for_body.png";

    private readonly List<DynamicVar> _vars;

    // Mu 只加格挡 → 不需要选目标；Mo 攻击需要选目标
    public override TargetType TargetType =>
        !IsCanonical && Owner != null && Forms.IsMutsumiForm(Owner)
            ? TargetType.Self
            : TargetType.AnyEnemy;

    public FightForBody() : base(2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
        _vars = new List<DynamicVar>
        {
            new DynamicVar("MuBlockPerDraw", 4m),
            new DynamicVar("MoDmgPerDraw", 5m),
            // 实时算 —— 两个 LambdaVar 都不门控当前形态，两边各自显示
            new LambdaVar("MuActual", card =>
                CountExtraDrawsThisTurn(card) * (int)card.DynamicVars["MuBlockPerDraw"].BaseValue,
                LambdaVar.ModifierKind.Block),
            new LambdaVar("MoActual", card =>
                CountExtraDrawsThisTurn(card) * (int)card.DynamicVars["MoDmgPerDraw"].BaseValue,
                LambdaVar.ModifierKind.Damage),
        };
    }

    protected override IEnumerable<DynamicVar> CanonicalVars => _vars;

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            foreach (var t in FormTooltips.BothEnter()) yield return t;
        }
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);   // 2 → 1 费
    }

    // 查询 vanilla CombatHistory 算"本回合非 hand-draw pipeline 抽过的牌数"。
    // 跟 vanilla DeathMarch 模式一致 (IL probed): CombatManager.Instance.History.Entries
    //   .OfType<CardDrawnEntry>()
    //   .Count(e => e.HappenedThisTurn(state) && e.Actor == ownerCreature && !e.FromHandDraw);
    // 替代了原来自维护的 CombatCounters.ExtraDrawsThisTurn SpireField (per-client，联机不同步)。
    private static int CountExtraDrawsThisTurn(CardModel card)
    {
        var owner = card.Owner;
        if (owner == null) return 0;
        var state = owner.Creature?.CombatState;
        if (state == null || !CombatManager.Instance.IsInProgress) return 0;
        var ownerCreature = owner.Creature;
        return CombatManager.Instance.History.Entries
            .OfType<CardDrawnEntry>()
            .Count(e => e.HappenedThisTurn(state)
                     && e.Actor == ownerCreature
                     && !e.FromHandDraw);
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        int extraDraws = CountExtraDrawsThisTurn(this);

        if (Forms.IsMortisForm(Owner))
        {
            int dmg = extraDraws * (int)DynamicVars["MoDmgPerDraw"].BaseValue;
            // 不 guard dmg > 0 —— extraDraws=0 时仍要让力量加成生效（framework 内部加 Str）
            if (play.Target != null)
            {
                await DamageCmd.Attack(dmg).FromCard(this).Targeting(play.Target).Execute(ctx);
            }
            await Forms.EnterMutsumi(Owner, this, ctx);
        }
        else
        {
            int block = extraDraws * (int)DynamicVars["MuBlockPerDraw"].BaseValue;
            // 不 guard block > 0 —— framework 对 ValueProp.Move 自动加 Dex
            await CreatureCmd.GainBlock(Owner.Creature, block, ValueProp.Move, play);
            await Forms.EnterMortis(Owner, this, ctx);
        }
    }

    public override List<(string, string)>? Localization => LocManager.Instance.Language switch
    {
        "zhs" => new CardLoc("争夺身体",
            "{MuSec}{MuOpen}小睦{MuClose}：获得{MuActual:diff()}点[gold]格挡[/gold]，[gold]进入小墨[/gold]。{MuSecEnd}{MoSec}{MoOpen}小墨{MoClose}：造成{MoActual:diff()}点伤害，[gold]进入小睦[/gold]。{MoSecEnd}\n" +
            "你在回合进行中每抽到1张牌，{MuSec}{MuOpen}小睦{MuClose}额外获得{MuBlockPerDraw}点[gold]格挡[/gold]；{MuSecEnd}{MoSec}{MoOpen}小墨{MoClose}额外造成{MoDmgPerDraw}点伤害{MoSecEnd}。"),
        _ => new CardLoc("Fight for Body",
            "{MuSec}{MuOpen}Mu{MuClose}: Gain {MuActual:diff()} [gold]Block[/gold]. [gold]Enter Mo[/gold].{MuSecEnd}{MoSec}{MoOpen}Mo{MoClose}: Deal {MoActual:diff()} damage. [gold]Enter Mu[/gold].{MoSecEnd}\n" +
            "For each card drawn this turn, {MuSec}{MuOpen}Mu{MuClose} gains {MuBlockPerDraw} extra [gold]Block[/gold]{MuSecEnd}{MoSec}{MoOpen}Mo{MoClose} deals {MoDmgPerDraw} extra damage{MoSecEnd}."),
    };
}
