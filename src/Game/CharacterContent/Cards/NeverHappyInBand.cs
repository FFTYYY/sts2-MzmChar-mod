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
/// 从来没觉得玩乐队开心过（蓝、新版）：1 费蓝色技能。
/// 把所有「演奏」(Perform 词条) 的牌变化为**MzmChar 牌池里的随机牌**。消耗。
/// 升级：变化后的牌为升级版。
///
/// 之前版本（已重命名为 Rebellion 叛逆）作用的是 Strike 标签卡 → 无色牌。
/// 这里换成「演奏词条卡 → MzmChar 池随机牌」。
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
        // 收集所有 piles（含消耗堆）里带 Perform 词条的卡
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

        if (Forms.IsMortisForm(Owner)) await CombatCounters.BumpMortisCard(ctx, Owner);
        else await CombatCounters.BumpMutsumiCard(ctx, Owner);
    }

    public override List<(string, string)>? Localization => LocManager.Instance.Language switch
    {
        "zhs" => new CardLoc("从来没觉得玩乐队开心过",
            "把所有具有[gold]演奏[/gold]词条的牌变化为{IfUpgraded:show:升级过的|}随机牌。"),
        _ => new CardLoc("Never Happy in a Band",
            "Transform all cards with the [gold]Perform[/gold] keyword into {IfUpgraded:show:upgraded |}random cards."),
    };
}
