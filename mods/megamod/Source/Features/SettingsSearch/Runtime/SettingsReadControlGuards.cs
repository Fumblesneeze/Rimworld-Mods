using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using HarmonyLib;
using Megamod.Features.SettingsSearch.Harmony;
using Megamod.Patching;
using UnityEngine;
using Verse;

namespace Megamod.Features.SettingsSearch.Runtime;

// Initialization is owned by the complete neutral reader. This component registers no callbacks itself.
public sealed class SettingsReadControlGuards : IDisposable
{
    private const int TargetCount = 27;
    private readonly Thread owner = Thread.CurrentThread;
    private readonly AdmissionCoordinator admission;
    private readonly List<SettingsReadControlGuard> guards = new();
    private readonly List<PatchCandidate> candidates = new();
    private readonly PatchGroup group;
    private bool initialized, disposed;

    public SettingsReadControlGuards(Action<string> warning)
    {
        admission = new AdmissionCoordinator(warning);
        Bind("SS-READ-GUI-TEXT", typeof(GUI), "TextField", true, typeof(string), typeof(StringReturn), true,
            typeof(Rect), typeof(string), typeof(int), typeof(GUIStyle));
        Bind("SS-READ-GUI-AREA", typeof(GUI), "TextArea", true, typeof(string), typeof(StringReturn), true,
            typeof(Rect), typeof(string), typeof(int), typeof(GUIStyle));
        Bind("SS-READ-GUI-PASSWORD", typeof(GUI), "PasswordField", true, typeof(string), typeof(StringReturn), true,
            typeof(Rect), typeof(string), typeof(char), typeof(GUIStyle));
        Bind("SS-READ-WIDGET-TEXT", typeof(Widgets), "TextField", true, typeof(string), typeof(StringReturn), true,
            typeof(Rect), typeof(string));
        Bind("SS-READ-WIDGET-TEXT-LIMIT", typeof(Widgets), "TextField", true, typeof(string), typeof(StringReturn), true,
            typeof(Rect), typeof(string), typeof(int), typeof(Regex));
        Bind("SS-READ-WIDGET-AREA", typeof(Widgets), "TextArea", true, typeof(string), typeof(StringReturn), true,
            typeof(Rect), typeof(string), typeof(bool));
        Bind("SS-READ-DELAYED-TEXT", typeof(Widgets), "DelayedTextField", true, typeof(string), typeof(Delayed), true,
            typeof(Rect), typeof(string), typeof(string).MakeByRefType(), typeof(string), typeof(string));
        BindNumeric("SS-READ-NUMERIC-INT", typeof(int), typeof(IntBuffer));
        BindNumeric("SS-READ-NUMERIC-FLOAT", typeof(float), typeof(FloatBuffer));
        Bind("SS-READ-PERCENT", typeof(Widgets), "TextFieldPercent", true, typeof(void), typeof(FloatBuffer), true,
            typeof(Rect), typeof(float).MakeByRefType(), typeof(string).MakeByRefType(), typeof(float), typeof(float));
        Bind("SS-READ-VECTOR", typeof(Widgets), "TextFieldVector", true, typeof(void), typeof(VectorBuffer), true,
            typeof(Rect), typeof(Vector3).MakeByRefType(), typeof(string[]).MakeByRefType(), typeof(float), typeof(float));
        Bind("SS-READ-SLIDER", typeof(Widgets), "HorizontalSlider", true, typeof(float), typeof(FloatReturn), true,
            typeof(Rect), typeof(float), typeof(float), typeof(float), typeof(bool), typeof(string), typeof(string), typeof(string), typeof(float));
        Bind("SS-READ-SLIDER-REF", typeof(Widgets), "HorizontalSlider", true, typeof(void), typeof(FloatReference), true,
            typeof(Rect), typeof(float).MakeByRefType(), typeof(FloatRange), typeof(string), typeof(float));
        Bind("SS-READ-FREQUENCY", typeof(Widgets), "FrequencyHorizontalSlider", true, typeof(float), typeof(FloatReturn), true,
            typeof(Rect), typeof(float), typeof(float), typeof(float), typeof(bool));
        Bind("SS-READ-GUI-SCROLL", typeof(GUI), "BeginScrollViewImpl", false, typeof(Vector2), typeof(ScrollReturn), true,
            typeof(Rect), typeof(Vector2), typeof(Rect), typeof(GUIStyle), typeof(GUIStyle));
        Bind("SS-READ-GUI-SCROLL-DEFAULT", typeof(GUI), "BeginScrollView", true, typeof(Vector2), typeof(ScrollReturn), true,
            typeof(Rect), typeof(Vector2), typeof(Rect));
        Bind("SS-READ-GUI-SCROLL-STYLE", typeof(GUI), "BeginScrollView", true, typeof(Vector2), typeof(ScrollReturn), true,
            typeof(Rect), typeof(Vector2), typeof(Rect), typeof(GUIStyle), typeof(GUIStyle));
        Bind("SS-READ-GUI-SCROLL-BARS", typeof(GUI), "BeginScrollView", true, typeof(Vector2), typeof(ScrollReturn), true,
            typeof(Rect), typeof(Vector2), typeof(Rect), typeof(bool), typeof(bool));
        Bind("SS-READ-GUI-SCROLL-BARS-STYLE", typeof(GUI), "BeginScrollView", true, typeof(Vector2), typeof(ScrollReturn), true,
            typeof(Rect), typeof(Vector2), typeof(Rect), typeof(bool), typeof(bool), typeof(GUIStyle), typeof(GUIStyle));
        Bind("SS-READ-WIDGET-SCROLL", typeof(Widgets), "BeginScrollView", true, typeof(void), typeof(ScrollReference), true,
            typeof(Rect), typeof(Vector2).MakeByRefType(), typeof(Rect), typeof(bool));
        Bind("SS-READ-GUI-SLIDER", typeof(GUI), "Slider", true, typeof(float), typeof(FloatReturn), true,
            typeof(Rect), typeof(float), typeof(float), typeof(float), typeof(float), typeof(GUIStyle), typeof(GUIStyle),
            typeof(bool), typeof(int), typeof(GUIStyle));
        Bind("SS-READ-FLOAT-RANGE", typeof(Widgets), "FloatRange", true, typeof(void), typeof(FloatRangeReference), true,
            typeof(Rect), typeof(int), typeof(FloatRange).MakeByRefType(), typeof(float), typeof(float), typeof(string),
            typeof(ToStringStyle), typeof(float), typeof(GameFont), typeof(Color?), typeof(float));
        Bind("SS-READ-INT-RANGE", typeof(Widgets), "IntRange", true, typeof(void), typeof(IntRangeReference), true,
            typeof(Rect), typeof(int), typeof(IntRange).MakeByRefType(), typeof(int), typeof(int), typeof(string), typeof(int));
        Bind("SS-READ-RANGE-TEXT", typeof(Widgets), "FloatRangeWithTypeIn", true, typeof(void), typeof(FloatRangeReference), true,
            typeof(Rect), typeof(int), typeof(FloatRange).MakeByRefType(), typeof(float), typeof(float), typeof(ToStringStyle), typeof(string));
        Bind("SS-READ-SLIDER-SOUND", typeof(Widgets), "CheckPlayDragSliderSound", false, typeof(void), typeof(Sound), false);
        Bind("SS-READ-TOOLTIP", typeof(TooltipHandler), "TipRegion", true, typeof(void), typeof(Tooltip), false,
            typeof(Rect), typeof(TipSignal));
        BindListingInt();
        group = new PatchGroup("settings-search.read-controls", AdmissionPhase.GameReady, candidates.ToArray());
    }

