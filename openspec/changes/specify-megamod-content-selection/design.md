## Context

S038 replaces Cherry Picker's broad content-selection behavior under one owner:
**Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), target **1.7 beta**.
Read the [proposal](proposal.md), both capability specs and the
[research inventory](../../../mods/megamod/docs/refinement/2026-11-03-cherry-picker-research.md).
The source has 45 catalogue types plus three optional families, six attributed patch classes and
one dynamic patch. It combines global settings, destructive shared-Def mutations and partial
restoration. Those internals do not satisfy our save-isolation and compatibility rules.

This is a design for later vertical implementation, not evidence that every required native seam
is already found. No runtime code, metadata dependency, patch or migration is added by these docs.

## Goals / Non-Goals

**Goals:** preserve the full observable catalogue and owned grouped-preset behavior; define
precise exclusion effects and scope; compose owned selectors/policies directly; prevent cost loss,
endless generation, ambiguous restoration and cross-save contamination.

**Non-Goals:** arbitrary Def editing, automatic original-settings import, universal interception of
foreign direct spawns, original package/assembly impersonation, copying upstream assemblies, new
bottom tabs or speculative shared-cache frameworks. No live compatibility or performance claim.

## Decisions

### 2026-11-11 ordinary master and dedicated catalogue browser

The user confirmed that individual exclusions remain part of S038 but must live in a dedicated
Content selection browser. General settings now contain one browser button and `content.enabled`;
the browser creates another `SettingsRenderer` over the same `SettingsRuntime` and definitions.
No parallel storage, save editor or inheritance logic is introduced. Existing native selector and
save-override scenarios enter through the new button and retain their prior actions inside the browser.

The content master defaults true, uses global/save scope, and is captured with the explicit save
snapshot at the existing application boundary. An immutable startup effective-off reason overrides
only this master; it never flips `content.allowed/*` or `content.preset-active/*`. Disabled application
publishes allow-all key decisions with `Enabled=false`, bypasses unresolved retained member/preset
application and does not call drug reconciliation. Prior live publications remain unchanged until
the next genuine boundary. This is the existing incident-family milestone, not acceptance of early
world setup or any unfinished catalogue family.

`ContentProviderPolicy.FromActivePackages` supplies the master reason and a bounded warning for
the exact original package. Root combines it with the other immutable effective-off inputs before
constructing the common store. The old provider-specific incident admission gate is retired.
The shared Def lifecycle guard and incident semantic admission remain independent safety gates.


### Policy is data; finalized definitions retain their identity

Use stable owned content keys carrying resolved type/namespace, defName and source identity.
Resolve authored legacy strings separately; never use display labels or a simple-type-name-only
cache as identity. Missing keys remain serializable dormant choices. Guard malformed strings,
ambiguous types and absent providers independently so one bad member cannot abort the catalogue.

Maintain a catalogue of finalized native Defs and optional admitted adapters. Prefer native lists;
derive display metadata once per catalogue revision. Do not remove definitions from DefDatabase
or zero their shared nutrition/value/cost/component fields. Availability belongs at supported
selection, production and grant boundaries, preserving object identity for existing saves.
This replaces destructive mutation/snapshot restoration: a snapshot could overwrite another mod's
later changes and would still leak a policy across saves. No general-purpose Def mutation framework.

Per-save dynamic policy cannot be faithfully encoded as static startup XML. Use direct owned calls
and narrow runtime seams for that policy; reserve XML for owned declarative definitions and any
future genuinely static definition changes. This is the concrete reason for C# here.

The first reviewed host slice represents a key as canonical lower-invariant source package ID,
exact runtime full type name and exact defName. A length-prefixed tuple encodes persisted IDs
without delimiter collisions; localized labels never participate. Catalog membership is detached,
but each entry retains the exact native Def reference. Duplicate keys with different native
references resolve as ambiguous, never as an arbitrary first match. Repeated enumeration of the
same native reference remains resolvable. This is host identity evidence, not native enumeration
or availability acceptance.

