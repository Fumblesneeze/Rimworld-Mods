## Context

Owner **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
S097/P2-26. [Research](../../../mods/megamod/docs/refinement/2026-11-05-mining-priority-research.md)
pins installed/source1.7 Mining Priority (Continued), native1.7.4998 and the bounded consumer scan.
Selected public commit includes a byte-identical DLL. No license grant was found; independently
implement the documented behavior and do not reuse source code/art without resolving that separately.

The source patches target priority, scanner prioritization and worker eligibility. It uses generation
commonality/lump size rather than market price, plus small progress offsets. One static cached best
yield is shared between maps, callers and both work routes. This draft retains the intended policies
while correcting numerical crossover, a drilling toggle wiring error, incomplete footprint counting
and stale/caller-dependent benchmarks. Nothing is implemented or accepted.

## Goals / Non-Goals

**Goals:** predictable target order, actual yield-based worker selection, separate route benchmarks,
current explainable policy, native work/forced orders and shared data without stale permission.

**Non-goals:** new work types, automatic designations, deposit discovery, yield/work rebalance, exact
source floating-point score parity, job interruption/reassignment, general pawn optimization, source
configuration import, a private Thing cache or a new standalone mining UI.

## Decisions

### Integrate at ordinary work selection, not extraction effects

Use native MineAIUtility.PotentialMineables (Mine/MineVein designations), native building listers and
the actual relevant WorkGiver route. WorkGiver_Miner delegates to MineAIUtility.JobOnThing;
WorkGiver_DeepDrill has a distinct HasJobOnThing/JobOnThing pair. Preserve work type and
priorityInType order. Ranking acts only when choosing a target within that selection, not across
surface mining and drilling or over higher-priority work. S008 plans already create ordinary orders.
Do not change HP, Mineable.yieldPct, CompDeepDrill portion progress/yield, resources or job durations.

Native JobGiver_Work chooses prioritized scans only when scanner.Prioritized is true, then uses
GetPriority while retaining candidate validation and reachability. Source patches two base GetPriority
postfixes and injects after every Prioritized read using the last WorkGiver local. An owned selection
contribution needs coherent activation of both evaluation and ranking, preserving actual scanner/
target data flow. Prefer a narrow admitted seam or direct owned family contribution shared with
S058's work-selection infrastructure. Do not globally force every scanner to prioritized or copy
local indices/call counts from the old transpiler. Other owned work families retain their policies.

The source's deep-drill Prioritized branch reads priorityMining instead of priorityDrilling. Correct
this: deep ordering activates for its own rarity or exhaustion preference only. Independent yield
restriction does not require an ordering override. If both ordering preferences are off, preserve
the complete native/provider order and ties rather than writing an arbitrary zero result.

### Use ordered comparisons without score crossover

Represent ranking as ordered facts, then apply through a native-compatible comparison/projection
that preserves every distinction. Do not sum rarity, lump size and damage into one weighted float.
A finite snapshot can assign stable ordinal ranks if the native API requires floats, provided the
candidate set, validity and ordinary distance tie remain coherent. Exact mechanism is MNP-Q1; no
unbounded global sorting or persistent ranking framework is prescribed.

Surface rarity uses actual `building.isResourceRock`, `mineableScatterCommonality` and the average
of `mineableScatterLumpSizeRange`. Supported resource rocks with finite positive commonality and
usable lump bounds precede resource rocks with incomplete/nonpositive rarity data; both precede
ordinary nonresource rock. Do not treat zero as the rarest occurrence or as proof of ordinary rock.
Known rarity sorts ascending commonality, then ascending mean lump size. Progress sorts descending
MaxHitPoints minus HitPoints, after all enabled rarity keys. This deliberately retains the source's
absolute-damage meaning, including non-mining damage, while fixing weighted numerical crossover.
If rarity is off remove its keys entirely. Distance is ordinary horizontal distance, followed by
native tie behavior. No hidden per-pawn stockpile demand or market-value heuristic.

For drilling, resolve the same next output and finite-versus-fallback status that CompDeepDrill
will use through the admitted DeepDrillUtility path. Rank finite known-rarity, finite unknown-rarity,
then inexhaustible fallback; use the actual resource's deepCommonality and deepLumpSizeRange.
Exhaustion sums finite remaining counts across the **actual extraction footprint**, currently the
first21 GenRadial cells clipped to bounds. The source counts9, omitting reachable outer cells.
Count all types, not just the next resource or connected vein. Overlapping drills share the native
grid; resource depletion invalidates all affected comparisons. Do not change GetNextResource's
extraction order, radius or fallback. When only exhaustion is enabled, finite beats inexhaustible,
then fewer remaining units wins; this corrects source fallback count0 accidentally ranking first.
Use wide/checked sums, no count overflow or modulo priority. Unsupported custom extraction geometry
needs a narrow provider adapter; never silently assume21 cells for every drill subclass.

### Compare suitable worker facts, not the caller's cached answer

