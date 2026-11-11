## Context

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
S024 replaces specified Common Sense behavior. [Research](../../../mods/megamod/docs/refinement/2026-11-02-common-sense-research.md)
pins public/installed 1.6 inputs, selected 1.7 native comparisons, defaults, consumers and deliberate
corrections. Five observable capabilities separate the large source's behaviors without giving any
to another owner. No runtime implementation or native acceptance occurs in this change.

## Goals / Non-Goals

**Goals:** physical, bounded side work; correct native continuation; shared capacity/identity; useful
food/recreation choices; honest preferences and optional-provider admission.

**Non-goals:** blanket job-driver replacements, source binary/save spoofing, new content progression,
unlimited inventory, artificial need filling, broad map/pathfinding replacement, extra bottom tabs,
foreign setting mutation or copying unlicensed source/artwork.

## Decisions

### Compose behavior at the narrow native boundary

Own small policies beside their behavior and share only concrete S021 quantity/capacity and native
map facts. Do not copy Common Sense's full DoBill, SocialRelax, ingestion or hygiene driver bodies.
Use supported native extension/composition boundaries, then narrowly admitted Harmony where required.
Bill continuation retains the actual Bill, dominant ingredient, selected quantities, UFT, quota,
autonomous-container behavior and original notification/finish semantics. The inspected beta driver
still has separate collection, work and finish phases; exact selected callback semantics require
pre-implementation proof. Never reuse recipe progress fields as an unrelated packing timer.
S014 owns started linked-iteration policy: collection begins its single quota commitment, and all
later capacity-limited trips and cleaning/reload continuation retain its material/radius/worker
snapshot. Revalidate current physical permissions without substituting newly edited bill policy.
General pre-work cleaning excludes bills; the one bill-specific pass follows ingredient delivery.

One opportunity record identifies its parent job and finite selected targets. Preserve playerForced,
queues, emergency precedence and provider opt-outs. Cleaning is not a free side effect: use native
work, permission and effect semantics. Check cleaning areas and dynamic restrictions both at target
selection and use, not solely whether a source WorkGiver superficially offers work. Exact budgets
count filth objects globally across the selection, including multiple objects per cell. Limit 0 means
one finite snapshot. Changed/destroyed/reserved targets are skipped without regenerating the queue.

Avoid transiently substituting Filth for a bill-giver target across save/load, another patch or parent
validation; source does this and then adds special cases. Keep cleanup state distinct from native
recipe identity. A parent job that can no longer continue must return its real cargo/reservations to
the owned recovery path, not retry consumption or silently choose a same-Def replacement. Post-tend
care checks must not invoke an effectful scheduler merely as a predicate. Establish a read-only
clinical eligibility path that preserves actual medical priorities; S058/S062 will compose directly.

### Shared inventory, quantities and useful routing

S021 supplies one capacity budget and actual tracked transfer results for bill cargo and personal
items. No capacity disable setting. Respect kept supplies and player unload/equip decisions through
split, merge, temporary carry, partial deposit, failure, map exit and save. An inventory TryAdd result
does not identify the last element as the transferred item. Do not dynamically attach source comps
or globally mark whole stacks as unloadable. Consume owned quantity state directly; any genuinely
used external inventory projection needs its admitted capability adapter, not a source-parity facade.

Packing delay max(30,count×baseMass/10) is an explicit source-derived work cost in the supplies spec;
it is independent of recipe labor. Use integer-tick completion without reusing bill work fields.
Native heavy single-load fallback remains only the already specified S021 exception, never a new
capacity loophole. When batch support is unavailable, leave native ingredient collection intact.

Bill-supply storage detours use actual supported path costs and one detour per pending attempt, not repeated
StartJob recursion. No second per-feature route/index cache. S005 mixed storage groups and S033
shared Thing facts provide candidates; per-worker reach, reservation, bill radius and storage/refill
decisions are current checks. Validate cross-level topology; unsupported As Above routes skip only
the optional detour. Full route calculations are local/bounded and measured together with other users.

