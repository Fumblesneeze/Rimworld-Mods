# Generation smoothing

## Identity and provenance

- Owner **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
- Origin **original**, S078/P2-07. No source mod, Workshop identity, source license or upstream
  settings/save migration applies. Native game code is read-only evidence, not redistributable code.
- User intent: distribute preparation while complete raids/trader groups appear together; maps
  and encounters also belong to the request. Threads, timings and architecture were not selected.
- [Research](../refinement/2026-11-04-generation-smoothing-research.md) records the inspected native
  1.7.4998rev1299 binary identity, callers, world-pawn effects, map lifecycle and unresolved gates.
- [Proposal](../../../../openspec/changes/specify-megamod-generation-smoothing/proposal.md),
  [observable spec](../../../../openspec/changes/specify-megamod-generation-smoothing/specs/megamod-generation-smoothing/spec.md),
  [design](../../../../openspec/changes/specify-megamod-generation-smoothing/design.md) and
  [tasks](../../../../openspec/changes/specify-megamod-generation-smoothing/tasks.md) are future work.
  Draft safeguards are proposals; no runtime behavior or measured optimization exists.

## Behavior inventory

Each row maps to the named requirement in the linked observable spec. GS-Q1–6 are research gates,
not hidden defaults or permission to ship a subset as complete.

| ID / requirement | Input/action → observable result | Timing, options and edge cases | Evidence/status |
| --- | --- | --- | --- |
| GS01 Preparation leaves opportunities to interact | Supported live arrival prepares → camera/selection/pause usable between portions; active-game destination → background preparation while simulation continues | Actual heavy inner work must be split; no missing or indefinitely delayed operation | User intent + native loops; drafted, unimplemented |
| GS02 Arrivals become visible as complete groups | Group ready → complete usable arrival with required stock, gear and behavior | No early combat/trade/search/relationship leak; native pods/waves retained | Confirmed complete-group intent; hidden-state safeguards proposed |
| GS03 Smoothing preserves the intended encounter | Larger operation → same intended strength/content rules and existing-pawn identity | No weaker raid, forced-new visitor, omitted content or reroll from pause/speed/save | Native generation effects; drafted |
| GS04 Arrival outcomes and ordering remain truthful | One occurrence → one real outcome, appropriate notices/history/quest result | Pending is not success; native retries and dependencies preserved | Storyteller/queue/quest/native worker audit; other callers open |
| GS05 Player pause and speed choices remain effective | Pause pending live arrival → no game-time advance or publication until permitted | Native paused loading/dev actions and event-induced speed changes remain distinct | Proposed player-control contract |
| GS06 Saving and leaving retain one valid outcome | Save/load pending work → same choices and one occurrence | Failed save preserves prior valid save; no early arrival, lost work or cross-save leakage | Observable guarantee drafted; schema/checkpoint route GS-Q3 |
| GS07 Invalid or failed preparation leaves coherent gameplay | Changed target/failure → actual event-specific failure/cancel/retry | Forced exceptions retained; borrowed pawns unharmed; published events never replayed | Native side-effect hazards; complete rollback audit open |
| GS08 New destinations are complete before entry | Native colony/site/transport entry → finalized destination and one handoff | Existing maps reused; failures leave accounted-for caravan/transport contents | Map/VisitSite audit; additional paths GS-Q4 |
| GS09 Progress belongs to the current workflow | Loading/failure → honest stage/failure explanation in existing context | Seven languages; no bottom tab, future-event preview or invented percentage; no setting selected | Shared UI rules; drafted |
| GS10 Unsupported generation keeps its existing authority | Required semantics absent → affected smoothing inactive, provider retained | Compatible foreign patches allowed; bounded startup warning per skipped enabled patch | Target policy drafted; executable checks not implemented |
| GS11 Responsiveness claims include completed gameplay | Measured run → interaction plus full arrival/trade/combat/map outcome | Largest interruption, completion tail and game-time delay matter; GS-Q1 scheduling/overload research, no numeric completion threshold | Measurement plan only; no benchmark or native acceptance |

## Interactions and composed result

S004 weather-ready arrivals finishes its applicable clothing work before publication. S057 trader
provisions remain conditional on successful arrival, capacity bounded and once-only; a letter is
not proof of success. S039/S040 gathering/Trading Area provides the actual valid destination.
S027 visitor delivery starts only after legitimate arrival. These are direct owned stages/policies,
not Harmony patches against megamod or a second provision/grant pass.

Ordinary search and hauling share published native candidates and the common derived Thing index.
Pending objects are private preparation state, not a second searchable map inventory or haul source.
No speculative prepared-pawn pool or separate full-map cache is proposed. Recheck actual target and
eligibility at use without canceling forced/quest paths through a broader invented rule.

Native drop animation, explicit waves and arrival speed rules remain gameplay. Active-game map
preparation happens in the background while simulation continues; only initial creation without an
active game retains loading-only behavior. Notices/cards wait for complete valid generation. Lost
source maps/caravans or changed entry intents cancel/discard owned temporary results without deleting
borrowed pawns/property. Stock, loot, threat budget, resources and probabilities remain unchanged.
GS-Q1 retains scheduling/capacity/overload research. The rough1–2-second real-time estimate is not a
deadline, timeout, cancellation rule or assertion in code/tests/performance acceptance.

## UI audit and relocation

No upstream UI exists to copy. Existing entry points and their proposed treatment:

