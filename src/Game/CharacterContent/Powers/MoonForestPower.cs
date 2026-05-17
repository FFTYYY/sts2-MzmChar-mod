using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;

namespace MzmChar.Game;

/// <summary>
/// 月之森制服：如果你以小睦人格开始回合，则获得 Amount 点临时敏捷。
/// （Amount 由施加时传入：基础卡=1，升级卡=2，多次施加可叠加）
/// </summary>
public class MoonForestPower : CustomPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override string? CustomPackedIconPath => "res://MzmChar/powers/moon_forest.png";
    public override string? CustomBigIconPath => "res://MzmChar/powers/moon_forest.png";

    public override async Task AfterPlayerTurnStartEarly(PlayerChoiceContext ctx, Player player)
    {
        if (player.Creature != Owner) return;
        if (!Forms.IsMutsumiForm(player)) return;
        int gain = (int)Amount;
        if (gain <= 0) return;
        Flash();
        await PowerCmd.Apply<DexterityPower>(Owner, gain, Owner, null, false);
        await PowerCmd.Apply<TempDexterityPower>(Owner, gain, Owner, null, true);
    }

    public override List<(string, string)>? Localization => LocManager.Instance.Language switch
    {
        // 基础（卡牌悬停时）= 1 层默认效果，硬编码 1；详细（buff 在身上）= {Amount}（gain = Amount，无倍率）
        "zhs" => new PowerLoc("月之森制服",
            "以[gold]小睦[/gold]人格开始回合时，本回合获得[gold]敏捷[/gold]。",
            "以[gold]小睦[/gold]人格开始回合时，本回合获得{Amount}点[gold]敏捷[/gold]。"),
        _ => new PowerLoc("Moon Forest Uniform",
            "If you start your turn as [gold]Mu[/gold], gain 1 [gold]Dexterity[/gold] this turn.",
            "If you start your turn as [gold]Mu[/gold], gain {Amount} [gold]Dexterity[/gold] this turn."),
    };
}
