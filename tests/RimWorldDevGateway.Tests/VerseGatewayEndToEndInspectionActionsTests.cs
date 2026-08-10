using NUnit.Framework;
using RimWorldDevGateway.EndToEndTesting;
using System.Runtime.Serialization;
using Verse;

namespace RimWorldDevGateway.Tests;

[TestFixture]
public sealed class VerseGatewayEndToEndInspectionActionsTests
{
    [Test]
    public void Pawn_tab_requires_exact_sole_selection_then_verifies_native_open_state()
    {
        var runtime = new RecordingRuntime();

        var outcome = VerseGatewayEndToEndInspectionActions.Apply(
            new PawnInspectTabActionStep("open gear", "pawn_1", EndToEndPawnInspectTab.Gear),
            runtime);

        Assert.Multiple(() =>
        {
            Assert.That(outcome.Passed, Is.True);
            Assert.That(runtime.Calls, Is.EqualTo(new[]
            {
                "selected:pawn_1", "open-tab:Gear", "is-tab-open:Gear"
            }));
        });
    }

    [Test]
    public void Pawn_tab_fails_before_opening_for_stale_selection()
    {
        var runtime = new RecordingRuntime { SoleSelectionMatches = false };

        var outcome = VerseGatewayEndToEndInspectionActions.Apply(
            new PawnInspectTabActionStep("open health", "pawn_1", EndToEndPawnInspectTab.Health),
            runtime);

        Assert.Multiple(() =>
        {
            Assert.That(outcome.FailureCode, Is.EqualTo("inspect_tab_selection_mismatch"));
            Assert.That(runtime.Calls, Is.EqualTo(new[] { "selected:pawn_1" }));
        });
    }

    [Test]
    public void Thing_info_card_opens_and_closes_only_the_exact_thing()
    {
        var runtime = new RecordingRuntime();

        var opened = VerseGatewayEndToEndInspectionActions.Apply(
            ThingInfoCardActionStep.Open("open plate", "plate_1"),
            runtime);
        var closed = VerseGatewayEndToEndInspectionActions.Apply(
            ThingInfoCardActionStep.Close("close plate", "plate_1"),
            runtime);

        Assert.Multiple(() =>
        {
            Assert.That(opened.Passed, Is.True);
            Assert.That(closed.Passed, Is.True);
            Assert.That(runtime.Calls, Is.EqualTo(new[]
            {
                "open-card:plate_1",
                "is-card-open:plate_1",
                "is-card-open:plate_1",
                "close-card:plate_1",
                "is-card-open:plate_1"
            }));
        });
    }

    [Test]
    public void Info_card_close_fails_without_mutating_a_mismatched_window()
    {
        var runtime = new RecordingRuntime { InfoCardOpen = false };

        var outcome = VerseGatewayEndToEndInspectionActions.Apply(
            ThingInfoCardActionStep.Close("close plate", "plate_1"),
            runtime);

        Assert.Multiple(() =>
        {
            Assert.That(outcome.FailureCode, Is.EqualTo("info_card_window_mismatch"));
            Assert.That(runtime.Calls, Is.EqualTo(new[] { "is-card-open:plate_1" }));
        });
    }

    [Test]
    public void Info_card_resolution_limit_has_a_stable_failure_code()
    {
        var runtime = new RecordingRuntime { ThrowResolutionLimit = true };

        var outcome = VerseGatewayEndToEndInspectionActions.Apply(
            ThingInfoCardActionStep.Open("open plate", "plate_1"),
            runtime);

        Assert.That(outcome.FailureCode, Is.EqualTo("info_card_resolution_limit"));
    }

    [Test]
    public void Resolver_rejects_same_runtime_identity_across_supported_scopes()
    {
        var spawned = NewThing("SharedIdentity");
        var inventoried = NewThing("SharedIdentity");
        var source = new RecordingCandidateSource
        {
            Spawned = new[] { spawned },
            Inventoried = new[] { inventoried }
        };

        var resolved = new GatewayEndToEndThingResolver(source).ResolveExactThing(spawned.ThingID);

        Assert.That(resolved, Is.Null);
    }

