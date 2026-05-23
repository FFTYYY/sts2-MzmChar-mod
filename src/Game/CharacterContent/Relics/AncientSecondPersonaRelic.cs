using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace MzmChar.Game;

/// <summary>
/// 灵魂双生 (Soul Twin)：第二人格·先古版（TouchOfOrobas 替换得到）。
/// 战斗开始时，将一张已强化的[gold]表人格+[/gold]和一张已强化的[gold]里人格+[/gold]加入手牌。
///
/// 跟普通 relic 一样挂 `[Pool(typeof(MzmCharRelicPool))]` —— RelicModel.get_Pool() 反向查表
/// 在 hover/render 时会用，没有 pool 就抛 "Sequence contains no matching element"。
/// 用 RelicRarity.Starter 排除在普通 reward 之外（Starter 不会随机刷出）。
/// </summary>
[Pool(typeof(MzmCharRelicPool))]
public class AncientSecondPersonaRelic : CustomRelicModel
{
    [SavedProperty]
    private bool DidCombatStart { get; set; }

    public override RelicRarity Rarity => RelicRarity.Starter;

    public override string PackedIconPath           => "res://MzmChar/relics/soul_twin.png";
    protected override string BigIconPath           => "res://MzmChar/relics/soul_twin.png";
    protected override string PackedIconOutlinePath => "res://MzmChar/relics/soul_twin.png";

    public override async Task AfterCombatVictory(MegaCrit.Sts2.Core.Rooms.CombatRoom room)
    {
        Diag.Trace($"AncientSecondPersonaRelic[owner={Owner?.NetId}].AfterCombatVictory: start");
        DidCombatStart = false;
        if (Owner != null) await CombatCounters.ResetThisCombat(null, Owner);
        Diag.Trace($"AncientSecondPersonaRelic[owner={Owner?.NetId}].AfterCombatVictory: done");
    }

    public override Task AfterCombatEnd(MegaCrit.Sts2.Core.Rooms.CombatRoom room)
    {
        Diag.Trace($"AncientSecondPersonaRelic[owner={Owner?.NetId}].AfterCombatEnd: fired (alive={Owner?.Creature?.IsAlive})");
        return Task.CompletedTask;
    }

    public override Task BeforeDeath(Creature creature)
    {
        var ownerId = Owner?.NetId.ToString() ?? "?";
        var deadId = creature.Player?.NetId.ToString() ?? (creature.IsMonster ? "monster" : "?");
        Diag.Trace($"AncientSecondPersonaRelic[owner={ownerId}].BeforeDeath: dying={deadId} hp={creature.CurrentHp}");
        return Task.CompletedTask;
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner) return;
        if (DidCombatStart) return;
        if (Owner?.Creature?.CombatState == null) return;

        DidCombatStart = true;
        Flash();

        await CombatCounters.ResetThisCombat(choiceContext, player);
        await Forms.EnterMutsumi(player, null, choiceContext);

        var combatState = player.Creature.CombatState;
        if (combatState == null) return;
        var front = combatState.CreateCard(ModelDb.Card<FrontPersona>(), player);
        var back  = combatState.CreateCard(ModelDb.Card<BackPersona>(),  player);
        // 先古版：加入的两张牌已强化（Front+ / Back+）
        front.UpgradeInternal();
        front.FinalizeUpgradeInternal();
        back.UpgradeInternal();
        back.FinalizeUpgradeInternal();

        await Sts2Compat.AddGeneratedCardsToCombat(
            new List<CardModel> { front, back }, PileType.Hand, player, addedByPlayer: true);
    }

    // 0.106: AfterTurnEnd(ctx, side) → AfterSideTurnEnd(ctx, side, participants)
#if BETA
    public override Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
#else
    public override Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
#endif
    {
        var p = Owner;
        if (p != null && side == p.Creature.Side)
            CombatCounters.ResetThisTurn(p);
        return Task.CompletedTask;
    }

    // 诊断日志：见 SecondPersonaRelic.AfterDeath 注释。两版 relic 都挂同一个日志，覆盖 ancient 玩家
    public override Task AfterDeath(PlayerChoiceContext ctx, Creature creature, bool wasRemovalPrevented, float deathAnimLength)
    {
        var ownerId = Owner?.NetId.ToString() ?? "?";
        var deadId = creature.Player?.NetId.ToString() ?? (creature.IsMonster ? "monster" : "?");
        Diag.Trace($"AncientSecondPersonaRelic[owner={ownerId}].AfterDeath: observed dead={deadId} prevented={wasRemovalPrevented} animLen={deathAnimLength}");
        return Task.CompletedTask;
    }

    // 全局卡牌打出 hook —— 见 SecondPersonaRelic 同名 override 注释
    public override Task BeforeCardPlayed(CardPlay cardPlay)
    {
        if (Owner != null) CombatCounters.OnBeforeCardPlayed(Owner, cardPlay);
        return Task.CompletedTask;
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext ctx, CardPlay cardPlay)
    {
        if (Owner != null) await CombatCounters.OnAfterCardPlayed(ctx, Owner, cardPlay);
    }

    public override List<(string, string)>? Localization => LocManager.Instance.Language switch
    {
        "zhs" => new RelicLoc(
            Title:       "灵魂双生",
            Description: "战斗开始时，[gold]进入小睦[/gold]，并将一张[gold]表人格+[/gold]和一张[gold]里人格+[/gold]加入手牌。",
            Flavor:      "她不止两个，可能还有别的，沉睡在更古老的弦音里。"),
        _ => new RelicLoc(
            Title:       "Soul Twin",
            Description: "At combat start, enter [gold]Mu[/gold] form and add an upgraded [gold]Front Persona[/gold] and an upgraded [gold]Back Persona[/gold] to your hand.",
            Flavor:      "Two are not all. Older songs hold more."),
    };
}
