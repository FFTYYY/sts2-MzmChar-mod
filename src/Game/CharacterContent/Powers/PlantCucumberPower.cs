using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;

namespace MzmChar.Game;

/// <summary>
/// 「种植黄瓜」效果 buff：每回合开始时
///   - 小睦：获得 (6/8) 点格挡 per instance
///   - 小墨：获得 (4/6) 点活力 per instance
///
/// IsInstanced=true：多次施加产生独立 instance，每个 instance 自己的升级状态
/// 各自触发（同 vanilla OrbitPower 模式）。
/// </summary>
public class PlantCucumberPower : CustomPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.None;   // IsInstanced + Amount 永远 1 → 不显示层数
#if BETA
    public override PowerInstanceType InstanceType => PowerInstanceType.Instanced;
#else
    public override bool IsInstanced => true;
#endif

    public override string? CustomPackedIconPath => "res://MzmChar/powers/cucumber.png";
    public override string? CustomBigIconPath    => "res://MzmChar/powers/cucumber.png";

    [SavedProperty] public bool IsUpgradedVersion { get; set; }

    public override Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        if (cardSource != null) IsUpgradedVersion = cardSource.IsUpgraded;
        return Task.CompletedTask;
    }

    private int MuBlockPerStack => IsUpgradedVersion ? 8 : 6;
    private int MoVigorPerStack => IsUpgradedVersion ? 6 : 4;

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext ctx, Player player)
    {
        if (player.Creature != Owner) return;

        Flash();
        if (Forms.IsMortisForm(player))
        {
            await Sts2Compat.PowerApply<VigorPower>(ctx, player.Creature, MoVigorPerStack * Amount, player.Creature, null, false);
        }
        else
        {
            // 加 ValueProp.Unpowered：power 触发的格挡是固定字面值，不应吃敏捷/Frail 等 modifier
            // （跟 NobleHousePower / AddictionPower 同 pattern）
            await CreatureCmd.GainBlock(player.Creature, MuBlockPerStack * Amount, ValueProp.Move | ValueProp.Unpowered, null);
        }
    }

    protected override string SmartDescriptionLocKey => "";

    public override LocString Description
    {
        get
        {
            var d = base.Description;
            int n = System.Math.Max(1, Amount);
            d.Add("MuGain", MuBlockPerStack * n);
            d.Add("MoGain", MoVigorPerStack * n);
            return d;
        }
    }

    public override List<(string, string)>? Localization => LocManager.Instance.Language switch
    {
        "zhs" => new PowerLoc("种植黄瓜",
            "回合开始时：[gold]小睦[/gold]获得{MuGain}点[gold]格挡[/gold]；[gold]小墨[/gold]获得{MoGain}点[gold]活力[/gold]。",
            "回合开始时：[gold]小睦[/gold]获得{MuGain}点[gold]格挡[/gold]；[gold]小墨[/gold]获得{MoGain}点[gold]活力[/gold]。"),
        _ => new PowerLoc("Cucumber Patch",
            "Start of turn: [gold]Mu[/gold] gain {MuGain} [gold]Block[/gold]; [gold]Mo[/gold] gain {MoGain} [gold]Vigor[/gold].",
            "Start of turn: [gold]Mu[/gold] gain {MuGain} [gold]Block[/gold]; [gold]Mo[/gold] gain {MoGain} [gold]Vigor[/gold]."),
    };
}
