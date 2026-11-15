using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using Megamod.Features.SettingsSearch.Core;
using Megamod.Features.SettingsSearch.Harmony;
using Megamod.Features.SettingsSearch.Runtime;
using RimWorldDevGateway.EndToEndTesting;
using UnityEngine;
using Verse;

namespace Megamod.EndToEndTests.SettingsSearch;

// Supporting diagnostics invoked by the native button workflow, not independent acceptance.
// The caller owns the isolated backend/event and initializes every reader guard first.
internal static class NativeSettingsListingBoundsProbes
{
    private static readonly MethodInfo Getter = typeof(Listing_Standard).GetProperty(nameof(Listing_Standard.BoundingRectCached))!.GetMethod!;
    private static Listing_Standard? callbackListing;
    private static Exception? callbackFailure;
    private static int observations, replacements, throws, suppressions, finalizations;
    private static Type? drawnGenericType;

    internal static void VerifyInsideRead()
    {
        EndToEndAssert.True(SettingsReadScope.Current != null && !SettingsReadScope.Current.Failed);
        using var restoreGui = SettingsGuiStateScope.Capture();
        restoreGui.Isolate();
        GUI.matrix = Matrix4x4.identity; GUI.enabled = false;
        var ordinary = new Listing_Standard();
        var ordinaryBefore = Cache(ordinary);
        EndToEndAssert.True(ReadBounds(ordinary) == null, "Default bounds must remain unbounded during a read.");
        EndToEndAssert.Equal(ordinaryBefore, Cache(ordinary));

        int delegateCalls = 0;
        var cold = new Listing_Standard(new Rect(50000, 50000, 100, 100), () =>
        {
            delegateCalls++;
            throw new ProbeFailure("The cold listing scroll delegate must not run.");
        });
        var coldBefore = Cache(cold);
        EndToEndAssert.True(ReadBounds(cold) == null);
        InvokeEveryNativeRow(cold);
        EndToEndAssert.Equal(0, delegateCalls);
        EndToEndAssert.Equal(coldBefore, Cache(cold), "Cold getter caching must not be populated by hidden reading.");

        var warm = new Listing_Standard(new Rect(1, 2, 3, 4), () =>
        {
            delegateCalls++;
            throw new ProbeFailure("The warm listing delegate must not run.");
        });
        // Disposable precondition only: seed the exact native cache fields, then observe the
        // getter preserving them. This is not presented as a player-created warm-cache action.
        Field("boundingRectCachedForFrame").SetValue(warm, Time.frameCount);
        Field("boundingRectCached").SetValue(warm, (Rect?)new Rect(7, 11, 13, 17));
        var warmBefore = Cache(warm);
        EndToEndAssert.True(ReadBounds(warm) == null);
        EndToEndAssert.Equal(warmBefore, Cache(warm), "Warm cached frame and rectangle must remain identical.");
        EndToEndAssert.Equal(0, delegateCalls);

        var owner = new HarmonyLib.Harmony("tests.megamod.listing-bounds.getter-postfixes");
        observations = replacements = finalizations = 0;
        try
        {
            callbackListing = warm;
            owner.Patch(Getter, postfix: Patch(nameof(ObserveGetter)), finalizer: Patch(nameof(ObserveGetterFinalizer)));
            EndToEndAssert.True(ReadBounds(warm) == null);
            EndToEndAssert.Equal(1, observations, "A compatible getter observer must actually execute.");
            EndToEndAssert.Equal(1, finalizations, "A void exception-observing finalizer remains compatible.");
            owner.Patch(Getter, postfix: Patch(nameof(ReplaceGetter)));
            EndToEndAssert.True(ReadBounds(warm) == null, "The owned finalizer must restore null after a result-replacing postfix.");
            EndToEndAssert.Equal(2, observations);
            EndToEndAssert.Equal(1, replacements);
            EndToEndAssert.Equal(warmBefore, Cache(warm));
            EndToEndAssert.True(!SettingsReadScope.Current!.Failed);
        }
        finally
        {
            callbackListing = null;
            owner.Unpatch(Getter, HarmonyPatchType.All, owner.Id);
        }
        EndToEndAssert.True(ReadBounds(warm) == null && !SettingsReadScope.Current!.Failed);
    }

