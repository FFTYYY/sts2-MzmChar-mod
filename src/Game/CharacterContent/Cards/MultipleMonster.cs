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
/// 多首的怪物：3 费金色技能。本场战斗中每切换过一次人格，就获得一层「转换人格」。虚无。
/// 升级：移除虚无。
/// </summary>
[Pool(typeof(MzmCharCardPool))]
public class MultipleMonster : MzmCharBaseCard
{
    public override string PortraitPath => "res://MzmChar/cards/multiple_monster.png";

    private readonly HashSet<CardKeyword> _keywords = new() { CardKeyword.Ethereal };
    public override IEnumerable<CardKeyword> CanonicalKeywords => _keywords;

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get { yield return HoverTipFactory.FromPower<TransformPersonaPower>(); }
    }

    public MultipleMonster() : base(3, CardType.Skill, CardRarity.Rare, TargetType.Self) { }

    // 注入 {Switches} = 当前本场已切换形态次数，loc 用括号显示实际层数
    // canonical hover 时 Owner=null → 0
    protected override void AddExtraArgsToDescription(MegaCrit.Sts2.Core.Localization.LocString description)
    {
        base.AddExtraArgsToDescription(description);
        int switches = !IsCanonical && Owner != null
            ? CombatCounters.GetPersonaSwitchesThisCombat(Owner)
            : 0;
        description.Add("Switches", (decimal)switches);
    }

    protected override void OnUpgrade() { RemoveKeyword(CardKeyword.Ethereal); }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await PlayCast();
        int switches = CombatCounters.GetPersonaSwitchesThisCombat(Owner);
        if (switches > 0)
            await Sts2Compat.PowerApply<TransformPersonaPower>(ctx, Owner.Creature, switches, Owner.Creature, this, false);
    }

    public override List<(string, string)>? Localization => LocManager.Instance.Language switch
    {
        "zhs" => new CardLoc("多首的怪物",
            "本场战斗中每切换过一次人格，就获得一层[gold]转换人格[/gold]。获得{Switches}层[gold]转换人格[/gold]。"),
        _ => new CardLoc("Many-Headed Monster",
            "Gain 1 [gold]Transform Persona[/gold] for each persona switch this combat. (Gain {Switches} [gold]Transform Persona[/gold])"),
    };
}
