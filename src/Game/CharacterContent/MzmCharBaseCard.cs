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
        // canonical 上 IsUpgraded 永远 false，mutable 实例才反映真实升级状态
        bool isUp = !IsCanonical && IsUpgraded;
        description.Add("IfUpgraded", isUp);

        // ShowRealEffect 控制「演奏」关键字卡的描述切换：
        //   战斗中 + 在手中 + 没演奏会 → fallback「获得1点演艺热情」
        //   其他场景 → 真效果
        // 必须包装成 IfUpgradedVar（ShowIfUpgradedFormatter 走 isinst 检查类型）
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
    /// 「演奏」关键字卡检测是否在演奏会状态。canonical-safe（先 <c>!IsCanonical</c> 短路防 Owner 抛）。
    /// </summary>
    protected bool IsInConcert() =>
        !IsCanonical && Owner != null
        && Owner.Creature?.HasPower<ConcertPower>() == true;

    // ctor 时 set_BaseReplayCount 会抛 CanonicalModelException，绕过 setter 直写 backing field
    private static readonly FieldInfo? BaseReplayCountField =
        typeof(CardModel).GetField("_baseReplayCount", BindingFlags.NonPublic | BindingFlags.Instance);

    protected void SetDefaultReplayCount(int count)
    {
        BaseReplayCountField?.SetValue(this, count);
    }

    /// <summary>
    /// 播 cast 动画。技能/能力卡 OnPlay 第一句 await 这个。攻击卡和纯格挡卡不用。
    /// </summary>
    protected Task PlayCast()
        => CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
}