Read-only 1.7.5000 inspection identifies `GenDefDatabase.AllDefTypesWithDatabases()` and
`GetAllDefsInDatabaseForDef(Type)` as the native enumeration seam: the former accounts for a
nonabstract base class owning the database. The adapter must use each Def's actual runtime type,
not substitute the database's base type. Provenance uses `def.modContentPack.PackageIdPlayerFacing`
then canonical case; native `ModMetaData.PackageId` can append `_steam` for duplicate discovery,
whereas `PackageIdPlayerFacing` retains the metadata package identity. Do not strip arbitrary
suffixes from user-supplied IDs or fabricate Core provenance for a null content pack. A Def lacking
usable provenance/type/name remains unavailable and must not abort other catalog entries.

Incident eligibility is the first concrete availability-family candidate. Current
`StorytellerComp.UsableIncidentsInCategory` consumes `IncidentWorker.CanFireNow`; its ordinary
selection retains native weights and returns no result for an empty candidate set. A separate
`IncidentWorker.TryExecute` boundary invokes `TryExecuteWorker` and then records fired/codex state,
so an execution refusal must precede those side effects. These are research facts, not admitted
patches. Weather needs broader coupled treatment: its zero-weight fallback can still choose a
biome weather or Clear, and initial/forced weather use separate paths. A commonality-only veto
would not meet the exclusion contract. All other 45+3 family obligations remain unchanged.

### First incident implementation boundary (unaccepted)

The 2026-11-07 scoped implementation now provides five atomic native-shaped patch candidates:
`MM.CONTENT.IncidentEligibility`, `IncidentExecute`, `IncidentExecuteResult`, and the two per-target
`IncidentEligibilityGeneration` / `IncidentExecuteGeneration` observers. Root owns central admission
and target indexing. The first three implement result eligibility, refusal before effects and the
final result veto when an earlier foreign prefix skipped the owned prefix. The two observers return
their incoming instruction streams unchanged and invalidate publication on normal Harmony rebuilds.
This supersedes the earlier unimplemented-candidate status, not the remaining family requirements.

Metadata inspection rejects factory-shaped callbacks before sorting: Harmony's sorter can invoke
factories. The same immutable factory-free array is sorted. Compatible later observers remain
admitted. Later prefixes with a writable worker/Def field, direct identity writes or opaque calls
using the worker have no proven identity-preservation contract and reject publication. Later
void postfix result writers and finalizers with writable results also reject it. Non-void pass-through
postfixes reject it regardless of metadata position unless false preservation is independently proven:
the inspected Harmony provider emits its void postfix phase before its pass-through chain, so an
earlier-sorted pass-through writer can overwrite this void veto. Read-only void result observers
remain compatible on either side. This is deliberately a
narrow ordinary-native-path contract; foreign code performing incident effects itself is not covered.
No foreign transpiler is executed as an inspection probe. Removing the owned compilation observer
itself is outside automatic notification; explicit refresh detects its absence.

The final group installation validates publication and throws into exact owned rollback on failure.
Automatic incident patch revalidation is scheduled by compilation identity versus the last actual
refresh attempt, only after all five group members install. Refresh consumes the attempt before
metadata inspection; an unchanged denied generation is not scanned every main-thread frame. A later
observed compilation permits another attempt. Explicit owner refresh still intentionally reinspects.
Root reports the retained non-null denial reason once per scheduled attempt through the protected
sink, without a second generic warning for exceptions already reported by the runtime wrapper.

Callbacks consume immutable applied values and exact lifecycle owner/generation, never mutable
settings. Faults latch the bridge unavailable; a refusal already made remains false even if its
notification fails and an earlier postfix later writes true. The root's immutable canonical active
package snapshot drove original Cherry Picker suppression through the common admission coordinator
in that tranche. The revised policy requires a bounded startup warning plus the ordinary
per-feature settings disable path, best effort; the earlier implementation is not evidence that
this revised wiring is complete.