    public bool Initialize()
    {
        RequireOwner();
        if (disposed) return false;
        initialized = admission.Apply(group, AdmissionPhase.GameReady);
        return Available;
    }

    public bool Available
    {
        get
        {
            RequireOwner();
            if (!initialized || disposed || guards.Count != TargetCount) return false;
            try { return guards.All(guard => guard.Available); }
            catch (Exception) { return false; }
        }
    }

    public void Dispose()
    {
        RequireOwner();
        if (disposed) return;
        disposed = true;
        initialized = false;
        List<Exception>? failures = null;
        for (int index = group.Patches.Count - 1; index >= 0; index--)
            try { group.Patches[index].Rollback(); }
            catch (Exception failure) { (failures ??= new()).Add(failure); }
        if (failures != null) throw new AggregateException("Settings-read control guard cleanup failed.", failures);
    }

    private void Bind(string id, Type declaringType, string name, bool isPublic, Type result,
        Type callbacks, bool paired, params Type[] arguments)
    {
        MethodInfo? target = null;
        try
        {
            target = declaringType.GetMethod(name, BindingFlags.Static | BindingFlags.DeclaredOnly |
                (isPublic ? BindingFlags.Public : BindingFlags.NonPublic), null, arguments, null);
            if (target == null || target.DeclaringType != declaringType || !target.IsStatic ||
                target.IsPublic != isPublic || (!isPublic && !target.IsPrivate) || target.IsGenericMethod ||
                target.ContainsGenericParameters || target.ReturnType != result ||
                !target.GetParameters().Select(parameter => parameter.ParameterType).SequenceEqual(arguments)) target = null;
            if (callbacks == typeof(Tooltip))
            {
                var text = typeof(TipSignal).GetField(nameof(TipSignal.text), BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                if (text == null || text.FieldType != typeof(string) || text.IsStatic) target = null;
            }
        }
        catch (Exception) { target = null; }
        Add(id, declaringType.FullName + "." + name, target, callbacks, paired);
    }

    private void BindListingInt()
    {
        var arguments = new[] { typeof(int).MakeByRefType(), typeof(string).MakeByRefType(), typeof(int), typeof(int) };
        MethodInfo? target = null;
        try
        {
            target = typeof(Listing_Standard).GetMethod("IntEntry", BindingFlags.Public | BindingFlags.Instance |
                BindingFlags.DeclaredOnly, null, arguments, null);
            if (target == null || target.DeclaringType != typeof(Listing_Standard) || target.IsStatic ||
                !target.IsPublic || target.IsVirtual || target.IsGenericMethod || target.ContainsGenericParameters ||
                target.ReturnType != typeof(void) ||
                !target.GetParameters().Select(parameter => parameter.ParameterType).SequenceEqual(arguments)) target = null;
        }
        catch (Exception) { target = null; }
        Add("SS-READ-LISTING-INT", typeof(Listing_Standard).FullName + ".IntEntry", target, typeof(ListingIntBuffer), true);
    }

    private void BindNumeric(string id, Type number, Type callbacks)
    {
        MethodInfo? target = null;
        try
        {
            var definitions = typeof(Widgets).GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
                .Where(method => method.Name == "TextFieldNumeric" && method.IsGenericMethodDefinition &&
                    method.ReturnType == typeof(void) && method.GetGenericArguments().Length == 1)
                .Where(method =>
                {
                    var parameter = method.GetGenericArguments()[0];
                    return (parameter.GenericParameterAttributes & GenericParameterAttributes.NotNullableValueTypeConstraint) != 0 &&
                        method.GetParameters().Select(value => value.ParameterType).SequenceEqual(new[]
                        { typeof(Rect), parameter.MakeByRefType(), typeof(string).MakeByRefType(), typeof(float), typeof(float) });
                }).ToArray();
            if (definitions.Length == 1)
            {
                var closed = definitions[0].MakeGenericMethod(number);
                if (closed.DeclaringType == typeof(Widgets) && closed.IsPublic && closed.IsStatic &&
                    closed.IsGenericMethod && !closed.ContainsGenericParameters && closed.ReturnType == typeof(void) &&
                    closed.GetGenericArguments().SequenceEqual(new[] { number }) &&
                    closed.GetParameters().Select(parameter => parameter.ParameterType).SequenceEqual(new[]
                    { typeof(Rect), number.MakeByRefType(), typeof(string).MakeByRefType(), typeof(float), typeof(float) })) target = closed;
            }
        }
        catch (Exception) { target = null; }
        Add(id, typeof(Widgets).FullName + ".TextFieldNumeric<" + number.Name + ">", target, callbacks, true);
    }

    private void Add(string id, string targetName, MethodInfo? target, Type callbacks, bool paired)
    {
        SettingsReadControlGuard? guard = null;
        try
        {
            var prefix = callbacks.GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly);
            var finalizer = paired ? callbacks.GetMethod("Finalizer", BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly) : null;
            if (target != null && prefix != null && (!paired || finalizer != null))
                guard = new SettingsReadControlGuard(target, id, prefix, finalizer);
        }
        catch (Exception) { /* Fail closed through the ordinary C01 rejected candidates below. */ }
        if (guard == null)
        {
            candidates.Add(Rejected(id + ".prefix", paired ? "SS-READ-CONTROL-ENTRY" : id, targetName));
            if (paired) candidates.Add(Rejected(id + ".finalizer", "SS-READ-CONTROL-EXIT", targetName));
            return;
        }
        guards.Add(guard);
        for (int index = 0; index < guard.Candidates.Count; index++)
        {
            var candidate = guard.Candidates[index];
            if (!paired) candidate = candidate with { TargetSpecId = id };
            candidates.Add(index == guard.Candidates.Count - 1
                ? candidate with { Install = () => { candidate.Install(); guard.Enable(); } }
                : candidate);
        }
    }

