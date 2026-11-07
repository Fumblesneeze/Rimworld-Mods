## Context

Owner: **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`, RimWorld **1.7 beta**.
The product currently has an inert scaffold and 137 researched feature changes, not implemented
gameplay. The [foundation](../add-megamod-foundation/design.md) defines product boundaries; the
[reconciled decisions](../../../mods/megamod/docs/refinement/2026-11-07-decision-reconciliation-audit.md)
and individual feature specs define behavior. This design coordinates their future implementation.

The [roadmap](roadmap.md) assigns every feature to a primary chunk and integration part. The
[tasks](tasks.md) govern execution. Neither replaces the individual changes: intent stays in their
proposals, observable behavior in their specs, and implementation detail in their designs. This
planning change's capability covers delivery records and gates, not additional player mechanics.

## Goals / Non-Goals

**Goals:** one authority for shared facts, explicit lifetimes and freshness, independent development
within bounded assignments, and integrated acceptance by an orchestrator who can change the plan
when evidence changes. Reuse native mechanics and repository-owned helpers wherever suitable.

**Non-goals:** implementing gameplay in this change; a general mod framework; a runtime library mod;
copying upstream code; claiming compatibility from source inspection; enabling CE, multiplayer,
original-mod save import or portal containers. The initial new-colony scope still includes save/load
of colonies created with megamod. The user's generation-time estimate is not a code/test deadline.

## Decisions

### 1. Shared components have one owner and real consumers

Keep game-dependent implementation inside megamod. Use existing native databases/listers and
helpers before materializing derived data. Host-safe code genuinely needed across projects can be
source-linked from `shared/`; do not introduce dependencies on another repository product. Test-only
tooling references do not enter the shipped assembly or allowlist.

The following names describe responsibilities, not mandatory public interfaces or empty projects.
Create each component with its first concrete consumer and focused failing test. The listed chunk
owns its contract and implementation; other agents request changes from that owner through the
orchestrator. Central bootstrap, project/package metadata, common registration and language indexes
have one integration writer. Feature-local adapters remain under `Features/<Feature>/Compatibility/`.

| ID / owner | Responsibility and first consumers | Authority, lifetime and invalidation | Failure behavior |
|---|---|---|---|
| C01 / P0-A | Phased feature activation, target admission, startup diagnostics; S035/S094 and the first runtime hooks | Active package identities, documented target predicates and owned patch-group state; early loader hooks before their events, later hooks after their prerequisites. Revalidate supported repatches; transpilers validate every incoming stream. | Suppress only overlapping replacements or dependent atomic groups; one bounded warning per skipped enabled patch. Roll back only newly added owned patches. |
| C02 / P0-B | Setting definitions, global/save storage, effective resolution and common renderer; S034/S047/S071 | Stable setting ID, global revision and current save identity/revision. Global values and saved inheritance/override state are authoritative; UI is a projection. | Validate before mutation; keep documented last/default value on invalid input. No save callbacks at main menu. No private feature settings stores. |
| C03 / P0-C | Content identity/category facts, separately applied availability policy and drug-policy reconciliation; S020/S029/S038, later recipes/storage/pawn displays | Stable finalized Def catalog plus separately versioned requested settings and applied save-specific policy. Publish at new-game/load boundaries after overrides load; preserve native Def identity and existing Things. | Missing external content is unavailable; owned exclusions restrict only specified acquisition/production paths. Drug exclusions and policy rows publish atomically or remain inactive. Preserve documented external contracts only after consumer inspection. |
| C04 / P1-A | Common Thing candidate/query facts for S033/S121/S114, hauling, counts, recipes and inspection | Native listers are primary; materialize only missing shared projections. Map-instance generation, Thing identity and relevant revisions distinguish lifetimes. Clear on map/game teardown; rebuild after load. | Dirty/unknown data triggers a suitable native query/rebuild or an explicit unavailable result; never report a stale/empty result as authoritative. |
| C05 / P1-C | Physical portions, carried cargo, quantity provenance and scoped claims; S021/S041/S085/S126, later medical/production/loading | Actual Things, containers and native reservations remain authoritative. Durable portion provenance is serialized where required; transient claim receipts are not inventories. | Revalidate exact available quantity/capacity at action; release failed/cancelled claims, preserve physical conservation. No duplicate items, nutrition, doses or imaginary capacity. |
| C06 / P1-B | Linked storage membership, filter intersection, totals, refill thresholds and presets; S005/S049/S050/S098/S107 | Native shelf grouping semantics extended to stockpiles. Entity-owned configuration and current physical storage membership; invalidate on group/filter/stock/priority changes. | Recompute affected totals or use native facts. Validate destination constraints at delivery. A preset cannot create a selectable item outside the storage's real restrictions. |
| C07 / P2-A, with P2-B owning material planning | Logical bill identity/order, linked workbench quantities and shared recipe projections; S014/S017/S018, S026/S059, S009–S012 | Bills and recipes are authority; previews keyed by recipe, material, quality, effective policy and relevant content/stock revision. Material plans are proposals until a real job acquires ingredients. | Invalidate previews and replan on changed facts. Reject unavailable operations without changing other bills. Use C04/C05/C06 rather than a second recipe inventory. |
| C08 / P4-C | Medical request reasons, urgency, treatment eligibility, dose commitments and operation-effect matching; S062/S124/S134, S007/S137 | Native health, policy and job state plus explicit owned request records. Current patient/condition/body-part identity and treatment-state revisions. One authority arbitrates automatic care and manual takeover. | Cancel stale intents and release owned holds; preserve manual orders. Dose accounting uses C05. Unknown operation effects remain unknown; explicit owned rules supplement general/heuristic matching. |
| C09 / P5-B | Environmental safety facts and narrow provider adapters; S030/S031 and later arrivals/vacuum/hunting | Native current location, temperature, atmosphere, pawn equipment and capabilities. Share expensive measured facts only when useful; not a cached permission to enter a cell. | Missing provider support fails that adapter closed with documented fallback. Re-evaluate safety at the native job/action boundary. Earlier medical code reads native facts until this provider exists. |
| C10 / P0-B, extended by domain owners | Common scoped controls, formatting, search/filter presentation, reason/disabled-state display and localization conventions | UI reads domain-owned models; no widget owns medical, bill, cargo or settings truth. View-local transient search state is permitted. | Respect feature activation and native contextual entry points. An unavailable action has an honest disabled/absent presentation defined by its spec. |
| C11 / P7-C | Pending generation and publication lifecycle for S078 | Source intent, owned work identity, unpublished result and validity checks. Only publish a complete valid result; discard when the source/intent no longer exists. | Never publish half-generated work or issue an early notice. Cancel/discard owned work without deleting borrowed objects. No arbitrary real-time failure threshold. |

### 2. Shared data means shared facts, not shared decisions

**Thing indexing.** Inspect 1.7 native listers first. C04 exposes shared candidates and small query
projections; consumers define their own visibility, ownership and domain filters. Search cannot
reveal hidden objects because hauling uses the same index. A storage total does not automatically
include carried goods, inaccessible maps or every Thing of that Def. Every query states its scope.

Publish membership changes for spawn/despawn/destruction, movement, holder transfer, split/merge,
stack-count and relevant metadata changes. When an event cannot be reliably observed, invalidate the
affected projection and choose a documented native fallback; do not compensate with a private cache
in each consumer. Invalidation patches require target specs too. A cached snapshot can carry revision
tokens, but native reservations, reachability, forbidden/access rules and provider eligibility must
still be checked at the actual action. Paused UI actions must see current changes. Iteration must
tolerate a Thing disappearing during the action sequence.

Use the Unity/game thread for all Verse objects and mutable index updates. Immutable, detached data
may be processed elsewhere only when the owning design proves that path is safe. C11 does not grant
permission to run arbitrary map/Def/Unity generation on a background thread. It must inspect the real
generator, isolate safe work and stage thread-affine work while simulation continues. If that cannot
satisfy S078, report a design problem and replan; do not silently turn it into a loading pause.

**Quantities and claims.** C05 distinguishes physical amount, provenance, intended purpose and
temporary commitment. Inventory is not copied into an independent hauling store. Count every held,
equipped and carried load once under the feature's real carrying limits; visible cargo is a rendering
of that same load. Split/merge, partial ingestion, transport transfer and job cancellation preserve
provenance and conservation. A stock summary can expose on-hand and committed values separately;
each owning spec determines which enters its threshold. Do not subtract a native reservation and
an owned receipt twice. A claim records owner, Thing/portion, quantity, purpose and generation;
native reservations enforce actual jobs. Extra claims exist only for a demonstrated scheduling gap.

The core portion/claim contract has no dependency on storage or hauling policy. C06 reads physical
facts and supplied commitment records; C05's hauling feature subsequently asks C06 for destinations.
This staged dependency breaks the apparent storage–hauling cycle without duplicate ledgers. Rebuild
transient claims from actual jobs after load, or cancel/replan them according to the owning spec;
never trust serialized live references. Do not create a universal transaction engine: expose the
small reserve/commit/release operations that real consumers need, with failure and cleanup tests.

**Production.** P2-A owns logical bills, group accounting and execution state. P2-B owns ingredient
and material selection using the shared stock facts; P2-C renders that model in the native add-bill
workflow. All material comparisons use the specified quality/defaults and available/all-materials
mode. Right-click alternatives create the intended bill using a new explicit material choice, not a
different hidden planner. Bulk smelting preserves piecewise quantities. Reordering, partial work,
group changes and stock changes invalidate only affected plans. Item-processing jobs in P3-D use
the same portions and authorization facts; they do not patch our production code.

**Medical.** Consolidate Drug Response and Priority Treatment in P4-C. One request can have multiple
reasons without scheduling duplicate doses. Preserve policy, doctor capability, reserved medicine,
urgency and explicit player orders through cancellation, changes in condition and save/load. S137's
operation filter consumes a shared effect resolver: exact known effect, general/heuristic match, then
explicit owned exception rules, with the same user-facing presentation. Transitive body-part effects
must be represented. Keep concrete matching algorithms and upstream entry-point findings in the
medical designs; their observable outcomes stay in the specs. P4-D does not introduce a second care
scheduler. Individual pawn/condition orders are entity gameplay state; per-condition Drug Response
configuration follows C02's specified inheritance scope.

**Settings and presentation.** C02 stores stable definitions, validates edits atomically and resolves
inherited values against the current global default. Keep dormant save overrides. Paired constraints
such as minimum/maximum are validated as a unit; per-condition inheritance does not become one
checkbox for an entire feature. UI and callbacks disable save editing without an active game. Entity
configuration, such as storage thresholds, is not routed into settings. Shared UI fragments preserve
our debug/settings/contextual/bottom-tab/toggle decision tree and all seven baseline languages.

**Beds and temporary custody.** P4-A's S073 supplies one authority for portable-bed custody, temporary
rest and departure intentions. P5-B extends that same authority with S115's actual ownership/claim
transitions and one release-on-departure setting; it does not introduce a competing claim store.
The orchestrator explicitly transfers the provider edit lease for that extension and retains one
contract owner at a time. Native bed/owner facts can be a C04 projection when multiple consumers
need it. Preferences, suspended claims, temporary custody and current native ownership remain distinct.

### 3. Admission precedes feature registration and shared consumers

P0-A owns phased, idempotent activation coordinated with C02 and C03. Discover active providers and
overlap first. P0-B supplies the early global-settings load/resolve milestone without requiring its
renderer or an active save. Admit early loader/logging hooks before their events: S094 definition
context must surround the initial XML conversion, not start after catalogs have finalized. Admit
static XML/Def changes at their actual loader phase with XML Extensions package-ID/content/shape
guards, including original-provider suppression. A later constructor decision cannot undo XML that
already ran. Inspect actual timing and prove consistent C#/XML activation policy in real loader runs.

After Def finalization, build C03's catalog without removing Defs or zeroing fields. Admit later
runtime hooks when their required shapes/content exist. Registration is distinct from per-game
activation: S038 publishes its applied policy at new-game/load after save overrides are available
and before dependent generation/actions. During play, edited requested settings remain pending until
their specified application boundary. Policy-filtered projections carry the applied-policy revision;
the native catalog still contains excluded content, and existing physical Things remain valid under
the owning contract. Save switching cannot reuse another save's policy.

P0-C includes S029's drug-policy maintenance as a prerequisite for S038's drug exclusion group.
Prepare all affected policy rows and publish them with applied drug choices atomically, restoring
prior rows on failure. An active original DrugPolicyFix or an unadmitted maintenance group leaves
owned drug exclusions unavailable without disabling unrelated families. Do not implement a second
repair inside content selection. Register UI/jobs only for admitted capabilities and the current
applied policy; preserve native fallbacks at their documented scope.

Patches require minimal semantic target contracts, not exact binaries or a ban on foreign patches.
Original/effective-body assumptions, skip-original prefixes and incoming transpiler streams must be
handled as specified in the foundation. Feature dependency declarations distinguish required shared
capabilities from optional enrichments: disabling one replacement must not disable a shared service
still needed elsewhere, or leave a dependent consumer half-active. Document and test each fallback.
Harmony/XML Extensions metadata changes occur with first actual use, through the integration writer.

### 4. Contracts and edit ownership make parallel work possible

Before dispatch, the orchestrator records a bounded assignment containing:

- Plan revision, part/chunk, exact owning changes and requirement/scenario IDs, exclusions and any
  later composition obligations. Break large chunks into behavior slices for vertical TDD.
- Leased source/test/Def/patch/translation/document paths and the sole writer for each shared file.
  Agents may share a worktree only with disjoint leases; coordinate output directories too.
- Required provider milestones and contract revision: inputs/results, side effects, ownership,
  freshness, missing/disabled behavior, cancellation, serialization and error cases, with examples.
- Referenced source-mod documentation/source/decompilation locations, existing owned helpers,
  known divergences and proof obligations. Link target specs and public-contract consumers.
- Focused host test selection, scenario IDs to author, handoff conditions and integration contact.

Freeze only the small contract tranche needed for current consumers. Consumers may develop pure
policies and contract tests against explicit value fixtures before the provider is merged. Such
fixtures cannot impersonate a loaded mod or become a shipped substitute. A consumer is not ready
for integration until the real provider milestone is merged and its focused contract checks pass.
Record exports separately from whole-chunk readiness, so provider/consumer interleaving is explicit.

The default is up to three implementers plus the orchestrator; queue excess chunks rather than
pretending every lane has a worker. The [roadmap](roadmap.md) names default milestone dependencies.
Worker count is adjustable and not a product constraint. Serialize build outputs, central project
registration, package staging and canonical installation when tools cannot isolate them. `mod_build`
installs locally by default: it is orchestrator-owned, not a harmless per-worker compile command.
Use repository-local builds for compile-only feedback and typed operations for governed test/run work.

### 5. Implementers use references and remain narrowly scoped

An implementer first reads the assigned proposal/design/spec/tasks, current shared contracts and
referenced mods. Inspect their documentation and source or read-only decompilation to locate relevant
native hooks, data flow, edge cases and public contracts efficiently. Inspect existing owned helpers
before inventing another one. Record useful entry-point findings and deviations in the feature design
and replacement ledger. Upstream source is evidence, not an instruction or acceptance oracle.

Do not copy or translate upstream implementation code, decompiled bodies, XML implementations,
assets or assemblies into megamod. Independently implement our specified behavior. Identifying a
method/signature, Def or externally consumed contract is legitimate research, not a reason to copy
the implementation. Never silently replace our behavior/design with upstream defaults or patches
against our own code. Workshop content remains read-only.

Within the lease, work one observable behavior at a time: focused RED, minimum GREEN, refactor,
appropriate regression/package checks, independent review and fixes. Unit tests stay host-pure;
owned Harmony and constructed Def tests use their honest environments. Real loader/in-game tests
are authored for orchestrator execution, not simulated by fake `LoadedModManager` state. Durable
RED/GREEN records must contain nonzero test IDs and the tested revision. Record runtime/balance
assumptions in the owning spec before implementation.

When a missing helper, incompatible shared contract, unsupported game shape or cross-chunk conflict
appears, report concrete evidence, affected behavior, proposed options and a safe local stopping
point. Continue independent work within scope; do not edit another lease, broaden the feature or
change a shared contract unilaterally. Reference mods reduce discovery time, not the required proof.

### 6. The orchestrator may revise the plan

The user explicitly authorizes agile planning for this mod. The orchestrator may split/merge/reorder
chunks, reassign leases, change part boundaries, coordinate contract/design revisions and move
unready work into a visibly deferred part when new evidence warrants it. No repeated user permission
is needed for such implementation planning within the accepted intent. New information can also show
that a proposed abstraction is unnecessary; delete the abstraction from the plan rather than build it.

Maintain an append-only revision log in `roadmap.md`: evidence and reason, old/new allocation or
dependency, affected feature/spec/design/scenario IDs, contract revision, invalidated handoffs/test
evidence, assignment notices and remaining work. Update `tasks.md`, affected designs and refinement
progress together. Supersede old assignments explicitly; pause affected work at a safe boundary until
the new contract is acknowledged. Never have two active contracts for the same shared capability.

This authority does not waive root safety, test truthfulness, personal live inspection, package or
ownership requirements; silently drop requested features; reverse a user decision; or use an easier
test oracle to hide a defect. Genuine unresolved product choices are recorded for the user after
research, while independent work continues. A recurring missing automation capability gets a separate
tooling-owned change and typed operation, linked as a dependency, not an ad hoc public script here.

### 7. A handoff is not acceptance

Every chunk hands over a durable record under ignored artifacts with this minimum content:

| Field | Required evidence |
|---|---|
| Identity | Plan/contract revision, chunk, source revision plus relevant dirty-file/package hashes, exact path ownership and feature IDs |
| Coverage | Every assigned requirement and feature task mapped to host tests, native scenario IDs, later composition gate or explicit unresolved blocker |
| Host results | Focused nonzero RED/GREEN, compile/check results, honest test environment, independent review findings and dispositions |
| Reference/admission | Inspected upstream entry points, independent implementation deviations, target-spec IDs, active-provider suppression and optional-adapter cases |
| Scenarios | Executable native scenarios with fixtures/prerequisites, exact ordered mods, player actions, visible expected outcomes, supporting queries, capture points and cleanup |
| Integration | Consumed real provider milestone/revision, cross-contract checks, registered scenario selection/dry-run result, central registration requests and known risks |
| Status | Ready for integration, blocked or returned for fixes; never accepted based on authored scenarios alone |

Scenario authoring includes a native action-to-result path, not just direct state assertions. Setup
may arrange disposable preconditions; it cannot create the claimed outcome. A scenario record names
the actual control/job/designation and observable before/action/after checkpoints. If the Gateway
cannot exercise it faithfully, record the exact gap and tooling dependency. Compile/list/dry-run
evidence only proves registration, not gameplay. Do not invent passing scenario runs.

Existing feature tasks remain authoritative. If a task combines implementation and live acceptance,
leave it unchecked at handoff, or split it into traceable implementation and acceptance subtasks in
that change without losing obligations. Only check the implementation subtask with its own evidence.
Replacement status becomes implemented-unverified; accepted and verified await the orchestrator.

### 8. Part gates belong to the orchestrator

After **all chunks selected for a part** are ready with real dependencies integrated, the orchestrator
freezes the candidate build and completes scoped independent integration review. Review fixes require
affected host tests/checks again. The orchestrator builds/installs the exact candidate using existing
typed operations, validates package/dependencies, and selects stable in-game/E2E scenario IDs. Inspect
`operation_list` for current arguments rather than inventing a new orchestration surface.

Then run actual loader/integration checks and native workflows in fresh minimized 1.7 processes,
with unique disposable save data/logs and the smallest exact mod lists proving each claim. The
orchestrator personally inspects the relevant live view or causal before/action/after captures from
those exact processes. Startup, JSON assertions and prepared-scene screenshots are supporting
evidence only. Include selected combined workflows from the roadmap, save/load and relevant
original-provider/optional-content combinations. Prior accepted components used in a new composition
are exercised again on this build; old evidence is not evidence for changed runtime behavior.

Implementers do not independently launch acceptance games. An earlier orchestrator-run diagnostic
may investigate a blocking engine question, but does not accept the part or replace its final run.
Respect user stop/no-launch requests. Capture build/package identity, ordered mods, action sequence,
observable results, screenshots, supporting logs/request IDs, normal ModsConfig hashes before/after,
and cleanup. No tokens/current-session secrets in durable records; exact-process graceful cleanup.
Asset-bearing features retain the separate required visual/asset review gates.

For each failure, record scenario/build identity, expected versus observed behavior and causal
evidence. Route product defects to the owning chunk, scenario defects to their author, and recurring
tooling gaps to their separate owner. Fix through affected tests/review; do not change expectations
unless the owning contract is legitimately corrected. The orchestrator integrates the fix, makes a
new candidate, and reruns affected feature and composition scenarios. Block acceptance while any
required scenario remains unverified. Do not replay a full repository suite after each unrelated fix.

Finish the accumulated playability/balance review using the repository skill, update coverage and
feature task state, and record the accepted part manifest. All included runtime revisions need valid
evidence; later changes invalidate affected acceptance and reopen its tasks. A final release review
uses the release workflow and deliberate regression matrix separately. No publication is authorized
by completing a part.

## Risks / Trade-offs

- **A shared cache becomes a second game database** → Native authority, explicit query scope,
  measured derived projections and current validation at use; benchmark combined consumers,
  invalidation/update cost and memory before claiming a performance improvement.
- **Large chunks hide sequential bottlenecks** → Export small prerequisite milestones, keep vertical
  TDD slices, and let the orchestrator split based on evidence. Do not split ownership of one ledger.
- **Upstream assumptions survive the consolidation** → References locate hooks; our scenarios and
  direct policies decide behavior. Record conflicts and omissions per source in the replacement ledger.
- **A final integration run reveals many failures** → Focused host/contract checks and scenario
  registration before handoff; bounded parts, explicit failure routing and review before each run.
- **1.7 beta or XML loader timing invalidates the plan** → Reinspect actual binaries/tooling and
  semantic admission, replan dependencies; do not accept from version strings or old build evidence.
- **Generation cannot be safely detached** → Inspect thread affinity early in P7-C research, stage
  safe work and native continuation carefully; keep unsupported outcomes open instead of faking them.

## Migration Plan

This change modifies documentation only. Future P0 checks current tool/profile/package availability,
including the historical 1.7 inventory blocker, before runtime work. Do not assume the old failure
persists or bypass it. Runtime installation is orchestrator-owned and uses canonical local packaging;
Workshop inputs remain untouched. New-save defaults and owned save serialization are delivered with
their first consumer, with current-save round trips at part gates. Importing original-mod state is
not part of this plan. Roll back a failed candidate within the disposable run/build workflow; do not
test migrations on the user's normal save or configuration.

## Open Questions

Resolve implementation facts from source inspection at the relevant contract tranche: native shelf
linking interfaces, exact loader timing for guarded XML suppression, safe Thing invalidation hooks,
portion provenance persistence, operation-effect coverage and generator thread affinity. Record
findings in owning designs and target specs. None is an invitation to ask the user to explain existing
mods. Genuine product ambiguities stay in the refinement question register; unresolved acceptance
requirements remain unchecked.
