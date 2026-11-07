## Context

Owner **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`; target **1.7 beta**.
**Origin: original**, S064/P1-58–59. [Proposal](proposal.md),
[observable spec](specs/megamod-hunting-stations/spec.md),
[research](../../../mods/megamod/docs/refinement/2026-11-04-hunting-stations-research.md).
There is no source mod API/save schema to preserve. User provenance establishes station waiting,
range-only detection initially40 cells, departure to hunt and a map-specific grouped species filter.
Other choices are explicit proposed policy. S065/S066 are separate upcoming changes, not delivered
by this draft; Hunt for Me was rejected and is not a source implementation to port.

Current native1.7 work requires a Hunt designation, a suitable ranged weapon and ordinary work,
reservation and ideology/cast-position checks. The Hunt job can ignore designation requirements for
its own job, but that flag does not establish all safety/authorization checks. The job already does
real pursuit, a bounded timeout, downed execution and corpse transport. Native wildlife spawning is
multi-biome and affected by coast, pollution, habitats and some incidents. Calling random selection
or incident execution to populate a UI would mutate simulation; none is executed for this research.

## Goals / Non-Goals

**Goals:** functional spot/raised seat, current-map possible-species catalog, contextual authorization,
ordinary work scheduling, physical wait/depart/hunt/haul/return, bounded claims/retry/load recovery,
shared data and minimal admitted integration. Preserve existing manual and drafted hunting.

**Non-Goals:** an all-purpose animal manager, new Hunting work type, free ammunition or skill training,
automatic butchery, animal attraction/spawn changes, unrestricted hunting of pets/wild humans, an
elevated firing advantage, or a new pathfinding subsystem. No source-package/assembly facade or
legacy-settings migration is justified. No independent megamod runtime dependency on Pathfinding Lab.

## Decisions

### Own the station, reuse ordinary physical systems

Use owned declarative ThingDefs for the free marker and constructed seat, plus narrowly owned
station/job/WorkGiver definitions and entity state. The marker follows ordinary instant spot
semantics; the seat follows ordinary quality furniture construction/minification/reinstallation.
Its raised appearance is presentation, not a new elevation combat system. Do not implement a second
material/repair/quality economy; preserve S013/S028/S054/S060/S063 integration at the existing owned
construction boundaries. S048 furniture auto-orientation, if applicable to the seat, must not rotate
the circular detection range or defeat the user's manual rotation override.
The instant marker requires revealed legal ground. The positive-work seat participates in S028's
rock/fog planning when enabled, with no hidden-cell terrain/support preview. Its plan waits for
full-footprint reveal/revalidation, then the existing prerequisite and S063 deferred first-clearing
path; invalid plans use S028's actual-input recovery. Neither blueprint nor frame supplies station
watching, detection or seating comfort before valid construction completes.

Proposed seat values in HS01 are derived from twice the basic stool's material, work, HP and mass,
with stool-like comfort/fill/path behavior and Woody restriction. Static comparison is not final
balance. Do not dynamically mutate Core or multiply an unrelated mod's live stool values without a
new contract. Declare our baseline values in our own Defs and accept ordinary Stuff/quality effects.
No existing woody list is replaced. If actual XML patches become necessary, guard target content and
shape with XML Extensions; external targets additionally require canonical package checks and the
hard dependency at first use. No PatchOperation is required merely to define an owned Thing.

Apply the asset skill before creating/selecting artwork: reference measurements, transparent canvas,
cardinal appearance, material/quality/damage states and live zoom review remain future tasks. The
waiting cell, seated pawn alignment and departure must be visually legible without implying range
or cover advantages the mechanics do not grant. This draft does not select a texture or raster art.

### One map catalog, explicit species choices

Represent species by stable animal race ThingDef identity, collapsing PawnKind variants that share
the species. Distinct race identities stay separate even if translated labels match. Use resolved
native predator classification and base adult-size data for group metadata; do not classify an
individual's current BodySize, rendering scale, combatPower or herd size as species adult size.
The1.0 cutoff is a common setting and only affects display grouping, never stored permission.

Build a shared map-scoped catalog from admitted sources, not currently spawned pawns alone:

- All current map Biomes' native wild/pollution/coastal candidates, including race wildBiomes;
  active DLC and actual coast/map context still gate their respective routes.
- Positive possible generation/arrival routes after permanent exclusions and admitted mutator
  effects. Include seasonal/pollution-conditioned possibilities with a condition label; do not
  remove a species just because today's weather, current density or incident cooldown prevents it.
- Admitted migration and rare wild-arrival descriptors, including native ThrumboPasses and the
  map-specific aggressive-animal/habitat pathways. These make species possible; they do not make
  currently aggressive individuals eligible prey. FarmAnimalsWanderIn yields owned animals and is
  not a wild-hunting catalog source merely because the species can be imported.
- Map-local already revealed eligible wild species, retained as observed-species gameplay knowledge
  after the last specimen leaves. Persist only stable species IDs, not live Pawn references. This
  history is shared map knowledge, not an independent per-station cache or a spawn guarantee.

Use pure availability descriptors and native read-only facts. `CommonalityOfAnimalNow`, random incident
selection and spawn methods are not catalog queries: they consume RNG and/or create pawns. Native
AnimalHabitat resolves a tile-seeded selection with Rand push/pop; if consuming that mapping, validate
its effective state-isolation contract or use already resolved admitted data. Annual/seasonal
possibility must use the supported climate range, not a probe that advances game time. The inspected
MapTemperature and TileTemperaturesComp distinguish seasonal/current outdoor conditions; exact
potential-route predicates and unusual map climates remain HS-Q2 implementation research.

Incapable or unknown provider generators are not guessed from package names. Add a canonical,
shape-guarded descriptor only for a proven route, otherwise state catalog coverage limits and still
recognize actually revealed wild species. Missing DLC is ordinary absence, not a startup error.
Catalog refresh occurs on relevant map/tile/biome/habitat/content changes, supported generator-state
changes and revealed-wildlife observations, with lazy bounded work. Preserve disabled/unavailable
choice records; additions never inherit an Allow all from an older catalog. Never reveal hidden
individual counts, locations or unpublished quest choices while explaining possibility.

### Contextual authorization and shared index

Each station stores enabled=true and an explicit species-ID set initialized once from currently
listed map-possible species with exactly zero native species retaliation-on-hunting-damage chance.
Resolve the same chance information used by the chooser; unknown or positive rounded-to-zero values
are excluded. Do not rerun initialization after save/load, provider/stat changes or player edits. Use a native-style
searchable grouped filter surface, sharing rendering practices where useful without pretending
species authorization is a storage filter that controls physical storage acceptance. Search is
presentation; category actions materialize only currently applicable choices. Status and range are
on the selected entity; no global settings page for each station and no new bottom tab.

Keep live candidates in native pawn/Thing stores or the common map-owned Thing index used by Ctrl+F
and hauling. A truly needed derived species/spatial field is one shared extension, not one scanner
per station/companion/source. Catalog snapshots and live target eligibility are different things.
Recheck actual faction, tame/protection, fog, forbidden/access, equipment, ideology, path, cast
position and reservations for the actual worker. Native Thing queries cannot prove these per-pawn
permissions. Update on spawn/despawn/movement/faction/designation/visibility and relevant equipment/
worker changes; discard transient state on teardown, rebuild after load and stay on the Unity thread.

### Scheduling and a small explicit lifecycle

Add station work under Hunting below actionable explicit Hunt work, preserving ordinary work-type
priorities. Do not globally remove the native Hunt WorkGiver's ShouldSkip/designation check. Reuse
native worker/capacity/weapon/ideology/approach semantics through narrow admitted boundaries. Ordinary
hunting's Danger.Deadly is not a safety guarantee; configured allowed areas and explicit forbidden
state still apply. Stop as ordinary urgent/forced/needs workflows take control.

One station assignment and one prey claim per hunter suffice. Model the necessary current phases:
walking to station → watching → departing/pursuing → finishing/corpse handling → ordinary reassessment
→ returning/watching or released. Reserve the waiting position on arrival and retain the logical
station assignment while away; do not reserve every potential prey or route. Two overlapping stations
must acquire a target atomically with native/owned compatible reservation semantics. Preserve one
current actor through future companion handoff rather than treating each feature as a separate owner.

The initial acquisition circle is squared horizontal distance from the station cell, no LOS. Require
the hunter to have actually reached the station. The candidate's ordinary legal firing/approach path
can leave the circle; checking immediate visibility from the seat would contradict the user's intent.
The first firing position/departure must be off the station's waiting cell. A one-cell blocked room
is a valid wait/no-job result, not a reason to loop until a route appears. Use bounded native path
queries, stable ties and a fresh permission check at claim/start.

Proposed observable responsiveness bounds are60 game ticks for a stable available prey and250 for
waiting work/need reassessment. Keep them named constants rather than UI performance knobs; actual
timer staggering or index update budgets remain design details subject to those bounds and combined
benchmarks. No per-tick per-station map scan, recursive retry or unbounded native query repetition.

### Preserve native attacks without manufacturing public designations

An owned station hunt is authorized by its station/target intent, not a map-wide automatic Hunt
designation. Use a narrowly owned job context and native toils/driver semantics where they preserve
that authorization. If a native job uses `ignoreDesignations`, confine it to the admitted owned
instance; never globally bypass designation checks or temporarily create/remove native marks just
to trick eligibility queries. Guard before every new attack/finishing action, including changes to
the station/filter, faction, protective orders, fog, forbidden/access and actual worker eligibility.
Already fired projectiles retain native consequences.

The current native timeout is5000 ticks; downed safe execution prepares180 ticks. Preserve that
ordinary hunt lifecycle without a timer reset on restore. DangerousInMelee death action keeps
ranged handling. S008's explicit Finish off60 ticks is a different contract; do not substitute it
because it seems convenient. An initial manhunter exclusion does not erase a revenge reaction after
our admitted shot. Preserve native reactions and interruption priorities without claiming safety.

Use one actual corpse and ordinary results. The source Hunt driver moves TargetA from victim to
corpse and reserves a real storage cell. Integration with S021/S049 must honor current carrying
capacity, destination limits and ownership before carry/placement; it cannot assume native's job
construction has already enforced megamod's composed limits. No destination is an ordinary retained
corpse, not permission to fabricate a load. Do not convert a station hunt into endless opportunistic
retargeting; a new automatic prey requires a new physical watch cycle.

### Cancellation, return and saved state

Save station enabled/species choices, map observed-species knowledge and the minimum active job
intent (station identity/map, actor, prey/corpse, phase, existing timeout and relevant retry expiry).
Resolve IDs and validate actual native job/Thing state on load. A saved phase is not permission to
repeat a kill, spawn a corpse or replay a haul already completed. Conflicting claims resolve to one
current valid action; invalid references release owned intent. Existing manual designations never
belong to our automatic intent and must not be canceled as cleanup.

The proposed failed-attempt cooldown is station/prey scoped, default1 game hour; other stations may
attempt a later separate hunt, but no live attempt can evade its timer by moving context. Store
expiries with the game, prune after expiry/despawn and never retain another game's references. At
zero delay, re-evaluate on a later bounded acquisition check, not inside a completion callback loop.
Ordinary no-prey waiting itself is not a failed pursuit requiring a cooldown.

Reassess needs/work before return. Physical return to the same still-valid station is preferred
only while its assignment remains current; a higher-priority/player action releases it. Pausing,
moving/minifying or destroying a station ends its live attack/return intent. Filter removal or new
protection revokes only the affected target. If death/transport has already occurred, preserve the
actual load through permitted ordinary delivery or physical recovery, with no new attacks. Seat
minification preserves choices but no active claim; reinstallation uses the destination map catalog.
Free-marker removal creates no transferable station state by itself.

An admitted S065 transfer ends the human's current prey/corpse responsibility without waiting for
animal delivery. Allow ordinary reassessment/return, while the separate delegated claim retains
the original station/filter authority and its own current target. A later station hunt cannot
overwrite that claim; station pause/removal/filter changes still invalidate affected delegated work.

### Common settings and direct composition

Use the common load/store/resolve/render path for Detection radius40 positive whole cells,
Big-game cutoff1.0 positive finite size, and Retry delay1 finite nonnegative game hour. Invalid/overflowing
input fails visibly without changing the effective value. Radius covers actual map cells only;
no artificial small fixed cap is proposed. Scope is global default plus per-save override, inherited
for missing values, dormant override retained, and main-menu save mutations disabled. Apply radius
at new acquisition, cutoff only to grouping, and delay to newly failed attempts. Station/pawn/job
choices remain entity state. Seven baseline languages are required at UI implementation.

S008 drafted party hunting takes control only after drafting ends the station assignment; it does
not inherit this feature's no-LOS detection or station radius. S065 will own bonded animal eligibility,
claimant selection and post-shot finishing/retrieval; S066 will own directional following/wandering.
Their shared current-intent handoff must disable the hunter's competing finishing/carry action and
support interruption recovery. Until admitted, native hunter completion is the fallback. Direct
owned calls compose these features; no patches against our own ports and no runtime dependency on
another repository mod. S021/S049 own composed hauling quantities/destination admission.

### Per-patch target admission and packaging

Create a stable target document for every actual Harmony/XML patch before implementation. New owned
Defs/jobs are not a reason to patch all native hunting globally. Prefer declared native extension
points and narrow owned classes; use Harmony only where inspected semantics require interception.

| Group | Required effective meaning | Failure behavior |
| --- | --- | --- |
| Core station execution | Native worker/weapon/ideology/access admission, station-only intent, real departure/attacks, current authorization, reservations, timeout and load lifecycle | Inactivate affected station execution with UI reason; clear unsafe pending owned intent while preserving ordinary manual work and real loads |
| Native finish/corpse bridge | Correct downed/death identity, dangerous-death branch, one finishing actor, real corpse/carry/destination transition | Do not start/restore station hunts whose complete safe lifecycle cannot be provided; no partial attack-only fallback |
| Catalog source adapter | Correct map/context/species rule and side-effect-free possibility query | Mark unavailable coverage; retain admitted/observed species without claiming an exhaustive provider catalog |
| Optional weapon/map/companion adapter | Canonical package/Def identity and exact consumed lifecycle/permissions | Disable unsupported path with explanation; keep unrelated supported paths and ordinary work |

Required checks concern the actual incoming/effective control/data flow, including skip-original
prefixes and changed attack arguments. Compatible foreign owners/unrelated edits are allowed;
hashes, exact versions or whole-body equality are evidence identities, not runtime gates. Every
transpiler validates its actual incoming stream on each run and returns it unchanged on failure;
do not replay foreign transpilers as speculative probes. Startup/repatch handling is idempotent;
unknown predicates/exceptions/install failures roll back only newly owned patches in the affected
atomic group. Emit one bounded startup warning per skipped enabled patch with patch/spec/target,
failed predicate, unavailable behavior and fallback, never tick/job log spam.

Harmony becomes a hard dependency when eagerly used. No source provider, DLC, experimental mod or
Gateway dependency is introduced. Register our own Defs/WorkGivers additively; guard any actual XML
mutation, preserve unrelated lists and optional entries, and declare XML Extensions at first use.

## Risks / Trade-offs

- No-LOS notice is intentional but not hidden-state revelation → require revealed target facts and
  real departure/approach; show geometry without revealing fogged objects.
- Waiting can consume a high-priority Hunting workday → tell the player, retain needs/ordinary
  priority reassessment and provide a clear per-station pause control.
- Species generation varies with DLC/providers/climate → preserve source-tagged possibility and
  observed knowledge, distinguish temporary conditions, record unsupported generator coverage.
- Provisional raised-seat economy and size cutoff may need revision → static comparators and
  HS-Q1/3 before implementation, finalized/material/live workflow review before acceptance.
- Native hunting does not enforce every new mid-job authorization → scoped checks at actual attack/
  finish boundaries and deterministic phase recovery, not a one-time target query.
- Planning in fog is not an active station → retain S028's privacy and reveal validation, S063's
  deferred clearing, and require an actually completed usable seat before watching.
- Multiple features can finish/haul → one claim and physical state, with explicit S065/S021 handoff.

## Migration Plan

No runtime implementation or source migration in this draft. Add station state only through ordinary
new placement; old saves have no implicit active station or blanket animal permission. Version any
future owned saved-state changes with backup/atomic restore and verified postconditions in their
own contract. Unsupported saved work stops safely with physical state preserved. Do not claim safe
uninstall of a save containing new buildings/jobs without a separately verified removal workflow.

## Open Questions

HS-Q1–5 retain provisional balance/policy, potential-species source completeness, classification/UI,
effective native/adapters and composed performance/acceptance. Resolve through research and later
review, not questions asking the user to explain an existing mod. Nothing here accepts runtime work.
