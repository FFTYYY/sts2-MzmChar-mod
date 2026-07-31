using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;

namespace MzmChar.Game;

/// <summary>
/// 一心化万：2/1 费金色能力卡。联机专用。
///   所有队友（不含自己）进入小睦，并获得你完整牌组的复制，洗入各自抽牌堆。
///
/// 类名带 Mu 前缀 —— vanilla 已有 OneForAll（一心化万），同名类会撞 ModelId（MuMask 同前例）。
///
/// 牌组卡是 mutable 实例，不能直接喂 CombatState.CreateCard（其 ToMutable = AssertCanonical +
/// MutableClone，非 canonical 直接抛 CanonicalModelException，IL-verified）；CardModel.CreateClone
/// 又只接受战斗堆内的卡。所以走 canonical 重建：ModelDb.Card&lt;T&gt;（MakeGenericMethod）拿同类
/// canonical → CreateCard → 按原卡 CurrentUpgradeLevel 补升级（CardCmd.Upgrade + PreviewStyle.None，
/// NeverHappyInBand 同模式）。
/// </summary>
[Pool(typeof(MzmCharCardPool))]
public class MuOneForAll : MzmCharBaseCard
{
    public override string PortraitPath => "res://MzmChar/cards/mu_one_for_all.png";

    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get { foreach (var t in FormTooltips.EnterMu()) yield return t; }
    }

    public MuOneForAll() : base(2, CardType.Power, CardRarity.Rare, TargetType.Self) { }

    protected override void OnUpgrade() { EnergyCost.UpgradeBy(-1); /* 2 → 1 */ }

    private static readonly MethodInfo CardOfT =
        typeof(ModelDb).GetMethod("Card", BindingFlags.Public | BindingFlags.Static)!;

    /// <summary>同类 canonical；未注册类型（理论不该有）返回 null，调用方跳过。
    /// 异常只取决于卡的类型 → 各客户端跳过行为一致，无 desync。</summary>
    private static CardModel? CanonicalOf(CardModel instance)
    {
        try { return (CardModel?)CardOfT.MakeGenericMethod(instance.GetType()).Invoke(null, null); }
        catch { return null; }
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        // 不 PlayCast —— 能力牌自带出牌动画（PlayPowerCardFlyVfx），无需额外 cast
        if (Owner?.RunState == null) return;
        var deck = Owner.Deck?.Cards;
        if (deck == null) return;
        // snapshot：master 牌组战斗中不变，但防御性固定一份遍历序（各客户端同序）
        var deckSnapshot = deck.ToList();

        var allResults = new List<CardPileAddResult>();
        foreach (var p in Owner.RunState.Players)
        {
            if (p?.Creature?.CombatState == null) continue;
            if (!p.Creature.IsAlive) continue;

            if (p == Owner) continue;  // 「队友」不含自己：进入小睦和牌组复制都只给队友

            await Forms.EnterMutsumi(p, this, ctx);

            var copies = new List<CardModel>();
            foreach (var deckCard in deckSnapshot)
            {
                var canonical = CanonicalOf(deckCard);
                if (canonical == null)
                {
                    ModEntry.Log($"[MuOneForAll] no canonical for {deckCard.GetType().Name}, skip");
                    continue;
                }
                var copy = p.Creature.CombatState.CreateCard(canonical, p);
                if (copy == null) continue;
                for (int i = 0; i < deckCard.CurrentUpgradeLevel && copy.IsUpgradable; i++)
                    CardCmd.Upgrade(copy, CardPreviewStyle.None);
                copies.Add(copy);
            }
            if (copies.Count > 0)
            {
                var results = await Sts2Compat.AddGeneratedCardsToCombat(
                    copies, PileType.Draw, p, CardPilePosition.Random);
                allResults.AddRange(results);
            }
        }

        // 生成特效。战斗内只能用 HorizontalLayout / MessyLayout —— GridLayout / EventLayout 在
        // PreviewInternal 里 IsCombatPile 分支直接 throw InvalidOperationException（IL-verified，坑 #69）。
        // PreviewInternal 有 LocalContext.IsMine 过滤（纯本地视觉），每个客户端只显示自己收到的那批。
        if (allResults.Count > 0)
            CardCmd.PreviewCardPileAdd(allResults, 2.5f, CardPreviewStyle.HorizontalLayout);
    }

    public override List<(string, string)>? Localization => LocManager.Instance.Language switch
    {
        "zhs" => new CardLoc("一心化万",
            "所有队友[gold]进入小睦[/gold]。所有队友将一份你的完整卡组的复制品加入抽牌堆。"),
        _ => new CardLoc("One for All",
            "All allies [gold]Enter Mu[/gold]. Each ally adds a copy of your entire deck to their draw pile."),
    };
}
