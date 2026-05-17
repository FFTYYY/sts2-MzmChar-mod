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
/// 若叶家产：1 费金色技能。Apply 1 WakabaFortunePower（带初始 switches-this-combat 层数）+ Exhaust。
/// 升级：抽 1 张。
/// </summary>
[Pool(typeof(MzmCharCardPool))]
public class WakabaFortune : MzmCharBaseCard
{
    public override string PortraitPath => "res://MzmChar/cards/wakaba_fortune.png";

    private readonly HashSet<CardKeyword> _keywords = new() { CardKeyword.Exhaust };
    public override IEnumerable<CardKeyword> CanonicalKeywords => _keywords;

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get { yield return HoverTipFactory.FromPower<WakabaFortunePower>(); }
    }

    public WakabaFortune() : base(1, CardType.Skill, CardRarity.Rare, TargetType.Self) { }

    protected override void OnUpgrade() { /* IsUpgraded → 抽 1 张 */ }

    // 注入 {Gold} = 当前已切换形态次数（即"如果现在打出，可获得的金币数"）
    // canonical hover 时 Owner=null → 0；战斗中实时反映累计值
    protected override void AddExtraArgsToDescription(MegaCrit.Sts2.Core.Localization.LocString description)
    {
        base.AddExtraArgsToDescription(description);
        int gold = !IsCanonical && Owner != null
            ? CombatCounters.PersonaSwitchesThisCombat[Owner]
            : 0;
        description.Add("Gold", (decimal)gold);
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await PlayCast();
        // 把"本场已切换形态次数"作为初始层数施加（0 也照应用）；之后切换通过 OnPersonaSwitch + 1
        int currentSwitches = CombatCounters.PersonaSwitchesThisCombat[Owner];
        await Sts2Compat.PowerApply<WakabaFortunePower>(ctx, Owner.Creature, currentSwitches, Owner.Creature, this, false);

        if (IsUpgraded)
            await CardPileCmd.Draw(ctx, 1, Owner, false);

        if (Forms.IsMortisForm(Owner)) await CombatCounters.BumpMortisCard(ctx, Owner);
        else await CombatCounters.BumpMutsumiCard(ctx, Owner);
    }

    public override List<(string, string)>? Localization => LocManager.Instance.Language switch
    {
        "zhs" => new CardLoc("若叶家产",
            "到现在为止，本场战斗中你每切换过一次人格，战斗结束后获得1点金币（获得{Gold}点金币）。{IfUpgraded:show:抽1张牌。|}"),
        _ => new CardLoc("Wakaba Fortune",
            "After combat, gain 1 gold per persona switch this combat (gain {Gold} gold).{IfUpgraded:show: Draw 1.|}"),
    };
}
