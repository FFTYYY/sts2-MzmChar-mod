using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;

namespace MzmChar.Game;

/// <summary>
/// 眼泪（稀有）：将 2 张「演奏《春日影》」加入手牌，本回合免费打出。
/// 直接生成卡 → SetToFreeThisTurn → 进 Hand。参考 SecondPersonaRelic.AfterPlayerTurnStart 的加牌模式。
/// </summary>
[Pool(typeof(MzmCharPotionPool))]
public class TearsPotion : CustomPotionModel
{
    public override PotionRarity Rarity => PotionRarity.Rare;
    public override PotionUsage Usage => PotionUsage.CombatOnly;
    public override TargetType TargetType => TargetType.Self;

    public override string? CustomPackedImagePath   => "res://MzmChar/potions/tears.png";
    public override string? CustomPackedOutlinePath => "res://MzmChar/potions/tears.png";

    // 参考 vanilla CunningPotion：药水描述提到具体卡时挂 HoverTipFactory.FromCard<T>
    public override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get { yield return HoverTipFactory.FromCard<PlayHaruhikage>(true); }
    }

    protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature? target)
    {
        if (Owner == null) return;
        var combatState = Owner.Creature.CombatState;
        if (combatState == null) return;

        var cards = new List<CardModel>();
        for (int i = 0; i < 2; i++)
        {
            var c = combatState.CreateCard(ModelDb.Card<PlayHaruhikage>(), Owner);
            c.SetToFreeThisTurn();
            cards.Add(c);
        }
        await Sts2Compat.AddGeneratedCardsToCombat(cards, PileType.Hand, Owner, addedByPlayer: true);
    }

    public override List<(string, string)>? Localization => LocManager.Instance.Language switch
    {
        "zhs" => new PotionLoc(
            Title:       "眼泪",
            Description: "将2张[gold]演奏《春日影》[/gold]加入手牌。这两张牌在本回合可以免费打出。"),
        _ => new PotionLoc(
            Title:       "Tears",
            Description: "Add 2 [gold]Play Haruhikage[/gold] to your hand. They cost 0 this turn."),
    };
}
