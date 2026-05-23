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
/// 第二人格（初始遗物）：
///   1) 战斗第 1 回合开始时，自动应用「小睦」buff（保证两个形态 buff 始终有且只有一个）
///   2) 把「表人格」和「里人格」各一张加进手牌
///   3) 充当 CombatCounters 的 reset 钩子（per-turn / per-combat）
/// </summary>
[Pool(typeof(MzmCharRelicPool))]
public class SecondPersonaRelic : CustomRelicModel
{
    [SavedProperty]
    private bool DidCombatStart { get; set; }

    public override RelicRarity Rarity => RelicRarity.Starter;

    public override string PackedIconPath           => "res://MzmChar/relics/second_persona.png";
    protected override string BigIconPath           => "res://MzmChar/relics/second_persona.png";
    protected override string PackedIconOutlinePath => "res://MzmChar/relics/second_persona.png";

    public override async Task AfterCombatVictory(MegaCrit.Sts2.Core.Rooms.CombatRoom room)
    {
        Diag.Trace($"SecondPersonaRelic[owner={Owner?.NetId}].AfterCombatVictory: start");
        DidCombatStart = false;
        // 战斗结束 reset 计数器（power 形式现在 → PowerCmd.Remove 是 async，方法签名跟着 async）
        // 理论上 vanilla 战斗结束自动清所有 power，这里加一层保险
        if (Owner != null) await CombatCounters.ResetThisCombat(null, Owner);
        Diag.Trace($"SecondPersonaRelic[owner={Owner?.NetId}].AfterCombatVictory: done");
    }

    // AfterCombatEnd 在赢 / 输 / 中途退出 都触发 —— 用来追多人同死后的链路是否走到这
    public override Task AfterCombatEnd(MegaCrit.Sts2.Core.Rooms.CombatRoom room)
    {
        Diag.Trace($"SecondPersonaRelic[owner={Owner?.NetId}].AfterCombatEnd: fired (alive={Owner?.Creature?.IsAlive})");
        return Task.CompletedTask;
    }

    public override Task BeforeDeath(Creature creature)
    {
        var ownerId = Owner?.NetId.ToString() ?? "?";
        var deadId = creature.Player?.NetId.ToString() ?? (creature.IsMonster ? "monster" : "?");
        Diag.Trace($"SecondPersonaRelic[owner={ownerId}].BeforeDeath: dying={deadId} hp={creature.CurrentHp}");
        return Task.CompletedTask;
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner) return;
        if (DidCombatStart) return;
        if (Owner?.Creature?.CombatState == null) return;

        DidCombatStart = true;
        Flash();

        Diag.Trace($"SecondPersonaRelic.AfterPlayerTurnStart: combat first-turn init for player {player.NetId}");

        // 一次性 per-combat 初始化
        await CombatCounters.ResetThisCombat(choiceContext, player);
        await Forms.EnterMutsumi(player, null, choiceContext);

        var combatState = player.Creature.CombatState;
        if (combatState == null) return;  // 上面已 guard 过，但编译器不认
        var front = combatState.CreateCard(ModelDb.Card<FrontPersona>(), player);
        var back  = combatState.CreateCard(ModelDb.Card<BackPersona>(),  player);

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

    // 诊断日志：观察到死亡（report_46 多 MzmChar 同死卡死调查）
    // 这个 hook 在每个 alive creature 的 AfterDeath 广播链里都会触发，记录"谁观察到谁死了"
    // 卡死时 log 会停在某个 player 的死亡序列中间 → 显示链中断在哪
    public override Task AfterDeath(PlayerChoiceContext ctx, Creature creature, bool wasRemovalPrevented, float deathAnimLength)
    {
        var ownerId = Owner?.NetId.ToString() ?? "?";
        var deadId = creature.Player?.NetId.ToString() ?? (creature.IsMonster ? "monster" : "?");
        Diag.Trace($"SecondPersonaRelic[owner={ownerId}].AfterDeath: observed dead={deadId} prevented={wasRemovalPrevented} animLen={deathAnimLength}");
        return Task.CompletedTask;
    }

    // 全局卡牌打出 hook —— 计 Mu / Mo 出牌数。这里集中算保证覆盖 vanilla + 其它 mod 的卡
    // （之前是每张我们的卡 OnPlay 末尾显式 Bump，会漏算非 MzmChar 卡）。详见 CombatCounters 注释。
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
            Title:       "第二人格",
            Description: "战斗开始时，[gold]进入小睦[/gold]，并将一张[gold]表人格[/gold]和一张[gold]里人格[/gold]加入手牌。",
            Flavor:      "看似一个人，其实有两个。"),
        _ => new RelicLoc(
            Title:       "Second Persona",
            Description: "At combat start, enter [gold]Mu[/gold] form and add a [gold]Front Persona[/gold] and a [gold]Back Persona[/gold] to your hand.",
            Flavor:      "Looks like one person — really two."),
    };
}
