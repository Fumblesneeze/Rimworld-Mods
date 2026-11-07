## Context

Sole owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
S030 replaces Brrr and Phew (Continued), `Mlie.BrrandPhew`, Workshop 2195938471. The
[research report](../../../mods/megamod/docs/refinement/2026-11-02-brrr-and-phew-research.md)
pins public/installed/native evidence and separates source defects from authored corrections.
No code, Defs, runtime dependencies, tests or live evidence are delivered here.

Source 1.6.8 is built for advertised game versions through 1.6. Native research is 1.7.4995 rev309.
Its ordinary integration uses inserted ThinkTreeDefs; copying priorities/tags is not sufficient to
prove eligibility or ordering in beta. The separate Toddlers tree lacks Humanlike_PreMain and
uses injury-required native temperature response in one later branch. This is a concrete seam to
investigate, not a live reproduction of the user's reported symptom.

## Goals / Non-Goals

**Goals:** separately testable recovery, outdoor-break and toddler-safety behavior; real physical
access and exposure; retained native needs/care; direct composition; shared settings and bounded
failure; documented minimal patch prerequisites; honest optional-provider and save boundaries.

**Non-goals:** implementing now, a generic hazard framework, new healing or protective content,
general toddler/caregiver replacement, global pathfinding changes, source-identity spoofing,
automatic settings/save migration, multiplayer certification or a duplicate map/Thing cache.

## Decisions

### 1. One owned decision path with native jobs and health semantics

Keep the closely related code under `Source/Features/EnvironmentalResponse/` when implementation
begins. Use small explicit decisions for eligible causes, refuge suitability and activity choice;
ordinary directly called policies compose with the future owned toddler life stage and S002/S024.
Do not recreate eleven independent scanning job givers or use Harmony on our own implementation.
No interface hierarchy or registry is justified merely by the source's class count.

Native emergency/player/lord/medical behavior remains authoritative. Hook at the narrow job-selection
seams proven by the implementation slice; do not blindly prepend to the entire human or animal tree.
Adult/animal source eligibility and needs interruptions remain explicit, including following-master
exclusion. Toddler early escape is a distinct movement decision that must run before autonomous
idle/play and does not inherit the source hungry/low-joy admission block.
S065's explicit temporary hunting-companion yield composes through the same current follow-authority
decision. A still-enabled saved fieldwork flag is not active following while that owned suspension
holds. Preserve the flag and ordinary non-companion exclusion; do not immediately re-recruit the
yielding animal through native ShouldFollowMaster before its admitted recovery can run.

Use native rest/sleep/comfort/bed eligibility where they satisfy the contract. Do not clone the large
source lying-down toil with its mismatched smoke name, race-only thermal thoughts or downed teleport.
Owned job state retains only active recovery reasons and necessary targets. Reconcile causes with
current settings/health; never delete a health condition merely to end a job. Normal native swim
cooling is a separately admitted physical activity, not an owned direct health mutation.

### 2. Actual cell exposure, native candidate sources and use-time validation

Reuse native regions, bed listers and map hazard data. Candidate collection is not proof that the
pawn can use a location. Validate the final cell, access/doors, reservations, bed ownership and
actual thermal/exposure facts immediately before commitment and during the job. Search all
relevant suitable beds in native preference order before dry floor fallback, without claiming or
evicting another owner. The source's fourth FindBedFor argument ignores reservations; it must not
be used as an allowed-area override. The latter is a narrow separately applied policy.

Keep comfortable recovery distinct from merely nonworsening safe temperature. Native cold/heat
recovery has strict boundary conditions; a closed-range comparison is insufficient. Standard
health conditions are the initial supported set. Insectoid/other alternate conditions require
explicit proven mapping, not a fuzzy name match. Bed/floor decisions also consider currently
supported other hazards; a roof alone is never oxygen/smoke/vacuum evidence.

