using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace MzmChar.Game;

/// <summary>
/// 「无惧死亡」buff：本场战斗下一次死亡时以 Amount 点生命复活。
///
/// 关键 hook 是 ShouldDie + AfterPreventingDeath，参考 FairyInABottle / MockRevivePower。
/// 不能用 BeforeDeath —— 那是 death cascade 已经开始（card piles 被清）之后才触发。
/// ShouldDie 返回 false → 游戏内核**不会**进入 death cascade，HP 还是 0 但 creature 不死。
/// 然后 AfterPreventingDeath 里把 HP 设回 Amount。
/// </summary>
public class FearlessOfDeathPower : CustomPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override string? CustomPackedIconPath => "res://MzmChar/powers/fearless_of_death.png";
    public override string? CustomBigIconPath => "res://MzmChar/powers/fearless_of_death.png";

    [SavedProperty] public bool HasTriggered { get; set; }

    // 阻止死亡：只要 creature 是我们的 owner 且没用过，就 return false
    public override bool ShouldDie(Creature creature)
    {
        if (creature != Owner) return true;       // 不是我们的 owner，让它正常死
        if (HasTriggered) {
            Diag.Trace($"FearlessOfDeathPower[owner={Owner?.Player?.NetId}].ShouldDie: HasTriggered → allow die");
            return true;
        }
        Diag.Trace($"FearlessOfDeathPower[owner={Owner?.Player?.NetId}].ShouldDie: BLOCK die (Amount={Amount})");
        return false;
    }

    // 阻止后立刻 set HP，并标记用过 + 自我移除
    public override async Task AfterPreventingDeath(Creature creature)
    {
        if (creature != Owner) return;
        if (HasTriggered) return;
        Diag.Trace($"FearlessOfDeathPower[owner={Owner?.Player?.NetId}].AfterPreventingDeath: revive to HP={Amount} starting");
        HasTriggered = true;
        Flash();
        await CreatureCmd.SetCurrentHp(Owner!, Amount);
        await PowerCmd.Remove<FearlessOfDeathPower>(Owner!);
        Diag.Trace($"FearlessOfDeathPower[owner={Owner?.Player?.NetId}].AfterPreventingDeath: done");
    }

    public override List<(string, string)>? Localization => LocManager.Instance.Language switch
    {
        "zhs" => new PowerLoc("无惧死亡",
            "本场战斗中，下一次死亡时复活。",
            "本场战斗中，下一次死亡时以{Amount}点生命复活。"),
        _ => new PowerLoc("Fearless of Death",
            "On your next death this combat, revive.",
            "On your next death this combat, revive with {Amount} HP."),
    };
}
