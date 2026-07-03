using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;

namespace MzmChar.Game;

/// <summary>
/// vanilla cmd/hook 集中入口 + 运行时版本兼容层。
///
/// 策略：**编译只用一份代码，运行时按版本分派**。
///   1. 启动时探测 <c>Hook.ModifyDamage</c> 参数个数（beta = 11、stable = 10），置 <c>IsBeta</c> 全局
///   2. 三个签名分歧的 vanilla API（FromCard / ModifyDamage / CreatureCmd.Damage）用 <c>MethodInfo.Invoke</c>
///      按 <c>IsBeta</c> 分派 —— beta 装的 dll 有 <c>CardPlay</c> 参、stable 装的没有
///   3. 业务文件只调用本文件里的 wrapper（`FromCardCompat` / `ModifyDamageCompat` / `CreatureDamage`）
///
/// v0.108 beta 一起加了 <c>CardPlay</c> 到伤害管线（FromCard/ModifyDamage/CreatureCmd.Damage 都变），
/// 探一个就够，全局分派开销可忽略（<c>MethodInfo.Invoke</c> 比直调慢 ~100x，但攻击卡不是热路径）。
///
/// 门 rollback 步骤：notes/api_version_gating.md
/// </summary>
public static class Sts2Compat
{
    // ============ 版本检测 + MethodInfo 缓存（静态 ctor 里跑一次）============

    /// <summary>true = beta (v0.108+)，false = stable (v0.107)。由 Hook.ModifyDamage 参数个数决定</summary>
    public static readonly bool IsBeta;

    private static readonly MethodInfo _fromCard;
    private static readonly MethodInfo _modifyDamage;
    private static readonly MethodInfo _creatureDamage;

    static Sts2Compat()
    {
        // 探针：Hook.ModifyDamage 有多少个参？v0.108 加了 CardPlay → 11 参；v0.107 = 10 参
        var mdOverloads = typeof(Hook).GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(m => m.Name == "ModifyDamage").ToList();
        if (mdOverloads.Count == 0)
            throw new InvalidOperationException("Sts2Compat init: Hook.ModifyDamage not found");
        _modifyDamage = mdOverloads[0];
        IsBeta = _modifyDamage.GetParameters().Length == 11;

        // 按 IsBeta 定位 AttackCommand.FromCard 目标 overload
        var fromCardTypes = IsBeta
            ? new[] { typeof(CardModel), typeof(CardPlay) }
            : new[] { typeof(CardModel) };
        _fromCard = typeof(AttackCommand).GetMethod("FromCard", fromCardTypes)
            ?? throw new InvalidOperationException(
                $"Sts2Compat init: AttackCommand.FromCard(IsBeta={IsBeta}) not found");

        // 按 IsBeta 定位 CreatureCmd.Damage 目标 overload（6-参 stable / 7-参 beta，带 dealer + cardSource）
        var damageTypes = IsBeta
            ? new[] { typeof(PlayerChoiceContext), typeof(Creature), typeof(decimal), typeof(ValueProp),
                      typeof(Creature), typeof(CardModel), typeof(CardPlay) }
            : new[] { typeof(PlayerChoiceContext), typeof(Creature), typeof(decimal), typeof(ValueProp),
                      typeof(Creature), typeof(CardModel) };
        _creatureDamage = typeof(CreatureCmd).GetMethod("Damage", damageTypes)
            ?? throw new InvalidOperationException(
                $"Sts2Compat init: CreatureCmd.Damage(IsBeta={IsBeta}) not found");
    }

    // ============ 签名未变的 wrapper（直调，无版本分歧）============

    public static Task PowerApply<T>(
        PlayerChoiceContext? ctx,
        Creature target,
        decimal amount,
        Creature applier,
        CardModel? cardSource,
        bool silent = false)
        where T : PowerModel, new()
        => PowerCmd.Apply<T>(ctx!, target, amount, applier, cardSource, silent);

