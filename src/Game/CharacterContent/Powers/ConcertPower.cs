using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Rooms;

namespace MzmChar.Game;

/// <summary>
/// 「演奏会」状态。回合开始时：
///   1. 跳过正常抽牌阶段（override ModifyHandDraw 返回 0）
///   2. 额外获得 1 点能量
///   3. 将所有「演奏」牌从牌堆任何位置移到手牌（参考 vanilla SummonForth，IL-verified
///      用 PlayerCombatState.AllCards + CardPileCmd.Add(PileType.Hand)）
///   4. 直到手牌达到上限 (CardPile.maxCardsInHand = 10)
/// 回合结束自我移除。
/// </summary>
public class ConcertPower : CustomPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    public override string? CustomPackedIconPath => "res://MzmChar/powers/concert.png";
    public override string? CustomBigIconPath => "res://MzmChar/powers/concert.png";

    // 跳过正常抽牌：把 turn-start draw 改成 0（参考 MindRotPower 模式）
    public override decimal ModifyHandDraw(Player player, decimal count)
    {
        if (Owner?.Player != player) return count;
        return 0;
    }

    // 回合开始：(1) 召出舞台聚光灯 VFX；(2) 额外给 1 点能量；(3) 把所有「演奏」牌从抽/弃/消耗/出牌等位置移到手牌
    public override async Task AfterPlayerTurnStartEarly(PlayerChoiceContext ctx, Player player)
    {
        if (player.Creature != Owner) return;
        if (player.PlayerCombatState == null) return;

        // 召出聚光灯 VFX（per-client 渲染，fade-in ~1.5s 后转 dim 持续）
        ConcertSpotlightVfx.SpawnFor(player.Creature);

        // 额外 1 费（演奏会专属）
        await PlayerCmd.GainEnergy(1, player);

        var hand = player.PlayerCombatState.Hand;
        int slots = Sts2Compat.MaxCardsInHand - hand.Cards.Count;
        if (slots <= 0) return;

        // 参考 SummonForth：PlayerCombatState.AllCards + filter `Pile.Type != Hand`
        var perfCards = player.PlayerCombatState.AllCards
            .Where(c => c.Pile?.Type != PileType.Hand
                        && c.Keywords != null
                        && c.Keywords.Contains(MzmCharKeywords.Perform))
            .ToList();

        if (perfCards.Count == 0) return;
        Flash();
        foreach (var card in perfCards)
        {
            if (slots <= 0) break;
            await CardPileCmd.Add(card, PileType.Hand, CardPilePosition.Top, this, false);
            slots--;
        }
    }

    // 0.106: AfterTurnEnd(ctx, side) → AfterSideTurnEnd(ctx, side, participants)
#if BETA
    public override async Task AfterSideTurnEnd(PlayerChoiceContext ctx, CombatSide side, IEnumerable<Creature> participants)
#else
    public override async Task AfterTurnEnd(PlayerChoiceContext ctx, CombatSide side)
#endif
    {
        if (side != Owner.Side) return;
        // 先让聚光灯淡出（0.5s 后自销毁，不 await，跟 Remove 并行进行）
        ConcertSpotlightVfx.DespawnFor(Owner);
        await PowerCmd.Remove<ConcertPower>(Owner);
    }

    // 战斗结束兜底：聚光灯 VFX 不应该跨场景留下
    // （即使 power 因为某些路径没有被 AfterSideTurnEnd 移除）
    public override Task AfterCombatEnd(CombatRoom room)
    {
        if (Owner != null) ConcertSpotlightVfx.DespawnFor(Owner);
        return Task.CompletedTask;
    }

    // power 被移除时（vanilla 死亡时会 auto-remove power，AfterRemoved 随之 fire）也清 VFX
    public override Task AfterRemoved(Creature oldOwner)
    {
        if (oldOwner != null) ConcertSpotlightVfx.DespawnFor(oldOwner);
        return Task.CompletedTask;
    }

    public override List<(string, string)>? Localization => LocManager.Instance.Language switch
    {
        "zhs" => new PowerLoc("演奏会",
            "一个特殊的额外回合。回合开始时，移除所有[gold]演艺热情[/gold]并跳过抽牌，额外获得1点能量，将所有[gold]演奏[/gold]牌移入手牌，不论其在何处。",
            "一个特殊的额外回合。回合开始时，移除所有[gold]演艺热情[/gold]并跳过抽牌，额外获得1点能量，将所有[gold]演奏[/gold]牌移入手牌，不论其在何处。"),
        _ => new PowerLoc("Concert",
            "A special extra turn. At turn start, remove all [gold]Performance Passion[/gold], skip drawing, gain 1 extra energy, and move all [gold]Perform[/gold] cards into your hand regardless of their location.",
            "A special extra turn. At turn start, remove all [gold]Performance Passion[/gold], skip drawing, gain 1 extra energy, and move all [gold]Perform[/gold] cards into your hand regardless of their location."),
    };
}
