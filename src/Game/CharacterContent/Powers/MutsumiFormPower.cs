using System.Collections.Generic;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;

namespace MzmChar.Game;

/// <summary>
/// 小睦形态标记 buff。本身没效果 —— 卡牌通过 Forms.IsMutsumiForm(player) 判断使用哪份效果。
/// 默认形态规则：没 buff、或两个 buff 都有时，都视为小睦（在 Forms.IsMortisForm 里实现）。
/// </summary>
public class MutsumiFormPower : CustomPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    public override string? CustomPackedIconPath => "res://MzmChar/powers/mu_form.png";
    public override string? CustomBigIconPath    => "res://MzmChar/powers/mu_form.png";

    public override List<(string, string)>? Localization => LocManager.Instance.Language switch
    {
        "zhs" => new PowerLoc("小睦", "你是小睦。", "你是小睦。"),
        _     => new PowerLoc("Little Mu", "You are Mutsumi.", "You are Mutsumi."),
    };
}
