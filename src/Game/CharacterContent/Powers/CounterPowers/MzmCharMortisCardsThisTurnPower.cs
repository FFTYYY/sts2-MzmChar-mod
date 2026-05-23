using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;

namespace MzmChar.Game;

/// <summary>
/// 隐藏计数 power：本回合以小墨形态打出的牌数。
///
/// 替代旧 `CombatCounters.MortisCardsThisTurn` SpireField —— SpireField 不参与 vanilla
/// 多人协议同步，多 MzmChar 玩家场景下可能 desync。改用 vanilla power apply / sync。
///
/// 关键设置（同 `MzmCharMutsumiCardsThisTurnPower`）：
/// - `Type=Buff` —— Apply 到 enemy 也不被 Artifact 阻挡（虽然这个 power 只 apply 自己）
/// - `IsVisibleInternal=false` —— UI buff 栏不显示
/// - `StackType=Counter` —— 多次 apply 累加 Amount
/// - `AfterSideTurnEndLate` 自移除 —— per-turn 计数器
/// - guard 用 `participants.Contains(Owner)`（vanilla DisintegrationPower 同款）
///
/// 当前读取者：`Silence.cs`（小睦本回合每张小墨牌额外抽 2）
/// </summary>
public class MzmCharMortisCardsThisTurnPower : CustomPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    // 0.106: IsVisibleInternal 是 protected virtual
    protected override bool IsVisibleInternal => false;

    // 占位 icon（永不渲染）
    public override string? CustomPackedIconPath => "res://MzmChar/powers/performance_passion.png";
    public override string? CustomBigIconPath    => "res://MzmChar/powers/performance_passion.png";

    // 0.106: AfterTurnEndLate → AfterSideTurnEndLate(ctx, side, participants)
    // beta 用 participants.Contains(Owner) 做 guard，stable 退回 side == Owner.Side
#if BETA
    public override async Task AfterSideTurnEndLate(PlayerChoiceContext ctx, CombatSide side, IEnumerable<Creature> participants)
#else
    public override async Task AfterTurnEndLate(PlayerChoiceContext ctx, CombatSide side)
#endif
    {
        if (Owner == null) return;
#if BETA
        if (!participants.Contains(Owner)) return;
#else
        if (side != Owner.Side) return;
#endif
        await PowerCmd.Remove<MzmCharMortisCardsThisTurnPower>(Owner);
    }

    public override List<(string, string)>? Localization => LocManager.Instance.Language switch
    {
        "zhs" => new PowerLoc("（隐藏）本回合小墨出牌数",
            "本回合以[gold]小墨[/gold]形态打出的牌数（隐藏计数器）。",
            "本回合以[gold]小墨[/gold]形态打出的牌数：{Amount}（隐藏计数器）。"),
        _ => new PowerLoc("(Hidden) Mo Cards This Turn",
            "Hidden counter of cards played in Mo form this turn.",
            "Hidden counter of cards played in Mo form this turn: {Amount}."),
    };
}
