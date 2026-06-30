using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.ValueProps;

namespace MzmChar.Game;

/// <summary>
/// 演出服（罕见）：进入演奏会的回合开始时，获得 18 点格挡。
///
/// 时机问题（IL-verified via CombatManager hook 顺序）：
///   - vanilla 顺序：SwitchFromPlayerToEnemySide.AfterTakingExtraTurn（这里 PerformancePassion
///     Apply ConcertPower）→ StartTurn.AfterBlockCleared（block 清零）→
///     SetupPlayerTurn.AfterPlayerTurnStart。
///   - 如果 hook 走 AfterPowerAmountChanged 监听 ConcertPower 0→正，会在 block 被清之前就给 block，
///     立刻被清掉 → 等于没给。
///   - 改为监听 AfterPlayerTurnStart + 检查 HasPower&lt;ConcertPower&gt;()：此时 block 已清，
///     给的 18 格挡能留下。
///
/// 不需要 SavedProperty flag：ConcertPower 是 Single-stack 且在自己的 AfterSideTurnEnd 自移除，
/// 所以一场战斗里每次 Concert 只占一个 player turn，AfterPlayerTurnStart 看到 Concert 时给一次 block，
/// 下个回合 Concert 已被移除，自然不再触发。
/// </summary>
[Pool(typeof(MzmCharRelicPool))]
public class StageOutfitRelic : CustomRelicModel
{
    public override RelicRarity Rarity => RelicRarity.Uncommon;

    public override string PackedIconPath           => "res://MzmChar/relics/stage_outfit.png";
    protected override string BigIconPath           => "res://MzmChar/relics/stage_outfit.png";
    protected override string PackedIconOutlinePath => "res://MzmChar/relics/stage_outfit.png";

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get { yield return HoverTipFactory.FromPower<ConcertPower>(); }
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext ctx, Player player)
    {
        if (player != Owner) return;
        if (Owner?.Creature == null) return;
        if (!Owner.Creature.HasPower<ConcertPower>()) return;

        Flash();
        // Unpowered：固定字面 block，不吃敏捷/Frail 等 modifier（参考 AddictionPower / NobleHousePower / MortisCardPower / TwinFormsPower 同 pattern）
        await CreatureCmd.GainBlock(Owner.Creature, 18, ValueProp.Move | ValueProp.Unpowered, null, false);
    }

    public override List<(string, string)>? Localization => LocManager.Instance.Language switch
    {
        "zhs" => new RelicLoc(
            Title:       "演出服",
            Description: "进入[gold]演奏会[/gold]时，获得18点[gold]格挡[/gold]。",
            Flavor:      "哥特风格的演出服。"),
        _ => new RelicLoc(
            Title:       "Stage Outfit",
            Description: "When you enter [gold]Concert[/gold], gain 18 [gold]Block[/gold].",
            Flavor:      "Gothic-style stage outfit."),
    };
}
