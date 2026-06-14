using System.Collections.Generic;
using MegaCrit.Sts2.Core.HoverTips;

namespace MzmChar.Game;

/// <summary>
/// 卡牌 / power 描述的常用 hover tooltip 帮手。
/// <see cref="Both"/> 挂双形态 power tooltip；<see cref="BothEnter"/> 挂 Enter-X marker tooltip。
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
