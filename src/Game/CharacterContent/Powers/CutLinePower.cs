using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace MzmChar.Game;

/// <summary>
/// 剪切线：回合结束时，每有 1 层"回合结束失去力量"（TempStrengthPower 正 Amount 部分）就获得 Amount 点格挡。
/// </summary>
public class CutLinePower : CustomPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override string? CustomPackedIconPath => "res://MzmChar/powers/cut_line.png";
    public override string? CustomBigIconPath => "res://MzmChar/powers/cut_line.png";

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get { yield return HoverTipFactory.FromPower<TempStrengthPower>(); }
    }

    public override async Task BeforeTurnEnd(PlayerChoiceContext ctx, CombatSide side)
    {
        if (Owner?.Player == null || side != CombatSide.Player) return;
        var tempStr = Owner.GetPower<TempStrengthPower>();
        if (tempStr == null) return;
        // TempStrengthPower 合并后可为负（回合结束获得）—— 我们只数"失去"那部分
        int loseAmount = System.Math.Max(0, (int)tempStr.Amount);
        int gain = loseAmount * (int)Amount;
        if (gain <= 0) return;
        Flash();
        await CreatureCmd.GainBlock(Owner, gain, default, null, false);
    }

    // 卡 hover: 不写"1 点格挡"（实际是 1×Amount，多层时不对）→ 无数字 vague 版
    // buff hover: 用 {Amount} 框架自动注入
    public override List<(string, string)>? Localization => LocManager.Instance.Language switch
    {
        "zhs" => new PowerLoc("剪切线",
            "回合结束时，你每有1层[gold]若叶睦的临时力量[/gold]，就获得[gold]格挡[/gold]。",
            "回合结束时，你每有1层[gold]若叶睦的临时力量[/gold]，就获得{Amount}点[gold]格挡[/gold]。"),
        _ => new PowerLoc("Cut Line",
            "At turn end, gain [gold]Block[/gold] per stack of [gold]Wakaba's Temp Strength[/gold].",
            "At turn end, gain {Amount} [gold]Block[/gold] per stack of [gold]Wakaba's Temp Strength[/gold]."),
    };
}
