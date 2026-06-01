using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models.Powers;

namespace MzmChar.Game;

/// <summary>
/// 死亡之湖：如果你以小墨人格开始回合，则获得 Amount 点**永久**力量（StrengthPower，无 TempStr）。
/// 多次叠加 Power（多次打卡）→ Amount 累加 → 每回合 Mo 时给的力量随之累加。
/// 用 AfterPlayerTurnStartEarly 在 TransformPersonaPower 之前结算。
/// </summary>
public class DeathLakePower : CustomPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override string? CustomPackedIconPath => "res://MzmChar/powers/death_lake.png";
    public override string? CustomBigIconPath => "res://MzmChar/powers/death_lake.png";

    public override async Task AfterPlayerTurnStartEarly(PlayerChoiceContext ctx, Player player)
    {
        if (player.Creature != Owner) return;
        if (!Forms.IsMortisForm(player)) return;
        int gain = (int)Amount;
        if (gain <= 0) return;
        Flash();
        await Sts2Compat.PowerApply<StrengthPower>(ctx, Owner, gain, Owner, null, false);
    }

    public override List<(string, string)>? Localization => LocManager.Instance.Language switch
    {
        "zhs" => new PowerLoc("死亡之湖",
            "以[gold]小墨[/gold]开始回合时，获得[gold]力量[/gold]。",
            "以[gold]小墨[/gold]开始回合时，获得{Amount}点[gold]力量[/gold]。"),
        _ => new PowerLoc("Lake of Death",
            "If you start your turn as [gold]Mo[/gold], gain [gold]Strength[/gold].",
            "If you start your turn as [gold]Mo[/gold], gain {Amount} [gold]Strength[/gold]."),
    };
}