`IncidentContentRuntime` is constructed after finalized Defs on the main thread. Root calls its
`ProcessPendingLifecycle` after the common settings pump. This first family waits for Playing and
no current/queued long event, using the immutable finalized save receipt, then applies exactly once
per exact owner/generation. Receipts are rechecked around native owner reads and publication.
Subsequent settings edits stay pending. A benign explicit patch refresh can restore only the same
applied reference; it cannot consume pending settings. Unsupported requested exclusions, invalid
values or persisted active presets reject this initial candidate instead of claiming them applied.
The finished-main-thread incident boundary does **not** solve early map/save finalization, new-game
setup or the other 45+3 families; those earlier receipts and independent-family status remain open.

Before worker loading, `ContentSettings.PrepareLoadInputs` can pin the common runtime's immutable
global snapshot plus registered Boolean definitions. Its detached reader retains no mutable store
or registry and can resolve the loaded component snapshot without UI activation. This prepared
input is infrastructure for the still-open early family boundary, not an applied policy.

The localized explicit refusal message names the excluded incident and explains reload timing.
Its seven baseline catalogs are authored from that gameplay context with identical `{0}` usage;
native rendering and screenshot inspection remain unrun. Existing allowed/refusal executable
scenarios exercise the direct command boundary; weighted selection, empty candidates, settings UI,
ordinary feature-off behavior remains an additional acceptance obligation. No original-provider
compatibility matrix is required; genuine content-provider integration checks remain separate.

### One common settings path with deliberate application timing

#### Native Def reload safety milestone (recovery incomplete)

On inspected 1.7.5000, HotReloadDefs queues a synchronous long event without clearing Loaded. Its
body mutates existing Def objects, flushes queued shallow-copy actions, resolves references and
queues physical-object/map/window finishing work. LongEventHandler catches both body and finishing
action exceptions. Neither the outer method returning nor Loaded plus an idle event queue proves a
successful finalized generation. ClearAllPlayData removes databases before clearing Loaded.

The first safety milestone observes entry to exact public static void HotReloadDefs() and
ClearAllPlayData(). Either call atomically invalidates the incident application before the native
body; no worker callback reads Store, native Def fields or UI. The main-thread pump disables the
bridge and reports one protected bounded explanation. Rows explain unavailable Def lifecycle and
disable edits, while requested values and the original immutable applied stable-key decisions are
retained. This is conservative invalidation even if another prefix later skips the native body.
It never cancels native reload, reconstructs runtime/settings/patch registration or recaptures pending
choices. No idle predicate, ordinary save switch or patch refresh clears this latch in this milestone.

Before S029 shares this observer, root migrated its ownership inspection to the existing C01
immutable publication reader. Only the owned prefix slot is consumed: filter owned registrations
before projecting a receipt, without inspecting attributes, invoking factories, or sorting unrelated
foreign callbacks. Other owners' factory-shaped patches are compatible with this observational latch;
the latch does not depend on their body or ordering. Busy, unreadable or changed provider metadata
withholds installation through the shared admission coordinator. Native entry itself only sets the
latch and performs no metadata access. Exact acquired-slot rollback remains mandatory. Independent
review is CLEAR after six affected host cases, including the actual foreign-factory RED/GREEN;
earlier native tracers do not accept it. Root will inject the same instance into incident and drug baseline owners,
admit it once before publication, and keep lifecycle availability independent of either original
provider's feature suppression. No duplicate prefix pairs or general lifecycle framework are needed.

Recovery remains a required next milestone, not restart-only supported behavior: establish a proven
successful native Def generation, reconcile the same runtime/catalog/UI owners, and retain prior
applied decisions for the exact save/load boundary. Handling genuinely new keys (proposed native
allowed until a new application boundary) still requires an explicit approved contract; this guard
does not encode it. Ordinary save/load and early-family application timing are unchanged. Native
reload/recovery acceptance tasks remain open.

