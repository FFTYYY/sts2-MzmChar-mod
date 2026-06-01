using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;

namespace MzmChar.Game;

/// <summary>
/// 心灵共鸣：2/1 费蓝色技能。消耗。联机专用。
///   所有玩家进入小睦。在所有玩家的手牌、抽牌堆、弃牌堆**各**随机添加一张若叶睦的卡牌
///   （每玩家共 3 张，每个 pile 一张）。这些牌第一次打出免费。
///
/// 多人模式参考 BullyingYou（CardMultiplayerConstraint.MultiplayerOnly + 遍历
/// Owner.RunState.Players）。
/// 随机印牌过滤参考 InnerNoise（CanBeGeneratedInCombat + MultiplayerConstraint check）。
/// 首次免费走 vanilla `card.EnergyCost.SetUntilPlayed(0, false)` —— LocalCostModifier
/// 加一条 `Expiration=WhenPlayed`，跨回合保留，打出后由 `AfterCardPlayedCleanup.RemoveAll`
/// 自动清掉。模式参考 vanilla `RocketPunch.AfterCardGeneratedForCombat`。
/// 加牌动画走 SINGLE 版 AddGeneratedCardToCombat 收集 CardPileAddResult + CardCmd.PreviewCardPileAdd
/// （vanilla Undeath 模式）。
/// </summary>
[Pool(typeof(MzmCharCardPool))]
public class HeartResonance : MzmCharBaseCard
{
    public override string PortraitPath => "res://MzmChar/cards/heart_resonance.png";

    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;

    private readonly HashSet<CardKeyword> _keywords = new() { CardKeyword.Exhaust };
    public override IEnumerable<CardKeyword> CanonicalKeywords => _keywords;

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            foreach (var t in FormTooltips.BothEnter()) yield return t;
        }
    }

    public HeartResonance() : base(2, CardType.Skill, CardRarity.Rare, TargetType.Self) { }

    protected override void OnUpgrade() { EnergyCost.UpgradeBy(-1); /* 2 → 1 */ }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        // 不 PlayCast —— 自己有 EnterMutsumi 会切人格触发形态动画，cast 会冲突
        // （MzmChar 卡通用原则）
        if (Owner?.RunState == null) return;
        var rng = Owner.RunState.Rng?.CombatCardSelection;
        if (rng == null) return;

        // 候选池：MzmCharCardPool 全部牌中可在战斗内生成 + 联机模式匹配
        var pool = ModelDb.CardPool<MzmCharCardPool>();
        var allCards = pool?.AllCards;
        bool isMultiplayer = Owner.RunState.Players.Count > 1;
        var candidates = allCards?.Where(c =>
            c.CanBeGeneratedInCombat
            && (isMultiplayer
                ? c.MultiplayerConstraint != CardMultiplayerConstraint.SingleplayerOnly
                : c.MultiplayerConstraint != CardMultiplayerConstraint.MultiplayerOnly))
            .ToList() ?? new List<CardModel>();
        if (candidates.Count == 0) return;

        var pileTypes = new[] { PileType.Hand, PileType.Draw, PileType.Discard };
        var allResults = new List<CardPileAddResult>();

        foreach (var p in Owner.RunState.Players)
        {
            if (p?.Creature?.CombatState == null) continue;

            // 1) 所有玩家进入小睦
            await Forms.EnterMutsumi(p, this, ctx);

            // 2) 每个 pile 各 1 张若叶睦卡（每玩家共 3 张）
            foreach (var pileType in pileTypes)
            {
                var template = rng.NextItem(candidates);
                if (template == null) continue;
                var card = p.Creature.CombatState.CreateCard(template, p);
                if (card == null) continue;
                var pos = pileType == PileType.Draw ? CardPilePosition.Random : CardPilePosition.Top;
                var result = await Sts2Compat.AddGeneratedCardToCombat(card, pileType, p, pos);
                // 加到 HAND 的不进 preview list —— 玩家已经直接看到，不需要"飞向 pile"动画
                if (pileType != PileType.Hand)
                    allResults.Add(result);

                // 3) 首次免费：一行 vanilla API。LocalCostModifier 加 WhenPlayed flag，
                //    跨回合保留显示 0，打出后 AfterCardPlayedCleanup 自动 RemoveAll 恢复 base
                var actualCard = result.cardAdded ?? card;
                actualCard.EnergyCost.SetUntilPlayed(0, false);
            }
        }

        // 缩短 preview 时长（vanilla Undeath 用 2.2s，这里 1.0s 已经够看清）。
        if (allResults.Count > 0)
            CardCmd.PreviewCardPileAdd(allResults, 1.0f, CardPreviewStyle.HorizontalLayout);
    }

    public override List<(string, string)>? Localization => LocManager.Instance.Language switch
    {
        "zhs" => new CardLoc("心灵共鸣",
            "所有玩家[gold]进入小睦[/gold]。在所有玩家的手牌、抽牌堆、弃牌堆各随机添加一张[gold]若叶睦[/gold]的卡牌。这些牌第一次打出免费。"),
        _ => new CardLoc("Heart Resonance",
            "All players [gold]Enter Mu[/gold]. Add a random [gold]Mutsumi[/gold] card to each player's hand, draw pile, and discard pile. These cards can be played once for free."),
    };
}
