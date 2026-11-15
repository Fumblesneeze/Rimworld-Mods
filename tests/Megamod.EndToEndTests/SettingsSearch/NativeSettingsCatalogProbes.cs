using System.Collections;
using System.Reflection;
using HarmonyLib;
using Megamod.Features.ModSettingsNavigation;
using Megamod.Features.SettingsSearch.Core;
using Megamod.Features.SettingsSearch.Runtime;
using RimWorld;
using RimWorldDevGateway.EndToEndTesting;
using UnityEngine;
using Verse;

namespace Megamod.EndToEndTests.SettingsSearch;

// Supporting diagnostics in the real game/GUI tier. No fixture Mod enters LoadedModManager,
// no visible collection is replaced, and no native/provider settings renderer is invoked.
internal sealed class NativeSettingsCatalogProbes
{
    private enum Phase
    {
        Startup, DuplicateObjects, EnumerationFailure, DisposalFailure, ClearDuringRead,
        RefreshDuringRead, ReplacementRequest, SourceReplacement, PageBoundary, PageOverflow,
        TextBoundary, TextOverflow, Complete
    }
    private readonly Mod[] expectedStartup;
    private readonly Mod first, second;
    private readonly SettingsNativeCatalog catalog = new();
    private readonly FieldInfo source = AccessTools.Field(typeof(Dialog_Options), "cachedModsWithSettings")
        ?? throw new MissingFieldException(typeof(Dialog_Options).FullName, "cachedModsWithSettings");
    private readonly Dictionary<string, string> evidence = new(StringComparer.Ordinal);
    private Phase phase;
    private bool prepared;
    private ScopedSequence? sequence;
    private MetadataMod? metadata;
    private int deferrals;

    internal NativeSettingsCatalogProbes(Mod[] expectedStartup, Mod first, Mod second)
    { this.expectedStartup = expectedStartup; this.first = first; this.second = second; }
    internal bool Complete => phase == Phase.Complete;
    internal void Clear() => catalog.Clear();
    internal IReadOnlyDictionary<string, string> Evidence => new Dictionary<string, string>(evidence)
    { ["completedPhases"] = ((int)phase).ToString(), ["deferredAttempts"] = deferrals.ToString() };