    internal static void VerifyCallbackFailure(SettingsReadActionGuards actions)
    {
        EndToEndAssert.True(SettingsReadScope.Current == null, "The intentional exception needs its own read scope.");
        var owner = new HarmonyLib.Harmony("tests.megamod.listing-bounds.getter-throw");
        var listing = new Listing_Standard();
        var expected = new ProbeFailure("Owned native getter postfix failure.");
        throws = 0;
        try
        {
            callbackListing = listing; callbackFailure = expected;
            owner.Patch(Getter, postfix: Patch(nameof(ThrowGetter)));
            using var scope = actions.BeginRead();
            Exception? observed = null;
            try { ReadBounds(listing); }
            catch (Exception error) { observed = error; }
            EndToEndAssert.True(ReferenceEquals(expected, observed), "The original callback exception instance must remain observable.");
            EndToEndAssert.Equal(1, throws);
            EndToEndAssert.True(scope.Failed);
            EndToEndAssert.Equal("settings-read-listing-bounds-callback-failed", scope.FailureReason);
            scope.Refuse("later-probe-refusal");
            EndToEndAssert.Equal("settings-read-listing-bounds-callback-failed", scope.FailureReason,
                "The first callback failure must remain latched.");
        }
        finally
        {
            callbackListing = null; callbackFailure = null;
            owner.Unpatch(Getter, HarmonyPatchType.All, owner.Id);
        }
        EndToEndAssert.True(SettingsReadScope.Current == null);
    }

    internal static void VerifySuppressionRefusal(SettingsReadListingBoundsGuard bounds, SettingsReadActionGuards actions)
    {
        EndToEndAssert.True(SettingsReadScope.Current == null, "Run suppression refusal after the successful main capture.");
        var failures = new List<string>();
        foreach (string finalizer in new[] { nameof(SuppressGetterFailure), nameof(SuppressAliasedGetterFailure) })
        {
            var owner = new HarmonyLib.Harmony("tests.megamod.listing-bounds.getter-suppression");
            var listing = new Listing_Standard();
            throws = suppressions = 0;
            try
            {
                callbackListing = listing;
                callbackFailure = new ProbeFailure("Owned native suppressible getter failure.");
                var suppress = Patch(finalizer);
                suppress.priority = int.MaxValue;
                owner.Patch(Getter, postfix: Patch(nameof(ThrowGetter)), finalizer: suppress);
                ReadBounds(listing); // Outside a read: prove actual native suppression on our object.
                EndToEndAssert.Equal(1, throws);
                EndToEndAssert.Equal(1, suppressions);
                callbackListing = null;
                if (bounds.Available) failures.Add(finalizer);
                int delegateCalls = 0;
                var blocked = new Listing_Standard(new Rect(1, 2, 3, 4), () => { delegateCalls++; return Vector2.zero; });
                using (var scope = actions.BeginRead())
                {
                    Exception? observed = null;
                    try { ReadBounds(blocked); }
                    catch (Exception error) { observed = error; }
                    EndToEndAssert.True(observed is InvalidOperationException &&
                        observed.Message == "settings-read-listing-bounds-unavailable",
                        "Installed getter callbacks must enforce the rejected composition during a real read.");
                    EndToEndAssert.True(scope.Failed);
                    EndToEndAssert.Equal("settings-read-listing-bounds-unavailable", scope.FailureReason);
                    EndToEndAssert.Equal(0, delegateCalls, "Rejected reading must not execute the native getter.");
                }
            }
            finally
            {
                callbackListing = null; callbackFailure = null;
                owner.Unpatch(Getter, HarmonyPatchType.All, owner.Id);
            }
        }
        EndToEndAssert.True(failures.Count == 0,
            "Getter exception suppression must revoke read admission: " + string.Join(", ", failures));
    }

