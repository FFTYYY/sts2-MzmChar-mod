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
using MegaCrit.Sts2.Core.ValueProps;

namespace MzmChar.Game;

/// <summary>
/// 开幕：0 费白色攻击。造成 5 点伤害 2/3 次。演奏 / 虚无 / 消耗。
/// </summary>
[Pool(typeof(MzmCharCardPool))]
public class Opening : MzmCharBaseCard
{
    public override string PortraitPath => "res://MzmChar/cards/opening.png";

    private readonly List<DynamicVar> _vars = new()
    {
        new DamageVar(5, ValueProp.Move),
        new DynamicVar("Hits", 2m),
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
        }
    }

    public Opening() : base(0, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy) { }

    // 非演奏会时只是「获得演艺热情」→ 不需要选敌人
    public override TargetType TargetType =>
        IsInConcert() ? TargetType.AnyEnemy : TargetType.Self;

    protected override void OnUpgrade()
    {
        DynamicVars["Hits"].UpgradeValueBy(1);   // 2 → 3
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        if (!IsInConcert())
        {
            await PlayCast();
            await PowerCmd.Apply<PerformancePassionPower>(Owner.Creature, 1, Owner.Creature, this, false);
            await BumpFormCard(ctx);
            return;
        }
        int hits = (int)DynamicVars["Hits"].BaseValue;
        // WithHitCount —— 活力/力量加成应用每次 hit
        if (play.Target != null)
        {
            await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                .FromCard(this).Targeting(play.Target).WithHitCount(hits).Execute(ctx);
            if (Forms.IsMortisForm(Owner))
                CombatCounters.StruckByMortisThisTurn[play.Target] += hits;
        }
        await BumpFormCard(ctx);
    }

    private async Task BumpFormCard(PlayerChoiceContext ctx)
    {
        if (Forms.IsMortisForm(Owner)) await CombatCounters.BumpMortisCard(ctx, Owner);
        else                            await CombatCounters.BumpMutsumiCard(ctx, Owner);
    }

    public override List<(string, string)>? Localization => LocManager.Instance.Language switch
    {
        "zhs" => new CardLoc("开幕",
            "{ShowRealEffect:show:造成{Damage:diff()}点伤害{Hits:diff()}次。|非演奏会回合，获得1点[gold]演艺热情[/gold]。}"),
        _ => new CardLoc("Opening Act",
            "{ShowRealEffect:show:Deal {Damage:diff()} damage {Hits:diff()} times.|Outside concert, gain 1 [gold]Performance Passion[/gold].}"),
    };
}