    private static PatchCandidate Rejected(string id, string spec, string target) => new(id, spec, target,
        "automatic settings discovery", "ordinary settings pages",
        () => "The exact native settings-control callable or callback shape is unavailable.", () => { }, () => { });

    private void RequireOwner()
    {
        if (!ReferenceEquals(Thread.CurrentThread, owner))
            throw new InvalidOperationException("Settings-read control guards require their initializing thread.");
    }

    private static class StringReturn
    {
        [ThreadStatic] private static int depth;

        [HarmonyPriority(int.MaxValue)]
        private static void Prefix(string? __1, MethodBase __originalMethod, out (bool Entered, int ParentDepth, string? Value) __state)
        {
            __state = default;
            if (!SettingsReadControlGuard.ShouldCapture(__originalMethod)) return;
            // The limited Widgets.TextField consumes the simpler field's normalized intermediate.
            // Retain the caller's value at the outer boundary without breaking that native chain.
            __state = (true, depth, __1);
            depth++;
        }
        [HarmonyPriority(int.MinValue)]
        private static void Finalizer(ref string? __result, ref (bool Entered, int ParentDepth, string? Value) __state)
        {
            if (!__state.Entered) return;
            __state.Entered = false;
            depth = __state.ParentDepth;
            if (__state.ParentDepth == 0) __result = __state.Value;
        }
    }

