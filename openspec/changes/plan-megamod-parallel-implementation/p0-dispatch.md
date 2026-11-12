# P0 dispatch — contract revision 5, 2026-11-07

Owner: Make Rimworld Great Again (`fumblesneeze.megamod`). Implementation starts from checkpoint
`c0a600e`; full139-entry goal remains active. Governing plan revision17 and all individual owning
specifications remain authoritative. This is an active assignment record, not acceptance evidence.

## Current environment and central ownership

Inspected game: **1.7.5000 rev1275**, Steam build **26468007**, Assembly-CSharp SHA256
`D55AA8E47FA3C5463CA85B0ECCD51F195B4BBCD04F56DA3AF21C2F6688A81254`.
The original package-validation attempt failed because the private profile still pinned 1.7.4995.
The profile now records the inspected development target; no release or runtime compatibility claim
follows from updating it. Source research against older binaries must be refreshed for consumed seams.

The orchestrator owns bootstrap, product/test project files, solution registration, package/profile,
central language/target indexes, this manifest and governing task states. It installs builds and runs
all final native acceptance. No implementer commits, installs, launches acceptance games or edits
another lease. The first test suite is `Megamod.Unit`: source-linked `Source/**/Core/**/*.cs`, .NET 10,
no game/Harmony references. The separately owned [host dispatch change](../register-megamod-host-tests/)
registers it in existing typed operations. Tests use exact filters and serialized test-run leases to
avoid shared build-output races; compile-coherent unrelated edits may proceed between runs.
The first actual patch consumer also gets `Megamod.Harmony`, an explicit net10 scoped-host patch
environment using the inspected1.7 provider. It source-links owned patch bridges and their host-safe
core dependencies, never Verse adapters or a loaded mod. Its separate output does not share the
Unit test-run lease.

## Assignments and leases

| Chunk / agent | Owned features and paths | Initial behavior and exports |
|---|---|---|
| P0-A / megamod_p0_admission | S035/S094; `Source/Patching/**`, `Source/Features/Diagnostics/**`, matching `tests/Megamod.Unit/Patching/**` and `Features/Diagnostics/**`; later exact Harmony folders when registered; feature-local diagnostics catalogs and target/owning feature docs | Active-provider suppression → atomic admission coordination → early diagnostics. Exports admission core, early hooks and activation after C02 early globals. |
| P0-B / megamod_p0_settings | S034/S047/S071 and C02; `Source/Settings/**`, `Source/Features/{ReadableNumbers,ModSettingsNavigation,MessagePlacement}/**`, matching Unit settings/feature folders, feature-local Settings/ReadableNumbers/ModSettingsNavigation/MessagePlacement catalogs and owning docs | Global edit/read without save → inheritance/dormant overrides/validation → typed snapshots and runtime adapters → common UI and feature behavior. Exports settings-core before renderer. |
| P0-C / megamod_p0_content | S020/S029/S038; `Source/Features/{ContentSelection,DrugPolicyMaintenance}/**`, matching Unit folders, feature-local ContentSelection/DrugPolicyMaintenance catalogs and owning docs | Drug-policy stale/duplicate cleanup preserving winner identity and choices → normalization/defaults/native ordering → rollbackable publication → full content/selector implementation. Exports catalog, drug-maintenance, applied-policy. |

All pure code goes in the leased `Core` subfolders; game/Harmony adapters stay outside them. Each
agent reads referenced documentation and source/decompilation, independently implements our owned
contracts, and retains source facts/deviations. No copied upstream implementations or self-patches.
Individual feature tasks carry the complete requirement inventory; the initial tests below are
tracer slices and do not reduce the assigned scope. Composite acceptance tasks remain unchecked.

P0-B continuation: `megamod_p0_messages` holds only the S071 MessagePlacement source/core/test
subtree and owning feature documents for pure placement and atomic X/Y preference slices. Its
`Megamod.Unit` output lease is separate from the Gateway input and XML fixture tooling outputs.
Existing settings/readable-number sources and central registration remain frozen/root-owned. This
subdivision preserves the existing feature allocation and does not authorize a new renderer or
private settings store. The original B handoff remains scoped to its reviewed milestones.

## Shared contracts agreed before implementation

**C02:** `Megamod.Settings` typed definitions carry stable ID, built-in value, validation/codec,
category/order, scope and application timing. The store registers definitions, reads/edits globals,
resolves inherited values, preserves dormant overrides and exports/loads typed encoded snapshots.
No arbitrary runtime type activation. Unknown registered-later IDs survive dormant round trips.
Save writes require the current binding token (save identity plus activation generation), rejecting
main-menu and stale callbacks including reloads of the same save. Detached requested-value reads
are explicit and do not activate a previous save. Verse ModSettings/GameComponent adapters remain
separate from the host-safe core. C03 owns requested-versus-applied policy, not C02.

Mutable settings storage is initialized in the serialized bootstrap phase; subsequent native
persistence/UI access is game-thread-owned. Do not assume the asynchronous loader's constructor
thread is the later UI thread. Consumers such
as loader diagnostics that run on worker threads receive immutable derived option snapshots,
published atomically after a successful settings change. Initial global options publish before
early hooks are installed. A snapshot is not a second persistence authority, and worker hooks must
not enumerate or read the mutable store concurrently with edits.

Native load hooks are worker callbacks in inspected 1.7. They publish immutable game/generation
requests and revoke stale save tokens without accessing mutable dictionaries or firing store events.
An admitted main-thread LongEventsUpdate postfix processes detachment and activates a finalized
snapshot only for the exact current game/generation, Playing state and completed long events.
UI entry points reject stale tokens. Component serialization uses a published immutable save
snapshot; worker content resolution uses published globals plus detached loaded-save data.
The early content-policy load receipt is separate from later settings-UI activation: Scribe/map
finalization happens before GameComponent.FinalizeInit. ExecuteWhenFinished is not used as a
general dispatcher because it can immediately execute on the caller. B owns the runtime/core and
Settings/Harmony pump; root integrates both required lifecycle candidates atomically.

**C01:** concrete registrations identify patch/spec/target, activation phase, required capability,
validator result, unavailable behavior and fallback. Establish cleanup ownership before invoking
installation, so an install that mutates then throws can also be rolled back. A failed atomic group
rolls back the failed candidate and prior newly installed members in reverse order, preserving all
preexisting patches (including preexisting owned members). Disabled
candidates are silent; failed enabled predicates/installations warn once per patch. Canonical active
provider policy suppresses overlap only. Other patch owners alone never reject a target. No speculative
general plugin/IL framework. S094 is the first actual early-hook consumer. Constructor admission is
before XML conversion; existing constructors are not rerun during hot reload, so scopes clean up per
invocation. Effective-stream semantics still require actual guards and later Harmony/live proof.

**C03:** content keys use canonical source package plus exact Def type/name; native catalog identity
is distinct from availability policy. Pure drug reconciliation takes opaque native identity handles,
retains the first valid native row and applicable choices, and delegates native ordering/ties rather
than inventing a new sort. Snapshots stage changes; applied drug policy and all affected native rows
publish atomically or restore. Early constructor preset safety uses finalized catalog facts, not a
previous save's policy. C02 loaded save data and current native load boundaries must be available
before applied save policy publication and subsequent generation/actions. Active original provider
or failed required maintenance leaves the owned drug exclusion group unavailable.

## Tests, scenarios and handoffs

First exact host test IDs:

