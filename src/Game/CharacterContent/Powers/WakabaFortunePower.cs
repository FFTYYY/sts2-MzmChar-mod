using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Rooms;

namespace MzmChar.Game;

/// <summary>
/// 若叶家产：战斗结束后获得 Amount 点金币。
/// Amount 由 WakabaFortune 卡在 OnPlay 时按当时的"已切换形态次数"快照应用并叠加（StackType.Counter）。
/// 一旦应用后 Amount 就**冻结**——之后再切换形态不再自动 +1。要继续涨只能再次打出本卡。
/// （之前版本在 OnPersonaSwitch 里自动 +1，违反 spec "一旦打出，power 层数不再随切换叠加"）
/// </summary>
public class WakabaFortunePower : CustomPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool AllowNegative => true;   // 允许 Amount=0 时不被自动移除

    public override string? CustomPackedIconPath => "res://MzmChar/powers/wakaba_fortune.png";
    public override string? CustomBigIconPath => "res://MzmChar/powers/wakaba_fortune.png";

    public override async Task AfterCombatEnd(CombatRoom room)
    {
        var player = Owner?.Player;
        if (player == null) return;
        int gold = (int)Amount;
        if (gold <= 0) return;
        await PlayerCmd.GainGold(gold, player, false);
    }

    public override List<(string, string)>? Localization => LocManager.Instance.Language switch
    {
        "zhs" => new PowerLoc("若叶家产",
            "战斗结束后获得[gold]金币[/gold]。",
            "战斗结束后获得{Amount}点[gold]金币[/gold]。"),
        _ => new PowerLoc("Wakaba Fortune",
            "After combat, gain some [gold]gold[/gold].",
            "After combat, gain {Amount} [gold]gold[/gold]."),
    };
}
