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
/// 「转换人格」buff。可叠加（StackType.Counter）。
/// 回合开始时，每层翻转一次人格（小睦↔小墨）。翻转触发 Mortis 等 per-switch 钩子。
/// 触发完所有层后自我移除。
/// </summary>
public class TransformPersonaPower : CustomPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override string? CustomPackedIconPath => "res://MzmChar/powers/revert_form.png";
    public override string? CustomBigIconPath    => "res://MzmChar/powers/revert_form.png";

    public override async Task AfterPlayerTurnStartEarly(PlayerChoiceContext ctx, Player player)
    {
        if (player.Creature != Owner) return;
        Flash();

        int n = Amount;
        for (int i = 0; i < n; i++)
        {
            if (Forms.IsMortisForm(player))
                await Forms.EnterMutsumi(player, null, ctx);
            else
                await Forms.EnterMortis(player, null, ctx);
        }
        await PowerCmd.Remove<TransformPersonaPower>(Owner);
    }

    public override List<(string, string)>? Localization => LocManager.Instance.Language switch
    {
        "zhs" => new PowerLoc("转换人格",
            "回合开始时转换人格。",
            "回合开始时转换人格。如果你是[gold]小睦[/gold]：[gold]进入小墨[/gold]；如果你是[gold]小墨[/gold]：[gold]进入小睦[/gold]。重复{Amount}次。"),
        _ => new PowerLoc("Transform Persona",
            "At turn start, transform persona.",
            "At turn start, transform persona. If you are [gold]Mu[/gold], [gold]Enter Mo[/gold]; if you are [gold]Mo[/gold], [gold]Enter Mu[/gold]. Repeat {Amount} times."),
    };
}
