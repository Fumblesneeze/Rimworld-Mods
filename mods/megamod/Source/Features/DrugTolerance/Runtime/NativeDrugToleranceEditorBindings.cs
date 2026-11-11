using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace Megamod.Features.DrugTolerance.Runtime;

// Concrete signatures survive this provider's module/token callback publication.
public static class NativeDrugToleranceEditorBindings
{
    public static DrugToleranceEditorCallbacks Callbacks(bool header) => new(Method(header ? nameof(Header) : nameof(Row)), Method(nameof(Complete)), Method(nameof(Finish)));
    private static MethodInfo Method(string name) => typeof(NativeDrugToleranceEditorBindings).GetMethod(name)!;
    [HarmonyPriority(int.MinValue)]
    public static void Header(MethodBase __originalMethod, object __instance, ref Rect __0, out object? __state)
    {
        __state = DrugToleranceEditorPatch.Prepare(__originalMethod, __instance, __0, null, -1, out var rectangle);
        __0 = (Rect)rectangle;
    }
    [HarmonyPriority(int.MinValue)]
    public static void Row(MethodBase __originalMethod, object __instance, ref Rect __0, object __1, int __2, out object? __state)
    {
        __state = DrugToleranceEditorPatch.Prepare(__originalMethod, __instance, __0, __1, __2, out var rectangle);
        __0 = (Rect)rectangle;
    }
    [HarmonyPriority(int.MaxValue)]
    public static void Complete(bool __runOriginal, object? __state) => DrugToleranceEditorPatch.CompleteNative(__runOriginal, __state);
    [HarmonyPriority(int.MaxValue)]
    public static Exception? Finish(Exception? __exception, bool __runOriginal, object? __state) => DrugToleranceEditorPatch.Draw(__exception, __runOriginal, __state);
}
