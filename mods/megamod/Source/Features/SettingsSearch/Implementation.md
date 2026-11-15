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