Yield restriction is a separate added permission for new automatic starts. Surface applicability is
actual mineableThing plus mineableYieldWasteable; drilling applicability is actual finite resource
presence, as distinct from base-rock fallback. Compare the current MiningYield stat against
maxSuitableYield × fraction, with inclusive ties, finite nonnegative values and no100% clamp. The
inspected stat reaches1.13 from skill20 and permits up to1.25 before other effective modifications;
source text saying yield ends at skill8 is outdated. Do not reimplement the game's stat curve.

Use the shared map worker facts already needed by S019/S017, but keep **eligibility and comparison
semantics separate**. S019 compares skill for a bill and intentionally retains some unavailable
experts; S097 compares current yield for an actual mining route/target and excludes downed, drafted,
mental-state, incapable and inaccessible workers. It is an explicit throughput/suitability correction
to the source's broad work-enabled pool, not a change to Most Skilled Bill Restriction.

Benchmark population comes from actual player work scheduling: route enabled, valid worker type,
required capacities/tags and target permission/access. Allow capable colony mechs/subhumans only
where that exact native route permits them; no animals or prisoners from merely sharing faction.
Animal Dig uses a different route and is not silently claimed. Exact provider eligibility may
require an adapter. Do not call effectful JobOnThing or HistoryEvent.Notify_PawnAboutToDo_Job for
every comparison merely to probe suitability: use admitted read-only facts and checks, then invoke
the real work path once for the selected action. Protect against recursion through our own filter.
Preserve native rejection/reason before adding ours. Reservations and access must be rechecked at
actual claim/start; the benchmark must not treat a known unusable target as assignable.

Ignore busy false intentionally retains otherwise suitable workers on other jobs/sleep/needs.
Ignore busy true retains actual mining/drilling workers, idle workers and workers in a work-seeking
state; excludes unrelated work/needs/rest. Define work-seeking from state, not `p == caller`, so all
simultaneously ready miners form one pool regardless of search order. Mining/drilling current jobs
may count as available but route capability remains necessary. No preemption. Native active jobs
continue; a later genuine new-start decision uses current policy. Settings do not rewrite progress
or reverse lower-yield work already done.

Keep map membership, work configuration, effective yield/stat inputs and activity facts separate
from target-specific path/reservation eligibility. Cache shared facts only when useful; map+route+
policy identity and revisions are mandatory. Do not copy a single `bestMiningYield`, 2500-tick TTL,
caller exception or exact GetType equality that excludes legitimate admitted subclasses. Invalidate
health/stat, work assignment, busy state, spawn/despawn/death/faction/map, settings and relevant
provider changes. Revalidate fresh facts for actual display/start. Clear on teardown, rebuild after
load, no live references in saves or off-thread Verse/Unity access. No full-map Thing scan per pawn;
reuse native listers and shared S021/S033 candidate data where relevant. Benchmark combined update,
memory and selection cost before claiming a performance improvement.

### Preserve the real manual override and explain automatic refusal

Current FloatMenuOptionProvider_WorkGivers passes forced:true to both HasJobOnThing and JobOnThing.
Both source restriction prefixes return through on forced work; the Workshop FAQ's claimed disabled
right-click is not the selected1.7 initial route. Native GiverTryGiveJobPrioritized subsequently calls
HasJobOnThing/JobOnThing with the default forced:false, and TryIssueJobPackage sets playerForced only
after a job is returned. The selected source can therefore reapply its yield gate during a renewal;
the native drill job currently expires after1500ticks with checkOverrideOnExpiry. The owned contract
deliberately honors the override during native renewal of that same active prioritized order too.
Resolve the actual pawn, map, cell/target and workgiver identity in a narrowly scoped owned context;
clear it on return/exception and never leak it to another worker, route, target or automatic search.
Do not globally change native forced flags or permission checks, and do not prolong, recreate or
save an override after native priorityWork is cleared, replaced or expired. Exact admission and
renewal/end evidence belong to MNP-Q1 and implementation tasks.

Add an explanation to the actual
mining/drill contextual information/appropriate native command tooltip: automatic eligibility,
threshold and that this explicit order overrides the yield policy. Do not disable a valid forced
command just to show the automatic failure. Native no-path, capacity, ideology and facility reasons
still win. A menu without an evaluated pawn can show policy, not invent a pawn-specific threshold.
No bottom tab, view toggle or debug action is necessary.

Unknown required comparison data is distinct from a legitimate zero/empty comparison. Leave the
affected contribution inactive with a bounded diagnostic/contextual explanation and the remaining
native/provider behavior. Do not fake zero and assert successful protection, silently discard the
best worker or make unbounded retry scans. Runtime unknowns need the same explicit fallback as
their admitted adapter contract; actual warning timing/reason is included in MNP-Q1 tests.

A successfully evaluated empty benchmark population is a distinct valid result: no available
benchmark and no numeric threshold, with no invented zero, support failure or automatic permission.
When a worker becomes idle/work-seeking, recompute the full eligible population before a new start;
do not cache an empty-population bypass. Explicit-order behavior remains independently admitted.

### Common settings and external identities

