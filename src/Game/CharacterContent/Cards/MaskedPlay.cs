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
/// 假面剧：1 费蓝色技能。
///   小墨：获得 3/4 费
///   小睦：抽 3/4 张
/// 演奏 / 虚无 / 消耗。
/// </summary>
[Pool(typeof(MzmCharCardPool))]
public class MaskedPlay : MzmCharBaseCard
{
    public override string PortraitPath => "res://MzmChar/cards/masked_play.png";

    private readonly List<DynamicVar> _vars = new()
    {
        new EnergyVar(3),
        new CardsVar(3),
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

    public MaskedPlay() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }

    protected override void OnUpgrade()
    {
        DynamicVars.Energy.UpgradeValueBy(1);   // 3 → 4
        DynamicVars.Cards.UpgradeValueBy(1);    // 3 → 4
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await PlayCast();
        if (!IsInConcert())
        {
            await PowerCmd.Apply<PerformancePassionPower>(Owner.Creature, 1, Owner.Creature, this, false);
        }
        else if (Forms.IsMortisForm(Owner))
        {
            await PlayerCmd.GainEnergy(DynamicVars.Energy.IntValue, Owner);
        }
        else
        {
            await CardPileCmd.Draw(ctx, DynamicVars.Cards.BaseValue, Owner, false);
        }
        if (Forms.IsMortisForm(Owner)) await CombatCounters.BumpMortisCard(ctx, Owner);
        else                            await CombatCounters.BumpMutsumiCard(ctx, Owner);
    }

    public override List<(string, string)>? Localization => LocManager.Instance.Language switch
    {
        "zhs" => new CardLoc("假面剧",
            "{ShowRealEffect:show:{MuSec}{MuOpen}小睦{MuClose}：抽{Cards:diff()}张牌。{MuSecEnd}\n{MoSec}{MoOpen}小墨{MoClose}：获得{Energy:energyIcons()}。{MoSecEnd}|非演奏会回合，获得1点[gold]演艺热情[/gold]。}"),
        _ => new CardLoc("Masked Play",
            "{ShowRealEffect:show:{MuSec}{MuOpen}Mu{MuClose}: Draw {Cards:diff()}.{MuSecEnd}\n{MoSec}{MoOpen}Mo{MoClose}: Gain {Energy:energyIcons()}.{MoSecEnd}|Outside concert, gain 1 [gold]Performance Passion[/gold].}"),
    };
}
