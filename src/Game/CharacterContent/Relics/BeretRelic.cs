using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace MzmChar.Game;

/// <summary>
/// 贝雷帽（普通）：每场战斗第一次打出「表人格」或「里人格」时，将其移回手牌。
/// 不论卡当前在哪个 pile（默认会到 discard / exhaust），都用 CardPileCmd.Add 移到手牌顶部
/// （参考 ConcertPower 把 Perform 卡拉回手牌的写法）。
/// </summary>
[Pool(typeof(MzmCharRelicPool))]
public class BeretRelic : CustomRelicModel
{
    [SavedProperty]
    private bool UsedThisCombat { get; set; }

    public override RelicRarity Rarity => RelicRarity.Common;

    public override string PackedIconPath           => "res://MzmChar/relics/beret.png";
    protected override string BigIconPath           => "res://MzmChar/relics/beret.png";
    protected override string PackedIconOutlinePath => "res://MzmChar/relics/beret.png";

    public override async Task AfterCardPlayed(PlayerChoiceContext ctx, CardPlay cardPlay)
    {
        if (UsedThisCombat) return;
        if (Owner == null) return;
        if (cardPlay.Card.Owner != Owner) return;  // 多人：只关心自己出的牌
        if (cardPlay.Card is not FrontPersona && cardPlay.Card is not BackPersona) return;

        UsedThisCombat = true;
        Flash();
        // 把刚打出的卡（此时通常在 discard / exhaust）拉回手牌顶部
        await CardPileCmd.Add(cardPlay.Card, PileType.Hand, CardPilePosition.Top, this, false);
    }

    // 每场战斗开始时重置 flag — 比 AfterCombatVictory 更鲁棒（覆盖逃跑/中途结束等情况）。
    // vanilla 标准 hook（IL probe：Anchor / MeatOnTheBone / BeltBuckle 等都用 BeforeCombatStart）。
    public override Task BeforeCombatStart()
    {
        UsedThisCombat = false;
        return Task.CompletedTask;
    }

    public override List<(string, string)>? Localization => LocManager.Instance.Language switch
    {
        "zhs" => new RelicLoc(
            Title:       "红色贝雷帽",
            Description: "每场战斗中，第一次打出[gold]表人格[/gold]或[gold]里人格[/gold]时，将其移回你的手牌。",
            Flavor:      "她好像变了一个人..."),
        _ => new RelicLoc(
            Title:       "Red Beret",
            Description: "The first time each combat you play [gold]Front Persona[/gold] or [gold]Back Persona[/gold], return it to your hand.",
            Flavor:      "She seems like a different person..."),
    };
}
