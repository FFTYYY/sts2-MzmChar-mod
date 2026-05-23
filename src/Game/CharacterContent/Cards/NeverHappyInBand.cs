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
/// 从来没觉得玩乐队开心过（蓝、新版）：1 费蓝色技能。消耗。
///   步骤 1：把消耗堆中所有[gold]演奏[/gold]牌移入抽牌堆（随机位置 = shuffle）
///   步骤 2：把所有[gold]演奏[/gold]牌（在所有 pile 中）变化为 MzmChar 牌池里的随机牌
///   升级：变化后的牌为升级版
///
/// 之前版本（已重命名为 Rebellion 叛逆）作用的是 Strike 标签卡 → 无色牌。
/// 这里换成「演奏词条卡 → MzmChar 池随机牌」+ 前置消耗堆回收。
///
/// 步骤 1 用 vanilla `CardPileCmd.Add(card, PileType.Draw, Random, null, false)`（参考
/// `notes/implementation_patterns.md`「选牌」一节：第 4 参 source 必须 `null`，不是 `this`）。
/// 步骤 2 沿用之前的 transform 逻辑（CombatState.CreateCard + CardCmd.Transform）。
/// </summary>
[Pool(typeof(MzmCharCardPool))]
public class NeverHappyInBand : MzmCharBaseCard
{
    public override string PortraitPath => "res://MzmChar/cards/never_happy_in_band.png";

    private readonly HashSet<CardKeyword> _keywords = new() { CardKeyword.Exhaust };
    public override IEnumerable<CardKeyword> CanonicalKeywords => _keywords;

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get { yield return HoverTipFactory.FromKeyword(MzmCharKeywords.Perform); }
    }

    public NeverHappyInBand() : base(1, CardType.Skill, CardRarity.Rare, TargetType.Self) { }

    protected override void OnUpgrade() { /* upgrade flag 控制 replacement upgrade */ }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await PlayCast();

        // ── 步骤 1：把消耗堆里的演奏牌移到抽牌堆（shuffle in = Random position）──
        // 先 .ToList() snapshot —— `ExhaustPile.Cards` 在 `CardPileCmd.Add` 时被 mutate
        // （卡从 exhaust 移走），原地遍历会报 collection modified
        var exhaustPile = PileType.Exhaust.GetPile(Owner);
        if (exhaustPile != null)
        {
            var fromExhaust = exhaustPile.Cards
                .Where(c => c.Keywords != null && c.Keywords.Contains(MzmCharKeywords.Perform))
                .ToList();
            foreach (var c in fromExhaust)
                await CardPileCmd.Add(c, PileType.Draw, CardPilePosition.Random, null, false);
        }

        // ── 步骤 2：收集所有 piles 里带 Perform 词条的卡，并变化 ──
        // 此时消耗堆已无演奏牌，但仍遍历 4 个 pile 防御性兜底（vanilla 边角可能在 Add 之后又入消耗）
        var performCards = new List<CardModel>();
        foreach (var pile in new[] { PileType.Hand, PileType.Discard, PileType.Draw, PileType.Exhaust })
        {
            var p = pile.GetPile(Owner);
            if (p == null) continue;
            foreach (var c in p.Cards.ToList())
                if (c.Keywords != null && c.Keywords.Contains(MzmCharKeywords.Perform))
                    performCards.Add(c);
        }

        // 候选池：MzmChar 牌池所有卡。按单机/联机过滤
        var pool = ModelDb.CardPool<MzmCharCardPool>();
        var allCards = pool?.AllCards;
        var rng = Owner.RunState?.Rng?.CombatCardSelection;
        bool isMultiplayer = Owner.RunState != null && Owner.RunState.Players.Count > 1;
        var allowed = allCards?.Where(c =>
            c.CanBeGeneratedInCombat   // ⚠️ 排除里人格/表人格/不和谐音/MutsumiCharge 等特殊卡
            && (isMultiplayer
                ? c.MultiplayerConstraint != CardMultiplayerConstraint.SingleplayerOnly
                : c.MultiplayerConstraint != CardMultiplayerConstraint.MultiplayerOnly)).ToList();

        if (rng != null && allowed != null && allowed.Count > 0)
        {
            foreach (var original in performCards)
            {
                var template = rng.NextItem(allowed);
                if (template == null) continue;
                var replacement = Owner.Creature.CombatState!.CreateCard(template, Owner);
                if (IsUpgraded) CardCmd.Upgrade(replacement, CardPreviewStyle.None);
                await CardCmd.Transform(original, replacement, CardPreviewStyle.MessyLayout);
            }
        }

    }

    public override List<(string, string)>? Localization => LocManager.Instance.Language switch
    {
        "zhs" => new CardLoc("从来没觉得玩乐队开心过",
            "将消耗堆中所有[gold]演奏[/gold]牌移入抽牌堆。把所有[gold]演奏[/gold]牌变化为{IfUpgraded:show:升级过的|}随机牌。"),
        _ => new CardLoc("Never Happy in a Band",
            "Move all [gold]Perform[/gold] cards from your exhaust pile to your draw pile. Transform all [gold]Perform[/gold] cards into {IfUpgraded:show:upgraded |}random cards."),
    };
}
