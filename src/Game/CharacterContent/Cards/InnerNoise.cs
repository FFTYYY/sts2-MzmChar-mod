using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;

namespace MzmChar.Game;

/// <summary>
/// 心底的噪音：1/0 费金色技能。随机印 3 张 0 费牌（从 MzmCharCardPool）。
/// 参考 vanilla Jackpot：filter 用 `EnergyCost.Canonical == 0 && !CostsX`（排除 X 费=0 误中）；
/// 用 `CardFactory.GetForCombat` 拿随机 N 张 + `CardPileCmd.AddGeneratedCardToCombat` 加到手牌。
/// </summary>
[Pool(typeof(MzmCharCardPool))]
public class InnerNoise : MzmCharBaseCard
{
    public override string PortraitPath => "res://MzmChar/cards/inner_noise.png";

    public InnerNoise() : base(1, CardType.Skill, CardRarity.Rare, TargetType.Self) { }

    protected override void OnUpgrade() { EnergyCost.UpgradeBy(-1); }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await PlayCast();
        var pool = ModelDb.CardPool<MzmCharCardPool>();
        var allCards = pool?.AllCards;
        var rng = Owner.RunState?.Rng?.CombatCardSelection;
        bool isMultiplayer = Owner.RunState != null && Owner.RunState.Players.Count > 1;

        var candidates = allCards?.Where(c =>
            c.CanBeGeneratedInCombat   // ⚠️ 排除里人格/表人格/不和谐音/MutsumiCharge 等特殊卡
            && c.EnergyCost != null && c.EnergyCost.Canonical == 0 && !c.EnergyCost.CostsX
            && (isMultiplayer
                ? c.MultiplayerConstraint != CardMultiplayerConstraint.SingleplayerOnly
                : c.MultiplayerConstraint != CardMultiplayerConstraint.MultiplayerOnly))
            .ToList() ?? new List<CardModel>();

        if (rng != null && candidates.Count > 0)
        {
            // 用 vanilla CardFactory.GetForCombat 拿 3 张（参考 Jackpot 模式）
            var picked = CardFactory.GetForCombat(Owner, candidates, 3, rng);
            foreach (var card in picked)
            {
                await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, false, CardPilePosition.Top);
            }
        }
        if (Forms.IsMortisForm(Owner)) await CombatCounters.BumpMortisCard(ctx, Owner);
        else                            await CombatCounters.BumpMutsumiCard(ctx, Owner);
    }

    public override List<(string, string)>? Localization => LocManager.Instance.Language switch
    {
        "zhs" => new CardLoc("心底的噪音",
            "随机获得3张0{energyPrefix:energyIcons(1)}牌。"),
        _ => new CardLoc("Inner Noise",
            "Add 3 random 0{energyPrefix:energyIcons(1)} cards to your hand."),
    };
}
