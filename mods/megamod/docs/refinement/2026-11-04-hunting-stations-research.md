# S064 — Hunting spot and raised seat research

Owner **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`; target **1.7 beta**.
**Origin: original**, S064/P1-58–59, researched2026-11-04 after the reviewed S063 checkpoint.
No replacement source package, Workshop item, source version/license or consumed source API applies.
The user's notes/interview are the requirements provenance. No gameplay implementation or live run.
[Proposal](../../../../openspec/changes/specify-megamod-hunting-stations/proposal.md),
[design](../../../../openspec/changes/specify-megamod-hunting-stations/design.md),
[observable spec](../../../../openspec/changes/specify-megamod-hunting-stations/specs/megamod-hunting-stations/spec.md).

## User decisions and deliberate specification assumptions

Read the original [photograph/interview refinement](2026-11-01-handwritten-notes.md) and
[identity reconciliation](2026-11-01-source-audit.md) before native investigation. P1-58/59 describes
a hunting spot or Hochsitz, a raised hunting seat/stand. The later interview settles:

- The hunter waits at the spot/seat until an animal enters detection range, then leaves to hunt.
- Detection is range alone, **no line-of-sight test**, initially **about40 cells**, expressly arbitrary.
- The station has a storage-style enable/disable species filter with predator/non-predator and
  small/big-game groupings. Species must include those that can appear on that map even when absent.
- Small/big cutoff, target choice and detailed return rules were not selected by the user.
- S065 is the separate custom bonded Attack+Haul companion, all eligible companions with one claimant
  per shot/downed prey; Hunt for Me is rejected. S066 separately adopts directional wandering for
  companions/wild herds/pen-room animals. This source does not absorb those entries or implement them.

The draft supplies explicit proposed defaults rather than new interview claims: one hunter per
station, nearest legal station/prey with stable ties, manual Hunt precedence, initially empty species
permissions, species adult-size cutoff1.0, ordinary hunter completion, need/work-aware return, and
1-game-hour failed station/prey retry delay. The raised-seat values are provisional comparator-derived
choices. HS-Q1–5 retain these choices and remaining proof before code; no questions interrupt the dive.

Public lookup did not supply a primary technical contract for this original feature. The developer's
[Alpha12 animal-taming announcement](https://ludeon.com/blog/2015/08/rimworld-alpha-12-animal-taming-released/)
is historical context for the distinction between colony animal training/ownership and wild animals,
not evidence of current1.7 eligibility or numbers. No third-party guide or current search snippet is
used to override the installed game. No unrelated hunting mod was inferred as the feature's source.

## Exact native evidence

Current game rechecked at this turn: **1.7.4998 rev1299**,
`F:/Steam/steamapps/common/RimWorld/Assembly-CSharp.dll`, SHA256
`FE9B615CF5918955AB750F9D627FB0082FD23C556BDFDEE410F88857C67A081B`.
Read-only exact-type reconstructions are under
`artifacts/Refinement/megamod-spec-20261104/hunting-stations/native/`. Inspected successful types:

- WorkGiver_HunterHunt, JobDriver_Hunt, Designator_Hunt, PawnColumnWorker_Hunt,
  MainTabWindow_Wildlife and JobGiver_Work.
- WildAnimalSpawner, GenStep_Animals, BiomeDef, RaceProperties and PawnKindDef fields used by the
  candidate catalog/classification; targeted source sections rather than a claim of full-class audit.
- IncidentWorker_HerdMigration, IncidentWorker_ThrumboPasses, IncidentWorker_FarmAnimalsWanderIn,
  IncidentWorker_AggressiveAnimals, AggressiveAnimalIncidentUtility and TileMutatorWorker_AnimalHabitat.
- MapTemperature and TileTemperaturesComp seasonal/current-temperature query sections.

Core XML inputs include Jobs_Work/Hunt, WorkGivers/HunterHunt, furniture Stool/DiningChair,
PartySpot, animal race/kind definitions and biome wildlife declarations. Native assembly/license
provenance is the installed game; no proprietary native source or binaries are copied into product
files. One wrong namespace lookup left an empty diagnostic file, excluded from evidence. No game
method, generator, incident or random-selection routine was executed. Operation registry was checked;
its decompile operation requires a live Gateway run, so bounded offline ILSpy was used for this prose
research instead of launching the game. No new public orchestration script was introduced.

## Hunting behavior that the station must compose with

### Eligibility and manual commands

HunterHunt is ordinary Hunting work, priority100 within that work type, requiring Manipulation and
tagged Fieldwork. Native JobGiver_Work additionally checks worker category, disabled tags/work types,
required capacities and mech-specific permission. A generic humanlike/pawn test alone is insufficient.
The Hunt giver searches spawned Hunt designations, requires a primary health-harming non-explosive
ranged weapon, rejects blocking shields/projectile combinations, and skips when no Hunt marks exist.

HasJobOnThing accepts AnimalOrWildMan, requires reservation and an existing Hunt designation, checks
innocent/venerated-animal ideology events and finds a legal cast position. New station prey is
deliberately narrower: actual wild animals, no wild humans/owned animals, no tame/protective marks,
and no initially aggressive targets. This is an autonomous authorization rule, not a change to the
player's separate native manual Hunt command. A currently legal native cast position is not a demand
for line of sight from the station itself; the hunter can leave and approach it.

Native Hunt designation can remove all prior marks. Wildlife's Hunt column specifically removes a
Tame mark and shows native warnings, including suitable hunters/weapons and retaliation/veneration.
Owned station detection must not invoke those destructive manual-command semantics merely to
authorize a job. It creates no public Hunt designation and preserves independent explicit orders.
Native map access/forbidden checks live partly in the surrounding job selection path, not just in
HunterHunt.HasJobOnThing. Danger.Deadly allows dangerous work; it is not assurance of a safe station.

### Active hunt and physical results

JobDriver_Hunt reserves one victim. Its failure predicate checks victim existence/forbidden state
and, unless ignoreDesignations is set, a continuing Hunt mark while the victim lives. This alone does
not validate station/filter/faction/ownership/taming/fog changes. The future owned intent needs those
checks before every new attack/finishing action; an already fired projectile cannot be recalled.

The native driver starts one hunt timer, uses ordinary cast positions at0.95 effective range and
fails pursuit/casting after5000 ticks. It does not teleport prey or add range. For downed prey whose
death action is not DangerousInMelee, it approaches, waits180 ticks and executes through the hunting
execution path. Mobile recovery aborts that finishing approach/preparation. Dangerous melee-death
prey remains on the ranged branch. S008 Finish off's60-tick path is separate and must not replace
this one merely because both features are owned by megamod. Hunting reactions and all ordinary
weapon/ammo behavior remain; an initial manhunter exclusion cannot nullify later retaliation.

After death, the driver records the hunting tale, obtains the actual Victim.Corpse, requires ordinary
reservation/reachability and unforbids it. A best valid storage cell is reserved with the corpse;
TargetA becomes the corpse and TargetB the cell, count1, followed by actual carry and placement.
When no valid destination exists, the job can end successfully with the corpse left in place. This is not an implicit
proof of S021's composed capacity or S049's refill limits; their actual transfer checks still apply.
Save/recovery must distinguish live prey, corpse, carried corpse and completed placement to avoid
double finishing/hauling or resetting the native timeout.

## Possible species is a map-rule query, not a spawn-list snapshot

| Native source | Relevant inspected behavior | Selected implication |
| --- | --- | --- |
| BiomeDef.AllWildAnimals | Unions positive normal, pollution and coastal commonality over PawnKinds; race wildBiomes contributes to normal commonality | Read all relevant map biomes and stable species identities, not only map.mapPawns |
| WildAnimalSpawner | Uses BiomeAt(location), season, coast, pollution, water needs and tile-mutator factors; CurrentTotalAnimalWeight/desired density affect whether it spawns now | Separate possible routes from today's temporary density/season; actual coast/habitat restrictions still matter |
| GenStep_Animals | Calls the same spawner while filling the initial ecosystem | Map generation belongs to the same possibility analysis, not a second arbitrary animal list |
| HerdMigration | Uses all map biomes, native CanDoHerdMigration and normal/pollution/coastal membership | Include suitable absent migration species, without creating a migration or consuming its random selection |
| ThrumboPasses | Uses thrumbo, entry availability and suitable seasonal/outdoor conditions; toxic fallout/noxious haze can suppress the current incident | A possible rare arrival is not limited to common biome spawn rows; temporary suppression is not a permanent species deletion |
| AggressiveAnimalIncidentUtility | Considers eligible biome/pollution/coastal species and an Odyssey habitat branch, then point-weighted choice | Species can be possible without authorizing a currently manhunting individual; do not call random choice for a UI |
| FarmAnimalsWanderIn | Selects suitable farm-tagged animals and gives spawned animals to Faction.OfPlayer | Owned livestock arrivals are not wild prey or permission to populate the catalog with every importable pet |
| Observed other-source wildlife | Actual revealed eligible wild animals prove that species occurred locally | Retain shared map-local observed species, initially disallowed, without exposing hidden spawns/quest contents |

Native current season/outdoor checks are not a complete future-climate possibility predicate.
Supported annual climate/habitat/conditional-route enumeration remains HS-Q2 before implementation;
the observable contract includes seasonal absence and permanent map exclusions. Pollution and coastal
commonality methods already gate their DLC, but map coast must still be considered. Unknown scripted
provider events are not enumerable by guessing names; disclose coverage and add proven descriptors
when needed, while observing actually revealed wild species. Neither arbitrary quest generation nor
trader/pet ownership makes every loaded animal locally huntable by default.

Queries must be simulation-neutral. WildAnimalSpawner.CommonalityOfAnimalNow consumes RNG to choose
a pollution route; incident methods choose/spawn animals. AnimalHabitat.GetAnimalKind temporarily
uses a tile-seeded random state and a reusable list, then restores state on its normal path. Inspect
effective isolation/failure semantics before consuming it; do not call spawning/incident routines
as an availability probe. No runtime compatibility or safety is proved merely by these static bodies.

## Classification and economic comparators

RaceProperties has a native predator flag and baseBodySize. Use species baseline adult data for
grouping, not a Pawn's current age-scaled body size, sprite size or combat points. The proposed cutoff
is1.0: equality belongs to big game. Predator is independent; small does not mean safe. The exact
chosen classification can be changed through common settings without rewriting per-species choices.

Inspected static Core XML, not finalized active-mod values:

| Comparator | Source values / inherited context | Implication |
| --- | --- | --- |
| Hare | Base body size0.2 | Small at proposed1.0 cutoff |
| Turkey | Base body size0.6 | Small at proposed1.0 cutoff |
| Deer | Base body size1.2 | Big even while an individual is juvenile |
| Muffalo | Base body size2.4 | Big; selection is species-level, not an age-based slaughter rule |
| PartySpot | Work0, no HP, standable marker | Free spot precedent; no physical cover/comfort/material refund |
| Stool |25 Stuff,450 work,75HP,mass3,comfort0.5,beauty0,fill0.2,path30; Metallic/Woody/Stony | Basic labor/comfort anchor |
| DiningChair |45 Stuff,8000 work,100HP,mass5,comfort0.70,beauty8,fill0.35,path30; Construction4/ComplexFurniture, Metallic/Woody | Higher furniture/progression comparator in this exact beta; do not substitute recalled older-version work values |

Proposed raised seat:50 Woody material,900 work,150HP,mass6,comfort0.5,beauty0,flammability1,fill0.2,
path30, no research/skill threshold, normal Stuff/quality/minification/salvage. This doubles stool
material/work/HP/mass for a simple raised structure while retaining basic seating comfort. It does
not grant height-based firing or spotting advantages over the free marker. These are explicit
assumptions, not measured finalized balance; HS-Q1 and future material/quality/live review retain
the decision. No new trader stock or profitable material/refund cycle is intended.

## Composition, persistence and external contracts

There is no actual source API/settings/save contract to impersonate. New station Def IDs and state
will be owned; migration/uninstall safety is not assumed. Ordinary compatible animal/weapon/map
mods participate through native metadata and scoped admitted permissions. Unknown optional APIs
need canonical identity/shape checks and an explained fallback, never a hard dependency merely to
list base-game species. Missing DLC is ordinary absence.

S008's drafted party hunting retains visible targets and weapon-range rules, not the station's
range-only discovery; drafting releases station intent. S021 hauling and S049 storage limits govern
actual loads/destinations. S033 shared search facts can supply the same live Thing data. S013/S028/
S048/S054/S060/S063 retain construction, rotation, repair, access and clearing contracts for the seat.
In particular, the positive-work seat qualifies for S028 rock/fog planning. Its own blueprint can
remain visible in unexplored cells, without revealing terrain/support costs or providing a working
station. Full-footprint reveal revalidation and S063 deferred first clearing precede valid completion;
unfixable plans recover actual supplied inputs. The instant marker still requires revealed ground.
S065 will define companion finishing/retrieval and S066 wandering; shared claims and direct calls
must prevent two actors finishing or carrying the same prey. No patches against internal ports.

Proposed nearest station/prey selection, one assignment through an excursion, physical departure,
work-aware return and station/prey cooldown require current state at commitment. Persist only the
needed identities/phase/timer; missing targets release claims while actual cargo survives. No permanent
worker ownership, delayed forced return, unbounded scan/retry, old-game references or off-thread
Verse access. Same-map observed species is durable knowledge; candidate snapshots are transient and
belong in native listers/common map data, not each station's private cache.

## Remaining questions and evidence limits

- HS-Q1: proposed marker/seat economy, work allocation, return/retry defaults and balance.
- HS-Q2: complete admitted potential-species descriptors and climate/conditional-provider boundaries.
- HS-Q3:1.0 grouping cutoff, actual adult-species metadata, localized contextual usability and art.
- HS-Q4: effective beta eligibility/attack/death/transport/load contracts and optional adapters.
- HS-Q5: direct S008/S021/S033/S049/S065/S066 composition, measured shared costs and native acceptance.

No implementation task is checked. No code/Defs/assets, tests, build/install, live Gateway run,
Workshop mutation, normal save/config change or simulation execution occurred. Static provenance
and document validation cannot replace a reviewed-build native wait/hunt/haul/return workflow.

## Documentation review checkpoint

Independent native correctness review found no issues. Composition review found one P2: HS01's
original revealed-only placement rule incorrectly excluded the positive-work seat from S028 fog
planning. The corrected contract distinguishes the instant marker from the seat blueprint and
retains hidden support privacy, full-footprint reveal validation, S063 deferred first clearing,
actual-input recovery and completed-only station activity. Design/tasks/ledgers were aligned;
the scoped resolution review found no remaining issues.

Final draft:10 requirements,42 scenarios,31 unchecked tasks. Typed strict OpenSpec validation:
86 items passed,0 failed. Eight scoped Markdown documents pass local-link and whitespace checks.
The static semantic pass preserves real work/materials, ordinary hunting risk/results, native
single-load versus owned batch limits, current storage demand and all remaining live balance gates.
No gameplay task or compatibility claim is accepted by this documentation checkpoint.