    internal void Tick(SettingsNeutralReader reader)
    {
        if (Complete || Event.current?.type != EventType.Repaint) return;
        if (!prepared) { Prepare(); prepared = true; }
        var windows = Find.WindowStack.Windows.ToArray();
        var game = Current.Game;
        var status = catalog.ReadPending(reader, out string? reason);
        EndToEndAssert.True(SettingsReadScope.Current == null, "Catalog callback leaked its read scope.");
        EndToEndAssert.True(ReferenceEquals(Current.Game, game) && windows.SequenceEqual(Find.WindowStack.Windows),
            "A catalog diagnostic changed the native game or visible windows.");
        if (status == SettingsNeutralReadStatus.Deferred) { deferrals++; return; }

        if (phase is Phase.Startup or Phase.DuplicateObjects or Phase.ReplacementRequest or Phase.PageBoundary or Phase.TextBoundary)
        {
            EndToEndAssert.Equal(SettingsNeutralReadStatus.Captured, status, phase + ": " + reason);
            EndToEndAssert.True(!catalog.Pending);
        }
        else
        {
            EndToEndAssert.Equal(SettingsNeutralReadStatus.Failed, status, phase + ": " + reason);
            EndToEndAssert.Equal(0, catalog.Pages.Count, "Failed catalog must not publish partial pages: " + phase);
            EndToEndAssert.Equal(phase == Phase.RefreshDuringRead, catalog.Pending,
                "Only a new reentrant request may retain the pending slot: " + phase);
        }

        switch (phase)
        {
            case Phase.Startup:
                EndToEndAssert.Equal(expectedStartup.Length, catalog.Pages.Count);
                for (int index = 0; index < expectedStartup.Length; index++)
                    RequireNative(catalog.Pages[index], expectedStartup[index]);
                break;
            case Phase.DuplicateObjects:
                EndToEndAssert.Equal(2, catalog.Pages.Count);
                var a = catalog.Pages[0]; var b = catalog.Pages[1];
                EndToEndAssert.True(!ReferenceEquals(a, b) && a.PageIdentity != b.PageIdentity && a.OpenToken != b.OpenToken);
                RequireNative(a, first); RequireNative(b, first);
                sequence!.AssertCounts(3, 2);
                break;
            case Phase.EnumerationFailure:
                EndToEndAssert.Equal("settings-reader-callback-failed", reason);
                sequence!.AssertCounts(2, 1);
                break;
            case Phase.DisposalFailure:
                EndToEndAssert.Equal("settings-reader-callback-failed", reason);
                sequence!.AssertCounts(2, 1);
                break;
            case Phase.ClearDuringRead:
                EndToEndAssert.Equal("settings-catalog-request-replaced", reason);
                sequence!.AssertCounts(2, 1);
                break;
            case Phase.RefreshDuringRead:
                EndToEndAssert.Equal("settings-catalog-request-replaced", reason);
                sequence!.AssertCounts(2, 1);
                break;
            case Phase.ReplacementRequest:
                EndToEndAssert.Equal(1, catalog.Pages.Count);
                RequireNative(catalog.Pages[0], second);
                sequence!.AssertCounts(2, 1);
                break;
            case Phase.SourceReplacement:
                EndToEndAssert.Equal("settings-catalog-source-replaced", reason);
                sequence!.AssertCounts(2, 1);
                break;
            case Phase.PageBoundary:
                EndToEndAssert.Equal(4096, catalog.Pages.Count);
                EndToEndAssert.Equal(4096, metadata!.TitleReads);
                sequence!.AssertCounts(4097, 4096);
                break;
            case Phase.PageOverflow:
                EndToEndAssert.Equal("settings-catalog-page-limit", reason);
                EndToEndAssert.Equal(4096, metadata!.TitleReads);
                sequence!.AssertCounts(4097, 4096);
                break;
            case Phase.TextBoundary:
                EndToEndAssert.Equal(2, catalog.Pages.Count);
                EndToEndAssert.Equal(524288, catalog.Pages.Sum(page => page.Title!.Length));
                EndToEndAssert.Equal(2, metadata!.TitleReads);
                sequence!.AssertCounts(3, 2);
                break;
            case Phase.TextOverflow:
                EndToEndAssert.Equal("settings-catalog-text-limit", reason);
                EndToEndAssert.Equal(2, metadata!.TitleReads);
                sequence!.AssertCounts(2, 2);
                break;
        }
        if (metadata != null) EndToEndAssert.Equal(0, metadata.DrawCalls, "Catalog must not invoke an option renderer.");
        evidence.Add(phase.ToString(), status + "; reason=" + (reason ?? "none") + "; pages=" + catalog.Pages.Count +
            (sequence == null ? "" : "; " + sequence.Counts));
        phase++;
        prepared = false;
    }

    private void Prepare()
    {
        metadata = null;
        if (phase == Phase.Startup) { sequence = null; catalog.RefreshStartup(); return; }
        if (phase == Phase.ReplacementRequest)
        {
            // The preceding callback owns this replacement; do not schedule another request.
            sequence = nextSequence ?? throw new InvalidOperationException("Replacement sequence was not retained.");
            nextSequence = null;
            return;
        }
        sequence = new ScopedSequence(1, first);
        switch (phase)
        {
            case Phase.DuplicateObjects: sequence = new(2, first); break;
            case Phase.EnumerationFailure: sequence = new(2, first) { ThrowOnSecondMove = true }; break;
            case Phase.DisposalFailure: sequence.ThrowOnDispose = true; break;
            case Phase.ClearDuringRead: sequence.OnCurrent = catalog.Clear; break;
            case Phase.RefreshDuringRead:
                var replacementSequence = new ScopedSequence(1, second);
                var replacement = Carrier(replacementSequence);
                sequence.OnCurrent = () => catalog.Refresh(replacement);
                nextSequence = replacementSequence;
                break;
            case Phase.PageBoundary:
            case Phase.PageOverflow:
                metadata = new MetadataMod(string.Empty);
                sequence = new(phase == Phase.PageBoundary ? 4096 : 4097, metadata);
                break;
            case Phase.TextBoundary:
            case Phase.TextOverflow:
                metadata = new MetadataMod(new string('x', phase == Phase.TextBoundary ? 262144 : 262145));
                sequence = new(2, metadata);
                break;
        }
        var carrier = Carrier(sequence);
        if (phase == Phase.SourceReplacement)
            sequence.OnDispose = () => source.SetValue(carrier, new[] { second });
        catalog.Refresh(carrier);
    }

