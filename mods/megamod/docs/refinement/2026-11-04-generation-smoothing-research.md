# S078 — Generation smoothing research

Owner **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`; **1.7 beta**.
Origin **original**, P2-07. Research started2026-11-04; owning draft now exists, with explicit
pre-implementation gates. No implementation or measured performance claim.

## Confirmed intent and scope

[Photo/interview refinement](2026-11-01-handwritten-notes.md) asks to avoid generating everything
in one tick, with traders, raids, maps/encounters as examples. The user confirmed that raids and
trader groups must appear together only when the whole group is ready; no partly prepared group
may enter combat or trade. This does not select threads, speculative pre-generation, exact budgets,
latency limits, determinism policy or a save format. Maps/encounters remain part of the request.
No named upstream mod or license applies; do not invent one or ask the user to explain existing code.

## Native evidence identity and inspection bounds

Current game1.7.4998rev1299, Assembly-CSharp.dll SHA-256
`FE9B615CF5918955AB750F9D627FB0082FD23C556BDFDEE410F88857C67A081B`.
Read-only type inventory followed by extracts under
`artifacts/Refinement/megamod-spec-20261104/generation-smoothing/native/`.

Fully read IncidentWorker_TraderCaravanArrival, IncidentWorker_NeutralGroup, PawnGroupKindWorker and
PawnGroupKindWorker_Trader. Read relevant methods of IncidentWorker_Raid (generation and execution),
IncidentWorker.TryExecute, PawnGroupMakerUtility (generation/selection), MapGenerator (GenerateMap,
ordered steps/post-init and cleanup), and LongEventHandler (queue shapes, flags and update paths).
Also read IncidentQueue/QueuedIncident, QuestPart_Incident, RaidEnemy, edge-walk/center-drop arrival,
GetOrGenerateMapUtility, VisitSite, Terrain and RocksFromGrid in full; relevant Storyteller.TryFire
and PawnGenerator new/redress/relations/discard paths. Full files are retained for further inspection;
this is not a complete caller, inner generator, DLC or external-provider audit.

## Findings that constrain the design

### An enumerable group API does not make generation incremental

PawnGroupMakerUtility.GeneratePawns is an iterator, but its selected worker calls the synchronous
PawnGroupKindWorker.GeneratePawns, which builds a List<Pawn> before returning. It exposes that list
through static pawnsBeingGeneratedNow during generation and removes it in finally. On exceptions,
it destroys members already in the list and clears it. A wrapper that yields one already-created
pawn per tick would delay delivery without necessarily reducing the expensive generation burst.

Trader worker builds trader, stock (including animals/slaves), carriers and guards in sequence.
Trader generation uses canGeneratePawnRelations=true, can reuse/redress world pawns, sets trading
components/state and calls CheckGiveTraderQuest. Stock generation is a synchronous ThingSetMaker
call. Carriers are based on non-pawn stack count, then inventory distribution; guard choices consume
remaining points. Trader generation explicitly warns that a provided deterministic seed is not
implemented for that worker. Do not claim identical outcomes merely from passing the same seed.

### Incident completion has more effects than spawning pawns

Neutral-group SpawnPawns first materializes the whole group, then spawns all members and appends
to storeGeneratedNeutralPawns. Trader execution resolves faction/kind/points, refuses hostile
factions, spawns the group, fills food needs, sends the arrival/relationship letter, searches a
common reachable gathering position, then creates the trade lord. A failed gathering-position
search can occur after spawn/letter; faithfully slicing the method requires explicit failure and
commit semantics, not an assumption that a false result implies no observable side effects.

Raid TryGenerateRaidInfo resolves points/faction/strategy/arrival/age restriction, invokes strategy
TryGenerateThreats and SpawnThreats, then falls back to materializing a generated pawn group and
calling its arrival mode. It subsequently postprocesses pawns and generates loot. TryExecuteWorker
then builds relationship/arrival letters and lords, with special controller handling. Strategy-
spawned threats and arrival modes can differ; no universal pawn-list-only commit is established.
Native debug generation is distinct and must not become the production integration by accident.

IncidentWorker.TryExecute calls the worker, conditionally records tales on success, then records
the incident fired and optional codex discovery even when the worker returned false (apart from an
earlier no-colonists shortcut). Replacing its bool result with a queued promise would misrepresent
the synchronous caller contract. The caller inspection below establishes why owned continuations
are required; it does not establish support for every caller or a complete implementation.

### Map generation already has global lifecycle and random-state boundaries

GenerateMap enters ProgramState.MapInitializing, clears static work data, pushes/seeds Rand, creates
a map and unique ID, sets mapBeingGenerated, constructs components, and registers it with Current.Game
before content generation. It invokes mutator/pre-content setup, ordered/prevented gen steps, scenario
callbacks, finalization, MapGenerated and parent callbacks, then post-init/gravship/lesson paths.
Finally clears working state where appropriate, restores ProgramState and pops Rand.

GenerateContentsIntoMap uses static grids/data/temp steps, per-step random seeds and direct GenStep
Generate calls. A safe continuation cannot leave the live simulation consuming these globals or an
unfinished registered map between slices. Debug step mode is a separate workflow with repeated
FinalizeInit and is not evidence that production generation is safely resumable. Individual gen
steps, encounter entry/transport callers and random-state isolation remain research.

### Native long events are not automatically a small frame budget

LongEventHandler supports Action, asynchronous-thread and IEnumerable variants. Any queued/active
event forces pause. Its enumerator update repeatedly calls MoveNext until a0.1-second real-time
deadline, and one MoveNext can itself exceed that deadline. It disposes on completion/failure and
executes queued completion actions/callbacks. The asynchronous variant runs its supplied action on
a separate thread. These facts do not authorize arbitrary Verse/Unity calls on new worker threads,
or establish that wrapping a large action makes its internal work interruptible.

Separate live-incident preparation across simulation ticks from map/encounter loading progress
across rendered frames while the native loading workflow pauses simulation. Paused behavior,
save/exit interactions and user-visible progress/error/cancellation still need an explicit contract.

## Further caller and ownership findings

### Storyteller, queue and quest are different completion contracts

Storyteller.TryFire calls CanFireNow and TryExecute, then only on true notifies target StoryState and
sets lastIncidentTick. IncidentQueue sorts by FireTick but iterates backwards; due entries call
TryFire, mark TriedToFire and remove on success or zero retry duration. Failed retryable entries
expire at FireTick+RetryDurationTicks and otherwise retry on the native833-tick seeded interval.
Map removal deletes queued incidents targeting that map. A generic FIFO or immediate queued=true
success would not preserve this behavior. The833 constant is a native observation, not a new setting.

QuestPart_Incident.Notify_QuestSignalReceived forces its parameters, binds quest, may retarget a
MapParent, temporarily assigns incidentParms.target to its Map, calls CanFireNow/TryExecute and
then clears that target. A continuation cannot retain the same mutable reference and assume its
target remains valid. This path does not call Storyteller.TryFire, so storyteller accounting cannot
be pasted onto every quest incident. Other direct/quest callers remain GS-Q2 research.

RaidEnemy adds its normal-speed signal when not silent, raid count and lastRaidFaction only after
base success. Its loot generation applies difficulty/strategy factors, generates a ThingSet and
distributes it. EdgeWalkIn spawns every pawn near the resolved edge center in one call. CenterDrop
delegates the full group to drop-pod arrival and ordinarily sets podOpenDelay520, with military-aid
exception and an EdgeDrop fallback when its center cannot be found. Those native animations/delays
must not be confused with staged generation or replaced with one visible pawn per work slice.

### Pawn generation is not private object allocation

GenerateOrRedressPawnInternal can select former colonists, prior settlement inhabitants or eligible
world pawns, redress them and remove them from WorldPawns. Redress can remove hediffs/genes, change
kind/faction/ideology/gear, reset guest/needs/mind state and more. The enclosing path notifies ideology
and scenario generation callbacks. GeneratePawn may additionally notify player population adaptation.
These observations exclude the simple assumption that an unpublished pawn is safe to destroy.

New generation has a native120-attempt loop; after70 failures it ignores scenario requirements and
after100 it ignores its validator. TryGenerateNewPawnInternal registers generation status, allocates
components, creates age/bio/traits/genes/skills and calls GeneratePawnRelations when permitted.
Relations choose among AllMapsWorldAndTemporary_AliveOrDead and invoke relation workers that can
affect existing pawns. DiscardGeneratedPawn uses WorldPawns and nested PawnsGeneratedInTheMeantime
ownership. This is evidence for a deeper reversible-effects audit, not proof that existing native
discard is a general rollback or that the entire generation implementation was read.

### Map entry awaits a finished map and several inner stages are whole-map loops

GetOrGenerateMapUtility first reuses Current.Game.FindMap(tile); otherwise it resolves or creates
a MapParent and uses MapGenerator or PocketMapUtility. VisitSite.Arrived queues a synchronous native
long event only for a new map. DoEnter waits for generation, applies hostile-map and relationship
notifications, entry/threat letters, optional attacked-site diplomacy, then enters the caravan with
native drafting and DoNotDrop inventory. Merely returning an unfinished Map violates this caller.

GenStep_Terrain loops all cells under DisableIncrementalScope, removes incompatible rock, sets
terrain, removes bulk collapsing roofs and cleans terrain patch makers. RocksFromGrid disables
region/room updates, loops all cells to place rock/roofs, flood-fills small roof groups, runs mineable
scatter and reenables updates. Yielding only between steps still leaves these large inner actions.
GenerateContentsIntoMap directly invokes each ordered step, catches/logs its exception and continues;
its per-step Rand.PopState is inside the try. There is no established general transactional failure
or safe suspension contract here. Do not fix unrelated engine error handling as an incidental patch;
an owned admitted path must explicitly preserve/restorably manage its own scopes.

Transport, pocket/gravship, initial-colony producers and other encounter callers are not yet fully
traced. They remain GS-Q4 within the requested scope; no supported-provider list is claimed.

## Measurement process and unresolved research

Applied the repository performance-benchmarking skill to the measurement plan only; no benchmark
was run. Read the benchmarking proposal/design/spec/tasks and Circinus reference. Future profiling
must follow its Circinus policy, ordinary native workload, exact packages/
build/runtime identity, raw units and stochastic distributions. Product-absent and product-present
runs are not arbitrarily paired deltas. Profiler attribution alone does not prove responsiveness,
nor replace actual complete arrival/trade/combat/map-entry acceptance. No measured claim or default
time budget has been selected. Historical1.6 adapter evidence does not accept exact1.7 support.
Design proposes full-operation and frame/input/commit-tail observations in addition to attribution;
rare bursts can be missed by Circinus duty/adaptive sampling. Missing recurring measurement support
needs its own tooling-owned contract/typed operation, not an ad hoc product profiler.

## Draft choices and remaining gates

[Owning change](../../../../openspec/changes/specify-megamod-generation-smoothing/) contains proposal,
observable requirements, technical design and unchecked tasks. [Coverage](../replacements/generation-smoothing.md)
retains the full scope and explicit limitations. Lifecycle safeguards are proposed requirements;
the user-confirmed choices remain distinguished above. No arbitrary timing values were promoted to
defaults and no new setting/UI toggle was invented. CLI artifact completeness is not readiness to
implement while the following gates remain:

| ID | Still to resolve | Current boundary |
| --- | --- | --- |
| GS-Q1 | Measured work allowance, maximum arrival delay, capacity, overload action and any setting defaults | No invented infinite delay, automatic pause, synchronous catch-up or magic numeric value; amend observable contract before code |
| GS-Q2 | Full caller/strategy/stock/relationship side effects, safe split/rollback and random-state strategy | Preserve actual native accounting and existing identities; method existence or enumerability does not prove resumability |
| GS-Q3 | Concrete pending-state schema/checkpoint route, manual/autosave, pause, exit, removal and failed-write recovery | Successful saves retain choices and one occurrence; cannot force early publication, reroll or lose work |
| GS-Q4 | Initial-colony, transport/pocket/gravship and other encounter continuations; measured inner stages/finalization | Still requested scope; no unfinished map return, normal-game ticking or partial content acceptance |
| GS-Q5 | Actual active external provider contracts and S004/S027/S039/S040/S057/shared-data composition | Exact absent-safe adapters, direct owned calls, loaded-provider fallback; no all-mod compatibility claim |
| GS-Q6 | Executable targets, focused tests, seven-language workflow, measurements and final native acceptance | Every runtime task remains unchecked; schema validation cannot prove correctness or performance |

Before code, add actual stable target documents and executable semantic checks, atomic per-path
admission, later repatch reconciliation and per-skipped-enabled-patch startup diagnostics. Use the
loaded provider when a path is not safely admitted. This must not become a feature that always falls
back for its core scope and then claims success. Record questions for later, not another interview
about existing native behavior.

## Document review and checkpoint

Two independent read-only reviews completed: native source/lifecycle/ownership evidence and
observable-contract/composition/performance honesty. Both reported **no issues**. The host also
checked S004 clothing, S057 arrival-provision success and S040 gathering composition, and performed
a semantic balance pass: no new difficulty/stock/resource values; delay advantage and retry timing
remain GS-Q1/2 gates rather than presumed neutral effects.

Typed strict OpenSpec validation passed **100 items, zero failed**, exit0 with no stderr. Eight scoped
documents passed relative-link and whitespace checks. Draft: **11 requirements, 33 scenarios,
27 unchecked tasks**. Queue: **79 reviewed drafts /136 entries /338 unique question IDs**.
Artifact completeness is not implementation readiness while GS-Q1–6 remain open. Next: S079
Stop, Drop, And Roll! [BAL]. Floor Pipette Tool remains queued once as S136.

No runtime product edits, tests, build/install, game launch, source execution, Workshop writes or
normal save/configuration mutation. This research closes no gameplay or performance acceptance task.
