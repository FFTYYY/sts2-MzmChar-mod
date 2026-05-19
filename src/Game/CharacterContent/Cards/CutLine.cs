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
using MegaCrit.Sts2.Core.Models.Powers;

namespace MzmChar.Game;

/// <summary>
/// 剪切线：1 费蓝色能力。回合结束时，每有 1 层「若叶睦的临时力量」获得 2/3 点活力。升级：固有 + 数值 2→3。
/// 多张本卡可叠加：power Amount 累加（2+2=4 / 2+3=5 / 3+3=6 per TempStr 层）。
/// </summary>
[Pool(typeof(MzmCharCardPool))]
public class CutLine : MzmCharBaseCard
{
    public override string PortraitPath => "res://MzmChar/cards/cut_line.png";

    private readonly List<DynamicVar> _vars = new()
    {
        new PowerVar<VigorPower>(2),    // 2 → 3 升级
    };
    protected override IEnumerable<DynamicVar> CanonicalVars => _vars;

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromPower<CutLinePower>();
            yield return HoverTipFactory.FromPower<TempStrengthPower>();
            yield return HoverTipFactory.FromPower<VigorPower>();
        }
    }

    public CutLine() : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self) { }

    protected override void OnUpgrade()
    {
        AddKeyword(CardKeyword.Innate);
        DynamicVars["VigorPower"].UpgradeValueBy(1);   // 2 → 3
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await PlayCast();
        await Sts2Compat.PowerApply<CutLinePower>(ctx, Owner.Creature, DynamicVars["VigorPower"].BaseValue, Owner.Creature, this, false);
        if (Forms.IsMortisForm(Owner)) await CombatCounters.BumpMortisCard(ctx, Owner);
        else await CombatCounters.BumpMutsumiCard(ctx, Owner);
    }

    public override List<(string, string)>? Localization => LocManager.Instance.Language switch
    {
        "zhs" => new CardLoc("剪切线",
            "回合结束时，你每有1层[gold]若叶睦的临时力量[/gold]，就获得{VigorPower:diff()}点[gold]活力[/gold]。"),
        _ => new CardLoc("Cut Line",
            "At turn end, gain {VigorPower:diff()} [gold]Vigor[/gold] per stack of [gold]Wakaba's Temp Strength[/gold]."),
    };
}