- `Megamod.Unit.Settings.SettingsStoreTests.MainMenuReadsTheEditedGlobalWithoutCreatingASave`
- `Megamod.Unit.Features.Diagnostics.DiagnosticProviderPolicyTests.ActiveContinuationSuppressesOnlyOwnedDefinitionContext`
- `Megamod.Unit.DrugPolicyMaintenance.DrugPolicyReconcilerTests.CleanupRetainsFirstEffectiveRowAndItsChoices`

Each has a captured failing behavior before its minimum implementation. Durable evidence lives in
canonical `artifacts/TestResults/` plus agent-local TDD ledgers; root records part-level evidence at
`artifacts/Implementation/megamod-p0/`. Subsequent test IDs extend those ledgers one behavior at a time.

Native scenario families (individual registrations and current tracer results below): `megamod.settings.*` for native
main-menu/save controls and persistence; `megamod.diagnostics.*` for capture/filter/copy/context and
early-load diagnostics; `megamod.content.*` for selectors and pending/applied exclusions;
`megamod.drug-policy.*` for actual native policy creation/edit/load and atomic exclusion interaction.
Implementers must author and register concrete stable IDs before handoff; root inspects real outcomes
only after all selected P0 chunks are ready. Source/provider combinations are exact and per feature;
the original-loaded suppression and installed-only control remain required alongside source-absent
owned behavior. Optional preset/provider claims require separate admitted combinations.

The P0 gate and later composition obligations remain those in [roadmap](roadmap.md). No partial core
test, compilation, package check or source inspection closes those gameplay requirements. Missing
scenario tooling is separately owned and reported; no fabricated runtime fixture in the Unit suite.

The first executable registration is `megamod.settings.native-page-lifecycle`, authored under
`tests/Megamod.EndToEndTests/Settings/` and compiled but unrun. It covers native page open/close and
save/reload identity only, not editing. Current minimized checkbox/text and main-menu navigation
gaps are owned by [the Gateway input change](../add-gateway-minimized-window-input/). Its live tooling
acceptance is separate from final P0 product acceptance. No direct Store mutation replaces a control.
Revision3 adds that concrete tooling prerequisite without reallocating features or weakening any
scenario. Inspected dispatch timing and reusable window labels require a native pre-dispatch hook,
exact instance leases and frame-driven operation completion, not ordinary Update-time input.

## Implementation status

Revision5 distinguishes initial finalized-Defs quiescence from successful hot reload. The initial
catalog can be composed from the admitted main-thread pump after common settings reconciliation,
PlayDataLoader.Loaded and no pending/current long event. Native HotReloadDefs leaves Loaded true
and can finish after swallowed errors; that predicate cannot authorize catalog recovery. P0-C first
adds explicit native reload/clear invalidation, retaining immutable applied stable-key decisions and
requested choices. Successful same-instance catalog/UI reconciliation needs a separate proven Def
generation receipt; it remains open and must not apply pending save/load-only choices. Root retains
central composition. No duplicate runtime/setting/patch registration is permitted on idle transitions.

The Gateway's first checkbox run failed at capture before input. Its reviewed ID0 eligibility and
failure-inventory corrections were followed by a successful basic enabled/disabled checkbox run.
Root inspected the exact three causal captures. A subsequent reviewed checkbox run also includes
native console inspection. The launcher failure-summary correction has been verified by an actual
failed run, and the post-layout target correction passed its native replacement workflow. Exact selected
dependency preflight and disposable XML fixture staging are separately owned tooling prerequisites.
These results close no P0 gameplay gate.

The current 1.7.5000 private product build includes the common settings renderer/persistence and
first startup-registered guarded save observer, with Harmony as a required external dependency.
Its checked package inventory and changing intermediate build identities are retained in
`artifacts/Implementation/megamod-p0/part-ledger.md`; no build is a final P0 candidate yet.
The prior platform-inventory blocker was corrected under
the separately owned `support-rimworld-17-verification` change, with focused RED/GREEN and independent
review. The first P0 product diagnostic run reached the real settings page and failed because the
content selector was not yet composed. Its actual RED is retained; no gameplay task is accepted.
Revision6 now composes the common settings, protected warning sink and initial incident catalog/UI
and admission groups. Early XML diagnostics wait for their separate fixture runner integration.
The native scenario adds real developer-console inspection before any eventual GREEN claim.

Reviewed milestones include common settings and renderer, admission/provider policy and Harmony
ownership receipts, diagnostic options/context scopes, drug reconciliation/batch restoration,
content catalog/requested choices/publication, and raw number formatting/punctuation validation.
Scoped review corrected invalid overrides/drafts, cleanup reporting, invisible punctuation and
failed content-restoration invalidation. Exact test counts/hashes belong to the local component
ledgers; these small slices do not complete their parent features.

Root reserves `Source/Patching/Harmony/CurrentGameObserverPatch.cs`, its matching Harmony tests,
central registrations and target index. Settings/Harmony and matching pump fixtures are leased to B.
Feature Harmony/E2E folders are separately leased to C;
common settings E2E folders to B. New executable incident scenarios cover ordinary native execution
and expected refusal with preloaded policy, but do not prove UI changes or weighted selection.
The separately owned `add-gateway-expected-incident-refusal` extension preserves old constructor
contracts and distinguishes expected native refusal from setup/execution failures. Native proof
remains pending along with the minimized widget capability and every integrated P0 acceptance gate.

## Current leases and tracer state — revision12

Revision12 adds a root-coordinated C01 milestone for inner transformations after ordinary Harmony
transpilers. The independent audit names six affected body/shape guards; ordinary generation
revocation is insufficient to publish a newly rewritten body. Target-specific required operations,
compatible/unrelated effects and safe publication are documented in the scoped C01 plan. P0-A's
NativeLogPatch slice and review fixes are61/61 focused Harmony GREEN and independently rechecked
CLEAR; sources are frozen. P0-A now plans the minimal published Prefixes/Finalizers exports needed
by actual S071 observers, without an edit/output lease. Converter follow-up is not yet allocated.
Persistence scope is inspected for these synchronous targets; generated/deferred
targets remain a separate obligation. No broad owner ban is allowed.
S071's next design uses actual prepared geometry and effective renderer consumption/stability;
private production and out-address proofs are no longer unconditional milestones. Existing partial
receipts stay unregistered pending that reassessment. P0-B now owns the narrow pending-bridge and
C01-admitted prepared-barrier handoff slice: MessagePlacementBridge, MessageGuiEventPatch, new
MessageBarPreparedPatch, narrow runtime adaptation, matching tests and target documents. It has
exclusive Megamod.Harmony output; root added the MessageBarBoundsReceipt source link. The first
real RED observes unchanged native placement at the first bar consumer after intervening layout.
Fallback, visible geometry/S001 and old observer/body-guard migration remain separate prerequisites;
no registration is authorized. Root serializes builds/runs and retains all central ownership.
Gateway screenshot readback is committed at6c2324c after33 focused tests and independent review.
The reviewed product reload now passes163 steps with personally inspected causal captures; initial
Copy rerun is incomplete after safe refusal of the current nontext clipboard. See local
artifacts/Implementation/megamod-p0/native-reload-reviewed.md. Incremental policy is committed
at9893d90; implementation checkpoints do not close part gates.

- Root owns central MegamodMod registration, project links, target index, canonical product/tool
  build/install and isolated runs. Initial S038 selector/global-edit actions were personally
  observed on their reviewed tracer build; persistence, actual exclusion and full P0 remain open.
- S035/S094 early diagnostic composition now consumes registered, loaded common globals before
  real XML conversion. The preceding actual-loader RED and independently reviewed registration
  are recorded in `artifacts/Implementation/megamod-p0/early-diagnostics-composition.md`.
  A passing loader assertion alone cannot accept the native log feature.
