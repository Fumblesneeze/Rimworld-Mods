## Context

Sole owner: Make Rimworld Great Again (`fumblesneeze.megamod`, `mods/megamod`), 1.7 beta.
On 2026-11-14 the user explicitly selected **all mods' individual options**, not just megamod or
a supported subset. This supersedes this change's original owned-only draft and S047's exclusion of
foreign control search. See [research](../../../mods/megamod/docs/refinement/2026-11-14-needle-research.md).

## Goals / Non-Goals

**Goals:** find options across all available active-mod settings pages with no package allowlist or
per-mod opt-in; reveal their real page/tab/control; preserve ordinary editing; make discovery gaps
visible. Keep the common settings scope/inheritance rules for owned controls.

**Non-goals:** replace foreign settings stores/editors, change settings during discovery, import
Needle preferences, reproduce its binary APIs, or provide a general Def browser. Universal mod
eligibility is required; invisible conditional branches and unreadable custom rendering cannot be
invented as captured options. Those limits require truthful status and refresh, not a curated scope.

## Decisions

1. **One chooser, two result kinds.** Extend native Options → Mod options with individual-option
   results alongside page matches. S047 owns page identity, author grouping and normal opening;
   S141 owns option discovery/query/reveal. Reuse one query field when both are enabled. With S141
   disabled, S047 retains its existing substring behavior; with S141 enabled, its query syntax
   governs both result kinds, while page cards match only their own metadata. Option hits remain
   visible even when no page title matches. Turning S047 sorting off must not disable S141.
   No second settings framework, gameplay tab or separate foreign editor.
2. **Generic reader, metadata fast path.** Enumerate the complete actual settings-page collection
   and retain independent page instances, including provider-generated pages sharing a title or
   package ID. Every page is eligible without a mod-specific adapter or author registration.
   Observe standard/native GUI text, tooltips, value displays, control geometry, scroll contexts and
   observed tab navigation, as Needle does. Traverse navigational tabs in a bounded session; do not
   click arbitrary buttons or change settings to expose conditional branches. Optional provider
   adapters may improve access to their actual page collection; they cannot be the only way a
   particular package qualifies. Missing provider access is an explicit coverage gap.
   For owned rows use existing registration IDs, labels/help, feature/category and reveal handles;
   composite/dynamic rows expose meaningful subentries through that same registration.
3. **Capture at a valid GUI boundary.** Inspect exact 1.7 contracts before choosing hooks. Draw
   callbacks and observed tab callbacks are permitted for discovery, with neutral input, scoped
   recording and reentrancy guards. Never dispatch editor/reset/apply/write actions. Restore
   capture-owned GUI/event state, selected tab path, scroll and temporary windows in guaranteed
   cleanup. Needle restores GUI state but cannot transactionally undo arbitrary callback effects;
   ours must not promise that either. Detectable mutation or restoration failure aborts that page,
   reports failure and retains ordinary access; native acceptance must demonstrate unchanged
   settings for the exercised readers. Do not swallow an unsafe failure as successful indexing.
4. **Bounded, observable discovery.** Perform initial capture at startup's valid GUI phase when
   enabled, or on first enable/open if not yet read. Schedule bounded work between callbacks with
   progress and cancellation. Pin draw/tab/depth/time budgets from measured layouts before code;
   exhausting one produces partial status, never silent complete coverage. An in-flight third-party
   callback cannot be forcibly preempted on the Unity thread; report measured overruns and stop
   further calls. No indexing loop during ordinary gameplay or capture on every query keystroke.
   Observe normal page draws to refresh changed rows; explicit Refresh retries incomplete pages.
5. **Source-inspired query.** Readable localized text, case-insensitive Unicode substrings,
   normalized whitespace, AND terms, quoted phrases, exclusions (`-term`) and
   `mod:`/`author:`/`id:`/`tab:` filters. Fields restrict the associated term; `mod:` covers
   mod and settings-page names, `id:` the package ID, and other fields their named metadata.
   Unknown field prefixes remain literal text. An unclosed quote consumes the remaining phrase.
   Exclusion-only queries filter all entries. Empty input restores ordinary page browsing without
   dumping all option rows. Label matches rank before help/value-only matches; exact matches precede
   typo fallback. Use deterministic page/path/occurrence ties, not copied source numeric weights.
   Default typo fallback applies only to positive unqualified unquoted words with no exact match,
   length 5–8 at most one edit, 9+ at most two; insertion, deletion, substitution and transposition
   count as edits. Phrases, exclusions and fields remain literal. No `def:` classifier is selected.
