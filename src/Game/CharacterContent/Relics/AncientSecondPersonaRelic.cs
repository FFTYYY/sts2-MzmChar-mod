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

    public override Task AfterCombatVictory(MegaCrit.Sts2.Core.Rooms.CombatRoom room)
    {
        DidCombatStart = false;
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

        CombatCounters.ResetThisCombat(player);
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

    public override Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        var p = Owner;
        if (p != null && side == p.Creature.Side)
            CombatCounters.ResetThisTurn(p);
        return Task.CompletedTask;
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
