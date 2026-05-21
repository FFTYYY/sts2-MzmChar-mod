using System.Collections.Generic;
using System.Linq;
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
/// 华丽谢幕：1 费金色技能。如果这是最后一张手牌，对全体敌人造成 5/8 点伤害 4 次。演奏 / 虚无 / 消耗。
/// </summary>
[Pool(typeof(MzmCharCardPool))]
public class MuGrandFinale : MzmCharBaseCard
{
    public override string PortraitPath => "res://MzmChar/cards/mzmchar_grand_finale.png";

    private readonly List<DynamicVar> _vars = new()
    {
        new DamageVar(5, ValueProp.Move),
        new DynamicVar("Hits", 4m),
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

    public MuGrandFinale() : base(1, CardType.Attack, CardRarity.Rare, TargetType.Self) { }

    protected override void OnUpgrade() { DynamicVars.Damage.UpgradeValueBy(3); /* 5→8 */ }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        if (!IsInConcert())
        {
            await PlayCast();
            await Sts2Compat.PowerApply<PerformancePassionPower>(ctx, Owner.Creature, 1, Owner.Creature, this, false);
        }
        else
        {
            var hand = PileType.Hand.GetPile(Owner);
            int handCount = hand?.Cards.Count ?? 0;
            if (handCount == 0)   // OnPlay 时本卡已经移出 hand，所以 0 = 这是最后一张
            {
                var cs = Owner.Creature.CombatState;
                if (cs != null && cs.HittableEnemies.Count > 0)
                {
                    int hits = (int)DynamicVars["Hits"].BaseValue;
                    await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                        .FromCard(this).TargetingAllOpponents(cs)
                        .WithHitCount(hits).Execute(ctx);
                }
            }
        }
        if (Forms.IsMortisForm(Owner)) await CombatCounters.BumpMortisCard(ctx, Owner);
        else                            await CombatCounters.BumpMutsumiCard(ctx, Owner);
    }

    public override List<(string, string)>? Localization => LocManager.Instance.Language switch
    {
        "zhs" => new CardLoc("华丽谢幕",
            "{ShowRealEffect:show:如果这是你最后一张手牌，对全体敌人造成{Damage:diff()}点伤害{Hits}次。|非演奏会回合，获得1点[gold]演艺热情[/gold]。}"),
        _ => new CardLoc("Grand Finale",
            "{ShowRealEffect:show:If this is your last card in hand, deal {Damage:diff()} damage to ALL enemies {Hits} times.|Outside concert, gain 1 [gold]Performance Passion[/gold].}"),
    };
}