- P0-A authored `megamod.diagnostics.definition-initial-default` beside the existing
  `megamod.diagnostics.initial-xml-preflight`. Both declare the same explicit XML fixture package.
  The native case uses actual log selection, native scrollbar track clicks and Copy; scenario and
  clipboard-preservation review passed. After the reviewed native Error-overload correction, the
  fresh complete run passed all66 steps; root personally inspected all12 screenshots. No legacy,
  reload or provider claim follows. A's narrow Gateway debug-action E2E projection passed independent
  review and focused host tests. The S094 toggle/reload case and late Options XML fixture are now
  authored, compiled and independently reviewed. Its first native run visibly toggled context off
  but timed out waiting for the reload; the precise queue/display state was not retained. A reviewed
  diagnostic-only revision adds bounded event checkpoints and a post-action loading-view capture
  before the unchanged wait/count assertions. That rerun passed163 steps and cleanup, but root
  personally found blank critical captures: native readback failed and returned an empty fallback
  texture. Native acceptance remains open. Gateway owns successful readback proof and bounded
  retry; root owns reviewed reruns and personal image inspection. Shared log-helper extraction
  makes the initial case an affected rerun. The projection reuses the existing
  exact native registry; it does not call the loader directly.
- The selected-XML-fixture tooling owner owns strict exact expected-error declarations and
  startup/final accounting, discovered necessary by the real malformed-type fixture. It owns
  affected host tooling/tests and fixture metadata; no megamod runtime/UI edits. Its reviewed handoff
  is complete. The actual native tracer exposed a live stdout-reader sharing defect; its focused
  open-writer/tail correction passed independent review and the fresh full runner. Exact expected
  counts, original context, final flushed-log checks and complete owned cleanup remain mandatory.
- P0-B's queued-registration guard and active-pass consumption veto have separate independent
  reviews and focused host evidence. The outer caller/event timing boundary is now separately
  reviewed. Native bindings/composition and pure bar preparation also passed independent review,
  but placement stays unregistered. The settings agent's Entries incoming-shape gate passed review
  after correcting harmless-constant rejection. The observed execution receipt's mutable-receiver
  correction also passed independent review. These historical partial implementations remain
  unregistered and are selected for retirement under revision12, rather than being prerequisites
  for further field-address/private-production proofs. Next work is the actual prepared-state and
  render-consumption barrier, geometry/visibility provider, and adapted message timing. Native
  bookkeeping remains intact. The paired-offset
  editor, transient draft tests and seven feature catalogs passed review; registration, preview and
  native UI evidence remain open. Root owns project links/registration, and output leases serialize
  shared test outputs. The narrowly scoped Gateway client/MCP agent owns the standalone debug-action
  projection and explicit oversized-output failure. Its argument/UTF-8 corrections passed independent
  review and51 focused tests; standalone native acceptance remains pending.

All137 features remain allocated. These are component/tracer states, not final part acceptance.

Current revision12 dispatch update: B's first prepared-barrier/bridge slice is frozen after41/41
focused Harmony checks and two CLEAR correction rechecks. Its early-consumption veto, cached
unchanged-generation admission and inherited-getter support have actual RED/GREEN evidence.
No feature registration or native geometry/fallback claim is made. Root runs the independently
reviewed `megamod.content.native-incident-save-override` scenario after its successful dry-run.
A owns only HarmonyPublication immutable Prefixes/Finalizers exports and their new focused test
fixture; existing reader, NativeLog, converter and S071 consumers are excluded. Root releases that
edit/test lease only after native deployment is frozen. B prepares its next migration read-only.

That export is now frozen after68/68 plus strengthened method-attribute coverage7/7, independently
rechecked CLEAR. B's current lease is only MessageGuiEventPatch and its existing test fixture,
using the reviewed snapshots and every owned-slot reader; no Scope/Consumption or old bar-helper
migration yet. Root owns shared docs and final build/install. C12 has a read-only completion-receipt
proposal and a documentation lease, with no product/test/launch authority. The C11 native diagnostic
passed38 steps and all8 captures were personally inspected; precise Gateway refusal tooling was
checkpointed at3a4830d, without accepting a megamod feature.

Event migration subsequently passed34/34 and independent review. Root retired the three unused
Entries/Execution source experiments and their two standalone fixtures; independent dependency
review was CLEAR, current Prepared tests passed10/10 and the1.7 product compiled without warnings.
No active behavior depended on the removed types. Scope/Consumption migration waits for A's next
narrow C01 converter slice: generation-bound safe admission and cleanup-sensitive Enter/Exit
effects, existing converter plus shared publication helper, one new fixture and S094 target/design
docs. A owns the Harmony output lease; root retains central integration and all native runs.

That converter slice reached120/120 focused checks and then received independent findings:
unchanged-generation admission must survive concurrent inspection; a metadata-size refusal must
not prevent another owner's repatch; and rejected nested conversions must not borrow parent
diagnostic provenance. A retains the converter/helper/test lease for focused reproductions and
corrections. Native reruns wait for reviewed fixes; earlier native evidence does not accept them.

While that shared boundary is revised, B completed the independent S047 chooser projection with
16 focused Unit checks and seven actual RED/GREEN pairs. Independent review is pending; native
page discovery, controls and opening remain due. C's next lease is S029 native policy storage and
its concrete `Megamod.Defs` tests. Root owns that separate host-environment registration, project
and guards. It uses real native data objects and scoped constructed Defs without active mods or
Harmony; no maintenance hook is enabled by this slice. Native early repair, preset recovery and
Assign-tab UI verification remain later coordinated work. A's Harmony and C's Defs output leases
are separate; root retains central metadata, installation and all game-process ownership.

The immutable package identities, actual RED/GREEN receipts, review dispositions, normal-configuration
hashes and exact-process cleanup evidence remain in the local ledgers. Task2.2 stays open until its
full selected scenario/provider manifest exists; no diagnostic run erases an unrun obligation.

## Current leases — revision13
This section retains the revision13 assignments and closure. Revision14 below is current.

A's first atomic S094 converter-family candidate reached144 Harmony and5 scope checks. Independent
review found three remaining provenance gaps: a rejected stale body can leave its parent visible,
an earlier newly installed prefix can report before the guard, and an individually renewed converter
generation can bypass a stale family receipt after guard-only removal. A now owns focused correction
of converter/guard/context/runtime, with Harmony and narrow scope Unit outputs. Final reviewed native
default/reload/legacy evidence waits; no older native capture accepts the changed family.

B's Scope reader migration is frozen after56 affected Harmony checks and awaits independent review.
Only MessageGuiScopePatch and its fixture changed; Event/Bridge and shared snapshot APIs are retained.
Scope requires prefix presence but sorts and rejects factories only for finalizers. Its output lease
has returned to A; Consumption migration remains a separate future assignment. S047 pure chooser
is reviewed at18/18; the current native card-boundary/provider research is retained, with no native
chooser implementation or provider loadability claim.

C's S029 storage6/6 and catalog8/8 have independent CLEAR reviews; existing Def fixtures and current
catalog identities remain separate from native loader authority. C is planning the smallest native
reconciliation caller read-only. Root owns central registrations/projects, all builds/install/native
processes and the part gate. Root committed only the audited standalone Gateway debug projection
at1832bf4, whose isolated45-case source export passed; no product files entered that checkpoint.

