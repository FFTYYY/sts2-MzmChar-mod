using BaseLib.Patches.Compatibility;   // CustomEnumAttribute
using BaseLib.Patches.Content;          // KeywordPropertiesAttribute, AutoKeywordPosition
using MegaCrit.Sts2.Core.Entities.Cards;

namespace MzmChar.Game;

/// <summary>
/// 本 mod 的自定义关键字。原理：BaseLib `GenEnumValues.FindAndGenerate` 在启动时扫描
/// 标了 [CustomEnum] 的 static CardKeyword 字段，给每个分配一个超出 vanilla 范围的新 int 值，
/// 改写字段。运行时这些字段就是合法的 CardKeyword 值，可以放进 CanonicalKeywords。
///
/// loc 走 `card_keywords.<UPPER_SNAKE_CASE_NAME>.title` / `.description`
/// （`StringHelper.Slugify("Perform")` = `"PERFORM"`，已 probe 确认）
/// </summary>
public static class MzmCharKeywords
{
    /// <summary>
    /// 「演奏」：只能在演奏会（ConcertPower）状态下打出真正的效果。其他时候打出 → 获得 1 点演艺热情。
    /// 行为由每张 Perform 卡的 OnPlay 自行实现（BaseLib 关键字系统只管显示 + tooltip）。
    /// 显示规则由 MzmCharBaseCard 注入的 HasConcert 描述变量驱动。
    /// </summary>
    [CustomEnum]
    [KeywordProperties(AutoKeywordPosition.Before, true)]   // 自动插入卡描述**开头**（用户希望演奏 tag 显眼）
    public static CardKeyword Perform = CardKeyword.None;
}
