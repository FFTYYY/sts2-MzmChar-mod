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
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace MzmChar.Game;

/// <summary>
/// 迷你小睦（稀有）：每回合你第一次获得[gold]演艺热情[/gold]时，获得 1 点[gold]敏捷[/gold]。
/// 每回合 once：用 [SavedProperty] _usedThisTurn，AfterSideTurnEnd / AfterTurnEnd 重置。
/// </summary>
[Pool(typeof(MzmCharRelicPool))]
public class MiniMutsumiRelic : CustomRelicModel
{
    [SavedProperty]
    private bool UsedThisTurn { get; set; }

    public override RelicRarity Rarity => RelicRarity.Rare;

    public override string PackedIconPath           => "res://MzmChar/relics/mini_mutsumi.png";
    protected override string BigIconPath           => "res://MzmChar/relics/mini_mutsumi.png";
    protected override string PackedIconOutlinePath => "res://MzmChar/relics/mini_mutsumi.png";

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromPower<PerformancePassionPower>();
            yield return HoverTipFactory.FromPower<DexterityPower>();
        }
    }

    public override async Task AfterPowerAmountChanged(
        PlayerChoiceContext ctx, PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
    {
        if (UsedThisTurn) return;
        if (Owner == null) return;
        if (power.Owner != Owner.Creature) return;
        if (power is not PerformancePassionPower) return;
        if (amount <= 0) return;  // amount 是 delta 增量（IL-verified），只关心 gain

        UsedThisTurn = true;
        Flash();
        await Sts2Compat.PowerApply<DexterityPower>(ctx, Owner.Creature, 1, Owner.Creature, null, false);
    }

    public override Task AfterSideTurnEnd(PlayerChoiceContext ctx, CombatSide side, IEnumerable<Creature> participants)
    {
        if (Owner != null && side == Owner.Creature.Side)
            UsedThisTurn = false;
        return Task.CompletedTask;
    }

    // 战斗开始重置 — 比 AfterCombatVictory 更鲁棒。turn-end reset 也保留作回合内复位。
    public override Task BeforeCombatStart()
    {
        UsedThisTurn = false;
        return Task.CompletedTask;
    }

    public override List<(string, string)>? Localization => LocManager.Instance.Language switch
    {
        "zhs" => new RelicLoc(
            Title:       "迷你小睦",
            Description: "每回合你第一次获得[gold]演艺热情[/gold]时，获得1点[gold]敏捷[/gold]。",
            Flavor:      "巴掌大的小睦人偶。盯着它看久了，它好像也会眨眼。"),
        _ => new RelicLoc(
            Title:       "Mini Mutsumi",
            Description: "The first time each turn you gain [gold]Performance Passion[/gold], gain 1 [gold]Dexterity[/gold].",
            Flavor:      "A palm-sized Mutsumi plush. Stare long enough and it seems to blink back."),
    };
}
