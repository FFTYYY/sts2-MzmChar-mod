using System.Collections.Generic;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Relics;

namespace MzmChar.Game;

/// <summary>
/// 给 vanilla 三件「先古」relic（TouchOfOrobas / ArchaicTooth / DustyTome）注入若叶睦的映射。
///
/// 这三件 relic 都靠**静态字典**查表把"起始遗物 / 起始卡"换成"先古版本"。Mu 的卡 / 遗物
/// 不在 vanilla 字典里 → null lookup → NullReferenceException。所以必须 postfix patch
/// 它们的字典 getter 把 Mu 的 entry 加进去。
///
/// **为什么 patch getter 而非用 Prepare/postfix on AfterObtained**：字典是 static getter
/// （每次调用都 new Dictionary{...}），patch getter 后 result 即可写入；不动 AfterObtained
/// 的复杂业务逻辑，最低侵入。
///
/// 详见 reports/report_5.md「Part B」。
/// </summary>
[HarmonyPatch(typeof(TouchOfOrobas), "RefinementUpgrades", MethodType.Getter)]
public static class TouchOfOrobasMzmCharPatch
{
    [HarmonyPostfix]
    static void Postfix(ref Dictionary<ModelId, RelicModel> __result)
    {
        // SecondPersonaRelic → AncientSecondPersonaRelic
        __result[ModelDb.Relic<SecondPersonaRelic>().Id] =
            ModelDb.Relic<AncientSecondPersonaRelic>();
    }
}

/// <summary>
/// ArchaicTooth.TranscendenceUpgrades 的 patch — 只用于 ArchaicTooth 的 starter-card 转化。
/// （DustyTome 路径不走这里 —— BaseLib 自己 prefix-patch 了 DustyTome.SetupForPlayer，
/// 用 ITomeCard 接口建 character→tome card 映射表。所以 DustyAncientCard 实现 ITomeCard
/// 即可，不需要在这里加 entry。）
/// </summary>
[HarmonyPatch(typeof(ArchaicTooth), "TranscendenceUpgrades", MethodType.Getter)]
public static class ArchaicToothMzmCharPatch
{
    [HarmonyPostfix]
    static void Postfix(ref Dictionary<ModelId, CardModel> __result)
    {
        // MutsumiCharge → Disharmony（古老牙齿把初始牌转化为先古版）
        __result[ModelDb.Card<MutsumiCharge>().Id] = ModelDb.Card<Disharmony>();
    }
}