6. **Identity and reveal use fresh observations.** Keep package identity plus actual page identity,
   tab path and control occurrence/owned setting ID; labels alone never select a duplicate. Result
   activation opens the real page, follows only observed navigation and resolves the target in the
   current layout before scrolling/highlighting for three real elapsed seconds. No setting edit is
   performed by selection. Missing or ambiguous targets show a reason and open the correct page
   without guessing or clicking a nearby control. User navigation cancels pending reveal.
7. **Values retain their provenance.** Owned results read common requested/effective values and
   current Global/Current save/Use global binding. Foreign values are last-observed display text,
   identified as such until refreshed by a real draw; never deserialize/reflection-read arbitrary
   private settings or label stale values current. Unknown values stay unknown. Opening/editing/
   closing uses the original owner's normal persistence; our scope controls apply only to our own
   options. Do not mirror foreign settings into megamod storage.
8. **Lifecycle and activation.** Query/results/highlight are view-local. Captured index data is
   process-local; language, page collection and main-menu/save/world binding changes invalidate
   affected observations and reveal targets before reuse. Conditional controls refresh when their
   owner renders them; discovery never flips a prerequisite setting. The default-on
   `settingsSearch.enabled` common GlobalAndSave Boolean belongs in Interface/navigation and uses
   NextDisplay. Effective off cancels further capture and restores normal browsing. Later enable
   schedules missing discovery. Active `ZIPender.Needle` takes the standard bounded warning and
   best-effort ordinary effective-off path, preserving stored preferences; no coexistence claim.

## Risks / Trade-offs

Generic GUI observation is the selected approach because foreign mods need no metadata contract.
Custom rendering, inaccessible tabs, conditional rows and nonreturning callbacks remain real limits.
Show per-page pending/partial/failed status and counts; a zero-result query with gaps must not imply
that no mod has that option. Absence of an exception is not proof that every custom control was read.
Unknown rendering should remain unverified. Shape guards apply to generic engine/provider contracts,
not package eligibility lists. Preserve ordinary page access even if the search hooks are unavailable.

The index contains displayed text only and is not serialized to saves or exported to logs. Keep
diagnostics bounded and avoid dumping arbitrary user-entered option values. No installed source
assembly/code/assets may be copied. Measure discovery/query/reveal cost on representative real pages
before performance claims. Host tests own detached matching/identity policy; real draw callbacks and
provider editing require fresh isolated native workflows.

## Migration Plan

The first implementation tranche is detached query behavior only, without runtime registration.
It reuses S047's immutable page descriptor and retains opaque page/path/occurrence identity; labels
and coordinates are never identity. Unicode text uses canonical composition, invariant case matching
and collapsed whitespace; syntactic rich-text tags are removed while literal comparisons remain.
Typo eligibility is determined per positive plain word against the whole supplied snapshot before
field/exclusion filtering: an exact occurrence anywhere suppresses that word's typo fallback.
This preserves the inspected source behavior without its numeric scoring algorithm. Match ranking
uses exact before typo, label matches before other context, then ordinal package/page/path/occurrence
ties. Page records search page metadata only. No callbacks, settings access or persistent index are
part of this tranche; snapshot lifecycle remains the future native adapter's obligation.

Later implementation adds one common enabled
preference and transient discovery state. Missing preference means inherited/default-on; disabling
does not alter foreign settings or require importing Needle data.

## Open Questions

### Native recorder tranche (plan revision29)

Build the reusable observer before adding extra foreign draw calls. A scoped recorder surrounds
one complete real page callback, forwards every GUI backend operation unchanged, and records text
and literal tooltip text in current screen coordinates. It must not invoke tooltip getters,
navigation, editors, settings writes or additional page draws. This ordinary-draw pilot does not
replace the required automatic startup discovery, tab traversal or reveal; those remain subsequent
work using the same recorder. No search UI or automatic runtime hooks are registered by this tranche.

