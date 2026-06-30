using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace MzmChar.Game;

/// <summary>
/// 庆功宴（商店专属）：每场战斗的第一次演奏会结束时，回复 8 点生命。
///
/// 实现关键（vanilla IL-probe 证据）：
///   ConcertPower 在自己的 AfterTurnEnd 里用 `PowerCmd.Remove&lt;ConcertPower&gt;(Owner)` 自移除。
///   probe `PowerCmd.&lt;Remove&gt;d__8.MoveNext` 显示该路径走的是 RemoveInternal + AfterRemoved，
///   **完全不触发 AbstractModel.AfterPowerAmountChanged 钩子**。
///   所以无法在 hook 里直接监听 "Concert 正→0"。
///
/// 解法：监听 Concert 被 Apply 的 0→正（PowerCmd.Apply 路径 IS 触发 hook，amount 是 delta），
/// 置一个 [SavedProperty] ConcertActiveThisTurn=true。然后在 AfterTurnEnd/AfterSideTurnEnd
/// 里如果 flag 为 true → 回血 + flag 复位。这等价于"本回合用过演奏会 → 回合结束时回血"。
///
/// 一战 once：[SavedProperty] _usedThisCombat。
/// </summary>
[Pool(typeof(MzmCharRelicPool))]
public class CelebrationBanquetRelic : CustomRelicModel
{
    [SavedProperty]
    private bool UsedThisCombat { get; set; }

    [SavedProperty]
    private bool ConcertActiveThisTurn { get; set; }

    public override RelicRarity Rarity => RelicRarity.Shop;

    public override string PackedIconPath           => "res://MzmChar/relics/celebration_banquet.png";
    protected override string BigIconPath           => "res://MzmChar/relics/celebration_banquet.png";
    protected override string PackedIconOutlinePath => "res://MzmChar/relics/celebration_banquet.png";

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get { yield return HoverTipFactory.FromPower<ConcertPower>(); }
    }

    public override Task AfterPowerAmountChanged(
        PlayerChoiceContext ctx, PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
    {
        if (UsedThisCombat) return Task.CompletedTask;
        if (Owner == null) return Task.CompletedTask;
        if (power.Owner != Owner.Creature) return Task.CompletedTask;
        if (power is not ConcertPower) return Task.CompletedTask;
        if (amount <= 0) return Task.CompletedTask;
        if (power.Amount != amount) return Task.CompletedTask;  // 等价 oldAmount == 0（0→正）

        ConcertActiveThisTurn = true;
        return Task.CompletedTask;
    }

    public override async Task AfterSideTurnEnd(PlayerChoiceContext ctx, CombatSide side, IEnumerable<Creature> participants)
    {
        if (Owner == null) return;
        if (side != Owner.Creature.Side) return;
        if (!ConcertActiveThisTurn) return;
        ConcertActiveThisTurn = false;
        if (UsedThisCombat) return;

        UsedThisCombat = true;
        Flash();
        await CreatureCmd.Heal(Owner.Creature, 8, false);
    }

    // 演奏会回合中途结束战斗时 AfterSideTurnEnd 不触发；这里兜底
    public override async Task AfterCombatVictory(CombatRoom room)
    {
        if (Owner == null) return;
        if (UsedThisCombat) return;
        if (!ConcertActiveThisTurn) return;

        UsedThisCombat = true;
        Flash();
        await CreatureCmd.Heal(Owner.Creature, 8, false);
    }

    // 每场战斗开始时重置 flag — 比 AfterCombatVictory 更鲁棒（覆盖逃跑/中途结束等情况）。
    public override Task BeforeCombatStart()
    {
        UsedThisCombat = false;
        ConcertActiveThisTurn = false;
        return Task.CompletedTask;
    }

    public override List<(string, string)>? Localization => LocManager.Instance.Language switch
    {
        "zhs" => new RelicLoc(
            Title:       "庆功宴",
            Description: "每场战斗的第一次[gold]演奏会[/gold]结束时，回复8点生命。",
            Flavor:      "那些美好的时光..."),
        _ => new RelicLoc(
            Title:       "After-Show Feast",
            Description: "At the end of the first [gold]Concert[/gold] each combat, heal 8 HP.",
            Flavor:      "Those were the good times..."),
    };
}