Revision13 correction closure: both converter-family rechecks are CLEAR after58 affected Harmony
and7 scope Unit checks. Scope migration plus its cleanup correction is independently CLEAR after60
affected checks. Root rebuilt and locally installed the reviewed350720-byte product, SHA256
428141F9E8302A5C639538CCB75122934DB679F9A0EC9D2B958257668CE1E65F, and owns its fresh default-loader
native reload run. During that frozen run, A researches genuine legacy-loader selection, B plans
the remaining Consumption migration, and C prepares the narrow preset-entry design/target. No
implementer edits runtime or builds while the native runner stages this candidate. Later assignments
must explicitly return the relevant source/output lease; no prior native image accepts changed code.

## Current leases — revision14

Default reload passed163 native steps; root personally observed the causal screenshots and complete
cleanup. Evidence: artifacts/Implementation/megamod-p0/native-reload-family-reviewed.md.
Consumption's two-file migration is independently CLEAR at56 affected checks; root verified all16
manifest hashes. Its changes postdate the staged reload binary and do not enter that evidence.

A now implements select-e2e-native-xml-loader's approved17-source/7-test lease, with sequential
MCP/HostTools/EndToEndHost and Waylith outputs. Root added only the declared host-test Compile link
and narrow ValidFixtures build target. There are no product-runtime or fixture-payload edits in A's
lease. The additional existing GatewayEndToEndBundleCatalogTests manifest builder is leased to A
only to propagate the new declaration/version; existing default/performance assertions stay intact.
B's two-file Draw admission correction is independently CLEAR at35 affected checks, including actual
oversized-metadata removal failure/recovery. Root revalidated all14 final manifest hashes. C resumes
the remaining preset guard behaviors with exclusive Harmony output after its first genuine1RED→1GREEN.
B plans registration and native geometry integration read-only. Root added the narrow preset source link.

C's initial baseline phase is now source-grounded: untouched startup trigger, post-Defs main-thread
drain, before queued native factory workers. Root must provide the shared invalidation instance and
publication before enabling the guard; that older invalidator's public metadata/receipt reader also
requires scoped safety review before reuse. No generic lifecycle framework or reload recovery is
implied. Root retains central wiring, all build/install/game ownership and native acceptance. No
aggregate feature task closes; no game remains running after the completed tracer.

Revision14 closures: queued registration is independently CLEAR at39 affected checks, and the shared
Def invalidator ownership-reader migration is CLEAR at6. Root still must inject one lifecycle owner
before publishing the early drug baseline. A's native-loader tooling is reviewed after an actual
selected-group projection failure and fix (8 targeted,34 runner checks); native mode runs remain open.
C continues the preset guard; B plans native geometry integration; A prepares the legacy scenario.

Root committed a1f0172 as a separate S029 core/storage/catalog checkpoint. Its exact source export
passed17 Unit,14 Defs and3 beta-boundary tests,161 OpenSpec items, clean product/tool builds and9-file
package validation. The checkpoint also pins the inspected1.7.5000 runtime/inventory; no Harmony
dependency or unfinished preset hook is included. Working-tree integration remains broader and
uncommitted. See artifacts/Implementation/drug-policy-checkpoint/verification.md; no native task closes.

Revision15: helper checkpoint566a824 contains immutable native drug-baseline capture and exact
original-provider selection, verified in an isolated six-file source export. Root's separately
reviewed startup wiring now shares one invalidator with incidents; native policy acceptance remains
open. The prepared-bar metadata correction is CLEAR at34 checks. A bounded recurrence audit found
the same issue in NativeLogPatch; its local correction is independently CLEAR at58 affected checks.
Later runtime builds invalidate affected earlier diagnostic evidence.

The S029 native preset fixture/scenario is authored and independently reviewed. Its missing Assign
entry path is being added to the already owned Gateway minimized-window-input change: native command
adapter, typed MCP/CLI and E2E step, then existing captured-window clicks. Root retains all launch
and acceptance work. Concrete leases, reviewed identities, test receipts and outstanding gates are
recorded in artifacts/Implementation/megamod-p0/revision15-dispatch.md. No clipboard is needed for
this scenario; the diagnostic Copy workflow retains its separate preservation precondition.

Revision15 native closure: the reviewed Assign action and initial S029 preset workflow passed 43
steps in `20261108T101457394Z-77b75d40df7043a793a9594469afc0a5`. Root inspected all ten captures,
including Beer edit/restoration, pawn assignment/restoration and the native console. Exact package,
process, normal-configuration and cleanup audit passed. S029 task 2.5.3 and the Gateway's scoped
2.7/2.8 tasks close; complete S029 and P0 gates remain open. Current product DLL is
`D91ACE306C13F82DB3F6BB862C0BA9125DFF9F1E1ABD3F5A075C1D2ED4EB33CF`.
The next narrow S029 lease authors unchanged-catalog save/reload through existing native controls
and SaveLoadActionStep. It owns only a new persistence scenario and its README; no product or shared
runtime changes are assigned. Root retains integration, build/install, review and native execution.

## Current leases — revision16

The revision15 persistence lease is complete: both native preset scenarios passed98 steps with20
root-inspected captures, exact package/configuration audit and graceful cleanup. Only the unchanged-
catalog edit/assignment persistence and initial preset scopes are accepted. See the current ledger
`artifacts/Implementation/megamod-p0/drug-native-preflight/persistence-verification.md`.

S138/S139 are new P0-C intake items. One researcher reads tolerance documentation/source first, then
generator documentation/source; separate observable drafts follow. Generator source is behavioral
reference only. Root owns queue/allocation/dependency changes. The latest user policy removes original-
mod API/drop-in compatibility work and substitutes best-effort ordinary feature-off handling with a
startup warning. Foundation and active feature-doc reconciliation have disjoint documentation leases.
No product code changes or game launches are assigned by this update.

S071 geometry implementation is held until root grants the necessary existing prepared-hook lease.
The separate owned transpiler proposal conflicts with current ownership/publication and was stopped
before any edit/test. Its corrected plan uses one incoming stream and distinguishes handoff from
geometry authority; automatic clearance remains unregistered. Existing reviews are component evidence,
not acceptance of that future composition. No feature is removed or aggregate gate checked.

Revision16 document closure: S138/S139 now have their own researched drafts and replacement reports;
no gameplay source was added. Their explicit prerequisites remain open. The original-mod policy is
committed at310e094, with independent review and strict validation of the five-path checkpoint export.
The two scoped policy-reconciliation batches cover49 active feature documents, preserving292 task
markers and all runtime acceptance states. Their independent reviews are CLEAR. Outside those
rewrites, the governing policy explicitly retires earlier original API/drop-in/coexistence obligations
across the full queue; retained historical findings must not be dispatched as implementation tasks.

## Current leases — revision17

The preceding assignments are historical. The narrow settings lease has handed off independently
reviewed effective-off resolution, requested-value reads and the first S035/S094 immutable option
consumers. Its exclusive Unit output lease is released. Root's provider mapping, startup warnings,
runtime integration and settings UI are also independently reviewed, with focused host checks and
canonical build/package checks passing. Native acceptance remains pending. The immutable startup
override never enters stored globals or save overrides. Existing non-Boolean options and content
allow/exclude values are not feature-off switches; content and drug master-off paths remain open.

The S071 entry-consumption lease has handed off its independently reviewed prepared-patch participant
and focused host checks; its Harmony output lease is released. This establishes entry-argument
authority only. Field-to-geometry production, runtime control flow and GUI lifetime remain open.
One actual incoming stream and publication owner serves timing handoff and separately revocable
entry-argument authority. Runtime adapters, Bridge, C01, projects and registration remain root-owned;
automatic clearance stays unregistered.