The detached buffer retains distinct draw occurrences even for equal labels; these are observations,
not yet proven control identities. It publishes an immutable partial observation only after the
whole callback returns. A thrown callback discards that capture and keeps the original exception;
backend replacement is a failed capture and must not overwrite the foreign replacement. Unknown
custom drawing and unvisited layouts remain unverified. Observing one repaint never establishes
complete page coverage. Geometry is diagnostic until current-layout control resolution is implemented.

Initial pilot retention limits are explicit safety policy, not engine limits: at most16384 nonempty
text records and262144 UTF-16 units per callback. Exceeding either stops further recording, retains
the bounded prefix with truncated status and continues forwarding drawing unchanged. These generous
research-proposed limits will be measured on the native pilot; no callback-time or traversal budget
is claimed by a passive observer. Failed, invalid or missing geometry cannot become a reveal target.

The first English workflow opens the actual unfamiliar Pick Up And Haul page through native Options,
observes labels and literal help during normal repaints, and compares its native values before/after
observation. A deliberate native checkbox edit and close/reopen proves original editing still works;
cleanup restores it through the same control. Test instrumentation may surround the native page call
and pass real tooltip arguments to the recorder; it must not draw a synthetic replacement page or
manufacture search results. This verifies the recorder component only, not SS-01/SS-03 acceptance.

### Automatic discovery session (plan revision30)

The next tranche schedules generic discovery for every supplied real page identity. A detached
session copies the page descriptors, keeps equal titles/packages independent, permits one in-flight
callback and at most one newly admitted callback per repaint, and publishes only the matching
current work receipt. Refresh replaces the catalog and invalidates all older receipts/results;
it preserves the last admitted repaint watermark, so neither refresh nor cancel/refresh permits
a second callback during the same native repaint. The caller supplies monotonic repaint IDs.
cancel stops future callbacks and rejects an in-flight completion while retaining earlier finished
observations with honest status. Runtime language/save/world/page changes use the same invalidation.
Page callbacks may be continued across repaints for observed navigation; completing a page still
means Partial observations, never proof of all custom or conditional controls.

Pilot work limits are policy, not framework ceilings:96 exploration callbacks per page,500ms
cumulative callback time and100ms for any single callback. The ordinary native pilot measured a
maximum5.1376ms for its simple page's entire callback; these limits provide explicit headroom while
further representative measurements remain required. The session admits only one callback per
repaint, rather than claiming that an indivisible foreign callback fits a4ms budget. It cannot
preempt a callback; an overrun is detected after return and stops further calls to that page.
Reaching a continuation limit retains bounded observations with an explicit stop reason. Buffer
retention overflow stops that page; failed captures discard that page's provisional observations.
Tab-state/depth and restoration budgets remain the native traversal adapter's later obligations.

Native neutral-draw dispatch is withheld until its exact engine state/editor/window-write contract
is pinned. Source inspection already establishes that ordinary GUI collection snapshots must be
isolated before a draw so reused scroll/editor objects are not mutated; public Text settings alone
do not restore shared style mutations. Startup collection creation must preserve the provider-
completed chooser list, with normal page access retained for any collection or draw failure. This
does not permit filtering eligibility to known packages or substituting visited-only discovery.

The exact1.7.5005 neutral contract is retained in the local discovery/neutral-contract matrix.
Implementation isolates explicitly named GUI collections by saving their entries, clearing before
the hidden callback, then restoring the same entries/objects. It must not leave original scroll or
text-editor objects reachable to hidden native controls. Capture/restore known scalar latches,
native reusable GUIContent values, Text style alignment/wrapping and the mouse-over scroll stack.
Resolve field shapes first; do not clone all static fields or inspect foreign settings. A pilot
cap of65536 aggregate retained native collection/array entries bounds snapshot work; an excess is
an explicit reader gap before mutation, not a framework or package eligibility limit.