    public static Task PowerModifyAmount(
        PlayerChoiceContext? ctx,
        PowerModel power,
        decimal offset,
        Creature applier,
        CardModel? cardSource,
        bool silent = false)
        => PowerCmd.ModifyAmount(ctx!, power, offset, applier, cardSource, silent);

    public static Task<CardPileAddResult> AddGeneratedCardToCombat(
        CardModel card,
        PileType newPileType,
        Player creator,
        CardPilePosition position = CardPilePosition.Bottom,
        bool addedByPlayer = true)
        => CardPileCmd.AddGeneratedCardToCombat(card, newPileType, creator, position);

    public static int MaxCardsInHand => CardPile.MaxCardsInHand;

    public static Task AddGeneratedCardsToCombat(
        IEnumerable<CardModel> cards,
        PileType newPileType,
        Player creator,
        CardPilePosition position = CardPilePosition.Bottom,
        bool addedByPlayer = true)
        => CardPileCmd.AddGeneratedCardsToCombat(cards, newPileType, creator, position);

    // ============ v0.107 vs v0.108 分歧的 wrapper（反射分派）============

    /// <summary>
    /// AttackCommand.FromCard 版本兼容。v0.108 加了 <c>CardPlay</c> 第 2 参。业务卡 OnPlay 里有 <c>play</c>，
    /// stable 分派会忽略 <c>play</c>。
    /// </summary>
    public static AttackCommand FromCardCompat(this AttackCommand cmd, CardModel card, CardPlay? play)
    {
        var args = IsBeta
            ? new object?[] { card, play }
            : new object?[] { card };
        return (AttackCommand)_fromCard.Invoke(cmd, args)!;
    }

    /// <summary>
    /// Hook.ModifyDamage 版本兼容。v0.108 在 <c>cardSource</c> 与 <c>modifyDamageHookType</c> 之间加了
    /// <c>CardPlay cardPlay</c>。display 路径（LambdaVar / *Var.UpdateCardPreview）传 null 是 vanilla 认证
    /// 模式（6/7 vanilla 调用点都传 null，见 report_57 §4.3）。
    /// </summary>
    public static decimal ModifyDamageCompat(
        IRunState runState,
        ICombatState combatState,
        Creature target,
        Creature dealer,
        decimal damage,
        ValueProp props,
        CardModel? cardSource,
        CardPlay? cardPlay,
        ModifyDamageHookType modifyDamageHookType,
        CardPreviewMode previewMode,
        out IEnumerable<AbstractModel> modifiers)
    {
        // 反射 Invoke 的 arg 数组末尾放一个 slot 承接 out 参
        object?[] args = IsBeta
            ? new object?[] { runState, combatState, target, dealer, damage, props, cardSource, cardPlay,
                              modifyDamageHookType, previewMode, null }
            : new object?[] { runState, combatState, target, dealer, damage, props, cardSource,
                              modifyDamageHookType, previewMode, null };
        var result = (decimal)_modifyDamage.Invoke(null, args)!;
        modifiers = (IEnumerable<AbstractModel>)args[args.Length - 1]!;
        return result;
    }

    /// <summary>
    /// CreatureCmd.Damage 版本兼容（(ctx, target, amount, props, dealer, cardSource[, cardPlay]) 那个 overload）。
    /// v0.108 加了 <c>CardPlay</c> 第 7 参。业务调用点都在 OnPlay 里，直接传 <c>play</c> 即可；stable 分派忽略。
    /// </summary>
    public static Task CreatureDamage(
        PlayerChoiceContext ctx,
        Creature target,
        decimal amount,
        ValueProp props,
        Creature dealer,
        CardModel? cardSource,
        CardPlay? cardPlay)
    {
        object?[] args = IsBeta
            ? new object?[] { ctx, target, amount, props, dealer, cardSource, cardPlay }
            : new object?[] { ctx, target, amount, props, dealer, cardSource };
        return (Task)_creatureDamage.Invoke(null, args)!;
    }
}
