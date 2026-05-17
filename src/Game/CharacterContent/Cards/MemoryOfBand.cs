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
/// 回忆中的乐队：3/2 费金色能力。获得 5 点[gold]演艺热情[/gold]。
/// </summary>
[Pool(typeof(MzmCharCardPool))]
public class MemoryOfBand : MzmCharBaseCard
{
    public override string PortraitPath => "res://MzmChar/cards/memory_of_band.png";

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromPower<PerformancePassionPower>();
            yield return HoverTipFactory.FromPower<ConcertPower>();
        }
    }

    public MemoryOfBand() : base(3, CardType.Power, CardRarity.Rare, TargetType.Self) { }

    protected override void OnUpgrade() { EnergyCost.UpgradeBy(-1); }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await PlayCast();
        await PowerCmd.Apply<PerformancePassionPower>(Owner.Creature, 5, Owner.Creature, this, false);
        if (Forms.IsMortisForm(Owner)) await CombatCounters.BumpMortisCard(ctx, Owner);
        else                            await CombatCounters.BumpMutsumiCard(ctx, Owner);
    }

    public override List<(string, string)>? Localization => LocManager.Instance.Language switch
    {
        "zhs" => new CardLoc("回忆中的乐队",
            "获得5点[gold]演艺热情[/gold]。"),
        _ => new CardLoc("Memories of the Band",
            "Gain 5 [gold]Performance Passion[/gold]."),
    };
}
