using System.Reflection;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace MzmChar.Game;

/// <summary>
/// 我们所有 mod 卡的统一基类，目的是把"当前形态高亮 label"的描述注入逻辑
/// 集中在一个地方，每张卡不用重复 override AddExtraArgsToDescription。
///
/// loc 模板里写 `[{MuC}]小睦[/{MuC}]：...` `[{MoC}]小墨[/{MoC}]：...`，
/// 渲染时根据当前形态把 `{MuC}` `{MoC}` 替换成对应颜色 (active=blue, inactive=gold)。
/// </summary>
public abstract class MzmCharBaseCard : CustomCardModel
{
    protected MzmCharBaseCard(int baseCost, CardType type, CardRarity rarity, TargetType target,
        bool showInCardLibrary = true, bool autoAdd = true)
        : base(baseCost, type, rarity, target, showInCardLibrary, autoAdd) { }

    protected override void AddExtraArgsToDescription(LocString description)
    {
        base.AddExtraArgsToDescription(description);
        FormDescription.AddTokens(this, description);
        // IfUpgraded 让 loc 用 SmartFormat 条件 {IfUpgraded:show:upText|baseText} 切换文字。
        // canonical model 的 IsUpgraded 是 false，所以卡库里看的是基础版；玩家升级后再访问的
        // 是 mutable instance，IsUpgraded 是 true，自动切到升级文本
        bool isUp = !IsCanonical && IsUpgraded;
        description.Add("IfUpgraded", isUp);

        // ShowRealEffect：用于「演奏」关键字卡的描述切换。
        //   只有"战斗中 + 在手中 + 没演奏会"才显示 fallback（"获得1点演艺热情"）；
        //   其他场景（卡库、牌堆查看、有演奏会）都显示真效果。
        // canonical instance 无 Owner / Pile，默认 showRealEffect=true（卡库看完整描述）。
        //
        // **必须包装成 IfUpgradedVar**——vanilla ShowIfUpgradedFormatter `isinst IfUpgradedVar` 检查
        // 类型，普通 bool 不匹配会抛 "No suitable Formatter could be found"（IL-verified）。
        // 用 (string, decimal) ctor 自定义 name，手动 set public field `upgradeDisplay`：
        //   Upgraded → 显示 :show: 的第 1 个分支
        //   Normal   → 显示 :show: 的第 2 个分支
        bool isInHand = !IsCanonical && Pile?.Type == PileType.Hand;
        bool hasConcert = !IsCanonical
                          && Owner?.Creature != null
                          && Owner.Creature.HasPower<ConcertPower>();
        bool showRealEffect = !isInHand || hasConcert;
        var realVar = new IfUpgradedVar("ShowRealEffect", showRealEffect ? 1m : 0m)
        {
            upgradeDisplay = showRealEffect ? UpgradeDisplay.Upgraded : UpgradeDisplay.Normal,
        };
        description.Add(realVar);
    }

    /// <summary>
    /// 「演奏」关键字卡的执行侧 + TargetType getter helper：检测是否处于演奏会状态。
    /// canonical-safe：`Owner` getter 在 canonical 实例上会抛 CanonicalModelException，
    /// 所以必须先 `!IsCanonical` 短路。这样 TargetType getter（canonical hover 也会触发）
    /// 也能安全调用本 helper。
    /// </summary>
    protected bool IsInConcert() =>
        !IsCanonical && Owner != null
        && Owner.Creature?.HasPower<ConcertPower>() == true;

    // CardModel.set_BaseReplayCount 调 AssertMutable() → 在 canonical model 构造期会抛
    // CanonicalModelException（vanilla 没有任何卡在 ctor 里设它）。要给一张卡"默认重放 N 次"，
    // 必须绕过 setter 直接写 _baseReplayCount field（field load 是普通赋值，没 mutable 检查）。
    private static readonly FieldInfo? BaseReplayCountField =
        typeof(CardModel).GetField("_baseReplayCount", BindingFlags.NonPublic | BindingFlags.Instance);

    protected void SetDefaultReplayCount(int count)
    {
        BaseReplayCountField?.SetValue(this, count);
    }

    /// <summary>
    /// 播 cast 动画 —— vanilla 标准 boilerplate（每张想要 cast 动画的技能/能力卡 OnPlay 第一句 await 这个）。
    /// 攻击卡不用：DamageCmd.Attack(...).Execute(...) 内部 AttackCommand.Execute 已自动触发 attack 动画。
    /// 纯获取格挡的卡也不用：vanilla DefendIronclad/DefendDefect 一致只播 block VFX，没 cast 动画。
    /// </summary>
    protected Task PlayCast()
        => CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
}