The eight spec preferences map to common settings under Work → Mining and drilling in the specified
order. Source defaults are retained: six policy booleans true, Ignore busy false, fraction1.0.
The owned fraction uses whole-percent0–100 steps; source's float slider displayed whole percent
without snapping. The source's refreshCacheTime setting2500 is deliberately omitted: correctness
and invalidation are implementation obligations, not a policy that should become stale by preference.
All rows use common load/store/resolve/render, dynamic global/save inheritance, dormant overrides,
main-menu callback guards and next-selection application. No scope exception. No new per-deposit
state is needed. Translate English, German, Spanish, French, ChineseSimplified, Russian and Japanese;
correct stale skill8 text, distinguish rarity from price and automatic refusal from forced override.

Source public Mod.settings, Settings fields, patch helper methods Priority/IsGoodMiner/
TranspilerPostfix and TD.Utilities.SliderLabeled are recorded in research. The bounded installed
scan found only this package's own About/two assemblies, not an external executable consumer. No
speculative facade or alias is selected. Original/continued providers share canonical package
`uuugggg.miningpriority`; active presence defers all S097 overlap with one explanatory warning.
Any differently identified fork needs actual evidence before being classified. Never unpatch or
remove the source, import its settings automatically or alter its active jobs.

### Admission and composition

| Candidate boundary | Minimal consumed semantics / grouping | Failure behavior |
| --- | --- | --- |
| MNP-T1 surface ordering | Actual miner/target, effective priority and native prioritized-selection route, coherent candidate validation | Withhold entire ordering contribution; retain remaining order/ties |
| MNP-T2 drill ordering | Same for drilling plus real next resource/finite flag/footprint | Withhold only affected route/provider ordering, no guessed footprint |
| MNP-T3 automatic yield restriction | Correct route/target/forced flag, native prioritized-order renewal scope, actual output sensitivity, native rejection and start boundary | Do not install a partial/manual-blocking filter; remaining native work and bounded explanation |
| MNP-T4 current facts and explanation | Consumed successful lifecycle/stat/work changes and exact pawn/target UI, not unrelated menu text | Recompute safely or suspend affected contribution; no falsely current cached answer |
| MNP-T5 optional provider adapter | Canonical package, real methods/Defs, work capability, resource/footprint/yield semantics | Absent quiet; changed unsupported adapter only, native/provider behavior remains |

These are candidates, not completed patch target documents. Before implementation register every
actual Harmony/XML patch's stable target spec and executable checks. Validate original/effective
semantics and relevant prefixes/transpilers; existence is enough only for a true observation hook.
Consumed argument use/control flow needs its minimal operands, cardinality and data flow, without
fixed offsets/compiler locals, full-method hashes or requiring no other owners. Every transpiler
checks actual incoming instructions each run and returns them unchanged on failure. Never execute
foreign transpilers repeatedly as a probe. Revalidate supported later repatching or deactivate
safely. Atomic group rollback removes only newly added owned patches. Warn once per skipped enabled
patch with package/patch/spec/target, failed predicate, unavailable behavior and fallback.

S058's other work-family ordering stays intact; direct shared infrastructure must not blend unrelated
numeric priorities. An unsupported non-neutral external ordering owns the whole affected selection,
including ties, while independent admitted yield restriction can remain. S008's mining plans/fog,
S019's bill skill policy, S052/S053 drill power demand and S021/S033 shared physical facts retain
their own contracts. Do not patch owned modules. No XML modification is currently needed; future
external XML requires XML Extensions packageId=true/content/shape checks, additive merge and the
required dependency at first use. Core/Harmony are required on implementation; optional providers
and DLC are not. No upstream or Gateway assembly enters the product.

## Risks / Trade-offs

- Strict100% threshold and busy-off can deliberately idle lesser miners → retain rock/other work,
  show the standard and provide the ordinary forced action and adjustable fraction.
- Access-aware suitability is more expensive than a map-wide maximum → share stable facts,
  evaluate target-specific permissions only as needed and measure without allowing stale decisions.
- Rare-resource preference can delay tunnels indefinitely under a continuing supply of ore → this
  is the selected priority policy; explicit orders and settings remain available, no invented aging.
- Unknown custom stats/geometry cannot be interpreted reliably → narrow adapters and honest
  inactive fallback, not metadata-name guesses or silently changed yields.

## Migration Plan

No runtime migration now. Owned common preferences persist; native jobs, HP, yields and drill
progress remain native. Rebuild all transient comparisons after load and reevaluate settings/provider
admission before use. No source settings or cached-best-worker state is imported. Any later import
requires a separate backup, atomic conversion/restoration and verified-postconditions contract.

## Open Questions

MNP-Q1 exact native effective seams/read-only suitability/priority projection; MNP-Q2 optional
workers/drills/consumers/license; MNP-Q3 finalized rarity/yield and throughput comparisons; MNP-Q4
shared invalidation, localization, focused TDD and final reviewed native acceptance. Record answers
when researched, without asking the user to explain existing source behavior.
