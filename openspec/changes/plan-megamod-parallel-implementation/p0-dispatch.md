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
