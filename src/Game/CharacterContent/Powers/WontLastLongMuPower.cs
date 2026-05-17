using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace MzmChar.Game;

/// <summary>
/// 不会长久的（小睦）buff：每打出阈值张牌，失去 1 点[gold]力量[/gold]，整场战斗循环触发。
///
/// 设计（2026-05 简化版）：
///   - `Amount` 当**倒计时**用：每打一张牌 -1；到 0 → 触发效果 + 重置回阈值
///   - `Data.Threshold` 隐藏存原始阈值（重置用），AfterApplied 时从 Amount 抄过来
///   - `_appliedFromCard` 模式：本卡 OnPlay 自身不计入（参考 DollHeartPower）
///   - 描述用 {Amount} 框架自动注入，无 SmartDescriptionLocKey trick，无 Description override
///
/// IsInstanced=true → 多次打 WontLastLong 卡 = 多个独立 buff 各自独立倒计时。
/// </summary>
public class WontLastLongMuPower : CustomPowerModel
{
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;
#if BETA
    public override PowerInstanceType InstanceType => PowerInstanceType.Instanced;
#else
    public override bool IsInstanced => true;
#endif

    public override string? CustomPackedIconPath => "res://MzmChar/powers/wont_last_long_mu.png";
    public override string? CustomBigIconPath    => "res://MzmChar/powers/wont_last_long_mu.png";

    // 隐藏状态：原始阈值，触发后重置 Amount 回到这个值
    public class Data { public int Threshold; }
    protected override object InitInternalData() => new Data();

    // 本卡 OnPlay 不算"打了一张牌"；用 DollHeartPower 同款 guard
    private CardModel? _appliedFromCard;

    public override Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        var d = GetInternalData<Data>();
        if (d != null && d.Threshold == 0) d.Threshold = (int)Amount;
        _appliedFromCard = cardSource;
        return Task.CompletedTask;
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext ctx, CardPlay play)
    {
        if (play.Card?.Owner != Owner?.Player) return;
        if (play.Card == _appliedFromCard)
        {
            _appliedFromCard = null;   // 仅跳第一次
            return;
        }

        SetAmount(Amount - 1, false);
        if (Amount <= 0)
        {
            Flash();
            // "本回合 -1 力量"：先 -1 Strength，再 TempStrengthPower(-1)（负 Amount = 回合末 +1 恢复）
            await Sts2Compat.PowerApply<StrengthPower>(ctx, Owner!, -1, Owner!, null, true);
            await Sts2Compat.PowerApply<TempStrengthPower>(ctx, Owner!, -1, Owner!, null, true);

            // 重置倒计时回阈值
            var d = GetInternalData<Data>();
            int threshold = d?.Threshold ?? 1;
            SetAmount(threshold, false);
        }
    }

    public override List<(string, string)>? Localization => LocManager.Instance.Language switch
    {
        // 第 2 参（卡 hover）：canonical 无 Amount，写无数字 vague 版（不显示阈值，遵循"卡 hover 不显示层数派生值"原则）
        // 第 3 参（战斗 buff hover）：用 {Amount} 框架自动注入显示倒计时
        "zhs" => new PowerLoc("不会长久的（睦）",
            "每打出一定数量的牌，本回合失去1点[gold]力量[/gold]。",
            "再打出{Amount}张牌，本回合失去1点[gold]力量[/gold]。"),
        _ => new PowerLoc("Won't Last Long (Mu)",
            "Per a number of cards played, lose 1 [gold]Strength[/gold] this turn.",
            "After {Amount} more cards, lose 1 [gold]Strength[/gold] this turn."),
    };
}