The S094 active-original native scenario is authored and independently reviewed; root owns its exact
build, isolated run and personal observation. Its startup-warning evidence may use the existing
durable owned fallback file; native settings close and Def reload remain the behavioral workflow.
No build, installation or native acceptance is delegated. The local revision17 manifest records
exact paths, tests and handoff evidence. Original-provider handling is a warning plus ordinary
best-effort off, not a compatibility certification. S138 route research is awaiting independent
review; S138/S139 implementation prerequisites remain open.

## Current leases — revision18 (2026-11-11)

Prior assignments are historical. The user requests coherent implementation batches, grouped checks
and fixes, and logical commits. Root integrates the existing dirty tree and retains game ownership.

- `content_batch`: ContentSelection/DrugPolicyMaintenance feature source, matching feature tests,
  feature-specific language catalogs and owning specs/target docs. Add ordinary master-off behavior
  before provider mapping; retain all requested exclusions and drug preferences. Exclusive Unit
  output lease. Central registrations/projects/shared settings remain root-owned.
- `message_batch`: MessagePlacement source, matching tests, S071 specs/targets/languages. Extend the
  existing prepared owner for control-flow/geometry authority and assess a faithful manual-offset
  runtime slice. Exclusive Harmony output lease. Automatic clearance is not accepted by a host guard.
- `gateway_review`: read-only independent review of inherited Gateway/MCP/runner prerequisite
  changes and their test/spec traceability. No product edits or process ownership.
- Root: shared registration, project links, original-provider mapping composition, dispatch docs,
  canonical builds/install, package checks, review dispositions, native runs and scoped commits.

Workers do not launch games or commit. Output leases are returned before integrated builds. Author
native cases using existing typed controls; no synthetic end state accepts a feature. Runtime
changes require fresh affected evidence. No feature or part acceptance box closes on this dispatch.

## Current leases — revision19 (2026-11-11)

Revision18 assignments are closed; the committed batch ends at bfe5e0e. Current installation remains
1.7.5005 rev1082. All unfinished feature obligations and native gates remain open.

- `message_batch`: S071 MessagePlacement source, matching feature tests/catalogs/owning documents,
  and a feature-local native preview scenario. Deliver inert effective-scope preview and honest
  automatic-clearance status. Existing shared C01/settings providers and bootstrap are root-owned.
- `tolerance_batch`: S138 DrugTolerance source, matching Unit/Defs tests and owning documents;
  narrow S029 DrugPolicyRow and NativeDrugPolicyStorage extension to preserve immutable tolerance
  choices through existing row transactions. Native patch registration and persistence hooks are
  excluded from this first tranche. Unit output is shared sequentially with message_batch;
  tolerance_batch owns Defs output until handoff.
- `gateway_review`: read-only independent review of the inherited minimized-input geometry,
  dispatch diagnostics, tests and owning contract. Root resolves findings and owns live runs.
- Root: project source links, central UI wiring, integration review, canonical build/install,
  exact selected native workflows, evidence and logical commits. No worker installs, launches a
  game or commits. Review and runtime edits invalidate affected earlier native evidence.

Revision19 handoffs are closed. Gateway input correction is checkpointed at9f0e1ed; S138's reviewed
state/codec/S029 transaction tranche at5db35c3. S138 native hooks/editor/persistence remain unregistered.
Root integrated and reviewed S071 preview and ordinary startup-off correction, and personally
inspected English native preview/reset/close, placement disable/reenable/reset and affected S029
save/reload workflows. The startup-off case has an actual failing RED and passing reviewed GREEN.
The first preview attempt failed screenshot readback; the unchanged-build retry passed. Exact
build identities, root observations, log classification and cleanup are retained locally in
`artifacts/Implementation/megamod-p0/revision19-integrated-verification.md`. This closes the edit
leases only. Automatic geometry, tolerance enforcement, wider native/localized cases and every
whole-feature/part gate remain open; no unrun previous content/master-switch task is accepted.

## Current leases — revision20 (2026-11-11)

Start: clean d1607aa; previous goal turn made implementation and verified-native progress. Revision19
leases are historical. Continue toward the full139-feature objective, with no acceptance reset.

- `message_batch`: Features/MessagePlacement including existing BarBounds prepared/entry owner,
  Bridge/runtime, matching feature tests/catalogs/owning specs and MSG target documents. Implement
  actual prepared native geometry and consumption stability, then executable causal native cases.
  Existing manual/preview behavior and later S001 composition remain obligations. Initial exclusive
  Megamod.Harmony output lease; coordinate Unit output with tolerance_batch.
- `tolerance_batch`: Features/DrugTolerance, matching tests/catalogs/owning specs and DT target
  documents; narrow S029 state-storage composition as required. Native copy/Scribe support precedes
  enabled contextual controls. Required enforcement and editor activate atomically; a persistence
  checkpoint alone is not protection. Resolve exact timing/recipient/route semantics on evidence.
  Initial Unit/Defs output lease; request Harmony output after message_batch releases it.
- Root: central registration, projects, shared contracts, broad indexes, integrated independent
  review, canonical build/install, exact native workflows, evidence and commits. No worker installs,
  launches games or commits. Final native runs occur after the selected integrated batch is reviewed.

The third slot, `generator_research`, has a documentation-only S139 lease: owning design/spec/tasks
and a feature-local refinement research note. Resolve DG-Q1–3 from exact current native helpers,
Core content and measured comparators, independently deriving supported suggestion rules, units,
rounding and explicit unsupported cases. No source/tests/project edits, host output lease, game
launch or commit. DG-Q4 publication and S138-dependent controls remain separate required milestones;
the research must not substitute universal refusal for the requested generator behavior.

Any handoff missing required behavior remains implemented-unverified. Record native scenario IDs,
current source identities, RED/GREEN evidence, new assumptions and unresolved dependencies before
root integration. Root may revise allocations on concrete evidence while retaining every obligation.

Revision20 lease refinement: root takes `MessageBarGeometryGuard.cs`, its focused Harmony fixtures,
and the existing `MessageBarPreparedPatch` observer integration/tests. `message_batch` retains the
native geometry snapshot/runtime, downstream label admission, owning docs and causal scenarios.
Root adds the read-only C01 postfix registration projection needed by that admission. Geometry
rejection must preserve the existing manual path. The first working automatic slice can decline
currently unproved visible adornments; their full implementation remains allocated, not waived.
`tolerance_batch` additionally owns native save/load finalization guards and visible failure notice:
exact source inspection proved Scribe_Deep swallows row failures. No successful missing-state save
is acceptable. The S139 research lane has handed off to independent contract review before its
calculation or native publication implementation. Harmony output is leased sequentially between
root and the two implementers; all root live/commit authority remains unchanged.

Revision20 continuation after research checkpoint251b1e6: `generator_research` now owns additive
S139 Core calculation/classification/channel-conflict source and Unit fixtures, plus DG-Q4 native
unpublished-object/publication research. The reviewed model explicitly discloses native batching
variance and residual tolerance. `tolerance_batch` may start additive S138 reader/enforcement/editor
files while its reviewed persistence tranche stays frozen for the root native gate; editor and
enforcement remain unregistered until the atomic batch is ready. New source stays compile-coherent
and freezes for root builds. Unit output is coordinated between these two workers; message owns
Harmony output for review fixes with short root handoffs. Root owns a narrowly inspected C01
publication-identity receipt requested by the concrete label authority hot path. No new code or
research handoff is accepted as a native result.

Revision20 drawing-proof refinement: `geometry_owner_review` changes from reviewer to implementer
for the new `MessageBarAdornmentGuard`, matching Harmony fixtures and `MSG-BAR-ADORNMENTS` contract.
It may compose that contribution through the root geometry guard's optional trailing input; it does
not replace the sole prepared-owner transpiler. `message_batch` retains drawer/portrait/icon helper
admission and native binding. These new changes need a different independent reviewer. Harmony
output leases are coordinated directly among those workers and `tolerance_batch`. S138 persistence
is checkpointed at7ed458e with native acceptance open. The additive tolerance reader and S139 C1
calculation/publication design have independent no-finding reviews; the tolerance lane continues
atomic enforcement/editor work while root prepares their separate implementation checkpoints.

