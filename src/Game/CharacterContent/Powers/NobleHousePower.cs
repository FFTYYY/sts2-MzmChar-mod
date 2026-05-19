using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.ValueProps;

namespace MzmChar.Game;

/// <summary>
/// 名门：回合开始时，每有 1 层演艺热情，获得 Amount 点格挡。
/// Amount = "每层换算多少格挡"（卡 Apply 时传 4 / 升级 6；多张卡 Apply 累加）。
/// </summary>
public class NobleHousePower : CustomPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override string? CustomPackedIconPath => "res://MzmChar/powers/noble_house.png";
    public override string? CustomBigIconPath => "res://MzmChar/powers/noble_house.png";

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext ctx, Player player)
    {
        if (player.Creature != Owner) return;
        var pp = Owner.GetPower<PerformancePassionPower>();
        int passion = pp != null ? (int)pp.Amount : 0;
        int blockGain = passion * (int)Amount;
        if (blockGain <= 0) return;
        Flash();
        // 加 ValueProp.Unpowered：power 触发的格挡 spec 是固定字面值，不再叠玩家 buff
        // （否则敏捷会让 4 防御 → 4+Dex 防御，违反 spec）。同 MortisCardPower 给伤害加 Unpowered 的 pattern。
        await CreatureCmd.GainBlock(Owner, blockGain, ValueProp.Move | ValueProp.Unpowered, null);
        await Task.CompletedTask;
        _ = ctx;  // ctx 不需要给 GainBlock，但保留方法签名
    }

    public override List<(string, string)>? Localization => LocManager.Instance.Language switch
    {
        // 卡 hover (Amount 默认 0 时 canonical 显示)：写无数字 vague 版（保持跟 CutLine 同款）
        // buff hover：{Amount} 框架自动注入，显示当前堆叠后每层换算的格挡数
        "zhs" => new PowerLoc("名门",
            "回合开始时，你每有1层[gold]演艺热情[/gold]，就获得[gold]格挡[/gold]。",
            "回合开始时，你每有1层[gold]演艺热情[/gold]，就获得{Amount}点[gold]格挡[/gold]。"),
        _ => new PowerLoc("Noble House",
            "At turn start, per stack of [gold]Performance Passion[/gold], gain [gold]Block[/gold].",
            "At turn start, per stack of [gold]Performance Passion[/gold], gain {Amount} [gold]Block[/gold]."),
    };
}
