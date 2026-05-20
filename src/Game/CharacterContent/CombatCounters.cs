using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace MzmChar.Game;

/// <summary>
/// 隐藏计数器（不显示在 buff 栏）。
///
/// 早期版本把这些做成 Counter buff，结果 buff 栏堆得很挤。改用 BaseLib 的 SpireField
/// （ConditionalWeakTable 包装）直接 attach 到 Player / Creature 上，UI 完全看不见。
///
/// 重置规则：
///   - per-turn 计数器（Mu/Mo cards、StruckByMortis）由 SecondPersonaRelic.AfterTurnEnd 清零
///   - PersonaSwitchesThisCombat 由 SecondPersonaRelic.AfterPlayerTurnStart 在战斗第一回合清零
///   - 战斗结束后所有 Player/Creature 引用还在（ConditionalWeakTable），但下一场战斗开始时同样会被清零
///
/// 注：「本回合额外抽过 N 张牌」类查询请用 vanilla `CombatManager.Instance.History.Entries
/// .OfType&lt;CardDrawnEntry&gt;().Count(e =&gt; e.HappenedThisTurn(state) &amp;&amp; e.Actor == owner.Creature &amp;&amp; !e.FromHandDraw)`，
/// 不要新建 SpireField counter（vanilla DeathMarch 模式，联机自动同步）。详见 FightForBody.cs。
///
/// 用法：
///   CombatCounters.MutsumiCardsThisTurn[player]++;          // 写
///   int n = CombatCounters.MutsumiCardsThisTurn[player];    // 读
///   CombatCounters.StruckByMortisThisTurn[enemyCreature]++; // 写敌人计数
/// </summary>
public static class CombatCounters
{
    public static readonly SpireField<Player, int>   MutsumiCardsThisTurn      = new(() => 0);
    public static readonly SpireField<Player, int>   MortisCardsThisTurn       = new(() => 0);
    public static readonly SpireField<Player, int>   MutsumiCardsThisCombat    = new(() => 0);
    public static readonly SpireField<Player, int>   MortisCardsThisCombat     = new(() => 0);
    public static readonly SpireField<Player, int>   PersonaSwitchesThisCombat = new(() => 0);
    public static readonly SpireField<Creature, int> StruckByMortisThisTurn    = new(() => 0);

    /// <summary>每回合结束清的：Mu/Mo 出牌数、敌人被小墨打次数。</summary>
    public static void ResetThisTurn(Player p)
    {
        MutsumiCardsThisTurn[p] = 0;
        MortisCardsThisTurn[p]  = 0;

        var cs = p.Creature.CombatState;
        if (cs == null) return;
        foreach (var e in cs.Enemies)
            StruckByMortisThisTurn[e] = 0;
    }

    /// <summary>战斗开始时清的（含 per-turn + 切换次数 + per-combat 出牌计数）。</summary>
    public static void ResetThisCombat(Player p)
    {
        ResetThisTurn(p);
        PersonaSwitchesThisCombat[p] = 0;
        MutsumiCardsThisCombat[p] = 0;
        MortisCardsThisCombat[p] = 0;
    }

    /// <summary>每次以小墨形态打出一张牌时调（Mo 分支 OnPlay 末尾用这个，不要直接 ++ 计数）。</summary>
    public static async Task BumpMortisCard(PlayerChoiceContext ctx, Player p)
    {
        MortisCardsThisTurn[p]++;
        MortisCardsThisCombat[p]++;
        await Task.CompletedTask;
    }

    /// <summary>每次以小睦形态打出一张牌时调（对应 Mu 分支）。</summary>
    public static async Task BumpMutsumiCard(PlayerChoiceContext ctx, Player p)
    {
        MutsumiCardsThisTurn[p]++;
        MutsumiCardsThisCombat[p]++;
        await Task.CompletedTask;
    }
}
