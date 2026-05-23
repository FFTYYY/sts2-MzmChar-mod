using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace MzmChar.Game;

/// <summary>
/// 隐藏计数器 façade。**全部**底层用 vanilla power 存（自动多人同步），
/// 见 `Powers/CounterPowers/Mzm*Power.cs`。
///
/// 历史：早期版本用 BaseLib `SpireField` 包装 `ConditionalWeakTable` 存计数。问题：SpireField
/// 是 per-client 本地状态，**不参与 vanilla 多人协议同步**。多 MzmChar 玩家 + 长战斗下 client
/// 状态分叉。现在所有计数都改用 vanilla `PowerCmd.Apply<TCounterPower>` 走 GameAction 自动同步。
/// hidden power 通过 `IsVisibleInternal=false` 隐藏 UI buff 栏。
///
/// 当前**只保留有读取者的 3 个**计数器；之前的 per-combat 计数（MutsumiCardsThisCombat /
/// MortisCardsThisCombat）和 per-creature 计数（StruckByMortisThisTurn）因为没卡牌读取已经清理。
/// 将来需要时直接照样建一个 `Mzm*Power.cs` 即可。
///
/// | counter                     | hidden power                              | reader |
/// |---|---|---|
/// | 本回合 Mu 出牌数               | `MzmCharMutsumiCardsThisTurnPower`         | `MirrorDoll`（额外攻击次数）|
/// | 本回合 Mo 出牌数               | `MzmCharMortisCardsThisTurnPower`          | `Silence`（额外抽牌）|
/// | 本场战斗形态切换次数            | `MzmCharPersonaSwitchesThisCombatPower`    | `CryInRain` / `MultipleMonster` / `WakabaFortune` |
///
/// 用法：
///   await CombatCounters.BumpMutsumiCard(ctx, player);
///   int n  = CombatCounters.GetMutsumiCardsThisTurn(player);
///   int sw = CombatCounters.GetPersonaSwitchesThisCombat(player);
///
/// 回滚到 SpireField：见 `notes/rollback_spirefield_to_power.md`。
/// </summary>
public static class CombatCounters
{
    // ═══════════════ Writes ═══════════════

    /// <summary>「以小睦形态打出一张牌」的底层 power apply。**不要在卡内直接调** ——
    /// 用全局 hook `OnAfterCardPlayed` 自动按"卡开始时的形态"计入（包括 vanilla / 其它 mod 的卡）。</summary>
    public static async Task BumpMutsumiCard(PlayerChoiceContext ctx, Player p)
    {
        await Sts2Compat.PowerApply<MzmCharMutsumiCardsThisTurnPower>(ctx, p.Creature, 1, p.Creature, null, silent: true);
    }

    /// <summary>同上，小墨形态版本。</summary>
    public static async Task BumpMortisCard(PlayerChoiceContext ctx, Player p)
    {
        await Sts2Compat.PowerApply<MzmCharMortisCardsThisTurnPower>(ctx, p.Creature, 1, p.Creature, null, silent: true);
    }

    /// <summary>每次形态切换调（从 `Forms.EnterMutsumi/EnterMortis` 内部）。</summary>
    public static async Task BumpPersonaSwitch(PlayerChoiceContext ctx, Player p)
    {
        await Sts2Compat.PowerApply<MzmCharPersonaSwitchesThisCombatPower>(ctx, p.Creature, 1, p.Creature, null, silent: true);
    }

    // ═══════════════ 卡牌打出全局 hook（覆盖 vanilla + 其它 mod 卡）═══════════════
    //
    // 问题：之前每张我们的卡在 OnPlay 末尾显式调 BumpMu/MoCard，会漏掉 vanilla / 其它 mod 的卡
    //  （PandorasBox 加入的、变化牌产生的、MoveCard 选过来的等等）。Silence / MirrorDoll 这种
    //  reader 卡就少算了形态非我们卡的打出次数。
    //
    // 修法：在 SecondPersonaRelic / AncientSecondPersonaRelic 的 BeforeCardPlayed + AfterCardPlayed
    //  hook 里集中算。逻辑：
    //   1. BeforeCardPlayed 时 snapshot 形态到 _formSnapshot（无 ctx，sync）
    //   2. AfterCardPlayed 时读回 snapshot，用 hook 自带的 ctx Bump 对应 power（async）
    //
    // 为什么用 snapshot 而不是 AfterCardPlayed 当场判形态：
    //  - 卡的 OnPlay 内部可能切形态（如 BackPersona Mu 分支末尾 EnterMortis）
    //  - 玩家直觉「这张卡是以 Mu 形态打的」= 打牌**那一刻**的形态，不是结束时的形态
    //  - BeforeCardPlayed 时间点 = 打牌瞬间，最对
    //
    // 为什么用 ConditionalWeakTable：
    //  - 嵌套打牌（AutoPlay 触发的子 play）可能在 outer BeforeCardPlayed 跟 outer AfterCardPlayed 之间
    //    插入 inner Before / After 对，所以不能用单一 static field
    //  - 用 CardPlay 实例做 key → 每张 play 一份 snapshot，互不干扰
    //  - ConditionalWeakTable 让 CardPlay GC 后 entry 自动释放
    private static readonly ConditionalWeakTable<CardPlay, object> _formSnapshot = new();
    private static readonly object _wasMo = true, _wasMu = false;

