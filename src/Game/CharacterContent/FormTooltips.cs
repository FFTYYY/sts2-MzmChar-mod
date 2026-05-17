using System.Collections.Generic;
using MegaCrit.Sts2.Core.HoverTips;

namespace MzmChar.Game;

/// <summary>
/// 卡牌 / power 描述的常用 hover tooltip 帮手。
///
/// - <see cref="Both"/>：当卡描述里含「小睦」/「小墨」字眼时挂双形态 power tooltip
/// - <see cref="BothEnter"/>：当卡描述含「进入小睦」/「进入小墨」时挂两个 Enter-X marker tooltip（"转换成 X 人格"）
/// - <see cref="EnterMu"/> / <see cref="EnterMo"/>：单边
///
/// 注：「进入 X」的描述应当用 Enter-X tooltip 而非 form tooltip（按用户要求 / instructions_3）。
/// </summary>
public static class FormTooltips
{
    public static IEnumerable<IHoverTip> Both()
    {
        yield return HoverTipFactory.FromPower<MutsumiFormPower>();
        yield return HoverTipFactory.FromPower<MortisFormPower>();
    }

    public static IEnumerable<IHoverTip> BothEnter()
    {
        yield return HoverTipFactory.FromPower<EnterMutsumiHoverPower>();
        yield return HoverTipFactory.FromPower<EnterMortisHoverPower>();
    }

    public static IEnumerable<IHoverTip> EnterMu()
    {
        yield return HoverTipFactory.FromPower<EnterMutsumiHoverPower>();
    }

    public static IEnumerable<IHoverTip> EnterMo()
    {
        yield return HoverTipFactory.FromPower<EnterMortisHoverPower>();
    }
}
