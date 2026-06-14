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
        // power 造伤走 CreatureCmd.Damage（参考 vanilla PoisonPower / ThornsPower），
        // AttackCommand 只给卡牌攻击用。ToList() 防敌人死亡修改 HittableEnemies。
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
