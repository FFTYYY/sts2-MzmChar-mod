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

namespace MzmChar.Game;

/// <summary>
/// 安可：1 费蓝色 Uncommon 能力牌。
/// 每当一个[gold]演奏会[/gold]回合结束时，获得 1 点（升级后 2 点）[gold]演艺热情[/gold]。
/// </summary>
[Pool(typeof(MzmCharCardPool))]
public class Encore : MzmCharBaseCard
{
    public override string PortraitPath => "res://MzmChar/cards/encore.png";

    private readonly List<DynamicVar> _vars = new()
    {
        new DynamicVar("PpGain", 1m),
    };
    protected override IEnumerable<DynamicVar> CanonicalVars => _vars;

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromPower<EncorePower>();
            yield return HoverTipFactory.FromPower<ConcertPower>();
            yield return HoverTipFactory.FromPower<PerformancePassionPower>();
        }
    }

    public Encore() : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self) { }

    protected override void OnUpgrade()
    {
        DynamicVars["PpGain"].UpgradeValueBy(1);     // 1 → 2
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await PlayCast();
        await Sts2Compat.PowerApply<EncorePower>(ctx, Owner.Creature,
            DynamicVars["PpGain"].BaseValue, Owner.Creature, this, false);
    }

    public override List<(string, string)>? Localization => LocManager.Instance.Language switch
    {
        "zhs" => new CardLoc("安可",
            "[gold]演奏会[/gold]回合结束时，获得{PpGain:diff()}点[gold]演艺热情[/gold]。\n每个[gold]演奏会[/gold]回合，你通过[gold]安可[/gold]获得的[gold]演艺热情[/gold]最多为2点。"),
        _ => new CardLoc("Encore",
            "At the end of a [gold]Concert[/gold] turn, gain {PpGain:diff()} [gold]Performance Passion[/gold].\nEach [gold]Concert[/gold] turn, [gold]Performance Passion[/gold] gained from [gold]Encore[/gold] is capped at 2."),
    };
}
