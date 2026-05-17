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
/// 表人格：0 费技能，进入小睦 + 获得 N 点能量（基础 1，升级 2）。Retain。Token rarity 自动排除在奖励 / 商店外。
/// </summary>
[Pool(typeof(MzmCharCardPool))]
public class FrontPersona : MzmCharBaseCard
{
    public override string PortraitPath => "res://MzmChar/cards/front_persona.png";

    private readonly List<DynamicVar> _vars = new()
    {
        new EnergyVar(1),
    };
    protected override IEnumerable<DynamicVar> CanonicalVars => _vars;

    private readonly HashSet<CardKeyword> _keywords = new() { CardKeyword.Retain };
    public override IEnumerable<CardKeyword> CanonicalKeywords => _keywords;

    // 初始牌：禁止被「发现」类效果抽到（Token rarity 不被 FilterForCombat 默认排除）
    public override bool CanBeGeneratedInCombat => false;
    public override bool CanBeGeneratedByModifiers => false;

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get { foreach (var t in FormTooltips.EnterMu()) yield return t; }
    }

    public FrontPersona() : base(0, CardType.Skill, CardRarity.Token, TargetType.Self) { }

    protected override void OnUpgrade()
    {
        DynamicVars.Energy.UpgradeValueBy(1);  // 1 → 2
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        if (Forms.IsMortisForm(Owner))
            await CombatCounters.BumpMortisCard(ctx, Owner);
        else
            await CombatCounters.BumpMutsumiCard(ctx, Owner);

        await Forms.EnterMutsumi(Owner, this, ctx);
        await PlayerCmd.GainEnergy(DynamicVars.Energy.IntValue, Owner);
    }

    public override List<(string, string)>? Localization => LocManager.Instance.Language switch
    {
        "zhs" => new CardLoc("表人格", "[gold]进入小睦[/gold]。获得{Energy:energyIcons()}。"),
        _     => new CardLoc("Front Persona", "Enter [gold]Mu[/gold]. Gain {Energy:energyIcons()}."),
    };
}
