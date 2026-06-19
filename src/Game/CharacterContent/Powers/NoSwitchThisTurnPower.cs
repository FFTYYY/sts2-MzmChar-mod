using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;

namespace MzmChar.Game;

/// <summary>
/// 「本回合无法切换人格」buff。Forms.EnterMortis/EnterMutsumi 进入前会检查这个 power。
/// 回合结束时自动移除。
/// </summary>
public class NoSwitchThisTurnPower : CustomPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    public override string? CustomPackedIconPath => "res://MzmChar/powers/no_switch.png";
    public override string? CustomBigIconPath    => "res://MzmChar/powers/no_switch.png";

    public override async Task AfterSideTurnEnd(PlayerChoiceContext ctx, CombatSide side, IEnumerable<Creature> participants)
    {
        if (side != Owner.Side) return;
        await PowerCmd.Remove<NoSwitchThisTurnPower>(Owner);
    }

    public override List<(string, string)>? Localization => LocManager.Instance.Language switch
    {
        "zhs" => new PowerLoc("本回合不可切换",
            "本回合无法切换人格。",
            "本回合无法切换人格。"),
        _ => new PowerLoc("No Switch",
            "Cannot switch persona this turn.",
            "Cannot switch persona this turn."),
    };
}
