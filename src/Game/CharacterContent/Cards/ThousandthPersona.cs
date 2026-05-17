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
/// 第一千个人格 (Thousandth Persona)：达弗（Darv）的尘封魔典（DustyTome）给若叶睦的专属先古牌。
/// 1 费先古能力牌：每切换一次人格，本回合获得 3 力量 + 3 敏捷（每张本卡都独立叠加）。升级添加「固有」。
///
/// `[Pool(...)]` 必挂；`CardRarity.Ancient` 给"无边框"先古卡面样式。
/// 实现 `ITomeCard` 让 BaseLib DustyTomePatch 把它加入本角色的 tome 候选池。
/// </summary>
[Pool(typeof(MzmCharCardPool))]
public class ThousandthPersona : MzmCharBaseCard, ITomeCard
{
    public override string PortraitPath => "res://MzmChar/cards/thousandth_persona.png";

    // 先古卡，不应被印牌/变化牌随机产生
    public override bool CanBeGeneratedInCombat => false;
    public override bool CanBeGeneratedByModifiers => false;

    private readonly List<DynamicVar> _vars = new()
    {
        new PowerVar<ThousandthPersonaPower>(3),
    };
    protected override IEnumerable<DynamicVar> CanonicalVars => _vars;

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromPower<ThousandthPersonaPower>();
            yield return HoverTipFactory.FromPower<StrengthPower>();
            yield return HoverTipFactory.FromPower<DexterityPower>();
        }
    }

    public ThousandthPersona() : base(1, CardType.Power, CardRarity.Ancient, TargetType.Self) { }

    protected override void OnUpgrade()
    {
        AddKeyword(CardKeyword.Innate);
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await PlayCast();
        await PowerCmd.Apply<ThousandthPersonaPower>(Owner.Creature,
            DynamicVars["ThousandthPersonaPower"].BaseValue, Owner.Creature, this, false);
        if (Forms.IsMortisForm(Owner)) await CombatCounters.BumpMortisCard(ctx, Owner);
        else await CombatCounters.BumpMutsumiCard(ctx, Owner);
    }

    public override List<(string, string)>? Localization => LocManager.Instance.Language switch
    {
        "zhs" => new CardLoc("第一千个人格",
            "每切换一次人格，本回合获得3点[gold]力量[/gold]和3点[gold]敏捷[/gold]。"),
        _ => new CardLoc("Thousandth Persona",
            "Per persona switch, this turn gain 3 [gold]Strength[/gold] and 3 [gold]Dexterity[/gold]."),
    };
}
