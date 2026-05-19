using System.Collections.Generic;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Localization;

namespace MzmChar.Game;

/// <summary>
/// 「坠入深渊」buff：本场战斗中你无法再进入小睦形态。
/// 检查在 Forms.EnterMutsumi 头部 —— 有这个 buff 直接 return。
/// 「重生」(Reborn) 卡可以移除此 buff。
/// </summary>
public class FallIntoAbyssPower : CustomPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    public override string? CustomPackedIconPath => "res://MzmChar/powers/fall_into_abyss.png";
    public override string? CustomBigIconPath    => "res://MzmChar/powers/fall_into_abyss.png";

    public override List<(string, string)>? Localization => LocManager.Instance.Language switch
    {
        "zhs" => new PowerLoc("坠入深渊",
            "本场战斗中你无法再[gold]进入小睦[/gold]。",
            "本场战斗中你无法再[gold]进入小睦[/gold]。"),
        _ => new PowerLoc("Fall Into Abyss",
            "This combat, you cannot enter [gold]Mu[/gold].",
            "This combat, you cannot enter [gold]Mu[/gold]."),
    };
}
