using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace MzmChar.Game;

/// <summary>
/// 「第一千个人格」buff：每切换一次人格，本回合获得 Amount 点力量 + Amount 点敏捷。
/// Amount 直接 = 每次给的值（卡 Apply 时传 3）→ hover 描述显示真实层数，多张叠加 Amount 累加。
/// Forms.EnterXxx 切换时调用 OnPersonaSwitch（见 Forms.cs 的 dispatcher）。
/// </summary>
public class ThousandthPersonaPower : CustomPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override string? CustomPackedIconPath => "res://MzmChar/powers/thousandth_persona.png";
    public override string? CustomBigIconPath => "res://MzmChar/powers/thousandth_persona.png";

    public async Task OnPersonaSwitch(PlayerChoiceContext ctx, CardModel? source)
    {
        if (Owner == null) return;
        Flash();
        int amt = Amount;
        await Sts2Compat.PowerApply<StrengthPower>(ctx, Owner, amt, Owner, source, false);
        await Sts2Compat.PowerApply<TempStrengthPower>(ctx, Owner, amt, Owner, source, true);
        await Sts2Compat.PowerApply<DexterityPower>(ctx, Owner, amt, Owner, source, false);
        await Sts2Compat.PowerApply<TempDexterityPower>(ctx, Owner, amt, Owner, source, true);
    }

    public override List<(string, string)>? Localization => LocManager.Instance.Language switch
    {
        "zhs" => new PowerLoc("第一千个人格",
            "每切换一次人格，本回合获得[gold]力量[/gold]和[gold]敏捷[/gold]。",
            "每切换一次人格，本回合获得{Amount}点[gold]力量[/gold]和{Amount}点[gold]敏捷[/gold]。"),
        _ => new PowerLoc("Thousandth Persona",
            "Per persona switch, this turn gain [gold]Strength[/gold] and [gold]Dexterity[/gold].",
            "Per persona switch, this turn gain {Amount} [gold]Strength[/gold] and {Amount} [gold]Dexterity[/gold]."),
    };
}
