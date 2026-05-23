using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace MzmChar.Game;

/// <summary>
/// 移开视线：1 费白色技能。消耗。移除自身的「易伤」、「虚弱」、「脆弱」。升级：获得「保留」。
/// </summary>
[Pool(typeof(MzmCharCardPool))]
public class LookAway : MzmCharBaseCard
{
    public override string PortraitPath => "res://MzmChar/cards/look_away.png";

    private readonly HashSet<CardKeyword> _keywords = new() { CardKeyword.Exhaust };
    public override IEnumerable<CardKeyword> CanonicalKeywords => _keywords;

    public LookAway() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }

    protected override void OnUpgrade() { AddKeyword(CardKeyword.Retain); }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await PlayCast();
        if (Owner.Creature.HasPower<VulnerablePower>())
            await PowerCmd.Remove<VulnerablePower>(Owner.Creature);
        if (Owner.Creature.HasPower<WeakPower>())
            await PowerCmd.Remove<WeakPower>(Owner.Creature);
        if (Owner.Creature.HasPower<FrailPower>())
            await PowerCmd.Remove<FrailPower>(Owner.Creature);
    }

    public override List<(string, string)>? Localization => LocManager.Instance.Language switch
    {
        "zhs" => new CardLoc("移开视线",
            "移除自身的[gold]易伤[/gold]、[gold]虚弱[/gold]、[gold]脆弱[/gold]。"),
        _ => new CardLoc("Look Away",
            "Remove [gold]Vulnerable[/gold], [gold]Weak[/gold], [gold]Frail[/gold] from self."),
    };
}