S125 now owns the combined bill-supply opportunity contract and canonical Hauling > Opportunities
setting. S024's settings inventory links there without a second editor. The actual selected-material
surplus and resource-to-work route benefit replace source-specific room/worker-distance gates;
S125's explicit equal-priority exception and whole-trip budget apply. This is one direct owned
decision through reload and parent resumption, not two source-style scheduling hooks.

Manual unloading is entity state with native Gear controls, safe cancellation and actual removal.
Use current quest/locked-apparel permissions at execution, not only UI creation. Source no-storage
fallback cannot remove worn equipment from an inventory it never entered. Restore-to-inventory must
check current combined capacity and use real transfer results; explicit drop/give/unload beats return.
Explicit manual unload tracks only selected quantities, their prior keep/unowned state and actual
remaining delivery. Cancellation restores that prior state for the cancelled remainder unless a
newer player choice supersedes it. Pre-existing S021 cargo remains a separate delivery obligation;
selecting it cannot create another manual order. Splits/merges preserve these portions, not stack flags.
Pending explicit orders held by unavailable admission need explicit Resume, not automatic
reactivation. Original-loaded disablement uses the ordinary settings disable/recovery path.
Optional Simple Sidearms forgetting occurs only after successful owned unloading through
a complete admitted signature; missing provider hooks cannot cause a pickup/drop loop.

### Food and needs preserve actual permission and effects

Perishable ranking consumes remaining rot progress, not a refrigerated spoil-time forecast. Retain
native medical potency where applicable, explicit Stuff/material filters and exact recipe eligibility.
The full source hourly-bucket/meal-score defaults are observable contracts. Medicine/surgery remain
native. Do not expand a candidate storage group outside the bill's actual radius/access. Revalidate
at pickup because the shared facts are not pawn permission. S026 composes active output-material
criterion ahead of S059's explicit ingredient choice and then spoilage among remaining ties;
secondary slots skip output-material comparison. S059 Default leaves ordinary/S024 behavior intact;
explicit Nearest/Random/current-temperature Expiry can decide before intrinsic spoilage ties.
Keep the linked-group candidate addition enabled independently of ranking. The
[S059 design](../specify-megamod-ingredient-preferences/design.md) specifies this once-only composition,
not competing sorts or invisible material changes.

Meal category restriction augments native CanStackWith. Keep category flags distinct from per-pawn
ideology and do not merge otherwise incompatible data. Optional generated provenance uses explicit
supported origin boundaries instead of intercepting every ThingMaker.MakeThing and clearing it later.
Resolve producing recipes/neutral candidate Defs once after finalized definitions, share concrete
metadata if already present, and leave no-candidate/custom provenance unchanged. Persist stable
inferred history; never mutate RNG/history when inspecting or reroll on split/reload. No source dummy
paste recipe. Informative summaries compute current effective constraints without writing filter
customSummary; S009–S012 are the direct host for recipe detail.

Scheduled drug mood logic is an additional rejection, not a replacement for native policy. Patient
treats validate the recipient and the delivering worker separately. Existing joy-provider weights
retain the specified source baseline/tolerance law; a missing need or invalid job is a normal fallback,
not an exception-driven endless retry. Only real native ingestion grants effects. S029 shares the
actual policy decision, not a Harmony chain against our code.

Use an owned persisted recreation-completion state with explicit 95/80 transitions at appropriate
need/decision boundaries, not a global bool or source StartJob/EndCurrentJob coincidence. Preserve
the native first 5000-tick/lord/schedule guards. Sleep priority must not defeat actual required rest.
Disabled recreation completion restores native thresholds and provider hygiene selection; the
source's retained-patch 0.8 threshold when fun_police=false is an explicitly rejected side effect.
With the policy enabled, nonurgent hygiene maintenance ranks below eligible incomplete recreation;
below-30% hygiene ranks above it. Test selection separately from forced interruption of active jobs.
Outdoor choice may ask admitted providers for real eligible locations, but cannot repeatedly execute
effectful job generation or erase another provider's restrictions. Manual S002 recreation is distinct.

