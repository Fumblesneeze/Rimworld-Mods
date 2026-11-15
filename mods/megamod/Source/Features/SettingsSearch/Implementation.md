# S141 query, recorder and discovery-session checkpoints

Owner `fumblesneeze.megamod`. Supplied-text matching and the ordinary-draw recorder are implemented;
the recorder has scoped native verification below. Nothing is registered with the native chooser.
Automatic discovery, metadata projection, reveal, coverage UI, lifecycle, preferences/catalogs and
full-feature acceptance remain open. No per-mod eligibility list exists.

`SettingsSearch` copies immutable `SettingsSearchEntry` descriptors and pre-normalizes their text.
Each retains S047's actual opaque `SettingsPage` descriptor, path identity and occurrence; native
code must derive those from actual page/tab/control identity, never display labels or coordinates.
`Find` returns original descriptors and match-kind flags. It never calls an owner, changes a value,
logs arbitrary text or persists anything. Recreate/discard snapshots when native lifecycle demands;
this class is not a runtime cache or currentness receipt.

Queries support AND words, quoted/unclosed phrases, exclusions and mod/author/id/tab filters.
Unrecognized prefixes stay literal. Text normalization removes syntactic rich tags, treats line
break tags as whitespace, collapses whitespace and uses Form C/invariant case. Original displayed
strings remain untouched. Page entries search their page metadata only; options additionally use
label/help/display/tab/feature/category. A page-title miss cannot hide an option hit. Empty input
returns no option hits so the future chooser can restore ordinary page browsing.

Typos apply only to eligible positive plain words with no exact occurrence anywhere in the supplied
snapshot before filters. Five-to-eight Unicode scalar values allow one edit; nine or more allow
two. Insertion/deletion/substitution and adjacent transposition are supported. A depth-two search
uses bounded branches rather than a quadratic distance matrix. Literal fields/exclusions/phrases
never expand. Results sort exact before typo, label before other context, then ordinal package,
page, path and numeric occurrence; equal complete identities retain input order.

Read-only inspiration: Needle1.7 DLL SHA256
7FBB6CE4E88EFB63A407E4277F96959761737D51BF3F94DAFB15BC95443D60F4,
`Needle.Search.Query` and `SearchEngine` in the retained November14 decompilation. Owned parser,
matching, normalization and bounded edit implementation are independent; no source code, weights,
private character-fold table, binary or assets were copied. The scoped native research packet is
`artifacts/Implementation/megamod-p0/settings-search/native-research/`.

Focused Unit tests exercise only detached data; no Verse/Harmony/loaded mod database is present.
Five initial RED/GREEN cycles, a generated two-edit counterexample/fix and seven review regressions are retained in
`artifacts/Implementation/megamod-p0/settings-search/query/`. The current affected search/chooser
batch passes61/61. Review regressions cover literal sharp-S casing, supplementary-letter typo words
and S047's empty-title fallback ranking. Independent re-review found no remaining issues; the
canonical build/package and strict OpenSpec checks passed. That detached-query checkpoint did not
accept native behavior; the later ordinary-draw component verification is scoped below.

The next component tranche adds `SettingsCaptureBuffer` and `SettingsPageRecorder`. The buffer
retains bounded immutable text/help observations and independent occurrences, discards a failed
callback's records, and never calls a successful draw complete coverage. The recorder forwards the
actual1.7 `IGuiBackend` contract unchanged around one ordinary page callback; it records only
repaints and supplied literal tooltip text, without invoking tooltip getters or extra page draws.
It restores its exact previous backend only while it still owns the slot. Nested scopes isolate
their observations; callback exceptions propagate unchanged after failed-capture publication.

All geometry is logical screen geometry from four transformed corners. It is not yet admitted
visibility, stable control identity or a reveal target. Invalid bounds are omitted; a coordinate
capture exception fails recording without suppressing the real draw. Default retention policy is
16384 records/262144 UTF-16 units per callback, explicitly a pilot limit rather than an engine
ceiling. Overflow retains a bounded prefix with Truncated status; it does not stop native rendering.

Five focused buffer RED/GREEN slices pass with82 total capture/query/chooser checks. The native
stub failed at the intended missing-observation assertion after the actual foreign page opened;
reviewed full-recorder verification passed41steps with seven personally inspected captures. The
foreign page retained its values during capture and normal edit/close/reopen remained functional.
Callback failure, distinct backend replacement, nested isolation and publication timing passed.
Evidence is under
`artifacts/Implementation/megamod-p0/settings-search/recorder/`. The native fixture uses test-only
instrumentation; no production hooks, search preference, UI, automatic discovery or reveal are
registered by this component checkpoint. Every remaining universal-discovery obligation is retained.

`SettingsDiscoverySession` now owns detached admission/progress for every supplied page descriptor,
including equal-value descriptors with independent ordinal identity. It admits one in-flight work
receipt and at most one callback per monotonically numbered repaint. Refresh invalidates old
receipts/results while preserving that repaint watermark; cancellation retains finished observations
and rejects late completion. Capture failure discards the page's provisional observations, and
successful callbacks remain Partial. Default limits are96 callbacks/page,500ms/page and100ms/callback;
time is supplied by the native caller and can stop only later callbacks, not preempt one in progress.
The session defensively copies captured record lists and publishes immutable progress snapshots.