The first settings presentation slice uses the existing common ContentSettings definitions and
Store. ContentChoiceRow is a read-only projection of the requested value plus a separately supplied
actual applied reference. It distinguishes pending allow/exclude, stable applied state, next-load
main-menu state, unavailable application and invalid stored requests; a missing applied reference
never becomes the requested value. Main-menu presentation reads globals even if a caller has not
yet detached an earlier save. Validation errors remain distinct from feature unavailability so the
common editor can repair malformed requested data. This is a host model, not native UI acceptance.

ContentChoiceBrowser combines exact type/source IDs with the main literal, case-insensitive query
inside individual displayed/identity fields. It never joins fields to create a match or applies the
selector's separate all-word search rule. Order follows the requested effective exclusion: excluded
first, then readable name and source using ordinal case-insensitive comparison, then canonical
package/type/Def identity as ordinal ties. The page's applied/pending status stays separate from
that requested ordering. Browsing returns a detached result and does not change store revisions.

The reviewed shared renderer supplies dynamic boolean registration and a nonpersistent navigation
section; the feature supplies visibility and deterministic display rank. It captures those values
once per draw, retains common scope controls and rechecks feature availability plus the current
binding at commit. ContentBrowseSession holds only transient query/type/source selection and caches
immutable ranks by common store revision. Full menu choices remain reachable under another filter.
No dummy persisted setting, duplicate settings store or copy of inheritance controls is introduced.

The first native incident page maps the exact finalized catalog's native labels/source names to
those definitions. It uses ordinary FloatMenus for type/source selection, with canonical identities
in choices and stale native identity checks, and the main literal search field. The common row
shows pending edits against IncidentContentRuntime's actual immutable current applied reference.
Unadmitted integration, ambiguous/missing identities and the ordinary feature-off state disable
edits without removing stored choices. Null application remains unavailable; supported globals can
be edited from the main menu. Runtime failures use the root-supplied protected warning sink rather
than a mutable native warning entry point. Root owns composition and native acceptance.

All seven baseline page/status catalogs are authored. The executable native selector scenario opens
and cancels native type/source menus, clicks the first incident's global checkbox off and back on,
and captures pending/current labels for personal inspection. It uses captured native window identity,
bounds and scale, never direct setting mutation. Its fresh default profile, inherited choices,
untouched initial scroll/filter state and concrete renderer geometry are explicit preconditions.
The first actual isolated run reached the real settings page, but the selector was absent because
central composition had not been added. This is retained RED evidence. The root now constructs the
protected warning sink before admission and snapshots canonical active package IDs once. The common
main-thread pump reconciles settings first, then attempts initial catalog/runtime/UI setup once
after Loaded and no pending/current long event. UI registration precedes guarded feature admission;
setup exceptions do not disable the common settings pump or retry partial registrations. Both the
Def lifecycle group and the incident group must be admitted before policy pumping. The same runtime
is retained through idle transitions; hot reload cannot rebuild it or consume pending edits.
The scenario also opens the native developer log for inspection. GREEN and final P0 acceptance
remain unclaimed. Search typing, committed filter choices, save overrides, main-menu
edits, reload application, absent/provider guards and language layouts still need native workflows.
The full S020 searchable/resizable selector windows and all remaining S038 families/presets remain
open; ordinary FloatMenus do not satisfy their richer contracts. No acceptance task is closed here.

Register individual Allowed and preset Active choices with the common load/store/resolve/row
renderer. Both inherit globally by default; retain dormant overrides. The displayed effective state
also carries all preset/individual reasons. Identity is stable across translations and sorting.

Capture an immutable applied policy at new-game/load boundary, after save overrides are available
but before policy-dependent generation or action eligibility. During play, edits change requested
settings only. Save/reload publishes the next policy; main-menu global edits affect the next setup
flow. Never publish a partial policy or use a previous game's snapshot after teardown. Define and
prove exact load timing in CK-Q1 before coding dependent hooks. If native initialization needs a
later availability phase, delay the affected behavior coherently rather than using global values
temporarily and generating content before overrides arrive.

Main-menu controls disable both save rendering and callbacks. No global-only exception is proposed:
source reliance on global mutable Defs is not an adequate reason to deny the user's save model.
Reload-only timing is an explicit proposed deviation to simplify consistent transitions and prevent
mid-job cost/output changes; it is not source parity. On reload, blocked saved jobs/order state must
stop before new output while preserving recoverable ingredients/progress without duplication.

