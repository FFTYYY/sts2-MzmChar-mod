using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace MzmChar.Game;

/// <summary>
/// 依存症：每切换一次人格，获得 Amount 点格挡。
/// 触发由 Forms.OnPersonaSwitched 派发到 OnPersonaSwitch 方法（同 MortisCardPower / DisintegrationPower）。
/// </summary>
public class AddictionPower : CustomPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override string? CustomPackedIconPath => "res://MzmChar/powers/addiction.png";
    public override string? CustomBigIconPath => "res://MzmChar/powers/addiction.png";

    public async Task OnPersonaSwitch(PlayerChoiceContext ctx, CardModel? source)
    {
        if (Owner == null) return;
        Flash();
        // 加 ValueProp.Unpowered：power 触发的格挡是固定字面值，不应吃敏捷/Frail 等 modifier
        // （跟 NobleHousePower / MortisCardPower / TwinFormsPower 同 pattern）
        await CreatureCmd.GainBlock(Owner, (int)Amount, ValueProp.Move | ValueProp.Unpowered, null);
    }

    public override List<(string, string)>? Localization => LocManager.Instance.Language switch
    {
        "zhs" => new PowerLoc("依存症",
            "每切换一次人格，获得[gold]格挡[/gold]。",
            "每切换一次人格，获得{Amount}点[gold]格挡[/gold]。"),
        _ => new PowerLoc("Addiction",
            "Whenever you switch personas, gain [gold]Block[/gold].",
            "Whenever you switch personas, gain {Amount} [gold]Block[/gold]."),
    };
}
