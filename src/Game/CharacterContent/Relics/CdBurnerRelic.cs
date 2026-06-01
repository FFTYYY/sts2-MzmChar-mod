using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Enchantments;
using MegaCrit.Sts2.Core.Nodes.CommonUi;

namespace MzmChar.Game;

/// <summary>
/// CD 刻录机（稀有）：战斗开始时，将 3 张随机[gold]演奏[/gold]牌加入抽牌堆。
/// 每张牌的**第一次打出免费**，之后恢复正常费用。所有 3 张牌都获得附魔[gold]迅捷[/gold] 1。
///
/// 触发时机判定：`_firedThisCombat` 私有 flag，`BeforeCombatStart` 重置，`AfterPlayerTurnStart`
/// check + set。这条路径 stable / beta 通用 —— stable v0.103 的 `PlayerCombatState` 没
/// `TurnNumber` 属性，所以不能用"判第 1 回合"的写法（beta v0.105+ 才有 TurnNumber）。
///
/// 「随机演奏」=从 MzmCharCardPool 的 unlocked cards 中过滤 Keywords.Contains(Perform) +
/// CanBeGeneratedInCombat 后随机 distinct 3 张。
///
/// **第一次免费的实现**：vanilla `card.EnergyCost.SetUntilPlayed(0, false)` 一行搞定。
///   LocalCostModifier 加一条 `Expiration=WhenPlayed`（IL: LocalCostModifierExpiration=4）：
///     - 跨回合保留（`EndOfTurnCleanup` 只清 EndOfTurn flag=2）→ 弃牌堆 / 抽牌堆始终显示 0
///     - 打出一次后 `AfterCardPlayedCleanup.RemoveAll(HasFlag(WhenPlayed))` 自动清掉 → 恢复 base
///   模式参考 vanilla `RocketPunch.AfterCardGeneratedForCombat`。
///   **历史坑**：星 cost 版 `CardModel.SetStarCostUntilPlayed(0)` 实测不生效，但那是 TemporaryStarCost
///   列表（另一套独立机制），能量 cost 走 LocalCostModifier 列表，跟星 cost 互不相干。
///
/// 附魔 = CardCmd.Enchant&lt;Swift&gt;(card, 1)。
/// 动画 + 随机洗入：参考 vanilla Undeath，SINGLE 版 AddGeneratedCardToCombat(Random) +
/// CardCmd.PreviewCardPileAdd(results, 2.2f, HorizontalLayout)。
/// </summary>
[Pool(typeof(MzmCharRelicPool))]
public class CdBurnerRelic : CustomRelicModel
{
    // 单战斗范围 flag —— BeforeCombatStart 重置，AfterPlayerTurnStart 一次性触发
    private bool _firedThisCombat;

    public override RelicRarity Rarity => RelicRarity.Rare;

    public override string PackedIconPath           => "res://MzmChar/relics/cd_burner.png";
    protected override string BigIconPath           => "res://MzmChar/relics/cd_burner.png";
    protected override string PackedIconOutlinePath => "res://MzmChar/relics/cd_burner.png";

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromKeyword(MzmCharKeywords.Perform);
        }
    }

    public override Task BeforeCombatStart()
    {
        _firedThisCombat = false;
        return Task.CompletedTask;
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext ctx, Player player)
    {
        if (player != Owner) return;
        if (_firedThisCombat) return;
        if (Owner?.Creature?.CombatState == null) return;
        _firedThisCombat = true;

        Flash();

        var pool = ModelDb.CardPool<MzmCharCardPool>();
        var unlock = Owner.UnlockState;
        var mpConstraint = Owner.RunState.CardMultiplayerConstraint;
        var candidates = pool.GetUnlockedCards(unlock, mpConstraint)
            .Where(c => c.Keywords != null
                        && c.Keywords.Contains(MzmCharKeywords.Perform)
                        && c.CanBeGeneratedInCombat)
            .ToList();
        if (candidates.Count == 0) return;

        var rng = Owner.RunState.Rng.CombatCardGeneration;
        var picked = new List<CardModel>();
        var pool2 = new List<CardModel>(candidates);
        int n = System.Math.Min(3, pool2.Count);
        for (int i = 0; i < n; i++)
        {
            var item = rng.NextItem(pool2);
            if (item == null) break;   // pool2 empty 时 NextItem 可能 null，理论不该到这里（上面有 candidates.Count 检查）
            picked.Add(item);
            pool2.Remove(item);
        }

        var combatState = Owner.Creature.CombatState;
        var results = new List<CardPileAddResult>();
        foreach (var template in picked)
        {
            var card = combatState.CreateCard(template, Owner);
            CardCmd.Enchant<Swift>(card, 1);
            var result = await Sts2Compat.AddGeneratedCardToCombat(
                card, PileType.Draw, Owner, CardPilePosition.Random);
            results.Add(result);
            // 首次免费：vanilla 一行 API。LocalCostModifier 加 WhenPlayed flag，
            // 跨回合保留显示 0，打出后 AfterCardPlayedCleanup 自动 RemoveAll 恢复 base
            (result.cardAdded ?? card).EnergyCost.SetUntilPlayed(0, false);
        }
        CardCmd.PreviewCardPileAdd(results, 2.2f, CardPreviewStyle.HorizontalLayout);
    }

    public override List<(string, string)>? Localization => LocManager.Instance.Language switch
    {
        "zhs" => new RelicLoc(
            Title:       "CD刻录机",
            Description: "战斗开始时，将3张随机[gold]演奏[/gold]牌加入抽牌堆。这些牌可以免费打出一次，并被[gold]附魔[/gold]：[purple]迅捷[/purple][blue]1[/blue]。",
            Flavor:      "录下今晚的演出，刻进薄薄的银碟里。"),
        _ => new RelicLoc(
            Title:       "CD Burner",
            Description: "At combat start, add 3 random [gold]Perform[/gold] cards to your draw pile. Each can be played once for free, and they are [gold]Enchanted[/gold] with [purple]Swift[/purple] [blue]1[/blue].",
            Flavor:      "Tonight's gig pressed into a thin silver disc."),
    };
}
