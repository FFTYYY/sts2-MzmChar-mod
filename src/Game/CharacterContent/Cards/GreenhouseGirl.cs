using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace MzmChar.Game;

/// <summary>
/// 温室少女：1 费蓝色技能。
///   小墨：消耗最多 2/3 张手牌
///   小睦：获得 8/10 点格挡
/// 消耗手牌实现参考 vanilla BurningPact。
/// </summary>
[Pool(typeof(MzmCharCardPool))]
public class GreenhouseGirl : MzmCharBaseCard
{
    public override string PortraitPath => "res://MzmChar/cards/greenhouse_girl.png";

    private readonly List<DynamicVar> _vars = new()
    {
        new BlockVar(8, ValueProp.Move),
        new DynamicVar("MoExhaust", 2m),
    };
    protected override IEnumerable<DynamicVar> CanonicalVars => _vars;

    public GreenhouseGirl() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(2);        // 8 → 10
        DynamicVars["MoExhaust"].UpgradeValueBy(1); // 2 → 3
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        if (Forms.IsMortisForm(Owner))
        {
            int count = (int)DynamicVars["MoExhaust"].BaseValue;
            var hand = PileType.Hand.GetPile(Owner);
            var candidates = hand?.Cards.Where(c => c != this).ToList() ?? new List<CardModel>();
            if (candidates.Count > 0)
            {
                var prefs = new CardSelectorPrefs(SelectionScreenPrompt, 0, count);
                var selected = await CardSelectCmd.FromSimpleGrid(ctx, candidates, Owner, prefs);
                foreach (var c in selected) await CardCmd.Exhaust(ctx, c, false, false);
            }
            await CombatCounters.BumpMortisCard(ctx, Owner);
        }
        else
        {
            await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block.BaseValue, ValueProp.Move, play, false);
            await CombatCounters.BumpMutsumiCard(ctx, Owner);
        }
    }

    public override List<(string, string)>? Localization => LocManager.Instance.Language switch
    {
        "zhs" => new CardLoc("温室少女",
            "{MuSec}{MuOpen}小睦{MuClose}：获得{Block:diff()}点[gold]格挡[/gold]。{MuSecEnd}\n" +
            "{MoSec}{MoOpen}小墨{MoClose}：消耗至多{MoExhaust:diff()}张手牌。{MoSecEnd}",
            ("selectionScreenPrompt", "选最多{MoExhaust}张手牌消耗")),
        _ => new CardLoc("Greenhouse Girl",
            "{MuSec}{MuOpen}Mu{MuClose}: Gain {Block:diff()} [gold]Block[/gold].{MuSecEnd}\n" +
            "{MoSec}{MoOpen}Mo{MoClose}: Exhaust up to {MoExhaust:diff()} hand cards.{MoSecEnd}",
            ("selectionScreenPrompt", "Choose up to {MoExhaust} cards to Exhaust")),
    };
}
