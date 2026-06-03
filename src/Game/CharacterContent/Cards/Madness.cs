using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace MzmChar.Game;

/// <summary>
/// 疯癫：1 费蓝色技能。
///   通用：获得 PlayCount 点能量 —— 初始 0（不再获得费用）。每打出一次后，下次能量量 +1（本战斗内永久）
///   小睦：额外获得 2/3 能量（不受 PlayCount 影响），进入小墨
///   小墨：抽 (3/4 + PlayCount) 张牌，进入小睦 —— 每打出一次抽数也 +1
///   每打出一次：(1) 耗能 +1（EnergyCost.AddThisCombat）；(2) 通用 gain +1；(3) Mo 抽数 +1（都靠 PlayCount）
///
/// 自增长部分参考 Emptiness.GrowingDexVar：自定义 DynamicVar 读 SavedProperty，
/// 让卡描述的 {Gain:energyIcons()} / {Cards:diff()} 动态反映 base + PlayCount。
/// 顶部「获得能量」一行用 HasGain (IfUpgradedVar) 在 PlayCount=0 时隐藏（卡库 / 未打出过时不显示空 line）。
/// </summary>
[Pool(typeof(MzmCharCardPool))]
public class Madness : MzmCharBaseCard
{
    public override string PortraitPath => "res://MzmChar/cards/madness.png";

    private const int BaseGain = 0;            // 初始 0（再不获得费用）；PlayCount 决定增长
    private const int CardsBaseUnupgraded = 3;
    private const int CardsUpgradeBonus = 1;

    // 本战斗内累积：每打出一次 +1。SavedProperty 跨存档持久化；
    // 新战斗时 deck master copy 重新实例化，自然 PlayCount=0。
    [SavedProperty] public int PlayCount { get; set; }

    private readonly List<DynamicVar> _vars;

