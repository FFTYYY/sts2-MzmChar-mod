using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using System.Linq;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace MzmChar.Game;

/// <summary>
/// 模仿：2/1 费蓝色攻击。
///   小睦：获得 12 点格挡，给目标 2 层虚弱。
///   小墨：如果目标的意图是攻击，造成其意图攻击值的伤害，否则造成 1 点伤害。
/// </summary>
[Pool(typeof(MzmCharCardPool))]
public class Imitate : MzmCharBaseCard
{
    public override string PortraitPath => "res://MzmChar/cards/imitate.png";

    private readonly List<DynamicVar> _vars = new()
    {
        new BlockVar("MuBlock", 12m, ValueProp.Move),
        new PowerVar<WeakPower>(2),
        // Mo 实算：被瞄准的敌人意图攻击值（GetTotalDamage 含 Repeats + 怪物自身 modifier），无攻击意图则 1
        // ModifierKind.Damage 让显示值额外走 Hook.ModifyDamage —— 套上**我们**的 vigor / strength + 目标的 vuln / weak
        // 这样显示值就 == OnPlay 里 DamageCmd.Attack(dmg) 实际打出的伤害（OnPlay 也走同套 modifier 链）
        new LambdaVar("MoDmg", (card, t) =>
        {
            // Target-aware lambda：直接拿 UpdateCardPreview 的 target 参数，比 card.CurrentTarget 可靠
            // （hover 预览时 CurrentTarget 不一定及时同步）
            if (t == null || !t.IsMonster || t.Monster == null) return 1;
            var attackIntent = t.Monster.NextMove?.Intents?.OfType<AttackIntent>().FirstOrDefault();
            if (attackIntent == null) return 1;
            var targets = card.Owner?.Creature != null ? new[] { card.Owner.Creature } : System.Array.Empty<Creature>();
            int total = attackIntent.GetTotalDamage(targets, t);
            return total > 0 ? total : 1;
        }, LambdaVar.ModifierKind.Damage),
    };
    protected override IEnumerable<DynamicVar> CanonicalVars => _vars;

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get { yield return HoverTipFactory.FromPower<WeakPower>(); }
    }

    public Imitate() : base(2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy) { }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);  // 2 → 1
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        if (Forms.IsMortisForm(Owner))
        {
            // 复算 dmg —— 跟 LambdaVar 一致，不依赖 BaseValue（它可能为 stale 的 preview value）
            int dmg = 1;
            if (play.Target?.IsMonster == true && play.Target.Monster != null)
            {
                var attackIntent = play.Target.Monster.NextMove?.Intents?.OfType<AttackIntent>().FirstOrDefault();
                if (attackIntent != null)
                {
                    var targets = new[] { Owner.Creature };
                    int total = attackIntent.GetTotalDamage(targets, play.Target);
                    if (total > 0) dmg = total;
                }
            }
            if (play.Target != null)
            {
                await DamageCmd.Attack(dmg).FromCard(this).Targeting(play.Target).Execute(ctx);
                CombatCounters.StruckByMortisThisTurn[play.Target]++;
            }
            await CombatCounters.BumpMortisCard(ctx, Owner);
        }
        else
        {
            await PlayCast();
            await CreatureCmd.GainBlock(Owner.Creature,
                DynamicVars["MuBlock"].BaseValue, ValueProp.Move, play, false);
            if (play.Target != null)
                await PowerCmd.Apply<WeakPower>(play.Target,
                    DynamicVars["WeakPower"].BaseValue, Owner.Creature, this, false);
            await CombatCounters.BumpMutsumiCard(ctx, Owner);
        }
    }

    public override List<(string, string)>? Localization => LocManager.Instance.Language switch
    {
        "zhs" => new CardLoc("模仿",
            "{MuSec}{MuOpen}小睦{MuClose}：获得{MuBlock:diff()}点[gold]格挡[/gold]。施加{WeakPower:diff()}层[gold]虚弱[/gold]。{MuSecEnd}\n" +
            "{MoSec}{MoOpen}小墨{MoClose}：造成等同于目标攻击意图的伤害。无攻击意图则造成1点伤害。（造成{MoDmg:diff()}点伤害）{MoSecEnd}"),
        _ => new CardLoc("Imitate",
            "{MuSec}{MuOpen}Mu{MuClose}: Gain {MuBlock:diff()} [gold]Block[/gold]; apply {WeakPower:diff()} [gold]Weak[/gold].{MuSecEnd}\n" +
            "{MoSec}{MoOpen}Mo{MoClose}: Deal damage equal to the target's attack intent ({MoDmg:diff()}); deal 1 damage if no attack intent.{MoSecEnd}"),
    };
}
