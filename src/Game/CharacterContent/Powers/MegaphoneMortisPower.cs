using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;

namespace MzmChar.Game;

/// <summary>
/// 「传话筒」小墨版 buff：回合开始时，手牌中随机一张牌本回合变0费。
/// 用 card.EnergyCost.SetThisTurnOrUntilPlayed(0, true) —— 干净的内置 API。
/// </summary>
public class MegaphoneMortisPower : CustomPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override string? CustomPackedIconPath => "res://MzmChar/powers/megaphone_mo.png";
    public override string? CustomBigIconPath => "res://MzmChar/powers/megaphone_mo.png";

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext ctx, Player player)
    {
        if (player.Creature != Owner) return;

        var hand = PileType.Hand.GetPile(player);
        // 排除 X 费牌；排除已经是 0 费的牌（spec：不能把 0 费牌再变成 0 费）
        var cards = hand.Cards
            .Where(c => !c.EnergyCost.CostsX && c.EnergyCost.GetResolved() > 0)
            .ToList();
        if (cards.Count == 0) return;

        var rng = player.RunState?.Rng?.CombatTargets;
        // 选 Amount 张（不超过剩余非 0 费数量），无重复
        int n = System.Math.Min(Amount, cards.Count);
        Flash();
        for (int i = 0; i < n; i++)
        {
            var picked = rng != null ? rng.NextItem(cards) : cards[0];
            if (picked == null) break;
            picked.EnergyCost.SetThisTurnOrUntilPlayed(0, reduceOnly: true);
            cards.Remove(picked);
        }
        await Task.CompletedTask;
    }

    public override List<(string, string)>? Localization => LocManager.Instance.Language switch
    {
        "zhs" => new PowerLoc("传话筒（墨）",
            "回合开始时，手牌中随机的非0费的牌本回合变0费。",
            "回合开始时，手牌中随机{Amount}张非0费的牌本回合变0费。"),
        _ => new PowerLoc("Megaphone (Mo)",
            "At the start of your turn, random non-zero-cost cards in your hand cost 0 this turn.",
            "At the start of your turn, {Amount} random non-zero-cost cards in your hand cost 0 this turn."),
    };
}