    /// <summary>`SecondPersonaRelic.BeforeCardPlayed` / `AncientSecondPersonaRelic.BeforeCardPlayed` 调。
    /// snapshot 卡打出时的形态。owner = relic 的 Owner Player。</summary>
    public static void OnBeforeCardPlayed(Player owner, CardPlay cardPlay)
    {
        if (cardPlay?.Card?.Owner != owner) return;
        _formSnapshot.AddOrUpdate(cardPlay, Forms.IsMortisForm(owner) ? _wasMo : _wasMu);
    }

    /// <summary>`SecondPersonaRelic.AfterCardPlayed` / `AncientSecondPersonaRelic.AfterCardPlayed` 调。
    /// 读回 snapshot，按 snapshot 时的形态 Bump 对应 power。</summary>
    public static async Task OnAfterCardPlayed(PlayerChoiceContext ctx, Player owner, CardPlay cardPlay)
    {
        if (cardPlay?.Card?.Owner != owner) return;
        if (!_formSnapshot.TryGetValue(cardPlay, out var formObj)) return;
        _formSnapshot.Remove(cardPlay);
        if (ReferenceEquals(formObj, _wasMo))
            await BumpMortisCard(ctx, owner);
        else
            await BumpMutsumiCard(ctx, owner);
    }

    // ═══════════════ Reads ═══════════════

    /// <summary>读本回合小睦形态出牌数。canonical/战斗外 → 0。</summary>
    public static int GetMutsumiCardsThisTurn(Player? p)
    {
        if (p?.Creature == null) return 0;
        return p.Creature.GetPower<MzmCharMutsumiCardsThisTurnPower>()?.Amount is { } amt ? (int)amt : 0;
    }

    /// <summary>读本回合小墨形态出牌数。</summary>
    public static int GetMortisCardsThisTurn(Player? p)
    {
        if (p?.Creature == null) return 0;
        return p.Creature.GetPower<MzmCharMortisCardsThisTurnPower>()?.Amount is { } amt ? (int)amt : 0;
    }

    /// <summary>读本场战斗形态切换次数（per-combat 累计）。</summary>
    public static int GetPersonaSwitchesThisCombat(Player? p)
    {
        if (p?.Creature == null) return 0;
        return p.Creature.GetPower<MzmCharPersonaSwitchesThisCombatPower>()?.Amount is { } amt ? (int)amt : 0;
    }

    // ═══════════════ Resets ═══════════════

    /// <summary>
    /// 每回合结束（Player side）由 `SecondPersonaRelic.AfterSideTurnEnd` 调。
    /// **No-op**：per-turn 隐藏 power（MzmCharM[ou]CardsThisTurnPower）已在自己的
    /// `AfterSideTurnEndLate` 自移除。留 API 名字让 callsite 显式表达"回合末清理"的意图。
    /// </summary>
    public static void ResetThisTurn(Player p)
    {
        // intentional no-op
    }

    /// <summary>
    /// 战斗开始 (`AfterPlayerTurnStart` 第 1 回合) + 战斗结束 (`AfterCombatVictory`) 都调。
    /// 显式 Remove per-combat power 防残留（vanilla 战斗结束理应自动清，加一层保险）。
    /// 注：async，callsite 必须 await。
    /// </summary>
    public static async Task ResetThisCombat(PlayerChoiceContext? ctx, Player p)
    {
        var c = p?.Creature;
        if (c == null) return;
        if (c.HasPower<MzmCharPersonaSwitchesThisCombatPower>())
            await PowerCmd.Remove<MzmCharPersonaSwitchesThisCombatPower>(c);
        // per-turn powers 同样清一下（理论上已经自移除了，保险起见）
        if (c.HasPower<MzmCharMutsumiCardsThisTurnPower>())
            await PowerCmd.Remove<MzmCharMutsumiCardsThisTurnPower>(c);
        if (c.HasPower<MzmCharMortisCardsThisTurnPower>())
            await PowerCmd.Remove<MzmCharMortisCardsThisTurnPower>(c);
    }
}