Further revision20 bounded division: `geometry_owner_review` now owns a separate DrawColonist
exclusion participant and fixtures for selection/world/edge-expansion eligibility, supplied to the
existing passive observer; `message_batch` owns portrait/icon drawing chains and native binding.
Root owns the finite direct outward-render-call inventory in the existing bar geometry guard,
including its added-draw regression, and S139's detached immutable draft/edit/reset component.
The prior drawing participant is frozen at its 34-check green handoff. New contributions still need
independent review before the native gate. C1 and native-reader checkpoints are e4f7827/28da488;
the separately reviewed C01 publication-identity helper is checkpointed at4f3b69f. No checkpoint
changes the full139-feature obligations or accepts an unobserved gameplay scenario.

Revision20 batch refinement after the user's focused-verification instruction: the reviewed S071
automatic implementation is checkpointed with native acceptance explicitly open. Native fixture
repairs are collected together: an actual two-row-to-one-row setup, exact UI/window contracts,
positive fault reachability and visible failure notices. The save-failure fixture uses only its
required successful-save baseline; it no longer replays the separate copy/edit workflow. A single
tooling-owned change, `extend-selected-e2e-diagnostic-expectations`, adds precise selected-test
runtime error accounting through the existing typed E2E surface. It does not waive unexpected logs.

Additive S138 enforcement/editor and S139 native-fact implementation may proceed while this
verification batch is repaired. Root integrates and independently reviews the next coherent build
before selected native runs. Workers freeze compile inputs for root build/staging checkpoints;
they do not launch or install. Earlier component observations retain their exact build identity
and do not accept later runtime changes. No feature, aggregate part or remaining comparator/native
workflow obligation is removed by this scheduling change.

Revision20 generator checkpoint9eab5a4 adds reviewed detached native facts and common preference
definitions, with the reviewed body-part metadata correction and seven-language validation text.
Neither is exposed through runtime UI. `generator_research` now owns DG-POLICY-CREATE preparation,
publication and effective constructor/getter admission, plus its narrow metadata-only C01 inner
callback projection; root retains project links, central wiring, installs, native workflows and
commits. S138's reviewed decision/width/social tranches continue through targeted review fixes;
the separate editor-call tracer is still incomplete and unregistered. Runner diagnostic declarations
are compiled selected-test metadata with hash-bound private handoff, never a user-supplied ignore
list. Root's next native batch remains the exact message-layout, failed-overwrite and late-load
scenarios after the relevant reviewed freeze. No full fixture or original-provider matrix replay
is implied, and every remaining feature/part obligation stays open.

Revision21 active leases after native route checkpoint9e0f032:

- `generation_admission` owns the first F1 native decay-contribution guard/admission and matching
  Harmony tests, plus its DG-NATIVE-FACTS contract sections. No central registration, C01/S029/C03
  changes, installation, game launch or commit. The lease proves one contribution; remaining
  effective-fact milestones stay open. It coordinates the shared Harmony output and freezes all
  compiled inputs for root builds.
- `tolerance_native_routes` owns social-drink/drinkless research and executable scenario drafts
  under ignored artifacts only, followed by independent review. It does not edit the compiling E2E
  tree or product source. Root integrates the reviewed exact hash at a coherent batch boundary.
- Root records the passed scheduled/direct-recreation scope, integrates/reviews batches, builds,
  installs, runs exact native scenarios, personally observes evidence and commits logical units.

Earlier source freezes are released for these explicit leases. Both lanes preserve every unrun
feature/composition obligation and use native player paths, never direct end-state mutation, for
acceptance. These leases supersede immediate assignments only, not the139-feature/P0–P8 scope.

Revision21 next batch after5d2ef96: the reviewed contribution receipt is committed at7899799;
social/drinkless and global off/on timing have scoped native acceptance. Full gates remain open.

- `generation_admission` now owns the actual pawn-field projection and deterministic zero-range
  SeverityPerDay initialization guards/admission plus exact focused Harmony tests and its owned
  DG-NATIVE-FACTS contract sections. Follow the local `f1-next-admission-plan.md`: preserve native
  field/call/Single-store ordering and actual zero-helper branches. No central registration,
  broader rate/hash/calendar chain, C01/S029/C03 edits or duplicated observer infrastructure.
  Request a shared lifecycle lease if finite-bundle reuse requires extraction. Harmony output is
  leased for focused RED/GREEN; freeze compiling inputs when root requests native deployment.
- `tolerance_native_routes` owns artifact-only positive20% equality→natural-decay native scenario
  A and independent review. Planned Any-versus100 and shared-Psychite cases remain subsequent
  obligations. Root owns compilation, exact integration, native runs and personal observations.
- Root owns F0 catalog/currentness research and its subsequent explicitly recorded integration,
  central files, package/install outputs, native acceptance and logical commits. The existing
  initial native baseline is not relabeled as applied C03 drug-exclusion authority.

Revision21 continuation after3666ca0: the shared initial catalog, pawn projection and positive
tolerance boundary are checkpointed; scoped native observations do not close full S029/S138/S139.

- `tolerance_native_routes` owns the artifact-only numeric100/Any and Tea-to-Yayo scenario batch
  and its independent review. Root owns the compiling counterparts and exact-ID execution.
- `generation_admission` continues the seven-method zero-range initialization slice using the
  existing `DrugGenerationObservedBodies` bundle unchanged. Its current drafts are uncompiled;
  root explicitly releases the two source/test files after the native package is staged. The
  helper, calculator and initialization stages each retain focused RED/GREEN, then one affected
  regression/review batch. Native initialization premises and later F1/F2 obligations remain open.
- `catalog_publication_review` changes from reviewer to a distinct F0 implementation lane. Its
  initial lease is only ignored `s139-native-facts/fact-inputs` plans and drafts for consumed-input
  snapshots and focused Defs tests. Capture must use the same shared catalog identity and explicit
  caller-supplied native handles, retain every consumed known identity/value and refuse unsupported
  shapes. Human adult inputs and the effect-factor curve require separately established native
  binding; detached snapshots cannot certify it. No duplicate Def database, general reflection
  graph, applied-exclusion authority, compiled edits or runtime registration is authorized by this
  draft lease. Root releases an exact compiled file scope after reviewing the draft.
- Root retains finalized native binding, central runtime composition, shared output coordination,
  reviewed build/package identity, native action/capture inspection and logical commits. No worker
  launches games or installs packages. Source work may resume only after the immutable reviewed
  native deployment is running, without modifying its installed files.

This division groups related fixes and verification while keeping source ownership disjoint.
Every remaining native case, original feature and P0–P8 obligation remains allocated.

After the06f9894 B/C native batch, `tolerance_native_routes` advances to artifact-only D/E:
save-specific override/inheritance with a dormant-off value preserved through one native save/load,
and save/resume during actual scheduled ingestion after a native editor change to zero. The latter
must rebind fresh Game/pawn/job/source identities and prove actual resumed consumption plus a fresh
rejection. Each case uses only its own one-roundtrip baseline; neither claims genuine two-save
switching. Existing native input/view/save helpers remain the control surface. Root compiles and
accepts only independently reviewed exact hashes; transient draft/review-slot pauses do not remove
these cases or the separate two-save-switching obligation.

