using System;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
// AbstractModel for hook out modifiers parameter

namespace MzmChar.Game;

/// <summary>
/// 通用 calculated DynamicVar：每次卡牌 UpdateCardPreview 时调一次 lambda 算出当前值。
/// 用于"抽牌数 = 本回合以 X 形态打出的牌数"这类需要实时算的 N。
///
/// 有 ModifierKind 选项决定是否再用 Hook.ModifyDamage / Hook.ModifyBlock 处理一次：
///   - None  : PreviewValue = lambda 返回值。loc 显示 base 值
///   - Damage: PreviewValue = Hook.ModifyDamage(lambda 值)（应用 Strength / Vulnerable / etc.）
///   - Block : PreviewValue = Hook.ModifyBlock(lambda 值)（应用 Dex / Frail / etc.）
///
/// loc 里 "{Name}" 显示 PreviewValue（基于 ToString(IFormatProvider) 是 BaseValue 的反例 —
/// 我们这里手动让 BaseValue==PreviewValue，这样 plain `{Name}` 也能显示 modifier-applied 值）。
/// 用 ":diff()" 时也是从 PreviewValue 取。
///
/// 用法：
///   new LambdaVar("MoTotalDmg", card => MuCards * Per, ModifierKind.Damage)
///   OnPlay 用 (int)DynamicVars["MoTotalDmg"].BaseValue 拿到 modifier-applied 值（注意：是 modified 不是 base）
///   如果你 OnPlay 想要 raw base，自己再算一遍。
/// </summary>
public class LambdaVar : DynamicVar
{
    public enum ModifierKind { None, Damage, Block }

    private readonly Func<CardModel, decimal>? _calc;
    private readonly Func<CardModel, Creature?, decimal>? _calcWithTarget;
    private readonly ModifierKind _kind;

    public LambdaVar(string name, Func<CardModel, decimal> calc, ModifierKind kind = ModifierKind.None)
        : base(name, 0)
    {
        _calc = calc;
        _kind = kind;
    }

    /// <summary>
    /// Target-aware overload: lambda 拿到当前 hover 的 target（Imitate 这种"基于敌人意图"算的卡用）。
    /// `card.CurrentTarget` 在 hover 预览时不一定及时——直接拿 UpdateCardPreview 的 target 参数最可靠。
    /// </summary>
    public LambdaVar(string name, Func<CardModel, Creature?, decimal> calc, ModifierKind kind = ModifierKind.None)
        : base(name, 0)
    {
        _calcWithTarget = calc;
        _kind = kind;
    }

    public override void UpdateCardPreview(CardModel card, CardPreviewMode previewMode, Creature? target, bool runGlobalHooks)
    {
        var raw = _calcWithTarget != null ? _calcWithTarget(card, target) : _calc!(card);

        // 关键（IL-verified `DynamicVar`）：plain `{Name}` → ToString → IntValue → BaseValue
        //   vanilla DamageVar 让 BaseValue 保持 raw、PreviewValue 装 modifier-applied，
        //   因为 vanilla 卡都用 `{Damage:diff()}` 触发 ToHighlightedString → PreviewValue。
        //   我们的 LambdaVar 常用 plain `{Name}`（FightForBody 的 MuActual/MoActual），
        //   所以两值都设成 modifier-applied，让 plain 和 :diff() 都显示最终值。
        if (_kind == ModifierKind.None || card.Owner == null || card.CombatState == null)
        {
            BaseValue = raw;
            EnchantedValue = raw;
            PreviewValue = raw;
            return;
        }

        var creature = card.Owner.Creature;
        if (creature == null) { BaseValue = raw; EnchantedValue = raw; PreviewValue = raw; return; }

        // EnchantedValue = raw (modifier 应用之前)，PreviewValue = modified
        // ToHighlightedString (IL-verified) 用 PreviewValue.CompareTo(EnchantedValue) 决定绿/红染色
        // —— 必须在 :diff() formatter 下才生效（plain `{Var}` 走 ToString→BaseValue 不染色）
        decimal modified;
        if (_kind == ModifierKind.Damage)
        {
            var dealer = creature;
            var dmgTarget = target ?? creature;
            // v0.108 加了 CardPlay 参数；display preview 无 CardPlay context，传 null
            // （vanilla DamageVar.UpdateCardPreview 也这么做，IL-verified，见 report_57 §4.3）
            modified = Sts2Compat.ModifyDamageCompat(
                card.Owner.RunState!, card.CombatState, dmgTarget, dealer,
                raw, ValueProp.Move, card, null,
                ModifyDamageHookType.All, previewMode, out _);
        }
        else // Block
        {
            modified = Hook.ModifyBlock(
                card.CombatState, creature, raw, ValueProp.Move, card, null, out _);
        }
        BaseValue = modified;
        EnchantedValue = raw;
        PreviewValue = modified;
    }
}
