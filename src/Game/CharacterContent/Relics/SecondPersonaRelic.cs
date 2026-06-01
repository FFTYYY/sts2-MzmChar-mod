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

    // 注：以前在这里 override BeforeCardPlayed / AfterCardPlayed → CombatCounters.OnBefore/AfterCardPlayed。
    // 已下沉到 MutsumiFormPower / MortisFormPower（两个 form power 都各自 hook）。
    // 这样任何持有 form power 的 creature（包括 HeartResonance 给队友 EnterMutsumi 之后的队友）
    // 都自动 bump 计数，不再依赖遗物。这里不能再 hook 否则会跟 form power 双触发。

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
