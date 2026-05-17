using System.Collections.Generic;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Localization;

namespace MzmChar.Game;

/// <summary>
/// 「进入小睦」hover-tip 专用 marker power。**永不应用到 creature**。
/// 仅作为 HoverTipFactory.FromPower&lt;EnterMutsumiHoverPower&gt;() 注入卡牌/power 描述里
/// 「进入小睦」短语的 tooltip 注释。
/// 图标复用小睦形态图。
/// </summary>
public class EnterMutsumiHoverPower : CustomPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.None;

    public override string? CustomPackedIconPath => "res://MzmChar/powers/mu_form.png";
    public override string? CustomBigIconPath    => "res://MzmChar/powers/mu_form.png";

    public override List<(string, string)>? Localization => LocManager.Instance.Language switch
    {
        "zhs" => new PowerLoc("进入小睦",
            "转换成小睦人格。",
            "转换成小睦人格。"),
        _ => new PowerLoc("Enter Mu",
            "Switch to Mu persona.",
            "Switch to Mu persona."),
    };
}
