using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
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
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace MzmChar.Game;

/// <summary>
/// 合奏：1 费蓝色技能。联机专用。
///   所有队友（不含自己）进入小睦，并在其抽牌堆随机位置加入 (1/2 + PlayCount) 张随机演奏牌。
///   每打出一次 PlayCount +1（本战斗内累积）。死亡队友跳过（不切形态、不发牌）。
///
/// 多人遍历 + 给其他玩家加牌参 HeartResonance；随机演奏牌候选池参 CdBurnerRelic；
/// 战斗内 PlayCount 自增长 + 实算显示参 Madness。
/// </summary>
[Pool(typeof(MzmCharCardPool))]
public class Ensemble : MzmCharBaseCard
{
    public override string PortraitPath => "res://MzmChar/cards/ensemble.png";

    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;

    private const int BaseCountUnupgraded = 1;
    private const int UpgradeBonus = 1;

    // 本战斗内累积：每打出一次 +1（参 Madness.PlayCount 同模式）。
    // SavedProperty 让战斗中途存读档不丢计数；新战斗实例重建自然归零。
    [SavedProperty] public int PlayCount { get; set; }

    private readonly List<DynamicVar> _vars = new() { new GrowingPerformVar() };
    protected override IEnumerable<DynamicVar> CanonicalVars => _vars;

    /// <summary>演奏牌数 = base(1 或升级后 2) + PlayCount。参 Madness.GrowingCardsVar。</summary>
    private class GrowingPerformVar : DynamicVar
    {
        public GrowingPerformVar() : base("PerformCards", BaseCountUnupgraded) { }
        public override void UpdateCardPreview(CardModel card, CardPreviewMode mode, Creature? target, bool runGlobalHooks)
        {
            int baseCount = BaseCountUnupgraded + (card.IsUpgraded ? UpgradeBonus : 0);
            int total = baseCount + (card is Ensemble e ? e.PlayCount : 0);
            BaseValue = total;                    // 让 {PerformCards}（不带 :diff()）也显示当前值
            EnchantedValue = BaseCountUnupgraded;      // 基线 = 1 → 升级 +1 / PlayCount 增长 都通过 :diff() 显绿
            PreviewValue = total;
        }
    }

    // 描述有「进入小睦」+「演奏」 → 挂对应 tip
    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            foreach (var t in FormTooltips.EnterMu()) yield return t;
            yield return HoverTipFactory.FromKeyword(MzmCharKeywords.Perform);
        }
    }

    public Ensemble() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }

    protected override void OnUpgrade()
    {
        DynamicVars["PerformCards"].UpgradeValueBy(UpgradeBonus);  // 1 → 2（实算时走 IsUpgraded 重算）
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await PlayCast();

        int count = BaseCountUnupgraded + (IsUpgraded ? UpgradeBonus : 0) + PlayCount;
        PlayCount++;

        if (Owner?.RunState == null) return;
        var rng = Owner.RunState.Rng?.CombatCardGeneration;
        if (rng == null) return;

        // 候选池：演奏牌 + 可战斗内生成（CdBurnerRelic 同过滤）
        var pool = ModelDb.CardPool<MzmCharCardPool>();
        var candidates = pool.GetUnlockedCards(Owner.UnlockState, Owner.RunState.CardMultiplayerConstraint)
            .Where(c => c.Keywords != null
                        && c.Keywords.Contains(MzmCharKeywords.Perform)
                        && c.CanBeGeneratedInCombat)
            .ToList();

        var allResults = new List<CardPileAddResult>();
        foreach (var p in Owner.RunState.Players)
        {
            if (p == Owner) continue;                     // 「队友」不含自己
            if (p?.Creature?.CombatState == null) continue;
            if (!p.Creature.IsAlive) continue;            // 死亡队友跳过

            await Forms.EnterMutsumi(p, this, ctx);

            if (candidates.Count == 0) continue;
            // 每个队友独立随机；单个队友内不重复（count 超过候选数时上限 = 候选数）
            var pickPool = new List<CardModel>(candidates);
            int n = System.Math.Min(count, pickPool.Count);
            for (int i = 0; i < n; i++)
            {
                var template = rng.NextItem(pickPool);
                if (template == null) break;
                pickPool.Remove(template);
                var card = p.Creature.CombatState.CreateCard(template, p);
                if (card == null) continue;
                var result = await Sts2Compat.AddGeneratedCardToCombat(
                    card, PileType.Draw, p, CardPilePosition.Random);
                allResults.Add(result);
            }
        }

        if (allResults.Count > 0)
            CardCmd.PreviewCardPileAdd(allResults, 2.0f, CardPreviewStyle.HorizontalLayout);
    }

    public override List<(string, string)>? Localization => LocManager.Instance.Language switch
    {
        "zhs" => new CardLoc("合奏",
            "所有队友[gold]进入小睦[/gold]。在所有队友的抽牌堆中加入{PerformCards:diff()}张随机[gold]演奏[/gold]牌。\n这张牌每被打出一次，生成的[gold]演奏[/gold]牌数量增加1。"),
        _ => new CardLoc("Ensemble",
            "All allies [gold]Enter Mu[/gold]. Add {PerformCards:diff()} random [gold]Perform[/gold] cards to each ally's draw pile. \nEach time this card is played, add 1 more [gold]Perform[/gold] card."),
    };
}
