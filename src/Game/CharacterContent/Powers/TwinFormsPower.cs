using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;

namespace MzmChar.Game;

/// <summary>
/// 「双生形态」效果 buff：回合开始时
///   - 小睦：对随机敌人造成 15/25 点伤害，进入小墨
///   - 小墨：对随机敌人施加 2/4 层虚弱 + 2/4 层易伤，进入小睦
///
/// IsInstanced=true：多次施加产生独立 instance，每个 instance 各自记自己的升级状态
/// 并各自触发（同 vanilla OrbitPower / WontLastLongMuPower 模式）。
/// AfterApplied 从 cardSource.IsUpgraded 读升级状态写到 [SavedProperty]。
/// </summary>
public class TwinFormsPower : CustomPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.None;   // IsInstanced + Amount 永远 1 → 不显示层数
    public override bool IsInstanced => true;

    public override string? CustomPackedIconPath => "res://MzmChar/powers/twin_forms.png";
    public override string? CustomBigIconPath    => "res://MzmChar/powers/twin_forms.png";

    [SavedProperty] public bool IsUpgradedVersion { get; set; }

    public override Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        if (cardSource != null) IsUpgradedVersion = cardSource.IsUpgraded;
        return Task.CompletedTask;
    }

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            foreach (var t in FormTooltips.BothEnter()) yield return t;
            yield return HoverTipFactory.FromPower<VulnerablePower>();
            yield return HoverTipFactory.FromPower<WeakPower>();
        }
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext ctx, Player player)
    {
        if (player.Creature != Owner) return;
        Flash();
        var cs = player.Creature.CombatState;

        int n = Amount;
        if (Forms.IsMortisForm(player))
        {
            // Mo: 对随机敌人施加 vuln + weak
            int debuff = (IsUpgradedVersion ? 4 : 2) * n;
            if (cs != null && cs.HittableEnemies.Count > 0)
            {
                var rng = player.RunState?.Rng?.CombatTargets;
                var target = rng != null ? rng.NextItem(cs.HittableEnemies) : cs.HittableEnemies[0];
                if (target != null)
                {
                    await PowerCmd.Apply<WeakPower>(target, debuff, player.Creature, null, false);
                    await PowerCmd.Apply<VulnerablePower>(target, debuff, player.Creature, null, false);
                }
            }
            await Forms.EnterMutsumi(player, null, ctx);
        }
        else
        {
            // Mu: 对随机敌人造成伤害
            int dmg = (IsUpgradedVersion ? 25 : 15) * n;
            if (cs != null && cs.HittableEnemies.Count > 0)
            {
                var rng = player.RunState?.Rng?.CombatTargets;
                var target = rng != null ? rng.NextItem(cs.HittableEnemies) : cs.HittableEnemies[0];
                if (target != null)
                    await CreatureCmd.Damage(ctx, target, dmg, ValueProp.Move | ValueProp.Unpowered, player.Creature);
            }
            await Forms.EnterMortis(player, null, ctx);
        }
    }

    // SmartDescription getter 不可 override —— 走 SmartDescriptionLocKey 返回 "" 让 HasSmartDescription = false。
    protected override string SmartDescriptionLocKey => "";

    public override LocString Description
    {
        get
        {
            var d = base.Description;
            int n = System.Math.Max(1, Amount);
            int dmg = (IsUpgradedVersion ? 25 : 15) * n;
            int debuff = (IsUpgradedVersion ? 4 : 2) * n;
            d.Add("Damage", dmg);
            d.Add("Debuff", debuff);
            return d;
        }
    }

    public override List<(string, string)>? Localization => LocManager.Instance.Language switch
    {
        "zhs" => new PowerLoc("双生形态",
            "回合开始时：[gold]小睦[/gold]对随机敌人造成{Damage}点伤害，[gold]进入小墨[/gold]；[gold]小墨[/gold]对随机敌人施加{Debuff}层[gold]虚弱[/gold]和{Debuff}层[gold]易伤[/gold]，[gold]进入小睦[/gold]。",
            "回合开始时：[gold]小睦[/gold]对随机敌人造成{Damage}点伤害，[gold]进入小墨[/gold]；[gold]小墨[/gold]对随机敌人施加{Debuff}层[gold]虚弱[/gold]和{Debuff}层[gold]易伤[/gold]，[gold]进入小睦[/gold]。"),
        _ => new PowerLoc("Twin Forms",
            "Start of turn: [gold]Mu[/gold] deal {Damage} damage to a random enemy, [gold]Enter Mo[/gold]; [gold]Mo[/gold] apply {Debuff} [gold]Weak[/gold] and {Debuff} [gold]Vulnerable[/gold] to a random enemy, [gold]Enter Mu[/gold].",
            "Start of turn: [gold]Mu[/gold] deal {Damage} damage to a random enemy, [gold]Enter Mo[/gold]; [gold]Mo[/gold] apply {Debuff} [gold]Weak[/gold] and {Debuff} [gold]Vulnerable[/gold] to a random enemy, [gold]Enter Mu[/gold]."),
    };
}
