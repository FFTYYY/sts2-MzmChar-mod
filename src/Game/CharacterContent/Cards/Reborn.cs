using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Cards.Variables;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace MzmChar.Game;

/// <summary>
/// 重生：4/3 费金色技能。消耗。移除「坠入深渊」，对全体造成 35 伤害，获得 10 格挡。
/// </summary>
[Pool(typeof(MzmCharCardPool))]
public class Reborn : MzmCharBaseCard
{
    public override string PortraitPath => "res://MzmChar/cards/reborn.png";

    private readonly List<DynamicVar> _vars = new()
    {
        new DamageVar(35, ValueProp.Move),
        new BlockVar(10, ValueProp.Move),
    };
    protected override IEnumerable<DynamicVar> CanonicalVars => _vars;

    private readonly HashSet<CardKeyword> _keywords = new() { CardKeyword.Exhaust };
    public override IEnumerable<CardKeyword> CanonicalKeywords => _keywords;

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get { yield return HoverTipFactory.FromPower<FallIntoAbyssPower>(); }
    }

    public Reborn() : base(4, CardType.Skill, CardRarity.Rare, TargetType.Self) { }

    protected override void OnUpgrade() { EnergyCost.UpgradeBy(-1); }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        if (Owner.Creature.HasPower<FallIntoAbyssPower>())
            await PowerCmd.Remove<FallIntoAbyssPower>(Owner.Creature);
        var cs = Owner.Creature.CombatState;
        if (cs != null && cs.HittableEnemies.Count > 0)
            await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                .FromCardCompat(this, play).TargetingAllOpponents(cs).Execute(ctx);
        await CreatureCmd.GainBlock(Owner.Creature,
            DynamicVars.Block.BaseValue, ValueProp.Move, play, false);
    }

    public override List<(string, string)>? Localization => LocManager.Instance.Language switch
    {
        "zhs" => new CardLoc("重生",
            "移除[gold]坠入深渊[/gold]。对全体敌人造成{Damage:diff()}点伤害。获得{Block:diff()}点[gold]格挡[/gold]。"),
        _ => new CardLoc("Reborn",
            "Remove [gold]Fall Into Abyss[/gold]; deal {Damage:diff()} damage to ALL enemies; gain {Block:diff()} [gold]Block[/gold]."),
    };
}
