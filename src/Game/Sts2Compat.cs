using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace MzmChar.Game;

/// <summary>
/// vanilla cmd 集中入口。当 beta 又破坏某 cmd 签名时，只在对应 wrapper 内部包 #if BETA / #else
/// 分两段；调用方（业务文件）不动。门 rollback 步骤：notes/api_version_gating.md
/// </summary>
public static class Sts2Compat
{
    public static Task PowerApply<T>(
        PlayerChoiceContext? ctx,
        Creature target,
        decimal amount,
        Creature applier,
        CardModel? cardSource,
        bool silent = false)
        where T : PowerModel, new()
        => PowerCmd.Apply<T>(ctx!, target, amount, applier, cardSource, silent);

    public static Task PowerModifyAmount(
        PlayerChoiceContext? ctx,
        PowerModel power,
        decimal offset,
        Creature applier,
        CardModel? cardSource,
        bool silent = false)
        => PowerCmd.ModifyAmount(ctx!, power, offset, applier, cardSource, silent);

    // 返回 CardPileAddResult —— 想配合 CardCmd.PreviewCardPileAdd 显示
    // 「卡牌飞向 pile」的动画时需要这个 result（参考 vanilla Undeath）。
    public static Task<CardPileAddResult> AddGeneratedCardToCombat(
        CardModel card,
        PileType newPileType,
        Player creator,
        CardPilePosition position = CardPilePosition.Bottom,
        bool addedByPlayer = true)
        => CardPileCmd.AddGeneratedCardToCombat(card, newPileType, creator, position);

    public static int MaxCardsInHand => CardPile.MaxCardsInHand;

    public static Task AddGeneratedCardsToCombat(
        IEnumerable<CardModel> cards,
        PileType newPileType,
        Player creator,
        CardPilePosition position = CardPilePosition.Bottom,
        bool addedByPlayer = true)
        => CardPileCmd.AddGeneratedCardsToCombat(cards, newPileType, creator, position);
}
