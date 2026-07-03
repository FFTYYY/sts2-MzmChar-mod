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
/// 崩坏：2 费金色攻击。
///   小墨：造成 3 点伤害 3/4 次。对方每有 1 层易伤，额外造成 1 点伤害（不再升级）
///   小睦：施加 6/8 层易伤
/// </summary>
[Pool(typeof(MzmCharCardPool))]
public class Crumble : MzmCharBaseCard
{
    public override string PortraitPath => "res://MzmChar/cards/crumble.png";

    private readonly List<DynamicVar> _vars = new()
    {
        new DamageVar(3, ValueProp.Move),
        new DynamicVar("Hits", 3m),
        new PowerVar<VulnerablePower>(6),
        new DynamicVar("BonusPerVuln", 1m),     // 每层易伤的额外伤害（不再升级）
        // Mo 实算：base + 目标 Vuln 层数。ModifierKind.Damage 让显示走 Hook.ModifyDamage
        // 套上力量/活力/易伤 multiplier —— 这样 hover 显示就 == OnPlay 实际打出的伤害
        // Target-aware lambda：直接拿 UpdateCardPreview 的 target 参数（比 card.CurrentTarget 可靠，
        // hover 预览时 CurrentTarget 不一定及时同步）
        new LambdaVar("MoDmg", (card, target) =>
        {
            int baseDmg = (int)card.DynamicVars.Damage.BaseValue;
            int bonus = 0;
            if (target != null)
            {
                var vuln = target.GetPower<VulnerablePower>();
                int per = (int)card.DynamicVars["BonusPerVuln"].BaseValue;
                bonus = vuln != null ? (int)vuln.Amount * per : 0;
            }
            return baseDmg + bonus;
        }, LambdaVar.ModifierKind.Damage),
    };
    protected override IEnumerable<DynamicVar> CanonicalVars => _vars;

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get { yield return HoverTipFactory.FromPower<VulnerablePower>(); }
    }

    public Crumble() : base(2, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy) { }

    protected override void OnUpgrade()
    {
        // 伤害不升级；vuln 升级 +2（6→8）；Hits 升级 +1（3→4）；BonusPerVuln 不再升级
        DynamicVars["VulnerablePower"].UpgradeValueBy(2);    // 6 → 8
        DynamicVars["Hits"].UpgradeValueBy(1);               // 3 → 4
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        if (Forms.IsMortisForm(Owner))
        {
            int hits = (int)DynamicVars["Hits"].BaseValue;
            int baseDmg = (int)DynamicVars.Damage.BaseValue;
            // Vuln.Amount 在多次 hit 之间不变（Vuln 在回合结束才递减），
            // 所以 bonus 可以攻击前读一次，配合 WithHitCount 让 vigor/str 也应用每 hit
            if (play.Target != null)
            {
                var vuln = play.Target.GetPower<VulnerablePower>();
                int per = (int)DynamicVars["BonusPerVuln"].BaseValue;
                int bonus = vuln != null ? (int)vuln.Amount * per : 0;
                await DamageCmd.Attack(baseDmg + bonus)
                    .FromCardCompat(this, play).Targeting(play.Target).WithHitCount(hits).Execute(ctx);
            }
        }
        else
        {
            await PlayCast();
            if (play.Target != null)
                await Sts2Compat.PowerApply<VulnerablePower>(ctx, play.Target, DynamicVars["VulnerablePower"].BaseValue, Owner.Creature, this, false);
        }
    }

    public override List<(string, string)>? Localization => LocManager.Instance.Language switch
    {
        "zhs" => new CardLoc("崩坏",
            "{MuSec}{MuOpen}小睦{MuClose}：施加{VulnerablePower:diff()}层[gold]易伤[/gold]。{MuSecEnd}\n" +
            "{MoSec}{MoOpen}小墨{MoClose}：造成{MoDmg:diff()}点伤害{Hits:diff()}次。对方每有1层[gold]易伤[/gold]，额外造成{BonusPerVuln}点伤害。{MoSecEnd}"),
        _ => new CardLoc("Crumble",
            "{MuSec}{MuOpen}Mu{MuClose}: Apply {VulnerablePower:diff()} [gold]Vulnerable[/gold].{MuSecEnd}\n" +
            "{MoSec}{MoOpen}Mo{MoClose}: Deal {MoDmg:diff()} damage {Hits:diff()} times. For each [gold]Vulnerable[/gold] on the target, deal {BonusPerVuln} extra damage.{MoSecEnd}"),
    };
}
