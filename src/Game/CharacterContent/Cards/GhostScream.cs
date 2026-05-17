using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace MzmChar.Game;

/// <summary>
/// 鬼叫：2/1 费蓝色攻击。
///   小睦：自己每有 5 点活力，就获得 2 点活力。给目标 1 易伤。
///   小墨：造成 12 伤害，给目标 2 虚弱。
/// </summary>
[Pool(typeof(MzmCharCardPool))]
public class GhostScream : MzmCharBaseCard
{
    public override string PortraitPath => "res://MzmChar/cards/ghost_scream.png";

    private readonly List<DynamicVar> _vars;

    public GhostScream() : base(2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
        _vars = new List<DynamicVar>
        {
            new DamageVar(12, ValueProp.Move),
            new PowerVar<WeakPower>(2),
            new DynamicVar("MuVigorBlock", 2m),       // Mu: 每 5 vigor 给 2 vigor
            new DynamicVar("MuVulnerable", 1m),
            // Mu 实算：根据当前 vigor 算 bonus vigor
            new LambdaVar("MuActualVigor", card =>
            {
                if (card.Owner?.Creature == null) return 0;
                var vp = card.Owner.Creature.GetPower<VigorPower>();
                int v = vp != null ? (int)vp.Amount : 0;
                int per = (int)card.DynamicVars["MuVigorBlock"].BaseValue;
                return (v / 5) * per;
            }),
        };
    }

    protected override IEnumerable<DynamicVar> CanonicalVars => _vars;

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromPower<VigorPower>();
            yield return HoverTipFactory.FromPower<VulnerablePower>();   // Mu 的易伤
            yield return HoverTipFactory.FromPower<WeakPower>();
        }
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);  // 2 → 1
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        if (Forms.IsMortisForm(Owner))
        {
            if (play.Target != null)
            {
                await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                    .FromCard(this).Targeting(play.Target).Execute(ctx);
                CombatCounters.StruckByMortisThisTurn[play.Target]++;
                await PowerCmd.Apply<WeakPower>(play.Target, DynamicVars["WeakPower"].BaseValue, Owner.Creature, this, false);
            }
            await CombatCounters.BumpMortisCard(ctx, Owner);
        }
        else
        {
            await PlayCast();
            var vp = Owner.Creature.GetPower<VigorPower>();
            int v = vp != null ? (int)vp.Amount : 0;
            int bonusVigor = (v / 5) * (int)DynamicVars["MuVigorBlock"].BaseValue;
            if (bonusVigor > 0)
                await PowerCmd.Apply<VigorPower>(Owner.Creature, bonusVigor, Owner.Creature, this, false);
            if (play.Target != null)
                await PowerCmd.Apply<VulnerablePower>(play.Target,
                    DynamicVars["MuVulnerable"].BaseValue, Owner.Creature, this, false);
            await CombatCounters.BumpMutsumiCard(ctx, Owner);
        }
    }

    public override List<(string, string)>? Localization => LocManager.Instance.Language switch
    {
        "zhs" => new CardLoc("鬼叫",
            "{MuSec}{MuOpen}小睦{MuClose}：自身每有5点[gold]活力[/gold]，就获得{MuVigorBlock}点[gold]活力[/gold]（{MuActualVigor}活力）。施加{MuVulnerable}层[gold]易伤[/gold]。{MuSecEnd}\n" +
            "{MoSec}{MoOpen}小墨{MoClose}：造成{Damage:diff()}点伤害。施加{WeakPower}层[gold]虚弱[/gold]。{MoSecEnd}"),
        _ => new CardLoc("Ghost Scream",
            "{MuSec}{MuOpen}Mu{MuClose}: Per 5 [gold]Vigor[/gold] you have, gain {MuVigorBlock} [gold]Vigor[/gold] ({MuActualVigor}). Apply {MuVulnerable} [gold]Vulnerable[/gold].{MuSecEnd}\n" +
            "{MoSec}{MoOpen}Mo{MoClose}: Deal {Damage:diff()} damage; apply {WeakPower} [gold]Weak[/gold].{MoSecEnd}"),
    };
}
