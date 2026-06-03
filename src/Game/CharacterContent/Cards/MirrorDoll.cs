using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Cards.Variables;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
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
/// 镜中人偶：2 费金色攻击。
///   小墨：造成 3/5 点伤害 2 次。本回合中小睦每打出过一张牌就额外攻击一次。进入小睦
///   小睦：获得 2 费，获得 6/9 格挡，进入小墨
/// </summary>
[Pool(typeof(MzmCharCardPool))]
public class MirrorDoll : MzmCharBaseCard
{
    public override string PortraitPath => "res://MzmChar/cards/mirror_doll.png";

    private readonly List<DynamicVar> _vars = new()
    {
        new DamageVar(3, ValueProp.Move),
        new DynamicVar("BaseHits", 2m),
        new EnergyVar(2),
        new BlockVar(6, ValueProp.Move),
        // 自定义 var：EnchantedValue = baseHits（基线 3）；PreviewValue = baseHits + 小睦出牌数
        // → {ActualHits:diff()} 在累计 > 3 时框架自动绿色染色
        new GrowingHitsVar(),
    };
    protected override IEnumerable<DynamicVar> CanonicalVars => _vars;

    /// <summary>baseHits 基线 + 本回合小睦出牌数 → diff 染色：超过基线绿色显示。</summary>
    private class GrowingHitsVar : DynamicVar
    {
        // 初始值 = baseHits（3）。卡库 / canonical 时若 UpdateCardPreview 不被调用，
        // 就保留这个 fallback 值显示，不会出现"0次"。
        public GrowingHitsVar() : base("ActualHits", 2) { }
        public override void UpdateCardPreview(CardModel card, CardPreviewMode mode, Creature? target, bool runGlobalHooks)
        {
            int baseHits = (int)card.DynamicVars["BaseHits"].BaseValue;
            int total = baseHits;
            if (card.Owner != null)
                total = baseHits + CombatCounters.GetMutsumiCardsThisTurn(card.Owner);
            BaseValue = total;            // 让 {ActualHits}（不带 :diff()）也显示当前值
            EnchantedValue = baseHits;    // diff 基线：原始攻击次数
            PreviewValue = total;         // 实际次数，比基线大→绿色
        }
    }

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get { foreach (var t in FormTooltips.BothEnter()) yield return t; }
    }

    public MirrorDoll() : base(2, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy) { }

    public override TargetType TargetType =>
        !IsCanonical && Owner != null && Forms.IsMutsumiForm(Owner)
            ? TargetType.Self
            : TargetType.AnyEnemy;

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(2);   // Mo: 3 → 5
        DynamicVars.Block.UpgradeValueBy(3);    // Mu: 6 → 9
        // Mu energy 不再升级（保持 2）
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        if (Forms.IsMortisForm(Owner))
        {
            int baseHits = (int)DynamicVars["BaseHits"].BaseValue;
            int extra = CombatCounters.GetMutsumiCardsThisTurn(Owner);
            int hits = baseHits + extra;
            // WithHitCount —— 力量/活力 modifier 应用每次 hit
            if (play.Target != null && hits > 0)
            {
                await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                    .FromCard(this).Targeting(play.Target).WithHitCount(hits).Execute(ctx);
            }
            await Forms.EnterMutsumi(Owner, this, ctx);
        }
        else
        {
            await PlayerCmd.GainEnergy(DynamicVars.Energy.IntValue, Owner);
            await CreatureCmd.GainBlock(Owner.Creature,
                DynamicVars.Block.BaseValue, ValueProp.Move, play, false);
            await Forms.EnterMortis(Owner, this, ctx);
        }
    }

    public override List<(string, string)>? Localization => LocManager.Instance.Language switch
    {
        "zhs" => new CardLoc("镜中人",
            "{MuSec}{MuOpen}小睦{MuClose}：获得{Energy:energyIcons()}，获得{Block:diff()}点[gold]格挡[/gold]。[gold]进入小墨[/gold]。{MuSecEnd}\n" +
            "{MoSec}{MoOpen}小墨{MoClose}：造成{Damage:diff()}点伤害{ActualHits:diff()}次。本回合中小睦每打出过一张牌，就额外攻击1次。[gold]进入小睦[/gold]。{MoSecEnd}"),
        _ => new CardLoc("Mirror Doll",
            "{MuSec}{MuOpen}Mu{MuClose}: Gain {Energy:energyIcons()}; gain {Block:diff()} [gold]Block[/gold]. [gold]Enter Mo[/gold].{MuSecEnd}\n" +
            "{MoSec}{MoOpen}Mo{MoClose}: Deal {Damage:diff()} damage {ActualHits:diff()} times. +1 hit for each Mu card played this turn. [gold]Enter Mu[/gold].{MoSecEnd}"),
    };
}