For an injured toddler, prefer an improving refuge and permit a merely nonworsening safe escape
if no improving one exists. Other original recovery remains a suitable-recovery search, without
an invented cure or best-effort dangerous rest. Escape paths need not have a safe starting cell;
new idle/play paths must not introduce avoidable thermal exposure. Use native path constraints
plus the narrow candidate/route predicates needed here; do not patch all pathfinding.
Safe arrival always ends escape movement. A remaining above-threshold injury does not convert a
merely nonworsening refuge into an eligible ordinary recovery bed/hold; hand back to ordinary care
and safe activity, with no fresh escape while the current location is thermally safe.

Searches are bounded. The spec's proposed 250-tick ongoing safety evaluation and 500-tick unchanged
no-route retry are named constants in the first implementation, with focused behavior tests and
native timing evidence; they are not additional settings yet. Changed relevant facts allow an
earlier retry at a safety evaluation, not unbounded every-frame replanning. The inspected native
safe-temperature wait of 500 ticks is a comparator; these authored bounds are not source defaults.
Avoid per-pawn full-map scans at each evaluation. Measure decision/update cost with multiple pawns
and no-route cases before claiming performance. Do not create a secondary Ctrl+F/hauling Thing index.

### 3. Preserve intended swimming and outdoor behavior through real activities

Heat response considers a local eligible swim within the source's 29-cell radius before dry refuge.
Use Odyssey's actual capability, route and health outcome; Royalty presence is not that capability.
The source's two GoTo toils/lastSwamTick cannot stand in for a native swim. Reconcile swimming with
the same active causes and interruption policy, without two competing jobs or duplicate joy gain.
Confirm how native swim completion and residual causes hand back to recovery; health must not be
cleared by synthetic setup in acceptance.

Outdoor breaks choose from feasible skygaze/walk activities. Preserve source 45/99 weighting when
both exist, half equivalent native recreation, 75% outdoors goal and 4,000/6,000-tick caps. Do not
copy the requirement to find both activities before choosing one. The timetable still permits joy;
this does not add an unrestricted mandatory break during every schedule assignment. Source
allowed-area exemption is explicit and separate from recovery's outside-area setting. Hazards and
life-stage physical eligibility still apply. Preserve descriptive reports and ordinary gain clamping.
Full recreation does not end an outdoors-need break or block its selection: do not inherit the
native recreational job's full-joy early exit, or let S024's completed-recreation preference veto
this distinct need. Keep the outdoors goal/duration and ordinary safety/control ending conditions.
S002 directed recreation retains its current player-directed job and own scope; S024 can share
native needs/physical facts but must not schedule an additional job or reward for the same action.

### 4. Toddler composition uses actual mobility, not age guesses

S031 will own the full life-stage definition, learning, crib and caregiver behavior. This change
owns only the user-confirmed thermal prevention and early escape. Owned calls use those established
life-stage/mobility facts directly. For an external `cyanobot.toddlers`, resolve only the admitted
life-stage/learning/door/crib behavior behind a narrow optional adapter; avoid eager assembly type
references. Source evidence says crawler stage 0 cannot open doors or leave cribs, whereas later
walkers can differ. Never flatten this to all age-one/two pawns or CanMove alone.

Reservation/carry handoff must be checked before independent motion. Existing adult pickup/carry
takes precedence; do not suppress a needed rescue simply because some geometrically close cell
exists. Only actual toddler reachability can establish self-escape. The child's and caregiver's
reachability are distinct. No-route and single-cell cases must terminate, leave ordinary rescue
available and avoid repeatedly undoing safe placement. Integration with a provider that cannot
expose these semantics is unavailable until its target contract is established.
The inspected external tree selects SeekAllowedArea before SeekSafeTemperature. Prevent automatic
area return from immediately undoing self-escape or safe caregiver placement outside a still-unsafe
assigned area. This requires admission of that additional decision boundary, not only an idle
destination filter. Keep the area assignment and explicit player orders intact; resume permitted
return when both destination and route are thermally safe. Waiting at the refuge for that reason
is not an injury-recovery hold and does not require an illness to disappear.

### 5. Common settings, activity state and localization

