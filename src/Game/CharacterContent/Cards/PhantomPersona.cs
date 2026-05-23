using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.CommonUi;

namespace MzmChar.Game;

/// <summary>
/// 幻想人格：1 费蓝色技能。消耗。
///   基础：移除所有「若叶睦的临时敏捷」debuff
///   升级：上述效果 + 在弃牌堆中放入一张此牌的[gold]虚无[/gold]复制品
///         （复制品也是升级版且也带本效果，无限链；带 Ethereal → 进手未打则回合末自消耗，链自然终止）
///
/// 升级"放副本"实现：参考 vanilla `Undeath`（`CreateClone` + `AddGeneratedCardToCombat`）
/// + 我们已有 `DisintegrationPower.OnPersonaSwitch` 的多人安全 `CardCmd.ApplyKeyword(Ethereal)` 模式。
/// `CreateClone` 走 `CombatState.CloneCard` → `AbstractModel.ClonePreservingMutability` 深克隆，
/// 保留升级状态（IL 实证 `_currentUpgradeLevel` 也复制）。
/// </summary>
[Pool(typeof(MzmCharCardPool))]
public class PhantomPersona : MzmCharBaseCard
{
    public override string PortraitPath => "res://MzmChar/cards/phantom_persona.png";

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromPower<DexterityPower>();
            yield return HoverTipFactory.FromPower<TempDexterityPower>();
        }
    }

    private readonly HashSet<CardKeyword> _keywords = new() { CardKeyword.Exhaust };
    public override IEnumerable<CardKeyword> CanonicalKeywords => _keywords;

    public PhantomPersona() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }

    // 升级**不**改 Exhaust 或费用 —— 升级新效果靠 IsUpgraded 在 OnPlay 里判定
    protected override void OnUpgrade() { }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await PlayCast();
        // 合并后 TempDexterityPower 的语义：Amount > 0 = "回合结束失去敏捷"（要被本卡移除）；
        // Amount < 0 = "回合结束恢复敏捷"（Distort 的 Mu 路径用的，不能误删）
        var pw = Owner.Creature.GetPower<TempDexterityPower>();
        if (pw != null && pw.Amount > 0)
            await PowerCmd.Remove<TempDexterityPower>(Owner.Creature);

        if (IsUpgraded)
        {
            // 参考 vanilla Undeath：CreateClone 在 OnPlay 内合法（this 在 PileType.Play, IsCombatPile=true）
            // ApplyKeyword 必须在 AddGeneratedCardToCombat 之前 —— Add 之后 PreviewCardPileAdd
            // 显示已带 Ethereal 的最终态
            var clone = CreateClone();
            CardCmd.ApplyKeyword(clone, new[] { CardKeyword.Ethereal });
            var result = await Sts2Compat.AddGeneratedCardToCombat(clone, PileType.Discard, Owner, CardPilePosition.Bottom);
            // ★ 关键：没有这一行就只有"卡进 pile"无动画。vanilla Undeath IL 实证用 2.2s + HorizontalLayout
            CardCmd.PreviewCardPileAdd(result, 2.2f, CardPreviewStyle.HorizontalLayout);
        }
    }

    public override List<(string, string)>? Localization => LocManager.Instance.Language switch
    {
        "zhs" => new CardLoc("幻想人格",
            "移除所有[gold]若叶睦的临时敏捷[/gold]。{IfUpgraded:show:在弃牌堆中放入一张此牌的[gold]虚无[/gold]复制品。|}"),
        _ => new CardLoc("Phantom Persona",
            "Remove all \"lose [gold]Dexterity[/gold] at end of turn\" debuffs.{IfUpgraded:show: Add an [gold]Ethereal[/gold] copy of this card to your discard pile.|}"),
    };
}