    [Test]
    public void Resolver_uses_the_live_reference_and_reference_policy_rejects_a_stale_card()
    {
        var stale = NewThing("PlateIdentity");
        var live = NewThing("PlateIdentity");
        var source = new RecordingCandidateSource { Spawned = new[] { live } };

        var resolved = new GatewayEndToEndThingResolver(source).ResolveExactThing(live.ThingID);

        Assert.Multiple(() =>
        {
            Assert.That(resolved, Is.SameAs(live));
            Assert.That(
                GatewayEndToEndThingReferencePolicy.HasExactlyOneExactReference(live, new[] { stale }),
                Is.False);
            Assert.That(
                GatewayEndToEndThingReferencePolicy.HasExactlyOneExactReference(live, new[] { live }),
                Is.True);
            Assert.That(
                GatewayEndToEndThingReferencePolicy.HasExactlyOneExactReference(live, new[] { live, live }),
                Is.False);
        });
    }

    [Test]
    public void Reference_policy_permits_open_only_with_zero_existing_exact_cards()
    {
        var exact = NewThing("PlateIdentity");
        var stale = NewThing("PlateIdentity");
        var unrelated = NewThing("OtherIdentity");
        var runtimeId = exact.ThingID;

        Assert.Multiple(() =>
        {
            Assert.That(
                GatewayEndToEndThingReferencePolicy.CanOpenExactReference(
                    exact,
                    runtimeId,
                    Array.Empty<Thing>()),
                Is.True);
            Assert.That(
                GatewayEndToEndThingReferencePolicy.CanOpenExactReference(
                    exact,
                    runtimeId,
                    new[] { unrelated }),
                Is.True);
            Assert.That(
                GatewayEndToEndThingReferencePolicy.CanOpenExactReference(
                    exact,
                    runtimeId,
                    new[] { stale }),
                Is.False);
            Assert.That(
                GatewayEndToEndThingReferencePolicy.CanOpenExactReference(
                    exact,
                    runtimeId,
                    new[] { exact }),
                Is.False);
            Assert.That(
                GatewayEndToEndThingReferencePolicy.CanOpenExactReference(
                    exact,
                    runtimeId,
                    new[] { exact, exact }),
                Is.False,
                "An ambiguous existing-card state must be rejected without opening another card.");
        });
    }

    [Test]
    public void Resolver_enforces_the_shared_unique_interaction_candidate_budget()
    {
        var source = new RecordingCandidateSource
        {
            Spawned = Enumerable.Range(0, GatewayEndToEndThingResolver.MaximumThingCandidates + 1)
                .Select(index => NewThing("Candidate" + index))
                .ToArray()
        };

        Assert.Throws<GatewayEndToEndInspectionLimitException>(() =>
            new GatewayEndToEndThingResolver(source).ResolveExactThing("missing"));
    }

    [Test]
    public void Resolver_fails_closed_when_a_supported_holder_cannot_be_inspected()
    {
        var source = new RecordingCandidateSource
        {
            Spawned = new Thing[] { NewPawn("Holder") },
            DirectHolderFailure = new InvalidOperationException("fixture failure")
        };

        var exception = Assert.Throws<GatewayEndToEndInspectionIncompleteException>(() =>
            new GatewayEndToEndThingResolver(source).ResolveExactThing("missing"));

        Assert.That(exception!.InnerException, Is.TypeOf<InvalidOperationException>());
    }

    [Test]
    public void Verse_candidate_source_treats_a_null_direct_owner_as_an_empty_holder()
    {
        var source = new VerseGatewayEndToEndThingCandidateSource();

        Assert.That(source.DirectlyHeldThings(new NullThingHolder()), Is.Empty);
    }

    [Test]
    public void Inspect_pane_close_requires_exact_selection_and_tab_then_verifies_closed_state()
    {
        var runtime = new RecordingRuntime();

        var outcome = VerseGatewayEndToEndInspectionActions.Apply(
            new InspectPaneCloseActionStep(
                "close contents",
                "fridge_1",
                "AdaptiveStorage.ContentsITab"),
            runtime);

        Assert.Multiple(() =>
        {
            Assert.That(outcome.Passed, Is.True);
            Assert.That(runtime.Calls, Is.EqualTo(new[]
            {
                "selected-thing:fridge_1",
                "close-inspect:AdaptiveStorage.ContentsITab",
                "is-inspect-closed"
            }));
        });
    }

    [Test]
    public void Window_cancel_invokes_only_one_exact_native_window_then_verifies_removal()
    {
        var runtime = new RecordingRuntime { ExactWindowOpen = true };

        var outcome = VerseGatewayEndToEndInspectionActions.Apply(
            new WindowCancelActionStep("cancel dialog", "Verse.Dialog_MessageBox"),
            runtime);

        Assert.Multiple(() =>
        {
            Assert.That(outcome.Passed, Is.True);
            Assert.That(runtime.Calls, Is.EqualTo(new[]
            {
                "is-window-open:Verse.Dialog_MessageBox",
                "cancel-window:Verse.Dialog_MessageBox",
                "is-window-open:Verse.Dialog_MessageBox"
            }));
        });
    }