| Entry point | Purpose | Classification | Destination / impact |
| --- | --- | --- | --- |
| Native live arrival and notices | Announces and enables actual event | Existing gameplay | One complete arrival; no new tab or preview of upcoming groups |
| Native map/encounter loading | Wait for usable destination | Existing loading workflow | Responsive progress with honest stages; no invented percent/time promise |
| Pause/speed, save, exit | Existing player control | Native controls | Remain effective and do not lose, prematurely publish or duplicate pending work |
| Future timing configuration, if GS-Q1 selects it | Performance/delay policy | Mod settings | Exact values first specified; common global/save/Use global rows, disabled save editing at main menu |
| Startup compatibility warning | Explains skipped patch and fallback | Invariant technical log | No new window solely to show diagnostic identifiers |

No setting, toggle, default value or scope exception is selected. Per-operation state is gameplay
state. New player explanations require seven-language coverage; log-only technical details remain
invariant. Developer test actions stay in developer surfaces and do not become ordinary gameplay UI.

## Contracts consumed by native callers, other mods and saves

| Consumer / evidence | Binding and lifecycle | Planned handling / current limitation |
| --- | --- | --- |
| Storyteller.TryFire | Synchronous CanFireNow/TryExecute success controls StoryState and lastIncidentTick | Own audited producer/continuation; never return queued work as completed success |
| IncidentQueue / QueuedIncident | Serialized FireTick/retry/tried state; remove on success or zero retry; native expiry/retry, map removal | Retain actual occurrence and native outcomes; no generic false-return polling or invented FIFO |
| QuestPart_Incident | Forced/quest context; temporary target binding cleared after synchronous execution | Capture actual operation context and continuation; other quest/direct callers still require audit |
| IncidentWorker / RaidEnemy / trader worker | Tales/history/codex, loot, speed/count/faction, trade lord and notices have distinct conditions | Preserve each condition and one outcome; false result is not assumed effect-free |
| PawnGenerator and world-pawn relationships | Existing identities can be redressed/removed; recursive generation and external notifications | Prove staged or reversible writes before admission; no borrowed-object destruction or forced-new workaround |
| GetOrGenerateMapUtility / VisitSite | Existing-map reuse or generation followed by entry/diplomacy/letters and caravan handoff | Finished destination required before caller continuation; no unfinished Map substitution |
| Save/load and teardown | No megamod generation schema exists yet | GS-Q3 selects durable state/checkpoints; no migration/removal compatibility claimed |
| External generation/optimization providers | No complete active-consumer inventory or cooperation proof yet | GS-Q5: exact package/Def/member resolution, narrow adapters and explicit unsupported paths |

There is no foreign package/assembly API to impersonate. Native caller signatures cannot simply
change to asynchronous returns, and unaffected external synchronous callers must retain their
provider's behavior. Metadata presence or a harmless foreign Harmony owner alone neither proves nor
disproves support. Any future save migration requires backup, atomic change, failure restoration
and verified postconditions; none is implicitly authorized here.

## Implementation and coverage

| Scope | Implemented here | Deviation / possible conflict | Missing / acceptance |
| --- | --- | --- | --- |
| Raids and traders | Nothing | Distributed hidden preparation with complete publication; must reconcile native side effects and timing | Actual caller/split/rollback paths, save policy, measured performance and native trade/combat proof |
| Colony maps and encounters | Nothing | Background preparation during active simulation, responsive initial creation, finished-map handoff | Initial-colony/transport/pocket/gravship audit, costly inner stages, finalization, failure recovery and native entry proof |
| Shared data and owned arrival features | Nothing | One publication boundary and direct composition | No premature cache visibility, duplicated provisions or late clothing; combined workflows untested |
| Optional providers and target checks | Nothing | Per-path semantic admission, loaded-provider fallback | Actual stable target documents, executable checks, provider matrix and later-invalidation proof |
| UI, settings and measurements | Nothing | Existing surfaces and honest progress; no new settings yet | GS-Q1 policy, seven languages, exact1.7 measurement support and reviewed-build evidence |

Harmony becomes required with its first use; no XML mutation or new XML Extensions use is selected.
DLC/content providers remain optional. Gateway, Circinus and foreign assemblies never ship with or
become product dependencies. A path that always falls back does not count as delivering the feature.

## Verification and acceptance

Two independent document reviews found no issues: native source/lifecycle evidence and observable
contract/direct composition. Typed strict validation passed100 items, zero failed; eight scoped
documents passed links/whitespace checks. The draft has11 requirements,33 scenarios and27 unchecked
tasks. GS-Q1–6 remain pre-implementation gates. No runtime edit, test/build/install, game launch,
source execution, Workshop write or normal save/config mutation is part of this checkpoint.

Later verification needs honest host tests for deterministic lifecycle rules and fresh exact game
processes for real callers, loaded content and side effects. Final acceptance requires personally
observed native preparation/interaction/arrival/trade/combat/map-entry workflows on the reviewed build,
including pause/save/failure and optional-provider cases, with build identity, causal screenshots,
logs, ordered mods, normal-configuration hashes and cleanup. Diagnostics alone cannot accept it.

Use the repository Circinus process for attribution plus supported whole-operation/frame/input
evidence. Preserve units, duty/adaptive limitations, independent stochastic distributions, actual
completion and pending-memory costs. No claimed speedup, numeric default or performance acceptance
exists. Balance review must include arrival delay advantage, native event frequency/retries,
threat/stock/loot/map-content preservation and ordinary job reachability.
