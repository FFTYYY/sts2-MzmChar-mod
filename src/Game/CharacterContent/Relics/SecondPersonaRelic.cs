using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
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

    public override Task AfterCombatVictory(MegaCrit.Sts2.Core.Rooms.CombatRoom room)
    {
        DidCombatStart = false;
        // 战斗结束 reset 计数器，让卡奖励界面 hover 时看到的数值是 0
        // （之前只在战斗开始第一回合 reset，导致选牌界面看到上场战斗的 stale 数据）
        if (Owner != null) CombatCounters.ResetThisCombat(Owner);
        return Task.CompletedTask;
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner) return;
        if (DidCombatStart) return;
        if (Owner?.Creature?.CombatState == null) return;

        DidCombatStart = true;
        Flash();

        // 一次性 per-combat 初始化
        CombatCounters.ResetThisCombat(player);
        await Forms.EnterMutsumi(player, null, choiceContext);

        var combatState = player.Creature.CombatState;
        if (combatState == null) return;  // 上面已 guard 过，但编译器不认
        var front = combatState.CreateCard(ModelDb.Card<FrontPersona>(), player);
        var back  = combatState.CreateCard(ModelDb.Card<BackPersona>(),  player);

        await Sts2Compat.AddGeneratedCardsToCombat(
            new List<CardModel> { front, back }, PileType.Hand, player, addedByPlayer: true);
    }

    public override Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        var p = Owner;
        if (p != null && side == p.Creature.Side)
            CombatCounters.ResetThisTurn(p);
        return Task.CompletedTask;
    }

    /// <summary>
    /// 卡牌触发的抽牌（非回合开始 hand-draw pipeline）→ 计入 ExtraDrawsThisTurn。
    /// 用于「争夺身体」（FightForBody）等"本回合每额外抽一张牌"的卡。
    /// `fromHandDraw=true` 的是回合开始/Megaphone 的统一抽牌流程，不算 extra。
    /// </summary>
    public override Task AfterCardDrawn(PlayerChoiceContext choiceContext, CardModel card, bool fromHandDraw)
    {
        if (Owner == null) return Task.CompletedTask;
        if (card.Owner != Owner) return Task.CompletedTask;  // 多人：只算自己抽的
        if (!fromHandDraw)
            CombatCounters.ExtraDrawsThisTurn[Owner]++;
        return Task.CompletedTask;
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