    [TestCase(false, false, true)]
    [TestCase(true, false, true)]
    [TestCase(true, true, false)]
    public void Window_cancel_uses_a_synthetic_key_event_outside_a_real_key_down(
        bool currentEventIsKeyDown,
        bool currentKeyIsEscape,
        bool expected)
    {
        Assert.That(
            GatewayEndToEndCancelEventPolicy.RequiresSyntheticKeyEvent(
                currentEventIsKeyDown,
                currentKeyIsEscape),
            Is.EqualTo(expected));
    }

    private sealed class RecordingRuntime : IGatewayEndToEndInspectionRuntime
    {
        public bool PlayerHasControl { get; set; } = true;

        public bool SoleSelectionMatches { get; set; } = true;

        public bool InfoCardOpen { get; set; }

        public bool ThrowResolutionLimit { get; set; }

        public bool ExactWindowOpen { get; set; }

        public List<string> Calls { get; } = new();

        public bool IsSoleSelectedPawn(string runtimeId)
        {
            Calls.Add("selected:" + runtimeId);
            return SoleSelectionMatches;
        }

        public bool IsSoleSelectedThing(string runtimeId)
        {
            Calls.Add("selected-thing:" + runtimeId);
            return SoleSelectionMatches;
        }

        public bool OpenPawnTab(EndToEndPawnInspectTab tab)
        {
            Calls.Add("open-tab:" + tab);
            return true;
        }

        public bool IsPawnTabOpen(EndToEndPawnInspectTab tab)
        {
            Calls.Add("is-tab-open:" + tab);
            return true;
        }

        public bool OpenThingInfoCard(string runtimeId)
        {
            Calls.Add("open-card:" + runtimeId);
            if (ThrowResolutionLimit)
            {
                throw new GatewayEndToEndInspectionLimitException();
            }
            InfoCardOpen = true;
            return true;
        }

        public bool IsExactThingInfoCardOpen(string runtimeId)
        {
            Calls.Add("is-card-open:" + runtimeId);
            return InfoCardOpen;
        }

        public bool CloseExactThingInfoCard(string runtimeId)
        {
            Calls.Add("close-card:" + runtimeId);
            InfoCardOpen = false;
            return true;
        }

        public bool CloseInspectPane(string expectedTabRuntimeType)
        {
            Calls.Add("close-inspect:" + expectedTabRuntimeType);
            return true;
        }

        public bool IsInspectPaneClosed
        {
            get
            {
                Calls.Add("is-inspect-closed");
                return true;
            }
        }

        public bool IsExactWindowOpen(string expectedWindowRuntimeType)
        {
            Calls.Add("is-window-open:" + expectedWindowRuntimeType);
            return ExactWindowOpen;
        }

        public bool CancelExactWindow(string expectedWindowRuntimeType)
        {
            Calls.Add("cancel-window:" + expectedWindowRuntimeType);
            ExactWindowOpen = false;
            return true;
        }
    }

    private static Thing NewThing(string defName) => new() { def = UninitializedThingDef(defName) };

    private static Pawn NewPawn(string defName) => new() { def = UninitializedThingDef(defName) };

    private static ThingDef UninitializedThingDef(string defName)
    {
        var def = (ThingDef)FormatterServices.GetUninitializedObject(typeof(ThingDef));
        def.defName = defName;
        return def;
    }

    private sealed class RecordingCandidateSource : IGatewayEndToEndThingCandidateSource
    {
        public IReadOnlyList<Thing> Spawned { get; set; } = Array.Empty<Thing>();

        public IReadOnlyList<Thing> Inventoried { get; set; } = Array.Empty<Thing>();

        public IReadOnlyList<Thing> DirectlyHeld { get; set; } = Array.Empty<Thing>();

        public Exception? DirectHolderFailure { get; set; }

        public IEnumerable<Thing> SpawnedThings() => Spawned;

        public IEnumerable<Thing> PawnInventoryThings() => Inventoried;

        public IEnumerable<Thing> DirectlyHeldThings(IThingHolder holder)
        {
            if (DirectHolderFailure is not null)
            {
                throw DirectHolderFailure;
            }

            return DirectlyHeld;
        }
    }

    private sealed class NullThingHolder : IThingHolder
    {
        public IThingHolder ParentHolder => null!;

        public ThingOwner GetDirectlyHeldThings() => null!;

        public void GetChildHolders(List<IThingHolder> outChildren)
        {
        }
    }
}
