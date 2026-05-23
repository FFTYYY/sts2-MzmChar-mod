using System.Collections.Generic;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Localization;

namespace MzmChar.Game;

/// <summary>
/// 隐藏计数 power：本场战斗的形态切换次数。
///
/// 替代旧 `CombatCounters.PersonaSwitchesThisCombat` SpireField。
///
/// 读取者：
/// - `CryInRain.cs` —— 切换次数影响 Mu 分支抽牌
/// - `MultipleMonster.cs` —— 切换次数 buff DamageVar
/// - `WakabaFortune.cs` —— 切换次数换金币
///
/// Per-combat 累计，不自我移除（vanilla 战斗结束清）。
/// </summary>
public class MzmCharPersonaSwitchesThisCombatPower : CustomPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    protected override bool IsVisibleInternal => false;

    public override string? CustomPackedIconPath => "res://MzmChar/powers/performance_passion.png";
    public override string? CustomBigIconPath    => "res://MzmChar/powers/performance_passion.png";

    public override List<(string, string)>? Localization => LocManager.Instance.Language switch
    {
        "zhs" => new PowerLoc("（隐藏）本场战斗形态切换次数",
            "本场战斗中切换形态的次数（隐藏计数器）。",
            "本场战斗中切换形态的次数：{Amount}（隐藏计数器）。"),
        _ => new PowerLoc("(Hidden) Persona Switches This Combat",
            "Hidden counter of persona switches this combat.",
            "Hidden counter of persona switches this combat: {Amount}."),
    };
}
