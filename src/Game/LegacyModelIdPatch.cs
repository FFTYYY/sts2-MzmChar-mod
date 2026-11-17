using System;
using System.Collections.Generic;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;

namespace MzmChar.Game;

/// <summary>
/// 改过类名的卡 / 能力：旧存档里的 ID 映射到新 ID。
///
/// ModelId 的 Entry 就是类名（BaseLib 再加 <c>MZMCHAR-</c> 前缀），改类名 = 改 ID。
/// 旧存档（进行中的局、历史记录、进度统计）里还是旧 ID，vanilla <c>ModelDb.GetById</c> 找不到会抛
/// <c>ModelNotFoundException</c>。存档里的 ID 字符串全都过 <c>ModelId.Deserialize</c>
/// （<c>ModelIdRunSaveConverter.Read</c> 和存档迁移 <c>MigratingData</c> 两处，v0.113 全量反编译核过），
/// 在这里把旧 Entry 换成新 Entry。只认带 <c>MZMCHAR-</c> 前缀的，原版同名卡（<c>CARD.DISINTEGRATION</c> 等）不动。
///
/// 0.2.10 改名的 6 个（原版有同名类，见各类的注释）：Encore / Tune / Ensemble / Disintegration 四张卡、
/// EncorePower / DisintegrationPower 两个能力，都加了 Mu 前缀。
/// </summary>
[HarmonyPatch(typeof(ModelId), nameof(ModelId.Deserialize))]
internal static class LegacyModelIdPatch
{
    private static readonly Dictionary<string, string> RenamedEntries = new(StringComparer.Ordinal)
    {
        ["MZMCHAR-ENCORE"] = "MZMCHAR-MU_ENCORE",
        ["MZMCHAR-TUNE"] = "MZMCHAR-MU_TUNE",
        ["MZMCHAR-ENSEMBLE"] = "MZMCHAR-MU_ENSEMBLE",
        ["MZMCHAR-DISINTEGRATION"] = "MZMCHAR-MU_DISINTEGRATION",
        ["MZMCHAR-ENCORE_POWER"] = "MZMCHAR-MU_ENCORE_POWER",
        ["MZMCHAR-DISINTEGRATION_POWER"] = "MZMCHAR-MU_DISINTEGRATION_POWER",
    };

    [HarmonyPrefix]
    private static void Prefix(ref string json)
    {
        var dot = json.IndexOf('.');
        if (dot < 0 || string.CompareOrdinal(json, dot + 1, "MZMCHAR-", 0, 8) != 0) return;
        if (RenamedEntries.TryGetValue(json.Substring(dot + 1), out var entry))
            json = json.Substring(0, dot + 1) + entry;
    }

    /// <summary>启动自检：补丁真的挂上了（ModEntry 调，结果写日志）。</summary>
    internal static string SelfCheck() =>
        $"{ModelId.Deserialize("CARD.MZMCHAR-ENCORE")} / {ModelId.Deserialize("POWER.MZMCHAR-DISINTEGRATION_POWER")}";
}