The first state-isolation component workflow uses an owned native text-field fixture: type through
the real control, preserve its focus/editor/cursor/selection while an isolated callback uses the
same control ID, restore exact shared text styles on success and exception, then continue typing
without a refocus click. This establishes only native state restoration; hidden rendering, native
widget/write guards and unfamiliar/provider-page discovery still require the integrated D2/D3 run.

Hidden geometry has independent group/clip/matrix state and never forwards drawing/window/input
state changes to the visible backend. Preserve native slider/string/ref-buffer outputs, suppress
the exact native slider sound boundary, and stop native Add/ImmediateWindow/TryRemove/settings-write
and clipboard writes before effects. Unknown lazy tooltip getters are not invoked. A caught
forbidden-action exception must still leave failure latched. Exact native frame-loop inspection
supports idle-input admission rather than global input shadowing: defer on actual held/down/up/
repeat, all five mouse buttons, motion/scroll or IME; a focused idle editor alone does not block.
Foreign direct device pumping, I/O or arbitrary field assignment cannot be rolled back generically.

Native text metrics may delegate only at a completed callback boundary on the GUI thread, with
no reentry into an in-progress DrawText/Layout. The inspected native methods rebuild their own
layout scratch and copy detached outputs; the next visible draw rebuilds it again. Lease and
restore only the exact native TextLayout.Scale around each metric call, matching the hidden
matrix's x-column magnitude (native zero-scale identity fallback). Keep scale-keyed glyph caches;
do not forward SetMatrix to correct measurement. Unknown replacement backends remain an explicit
reader gap. Hidden group geometry reproduces native per-push pixel snapping, including fractional
nested origins; plain accumulated offsets are insufficient. These source premises still require
the hidden backend's native workflow and do not establish accepted rendering or reveal behavior.
The hidden backend's draft pilot cap is256 aggregate simultaneously open group/clip/window entries;
excess or unbalanced structure fails that capture, not package eligibility. This is a bounded-work
policy rather than a native framework limit. Unsupported render-target/font/language mutations
latch a fixed failure even if a callback catches the immediate exception.

The first hidden-backend component workflow uses a native button to request isolated repaints
of a real native label and a conspicuous colored fill. Keep the probe active through screenshot
completion, bounded to128 fixture repaints, so a one-frame leak cannot disappear before evidence.
Capture must retain the label and native text
metrics while neither hidden draw appears over the visible panel. Diagnostics check fractional
nested group origins and balanced cleanup; a subsequent deliberate native checkbox click must
still change the original visible control. This component fixture does not yet establish foreign
callbacks, transformed text metrics, scroll/control guards, every draw method or reveal geometry.

For startup page acquisition, use a fresh detached native Dialog_Options with soundAppear and
soundAmbient null, call its normal PostOpen under the same isolation, and materialize the final
cachedModsWithSettings there. Do not call PreOpen, add, draw or close that carrier. This preserves
provider PostOpen contributions without ordinary window/selection/persistence effects; provider
callbacks still have the same explicit arbitrary-effect limits. Normal Options opening refreshes
from its actual completed collection. The source's deferred enumerable must not escape isolation.

Implementation must pin native/provider page enumeration, GUI/tab/scroll capture and reveal
semantics, budgets, custom-control failure detection and representative real mod fixtures. Those are
implementation research tasks; they cannot be resolved by restricting product scope to those fixtures.
Plan revision28 dispatches the detached query tranche and read-only native-contract research.

### Read-scope action guards (revision30 continuation)

The next D2 batch provides a thread-owned `SettingsReadScope`: `Begin()` rejects nesting and
latches the existing scope's first failure; `Refuse(fixedReason)` latches failure and returns the
exception to throw. Catching that exception never makes the read successful. Only the creating
thread can dispose the scope; repeated disposal cannot clear a later scope. This small core owns
no native state, callbacks or settings values. The eventual reader still owns GUI-thread and idle
input admission, state/backend/event cleanup, and discarding failed observations.