    private ScopedSequence? nextSequence;
    private Dialog_Options Carrier(IEnumerable<Mod> collection)
    {
        var carrier = new Dialog_Options { soundAppear = null, soundAmbient = null };
        source.SetValue(carrier, collection);
        return carrier; // Never added, opened, drawn or closed.
    }
    private void RequireNative(SettingsPage page, Mod expected) =>
        EndToEndAssert.True(catalog.TryResolve(page, out var actual) && ReferenceEquals(expected, actual));

    private sealed class MetadataMod : Mod
    {
        private readonly string title;
        internal int TitleReads, DrawCalls;
        internal MetadataMod(string title) : base(null!) => this.title = title;
        public override string SettingsCategory()
        {
            EndToEndAssert.True(SettingsReadScope.Current != null, "Deferred metadata escaped the read scope.");
            TitleReads++;
            return title;
        }
        public override void DoSettingsWindowContents(Rect rect)
        { DrawCalls++; throw new InvalidOperationException("Catalog must not call the diagnostic renderer."); }
    }

    private sealed class ScopedSequence : IEnumerable<Mod>
    {
        private readonly int count;
        private readonly Mod item;
        private int enumerators, moves, currents, disposals;
        internal bool ThrowOnSecondMove, ThrowOnDispose;
        internal Action? OnCurrent, OnDispose;
        internal ScopedSequence(int count, Mod item) { this.count = count; this.item = item; }
        internal string Counts => "GetEnumerator=" + enumerators + "; MoveNext=" + moves + "; Current=" + currents + "; Dispose=" + disposals;
        private static void RequireScope() => EndToEndAssert.True(SettingsReadScope.Current != null,
            "Deferred enumeration or disposal escaped the read scope.");
        internal void AssertCounts(int expectedMoves, int expectedCurrent)
        {
            EndToEndAssert.Equal(1, enumerators, Counts);
            EndToEndAssert.Equal(expectedMoves, moves, Counts);
            EndToEndAssert.Equal(expectedCurrent, currents, Counts);
            EndToEndAssert.Equal(1, disposals, Counts);
        }
        public IEnumerator<Mod> GetEnumerator()
        { RequireScope(); enumerators++; return new Enumerator(this); }
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        private sealed class Enumerator : IEnumerator<Mod>
        {
            private readonly ScopedSequence owner;
            private int position = -1;
            internal Enumerator(ScopedSequence owner) => this.owner = owner;
            public bool MoveNext()
            {
                RequireScope(); owner.moves++;
                if (owner.ThrowOnSecondMove && owner.moves == 2)
                    throw new InvalidOperationException("Owned catalog enumeration failure.");
                return ++position < owner.count;
            }
            public Mod Current
            {
                get
                {
                    RequireScope(); owner.currents++;
                    owner.OnCurrent?.Invoke();
                    return owner.item;
                }
            }
            object IEnumerator.Current => Current;
            public void Reset() => throw new NotSupportedException();
            public void Dispose()
            {
                RequireScope(); owner.disposals++;
                owner.OnDispose?.Invoke();
                if (owner.ThrowOnDispose) throw new InvalidOperationException("Owned catalog disposal failure.");
            }
        }
    }
}