    private static class Delayed
    {
        [HarmonyPriority(int.MaxValue)]
        private static void Prefix(string? __1, ref string? __2, MethodBase __originalMethod,
            out (bool Active, string? Value, string? Buffer) __state)
        {
            __state = default;
            if (SettingsReadControlGuard.ShouldCapture(__originalMethod)) __state = (true, __1, __2);
        }
        [HarmonyPriority(int.MinValue)]
        private static void Finalizer(ref string? __result, ref string? __2, (bool Active, string? Value, string? Buffer) __state)
        { if (__state.Active) { __result = __state.Value; __2 = __state.Buffer; } }
    }

    private static class IntBuffer
    {
        [HarmonyPriority(int.MaxValue)]
        private static void Prefix(ref int __1, ref string? __2, MethodBase __originalMethod,
            out (bool Active, int Value, string? Buffer) __state)
        {
            __state = default;
            if (SettingsReadControlGuard.ShouldCapture(__originalMethod)) __state = (true, __1, __2);
        }
        [HarmonyPriority(int.MinValue)]
        private static void Finalizer(ref int __1, ref string? __2, (bool Active, int Value, string? Buffer) __state)
        { if (__state.Active) { __1 = __state.Value; __2 = __state.Buffer; } }
    }

    private static class ListingIntBuffer
    {
        [HarmonyPriority(int.MaxValue)]
        private static void Prefix(ref int __0, ref string? __1, MethodBase __originalMethod,
            out (bool Active, int Value, string? Buffer) __state)
        {
            __state = default;
            if (SettingsReadControlGuard.ShouldCapture(__originalMethod)) __state = (true, __0, __1);
        }
        [HarmonyPriority(int.MinValue)]
        private static void Finalizer(ref int __0, ref string? __1, (bool Active, int Value, string? Buffer) __state)
        { if (__state.Active) { __0 = __state.Value; __1 = __state.Buffer; } }
    }

    private static class FloatBuffer
    {
        [HarmonyPriority(int.MaxValue)]
        private static void Prefix(ref float __1, ref string? __2, MethodBase __originalMethod,
            out (bool Active, float Value, string? Buffer) __state)
        {
            __state = default;
            if (SettingsReadControlGuard.ShouldCapture(__originalMethod)) __state = (true, __1, __2);
        }
        [HarmonyPriority(int.MinValue)]
        private static void Finalizer(ref float __1, ref string? __2, (bool Active, float Value, string? Buffer) __state)
        { if (__state.Active) { __1 = __state.Value; __2 = __state.Buffer; } }
    }