    internal static void VerifyGenericFamily()
    {
        EndToEndAssert.True(SettingsReadScope.Current == null, "The owned runtime-family canary runs outside capture.");
        MethodInfo definition = typeof(FamilyListing).GetMethod(nameof(FamilyListing.Row))!;
        MethodInfo integer = definition.MakeGenericMethod(typeof(int)), single = definition.MakeGenericMethod(typeof(float));
        var owner = new HarmonyLib.Harmony("tests.megamod.listing-bounds.native-family");
        var listing = new FamilyListing();
        SettingsListingBoundsAdmission.Certificate? last = null;
        try
        {
            AssertFamily(listing, 7, 9);
            var empty = CaptureFamily(definition);
            owner.Patch(integer, prefix: Patch(nameof(AddTen)));
            AssertFamily(listing, 17, 9);
            EndToEndAssert.True(!empty.IsCurrent(), "Publishing a native closure invalidates the earlier empty family.");
            var one = CaptureFamily(definition);
            owner.Patch(single, prefix: Patch(nameof(AddTwenty)));
            AssertFamily(listing, 17, 29);
            EndToEndAssert.True(!one.IsCurrent(), "A new sibling closure invalidates the earlier family receipt.");
            var both = CaptureFamily(definition);
            owner.Unpatch(single, HarmonyPatchType.All, owner.Id);
            owner.Patch(single, prefix: Patch(nameof(AddThirty)));
            AssertFamily(listing, 17, 39);
            EndToEndAssert.True(!both.IsCurrent(), "Repatching the sibling invalidates the whole earlier family.");
            EndToEndAssert.True(!empty.IsCurrent() && !one.IsCurrent());
            last = CaptureFamily(definition);
        }
        finally
        {
            try { owner.Unpatch(integer, HarmonyPatchType.All, owner.Id); }
            finally { owner.Unpatch(single, HarmonyPatchType.All, owner.Id); }
        }
        AssertFamily(listing, 7, 9);
        EndToEndAssert.True(last != null && !last.IsCurrent(), "Native exact-owner cleanup invalidates the captured publication.");
    }

    private static void InvokeEveryNativeRow(Listing_Standard listing)
    {
        bool check = true, selected = false;
        int integer = -7; float number = 0.3737f;
        string integerBuffer = null!, floatBuffer = null!;
        var range = new IntRange(-3, 130);
        listing.Begin(new Rect(0, 0, 400, 10000));
        try
        {
            listing.Label("Native short label", -1, (TipSignal?)null);
            listing.Label(string.Join(" ", Enumerable.Repeat("Native long label", 24)), 28f, (TipSignal?)null);
            listing.LabelDouble("Native left", "Native right");
            listing.RadioButton("Native radio", false, 0f, 0f, "Native radio help", null, false);
            listing.CheckboxLabeled("Native tabbed checkbox", ref check, 4f);
            listing.CheckboxLabeled("Native helped checkbox", ref check, "Native checkbox help", 24f, 0.75f);
            listing.CheckboxLabeledSelectable("Native selectable checkbox", ref selected, ref check);
            listing.ButtonText("Native button");
            listing.ButtonTextLabeledPct("Native split label", "Native split button", 0.4f);
            listing.ButtonImage(BaseContent.WhiteTex, 24f, 24f);
            listing.TextFieldNumeric(ref integer, ref integerBuffer, 0f, 10f);
            listing.TextFieldNumeric(ref number, ref floatBuffer, 0f, 1f);
            listing.TextFieldNumericLabeled("Native integer", ref integer, ref integerBuffer, 0f, 10f);
            listing.TextFieldNumericLabeled("Native float", ref number, ref floatBuffer, 0f, 1f);
            listing.IntRange(ref range, 0, 100);
            listing.IntEntry(ref integer, ref integerBuffer, 1, 0);
            listing.LabelCheckboxDebug("Native debug checkbox", ref check, false);
            listing.ButtonDebug("Native debug button", false);
            listing.ButtonDebugPinnable("Native pinnable button", false, false);
            listing.CheckboxPinnable("Native pinnable checkbox", ref check, false, false);
        }
        finally { listing.End(); }
        EndToEndAssert.True(check && !selected);
        EndToEndAssert.Equal(-7, integer);
        EndToEndAssert.Equal(BitConverter.SingleToInt32Bits(0.3737f), BitConverter.SingleToInt32Bits(number));
        EndToEndAssert.True(integerBuffer == null && floatBuffer == null);
        EndToEndAssert.Equal(new IntRange(-3, 130), range);
    }