After526ed1e, root integrates the independently reviewed D/E fixtures and owns their complete
compile/discovery, fresh-process native verification and any batch defect return. The new F0
consumed-input and seven-method initialization APIs remain unregistered checkpoints.

The next `generation_admission` lease is exactly
`Harmony/DrugGenerationRateBodyGuard.cs` under the S139 feature, its matching
`DrugGenerationRateAdmissionTests.cs` in Megamod.Harmony, and the corresponding S139 design,
specification and DG-NATIVE-FACTS sections. Root releases these compiled files and focused Harmony
outputs only after the reviewed D/E package has deployed. Prove the actual age getter, Core flag
getter and ordered rate consumer using the existing finite observation bundle unchanged; preserve
the concrete consumed static's declaring-type initializer premise. Current native fields, CurStage
membership and actual runtime call-edge/inlining proof remain supplied, unproved prerequisites.
No registration, shared helper, BCL patch, generic observation or stage-body extension belongs to
this lease. The closed generic/interface stage gate remains separately allocated. Root retains
project links, installs, games, integration and commits. This refines sequencing without removing
any positive profile, native scenario, feature or P0–P8 obligation.

During the corrected D/E native batch, `native_input_binding` has an artifact-only research lease
for the actual Human adult inputs and drug effect-factor static curve needed by the committed F0
snapshot. It inspects existing authorities and exact native APIs under ignored
`s139-native-facts/native-input-binding`; no compiled/spec/project edits, tests, builds, installation
or game control are delegated. Extraction, effective-method proof and still-unwired applied-save
content authority remain distinct. Root decides the later implementation/native binding scope.

After that research, the same lane may prepare artifact-only drafts for the Human-origin slice:
`Runtime/NativeDrugGenerationHumanInput.cs` under S139 and matching existing-tier focused tests.
Use the explicit full-grown native Def reference and current ordered list, with pure selection
tests over opaque identities; no native LifeStageAge construction in host tests. Curve initialization,
stage/BodySize execution and controller registration are excluded. Root releases any compiled paths
and project links only after the D/E deployment freeze; source authoring remains test-first then.

After51c839a, D's reviewed native save/inheritance workflow passed176steps and root inspected
all21 causal captures. E has deployed the same immutable package in its fresh isolated process.
The rate lease above and the independently reviewed Human-origin plan are now released for
compiled implementation. Human scope is exactly new `Core/HumanAdultInput.cs`, new
`Runtime/NativeDrugGenerationHumanInput.cs`, its new Unit `HumanAdultInputTests.cs`, and the
extraction of the two existing input declarations plus one constant alias from
`Runtime/DrugGenerationFactInputs.cs`. Preserve the existing Runtime namespace and API. Root owns
the one new Core file link in `Megamod.Defs.csproj`. Human leases only focused Unit and affected
F0 Defs outputs; rate leases only its focused Harmony outputs. No shared helper, native stage/
BodySize proof, curve preparation, runtime registration, product build/install, game or commit is
delegated. Each lane retains focused RED/GREEN and obtains independent review; root integrates
the batch and records remaining native premises. No task or positive generator obligation is removed.

After9833043/a82beb2, the Human-origin and age/flag/rate checkpoints are reviewed and committed.
Root's canonical integrated build/install and65-file package validation pass; these unregistered
helpers do not claim native generation acceptance. D/E's earlier exact-build one-save observations
remain scoped to their retained evidence. Tooling checkpoint3697b57 adds exact single-group E2E
batches with37 distinct focused checks and an actual two-ID, no-mutation dry-run.

The next selected native obligation is S138 main-menu guards and two genuinely independent saves.
`entry_menu_gateway` owns the narrow Gateway Entry-menu Options/DevQuickTest input prerequisite:
new Input entry-menu lease/automations/native implementation, the existing minimized mouse backend's
generic target seam, shared typed Entry-menu steps, E2E driver/bootstrap wiring, focused Waylith
tests/native scenario and its Gateway-owned contract/docs. It must capture actual native controls,
preserve scene/root/layout/DevMode/window stale checks and reuse the public typed automation surface.
This is a Gateway-owned change; it adds no product dependency. Its exact path list and TDD ledger
live under ignored `artifacts/Implementation/gateway-entry-menu`.

`save_switch_fixture` owns artifact-only drafts under `s138-native-save-switch` for a new independent
native scenario using that API. Existing D/E fixtures remain untouched. Root releases the compiling
fixture only after reviewing the full UI sequence and counted budgets. `generation_rate` prepares
the separately allocated closed generic/interface stage-membership plan under `s139-native-facts`;
its next compiled lease awaits the exact supported native/host boundary. No worker installs packages,
launches games or accepts native outcomes. Root retains integration, current package identity,
personal observations and commits. Remaining native facts, compositions, all139 features and every
P0–P8 obligation remain required.

Stage follow-through: the owned closed-method canary demonstrated shared executable code while
its sibling metadata receipt still reported current. This supersedes the proposed generic
observer expansion. `generation_rate` completes the positive severity-field leaf only; root owns
the focused pre-acquisition isolation refusal in `DrugGenerationObservedBodies` and its new
`DrugGenerationObservationIsolationTests`. No BCL observer is installed. The native stage
collection/helper proof is retained and requires a separately reviewed passive publication/body
path or proven isolation. Root also owns the green F0 physical-curve-storage refactor in the
existing `DrugGenerationFactInputs`, using existing Defs coverage without new test tiers/APIs.
One affected host regression/review batch follows the finished leaf/helper/refactor; native
origin, first-use initialization, F0–F5 composition and generation acceptance remain open.

After fd28279, root has integrated and compiled the independently reviewed S138 two-save fixture.
Its source lease returns to root; the native run remains pending the Entry-menu Gateway review
corrections. The Gateway author fixes the enclosing-layout publication and failed-capture evidence
findings together, then returns frozen source for independent re-review. Root runs only the affected
checkbox/layout-replacement pair, the singular process-owned Entry scenario, and the singular
process-owned S138 scenario. The new exact-batch selector does not relax process-owned isolation.

The next S139 lane is artifact-only passive published-body reader design and test drafts beneath
`s139-native-facts/stage-membership/passive-observation`. Existing source remains frozen for S138
deployment. Root will separately release the precise nongeneric reader/shared-gate paths after
independent design review and immutable staging; no generic observer, stage registration or native
proof is authorized by the draft. The separately reviewed S138 next-batch plan now selects an
artifact-only `native-stock-routes` fixture under `s138-next-batch`: actual scheduled rejection,
native Any edit with attached pickup, then independent refill from empty. Its author changes no
compiled scenario and launches no game; root must review counted actions/cleanup before release.
Caravan/world control and language-only runs remain separately allocated prerequisites. All
remaining S138/S139 contracts, other features and P0–P8 gates stay allocated and unaccepted.

After0fda103, root accepted the scoped S138 main-menu/two-save workflow on its isolated reviewed
build:138 native steps and14 personally inspected captures, with exact cleanup/configuration proof.
The Gateway ordinary pair and singular Entry prerequisites also passed; their full native matrices
remain separate. A managed detached acceptance worktree keeps compiled inputs fixed while main
checkout workers use their disjoint leases. Completed stopped-run evidence is mirrored under the
main ignored artifacts directory before that worktree can be archived.

Checkpoint0374d55 contains the independently reviewed passive nongeneric published-body reader:
37 affected checks, strict validation and canonical65-file product build/install pass. It installs
no new observer and remains unregistered; no Waylith execution, generic-family, stage-membership
or generator acceptance is inferred. Its source lease is returned to root.

