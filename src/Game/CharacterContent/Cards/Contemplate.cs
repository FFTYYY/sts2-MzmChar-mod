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
/// 低头沉思：1 费蓝色技能。
///   小墨：从弃牌堆选一张加入手牌（升级：被选中的牌获得「保留」）
///   小睦：获得 8/12 点格挡
/// </summary>
[Pool(typeof(MzmCharCardPool))]
public class Contemplate : MzmCharBaseCard
{
    public override string PortraitPath => "res://MzmChar/cards/contemplate.png";

    private readonly List<DynamicVar> _vars = new()
    {
        new BlockVar(8, ValueProp.Move),
    };
    protected override IEnumerable<DynamicVar> CanonicalVars => _vars;

    public Contemplate() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(4);   // 8 → 12
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        if (Forms.IsMortisForm(Owner))
        {
            var discard = PileType.Discard.GetPile(Owner);
            var candidates = discard?.Cards.ToList() ?? new List<CardModel>();
            if (candidates.Count > 0)
            {
                var prefs = new CardSelectorPrefs(SelectionScreenPrompt, selectCount: 1);
                var selected = await CardSelectCmd.FromSimpleGrid(ctx, candidates, Owner, prefs);
                var picked = selected.FirstOrDefault();
                if (picked != null)
                {
                    if (IsUpgraded) picked.AddKeyword(CardKeyword.Retain);
                    await CardPileCmd.Add(picked, PileType.Hand, CardPilePosition.Top, null);
                }
            }
        }
        else
        {
            await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block.BaseValue, ValueProp.Move, play, false);
        }
    }

    public override List<(string, string)>? Localization => LocManager.Instance.Language switch
    {
        "zhs" => new CardLoc("低头沉思",
            "{MuSec}{MuOpen}小睦{MuClose}：获得{Block:diff()}点[gold]格挡[/gold]。{MuSecEnd}\n" +
            "{MoSec}{MoOpen}小墨{MoClose}：从弃牌堆中选一张牌加入手牌。{IfUpgraded:show:这张牌获得[gold]保留[/gold]。|}{MoSecEnd}",
            ("selectionScreenPrompt", "选一张牌加入手牌")),
        _ => new CardLoc("Contemplate",
            "{MuSec}{MuOpen}Mu{MuClose}: Gain {Block:diff()} [gold]Block[/gold].{MuSecEnd}\n" +
            "{MoSec}{MoOpen}Mo{MoClose}: Add a card from your discard pile to your hand.{IfUpgraded:show: That card gains [gold]Retain[/gold].|}{MoSecEnd}",
            ("selectionScreenPrompt", "Choose a card to add to your hand")),
    };
}