    private static class VectorBuffer
    {
        private struct State
        {
            internal bool Active;
            internal Vector3 Value;
            internal string[]? Buffer;
            internal string? First, Second, Third;
        }
        [HarmonyPriority(int.MaxValue)]
        private static void Prefix(ref Vector3 __1, ref string[]? __2, MethodBase __originalMethod, out State __state)
        {
            __state = default;
            if (!SettingsReadControlGuard.ShouldCapture(__originalMethod)) return;
            __state.Active = true;
            __state.Value = __1;
            __state.Buffer = __2;
            if (__2 != null)
            {
                if (__2.Length > 0) __state.First = __2[0];
                if (__2.Length > 1) __state.Second = __2[1];
                if (__2.Length > 2) __state.Third = __2[2];
            }
        }
        [HarmonyPriority(int.MinValue)]
        private static void Finalizer(ref Vector3 __1, ref string[]? __2, State __state)
        {
            if (!__state.Active) return;
            __1 = __state.Value;
            __2 = __state.Buffer;
            if (__2 == null) return;
            if (__2.Length > 0) __2[0] = __state.First!;
            if (__2.Length > 1) __2[1] = __state.Second!;
            if (__2.Length > 2) __2[2] = __state.Third!;
        }
    }

    private static class FloatReturn
    {
        [HarmonyPriority(int.MaxValue)]
        private static void Prefix(float __1, MethodBase __originalMethod, out (bool Active, float Value) __state)
        {
            __state = default;
            if (SettingsReadControlGuard.ShouldCapture(__originalMethod)) __state = (true, __1);
        }
        [HarmonyPriority(int.MinValue)]
        private static void Finalizer(ref float __result, (bool Active, float Value) __state)
        { if (__state.Active) __result = __state.Value; }
    }

    private static class FloatReference
    {
        [HarmonyPriority(int.MaxValue)]
        private static void Prefix(ref float __1, MethodBase __originalMethod, out (bool Active, float Value) __state)
        {
            __state = default;
            if (SettingsReadControlGuard.ShouldCapture(__originalMethod)) __state = (true, __1);
        }
        [HarmonyPriority(int.MinValue)]
        private static void Finalizer(ref float __1, (bool Active, float Value) __state)
        { if (__state.Active) __1 = __state.Value; }
    }

    private static class ScrollReturn
    {
        [HarmonyPriority(int.MaxValue)]
        private static void Prefix(Vector2 __1, MethodBase __originalMethod, out (bool Active, Vector2 Value) __state)
        {
            __state = default;
            if (SettingsReadControlGuard.ShouldCapture(__originalMethod)) __state = (true, __1);
        }
        [HarmonyPriority(int.MinValue)]
        private static void Finalizer(ref Vector2 __result, (bool Active, Vector2 Value) __state)
        { if (__state.Active) __result = __state.Value; }
    }

    private static class ScrollReference
    {
        [HarmonyPriority(int.MaxValue)]
        private static void Prefix(ref Vector2 __1, MethodBase __originalMethod, out (bool Active, Vector2 Value) __state)
        {
            __state = default;
            if (SettingsReadControlGuard.ShouldCapture(__originalMethod)) __state = (true, __1);
        }
        [HarmonyPriority(int.MinValue)]
        private static void Finalizer(ref Vector2 __1, (bool Active, Vector2 Value) __state)
        { if (__state.Active) __1 = __state.Value; }
    }

    private static class FloatRangeReference
    {
        [HarmonyPriority(int.MaxValue)]
        private static void Prefix(ref FloatRange __2, MethodBase __originalMethod, out (bool Active, FloatRange Value) __state)
        {
            __state = default;
            if (SettingsReadControlGuard.ShouldCapture(__originalMethod)) __state = (true, __2);
        }
        [HarmonyPriority(int.MinValue)]
        private static void Finalizer(ref FloatRange __2, (bool Active, FloatRange Value) __state)
        { if (__state.Active) __2 = __state.Value; }
    }

    private static class IntRangeReference
    {
        [HarmonyPriority(int.MaxValue)]
        private static void Prefix(ref IntRange __2, MethodBase __originalMethod, out (bool Active, IntRange Value) __state)
        {
            __state = default;
            if (SettingsReadControlGuard.ShouldCapture(__originalMethod)) __state = (true, __2);
        }
        [HarmonyPriority(int.MinValue)]
        private static void Finalizer(ref IntRange __2, (bool Active, IntRange Value) __state)
        { if (__state.Active) __2 = __state.Value; }
    }

    private static class Sound
    {
        [HarmonyPriority(int.MaxValue)]
        private static bool Prefix(MethodBase __originalMethod) => !SettingsReadControlGuard.ShouldCapture(__originalMethod);
    }

    private static class Tooltip
    {
        [HarmonyPriority(int.MaxValue)]
        private static bool Prefix(Rect __0, TipSignal __1, MethodBase __originalMethod)
        {
            if (!SettingsReadControlGuard.ShouldCapture(__originalMethod)) return true;
            SettingsPageRecorder.ObserveTooltip(__0, __1.text);
            return false;
        }
    }
}
