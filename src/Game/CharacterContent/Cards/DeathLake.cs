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
/// 死亡之湖：2 费蓝色能力，自带「虚无」(Ethereal) keyword。
/// 如果你以小墨人格开始回合，则获得 1 点永久力量。升级：去掉「虚无」（费用不变）。
/// 力量是永久的（StrengthPower，无 TempStrengthPower 配对）。
/// </summary>
[Pool(typeof(MzmCharCardPool))]
public class DeathLake : MzmCharBaseCard
{
    public override string PortraitPath => "res://MzmChar/cards/death_lake.png";

    private readonly HashSet<CardKeyword> _keywords = new() { CardKeyword.Ethereal };
    public override IEnumerable<CardKeyword> CanonicalKeywords => _keywords;

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get { yield return HoverTipFactory.FromPower<DeathLakePower>(); }
    }

    public DeathLake() : base(2, CardType.Power, CardRarity.Uncommon, TargetType.Self) { }

    protected override void OnUpgrade() { RemoveKeyword(CardKeyword.Ethereal); /* 升级去掉「虚无」，费用不变 */ }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await PlayCast();
        await Sts2Compat.PowerApply<DeathLakePower>(ctx, Owner.Creature, 1, Owner.Creature, this, false);
    }

    public override List<(string, string)>? Localization => LocManager.Instance.Language switch
    {
        "zhs" => new CardLoc("死亡之湖",
            "以[gold]小墨[/gold]开始回合时，获得1点[gold]力量[/gold]。"),
        _ => new CardLoc("Lake of Death",
            "If you start your turn as [gold]Mo[/gold], gain 1 [gold]Strength[/gold]."),
    };
}