### Exclusion reasons, not destructive preset expansion

Resolve each active preset through a visited-key graph, maintaining contribution provenance.
Duplicates are idempotent; cycles stop at visited nodes. Root activation, nested reachability and
individual exclusions remain separate. Deactivating a root recomputes contributions without
rewriting any member's direct setting. Bound diagnostics per invalid identity and show unresolved
members. Unavailable adapters contribute no claimed exclusion. Preserve provider list order for
membership inspection while effective membership is a set.

The owned UI explicitly labels preset activation. Its inverse removes only that preset's
contribution, rather than the source's broad removal of member keys. This preserves the intended
grouped-choice behavior without reading Cherry Picker `DefList` data or emulating third-party lists.

### Dependency behavior is explicit

For recipes, check every required ingredient group and ordinary product, plus fixed construction
costs. Keep original quantities, work, skill and required resources. Return blocked eligibility with
an explanatory reason instead of deleting costs. Already-created bills remain; S012 views do not
resurrect disabled recipes via cached lists. The explicit butchering exception omits excluded
byproducts while retaining ordinary allowed quantities; if no product remains, refuse that job.
Ordinary destruction still occurs but omits excluded killedLeavings without compensation. Other
special recipe paths require precise classification against these rules before admission.
Fixed scenario starting supplies are omitted with a setup explanation, without rewriting the
scenario definition or substituting goods; do not rely only on random ThingSetMaker filtering.

For research and VPE prerequisite chains, compute the surviving ancestors through excluded nodes,
union independent requirements and detect cycles. This is a policy projection, not in-place list
rewriting. Preserve completed progress. Source removal's implicit unlock effect is now explicit
in spec and UI. No free unlock on a missing or cyclic edge.

Generation chooses among valid ordinary candidates without changing relative weights. An impossible
required choice terminates with a bounded result. Do not substitute Colonist for a required faction
member, fabricate supplies, use extreme combat values or leave a rejection loop unbounded.
Existing-world state is not regenerated. Existing apparel wear/hediff/new quest restrictions are
kept as distinct action gates; existing learned genes/abilities and worn apparel are retained.

SpecialThingFilter hiding does not rewrite existing filters into a broader allowed set. WorkType
visibility remains separate from actual work eligibility. Treat unsupported hard-reference paths
as named coverage limitations, not blanket guarantees.

### Owned presets without a legacy compatibility adapter

Owned presets retain stable identity, label, source attribution and member choices for the
reason-aware graph above. They use the owned definition/loading path; no CherryPicker.DefList
parser, Class rewrite, original-only dependency-gate rewrite or external consumer parity is required.
The inspected `Wara.toomanymods` and parser behavior remain historical research only. Do not copy
provider list contents, spoof package/type identities or add a facade to make old add-ons activate.

Actual owned declarative patches, if needed, still require XML Extensions existence/shape checks,
external package/content guards where applicable, additive preservation and corresponding dependency
metadata. Removing the legacy adapter does not waive those rules or genuine DLC/content admission.
S020 selectors compose directly with owned content settings and need no source API emulation.

### Startup admission and failure isolation

Create a stable target document per **actual patch**, linked to its scenario, validator, tests and
eventual native evidence. The following groups are an implementation inventory, not pre-approved
targets or a license to share one vague validator across multiple patches:

