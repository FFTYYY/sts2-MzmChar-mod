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
/// 隐藏计数 power：本回合以小睦形态打出的牌数。
///
/// 替代旧 `CombatCounters.MutsumiCardsThisTurn` SpireField —— SpireField 是
/// per-client `ConditionalWeakTable`，**不参与 vanilla 多人协议同步**，容易在
/// 长战斗 + 多 MzmChar 玩家场景下导致 desync。改成 power 后利用 vanilla 现成的
/// power apply / sync 机制（PowerCmd.Apply 走 GameAction，自动多人同步）。
///
/// 关键设置：
/// - <c>IsVisibleInternal => false</c>：UI buff 栏不显示
/// - <c>StackType = Counter</c>：多次 apply 累加 Amount
/// - <c>AfterTurnEnd</c> 自移除（per-turn 生命周期）
/// </summary>
public class MzmCharMutsumiCardsThisTurnPower : CustomPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    // 不显示在 UI buff 栏 —— vanilla `PowerModel.IsVisible` getter 默认 fallback
    // 到 IsVisibleInternal，所以 override 这里就够。
    protected override bool IsVisibleInternal => false;

    // 不需要 icon（不可见），但 CustomPowerModel 要求 path 非空。给个占位（永不渲染）。
    public override string? CustomPackedIconPath => "res://MzmChar/powers/performance_passion.png";
    public override string? CustomBigIconPath    => "res://MzmChar/powers/performance_passion.png";

    // 每回合结束自动清零（per-turn 计数器）—— 用 Late 阶段，保证排在普通 AfterSideTurnEnd 之后
    // guard 用 participants.Contains(Owner)（DisintegrationPower IL 实证）
    public override async Task AfterSideTurnEndLate(PlayerChoiceContext ctx, CombatSide side, IEnumerable<Creature> participants)
    {
        if (Owner == null) return;
        if (!participants.Contains(Owner)) return;
        await PowerCmd.Remove<MzmCharMutsumiCardsThisTurnPower>(Owner);
    }

    // 不可见，loc 内容理论上不会显示。但 BaseLib 注册 power 要求有 loc，写个占位。
    public override List<(string, string)>? Localization => LocManager.Instance.Language switch
    {
        "zhs" => new PowerLoc("（隐藏）本回合小睦出牌数",
            "本回合以[gold]小睦[/gold]形态打出的牌数（隐藏计数器，不应显示）。",
            "本回合以[gold]小睦[/gold]形态打出的牌数：{Amount}（隐藏计数器，不应显示）。"),
        _ => new PowerLoc("(Hidden) Mu Cards This Turn",
            "Hidden counter of cards played in Mu form this turn (should not be displayed).",
            "Hidden counter of cards played in Mu form this turn: {Amount} (should not be displayed)."),
    };
}
