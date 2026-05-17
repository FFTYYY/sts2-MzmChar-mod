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
/// 若叶睦的临时敏捷。
///   Amount &gt; 0 → 本回合结束**失去** Amount 点敏捷（"本回合 +N 敏捷" 的回收钩）
///   Amount &lt; 0 → 本回合结束**获得** |Amount| 点敏捷（"本回合 -N 敏捷" 的恢复钩，如 Distort 的 Mu 路径）
///   Amount = 0 → 直接自移除
///
/// 替代旧的 LoseDexterityAtTurnEndPower + GainDexterityAtTurnEndPower + RestoreDexterityAtTurnEndPower。
/// AllowNegative=true 让正负叠加可以共存于同一实例。
/// </summary>
public class TempDexterityPower : CustomPowerModel
{
    public override PowerType Type => Amount >= 0 ? PowerType.Buff : PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool AllowNegative => true;

    public override string? CustomPackedIconPath => "res://MzmChar/powers/lose_dexterity.png";
    public override string? CustomBigIconPath => "res://MzmChar/powers/lose_dexterity.png";

    public override async Task AfterTurnEnd(PlayerChoiceContext ctx, CombatSide side)
    {
        if (side != Owner.Side) return;
        int amt = (int)Amount;
        if (amt != 0)
            await Sts2Compat.PowerApply<DexterityPower>(ctx, Owner, -amt, Owner, null, true);
        await PowerCmd.Remove<TempDexterityPower>(Owner);
    }

    protected override string SmartDescriptionLocKey => "";

    public override LocString Description
    {
        get
        {
            var d = base.Description;
            int amt = (int)Amount;
            d.Add("AbsAmount", (decimal)Math.Abs(amt));
            d.Add(new IfUpgradedVar("IsLose", amt >= 0 ? 1m : 0m)
            {
                upgradeDisplay = amt >= 0 ? UpgradeDisplay.Upgraded : UpgradeDisplay.Normal,
            });
            return d;
        }
    }

    public override List<(string, string)>? Localization => LocManager.Instance.Language switch
    {
        "zhs" => new PowerLoc("若叶睦的临时敏捷",
            "回合结束时，移除本回合临时获得的敏捷。",
            "回合结束时，{IsLose:show:失去|获得}{AbsAmount}点[gold]敏捷[/gold]。"), // XXX 这一句没有应用，因为有SmartDescriptionLocKey => ""
        _ => new PowerLoc("Temp Dexterity",
            "At end of turn, remove the [gold]Dexterity[/gold] gained temporarily this turn.",
            "At end of turn, {IsLose:show:lose|gain} {AbsAmount} [gold]Dexterity[/gold]."),
    };
}