    public Madness() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
        _vars = new List<DynamicVar>
        {
            new GrowingGainVar(),                // 通用能量 = BaseGain + PlayCount
            new DynamicVar("MuExtra", 2m),       // Mu 额外 2/3（不参与自增长）
            new GrowingCardsVar(),               // Mo 抽数 = (2 或 3) + PlayCount
        };
    }

    /// <summary>
    /// 通用能量 = BaseGain + PlayCount。EnchantedValue 锁 BaseGain → diff 染色让 PlayCount>0 时显示绿色。
    /// 参考 Emptiness.GrowingDexVar。
    ///
    /// **必须继承 EnergyVar 而非 DynamicVar** —— IL-verified: vanilla `EnergyIconsFormatter.TryEvaluateFormat`
    /// 的第一段 `isinst EnergyVar` 检查，不是 EnergyVar 子类则 fallback 报"Unknown value=..."。
    /// EnergyVar 是 DynamicVar 的薄壳子类（只加了 ColorPrefix）—— 继承它后 `{Gain:energyIcons()}`
    /// 走 `get_PreviewValue()` 渲染当前值的图标数。
    /// </summary>
    private class GrowingGainVar : EnergyVar
    {
        public GrowingGainVar() : base("Gain", BaseGain) { }
        public override void UpdateCardPreview(CardModel card, CardPreviewMode mode, Creature? target, bool runGlobalHooks)
        {
            int current = card is Madness m ? BaseGain + m.PlayCount : BaseGain;
            BaseValue = current;
            EnchantedValue = BaseGain;
            PreviewValue = current;
        }
    }

    /// <summary>
    /// Mo 抽数 = base(2 或 3) + PlayCount。**必须从常量重算，不能从 BaseValue 累加**——
    /// `DynamicVar.UpdateCardPreview` 和 `CardsVar.UpdateCardPreview` 都是空方法（IL-verified，size=1）。
    /// 父类不重置 BaseValue → 之前 `: CardsVar` + `BaseValue = BaseValue + played` 写法每次
    /// UpdateCardPreview 被调（hand 重排 / hover / UI 刷新都会调）都累加一次 PlayCount，
    /// 视觉上看像"统计总打牌数"。OnPlay 用常量算所以实际抽数对，只是显示飘了。
    /// 标准写法参考 Silence.GrowingDrawsVar / Emptiness.GrowingDexVar / MirrorDoll.GrowingHitsVar。
    /// </summary>
    private class GrowingCardsVar : DynamicVar
    {
        public GrowingCardsVar() : base("Cards", CardsBaseUnupgraded) { }
        public override void UpdateCardPreview(CardModel card, CardPreviewMode mode, Creature? target, bool runGlobalHooks)
        {
            int baseDraw = CardsBaseUnupgraded + (card.IsUpgraded ? CardsUpgradeBonus : 0);
            int played = card is Madness m ? m.PlayCount : 0;
            int total = baseDraw + played;
            BaseValue      = total;                  // 让 {Cards}（不带 :diff()）也显示当前值
            EnchantedValue = CardsBaseUnupgraded;    // 基线 = 2（未升级 base）→ 升级 +1 / PlayCount 增长 都通过 :diff() 显绿
            PreviewValue   = total;
        }
    }

    protected override IEnumerable<DynamicVar> CanonicalVars => _vars;

    // 双形态都「进入X」 → 双挂 form tip
    protected override IEnumerable<IHoverTip> ExtraHoverTips => FormTooltips.BothEnter();

    // HasGain：PlayCount > 0 时为 true，控制描述顶部「获得能量」一行的显隐。
    // 用 IfUpgradedVar (不是裸 bool) 因为自定义 token 的 :show: formatter 要 isinst IfUpgradedVar
    // （参 MzmCharBaseCard.ShowRealEffect 同模式）
    protected override void AddExtraArgsToDescription(LocString description)
    {
        base.AddExtraArgsToDescription(description);
        bool hasGain = !IsCanonical && Owner != null && PlayCount > 0;
        var hasGainVar = new IfUpgradedVar("HasGain", hasGain ? 1m : 0m)
        {
            upgradeDisplay = hasGain ? UpgradeDisplay.Upgraded : UpgradeDisplay.Normal,
        };
        description.Add(hasGainVar);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["MuExtra"].UpgradeValueBy(1);  // Mu 额外 2 → 3
        DynamicVars["Cards"].UpgradeValueBy(1);    // Mo 抽 2 → 3（GrowingCardsVar 继承 CardsVar，走 vanilla 升级路径）
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        // 通用：获得 PlayCount 点能量（BaseGain=0 → 首次打出 0 能量）
        int gain = BaseGain + PlayCount;
        if (gain > 0)
            await PlayerCmd.GainEnergy(gain, Owner);

        if (Forms.IsMortisForm(Owner))
        {
            // Mo 抽数 = base (2/3 depending on upgrade) + PlayCount
            int upgradeBonus = IsUpgraded ? CardsUpgradeBonus : 0;
            int draws = CardsBaseUnupgraded + upgradeBonus + PlayCount;
            await CardPileCmd.Draw(ctx, draws, Owner, false);
            await Forms.EnterMutsumi(Owner, this, ctx);
        }
        else
        {
            // Mu 额外 2/3 能量（不受 PlayCount 影响）
            await PlayerCmd.GainEnergy((int)DynamicVars["MuExtra"].BaseValue, Owner);
            await Forms.EnterMortis(Owner, this, ctx);
        }

        // 本场战斗永久 +1 费用（参 vanilla BansheesCry IL-verified：用 EnergyCost.AddThisCombat 而非
        // CardModel.SetStarCostThisCombat —— 后者走 TemporaryCardCost 路径会被 cleanup 清掉。
        // AddThisCombat 走 _localModifiers 列表，LocalCostType=Combat 持续整场战斗）
        EnergyCost.AddThisCombat(1, false);
        // 通用 gain 计数 +1（本战斗内累积）
        PlayCount++;
    }

    public override List<(string, string)>? Localization => LocManager.Instance.Language switch
    {
        "zhs" => new CardLoc("疯癫",
            "{HasGain:show:获得{Gain:energyIcons()}。\n|}" +
            "{MuSec}{MuOpen}小睦{MuClose}：获得{IfUpgraded:show:{energyPrefix:energyIcons(3)}|{energyPrefix:energyIcons(2)}}。[gold]进入小墨[/gold]。{MuSecEnd}\n" +
            "{MoSec}{MoOpen}小墨{MoClose}：抽{Cards:diff()}张牌。[gold]进入小睦[/gold]。{MoSecEnd}\n" +
            "这张牌每被打出一次，耗能增加1，[gold]小墨[/gold]抽牌数增加1，且为这张牌添加效果：获得{energyPrefix:energyIcons(1)}。"),
        _ => new CardLoc("Madness",
            "{HasGain:show:Gain {Gain:energyIcons()}.\n|}" +
            "{MuSec}{MuOpen}Mu{MuClose}: Gain {IfUpgraded:show:{energyPrefix:energyIcons(3)}|{energyPrefix:energyIcons(2)}} more; [gold]Enter Mo[/gold].{MuSecEnd}\n" +
            "{MoSec}{MoOpen}Mo{MoClose}: Draw {Cards:diff()}; [gold]Enter Mu[/gold].{MoSecEnd}\n" +
            "Each time this card is played, its cost increases by 1, add the effect to this card: gain {energyPrefix:energyIcons(1)}, and [gold]Mo[/gold] draw count increases by 1."),
    };
}