Social economy predicts current drink joy after tolerance and uses the specified minimum/ending
policy; it does not change native gain rates or ingest twice. Its simpler duration rule deliberately
replaces the source's offset arithmetic. Gatherings, interruptions, missing/destroyed drinks and
changed tolerance need explicit tests. Meditation preserves scheduled exceptions and both needs.
Sleep mood motion uses current normal per-hour rates at ordinary updates and clamps at potential/50;
restrict to actual sleep so anesthesia/cryptosleep is not silently treated as rest. Do not backfill
elapsed mood on load. No beta rate equivalence is claimed before the actual native update is pinned.

### Idle destinations are capability-based and bounded

Use actual environmental/toxic/vacuum immunity; source 100f thresholds are wrong for inspected 0..1
stats. Check the specific hazard, breathable room and traversed path, not just roof or IsFlesh.
Retain pawn allowed area, pen/room confinement, native duty and urgent survival. A shelter failure
must not generate immediate repeat jobs or run general wandering into the same hazard. Search 100
regions and destination radius 7 are named source-derived bounds; sheltered 400–800/200 movement-cost
budgets are proposals with the stronger susceptible-path guard explicitly in the spec.
Distinguish a sheltered pawn's optional trip from escape out of existing exposure. An exposed pawn
can use a permitted direct escape route through that existing hazard; no already-protected-path
requirement may strand it outside. Retain actual native danger handling, no immunity grant or new
unrelated hazard permission. Test the same path from exposed and sheltered starting states.

Bedroom ownership belongs to the candidate room. Idle avoidance does not forbid ordinary transit.
CooledStoreroom is a scoped room-role addition using native storage score plus actual qualifying
coolers, not a forced rewrite of all room roles. Its label expresses configured intent, not power or
temperature truth. Custom cooler semantics need an adapter; Anomaly/Odyssey absence remains safe.
S030 toddler temperature safety and original companion/pen wandering will reuse policy facts while
retaining their own active-job and confinement contracts.

### Configuration and dependency admission

All real preferences use the foundation's shared global/save definition, resolver, persistence and
renderer, including dormant overrides and main-menu callback disabling. Entity choices remain entity
state. Replace source optimal_patching with mandatory safe startup admission and runtime settings
gates; toggling preferences cannot create unvalidated patches. Hiding controls never discards pending
state. Define safe next-decision boundaries and visible held/recovery status, not blanket restarts.

Required implementation dependencies are Harmony and XML Extensions. Guarded XML owns declarative
Job/room-role/joy-provider/component additions and the single food-packing threshold replacement.
Use real node/existence/shape and duplicate guards; external XML also checks canonical packageId=true.
The packing edit intentionally replaces one scalar from 0.6 to 0.3, never the surrounding think tree.
Do not duplicate the source's two identical patches, add missing arbitrary trees, or mutate all Defs
at runtime. Preserve other components, extensions, priorities and lists. Native actor scope for any
pawn state component must be explicit, not a narrow thingClass string assumed to cover every race.

| Candidate boundary | Required consumed semantics | Fallback |
| --- | --- | --- |
| CS-T1 job opportunity/continuation and filth work | Actual parent/queue/forced state, eligible clean target and resumable physical work | Skip side work, preserve valid parent and physical state |
| CS-T2 bill selection/collection | Exact ingredient count, primary material, holders, autonomous/UFT/finish boundaries | Native collection; safely recover already held owned cargo |
| CS-T3 food selection/stack/provenance/info | Current permission and rot history, additive merge veto, real versus inferred provenance, read-only summary | Native selection/history; no fabricated permission or overwritten data |
| CS-T4 inventory/drop/equip/load | Successful actual transfer and quantities, explicit intent, current protection, cancellation and save admission | Physical safe recovery; no whole-stack flag or repeated equip/drop |
| CS-T5 recreation/ingestion/mood | Actual schedule/need, recipient policy, native gain/consume-once and interrupt semantics | Native activity; no duplicate effect or free need change |
| CS-T6 wander/weather/room roles | Idle context, destination ownership, native hazard units, actual safe route | Skip optional destination change without blocking survival |
| CS-T7 declarative XML | Expected Def/node/content, additive memberships, one packing scalar's consumed meaning | Specific integration inactive, unrelated content retained |
| CS-T8 external hygiene/inventory/routing/job ordering | Canonical package and every consumed callable/Def/lifecycle shape | Only unsupported integration held, native workflow usable |

