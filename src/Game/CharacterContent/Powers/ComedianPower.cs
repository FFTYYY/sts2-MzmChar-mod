using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace MzmChar.Game;

/// <summary>
/// 「搞笑艺人」buff：IsInstanced。每个实例独立倒计时——每切换 1 次人格，Amount-1；
/// 切到 Amount==1 的那次：下回合 +1 费 + 重置回 InitialAmount(=2)。
///
/// 多次施加 = 多个独立 buff（参考 WontLastLong / vanilla OrbitPower 模式）。
/// 切人格触发由 <see cref="Forms"/> 通过 <c>GetPowerInstances&lt;ComedianPower&gt;()</c> 派发到每个 instance。
/// </summary>
public class ComedianPower : CustomPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
#if BETA
    public override PowerInstanceType InstanceType => PowerInstanceType.Instanced;
#else
    public override bool IsInstanced => true;
#endif

    public override string? CustomPackedIconPath => "res://MzmChar/powers/comedian.png";
    public override string? CustomBigIconPath => "res://MzmChar/powers/comedian.png";

    public const int InitialAmount = 2;

    public async Task OnPersonaSwitch(PlayerChoiceContext ctx, CardModel? source)
    {
        var player = Owner?.Player;
        if (player == null) return;
        if (Amount <= 1)
        {
            Flash();
            await Sts2Compat.PowerApply<EnergyNextTurnPower>(ctx, player.Creature, 1, player.Creature, null, false);
            // 当前 Amount==1，重置回 InitialAmount：offset = InitialAmount - 1
            await Sts2Compat.PowerModifyAmount(ctx, this, InitialAmount - Amount, Owner!, null, false);
        }
        else
        {
            await Sts2Compat.PowerModifyAmount(ctx, this, -1, Owner!, null, false);
        }
    }

    public override List<(string, string)>? Localization => LocManager.Instance.Language switch
    {
        // 基础描述用纯文本不引用 energyPrefix（卡牌悬停时该变量未注入，会触发 fallback warning）
        "zhs" => new PowerLoc("搞笑艺人",
            "每切换2人格，下回合获得[gold]能量[/gold]。",
            "再切换{Amount}次人格，下回合获得1点[gold]能量[/gold]。"),
        _ => new PowerLoc("Comedian",
            "After several more persona switches, gain [gold]Energy[/gold] next turn.",
            "After {Amount} more persona switches, gain [gold]1 Energy[/gold] next turn."),
    };
}
