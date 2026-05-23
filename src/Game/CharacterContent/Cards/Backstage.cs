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
/// 候场：0 费白色技能。获得 10/15 格挡。演奏 / 虚无 / 消耗。
/// </summary>
[Pool(typeof(MzmCharCardPool))]
public class Backstage : MzmCharBaseCard
{
    public override string PortraitPath => "res://MzmChar/cards/backstage.png";

    private readonly List<DynamicVar> _vars = new()
    {
        new BlockVar(10, ValueProp.Move),
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

    public Backstage() : base(0, CardType.Skill, CardRarity.Common, TargetType.Self) { }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(5);   // 10 → 15
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await PlayCast();
        if (!IsInConcert())
        {
            await Sts2Compat.PowerApply<PerformancePassionPower>(ctx, Owner.Creature, 1, Owner.Creature, this, false);
        }
        else
        {
            await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block.BaseValue, ValueProp.Move, play, false);
        }
    }

    public override List<(string, string)>? Localization => LocManager.Instance.Language switch
    {
        "zhs" => new CardLoc("候场",
            "{ShowRealEffect:show:获得{Block:diff()}点[gold]格挡[/gold]。|非演奏会回合，获得1点[gold]演艺热情[/gold]。}"),
        _ => new CardLoc("Backstage",
            "{ShowRealEffect:show:Gain {Block:diff()} [gold]Block[/gold].|Outside concert, gain 1 [gold]Performance Passion[/gold].}"),
    };
}