Eight actual focused RED/GREEN slices, including the independent review's same-repaint refresh
regression, pass110 affected discovery/capture/query/chooser checks. Evidence is under
`artifacts/Implementation/megamod-p0/settings-search/discovery/core/`; review is under
`discovery/review/`. This host checkpoint is not automatic discovery or native acceptance. D1–D3
remain open until integration with the neutral reader and the required native workflow are verified.

`SettingsGuiStateScope` captures an explicit native1.7 field/collection contract before mutation,
isolates native editor/scroll/control collections, and restores the same original objects and
known scalar/content/style state. It does not inspect foreign settings. Shape/budget refusal occurs
before isolation; partial isolation failure attempts every restoration, and disposal is idempotent.
The65,536-entry snapshot cap is pilot policy. Backend/event ownership, input admission, native
control output/write guards and callback dispatch remain the caller's unimplemented obligations.

The reviewed English `megamod.settings-search.native-state` component workflow passed24/24steps
after actual native RED. Root personally inspected five exact-run captures: native typing before
isolation, unchanged focused text after success and exception, subsequent native typing without
refocus, and the console. The same-ID scratch editor lookup and style/state assertions are supporting
diagnostics. They do not prove a hidden native control draw, moved caret/noncollapsed selection,
scroll restoration, budget/shape refusal, every captured field or arbitrary foreign-page behavior.
Those remain required in the integrated reader batch. Evidence and exact reviewed source/build
identities are in `artifacts/Implementation/megamod-p0/settings-search/discovery/root/verification.md`.
No automatic hooks or additional player settings are registered by this checkpoint.

`SettingsHiddenBackend` supplies private group/matrix/clip/window state and suppresses backend
rendering commands. Native metric delegation requires the exact initialized engine backend and
leases only its text-layout scale, restoring it before return. Unsupported state-changing backend
calls and structural/geometry failures latch a fixed diagnostic; callers must reject failure or
unbalanced state. The256-open-structure cap is pilot policy. No native backend ownership, event,
input or foreign callback admission is implied by this helper.

Its reviewed first English native workflow passed17steps/four personally inspected captures after
inert-stub RED. The real button requested hidden native label/fill drawing, sustained for19repaints
through screenshot completion. The label was recorded while no hidden text/magenta fill appeared;
subsequent real input changed the original checkbox. Native text sizing, fractional nested group
origins, balance and scope restoration passed supporting assertions. Review's transient screenshot
gap was fixed and the prevention saved to the testing skill. This does not yet verify every metric,
scale, transform, draw method, failure path, custom backend or real foreign page. Those and native
control/write guards, provider catalog and automatic dispatch remain D2/D3 integration work.

`SettingsReadScope` now retains a caught first failure, rejects nested reads, and keeps ownership
and cleanup on the creating thread. `SettingsReadActionGuard` uses existing C01 owned receipts and
safe publication inspection to prove first-prefix ordering without removing compatible patches.
`SettingsReadActionGuards` binds six exact native window/settings/clipboard/IME actions and admits
a read only after all guards are current. It has no production registration yet.

This component has23 focused passing host tests, three independent review passes with the one
native test-payload finding fixed, warning-free product build and package checks. Reviewed English
`megamod.settings-search.native-actions` passed16steps after actual inert-binder RED. Root inspected
four captures: real button, unchanged panel/checkbox after refusals, subsequent real checkbox edit
and console. Native assertions additionally verify the disposable saved baseline, IME and clipboard
backstop order. See local `discovery/actions/root/verification.md` for exact identities, logs and
successful cleanup. This does not close D2/D3: widget/ref/scroll/tooltip guards, idle admission,
provider collection and complete automatic discovery/search/reveal remain required.

`SettingsReadControlGuard` and `SettingsReadControlGuards` now preserve outward values at 26 exact
native control boundaries. Typed snapshots retain strings, numeric buffers, vectors, sliders,
ranges and scroll positions; separate prefixes suppress native slider-drag sound checks and hidden
tooltip registration while recording literal help. String nesting preserves native intermediate
normalization and restores only the outer string return. C01 validates first-prefix/last-finalizer
order and exclusive paired state, refusing unresolved relevant factories without executing them.
Ordinary calls outside an admitted read retain their native behavior. No production hook is added.

The independently reviewed batch passes 40 focused Harmony checks and the English native-control
workflow's 31 steps. Root personally inspected five captures: unchanged full text/0.3737 slider
after two real requests, then deliberate native typing and a slider drag to 0.9344, followed by the
native console. Native numeric/vector/range/scroll/tooltip/sound checks are supporting diagnostics.
Actual RED found the initial truncation, then a public scroll-call gap; all four public scroll
entries now have their own guards. The final fixture uses the native slider's required drag gesture.
Review fixes and these recurring lessons are retained in the development skills. Exact build,
source/process identities, logs and successful cleanup are in local
`artifacts/Implementation/megamod-p0/settings-search/discovery/values/root/verification.md`.
Idle input, Listing_Standard culling/value/cache preservation, other numeric closures, provider
collection, integrated automatic discovery and search/reveal remain open D2/D3 obligations.

