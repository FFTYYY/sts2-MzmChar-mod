using System.Collections.Generic;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;

namespace MzmChar.Game;

/// <summary>小墨形态标记 buff。本身没效果。</summary>
public class MortisFormPower : CustomPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    public override string? CustomPackedIconPath => "res://MzmChar/powers/mo_form.png";
    public override string? CustomBigIconPath    => "res://MzmChar/powers/mo_form.png";

    public override List<(string, string)>? Localization => LocManager.Instance.Language switch
    {
        "zhs" => new PowerLoc("小墨", "你是墨缇斯。", "你是墨缇斯。"),
        _     => new PowerLoc("Little Mo", "You are Mortis.", "You are Mortis."),
    };
}
