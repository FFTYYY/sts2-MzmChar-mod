using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Ancients;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models.Events;

namespace MzmChar.Game;

// 移除了旧 `AncientDialogueNextButtonGuardPatch`（全局 postfix 把不存在的 NextButtonText 设 null）：
// 那是治标不治本——按钮显示 raw key 的根本原因是我们没按 vanilla 设计在 ancients.json 里
// 给 ARCHITECT 对话每行配套写 `.next` loc key（vanilla `PopulateLocKeys` 会自动构造
// `<line_key>.next` 这个 LocString 作为按钮文字 LocString）。
// 正确修法：直接在 pack/MzmChar/localization/{zhs,eng}/ancients.json 给 THE_ARCHITECT 所有
// 对话行配套写 `.next` keys（ancient → "继续/Continue"，char → "回答/Respond"）。
// 其他先古（rest site）不走 NAncientEventLayout / TheArchitect.CreateOptionForCurrentLine 渲染路径，
// 所以不读 NextButtonText，不写 `.next` 也不出 raw key。参考 YuWan ancients.json。

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