Use the foundation's common typed definitions/resolver/store/renderer, under ordered Environment
sections. Source defaults and ranges are in the observable specs and research table; both
blood-rain settings are now persisted. Store per-save override/inheritance independently of global
defaults. Reset numeric values only in the explicit editable scope; do not accidentally reset
switches or a hidden save. Unavailable content leaves values dormant, with a disabled explanation.
No title/version-dependent settings identity, implicit source migration or per-feature ModSettings.

Activity state belongs to jobs/pawns, not global settings. No new bottom bar control is necessary;
these switches configure automatic decisions, while ordinary reports remain on the selected pawn.
Use native text where appropriate and add all new headings, scope/availability labels, reports and
tooltips in the seven baseline languages. Verify layout at main menu and loaded save, not only keys.

### 6. Optional providers and proven incoming bindings

| Provider | Evidence / intended boundary | Required proof before support |
| --- | --- | --- |
| Anomaly | Native BloodRage and exposure rules | Exact current content and physical blood-rain shelter/recovery |
| Odyssey | Native vacuum and actual swimming | Separate vacuum/cooling admission; physical pressure/swim workflows |
| Biotech / owned S031 | Toddler context and owned lifecycle | Actual selected life-stage and mobility/care semantics |
| `cyanobot.toddlers` | Inspected external life-stage/door/crib/think-tree behavior | Exact beta-compatible assembly and actual toddler/caregiver workflows |
| More Events | Public source promises OxygenStarvation/heavy-air response | Canonical identity, actual content/exposure/recovery still unresolved; no name-only implementation |
| `CETeam.CombatExtended` | Source optional SmokeInhalation response, default off | Current smoke exposure/recovery and package/shape semantics; no presumed equivalence to native smoke |
| `VanillaExpanded.Temperature` | Actual incoming Brrr consumer; selected 1.6 DLL explicitly used by its 1.7 LoadFolders mapping | Local effect input/baseline, enable state, map lifetime and exact-once effective temperature |

The current VTE binary gates source hooks by exact active title, then patches Brrr job givers,
GenNewRRJob and Phew. A replacement facade cannot honestly satisfy that title check without
impersonating the source. Instead consume the provider's local temperature through a narrow
shape-guarded adapter. General pawn AmbientTemperature and candidate-cell temperature do not
necessarily share the same provider path; the inspected TryGetTemperatureForCell patch only
participates in plant-growth context. Prove baseline and avoid applying the effect twice.

Missing optional providers are dormant, not errors. Active incompatible enabled branches emit
bounded admission warnings and stop using untrusted safety information. If a shared temperature
provider invalidates all thermal decisions, withhold the affected thermal consumers together;
independent understood toxic/blood behaviors can remain only if their full refuge predicates are
still provable. Do not silently fall back to ambient room temperature while advertising local-heat
safety. No version/hash allowlist and no probing by executing foreign transpilers repeatedly.

Original `Mlie.BrrandPhew` active: withhold owned overlapping recovery/outdoor groups and revalidate
any saved owned jobs into ordinary control. The novel toddler prevention/escape can remain only
with separately proven nonduplicating control; otherwise withhold it too with a reason. Never
unpatch the original or edit the active mod list. No blanket drop-in, removal or multiplayer claim.

### 7. Candidate target families and admission

These are **design candidates**, not completed per-patch target documents or executable guards.
Before any runtime change, create stable-ID target documents with actual member/Def predicates,
owning scenarios, validator, failure group and required player evidence. First inspect current
effective 1.7 paths; source class names are not sufficient contracts.

| Candidate family | Minimal semantics that must be demonstrated | Failure scope |
| --- | --- | --- |
| Recovery selection and owned job continuation | Selected native path reaches ordinary eligible job selection without overriding emergency/control; exact supplied pawn/job/return shape | Relevant adult/animal response group |
| Additive human/animal/toddler tree integration | Exact target Def and insertion context exist, intended urgent predecessors remain, own nodes inserted once; finalized tree still supplies the decision path | Full selected tree/job group, not half an insertion |
| Toddler idle/play filtering, escape and automatic area return | Actual selected candidates/route and current mobility are consumed on the relevant path; effective control cannot bypass the required veto or immediately return a safely placed toddler to thermal danger | Toddler safety group |
| Ongoing rest/swim/outdoor continuation | Native action performs the expected physical activity and applies ordinary completion/need/health effects; owned callbacks have correct lifecycle | Specific activity, with independently admitted alternative |
| Optional exposure/local-temperature/mobility adapter | Canonical provider plus actual Def/type/member shape and meaning, effect timing and lifetime | Adapter plus consumers whose safety depends on it |

