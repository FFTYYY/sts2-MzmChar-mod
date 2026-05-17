using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;

namespace MzmChar.Game;

/// <summary>
/// 若叶睦的临时力量。
///   Amount &gt; 0 → 本回合结束**失去** Amount 点力量（"本回合 +N 力量" 的回收钩）
///   Amount &lt; 0 → 本回合结束**获得** |Amount| 点力量（"本回合 -N 力量" 的恢复钩）
///   Amount = 0 → 直接自移除
///
/// 替代旧的 LoseStrengthAtTurnEndPower + GainStrengthAtTurnEndPower。
/// AllowNegative=true 让正负叠加可以共存于同一实例。
/// </summary>
public class TempStrengthPower : CustomPowerModel
{
    public override PowerType Type => Amount >= 0 ? PowerType.Buff : PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool AllowNegative => true;

    public override string? CustomPackedIconPath => "res://MzmChar/powers/lose_strength.png";
    public override string? CustomBigIconPath => "res://MzmChar/powers/lose_strength.png";

    public override async Task AfterTurnEnd(PlayerChoiceContext ctx, CombatSide side)
    {
        if (side != Owner.Side) return;
        int amt = (int)Amount;
        if (amt != 0)
            await Sts2Compat.PowerApply<StrengthPower>(ctx, Owner, -amt, Owner, null, true);
        await PowerCmd.Remove<TempStrengthPower>(Owner);
    }

    // SmartDescriptionLocKey="" 让 basic + smart 都走 Description override
    protected override string SmartDescriptionLocKey => "";

    public override LocString Description
    {
        get
        {
            var d = base.Description;
            int amt = (int)Amount;
            d.Add("AbsAmount", (decimal)Math.Abs(amt));
            // IsLose: Amount 正 → 回合结束失去，反之获得（参考 MzmCharBaseCard 的 IfUpgradedVar 用法）
            d.Add(new IfUpgradedVar("IsLose", amt >= 0 ? 1m : 0m)
            {
                upgradeDisplay = amt >= 0 ? UpgradeDisplay.Upgraded : UpgradeDisplay.Normal,
            });
            return d;
        }
    }

    public override List<(string, string)>? Localization => LocManager.Instance.Language switch
    {
        "zhs" => new PowerLoc("若叶睦的临时力量",
            "回合结束时，移除本回合临时获得的力量。",
            "回合结束时，{IsLose:show:失去|获得}{AbsAmount}点[gold]力量[/gold]。"),
        _ => new PowerLoc("Temp Strength",
            "At end of turn, remove the [gold]Strength[/gold] gained temporarily this turn.",
            "At end of turn, {IsLose:show:lose|gain} {AbsAmount} [gold]Strength[/gold]."),
    };
}