`SettingsReadInputAdmission` now binds native input storage once and classifies GUI repaints as
Idle, Busy or Unavailable without resetting or consuming input. It covers the inspected keyboard,
five mouse buttons, movement/scroll and Windows IME contract; focused but idle text remains eligible.
Constructor failures must be contained by the eventual reader initializer. The control binder also
adds exact instance Listing_Standard.IntEntry preservation, bringing it to 27 targets.

The reviewed English input/listing batch passed 47 native steps (31 input, 16 listing), with nine
personally inspected captures. Real typing/dragging produced independently witnessed Busy frames,
then focused idle eligibility resumed; text and slider edits remained visible. A real listing
request retained 7 despite a hidden minimum of 29, then native +1 changed it to 8. Worker rejection,
missing-device and empty-text IME checks are supporting diagnostics, not genuine IME action proof.
Review strengthened every-active-frame assertions, asynchronous diagnostic waits and independent
cleanup; these lessons are saved in the testing skill. Exact evidence is under local
`discovery/input-listing/root/verification.md`. Repeat/extra-button/wheel/IME native actions,
listing culling/cache preservation, other numeric closures and integrated automatic discovery
remain open. This component still registers no production reader callbacks.

`SettingsReadListingGuards` now protects the two native label-scroll cache fields around exact
Listing_Standard.Label/End calls. It temporarily supplies detached lists and restores the acquired
instance's original references on success or exception. The combined snapshot limit is 65,536
entries per call; unavailable state refuses the read without restricting provider eligibility.

The reviewed English cache workflow passed 22 native steps after a retained inert RED. Root
inspected all five captures: native scrolling exposed lines 13–20, a real hidden-read request kept
that view unchanged, and a subsequent native scrollbar click restored the first lines. Nonempty
seen-list state, null caches, nested/repeated calls, exceptions and exact/over-limit snapshots are
supporting diagnostics. Both review findings concerned missing test distinctions and are resolved;
the empty-baseline prevention is saved to the testing skill. Build/package/cleanup/config evidence
is under local `discovery/listing/root/verification.md`. Listing unculling, generic consumer proof,
remaining input/numeric coverage and complete provider/reader/search/reveal integration stay open.

The next host component adds passive generic-family publication inventory and an original-body
nullable listing proof. Inventory includes every published variant of the selected declaration,
without a numeric-type list; relevant publication changes permanently invalidate its receipt.
Currentness is separate from semantic admission. The checker follows the exact null/overlap draw
relationship, including post-draw backedges and tail blocks that can modify a row before drawing.

The initial family/semantic checkpoint has 12 detached inventory and 28 Harmony checks; its local
evidence is in `discovery/listing/proof-root/verification.md`. The following semantic extension
adds exact derived-rectangle producers, sequential drawing stages, symbolic generic operands,
pre-cull scalar/constructor preparation and shared scalar lifetimes. Its 56 focused checks include
executable review regressions for repeated final draws and scalar writes between projections,
paired with intact-gate reentry, harmless post-use controls and a complete recomputed next cycle.
Stored producer dependencies remain live through partial later-cycle reuse until those producers
actually refresh, preventing old/new projection pairs in either direction and through aliases.

`HarmonyOriginalBody` supplies bounded detached original instructions without replaying published
factories, attributes or transpilers. It reuses the existing structural decoder before the raw
provider reader, preserving generic context and copied branches/locals. It passes 16 focused
checks; the existing published-body reader's 18 checks also pass with its restrictions intact.
Current batch evidence is in local `discovery/listing/continuation-root/verification.md`.

The native listing integration binds all 17 direct consumers and checks the declaring class for
additional getter references. Original-body semantics are combined with exact owned value/cache
preservation, effective callback arguments and current generic publication families. The acquired
getter skips native bounds calculation only inside a read and retains the null sentinel through
the last finalizer. Exception-suppressing finalizers refuse admission. No automatic production
reader callbacks are registered.

This batch has 93 distinct passing focused host checks: 20 admission, 40 common control guard and
33 existing argument-binding consumer regressions. Independent review fixes cover aliased shared
state, bounded memoized value-shape inspection, independent rollback attempts and getter exception
suppression. The reviewed English native workflow passes 22 steps with six root-inspected captures:
a real read request finds an offscreen option/help without moving the viewport or changing its
value, normal scrolling reveals the original unchecked control, and a native click checks it.
All 17 row calls, cold/warm caches, getter effects and struct-generic publication/repatch/cleanup
have supporting diagnostic probes. Catalog/getter/review RED and final build/process/config/cleanup
evidence is retained in local `discovery/listing/integration-root/verification.md`.
Transformed consumer bodies and the remaining input/numeric/provider/automatic discovery/search/
reveal obligations stay open. This integration is a component of S141, not full feature acceptance.
