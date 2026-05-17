using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace MzmChar.Game;

/// <summary>
/// 「Mortis」buff：每切换一次人格，对全体敌人造成 N 伤害（N = Amount）。
/// </summary>
public class MortisCardPower : CustomPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override string? CustomPackedIconPath => "res://MzmChar/powers/mortis.png";
    public override string? CustomBigIconPath    => "res://MzmChar/powers/mortis.png";

    public async Task OnPersonaSwitch(PlayerChoiceContext ctx, CardModel? source)
    {
        var player = Owner?.Player;
        if (player == null) return;
        var cs = player.Creature.CombatState;
        if (cs == null || cs.HittableEnemies.Count == 0) return;
        Flash();
        // 标准模式（per vanilla PoisonPower / ThornsPower / FlameBarrierPower）：
        // power 直接造伤用 CreatureCmd.Damage，不要走 AttackCommand。AttackCommand 是给卡牌攻击用的，
        // 必须走 FromCard / FromMonster / FromOsty 设置 attacker，且 FromOsty 需要真正的 Osty 实体存在
        // 之前用 FromOsty(player.Creature, source!) 会卡住（无 Osty 时 attack 流程死锁）
        // ToList() 防迭代时 list 被修改（敌人死亡触发 cs.HittableEnemies 变化）
        foreach (var enemy in cs.HittableEnemies.ToList())
            await CreatureCmd.Damage(ctx, enemy, Amount, ValueProp.Move | ValueProp.Unpowered, player.Creature);
    }

    public override List<(string, string)>? Localization => LocManager.Instance.Language switch
    {
        "zhs" => new PowerLoc("Mortis",
            "每切换一次人格，对全体敌人造成伤害。",
            "每切换一次人格，对全体敌人造成{Amount}点伤害。"),
        _ => new PowerLoc("Mortis",
            "Per persona switch, deal damage to ALL enemies.",
            "Per persona switch, deal {Amount} damage to ALL enemies."),
    };
}
