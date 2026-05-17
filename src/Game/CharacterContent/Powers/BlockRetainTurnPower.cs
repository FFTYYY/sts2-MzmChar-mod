using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;

namespace MzmChar.Game;

/// <summary>
/// 「下回合开始时格挡不消失」一次性 buff。
///
/// 实现参考 vanilla `BlurPower`（StS2 残影）—— IL-verified：
///   1. `ShouldClearBlock(creature)` 必须**只对自己 owner 返回 false**（默认 true），
///      否则联机时会阻止其他玩家 / 敌人的格挡清除（"没打过里人格也保留格挡"的 root cause）
///   2. self-remove 走 `AfterPreventingBlockClear(preventer, creature)` —— Creature.ClearBlock
///      里 `Hook.ShouldClearBlock` 之后立刻触发 `Hook.AfterPreventingBlockClear`，
///      这里 `preventer == this` 就说明本 buff 拦下了一次清除 → 完成使命，自我移除
///
/// 文案：玩家原版认知里"回合结束格挡消失"是错的 —— StS2 块清除发生在**下回合开始**（Creature.ClearBlock
/// 在 player turn start 阶段触发），所以描述写"下回合开始时格挡不消失"才准确。
/// </summary>
public class BlockRetainTurnPower : CustomPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    public override string? CustomPackedIconPath => "res://MzmChar/powers/block_retain.png";
    public override string? CustomBigIconPath    => "res://MzmChar/powers/block_retain.png";

    // 关键：仅对 owner 阻止格挡清除。BlurPower IL: `creature != Owner ? true : false`
    public override bool ShouldClearBlock(Creature creature) => creature != Owner;

    // ClearBlock 里 Hook.ShouldClearBlock 之后触发；preventer 是阻止的那个 power
    public override async Task AfterPreventingBlockClear(AbstractModel preventer, Creature creature)
    {
        if (creature != Owner) return;
        if (preventer != this) return;
        await PowerCmd.Remove<BlockRetainTurnPower>(Owner);
    }

    public override List<(string, string)>? Localization => LocManager.Instance.Language switch
    {
        "zhs" => new PowerLoc("格挡保留",
            "下回合开始时，[gold]格挡[/gold]值不会消失。",
            "下回合开始时，[gold]格挡[/gold]值不会消失。"),
        _ => new PowerLoc("Block Retained",
            "Your [gold]Block[/gold] is not removed at the start of your next turn.",
            "Your [gold]Block[/gold] is not removed at the start of your next turn."),
    };
}
