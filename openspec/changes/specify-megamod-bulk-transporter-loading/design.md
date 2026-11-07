## Context

Sole owner **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`; target1.7 beta.
S133, specification only. Read [research](../../../mods/megamod/docs/refinement/2026-11-07-bulk-load-transporters-research.md),
[coverage](../../../mods/megamod/docs/replacements/bulk-load-transporters.md), the foundation and
S021's [composition record](../../../mods/megamod/docs/refinement/2026-11-03-hauling-composition-decisions.md).
Source `Ilarion.BulkLoadForTransporters` is not an owner or dependency. All runtime work is pending.

Public Steam/repository documentation was inspected before complete installed DLL/JobDef analysis.
The installed flat189440-byte DLL has SHA256`aa46929c3cfe4e27528f6fbf877f160b3e18783f1ed1349e7a4f408bfb4a19e1`.
Native comparisons use1.7.5000rev1275. Exact hashes identify evidence, not compatibility admission.
The user's 2026-11-07 instruction excludes portal loading; preserve its source analysis only as a
documented omission. Do not implement portal hooks or reuse a generic container path to include it.

## Goals / Non-Goals

**Goals:** useful multi-item loading/unloading within actual capacity, exact selected cargo,
coordinated workers, trustworthy completion, native permissions, contextual UI, safe recovery and
one shared data model. Preserve consumed contracts only with demonstrated consumers and real semantics.

**Non-goals:** portals; vehicle adapters without evidence; extra hand allowance; mass cheats;
teleporting goods; new worker permissions; a second inventory/index; internal Harmony composition;
automatic original-mod migration/removal; blanket save-removal, multiplayer or fork compatibility.
S135 separately owns Basic Caravan Loading's work-category changes.

## Decisions

### Own a transport workflow over S021 cargo

Use native `CompTransporter`, its group/manifest and destination rules with narrow direct owned
loading policies. S021 owns physical portions, protection, combined capacity and recovery; S126
renders those same portions, S132 displays them, S125 bounds any normal opportunity. S062/S123/S124
supplies remain protected. Reuse native listers and the shared map item index used by search; no
per-source all-map scan or separate backpack. Cargo metadata may refine an actual stack into
protected/committed/free quantities; never expose a whole-stack HashSet facade as equivalent.

Reject copying `CompHauledToInventory`, source per-Def claim dictionaries and planner hand overflow.
Use stable destination kind/map/group/request-revision identity; group integers alone can collide.
Keep destination selections, exact native transferable distinctions, physical item portions,
reservation ownership and committed versus accepted counts separate. Include admitted native
loaders as commitments, not just owned jobs; model each queued job independently. A quantity is
claimed only after its job/reservations are valid and released exactly once on lifecycle changes.
Do not overload `Job.loadID` with continuous-mode flags. Contextual intent is serialized job/order
state, not global configuration. Until-complete stays with the selected request, unlike the source's
ten-cell scan that can drift between nearby groups.

### Plan bounded work, then revalidate real actions

Candidate discovery may use the shared index; eligibility remains pawn/request specific on the main
thread. Validate reachability, allowed areas, forced/native permission semantics, danger, forbidden
state, holders, reservations and capacity at commit and pickup. Honor held/container sources through
their real access/removal path and positioned holder; do not drop one arbitrary item as a shortcut.
Live pawns retain native boarding/carriage, not batched inventory. Unsupported workers retain the
native job path rather than a fake two-tick wait. A short expensive-path shortlist is an internal
budget, not a reason to declare farther reachable cargo absent. Bound total planning and use a
continuation/fallback that preserves reachability. Do not stop all planning at the first heavy item.

The native transport request governs source/destination eligibility, including selected cargo
already in best storage. S021's ordinary better-storage destination and extra-pickup exclusion do
not govern this separately authorized transport workflow. Share its capacity, protection, access,
reservation, finite-collection and recovery policies; this does not broaden ordinary storage hauling.
S125's opportunity constraints remain authoritative for its separate opportunistic work. Do not copy the source's
independent90-cell scan or its150-cell value labeled Unlimited. For explicit orders, deliver only
eligible free cargo for that request first; do not redirect to another group's nearby container.
No new opportunity setting or second detour scheduler is introduced. There is no claim of optimal
packing/routes or measured speedup until combined-consumer benchmarks exist.

### Account for actual transfer outcomes

Preserve native selection identity through splits/merges; `FindBestMatchFor`'s same-Def fallback is
insufficient. Use strict request equivalence or explicitly broad quest demands, not a blanket
same-type test. Reserve destination capacity per accepted cargo portion, accounting for actual
contents and concurrent incoming work. Retain native notifications, mass invalidation, quest signals,
boarding and completion semantics. Update the manifest once based on actual accepted quantity, not
the merged stack's total or a requested amount. Prefer existing native transfer/accounting paths;
any correction must prove the consumed semantics and preserve the return value and notifications.

Native `SubtractFromToLoadList` now returns int, with transporter's optional `sendMessageOnFinished`;
source prefixes skip it without assigning that result. Do not copy reflective writes to old private
count/edit-buffer fields. Native dialog distribution's ceil share can exceed the last positive
remainder for some counts; specify exact totals and fitting per-member allocation, not the source's
postfix that clears all requests then permits overcapacity when none fits. Plan an entire revision
before applying it; retain old request/cargo on failure. Loaded contents are not physically moved to
make a proposed arrangement appear successful. Expose a revisable packing-blocked result when the
bounded allocator cannot establish a fitting arrangement. Preserve actual native request edits,
including surplus recovery, without introducing resources or unrequested variants.

An owned per-portion handling step uses3ticks plus native destination duration. Source15 cosmetic
ticks and carrier15 replace neither native handling nor S021 accounting. Carrier extraction retains
native10ticks, followed by shared delivery handling. Constants remain named and spec-linked; no
extra tuning UI for incidental planner limits or duplicated hauling timing.

### Carrier extraction and lifecycle

Gate through native `UnloadCarriersJobGiverUtility` and the actual unloadable portions, not every
entry in `inventory.innerContainer`. `FirstUnloadableThing` includes drug-policy, inventory-stock
and food protection; combine with owned protection and avoid sequentially reusing stale results.
Exclusive extraction must coordinate reservations and recheck the carrier's current job/unload
intent without immobilizing it unnecessarily. Specialized `CompMechCarrier` keeps native behavior
until an adapter is explicitly specified. A missing batch capability cannot suppress native unloading.

Use owned job finish/queue/cancel/map teardown hooks as narrowly as possible, and S021's600tick
unchanged-failure recovery. Never discard tracking before an irreversible transfer or erase it after
an unconfirmed failed drop. Saving persists physical cargo/order state; reconstruct derived claims
from actual jobs/items after load. Do not patch global `ScribeSaver.InitSaving` to interrupt loaders.
No broad1800tick all-colonist cleaner that drops whole tracked stacks. Disabling new work leaves
the recovery path available; failed startup admission must also reconcile saved owned cargo safely.
Unknown saved jobs remain suspended/recoverable, not resumed under unproved native assumptions.

### UI and settings ownership

Add no bottom main tab and no runtime-injected components to every transport Def. Use admitted
native/contextual menu entry points plus owned JobDefs. Single and until-complete orders are entity
state. Common settings define initial pod/shuttle batching, initial carrier batching, auto Contents
and auto carrier Gear, all true. The bottom-right hauling control holds explicit current-save
operational choices with reset-to-default; otherwise effective defaults follow inheritance. Shared
settings render Global default / Current save / Use global; only the save editor is disabled while
inherited, while all save callbacks are disabled at main menu. No source global-settings copy.

Selection-driven auto-opening is one scoped event, with subject/selection generation validation.
Use the actual current inspector tab (including admitted S132), not hardcoded base Gear type on a
provider-replaced inspector. The source selects trader-caravan-role carriers; native unloading allows
different faction/host combinations. Restrict owned auto Gear to an eligible controllable carrier
subject, never hijack an unrelated merchant pawn's selected tab. Keep later manual tab choice.
Debug categories are top-bar controls only. Seven-language labels, reasons, help and current-mode
indicators are required. No mass-bypass switch is selected, even as a development convenience here.

### Contracts, dependencies and source overlap

Complete installed source inspection found public `ILoadable`, `IManagedLoadable`, `IBulkHaulState`,
`Extensibility_Utility.AdapterFactories`/`OpportunisticTargetScanners`, `JobDefRegistry.OnRegisterDefs`
and job-registration sets/map. However `CreateGroupAdapterFor` hardcodes pods/portals, and the event
fires once during static initialization. These APIs are not a proven general vehicle contract.
Bounded installed XML/C#/assembly-name search found no incoming consumer; that does not prove none
exists. Preserve conceptual extensibility with narrow proven adapters; no speculative public registry
or legacy binary facade. Vehicle package flag and serialized setting alone do not establish support.

Core/Harmony required with implementation; transport-specific DLC optional. Original
`Ilarion.BulkLoadForTransporters` active withholds owned overlap. S021's upstream hauling-provider
selection/admission remains authoritative; no external fork is assumed safe just because its
namespace matches. Foreign Harmony owners can coexist when semantic predicates hold. Source MIT
license is documented; no assemblies/assets copied and no source settings/job migration selected.
Owned JobDefs use independent stable IDs. Any new declarative external change uses XML Extensions
package and Def/node guards, additive operations and matching required metadata, never runtime Def
mutation or whole-list replacement. The current design needs no source-style comp injection.

### Candidate target specs and admission groups

These are design candidates, not completed target documents or installed patches. Every actual
patch receives a stable indexed target document and executable predicate before implementation.

| Group / candidate | Minimal semantics and failure boundary |
| --- | --- |
| Load entry/menu | `LoadTransportersJobUtility` and native workgiver option path preserve request, worker and job result; replace only admitted item batch work, retain native passenger/unsupported paths; unavailable bulk menu must not suppress native option |
| Request/accounting | `Dialog_LoadTransporters`, `CompTransporter` add/subtract/merge notifications retain selected distinctions, actual transfer evidence separately from the int demand-subtracted result, mass invalidation and quest callbacks; all required hooks admit atomically with batch loading |
| Shuttle refresh/boarding | `CheckAutoload`, sendable/in-transit enumeration and boarding logic preserve current requirement refresh and actual cargo; do not flip `autoload` globally or block on stale claims; fail shuttle batching only if its additional group cannot admit |
| Carrier extraction | Native unloading eligibility/portion/extraction preserve intent, protected quantities, reservations and duration; fail carrier batching independently and leave native option |
| Lifecycle | Owned jobs/queues preferred; any native lifecycle observation must prove invocation/result timing including skip-original behavior, map identity and cleanup ownership; recovery prerequisites belong to the dependent atomic group |
| Inspection | Selection callback and inspector-opening shape/subject correlation; auto-open failure does not disable core loading |

Admission considers effective code and relevant prefixes/transpilers, supported late repatching and
actual incoming transpiler streams. Transpilers validate every run, tolerate unrelated edits and
return input unchanged on failure; never execute foreign transpilers speculatively. False/unknown
predicates, exceptions and installation failures disable the declared dependent group and remove
only newly added owned patches. Startup warns once per skipped enabled patch with package/patch/spec,
target, failed predicate, unavailable behavior and fallback. No exact-hash or no-foreign-owner gate.
Portal targets are deliberately absent from this table and must remain outside generic enumeration.

## Risks / Trade-offs

- Exact variants and native notification side effects → focused split/merge/partial-transfer tests
  plus actual loading, request editing and launch/quest outcomes in the reviewed beta build.
- Native/source claim disagreement → coordinate both current and queued native/owned jobs; do not
  claim compatibility solely because no exception occurs.
- Shared quantity model can expose old whole-stack integrations → fail unsafe adapter/new batching
  closed, retain real cargo/native controls and document unavailable bindings.
- Greedy routing/distribution is not globally optimal → bounded work and honest unresolved packing,
  without silently overfilling, inventing missing cargo or starving farther reachable candidates.
- Broad source compatibility claims → vehicle support deferred; exact optional content/worker
  provider runs required for any later claim, no blanket robot/PUAH-fork assertion.

## Migration Plan

No migration is implemented or promised. Existing source jobs/settings remain the source's domain;
do not import them, spoof IDs or mutate Workshop content. Owned schema is separately versioned;
new state starts at documented defaults. Native-save continuity and disabling owned batching require
reconciliation tests, not an unsupported assertion that removing the whole mod is safe. A future
import/removal workflow needs its own backup, atomicity, rollback and verified-postcondition contract.

## Open Questions

- BLT-Q1 resolved for source inventory; portal exclusion confirmed. Runtime remains pending.
- BLT-Q2: finalize exact beta effective-code predicates and smallest atomic groups; reproduce source
  distribution and partial-transfer risks without treating static inference as observed failure.
- BLT-Q3: actual external vehicle/worker/provider contracts and consumers, if later selected; no
  vehicle support or legacy facade is accepted by this draft.
- BLT-Q4: combined S021/S125/S126/S132 and supply-state workload/recovery evidence, including native
  loader coexistence and performance costs. No duplicate scheduler/cache or expanded capacity.
- BLT-Q5: seven-language native controls, save/reload, disabled modes, source co-installation and
  actual loaded cargo/boarding/launch acceptance on a fresh reviewed build.
