using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace MzmChar.Game;

/// <summary>
/// 「伤害减半」buff：owner 受到的伤害减半。Amount = 剩余回合数。
/// 通过 ModifyDamageMultiplicative 直接 ×0.5（owner 是 target 时）。
/// </summary>
public class HalfDamagePower : CustomPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override string? CustomPackedIconPath => "res://MzmChar/powers/half_damage.png";
    public override string? CustomBigIconPath    => "res://MzmChar/powers/half_damage.png";

    public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        // 返回乘数（非已乘 amount）。identity = 1m。
        if (target != Owner) return 1m;
        return 0.5m;
    }

    // 在玩家**下回合开始**时减层（不是回合结束）—— 否则 1 层情况下：
    // 玩家打出下跪 → 玩家回合结束 → 减层到 0 → buff 消失 → 敌人回合还没来就没保护了
    // 改成 AfterPlayerTurnStartEarly：本回合结束 → 敌人回合（伤害减半生效）→ 玩家下回合开始 → 减层
    public override async Task AfterPlayerTurnStartEarly(PlayerChoiceContext ctx, Player player)
    {
        if (player.Creature != Owner) return;
        Flash();
        if (Amount <= 1)
            await PowerCmd.Remove<HalfDamagePower>(Owner);
        else
            SetAmount(Amount - 1, false);
    }

    public override List<(string, string)>? Localization => LocManager.Instance.Language switch
    {
        "zhs" => new PowerLoc("伤害减半",
            "受到的伤害减半。",
            "{Amount}回合内，受到的伤害减半。"),
        _ => new PowerLoc("Damage Halved",
            "Incoming damage is halved.",
            "For {Amount} turns, incoming damage is halved."),
    };
}