Six native action boundaries receive separately documented owned prefixes: WindowStack.Add,
ImmediateWindow and TryRemove(Window,bool), LoadedModManager.WriteModSettings, the clipboard
setter and ImeComposition.Set. A live read scope refuses each call before its body or any later
outer prefix. Outside a read scope native behavior is unchanged. Admission proves the guard is
first in the effective prefix order from C01's safe publication snapshot; priority alone is not
proof. Revalidate publication/owned slot before entry. Compatible other prefixes, transpilers and
postfixes remain installed, including the owned message-placement ImmediateWindow transpiler.
Unknown order/factories or failed owned rollback leave the automatic reader unavailable. Arbitrary
repatching during a callback, arbitrary field writes and foreign I/O remain explicit limits; this
is not a general transaction. No hidden callback registration occurs in this component batch.

The focused native fixture requests guarded attempts through an ordinary button, observes that
the disposable window/settings state remains unchanged, and then edits a real visible control.
A test-only clipboard backstop runs after the production prefix and throws before the OS write:
asserting it was never entered proves ordering without risking the user's clipboard during RED.
This is supporting instrumentation, not evidence that a real clipboard write occurred. Other
value/ref-buffer/scroll/tooltip guards and the complete generic reader remain D2/D3 obligations.

### Native control value batch

Capture original native return/ref values before the first outer prefix and restore them in the
last outer finalizer, including exception paths. Use concrete typed callback classes so Harmony's
state identity is unambiguous; use C01 for each prefix/finalizer receipt and current ordering.
The helper also serves prefix-only silent native calls, avoiding dummy finalizers. Closed int/float
numeric target methods require actual independent invocation/repatch/cleanup proof; generic
callback declaring types are not used. Preserve null strings, float bits, vector array identity and
only the three array entries the native vector control consumes. Short arrays retain native failure.

The earlier proposal to rewrite slider `roundTo` is superseded by suppressing the exact native
CheckPlayDragSliderSound boundary during the read, while restoring original outward slider values.
This avoids argument rewriting and its unrelated arithmetic proof. It also covers the native
sound check for values such as NaN where equality can differ without rounding. Idle neutral input
still prevents direct mouse-down sound paths. Known native sound calls are suppressed; arbitrary
foreign side effects or repatching inside callbacks remain explicit limits. Tooltip registration
captures literal text only and skips native registration; lazy getters are never executed for search.

Root retains exact native bindings/wrappers, target documents, native scenarios and outputs.
The disjoint helper lane owns only SettingsReadControlGuard and its focused Harmony fixture; the
idle lane drafts exact input admission under ignored artifacts before tracked implementation.
No hidden callback registration occurs until all required guards and idle/cleanup admission exist.

Native string wrappers require one additional composition rule: preserve the outermost controlled
string return, while their nested native string calls retain normal intermediate normalization.
The limited Widgets.TextField dereferences the simpler wrapper's normalized result; restoring null
at every nested boundary would introduce an exception. A thread-owned string nesting token unwinds
in the guaranteed finalizer, so later independent calls still preserve null after success/failure.
Do not rewrite input arguments or replace the rendering/validation body to address this case.
Arbitrary foreign reentrant side effects retain the documented nontransactional limitation.

Native verification exposed a public scroll-entry gap: GUI.BeginScrollView(Rect,Vector2,Rect)
returned the clamped position despite an admitted private BeginScrollViewImpl guard. Do not infer
inlining or another compiled-call mechanism from that result alone. Guard all four exact public
scroll entries at their outward boundaries too, and exercise each actual native overload plus
both Widgets scroll modes. The private receipt does not establish those call-edge guarantees.

The next D2 listing contract remains unfinished. Native research identifies17 consumers where
null BoundingRectCached means draw without culling, including two generic numeric consumers whose
effective publication still needs proof. Listing_Standard.IntEntry also clamps values/buffers
after its widget call, and long labels/End mutate listing-owned scroll-cache collections. These
are concrete native preservation obligations before a complete reader; they do not limit which
mods are eligible. The artifact-only idle-input draft has an independent source review, but still
needs runtime compilation and native input workflows, including focused-but-idle resumption.

