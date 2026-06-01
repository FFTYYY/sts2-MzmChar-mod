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
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace MzmChar.Game;

/// <summary>
/// 空洞：2/1 费蓝色技能。获得 N 点临时敏捷（base 4）。消耗。
/// 每打出一次本场游戏获得量永久 +1。
///
/// 实现完全照搬 vanilla `GeneticAlgorithm`（IL-verified probe）：
///   - 两个 `[SavedProperty]`：CurrentDex（每次给的量）+ IncreasedDex（累计成长）
///   - UpdateDex 重算 CurrentDex = BaseDex + IncreasedDex（BaseDex=4）
///   - OnPlay 后 BuffFromPlay 在 `this` + `DeckVersion` 上各调一次
///     （this 改本场战斗的 instance，DeckVersion 改 master，下场战斗复制时带累计值）
///   - LambdaVar 让 loc 的 `{Dex}` 永远反映当前 CurrentDex
/// </summary>
[Pool(typeof(MzmCharCardPool))]
public class Emptiness : MzmCharBaseCard
{
    public override string PortraitPath => "res://MzmChar/cards/emptiness.png";

    private const int BaseDex = 3;

    // SavedProperty 跨战斗 / 读档持久化（vanilla GeneticAlgorithm 同款）
    [SavedProperty] public int CurrentDex { get; set; }
    [SavedProperty] public int IncreasedDex { get; set; }

    private readonly List<DynamicVar> _vars;

    public Emptiness() : base(2, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
        UpdateDex();  // 确保 CurrentDex 跟 IncreasedDex 同步（初次构造默认 BaseDex）
        _vars = new List<DynamicVar>
        {
            // 自定义 var：EnchantedValue 永远 = BaseDex（初始基线）；PreviewValue / BaseValue = CurrentDex
            // → 卡描述用 {Dex:diff()}，CurrentDex > BaseDex 时绿色显示
            new GrowingDexVar(),
        };
    }

    /// <summary>初始 BaseDex + 成长，diff 染色让大于 BaseDex 的当前值显示绿色。</summary>
    private class GrowingDexVar : DynamicVar
    {
        public GrowingDexVar() : base("Dex", BaseDex) { }
        public override void UpdateCardPreview(CardModel card, CardPreviewMode mode, Creature? target, bool runGlobalHooks)
        {
            int current = card is Emptiness e ? e.CurrentDex : BaseDex;
            BaseValue = current;          // 让 {Dex}（不带 :diff()）也显示当前值
            EnchantedValue = BaseDex;     // diff 基线：初始就是 BaseDex
            PreviewValue = current;       // 当前显示的值。比 EnchantedValue 大→绿色
        }
    }

    protected override IEnumerable<DynamicVar> CanonicalVars => _vars;

    private readonly HashSet<CardKeyword> _keywords = new() { CardKeyword.Exhaust };
    public override IEnumerable<CardKeyword> CanonicalKeywords => _keywords;

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get { yield return HoverTipFactory.FromPower<DexterityPower>(); }
    }

    protected override void OnUpgrade() { EnergyCost.UpgradeBy(-1); }

    private void UpdateDex()
    {
        // base + 累计成长
        CurrentDex = BaseDex + IncreasedDex;
    }

    private void BuffFromPlay(int extra)
    {
        IncreasedDex += extra;
        UpdateDex();
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await PlayCast();
        int dex = CurrentDex;
        if (dex > 0)
        {
            await Sts2Compat.PowerApply<DexterityPower>(ctx, Owner.Creature, dex, Owner.Creature, this, false);
            await Sts2Compat.PowerApply<TempDexterityPower>(ctx, Owner.Creature, dex, Owner.Creature, this, true);
        }
        // 永久成长：同时改本场战斗 instance + master deck
        // （this 是本场战斗副本，DeckVersion 是 master；下场战斗复制时会带 IncreasedDex 累计）
        BuffFromPlay(1);
        if (DeckVersion is Emptiness deckCopy)
            deckCopy.BuffFromPlay(1);

    }

    public override List<(string, string)>? Localization => LocManager.Instance.Language switch
    {
        "zhs" => new CardLoc("空洞",
            "本回合获得{Dex:diff()}点[gold]敏捷[/gold]。\n这张牌每被打出一次，获得敏捷的量在本场游戏中永久增加1点。"),
        _ => new CardLoc("Emptiness",
            "Gain {Dex:diff()} [gold]Dexterity[/gold] this turn.\nEach time you play this card, increase the amount by 1 for the rest of this game."),
    };
}
