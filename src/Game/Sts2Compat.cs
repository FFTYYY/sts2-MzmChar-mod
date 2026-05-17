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
/// 跨 STS2 版本的 API wrapper。把"stable 跟 beta 之间签名不一样的 vanilla cmd / property"集中包成一组稳定调用。
///
/// 构建配置：
///   - `dotnet build` (Debug / Release) → 自动检测 release_info.json，v0.105+ 自动 define BETA
///   - `dotnet build -c Beta`           → 强制 define BETA（不读 release_info.json）
///
/// 已知 beta breaking changes（v0.105.1）：
///   - PowerCmd.Apply<T>      : 加 PlayerChoiceContext 第 1 参（必填）
///   - PowerCmd.ModifyAmount  : 加 PlayerChoiceContext 第 1 参（必填）
///   - CardPileCmd.AddGeneratedCard(s)ToCombat : 砍 addedByPlayer，加 Player creator 必填，加 CardPilePosition
///   - PowerModel.IsInstanced (bool) → PowerModel.InstanceType (PowerInstanceType enum)
///   - ModManifest schema      : dependencies 从 List&lt;string&gt; 改成 List&lt;ModDependency&gt;
///
/// 添加新 wrapper：probe 两版 vanilla 签名（用 _scratch/probe/Program.cs），在下方加 wrapper，
/// 内部用 #if BETA 分两段。
/// </summary>
public static class Sts2Compat
{
    // ── PowerCmd.Apply ───────────────────────────────────────────────────────
    public static Task PowerApply<T>(
        PlayerChoiceContext? ctx,
        Creature target,
        decimal amount,
        Creature applier,
        CardModel? cardSource,
        bool silent = false)
        where T : PowerModel, new()
    {
#if BETA
        return PowerCmd.Apply<T>(ctx!, target, amount, applier, cardSource, silent);
#else
        return PowerCmd.Apply<T>(target, amount, applier, cardSource, silent);
#endif
    }

    // ── PowerCmd.ModifyAmount ────────────────────────────────────────────────
    public static Task PowerModifyAmount(
        PlayerChoiceContext? ctx,
        PowerModel power,
        decimal offset,
        Creature applier,
        CardModel? cardSource,
        bool silent = false)
    {
#if BETA
        return PowerCmd.ModifyAmount(ctx!, power, offset, applier, cardSource, silent);
#else
        return PowerCmd.ModifyAmount(power, offset, applier, cardSource, silent);
#endif
    }

    // ── CardPileCmd.AddGeneratedCardToCombat (single) ────────────────────────
    // stable: AddGeneratedCardToCombat(card, pile, addedByPlayer, position)
    // beta:   AddGeneratedCardToCombat(card, pile, Player creator, position = Bottom)
    public static Task AddGeneratedCardToCombat(
        CardModel card,
        PileType newPileType,
        Player creator,
        CardPilePosition position = CardPilePosition.Bottom,
        bool addedByPlayer = true)
    {
#if BETA
        return CardPileCmd.AddGeneratedCardToCombat(card, newPileType, creator, position);
#else
        return CardPileCmd.AddGeneratedCardToCombat(card, newPileType, addedByPlayer, position);
#endif
    }

    // ── CardPile.MaxCardsInHand ──────────────────────────────────────────────
    // stable: CardPile.maxCardsInHand (小写)
    // beta:   CardPile.MaxCardsInHand (大写)
    public static int MaxCardsInHand =>
#if BETA
        CardPile.MaxCardsInHand;
#else
        CardPile.maxCardsInHand;
#endif

    // ── CardPileCmd.AddGeneratedCardsToCombat (plural) ───────────────────────
    public static Task AddGeneratedCardsToCombat(
        IEnumerable<CardModel> cards,
        PileType newPileType,
        Player creator,
        CardPilePosition position = CardPilePosition.Bottom,
        bool addedByPlayer = true)
    {
#if BETA
        return CardPileCmd.AddGeneratedCardsToCombat(cards, newPileType, creator, position);
#else
        return CardPileCmd.AddGeneratedCardsToCombat(cards, newPileType, addedByPlayer: addedByPlayer);
#endif
    }
}
