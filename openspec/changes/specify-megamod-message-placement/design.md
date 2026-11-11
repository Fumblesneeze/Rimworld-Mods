## Context

Owner **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`; **1.7 beta**.
S071 replaces Better Message Placement (Continued), `Mlie.BetterMessagePlacement`, Workshop
2259516339. Manual runtime integration, common controls and the bounded inert preview are
partially implemented with bounded reviewed English native verification; automatic geometry and
the full native gate remain unfinished. Historical
tranche descriptions below retain their original scope. The [research](../../../mods/megamod/docs/refinement/2026-11-04-better-message-placement-research.md)
records documentation-first inspection, selected binary/hash/license and native 1.7.4998 rev1299 evidence.

Source injects additions after loosely matched Vector2 x/y loads in Messages.MessagesDoGUI. Its
helpers include MessagesTopLeftStandard again, use Prefs.ScreenWidth/Height, and infer rows from
colonist-bar scale. An incomplete match can leave a partially changed stream. Source sliders post
ordinary historical messages. These are implementation findings, not desired parity.

Native MessagesDoGUI applies the standard (140,16) and lesson offset, then calls Message.Draw in
newest-first order with 26-unit spacing. Message.CalculateRect, lastDrawRect and its immediate window
carry drawing and interactions together. WindowStack.ImmediateWindow stores the supplied rectangle;
Window.WindowOnGUI rounds it and passes it to GUI.Window. No explicit viewport clamp was found on
that inspected managed path; Unity rendering/input still require live verification.

UIRoot_Play calls base UIRootOnGUI (messages) before MapInterface/WorldInterface draw the bar. A
previous-frame-only rectangle capture can lag at initial display, changed rows or resolution. Native
ColonistBar.Entries prepares dirty layout; DrawLocs/Scale alone do not. Native visibility also depends
on view routing, showColonistBar, UI dimensions, tile picker and screenshot filtering. Empty groups
can still have visible frames. These facts rule out a portrait-count heuristic.

## Goals / Non-Goals

**Goals:** reliable short-message placement, current native geometry, unchanged message behavior,
common scoped offsets, inert preview and atomic compatible integration with explicit fallback.

**Non-Goals:** a notification manager, new archive, wrapped/reordered messages, collision avoidance
for every window, private pawn/Thing index, source-identity emulation, wholesale settings migration,
or unsupported Colony Groups compatibility. No new tab, toggle, debug function or raster asset.

## Decisions

Revision20 current native composition consumes prepared DrawLocs/Size/Scale and entry identities,
then measures the actual native truncated Tiny label and background. Before the real Entries getter,
the native dirty path is requested on first/current viewport/root/bar/map changes. No original
calculator is called speculatively; no draw/collision call measures geometry. The single prepared
owner proves the entry rectangle producer and its actual native consumers. Separate passive label
projection guards protect the required current effective code, retaining manual placement when
automatic admission fails. Receipt lifetime ends at the outer GUI exit; validation never recaches.

The connected first native slice supports current single-map portraits/labels. Visible weapons,
frames, selected overlays, any positive native mood-edge expansion, world selection and active reordering explicitly
remain unavailable for automatic clearance. The pure policy already describes these extents, but
that is not downstream native admission or acceptance. The initial mood predicate declines positive
expansion even with its preference off; it does not reserve hidden content or claim that this
conservative unavailable case completes the final visibility contract. Native effective selection
and positive-expansion gates, actual portrait fill/clipping/texture consumers, and icon sizing/cursor
helpers participate in the same automatic admission group. Full support remains required. Selection
will use only the actual remaining0.07-second jump for a currently selected overlay, contracting
within the same invocation; settled/unselected content never reserves the full animation envelope.
The authored native UI-scale/selection scenario proves only the bounded current slice once root
executes and personally inspects it on the reviewed build.

### 1. Translate the native message stack once

Use one common translation at the point where native message coordinates, including the active
lesson contribution, feed the draw loop. Retain native Message.Draw and all posting/lifetime/input
paths. Do not replace the loop with a second renderer or move only the text. Required semantics are
the consumed coordinates, their complete lesson contribution, one coherent translation and the
unchanged native relative arrangement. Exact implementation targets remain BMP-Q1, not approved patches.

Measure the native stack's padded rectangles with the correct font/UI scale without mutating
lastDrawRect during speculative sizing. Pass only final positions into the native drawing path so
backgrounds, hitboxes, hover and colonist-bar collision/fade queries agree. Native cached text sizing
can be reused where admitted; any needed extra measurements are bounded by the live message list.
Preserve/restore GUI font/color/matrix state. Do not add a separate message collection or lifetime cache.

### 2. Resolve current visible bar geometry directly

Read native prepared layout at the actual renderer handoff, after its preparation reads and before
its first draw/input operation. Bound visible portrait, label-background, weapon,
group-frame and selection/mood-outline extensions, rather than using the 0.58/0.42 allowed-row
thresholds. GenMapUI's 100-unit label container is not its visible label height; inspected backgrounds
are 12/16 units depending on tiny-text preference. Derive only consumed visible geometry and verify it.

At zero offsets the top of the first padded message rectangle is at least **4 logical UI units** below
the lowest visible bar content, unless viewport fitting forces overlap. Four is a provisional named
layout constant, recorded in the spec; the existing vertical setting provides adjustment. Keep the
native baseline if it is already lower. Do not add native origin or lesson offsets again. No visible
bar means no bar reserve, including main menu and genuinely hidden/suppressed bar states; world-view
bar content is handled by the same visibility contract.

S001 contributes its **effective visible presentation bounds** through a direct owned call: completely
hidden entries contribute nothing; reduced entries contribute only their visible name/frame/portrait
parts; labels/health strips, indicators, outlines and weapons contribute their actual rendered extent,
including configured weapon scale and vertical offset. Do not add hidden native portrait rectangles
back into that result. Separately visible native group frames still count. Consume the presentation
prepared for that draw rather than independently reimplementing S001's filtering or refreshing its
gameplay state on a different schedule. This preserves S001's configured update timing and avoids
duplicated decisions. No Harmony patch against megamod. A GUI-pass-local geometry value is sufficient;
invalidate/recompute when the native view/layout or composed presentation changes. Do not introduce
a second colonist census, map scan, persistent per-port cache or universal screen-layout service.
The shared Thing index has no role here.

Plan revision 12 replaces the proposed private-calculator production/out-field-address proof with
**prepared state and actual renderer consumption**. Native `ColonistBarOnGUI` constructs entry
rectangles from `cachedDrawLocs` and `Size` (which uses `cachedScale`); group frames consume the same
native locations/size. Clearance must use those same coherent values, identities and effective
visible extensions in this GUI invocation. A compatible provider may retain unchanged geometry,
skip the original calculator, use another calculator or assign its result through different storage.
None is an incompatibility by itself. Numerical change does not establish freshness; numerical
equality does not invalidate it. Do not rebuild the private layout algorithm or require its execution.

The selected next timing boundary is inside the actual `ColonistBarOnGUI` path after its `Entries`
and `ShowGroupFrames` preparation reads, before its first entry loop draw/input. Capture the actual
bar receiver and entries value plus the native location/scale/size values that the effective renderer
will consume. On first context or changed logical viewport/root/bar/admission, request ordinary
native invalidation before these reads, so the first changed display prepares its current state.
Normal map/group membership invalidation remains native. Never call the complete renderer twice,
force a numeric layout difference, or measure from a previous-frame capture.

Message collection completes earlier but must retain its queued native surfaces until this barrier.
The existing completion-at-`MessagesDoGUI` implementation is not sufficient: base UIRoot invokes
game-component/world callbacks after collection, and the play root subsequently invokes world/map
UI before windows. These callbacks may change view or bar state. At the prepared-state barrier,
validate the same GUI/root/window/settings context and message bindings, capture visible geometry,
and perform the one whole-stack translation before bar input/drawing or message collision consumes it.
Actual bar group frames can draw before the first collision query, so that query is not the selected
commit point. `WindowStackOnGUI` entry, before `ExtraOnGUI`, shadows and `WindowOnGUI`, is the fallback
barrier only when current routing/visibility proves no bar contribution, or automatic clearance is
explicitly unavailable. Lack of an observed bar callback alone does not prove absence.

Retain a bounded GUI-local snapshot of consumed references and values, including entry order/group,
locations, scale/size, visibility and native/S001 visible-extension inputs. Revalidation must catch
in-place mutations, not just list identity/count changes. Admit the effective relationships from
these values to native draw/input arguments, and validate stability through their actual consumption.
There must be no second bar invocation, late layout refresh, early message consumer or changed
draw-argument path silently reusing the snapshot. Before commit, uncertainty declines that contribution;
after native effects have occurred, preserve them and prohibit further owned movement, consistent
with the uncertain-display-order scenario. Such a run is not successful clearance evidence. Do not
rewind clicks/drawing or claim a whole-event transaction. Bound snapshots to this invocation; they
are not a private layout/pawn cache. The timing and stability adapter remains unimplemented.

### 3. Manual adjustments and viewport fitting have a fixed order

Treat horizontal/vertical percentages as a common scoped position pair, default **0/0**, each finite
and within **−100%..+100%**. Convert using current logical UI.screenWidth/Height, not stored physical
Prefs resolution. Apply once after the automatic anchor, allowing deliberate negative/manual overlap.

Then constrain one translation for the whole stack: if its native bounding extent fits on an axis,
clamp that extent into the viewport on that axis. If too wide, align its left edge with the viewport;
if too tall, constrain the newest message vertically when it fits, otherwise align its top edge.
Keep the other messages' original relative positions, including native multiline limitations. Never
clamp individual older messages onto each other. Overflow can clip; do not rewrite text, change
spacing, remove/archive messages early or manufacture a scrollable replacement window. Viewport
fitting can override bar clearance. This is deterministic and smaller than a collision packer.

Valid stored offsets are not rewritten by fitting, screen changes or hidden-bar fallback. Invalid
loaded values fall back for the complete pair to 0/0 in their existing scope with an explanation;
do not partially accept one field or modify global settings as a side effect of loading a save.
UI edits validate both values atomically; reject invalid edits without changing the previous pair.

### 4. Use common settings and an inert preview

One **Interface / Messages / Position offset** compound row shows the two controls in each scope
with Use global. The pair inherits together, retaining dormant overrides. Save reset sets the pair
to 0/0 only when its override editor is enabled, without changing Use global. Global reset only
resets the global pair. Main-menu callbacks cannot mutate Use global, a save pair or a save reset.
Changes affect the next message draw; no reload, repost or timer restart.

Show a labeled, scaled viewport preview inside settings with a sample stack and current bar reserve.
It uses effective current-save values during play and global values at the main menu, with that
scope named. Changing a global value under a save override therefore need not move the effective
preview. The preview uses the same placement policy and does not enter Messages, Archive, look-target
selection or sound paths. Refresh on independent coordinate changes, reset, inheritance, view/scale
and save changes; compare the pair, never x+y. Dispose transient preview state when settings close.
This preview is not native behavior acceptance and does not promise avoidance of the settings window.

Translate row labels, percentage/range help, preview scope, intentional overlap/overflow, reset,
fallback and provider explanations in all seven baseline languages. No source VersionFromManifest
footer dependency; the megamod's ordinary version presentation remains authoritative.

### 5. Admit supported contributions and handle unavailable features

Before coding, create and index each actual target spec with stable ID, minimal consumed semantics,
exact incoming/effective assumptions, cardinality, native fallback, tests and live workflow. Candidate
boundaries below are design responsibilities, not assertions that these exact patches exist:

| Boundary | Required evidence | Failure result |
| --- | --- | --- |
| Native position contribution | Coordinates after native lesson handling reach the normal draw loop exactly as consumed; translation preserves message geometry/actions | Entire owned position contribution inactive; native/source placement remains |
| Native bar geometry | Current layout preparation, actual draw visibility and consumed bound calculations agree with the supported bar | Omit automatic bar reserve; admitted native anchor plus manual offsets/fitting still work, with status explanation |
| Optional foreign bar adapter | Canonical active package plus exact callable/layout/visibility shape, effective semantics and proven consumed contract | Adapter inactive; same explained no-automatic-clearance fallback |
| Common settings preview | Shares the admitted effective policy; no mutation of message/history/targets | Show unavailable state rather than pretend the real game uses an unsupported placement |

Every actual transpiler validates its actual incoming stream on every execution and returns that
stream unchanged on false/unknown/ambiguous predicates. Materialize/validate before rewriting; no
partial X-only/Y-only result. Original IL alone cannot prove semantics changed by another transpiler.
Account for relevant skip-original prefixes and later repatching without speculatively reexecuting
foreign transpilers. Installation/validation exceptions roll back only newly added owned patches in
the declared group. Do not reject whole hashes/versions or merely another Harmony owner. One bounded
startup warning per skipped enabled patch names package/patch/spec, target, reason, lost behavior and
fallback. Runtime invalidation transitions likewise remain bounded, not per-GUI log spam.

When **Mlie.BetterMessagePlacement** is active, emit a bounded startup warning and attempt best-effort
suppression through the ordinary placement-feature off path, with the same behavior, timing and
preview availability as manually off. Preserve stored preferences and the original; an inactive
installed source does not trigger this rule. Do not promise sole-provider placement, rollback of
earlier effects or coexistence compatibility. Source metadata marks
**DerekBickley.LTOColonyGroupsFinal** incompatible; do not copy that whole-product flag. That known
replacement needs a proved geometry adapter before automatic clearance is claimed. Other foreign
bar changes are assessed by consumed semantics; package presence alone neither grants support nor
forbids an otherwise compatible patch. Dynamic Message Positioning is a reference, not selected scope.

Original API/consumer audits, facades and drop-in/coexistence work are outside scope. Preserve native
message interfaces and independently verify deliberately consumed optional bar-provider contracts.
No XML contribution is selected. Harmony will be required; optional bar/source mods and DLC are not
requirements. No source binary/helper, other repository mod or Gateway reference in the product.

## Risks / Trade-offs

### Implementation tranches — 2026-11-07

#### Current native call-site investigation

Fresh 1.7.5000 rev1275 source inspection (Assembly-CSharp SHA256
`D55AA8E47FA3C5463CA85B0ECCD51F195B4BBCD04F56DA3AF21C2F6688A81254`) found a smaller boundary than
reconstructing the native loop. `Message.Draw` calculates its padded rectangle, captures that rectangle
and this Message in its Action, and registers one ImmediateWindow. Registration only stores the
window/callback on repaint; `UIRoot_Play` renders windows after messages and the native bar. A narrowly
guarded transparent registration observer can therefore collect the actual native positions in call
order without copying baseline, lesson, spacing, loop or font-measurement rules.

The pending native frame adapter will update windowRect, Message.lastDrawRect and the callback's
captured rectangle together before native WindowStackOnGUI. It must identify the exact existing
message window/callback on non-repaint input passes, never fabricate a window for a not-yet-rendered
message, and retain all native callback objects and calls. Scope cleanup on exceptions and no partial
group movement remain mandatory. No speculative CalculateRect calls or second draw pass are needed.
`MSG-NATIVE-DRAW` currently owns only the tested registration call-site observer; frame/timing and
prepared-bar admission remain separate unfinished work. The observer is not centrally registered.

The next feature-local helper, `MessageRegistrationGuard`, is a read-only incoming-IL observer for
native ImmediateWindow and AddNewImmediateWindow. It requires rectangle/callback argument storage on
the same receiver and unchanged forwarding to the new-window path. Straight-line aliases and
unrelated compatible patches are supported; consumed argument mutation, ambiguous aliases and known
direct drawing-callback invocation lose admission. Every repatch changes its opaque generation,
which a future pass must capture and recheck before writing. Detailed limits are in
`mods/megamod/docs/patch-targets/MSG-NATIVE-FRAME.md`.

This helper is only a necessary dataflow prerequisite, not timing authority. Caller ordering and
actual runtime binding/revalidation remain separate unfinished gates. Opaque helper or foreign
prefix behavior cannot be inferred from the absence of a direct Invoke instruction.

`MessageConsumptionPatch` now provides a separate active-pass veto at entry to the structurally
resolved native callback and exact message collision query. It binds to the existing pass Invalidate
action, owns two read-only prefixes and preserves native arguments/results/callbacks/exceptions.
Both prefixes must remain first in effective prefix order; compatible foreign prefixes are allowed,
while unsupported ordering/factories or owned observation failures lose authority. Composition must
check availability again before committing, even when a foreign finalizer suppressed an earlier
consumer-prefix exception. This observer has no private pass state/cache and does not establish
caller order by itself. Direct composition with the new outer coordinator retains observations before
message collection. Keep the whole group unregistered until native composition and adapter gates are
ready. No common UI/bar provider is part of this tranche.

The next host-tested timing slice supplies a feature-local outer GUI invocation envelope around the exact native
UIRoot_Entry/UIRoot_Play overrides, directly composed with MessageGuiPass and the two existing consumer
observers. It preserves early-consumer history before MessagesDoGUI begins, retains ordinary base
UIRoot forwarding without nested ownership, and reject missing/reentrant/stale contexts. Use one
opaque token per actual invocation, not Time.frameCount or reusable Event.current object identity.
The exact target predicates and bounded failure authority are recorded in MSG-NATIVE-FRAME.
`MessageGuiEvent` owns opaque Envelope/Collection tokens and directly calls the existing pass;
`MessageGuiEventPatch` owns four receipts on the two overrides, leaving the base unpatched. Its
injected current-context predicate and per-commit native binding/generation checks still need actual
runtime composition. An outer finalizer occurs after bar/window drawing and cannot act as retroactive
commit authority. Late exceptions retain native handling; repeated/nested calls after a completed
pass preserve earlier effects and prohibit additional owned commits in that invocation. Compatible
foreign callers may forward to the actual message original while replacing the outer original.
Native writes must be direct field updates without reentrant native consumption during the batch.
Actual message-scope original execution, native binding/generations and remaining runtime/native
verification still govern placement. This bounded observation does not prove arbitrary consumer
absence, whole-caller bodies or retroactive all-event atomicity. The helpers remain unregistered.

`MessagePlacementBatch` applies the pure translation to pass-local observed surfaces, preserves each
size and native relative arrangement, validates before writing, and restores exact prior native state
when a write fails. It owns neither messages nor a persistent cache. Actual Unity/window behavior
remains subject to the orchestrator's native gate.

#### Pure placement and common setting

`Features/MessagePlacement/Core/MessagePlacement.cs` accepts newest-first already-padded rectangles,
current logical viewport, nullable visible-bar bottom and an immutable percentage pair. It returns
one translation; no baseline/lesson reconstruction, game object, cache, list ownership or rendering.
Input validation rejects unusable geometry without publishing a translation. A native caller must
preserve native positioning on false; it must also distinguish unavailable clearance from hidden bar
state in its separately owned status. Current native measurement/admission and incoming-stream proof
remain required before that adapter exists.

`MessagePlacementSettings.PositionOffset` registers one common definition, `messages.positionOffset`,
GlobalAndSave/NextDisplay, category `interface.messages`, order0, built-in0/0. The strict JSON codec
retains both percentage fields together; common validation and stored-value handling reject the pair
or expose the whole-pair fallback without mutating another scope. Common store APIs own inheritance,
snapshots, dormant overrides and callback restrictions. No private persistence/reset mechanism.

The setting is not centrally registered and has no UI or language catalog yet. Focused host tests
prove calculations and genuine common-store composition only. Reserved native scenarios and adapter
input contracts are in `mods/megamod/Source/Features/MessagePlacement/`; no gameplay task is accepted.

#### Native composition in progress

**Revision 12 status:** the following describes the existing unregistered implementation, not the
selected automatic-clearance timing. Its early frame/bar capture and commit at message-scope
completion must be split into collection completion and the later prepared-bar/window barrier
described in decision 2. Existing tests that require immediate completion/cleanup or bar capture at
message begin are historical evidence for that earlier design and must be replaced in the affected
slice. Keep their meaningful duplicate/reentrancy, native binding, rollback, early-consumer and
exception assertions. The actual runtime callback seam will change through a root-coordinated lease.

`MessagePlacementBridge` now composes actual acquired host-fixture helpers, including deferred
callback admission and atomic final publication. `MessagePlacementRuntime` supplies exact native
member resolution, current GUI/root/window identity, common settings/save receipts, live window
enumeration and unchanged once-only native registration forwarding. It remains unregistered;
constructor/Unity behavior is not proved by host fixtures. All captured surfaces are revalidated
before any field write. A reproduced consumer-during-final-validation case required retaining pending
authority through validation and rechecking it immediately before writes. Its actual RED/GREEN and
the final authorization-callback case are retained locally; see MSG-NATIVE-FRAME. Independent review
and the native player workflow remain required before registration/acceptance.

Bar dependency is deliberately narrow: MessageBarBoundsReceipt Visible/Absent/Unavailable plus
read-only IsCurrent, captured once by an injected actual-root provider at message collection. Missing
provider means unfinished integration, not a replacement of requested automatic clearance. The
provider must eventually include prepared native and S001 effective visible bounds; root coordinates
that disjoint work with the common offset editor/preview. No universal bar registry/cache is needed.

The unregistered `MessageBarEntriesGuard` and `MessageBarExecutionPatch` were reviewed historical
experiments, retired with their standalone fixtures on2026-11-08 after verifying no selected caller.
Their original-recache/calculator requirements, exact call counts, run-original checks,
finder witness and output-value observations are not prerequisites for the revised renderer boundary.
Historical source/hash snapshots remain in ignored artifacts; current prepared/event/bridge tests
remain the relevant behavior checks.
The small preparation helper's dimension/admission identity and reentrancy policy may be reused,
but its synchronous prepare-and-return API must fit the native handoff; it does not independently
prove current render geometry. No new field-address or private-production gate is planned without
a demonstrated consumed-state ambiguity.

C01 revision 12 also applies here: ordinary last-transpiler order alone does not certify Harmony 3
inner/persistence effects. Admission for the actual renderer operations and inserted observations
must account for relevant later transformations, preserve compatible unrelated effects, and reject
only unproved consumed relationships with bounded fallback. No blanket foreign-owner ban.

The shared C01 reader exports immutable Prefixes/Finalizers registration metadata from the
existing publication snapshot, with copied ordering constraints and fresh Patch projections only
for provider sorting and owned-slot receipts. The public GetPatchInfo path can construct foreign
parameter attributes during inspection. MessageGuiEventPatch and MessageGuiScopePatch now use one
snapshot per decision, including their OwnedHarmonyPatch precheck/removal readers.
MessageConsumptionPatch now does the same for each of its two native targets; its existing
first-prefix requirement rejects prefix factories before sorting, and its receipt readers project
only acquired-owner prefixes. The migration passed56 affected host checks and independent review.
Reject factories only where their methods would need
sorting: Scope checks prefix membership without sorting, so prefix factories remain admissible;
finalizer factories prevent its exception-order proof. Cleanup of an already-acquired envelope is
attempted independently in finally even when later metadata inspection throws. Completion and
reporting failures cannot replace the native exception or produce repeated reports. This export does not prove outer lifecycle
semantics or body effects and does not alter the existing provider reader. Draw body admission is now
independently reviewed at35 affected checks: actual recomposition revokes prior authority, the final
publication must retain consumed operations, and unreadable metadata preserves the incoming native
stream instead of blocking foreign patch removal. Registration's two-target publication/effect
admission is also independently CLEAR at39 affected checks; each decision checks one snapshot pair,
and both observers preserve the incoming streams on failure. Native geometry and composition remain
open. Retired Entries/Execution experiments are excluded from migration.

- **Bar draws after messages** → prepare current native layout; verify first frame/resize/group change,
  not merely steady-state screenshots or a previous-frame rectangle.
- **Visible extensions differ from portrait bounds** → direct S001 effective-presentation composition,
  including hidden/reduced entries and adjustable weapons, plus exact label/weapon/frame
  evidence and native screenshots; unsupported providers lose only their unproved clearance contribution.
- **Manual placement or small viewport overlaps the bar** → explicit priority/range/help and native hitboxes;
  no claim of universal non-overlap or all-message visibility.
- **Native multiline/window behavior** → retain spacing and test actual clipping/input on reviewed build;
  static IL does not prove Unity's rendered result.
- **Active original or unavailable integration** → retain preferences, name the reason/fallback and
  follow ordinary feature-off preview behavior; original handling is best effort, not coexistence safety.
- **Cost of geometry/text measurement** → use native prepared data and bounded live-list work, measure
  combined S001 costs before optimization claims; no private per-feature world scan.

## Migration Plan

Add the admitted implementation only after focused RED/GREEN and current target documents. New/missing
settings use global; source config remains untouched. No import, save conversion or source-class alias
is selected. Any later migration of our own settings requires backup, atomic commit, failure restoration and postcondition
verification in its own contract. Disabling/removing owned placement leaves ordinary message data and
native actions intact; do not clear messages, archive records or user preferences on admission failure.

## Open Questions

### 2026-11-11 inert preview continuation

The common settings page now has one stateless sample diagram beneath the compound offset row.
`MessagePlacementPreview.Capture` reads the common effective enable/pair and current save identity;
it returns detached sample rectangles after the same placement calculation. It owns no persistence,
Message, Archive, sound, target or callback lifetime. Each draw recalculates using current logical
viewport and native standard/lesson anchor, so coordinate sums, settings revisions and old save
identities cannot suppress refresh. The UI restores state through the existing section renderer.
The sample rectangles are labeled illustrations, not speculative measurements of live notifications.

The current native runtime explicitly has no admitted automatic geometry. The preview therefore
shows that status and no invented bar reserve. Disabled or unadmitted placement shows only its
explanation and viewport, retaining preferences. The future geometry integration must supply actual
current reserve before the whole preview task is accepted. Root passes common SettingsStore and a
read-only runtime availability callback during registration; there is no second settings store or
new shared API. Native scenario `megamod.messages.native-preview-reset` opens the real page, observes
the diagram, clicks the real global Reset and observes the changed diagram, then closes. Empty live
messages and unchanged native archive identities support its inertness claim; screenshots still
require root's personal inspection. No game run or full S071 acceptance is implied by authoring it.

### 2026-11-11 manual placement integration batch

The runtime now composes the existing bridge with native ColonistBar preparation and WindowStack
entry. Automatic geometry remains explicitly unavailable. At the bar handoff, manual offsets and
viewport fitting consume no bar fields or estimated bounds; the prepared receiver/root/game/logical
viewport and all existing message/window/settings receipts must remain current. The native consumers
HandleClicks, DrawColonist, DrawGroupFrame and HandleGroupFrameClicks must each occur once and be
dominated by the inserted handoff in the actual incoming branch graph. Loops and switch branches are
supported; unknown exception transfers decline this bounded adapter without changing native code.
C01 protects the handoff and selected consumer operations against relevant later inner effects.

Window entry may supply explicit Unavailable for manual positioning when no prepared call occurred;
only Entry/no-current-Game supplies Absent. Earlier observed message consumption vetoes movement.
Neither absence of a callback nor this fallback certifies hidden-bar geometry. Runtime remains
subject to central reviewed admission and the fresh native gate.

`messages.enabled` is a common GlobalAndSave/NextDisplay Boolean, default true. Its false effective
value suppresses collection/movement without replacing offsets, native registrations or settings.
The provider policy maps only the exact active original to this definition and emits one bounded
startup warning through root composition. The seven catalogs include the enable control and the
existing automatic-clearance status is visible above the offset editor. Preview remains unfinished.

The native manual tracer prepares one disposable pinned message and a nonzero offset pair, then
uses real settings checkbox and Reset controls to observe disable/reenable/reset movement. It does
not accept message creation, automatic geometry, target/quest actions, dismissal or complete S071.
No balance/progression/resource/research changes are introduced. Existing 0/0, ±100% and fitting
priorities remain unchanged; the Boolean default is stated above rather than inferred by the UI.

**BMP-Q1–4** in the [progress ledger](../../../mods/megamod/docs/refinement/specification-progress.md)
retain current target/geometry proof, proposed layout/preview choices, used bar-integration boundaries and
tests/localization/cost/native acceptance. They do not ask the user to re-explain upstream behavior.

Semantic pass: only message readability/navigation changes. Native events, warning eligibility,
quest outcomes, expiry/sound/archive policy and game progression remain native. Preview creates no
game event. No cost/reward, research, material, hauling or work-priority change is proposed.