    private static (int Frame, Rect? Cached, Rect? Bounds) Cache(Listing_Standard listing) =>
        ((int)Field("boundingRectCachedForFrame").GetValue(listing)!, (Rect?)Field("boundingRectCached").GetValue(listing),
            (Rect?)Field("boundingRect").GetValue(listing));
    private static FieldInfo Field(string name) => typeof(Listing_Standard).GetField(name, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly)!;
    private static HarmonyMethod Patch(string name) => new(typeof(NativeSettingsListingBoundsProbes).GetMethod(name, BindingFlags.NonPublic | BindingFlags.Static)!);
    [MethodImpl(MethodImplOptions.NoInlining)] private static Rect? ReadBounds(Listing_Standard listing) => listing.BoundingRectCached;
    private static void ObserveGetter(Listing_Standard __instance) { if (ReferenceEquals(__instance, callbackListing)) observations++; }
    private static void ObserveGetterFinalizer(Listing_Standard __instance, Exception? __exception)
    { if (ReferenceEquals(__instance, callbackListing)) finalizations++; }
    private static void ReplaceGetter(Listing_Standard __instance, ref Rect? __result)
    { if (ReferenceEquals(__instance, callbackListing)) { replacements++; __result = new Rect(20, 30, 40, 50); } }
    private static void ThrowGetter(Listing_Standard __instance, ref Rect? __result)
    {
        if (!ReferenceEquals(__instance, callbackListing)) return;
        throws++; __result = new Rect(20, 30, 40, 50);
        throw callbackFailure!;
    }
    private static Exception? SuppressGetterFailure(Listing_Standard __instance, Exception? __exception)
    {
        if (!ReferenceEquals(__instance, callbackListing) || !ReferenceEquals(__exception, callbackFailure)) return __exception;
        suppressions++; return null;
    }
    private static Exception? SuppressAliasedGetterFailure(Listing_Standard __instance,
        [HarmonyArgument("__exception")] Exception? renamed)
    {
        if (!ReferenceEquals(__instance, callbackListing) || !ReferenceEquals(renamed, callbackFailure)) return renamed;
        suppressions++; return null;
    }
    private sealed class ProbeFailure : Exception
    {
        internal ProbeFailure(string message) : base(message) { }
    }

    private static SettingsListingBoundsAdmission.Certificate CaptureFamily(MethodInfo definition)
    {
        var semantics = new SettingsListingBoundsSemantics(new(
            typeof(FamilyListing).GetProperty(nameof(FamilyListing.Bounds))!.GetMethod!,
            typeof(FamilyRect?).GetProperty(nameof(Nullable<FamilyRect>.HasValue))!.GetMethod!,
            typeof(FamilyRect?).GetProperty(nameof(Nullable<FamilyRect>.Value))!.GetMethod!,
            typeof(FamilyRect).GetMethod(nameof(FamilyRect.Overlaps))!,
            new[] { new SettingsListingBoundsSemantics.Draw(typeof(NativeSettingsListingBoundsProbes)
                .GetMethod(nameof(DrawFamily), BindingFlags.NonPublic | BindingFlags.Static)!, ConsumerGenericArguments: new[] { 0 }) }));
        EndToEndAssert.True(SettingsListingBoundsAdmission.TryCapture(new[] { new SettingsListingBoundsAdmission.Consumer(definition, semantics) },
            out var certificate, out var failure), failure ?? "Native generic-family capture failed.");
        EndToEndAssert.True(certificate!.IsCurrent());
        return certificate;
    }
    private static void AssertFamily(FamilyListing listing, int expectedInteger, int expectedSingle)
    {
        drawnGenericType = null;
        EndToEndAssert.Equal(expectedInteger, listing.Row<int>(7));
        EndToEndAssert.Equal(typeof(int), drawnGenericType);
        drawnGenericType = null;
        EndToEndAssert.Equal(expectedSingle, listing.Row<float>(9));
        EndToEndAssert.Equal(typeof(float), drawnGenericType);
    }
    private static void AddTen(ref int value) => value += 10;
    private static void AddTwenty(ref int value) => value += 20;
    private static void AddThirty(ref int value) => value += 30;
    [MethodImpl(MethodImplOptions.NoInlining)] private static void DrawFamily<T>(FamilyRect row) where T : struct => drawnGenericType = typeof(T);
    private readonly struct FamilyRect
    {
        private readonly int coordinate;
        internal FamilyRect(int coordinate) => this.coordinate = coordinate;
        [MethodImpl(MethodImplOptions.NoInlining)] public bool Overlaps(FamilyRect other) => coordinate == other.coordinate;
    }
    private sealed class FamilyListing
    {
        public FamilyRect? Bounds { [MethodImpl(MethodImplOptions.NoInlining)] get; set; }
        [MethodImpl(MethodImplOptions.NoInlining)] private FamilyRect GetRow() => new(1);
        [MethodImpl(MethodImplOptions.NoInlining)] public int Row<T>(int value) where T : struct
        {
            FamilyRect row = GetRow();
            if (Bounds.HasValue && !row.Overlaps(Bounds.Value)) return -1;
            DrawFamily<T>(row);
            return value;
        }
    }
}