An observer only needs existence/call shape when no body semantics are assumed. Any changed argument
or injected decision requires the relevant structural use/data flow, including prior transpiler
effects, skipping prefixes and later repatching. Every transpiler rechecks the incoming stream and
returns it unchanged on failure. Tolerate unrelated instructions, compiler locals and compatible
foreign owners. Never use PatchAll, exact whole-body matching or source/game hashes as runtime truth.

For declarative additions prefer XML/Defs. XML Extensions must guard required content/node shape;
external targets additionally require FindMod with packageId=true. Inspect actual operations before
choosing syntax. Add nodes/membership without replacing entire trees or discarding unrelated jobs.
Add XML Extensions as a hard dependency when first used, and Harmony when eagerly referenced.
Admission must occur at a documented phase before invalid nodes become usable; prove finalized
tree and error-warning behavior. If XML's phase cannot support an atomic safe edit, resolve the
mechanism before implementation rather than doing ad hoc runtime Def surgery.

One bounded startup warning per skipped enabled patch identifies package/patch/spec/target,
failed predicate, lost player behavior and fallback. Roll back only newly installed owned members
of the declared atomic group. Failure never unpatches another owner or releases an invalid saved
job back into active execution. Missing optional content is not an enabled failed candidate.

## Risks / Trade-offs

- **Source hunger/recreation interruption can delay survival** → retain and expose it explicitly;
  toddler escape is independent. Verify actual trade-off in play rather than claim guaranteed safety.
- **Outside-area outdoor breaks surprise players** → document retained source behavior separately
  from recovery's configurable area exception; physical restrictions/hazard checks still apply.
- **Strict recovery safety has no solution on some maps** → bounded no-route result and ordinary
  rescue/care, never fabricate a refuge, change the weather or erase an injury.
- **Beta/provider behavior changes** → semantic admission and exact tested build/provider evidence;
  current inspection does not accept source 1.6 binaries on 1.7.
- **Cell-local heat differs from room temperature** → one admitted provider calculation, use-time
  validation and actual pawn outcome; withhold dependent claims when semantics are unknown.
- **Rescue and autonomous escape race** → native reservation/carry handoff with bounded retry;
  exercise real caregiver actions rather than only synthetic position assertions.
- **Frequent decisions become repeated map scans** → reuse native listers/regions, measure bounded
  decisions, share concrete facts with existing consumers only when a real benefit is demonstrated.

## Migration Plan

No migration is performed or implied now. Implement one owned behavior at a time behind admitted
groups, with exact provider/absent cases. Owned settings start at documented defaults/inheritance;
source globals are not imported. Owned saved jobs are revalidated on load and invalid jobs yield
to ordinary control. No automatic removal or rewrite of source jobs/settings/saves is proposed.
A later importer or source-removal tool needs its own backup, atomic commit, restoration and
verified-postcondition contract. Runtime rollback withholds only owned groups and releases owned
reservations/state; it must not mutate normal user configuration during verification.

## Open Questions

**BP-Q1–4** in the research/progress ledger retain provider research, exact native/toddler control,
proposed timing/needs/area/swim playability, and implementation/live evidence. They do not ask the
user to explain source behavior and do not authorize treating unknown branches as complete.
Native admission, final package metadata and all runtime acceptance remain future work.


## Initial support scope — decision2

Multiplayer and Combat Extended adapters, synchronized sessions, CE-only controls/properties and
provider acceptance are deferred. Any source CE/MP implementation details above are retained research
for a future scoped change, not initial work. Implement and accept the single-player non-CE contracts.
Ordinary save/load of colonies started with megamod remains required.
