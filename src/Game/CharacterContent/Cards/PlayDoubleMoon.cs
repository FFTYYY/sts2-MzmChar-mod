using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Cards.Variables;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace MzmChar.Game;

/// <summary>
/// 演奏《双月》：1 费蓝色技能。抽 4/6 张。切换人格。
/// 演奏 / 虚无 / 消耗。
/// </summary>
[Pool(typeof(MzmCharCardPool))]
public class PlayDoubleMoon : MzmCharBaseCard
{
    public override string PortraitPath => "res://MzmChar/cards/play_double_moon.png";

    private readonly List<DynamicVar> _vars = new() { new CardsVar(4) };
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
            foreach (var t in FormTooltips.BothEnter()) yield return t;
            yield return HoverTipFactory.FromPower<PerformancePassionPower>();
            yield return HoverTipFactory.FromPower<ConcertPower>();
        }
    }

    public PlayDoubleMoon() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }

    protected override void OnUpgrade()
    {
        DynamicVars.Cards.UpgradeValueBy(2);   // 4 → 6
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        if (!IsInConcert())
        {
            await PowerCmd.Apply<PerformancePassionPower>(Owner.Creature, 1, Owner.Creature, this, false);
            if (Forms.IsMortisForm(Owner)) await CombatCounters.BumpMortisCard(ctx, Owner);
            else                            await CombatCounters.BumpMutsumiCard(ctx, Owner);
            return;
        }
        await CardPileCmd.Draw(ctx, DynamicVars.Cards.BaseValue, Owner, false);
        if (Forms.IsMortisForm(Owner))
        {
            await CombatCounters.BumpMortisCard(ctx, Owner);
            await Forms.EnterMutsumi(Owner, this, ctx);
        }
        else
        {
            await CombatCounters.BumpMutsumiCard(ctx, Owner);
            await Forms.EnterMortis(Owner, this, ctx);
        }
    }

    public override List<(string, string)>? Localization => LocManager.Instance.Language switch
    {
        "zhs" => new CardLoc("演奏《双月》",
            "{ShowRealEffect:show:抽{Cards:diff()}张牌。[gold]切换人格[/gold]。|非演奏会回合，获得1点[gold]演艺热情[/gold]。}"),
        _ => new CardLoc("Play Double Moon",
            "{ShowRealEffect:show:Draw {Cards:diff()}. [gold]Switch persona[/gold].|Outside concert, gain 1 [gold]Performance Passion[/gold].}"),
    };
}