The selected continuation batches native input admission with Listing_Standard.IntEntry outward
preservation. Input construction is GUI-thread-owned and binds inspected native storage once;
the initializer must catch shape/platform/delegate compilation failure and leave discovery
unavailable. Check reads stored state without initializing input devices or invoking virtual
input callbacks. Require real type/rawType Repaint, all 127 inspected keyboard slots, all five
mouse buttons and their held/press/release/repeat states, exact nonzero movement/scroll components,
and the attached Windows IME bridge including composition begun with empty text. Any nonzero or
nonfinite stored button value conservatively counts as busy. The 127-slot premise is the native
inventory, not a mod/provider limit; changed native/platform shapes remain explicit gaps. Focus
alone is not busy. This helper cannot prove the caller's completed-callback placement or prevent
arbitrary foreign event pumping; those remain integrated-reader obligations.

Listing IntEntry gets an exact public instance guard that retains its original integer and string
buffer through the native minimum clamp, including exception paths. This consumes the same
outward-preservation contract as the existing control pairs, without changing culling semantics.
Two focused English native workflows form one batch: real typing/dragging with idle resumption,
and a native request that would clamp a listed value followed by an ordinary listed-value edit.
Synthetic input-state probes, if used, are supporting diagnostics only. Broader hold/repeat,
extra-button, wheel and genuine IME action delivery remain to be verified through native workflows.

The reused-listing cache continuation isolates the exact two native label-scroll lists around
Label(string,float,TipSignal?) and End. Detached temporary collections let the native callbacks
run, while acquired finalizers restore the original field references; no copied contents are
written back into the original collections. Preserve nullness and bind restoration to the actual
acquired listing instance. Paired callbacks use separate concrete declaring types and existing
first-prefix/last-finalizer admission. Snapshot at most 65,536 aggregate entries per native call,
matching the current GUI-state pilot ceiling; reject before swapping when the budget or field
contract is unavailable. This native UI-cache boundary does not snapshot foreign settings or undo
arbitrary alias writes from unrelated callbacks. Ordinary calls outside the read remain untouched.
An English native workflow scrolls an ordinary long label, requests a shifted hidden label/End,
observes unchanged visible scroll and then scrolls normally again. Success, exception, original
collection identity/content/nullness and repeated/nested calls receive supporting assertions.

Listing-bounds research retains two effective-code prerequisites before unculling is implemented:
the two generic numeric consumer families need publication-wide currentness (a new/changed sibling
can escape a single closed-method receipt), and owned finalizer wrappers introduce exception
regions that the current passive body reader rejects. Reuse the existing publication storage and
bounded original/composed semantic checks; do not replay foreign transpilers, enumerate a fixed
set of numeric types or call an empty publication inventory a semantic certificate. The artifact
prototype under discovery/listing/semantics is uncompiled and unverified. These remain explicit
implementation obligations alongside all 17 native null-to-draw consumer proofs.

The first family inventory component is passive and scoped to a generic method definition on a
concrete nongeneric declaring type. Under the existing provider composition/state monitors, retain
all matching publication keys and immutable payload identities, including an open-definition key
if present. Recheck the whole family; a newly published, removed or replaced member invalidates the
old receipt permanently, while unrelated method publications do not. Currentness alone is never
semantic admission. Bound inspection to 65,536 global publication entries, 256 matching family
members and 4 MiB per consumed payload; exceeding a bound is an explicit unavailable capture,
not a type/provider whitelist.
Do not deserialize or execute foreign callbacks while collecting this inventory. Unknown method-key
shapes, contention and malformed consumed publication state must not be interpreted as an empty
verified family. Family receipt and original/composed body proof remain separate responsibilities.

The retained provider's ordinary unpatch path republishes an entry with a newly serialized payload;
it does not erase the key. Test capture-empty → patch → unpatch → first-currentness-check without
an intermediate observation, so this history premise is exercised rather than inferred from an
already latched failure. This is a contract of the inspected provider path, not a promise to
recover history after arbitrary external mutation of its private dictionary. Detached inventory
policy tests do not stand in for real provider publication/currentness tests.

