using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Ancients;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models.Events;

namespace MzmChar.Game;

/// <summary>
/// 修 vanilla `AncientDialogueSet.PopulateLocKeys` 给每条 line 强制 set `NextButtonText` 到
/// `{key}.next` 而**不检查 key 是否存在**的 bug。我们 mod 没写 `.next` keys，对话按钮显示
/// 成原 key 字符串。Postfix 扫所有 line，`.next` key 不存在就把 NextButtonText 设回 null，
/// 让 vanilla `CreateOptionForCurrentLine` fallback 到默认的 `_continueLocString` /
/// `_respondLocString`（"继续" / "回答"）。
/// </summary>
[HarmonyPatch(typeof(AncientDialogueSet), nameof(AncientDialogueSet.PopulateLocKeys))]
internal static class AncientDialogueNextButtonGuardPatch
{
    [HarmonyPostfix]
    private static void Postfix(AncientDialogueSet __instance)
    {
        foreach (var dlg in __instance.GetAllDialogues())
        {
            if (dlg?.Lines == null) continue;
            foreach (var line in dlg.Lines)
            {
                var nbt = line?.NextButtonText;
                if (nbt == null) continue;
                if (!LocString.Exists(nbt.LocTable, nbt.LocEntryKey))
                    line!.NextButtonText = null;
            }
        }
    }
}

/// <summary>
/// 修 vanilla TheArchitect.WinRun() 在没有有效对话时抛 NRE 的 bug。
///
/// 触发场景（IL 实证）：
///   - LoadDialogue → DialogueSet.GetValidDialogues(charId, charVisits, allWins, false)
///   - filter: `dialogue.VisitIndex.HasValue &amp;&amp; VisitIndex == charVisits`
///   - 玩家已经赢过 N 次 → charVisits = N。如果该角色对话只覆盖 0~M (M&lt;N) → filter 出来空 →
///     Rng.NextItem(empty) → null → _dialogue = null
///   - GenerateInitialOptions 看 _dialogue 为 null → 返回 CreateProceedOption（直接调 WinRun）
///   - WinRun 第一行 `_dialogue.EndAttackers` → NRE
///
/// 修法（抄 YuWan 的 ArchitectLoadDialogueNullGuard pattern）：
///   postfix LoadDialogue，若 _dialogue 还是 null，注入一个 stub AncientDialogue
///   (Lines=[]、EndAttackers=None)。WinRun 跑下去 EndAttackers=None → AnimX 早退
///   → 不 NRE，且仍然完成多人 sync 等收尾。
///
/// 用 RuntimeHelpers.GetUninitializedObject 绕过 ctor（AncientDialogue ctor 要 String[] sfxPaths）。
/// </summary>
[HarmonyPatch(typeof(TheArchitect), "LoadDialogue")]
internal static class ArchitectLoadDialogueNullGuardPatch
{
    private static readonly FieldInfo? DialogueField =
        typeof(TheArchitect).GetField("_dialogue", BindingFlags.Instance | BindingFlags.NonPublic);

    [HarmonyPostfix]
    private static void Postfix(TheArchitect __instance)
    {
        if (DialogueField == null) return;
        if (DialogueField.GetValue(__instance) != null) return;

        var stub = CreateSafeStub();
        if (stub != null)
        {
            DialogueField.SetValue(__instance, stub);
            ModEntry.Log("[Architect] LoadDialogue: no valid dialogue → injected empty stub to prevent WinRun NRE");
        }
    }

    private static AncientDialogue? CreateSafeStub()
    {
        try
        {
            // 绕过 AncientDialogue(String[] sfxPaths) ctor
            var stub = (AncientDialogue)RuntimeHelpers.GetUninitializedObject(typeof(AncientDialogue));

            // Lines 设空 array（直接命中 vanilla 的 brfalse Lines.Count == 0 早退）
            var linesField = typeof(AncientDialogue)
                .GetField("<Lines>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)
                ?? FindFieldOfType(typeof(AncientDialogue), typeof(IReadOnlyList<AncientDialogueLine>));
            if (linesField == null) return null;
            linesField.SetValue(stub, Array.Empty<AncientDialogueLine>());

            // 所有 ArchitectAttackers 字段（StartAttackers / EndAttackers backing）置 None
            foreach (var fi in typeof(AncientDialogue).GetFields(BindingFlags.Instance | BindingFlags.NonPublic))
            {
                if (fi.FieldType == typeof(ArchitectAttackers))
                    fi.SetValue(stub, ArchitectAttackers.None);
            }

            return stub;
        }
        catch
        {
            return null;
        }
    }

    private static FieldInfo? FindFieldOfType(Type type, Type fieldType)
    {
        foreach (var fi in type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic))
            if (fi.FieldType == fieldType)
                return fi;
        return null;
    }
}