These groups are design candidates, not completed individual targets. Each actual Harmony/XML patch
gets a stable document, owning observable scenario, executable startup predicate, tests and later live
evidence. Validate minimal consumed semantics in effective code, including skip-original prefixes;
existence-only is sufficient solely for a true observer. A changed argument's actual required use
must be checked. Every transpiler validates its actual incoming stream each execution and returns
that stream unchanged on failure. No compiler-local index/first-branch matcher, whole-method hash,
no-foreign-owner test or speculative rerunning of foreign transpilers. Repatch revalidation/suspension,
atomic own-group rollback and one bounded startup warning per skipped enabled patch are mandatory.

### Ordinary feature disablement and genuine provider integrations

Active original Common Sense produces a bounded startup warning and invokes the ordinary
per-feature settings disable path for the owned overlap, best effort. Active original While You're
Up invokes that same path for the overlapping owned detour. Do not inspect its internal enabled
state or establish a cooperative handoff. Neither decision promises that source and owned behavior
work together. Do not write source settings, uninstall/unpatch a provider or emulate its API.
Source consumer tables remain research; no facade, binary/save contract or consumer-parity work is
required, even for a newly identified caller of upstream internals.

Support genuine other providers through actual narrow routes: hygiene needs
and native fixture eligibility; inventory UI insertions; sidearm forgetting; baby-carry ordering;
cross-level path costs; explicit no-opportunity jobs. Pin packages and exact signatures during their
implementation, not display-name guesses. CashRegister's opt-out stays intact. Glittertech's warning
probe disappearing is not evidence of compatibility; custom bill handling remains unadmitted until
its physical lifecycle is proven. Repository ImmersiveChefs remains independent; its source-specific
adapter is not a replacement-compatibility task or runtime dependency. No unrelated repository mod
is edited by this draft.

## Risks / Trade-offs

- Side work can starve production/care → finite passes, one detour, urgent/forced/queue precedence.
- Broad driver replacement can lose foreign semantics → narrow admitted boundaries and native fallback.
- Stack flags can affect unrelated goods → S021 exact quantities, intent lifetime and transfer outcomes.
- Source-like convenience alters labor, drugs or mood → explicit defaults, optional sleep/provenance,
  real costs/effects and later semantic balance comparisons.
- More informative metadata can change stacking/thoughts → stable inferred provenance, untouched actual
  history, clear labels and optional default off; inspect provider composition before claims.
- Optional paths can appear to work on old source → exact beta and provider native workflows required.

## Migration Plan

No runtime migration in this prose change. Deserialize and round-trip dormant owned saved state even
when runtime admission fails; retain orders, quantities and entity choices without executing them.
Admission gates activation and action-capable reconciliation, not retention. Rebuild candidate facts
and reconcile physical quantities safely before supported actions resume, without replay. Unsafe
side work is held; pending unload orders suspended by unavailable admission require Resume. An
original-loaded disable uses the ordinary settings path and its existing recovery semantics, not
a special source-coexistence mode. Do not import source
CompUnloadChecker/CompJoyToppedOff/DoCleanComp, SocialRelaxCommonSense jobs or global settings. Any
future importer/removal process needs a separate backup, atomic conversion/restoration and verified
postcondition contract. Never use draft specs as proof that a save can safely remove this mod.

## Open Questions

CS-Q1 keeps proposed corrective defaults and balance; CS-Q2 exact native job/ingestion/save lifecycle;
CS-Q3 proven provider bindings and direct composition; CS-Q4 tests, target docs, UI/localization,
shared-data measurements and final native proof. These are retained work, not questions interrupting
the current specification pass. All implementation and acceptance tasks remain unchecked.