The nullable checker initially bounds an inspected body to 4,096 operations, at most 4,096 labels
on one operation and 8,192 labels overall. It proves the exact receiver/getter/HasValue/Value/
overlap slice and its draw argument relationship; control-flow checks must include later backedges
and tail blocks that execute before drawing, rather than stopping at the first draw or relying on
lexical instruction order. Preserve unrelated work and compatible paths while refusing unproved
mutations. This original-instruction component still needs derived-rectangle/sequential-draw,
generic operand, effective wrapper and all 17 actual native consumer integration before unculling
can be registered. It does not itself confer a live or complete-reader acceptance claim.

The next original-body proof must retain drawing cardinality and rectangle derivation: LabelDouble
requires both sequential calls of the same label method on the row's left and right halves;
ButtonTextLabeledPct requires its derived label and button rectangles, including the native
button-height adjustment. A proof that merely finds either call is insufficient. Distinguish
these sequences from Label's legitimate long/short alternative. Bind exact producer callables
and the relevant value/alias lifetime while preserving unrelated observers and layout work.
For the two generic numeric consumers, bind the widget operand to the consumer's own generic
parameters or actual arguments; an arbitrary same-signature generic closure is not that operand.
No finite numeric-type list defines eligibility. The original-body proof still needs separate
coherent publication/effective-composition evidence before getter acquisition or native acceptance.

Known scalar rectangle accessors may adjust the base row before its culling decision, as native
RadioButton and CheckboxLabeled do. This does not authorize writes after that decision: retain
the final compared row through the draw and reject escaping addresses or later alias writes.
Derived locals need their own producer-to-draw lifetime, including the declared pre-cull height
setter. Drawing stages describe mandatory sequential calls; alternatives occupy the same stage.
Validate actual reachable call order and cardinality rather than inferring either from source
order. Bind projection parameters to their actual consumer arguments, including the native
`1f - labelPct` complement, and retain exact operation order rather than algebraic equivalence.
The paired projections must consume the same scalar value: protect its lifetime between reads,
including aliases acquired earlier, while permitting changes after the final consumed read.
After the final required drawing stage, inspect later paths for duplicate stages until return
or reentry through the complete native culling gate; an intact gate may begin a fresh sequence.
The native in-place four-float rectangle constructor is permitted only before the relevant row
lifetime, with its exact address/argument/constructor chain intact; post-cull replacement refuses.
Bound a semantic binding to eight drawing sites/stages, 32 scalar accessors and four exact
constructors. A completed drawing sequence does not erase the dependencies of stored projections
reused later: if only one projection is rebuilt after a scalar change, refuse the mixed pair.

Original-body acquisition must also be passive. The inspected Harmony convenience path consults
patch metadata even when asked to apply zero transpilers, so that argument does not establish a
side-effect-free read. Reuse a shape-guarded raw original-body reader only after inspecting its
actual path; bound original IL/locals/branch metadata before invoking it and retain generic
definition context. Test an already-published factory/attribute case with counters reset after
ordinary acquisition, proving inspection itself performs neither callback execution nor patch
acquisition. This body read is separate from publication/effective-wrapper admission.

The initial raw reader is limited to exact runtime methods declared on nongeneric types, including
open generic definitions. It rejects original exception regions and InlineSig operands, and bounds
IL to 65,536 bytes, decoded operations to 4,096, each switch to 256 destinations and locals to 256.
Reuse the existing bounded structural decoder before the provider parser; do not allocate from an
unvalidated switch count. Preserve exact generic token context and branch target identity without
emitting or executing the inspected body. These are inspection bounds, not mod/type eligibility
rules; refusal leaves ordinary page access intact and the capture incomplete.

Native integration must account for all direct getter consumers on the declaring listing class,
including detecting an additional native consumer after a game change. The current 17-method
research inventory cannot silently excuse a newly discovered unproved consumer. This bounded
native prerequisite check is distinct from universal foreign-page eligibility and does not scan
arbitrary foreign callbacks to promise transactionality.

