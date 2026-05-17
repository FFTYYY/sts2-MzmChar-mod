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
/// 演奏《KillKiss》：1 费金色攻击。（原 PlayHaruhikageGold 改名）
/// Mo 造成 24/30 伤害；Mu 获得 7/9 覆甲。演奏 / 虚无 / 消耗。
/// </summary>
[Pool(typeof(MzmCharCardPool))]
public class PlayKillkiss : MzmCharBaseCard
{
    public override string PortraitPath => "res://MzmChar/cards/play_killkiss.png";

    private readonly List<DynamicVar> _vars = new()
    {
        new DamageVar(24, ValueProp.Move),
        // 用 plain DynamicVar 不走 PowerVar 的 target-aware preview pipeline。
        // 原因：本卡 TargetType 在 Mo+concert 时是 AnyEnemy，hover 敌人时框架把 target 喂给所有
        // DynamicVar 的 UpdateCardPreview，PowerVar<PlatingPower> 会被误算成 enemy-side context →
        // 显示值异常变大（实际 apply 仍是给 Owner，没问题，只 hover 显示错位）。
        // Kneel 用 PowerVar<PlatingPower> 没事是因为它 TargetType 永远是 Self。
        new DynamicVar("PlatingValue", 7m),
    };
    protected override IEnumerable<DynamicVar> CanonicalVars => _vars;

    private readonly HashSet<CardKeyword> _keywords = new()
    {
        CardKeyword.Ethereal, CardKeyword.Exhaust, MzmCharKeywords.Perform,
    };
    public override IEnumerable<CardKeyword> CanonicalKeywords => _keywords;

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromPower<PerformancePassionPower>();
            yield return HoverTipFactory.FromPower<ConcertPower>();
            yield return HoverTipFactory.FromPower<PlatingPower>();
        }
    }

    public PlayKillkiss() : base(1, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy) { }

    // 非演奏会 → 加演艺热情（Self）；演奏会内 Mu → 加覆甲（Self）；演奏会内 Mo → 单体攻击（AnyEnemy）
    public override TargetType TargetType =>
        IsInConcert() && !IsCanonical && Owner != null && !Forms.IsMutsumiForm(Owner)
            ? TargetType.AnyEnemy
            : TargetType.Self;

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(6);            // 24 → 30
        DynamicVars["PlatingValue"].UpgradeValueBy(2);   // 7 → 9
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        if (!IsInConcert())
        {
            await PlayCast();
            await PowerCmd.Apply<PerformancePassionPower>(Owner.Creature, 1, Owner.Creature, this, false);
        }
        else if (Forms.IsMortisForm(Owner))
        {
            if (play.Target != null)
            {
                await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                    .FromCard(this).Targeting(play.Target).Execute(ctx);
                CombatCounters.StruckByMortisThisTurn[play.Target]++;
            }
        }
        else
        {
            await PlayCast();
            await PowerCmd.Apply<PlatingPower>(Owner.Creature, DynamicVars["PlatingValue"].BaseValue, Owner.Creature, this, false);
        }
        if (Forms.IsMortisForm(Owner)) await CombatCounters.BumpMortisCard(ctx, Owner);
        else                            await CombatCounters.BumpMutsumiCard(ctx, Owner);
    }

    public override List<(string, string)>? Localization => LocManager.Instance.Language switch
    {
        "zhs" => new CardLoc("演奏《KillKiss》",
            "{ShowRealEffect:show:{MuSec}{MuOpen}小睦{MuClose}：获得{PlatingValue:diff()}层[gold]覆甲[/gold]。{MuSecEnd}\n{MoSec}{MoOpen}小墨{MoClose}：造成{Damage:diff()}点伤害。{MoSecEnd}|非演奏会回合，获得1点[gold]演艺热情[/gold]。}"),
        _ => new CardLoc("Play KillKiss",
            "{ShowRealEffect:show:{MuSec}{MuOpen}Mu{MuClose}: Gain {PlatingValue:diff()} [gold]Plating[/gold].{MuSecEnd}\n{MoSec}{MoOpen}Mo{MoClose}: Deal {Damage:diff()} damage.{MoSecEnd}|Outside concert, gain 1 [gold]Performance Passion[/gold].}"),
    };
}
