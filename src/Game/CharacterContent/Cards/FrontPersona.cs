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
using MegaCrit.Sts2.Core.Models.Powers;

namespace MzmChar.Game;

/// <summary>
/// 表人格：0 费技能，进入小睦 + 获得 1 点能量。升级后额外获得 4 点活力。Retain。Token rarity 自动排除在奖励 / 商店外。
/// </summary>
[Pool(typeof(MzmCharCardPool))]
public class FrontPersona : MzmCharBaseCard
{
    public override string PortraitPath => "res://MzmChar/cards/front_persona.png";

    private readonly List<DynamicVar> _vars = new()
    {
        new EnergyVar(1),
        new PowerVar<VigorPower>(0),  // 0 → 4 升级后
    };
    protected override IEnumerable<DynamicVar> CanonicalVars => _vars;

    private readonly HashSet<CardKeyword> _keywords = new() { CardKeyword.Retain };
    public override IEnumerable<CardKeyword> CanonicalKeywords => _keywords;

    // 初始牌：禁止被「发现」类效果抽到（Token rarity 不被 FilterForCombat 默认排除）
    public override bool CanBeGeneratedInCombat => false;
    public override bool CanBeGeneratedByModifiers => false;

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            foreach (var t in FormTooltips.EnterMu()) yield return t;
            yield return HoverTipFactory.FromPower<VigorPower>();
        }
    }

    public FrontPersona() : base(0, CardType.Skill, CardRarity.Token, TargetType.Self) { }

    protected override void OnUpgrade()
    {
        DynamicVars["VigorPower"].UpgradeValueBy(4);  // 0 → 4
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await Forms.EnterMutsumi(Owner, this, ctx);
        await PlayerCmd.GainEnergy(DynamicVars.Energy.IntValue, Owner);
        var vigor = (int)DynamicVars["VigorPower"].BaseValue;
        if (vigor > 0)
            await Sts2Compat.PowerApply<VigorPower>(ctx, Owner.Creature, vigor, Owner.Creature, this, false);
    }

    public override List<(string, string)>? Localization => LocManager.Instance.Language switch
    {
        "zhs" => new CardLoc("表人格",
            "[gold]进入小睦[/gold]。获得{Energy:energyIcons()}。{IfUpgraded:show:获得{VigorPower}点[gold]活力[/gold]。|}"),
        _     => new CardLoc("Front Persona",
            "Enter [gold]Mu[/gold]. Gain {Energy:energyIcons()}.{IfUpgraded:show: Gain {VigorPower} [gold]Vigor[/gold].|}"),
    };
}