The initial composition certificate combines up to 64 native consumer declarations and at most
1,024 method inspections, including actually published generic family members and repeated
descriptors so they cannot amplify work past the bound. Existing
per-family and global publication limits still apply. Inspect detached coherent snapshots, then
recheck every identity/family and exact owned preservation receipt outside publication callbacks;
the provider monitors are non-reentrant. A lost prerequisite invalidates that certificate forever.
Preservation exceptions bind concrete owned prefix/finalizer pairs and their current guard receipts,
never merely an owner string. Shared effective argument-alias resolution must retain its existing
S138 behavior and avoid constructing attributes. Unknown consumed skip, alias, result or exception
suppression effects refuse this original-body route. Ordinary transpilers need separate actual
published-body proof; this first route does not claim transformed generic/EH/ABI support.

Foreign injected observations must retain consumed scalar/value relationships. Reference-free
value inspection has pilot bounds of 16 nested types and 64 instance fields per type, with at most
1,024 distinct value types and 1,024 inspected instance fields across a capture. Memoize successful
shapes with their height so shared subtrees do not multiply work or bypass the depth bound. These
aggregate limits leave substantial headroom for native rectangle/nullable scalar shapes while
bounding unfamiliar metadata cost. Immutable strings are allowed. Unknown or reference-bearing
payloads are not called generally safe, and
mutable argument arrays refuse admission regardless of their declared object/interface type.
These inspection bounds do not restrict eligible mod pages. A callback with no injected writer
can still have arbitrary global effects; this certificate is not a callback transaction.

The getter guard acquires only after the existing native value/cache wrappers and all consumer
certificates are current. Its exact paired callbacks preserve an acquired null result independently
of later availability changes; outside a read they leave native execution alone. The getter's own
exception effects must also be proved: an earlier foreign finalizer must not be able to suppress
a callback exception and turn a failed hidden read into an accepted capture.

Native inventory bounds are 256 declared methods/constructors/type initializer, 65,536 IL bytes and
4,096 operations per member, and 1 MiB aggregate IL. Reuse bounded token decoding to identify all
getter method references, including function-pointer references, without applying consumer EH
semantics to unrelated members. Unknown direct users refuse the prerequisite; ordinary page
eligibility remains universal. Exact consumed methods then require the original semantic proof.

The next integration composes the existing GUI/input/backend/action/control/listing components
in one synchronous reader. A caller supplies a complete page callback after its visible-draw
delegate and all nested native widget/DrawText/layout calls have returned. The enclosing Window
callback may still be active; this boundary prevents metric reentry, not all nested OnGUI work.
Busy input defers without running it; unavailable prerequisites and
failed cleanup publish no observations. Check preservation receipts both before and after drawing,
discard caught scope failures, and restore all acquired state before returning the detached capture.
This batch registers no automatic startup/chooser callbacks. Its English workflow requests a read
of the real Pick Up And Haul page, observes captured label/help with all original values unchanged,
then deliberately edits and restores its ordinary native checkbox.

Exact native source and provider research narrows the numeric preservation claim: the native
parser implements only int and float, which have existing concrete guards. An unsupported struct
closure still initializes a null string buffer before parsing, and its ToString can have arbitrary
effects. This is a known remaining preservation gap, not proof of safety or a page eligibility
rule. The installed detour backend rejects open generic definitions, so patching that definition
is not an available all-T solution. Keep the gap explicit and full D2/S141 unaccepted; do not add a
whole-program scanner or a type/package whitelist to pretend generic transactionality. The
integrated-reader component workflow proves only the actual native paths exercised there.

The next catalog continuation has a real provider fixture available: installed XML Extensions1.7
emits independent Mod instances from actual SettingsMenuDefs during normal startup. Two owned
fixture defs may share title/package while retaining distinct modIds; do not spoof provider Mod
objects or populate LoadedModManager in a host test. Retain each occurrence of the completed native
cachedModsWithSettings collection and its exact Mod token, including nullable metadata; use public
Content.Name, PackageId and ModMetaData?.AuthorsString. Materialize inside isolation, preserve
ordinary opening and reject stale-generation tokens. The provider's ModSettingsWindow constructor
changes global provider selection and closes settings windows before WindowStack.Add, so blocking
Add does not make its renderer safe to probe. Catalog/native opening evidence is distinct from
hidden provider-page discovery; the latter remains an explicit unresolved integration obligation.