| Group | Minimal semantics to establish before implementation |
| --- | --- |
| Catalogue/application lifecycle | Finalized inputs, exact active DLC/provider identity, loaded-save overrides before decisions, teardown and setup flow boundaries; observe-only hook needs only the actual callable contract unless original execution matters |
| Ordinary generation | Exact candidate/weight/required-choice use for stock/rewards/resources/pawns/spawners/weather/events; rejection cannot cause endless retry; preserve prior exclusions |
| Production/construction | Product and required ingredient/cost flow, saved job resumption and native refusal reason path; no costs/ingredients consumed after rejection |
| Research/prerequisites | Current dependent and ancestral query semantics, independent requirements, invalid-chain handling and progress retention |
| Apparel/health/quests | Wear's actual garment argument, AddHediff's condition argument and state-changing body, quest-root use in add/letter paths; method signature alone cannot prove those argument semantics |
| Appearance/social/UI | Current selection routes, random versus explicit interactions, work-column versus job behavior, hidden filters versus their acceptance predicates, native tab/selector safety |
| Optional providers | Canonical package first, current types/members/Defs second; active content, not merely installed content; VEF/VPE/Ancients isolated from Core paths |
| Owned declarative preset changes, if selected | Actual owned nodes and member identities, external content requirements when used, duplicate-safe additive updates; no legacy-format/dependency-gate adapter |

Inspect original versus effective methods and any relevant prefix/transpiler ordering. A compatible
foreign patch or unrelated body edit is not grounds for rejection. If a prefix changes an argument,
validate the consumed data flow and required control-flow context. If an original can be skipped,
do not count its body as executed evidence. Every transpiler validates its actual incoming stream
on every run and returns it unchanged if incompatible; do not invoke foreign transpilers repeatedly
as speculative probes. Support later repatch invalidation/revalidation.

On false/unknown predicates, validator exceptions or install failures, keep the affected patch or
declared atomic family inactive and roll back only newly installed owned hooks. Log one bounded
startup warning per skipped enabled patch: package/patch/spec ID, target, predicate/reason, missing
behavior and fallback. Explicitly disabled candidates and absent optional providers do not emit
failure spam. Do not mark a row excluded if its essential boundary is inactive. Retain requests for
later compatible startup. Hashes in research identify evidence, never dictate runtime admission.

### Original-loaded warning, composition and data ownership

Original CP active: emit a bounded startup warning and invoke the ordinary per-feature settings
disable path for owned content selection, on a best-effort basis. Its effective off behavior and
state retention are exactly the ordinary user-disable behavior, not a separate cooperative mode.
Never modify the original's settings or patches. This does not certify what original mutation has
already done to native data, and requires no original API/binary/save parity or coexistence run.

Compose S020 selectors, S029 drug-catalogue reconciliation, S012 recipe browser, S014 whole-list
workbench linking and S036 Architect directly. Content blocking preserves S014's shared bill identity,
recoverable started work and quota accounting; no completion/credit is issued for a blocked job.
No self-patches. Prepare policy publication and the dependent drug-row reconciliation together.
The **drug-item exclusion group** requires effective, admitted owned S029 maintenance. If that
feature is off through its ordinary settings path or its required semantics are unavailable, withhold
this dependent group, retain requested reasons and show unavailable status without disabling unrelated
item families. Do not inspect or adapt original Drug Policy Fix as a substitute for this owned dependency.

For admitted owned maintenance, capture prior affected policy rows, prepare all relevant policy
results against the candidate drug catalogue, then publish rows and applied drug choices together.
If application fails, restore prior rows and leave that drug exclusion group inactive before player
actions resume. This is cross-feature orchestration around S029's per-policy atomic repair, not a
second maintenance implementation or foreign mutation. Tests must fail on a later policy to prove
earlier prepared/committed results do not leak a partial application. Successful publication causes
one coherent reconciliation; stock scans and later normal loads cannot resurrect excluded rows.
If restoration itself fails, native state is unknown: invalidate the affected current application
boundary and its prior applied reference before propagating the distinct restoration failure.
That token cannot retry publication, even without another native write. A genuinely newer boundary
must not be invalidated by an older boundary's restoration failure. Beginning a fresh boundary is
a native lifecycle responsibility after loading fresh state, not a UI retry that certifies or repairs
unknown rows. Clearing an applied reference does not mean restoration succeeded or gameplay is safe;
the native owner must surface that failure and stop claiming the affected policy is available.
S136 future floor sampling must respect current terrain eligibility without a second list of rules.
Physical Thing data remains map-owned and shared with search/hauling; exclusion is an availability
predicate, not deletion from the physical index. Inspect native listers first. Do not scan maps per
content row or allocate one cache per port. Clear policy/catalogue views at their respective
game/mod-list lifetimes; never persist live references or read Verse/Unity objects off-thread.
Benchmark combined consumers and invalidation before promising any optimization.

