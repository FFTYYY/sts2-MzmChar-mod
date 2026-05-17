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
/// 依存症：1 费金色能力。每切换一次人格，获得 4 点格挡。升级：添加固有（数值不变）。
/// 实现：AddictionPower 的 Amount = 每次给的格挡量（固定 4）。
/// </summary>
[Pool(typeof(MzmCharCardPool))]
public class Addiction : MzmCharBaseCard
{
    public override string PortraitPath => "res://MzmChar/cards/addiction.png";

    private readonly List<DynamicVar> _vars = new()
    {
        new DynamicVar("Block", 4m),    // 数值不升级
    };
    protected override IEnumerable<DynamicVar> CanonicalVars => _vars;

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get { yield return HoverTipFactory.FromPower<AddictionPower>(); }
    }

    public Addiction() : base(1, CardType.Power, CardRarity.Rare, TargetType.Self) { }

    protected override void OnUpgrade() { AddKeyword(CardKeyword.Innate); }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await PlayCast();
        await Sts2Compat.PowerApply<AddictionPower>(ctx, Owner.Creature,
            DynamicVars["Block"].BaseValue, Owner.Creature, this, false);
        if (Forms.IsMortisForm(Owner)) await CombatCounters.BumpMortisCard(ctx, Owner);
        else                            await CombatCounters.BumpMutsumiCard(ctx, Owner);
    }

    public override List<(string, string)>? Localization => LocManager.Instance.Language switch
    {
        "zhs" => new CardLoc("依存症",
            "每切换一次人格，获得{Block}点[gold]格挡[/gold]。"),
        _ => new CardLoc("Addiction",
            "Whenever you switch personas, gain {Block} [gold]Block[/gold]."),
    };
}