Root now integrates the reviewed `native-stock-routes` fixture. The sole post-review edit removes
its artifact-draft comment; the owning E2E project compiles. Root owns exact discovery, native run,
personal observations and any review/fix batch. The earlier pooled-Job evidence defect is corrected
through immutable active snapshots. Main-menu and stock scenarios remain distinct selected runs;
completed workflows are not replayed merely to exercise the batch API.

The artifact-only Gateway world/caravan plan now has a checked23-field camera restoration premise
and nongeneric Health-row observation seam. Independent review found that revisiting the same
caravan can retain an open Health detail; its author must correct desired-open handling and action
budgets before a compiled lease. That Gateway tooling owner remains separate from S138's normal
world-tick consumption contract. All139 features, full S138/S139 obligations and P0–P8 gates remain.

The corrected World/Health plan passed independent re-review at artifactSHA09934902E20EA4A5CD694738D275604D8DBBF8330296FBD10DED69012C8C9DC4.
Its explicit native detail closure and repeated-same-caravan case preserve1365s/840s counted budgets.
`world_controls_plan` now owns the Gateway prerequisite compiled batch: native World command,
caravan selection/Items/Health tab commands, observed Health-row input on the existing lane,
same-Game/World camera capture/restore, shared typed steps/driver, narrow bootstrap integration,
focused Waylith tests and singular Gateway scenario. It first amends the Gateway-owned contract;
root retains installation, final independent review, native acceptance and commits. The S138
consumer is a later lease. No new listener, queue or redundant MCP wrapper is allocated.

`generation_rate` has only an artifact research/design lease for the family-publication/native
execution premise after0374d55. It may not install generic/BCL observers, repeat the shared-code
canary or infer actual execution from matching managed bodies. Its bounded proposed path must
reuse the existing publication/reader, address native aliases/compile updates/inlining, and leave
unproved capabilities explicit. No tracked-source or test-output lease is released for that plan.

The S138 language/layout audit exposed a concrete logical-viewport omission. Root's correction
requires prospective native dimensions and an open manager's actual bounds to fit the scaled GUI
viewport; it preserves native geometry and scrolling. Independent review's stale-row recovery
finding has an executable RED/GREEN regression: only allocation failures persist, while viewport
fit remains current. The affected36-case batch and owning build pass, and re-review found no issues.
This is implemented-unverified; older S138 evidence does not accept the revised runtime. Root owns
the next native layout fixture and its combined bad-viewport/narrow-allocation/recovery gate, using
offered native scale controls. Independent fixture review found the installed windowed backend
offers only its current resolution, so explicit disposable size preconditions replace the impossible
resolution-menu steps; only native opening/scale behavior will be accepted. Its asynchronous resize
cleanup uses a minimal shared-context projection of the existing bounded runner cleanup queue.
Seven-language tooltip coverage awaits scoped Gateway
hover after the active world batch. The new UI skill guidance records the coordinate/recovery lesson.

Independent S139 family-scope review requires explicit provider lifetime/revocation, same-interval
consumption, a concrete conditional stage consumer and a nongeneric detour-only regression before
implementation. Those findings are retained under family-publication/review-disposition.md; no
compiled receipt lease is released. The selected custom runtime's native alias premise remains open.
All positive generator, complete S138,139-feature and P0–P8 obligations remain allocated.

The24-file World/caravan batch reached297Unit/12Harmony checks and a clean owning scenario compile.
The first independent review identified unbounded normal-frame drag history, foreign targeting during
restore, and input retirement consuming the full cleanup budget. These findings remain pending and
must be fixed together before root installation/native acceptance. The separate input/observer review
added clipped-row isolation and late detailed-card callback failure publication findings. The author
now owns all five corrections as one focused test/review batch. No world or caravan acceptance is
inferred from the host results.

Atc0cb69c the combined S138 layout/scale workflow passed91steps. Root personally inspected all7
captures showing native unavailable explanations, actual1.25x clipping, native1x recovery and retained
numeric editor state. Exact typed group20261112T184115950Z-295e3f3cb10f47f8a9249516cc221113 completed
graceful shutdown and unchanged normal hashes. Full evidence and scoped playability review are in
`artifacts/Implementation/megamod-p0/s138-language-layout/native-verification.md`; native resolution
selection and routine fail-open are unclaimed. Shared cleanup polling also has its host and native
usage gate. Remaining S138/S139, seven-language/hover and all139/P0–P8 obligations remain allocated.

The World/caravan correction batch now passes306Unit/15Harmony checks and its owning fixture
compiles. Independent World and caravan-input re-reviews found no remaining issues; the25-file
correction manifest is retained locally. Root owns the reviewed checkpoint and fresh exact native
caravan inspection gate; no host result closes native acceptance. The user concurrently requested
S138 use the same native slider as neighboring controls. Root prioritizes the complete slider,
scenario migration and editor/layout gate, replacing the older modal design and its acceptance.

Slider checkpoint0a19184 uses the same native control and removes the modal editor. Its selected
native batch exposed the exact Windows event distinction: MouseDown claims a slider, MouseDrag
adjusts it. Root fixes the premature rounding guard and adds press-retention plus real-drag
scenarios. The Gateway author owns a minimal typed native-window drag extension in the existing
input lane, shared NativeWindowSteps/driver, one bootstrap registration and focused tests/contracts;
ordinary clicks remain unchanged. Caravan consumer planning is paused at its artifact handoff.
The separate native caravan probe exposed a positive-versus-normalized-negative ID check in the
Health detail postfix. Root owns that one-line correction and its later independent review.
Both failed runs retained successful exact-process cleanup and unchanged normal configuration.
Neither checkpoint accepts the remaining native workflows; the corrected batches require fresh
reviewed runs and root's personal observation. All prior feature obligations remain allocated.

The corrected slider batch is now accepted for its scoped UI workflow at reviewed812ae49:
group20261112T195404895Z-b887624bb2834475be68349d64139eb1 passed131 steps, and root personally
inspected all15 captures. Ordinary press retains fractional precision; six real native drags cover
numeric endpoints, intermediate values, Any and malformed repair. Native layout/scale recovery,
complete logs, exact package identity, normal configuration preservation and graceful cleanup are
recorded under `artifacts/Implementation/megamod-p0/s138-native-slider/native-verification.md`.
The same native control now replaces the modal editor. Full S138, seven-language and P0–P8 work
remain open. Caravan consumer planning has resumed as an artifact-only assignment. The separate
corrected Gateway run reached an earlier unsettled World-view refusal at selection, with successful
cleanup; its Health correction remains unaccepted pending diagnosis and a fresh native run.

The focused Gateway correction is now accepted at8f8bbaa. The fixture passively waits for actual
native terrain generation before selection; no renderer invocation or retry manufactures readiness.
Group20261112T201834681Z-3afe38a6aae74ce58eb7248b52ae203c passed47 steps, with all8 causal captures
personally inspected by root: Items/Health, retained detail, stale-row refusal, fresh recovery and
restored map. Exact build, classified logs, unchanged normal hashes and graceful cleanup are in
`artifacts/Implementation/gateway-world-controls/native-verification.md`.

The independently reviewed S138 consumer plan has one reconciliation: inherit the30s World wait,
making100 counted steps/1875s under2100s, launcher2400. The World-controls author now owns only
NativeToleranceCaravanTests.cs, its local DrugTolerance test README, the S138 OpenSpec scenario and
local evidence packet. Root retains shared/runtime registration, commits, final independent review,
installation and the actual paired A-zero/B-Any caravan scheduled-use acceptance. The author cannot
claim that product result from the accepted Gateway inspection. Remaining S138/S139 and all139/P0–P8
obligations remain allocated.
