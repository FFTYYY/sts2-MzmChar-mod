using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;

namespace MzmChar.Game;

/// <summary>
/// 名门：1 费蓝色能力。回合开始时获得 (演艺热情 × 层数) 点活力。升级：固有。
/// </summary>
[Pool(typeof(MzmCharCardPool))]
public class NobleHouse : MzmCharBaseCard
{
    public override string PortraitPath => "res://MzmChar/cards/noble_house.png";

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromPower<NobleHousePower>();
            yield return HoverTipFactory.FromPower<PerformancePassionPower>();
        }
    }

    public NobleHouse() : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self) { }

    protected override void OnUpgrade() { AddKeyword(CardKeyword.Innate); }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await PlayCast();
        await Sts2Compat.PowerApply<NobleHousePower>(ctx, Owner.Creature, 1, Owner.Creature, this, false);
        if (Forms.IsMortisForm(Owner)) await CombatCounters.BumpMortisCard(ctx, Owner);
        else await CombatCounters.BumpMutsumiCard(ctx, Owner);
    }

    public override List<(string, string)>? Localization => LocManager.Instance.Language switch
    {
        "zhs" => new CardLoc("名门",
            "回合开始时，你每有1层[gold]演艺热情[/gold]，就获得1点[gold]活力[/gold]。"),
        _ => new CardLoc("Noble House",
            "At turn start, gain 1 [gold]Vigor[/gold] for each stack of [gold]Performance Passion[/gold]."),
    };
}