## Risks / Trade-offs

- **Breadth of 45+3 families** → implement one observable vertical slice at a time; no "complete
  Cherry Picker replacement" claim while any promised family lacks reviewed native evidence.
- **Source behavior changes existing items** → intentional preservation deviations are explicit;
  source fidelity does not require reproducing zero-nutrition, free-cost or lost-component defects.
- **Reload-only application is less immediate** → display pending state and effect timing in every
  relevant view, including global edits affecting inherited saves. No undocumented restart fallback.
- **Exclusions can remove all progression/threat choices** → explain dependencies, block impossible
  requests and preserve valid alternatives; do not impose an invented balancing multiplier.
- **Fixed supplies and incidental drops are intentionally reduced** → show omitted starting supplies,
  preserve allowed yields without compensation, and refuse empty-output butchering. This is content
  exclusion, not a claim that every acquisition leaves total resource yield unchanged.
- **Optional content dependencies on 1.7** → source support labels are not compatibility;
  exact absent/present/changed-provider evidence and semantic admission remain required for actual
  Ancients/VEF/VPE/DLC integrations, not original-mod replacement compatibility.
- **Legacy authored lists will not activate through this replacement** → intentional exclusion;
  no compatibility-format adapter, consumer parity audit or package/type spoof.

## Migration Plan

S068 composition clarification: thought acquisition exclusions do not remove existing memories or
their types from the fading catalog. Retained memories continue under current fading/expiry policy;
catalog rows distinguish applied acquisition restrictions, pending requests and fading support.
Use the applied S038 snapshot, not pending settings, in native acquisition decisions.

Native counselling reports success after a void TryGainMemory call, which can be refused. Direct
owned composition must correlate actual acquisition and report no granted bonus for refused
Counselled/Counselled_MoodBoost awards, without replacement compensation. If truthful reporting
cannot be admitted, an admitted native action guard refuses the affected counselling action before
cost/cooldown/effects. If neither route is supported, do not activate the coupled exclusion group:
retain requested choices, expose unavailable status and log the skipped patch/group. Never call an
exclusion applied while permitting a false granted-award report. Other content groups stay independent.
Exact targets/admission and reviewed-build native action evidence remain future work under S068/MF-Q2–4.

No automatic import or foreign-file write. Owned settings use stable keys, default inheritance and
dormant missing entries. Source `keys` strings are documented for a possible future opt-in migration,
which needs its own preview, backup, atomic write, verified postconditions and failure restoration
contract before implementation. Reverting the feature leaves existing physical entities intact and
owned requested choices dormant; it must not force other mods' restrictions back to defaults.

## Verification and Open Questions

CK-Q1–6 in the research and refinement ledger cover exact native seams, owned preset/content bindings,
proposed timing/dependency behavior, genuine optional integrations, future owned-data migration and
native acceptance. Earlier legacy-list compatibility questions are superseded by the no-adapter policy.
Use Unit for key/graph/policy calculations, Harmony for individually owned shape/admission tests,
Defs only for scoped constructed fixtures, and fresh isolated games for real loaded databases,
XML, providers and selection/generation/UI workflows. No fake active mods or copied Core Def state.

Retain RED/GREEN before each implementation; package/dependency/localization checks; independent
review; then personally observe actual reviewed-build UI/job/generation consequences through the
minimized Gateway workflow. Direct setup/spawning is precondition evidence only. Save switches,
blocked bills, existing-food consumption, native wear, quest offers, work columns and composed views
must be exercised rather than inferred from patch presence. Root config/PID/cleanup evidence applies.
Final balance pass checks research, acquisition, empty required sets, resource conservation and
unsupported-provider playability. Documentation validation cannot close those tasks.
