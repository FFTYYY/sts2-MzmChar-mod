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
///   小墨：从弃牌堆中选择至多 2/3 张牌消耗
///   小睦：获得 8/10 点格挡
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
            var discard = PileType.Discard.GetPile(Owner);
            var candidates = discard?.Cards.ToList() ?? new List<CardModel>();
            if (candidates.Count > 0)
            {
                var prefs = new CardSelectorPrefs(SelectionScreenPrompt, 0, count);
                var selected = await CardSelectCmd.FromSimpleGrid(ctx, candidates, Owner, prefs);
                foreach (var c in selected) await CardCmd.Exhaust(ctx, c, false, false);
            }
        }
        else
        {
            await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block.BaseValue, ValueProp.Move, play, false);
        }
    }

    public override List<(string, string)>? Localization => LocManager.Instance.Language switch
    {
        "zhs" => new CardLoc("温室少女",
            "{MuSec}{MuOpen}小睦{MuClose}：获得{Block:diff()}点[gold]格挡[/gold]。{MuSecEnd}\n" +
            "{MoSec}{MoOpen}小墨{MoClose}：从[gold]弃牌堆[/gold]中选择至多{MoExhaust:diff()}张牌，将其消耗。{MoSecEnd}",
            ("selectionScreenPrompt", "选最多{MoExhaust}张弃牌堆中的牌消耗")),
        _ => new CardLoc("Greenhouse Girl",
            "{MuSec}{MuOpen}Mu{MuClose}: Gain {Block:diff()} [gold]Block[/gold].{MuSecEnd}\n" +
            "{MoSec}{MoOpen}Mo{MoClose}: Choose up to {MoExhaust:diff()} cards from your [gold]discard pile[/gold] and exhaust them.{MoSecEnd}",
            ("selectionScreenPrompt", "Choose up to {MoExhaust} discarded cards to Exhaust")),
    };
}
