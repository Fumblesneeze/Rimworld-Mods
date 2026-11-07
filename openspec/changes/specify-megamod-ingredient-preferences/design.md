## Context

Owner **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`; target **1.7 beta**.
S059 Best Mix (Continued) reference 1.6.2 has 28 menu modes, shared bench/per-bill state, global options,
125-bill UI patch and an actual reflected button consumer. Its binary and current public bundled
binary match; native 1.7.4998 behavior was inspected separately. See
[research](../../../mods/megamod/docs/refinement/2026-11-04-best-mix-research.md) for hashes, complete
inventory, dormant code and source/license boundaries. No runtime implementation is selected here.

## Goals / Non-Goals

**Goals:** retain meaningful ingredient choices, direct S026/S024 composition, shared stock facts,
complete physical quantities, entity inheritance, coherent capacity, proven narrow consumer support
and recoverable unavailable choices.

**Non-Goals:** whole-map optimal recipe solver, parallel bill scheduler, source component/save
impersonation, silent input filter edits, hidden global context, irrelevant debug/radius controls,
automatic foreign-settings migration, copied asset bundle or unverified universal multiplayer support.

## Decisions

### 1. Attach choices to the existing owned bench and logical bill records

Use typed stable criterion IDs, bench default, bill inheritance and dormant override in existing
owned entity state. No source `uniqueLoadID;mode` parsing as the primary format, no runtime Def comp
injection and no new independent association/cache framework. Native-preserved bench identity retains
defaults through minification; logical linked bills retain shared state through S014. Source-selected
link preview includes the group default; rollback includes all changed entity choices.

S017 snapshots include the bill inheritance flag and dormant criterion. Rebinding an inherited copy
to the destination default is explicit in preview; do not silently materialize it as an override.
Source code lacks this copy integration, so full snapshot semantics are a deliberate improvement.
Keep unknown owned criterion IDs losslessly for recovery. Source presence does not migrate state.

A new Default sentinel contributes no ordering; explicit Nearest remains a real criterion. Default
is the bench's initial choice, unlike source DIS which also overwrites native mixing sort. Inherited
bills start from the bench. No selector uses the source's misleading `(No Change)` semantics.

### 2. One selection plan composes S026 and S024

Before mutating an ingredient sequence, resolve bill/group, committed-versus-fresh iteration,
effective settings, native candidate set, output-material role and explicit locks. Apply one ordered
policy: eligibility/lock; S026 output-material score if applicable; S059 explicit criterion; S024
perishable rank among remaining ties; native distance/discovery ties. Secondary ingredients skip
S026; Default leaves ordinary native value/distance ordering and other enabled policies intact.
Nearest and Random deliberately take their place before S024 ties. No later independent Harmony
sort resets the result. The S024 linked-group candidate expansion remains separate and occurs before
selection even if S059 controls ordering. Avoid rewriting surgical/pawn-bill order entirely.

Per-item criterion facts are evaluated once per selection snapshot, not in a comparator repeatedly
walking all map stock. Supported zero is meaningful; inherently inapplicable values form a trailing
neutral class. Missing a required property/provider invalidates an active choice instead of inventing
zero. Use current ingredient Beauty alone; do not add a multiplicative Stuff factor as if additive,
or misrepresent it as the finished product's Beauty. S026 already owns product comparisons.

Expiry uses native current-temperature prediction; normalize no finite expiry to the last class,
not a finite 72-million-tick pseudo-deadline that could outrank a sufficiently long-lived food. Preserve
ordinary actual rot eligibility. Damaged/Robust compare HP fractions and do not turn absent HP into
damage. Non-mix Def grouping still needs sufficient total allowed units, and within each type follows
the eligible stack order. Use native/shared quantity planning with explicit per-slot remaining units;
do not let independent requirements claim the same units or silently alter whole-stack rules.

Native mixing's ceil(value requirement/value per unit) can overconsume fractional value differently
under a preference. That consequence is observable in MX05, not an implicit free-efficiency bonus.
S014/S021/S024 retain actual chosen portions, output material, quotas and capacity through travel,
cleaning and reload. Do not infer dominant material from which item a route happens to deliver first.

### 3. Shared stock query with explicit source-like scope

Use the shared map Thing-index/native lister owner already serving S033/search and hauling. The
S059 query groups by ThingDef (not Stuff/quality) and counts spawned same-map units under radius,
forbidden/fire/fog and optional valid-storage predicates. Query keys include map, current scope,
bench/radius and storage option, with live snapshot epoch; aggregate each Def once per selection,
not twice per comparator invocation. Native listers may already make additional retained storage
unnecessary. Any retained derived aggregate belongs to the shared subsystem with measured need.

Counting scope intentionally excludes pawn inventories and container-only items, matching the
selected source's spawned list. Do not call this a complete colony inventory. Haul-source candidates
can still come from native 1.7 discovery: resolve MapHeld/PositionHeld and authoritative container
access rather than dereference source t.Map/t.Position assumptions. The stock signal for their type
can be zero without invalidating the actual candidate. A future broader count is MX-Q2, not silently
introduced here. Existing S017 product-count semantics are a different query, not an excuse to reuse
an incompatible total.

Refresh spawn/despawn/transfer/split/merge/count, map lifecycle, forbid/fire/fog/storage eligibility
and scope changes through the existing owner or on-demand authoritative reads. Keep pawn-specific
reach/reservations/allowed-area out of cached eligibility; those remain current native checks. Do
not access Verse/Unity off-thread. Benchmark combined users, writes and memory before optimization
claims, especially repeated stock sorts and large candidate sets.

### 4. Randomness is scoped to the attempt and actual commitment

Use a dedicated attempt seed and stable per-candidate random keys (or an equivalent uniform ordering)
so adding candidates preserves prior relative order. Do not use a comparator that generates fresh
random values or repeatedly shuffle a growing list. Define attempt lifetime across the native
found-all probes; abandon it only when the selection attempt ends. UI reads never draw random values.
Random applies within higher-priority groups; it does not randomly bypass sufficient-type or recipe
requirements. Stable physical identity breaks the vanishingly rare key tie without data duplication.

Once an iteration commits, persist its actual portions and material through the existing physical
work owner, not a new parallel list of fake supplies. Resuming committed work does not reroll. Any
replacement of lost supplies follows the established committed-policy/recovery contract with current
eligibility; it does not replay the original quota claim.

Source multiplayer synchronizes two setters but patches a random helper not used by Shuffle.
Retain that as incomplete research only. Decision2 defers all Multiplayer/CE adapters, properties,
synchronization and acceptance to a future separately scoped change. No initial task establishes
an admitted multiplayer session and no source helper-library DLL is bundled.

### 5. Contextual UI and common configuration

Use the owned bench gizmo and bill ingredient/view composition; no broad Bill.DoInterface EndGroup
injection. Show inherited/explicit/effective identity, applicability and recovery. A menu cancellation
has no state mutation. Missing optional stats are absent-safe and become choices only after package,
Def and stat-meaning validation. CE electric uses `CETeam.CombatExtended` plus
`StuffPower_Armor_Electric`; softness accepts `JPT.SoftWarmBeds` or `Mlie.JPTSoftWarmBeds` plus
`Textile_Softness`. Current installed softness continuation identity is confirmed; finalized 1.7
support remains open. A matching Def name alone cannot establish a supported foreign contract.

Use one shared settings definition/load/persistence/render path for the five MX09 configuration rows,
including main-menu mutation guards and live inheritance/dormant overrides. Bench and bill choices
are entity state and never appear as mod-setting rows. No new operational master-toggle button is
introduced; these rows configure feature participation/default behavior. Disabling never erases
entity choices or clears existing work. Source hidden debug/radius/position controls remain omitted;
future genuine debug actions follow the top debug toolbar rule.

No raster assets are selected or copied in this phase. Runtime icon work must use the repository
asset workflow, including any selected existing player-visible raster; translate all new UI/compat
tooltip text in seven baseline languages. Native readable controls must remain available if an
optional UI adapter fails.

### 6. Coherent 125-production-bill capacity

S017 already requires one shared capacity gate. Add a named 125-bill extension with common default true;
false uses native 15 unless an explicitly supported other provider owns the capacity policy. Scope
to supported production workbenches, deliberately not medical/pawn lists. Validate Add, quick-add,
clone, complete paste and link before mutation. AddBill itself has no native 15-bill enforcement, so a
DoListing literal replacement cannot implement this contract. Do not mutate all literals 15.

Decreasing limit only blocks new additions; existing rows, work and list controls remain. A group
transaction must validate all members and restore all affected lists/defaults on failure. External
limit coexistence requires one explicit supported result, not automatic max/min between unknown
providers. Capacity admission is independent of ingredient preference admission, but never activate
125 if supported mutation paths disagree. Failure retains existing rows under remaining capacity.

### 7. Preserve the proven external button contract narrowly

Provide `BestMix.BMBillUtility` with public static
`Texture2D GetBillBmTex(Thing,Bill)` and `void SetBillBmVal(Thing,Bill)` as a small facade over owned
state/UI. The actual Nice Bill Tab Expansion 1.7 consumer uses type-name reflection and these exact
signatures; it needs no source package/assembly name. Preserve `BestMix.SelectModeDesc` as a localized
tooltip alias. Getter is read-only and returns null for unsupported targets; setter validates the
same targets and opens the actual selector. No comp/settings/string-record facade is justified.
Keep the namespace shim isolated from owned implementation and validate its real external caller.

With source package `mlie.bestmix` or `pelador.bestmix` active, owned overlap is disabled. Duplicate
full type names must not create a recursive or nondeterministic behavior split: if our shim is
resolved, it forwards only to validated methods from the actual source assembly explicitly excluded
from its own resolution; if the consumer resolves the source type, the ordinary source action runs.
No blind global AccessTools.TypeByName from the shim. Unsupported forwarding returns no icon/no
mutation with bounded diagnostic; source's ordinary UI remains. No source removal, config edits or
foreign unpatching. Owned active incompatible choices retain a separate recovery/hold path.

### 8. Target admission and safe recovery are one design

Author/index actual per-patch target documents before code. Candidate atomic groups below are a
design inventory, not existing executable validators:

| Group | Minimum semantic contract and failure boundary |
| --- | --- |
| MX-SELECT | Actual bill/giver/attempt context, eligible candidate sequence into mix/no-mix counting, sorted-flag meaning, value/rounding/remaining counts and downstream dominant material; Default/medical/native path untouched |
| MX-STATE | Owned bill/bench persistence, copy/link/iteration identity and durable unknown-choice retention; no feature activation without persistence |
| MX-UI | Actual current entity/view lifetime, safe selector, recoverable Default/Disable route; adapter failure does not remove ordinary controls |
| MX-CAPACITY | Current logical destination/count and complete pre-mutation gate for every supported add/copy/link entry; preserve existing rows independently |
| MX-OPTIONAL | Canonical package and consumed property/API meaning; local absence inactive, drift fails only affected stat/UI/multiplayer branch |
| MX-DATA | Only concrete notifications needed by shared owner, current identity/map/physical lifecycle and safe native fallback |

A signature alone cannot justify rewriting arguments or removing native sort. Check structural
incoming flow, operands/cardinality and caller behavior, tolerating unrelated edits, locals and other
owners. Inspect effective code/skip-original interactions, not only original IL. Every transpiler
validates its actual incoming stream and returns it unchanged on false/unknown; never probe by
reexecuting foreign transpilers. Supported later repatches invalidate/revalidate before use.
No static unscoped curGiver: explicit owned context or exception-safe nested scope if unavoidable.

False/unknown/exception/install failures roll back only newly added owned group hooks. Initialization
is idempotent. One bounded startup warning per skipped enabled patch includes package, patch/spec ID,
target, failed predicate, unavailable behavior and fallback. An enabled explicit choice cannot
silently fall back to other consumption. Admission includes an independent hold and recovery UI; if
selection cannot be installed, retain unknown state and suspend affected new work while preserving
prior player suspension and committed cargo. Default/off bills continue remaining native/owned work.

## Risks / Trade-offs

- Explicit preferences can consume scarce/valuable items or increase rounding waste → current units
  and directions, unchanged filters/requirements and measured recipes; Default initially avoids a
  hidden global consumption change.
- Scoped stock totals can surprise with inaccessible or contained stock → exact scope/zero meaning
  in help, separate availability and optional future extension; do not broaden current behavior silently.
- New linked default changes multiple benches → existing source/target preview and atomic group
  validation, current iterations frozen to committed policy.
- Large limits and stock analysis can increase UI/search cost → shared facts and focused combined
  profiling; do not treat 125 as proof of acceptable performance.
- Optional type-name facade collides with original → explicit source-assembly forwarding and
  absent/present/drift tests; no namespace-only claim of general source replacement.
- Source license notices disagree and art is bundled → independent engineering; resolve actual
  license coverage before any future reuse.

## Migration Plan

No runtime deployment, normal-save edits or source-settings import here. Implement owned state and
recovery first, then vertical criterion/UI/selection/capacity slices with RED/GREEN and admission.
Legacy migration needs a separate preview/backup/atomicity/restore/postcondition contract. Disabling
restores remaining selection for future work and preserves rows, entity choices and physical items.
No general safe-uninstall claim. Package/review and causal native runs follow root AGENTS.md.

## Open Questions

MX-Q1–4 in the [ledger](../../../mods/megamod/docs/refinement/specification-progress.md) retain selected
UX/meaning review, stock/shared-data scope, exact external/multiplayer contracts and finalized runtime
admission/balance/acceptance. All runtime tasks remain unchecked.


## Initial support scope — decision2

Multiplayer and Combat Extended adapters, synchronized sessions, CE-only controls/properties and
provider acceptance are deferred. Any source CE/MP implementation details above are retained research
for a future scoped change, not initial work. Implement and accept the single-player non-CE contracts.
Ordinary save/load of colonies started with megamod remains required.
