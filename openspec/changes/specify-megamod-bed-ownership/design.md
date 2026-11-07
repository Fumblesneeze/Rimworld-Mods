## Context

Owner **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`, **1.7 beta**.
S115 documentation-only design. The inspected source advertises1.5/1.6, not1.7; no source or owned
behavior was run. See [research](../../../mods/megamod/docs/refinement/2026-11-06-sensible-bed-ownership-research.md)
for pinned public source, selected DLL, current native shape and consumer-search limits.

The source changes a native single-bed ownership model through many coordinated patches. Current
1.7 still stores intOwnedBed and reconciles it against building owner lists on load. A getter-only
port cannot safely deliver multi-map ownership. S073's pending home memory is also not an active
bed claim: releasing on every departure would defeat S115's primary behavior.

## Goals / Non-Goals

**Goals:** coherent per-map claims, safe contextual controls, meaningful travel bedroom information,
normal physical sleep and sharing, direct portable-bed composition, bounded failure and saved intent.

**Non-goals:** more sleeping slots, automatic third-party save conversion, universal custom-bed support,
new beds/art, throne/grave multi-map ownership, a reservation-system rewrite, runtime implementation,
or a second all-Thing index. No public/reflection compatibility facade without an actual consumer.

## Decisions

### One owned policy with native physical buildings

Use the actual CompAssignableToPawn owner lists for installed building ownership and one narrowly
owned game-scoped policy for lookup/claim/unclaim/reconciliation. Its key is pawn + map + assignment
kind (ordinary bed versus supported deathrest) + wholly-substructure boolean. This deliberately
matches the source's two allowances, not a speculative per-ship ownership framework. Derive the
substructure classification from the current complete footprint; a foundation change or map move
can change the key. Resolve unsupported custom bed kinds behind package/Def/shape-guarded adapters.

Keep saved preference, suspended packed claims, collision intents and S073 pending home intentions
as distinct records referencing actual pawn/bed/map identities. The active owner list is not a cache
of those intentions. Choose a stable bed identity for an otherwise equal priority; stable native map
creation identity supplies the observable oldest-map fallback. Current/containing map selection
precedes fallback home-map information; local sleep queries always require actual local eligibility.

Explicit full-bed assignment preflights eligibility, sharing and capacity before any mutation. Plan
only the required nonselected removals in displayed owner order, preserve selected incumbents, then
commit through the one owned policy with postconditions and rollback of this attempt only. Do not
overwrite intervening external/player edits. Distinguish ordinary explicit reassignment, native
relationship sharing, map move, install/load reconciliation and temporary S073 operations; caller
name alone does not establish intent. Do not add global Harmony interception to simulate our own API.

Current native ForceAddPawn sorts by thingIDNumber, not assignment time. Do not call that chronology
or use a HashSet iteration order as the displayed-owner removal contract. Partner sharing snapshots
the two sets before choosing at most one shared bed per allowance. It runs on a real native share
event, not as a periodic enforcer of couples' arrangements. Breakup cleanup checks the actual ended
relation and both current owner memberships, then performs one equal-probability choice per bed;
no persisted random reroll or repeated replay on load.

### Lifecycle and portable-bed composition

Update S073's unimplemented contract in place: release-on-departure false, with a single settings
definition. Optional true releases only the eligible home claims being left behind, recording each
map/allowance separately. A bed carried away on the same gravship is a moved assignment, not a
left-behind reclaim destination. Retained claims do not create pending memory. Existing remembered
records are canceled by explicit edits to their allowance or selected memory, not arbitrary bed
claims on other maps. A pending slot obeys S073's no-sleeper-displacement and co-returner protection.

Temporary physical deployment uses S073 custody/commitment records. On a map/allowance already
holding an ordinary claim, a valid unowned temporary bed is a rest target without a second ordinary
claim; ordinary native rest/claim paths must support that distinction before admission. Do not
silently release the permanent claim to force deployment. Where this cannot be proven, disable that
affected deployment path and retain normal rest. Recovery manipulates the actual portable item,
not the main-bed accessor. S062 counts the one physical commitment, never another fictional item.

Despawn/uninstall/move/destroy have different meanings. Capture and reconcile at committed phases,
not every DeSpawn. Reinstall may restore only a still-authorized claim in a free allowance. Native
PostSwapMap can remove absent owners: inspect the actual current transfer path before deciding
which cleanup to preserve or replace. Destination collisions retain the established claim and
store moved intent as nonactive pending gameplay state with choose/cancel controls. With no
established winner, a stable identity selects one; do not apply multiple active claims or evict a
third party. Reclassification uses the same policy, not another ship-specific cache.

Do not broadly skip PostPostExposeData or delete all native repair logic. Preserve unrelated
grave/throne/meditation/casket reconciliation, remove invalid references and reconcile owned keys
once after reference resolution. A missing ownership tracker is an exact native validity question,
not authorization to blindly repair arbitrary third-party saves as the source does.

### Native listers and shared derived data

Use map.listerBuildings and actual assignment lists first. If repeated consumers justify a derived
index, add a bed/owner projection to the existing shared map dataset: physical bed ID, assignment
kind, footprint classification and current owner IDs. The game-level view composes per-map results;
it is not a static source-style HashSet over all maps. S073, search and rest consumers share the
projection. Queries never cache pawn-specific eligibility, reservations, access or final job choice.

Invalidate on claim/unclaim, spawn/despawn, destruction, faction/status changes, foundation changes,
map/gravship transfer, restore and teardown. Rebuild derived state after load; clear previous-game
references. Run on the Unity thread and never serialize a live cache. Recheck physical identity and
permissions at action time, use a native bounded scan if the projection is unavailable, and stop
mutation if the required ownership authority is unknown. Measure combined query/update/memory cost
before any optimization claim. A generic cache framework is not required by this design.

### Contextual UI and configuration

Extend native Dialog_AssignBuildingOwner and ordinary right-click/gizmo surfaces. Keep a per-window
query/focus state; use literal matching of displayed short names. Do not reuse the source's global
QuickSearchWidget. Build rows from current native candidates, retaining disabled native candidates
with rejection text and excluding duplicate assigned rows. Keep owners outside the filter. Sort
partners first, then names with a stable identity tie; annotate relationships without replacing
native errors. Preserve controller behavior and other admitted contributions.

Right-click animal support must add only the needed assignment options without skipping the entire
GetOptions result or leaving a transient null result for foreign postfixes. Start from a snapshot
of selected pawns and present exact affected identities. Revalidate eligibility/group shape at
activation, not on drawing alone. Owner navigation uses ordinary supported map/world targets with
preflight before clearing selection. Preference and pending claims are saved entity state, never
configuration. Reuse native controls where possible; no source texture is selected or authorized.

Common settings definition: boolean seat-rest enabled true, Pawns / Rest and portable beds after
the single home release/reclaim options. Shared release default false; no inheritance exemption.
All global/save/Use-global behavior follows the foundation. Search is transient; preference is
saved. Required languages: English/German/Spanish/French/ChineseSimplified/Russian/Japanese.

### Seat rest and reservations

Compose rest selection directly: actual usable bed, then eligible S073 deployment, then comfortable
seat fallback, then native ground fallback, subject throughout to medical/duty/timetable/safety
authority. S073's opt-in distant-bed substitution retains its existing scope. Seat search uses4 then
12 cells as named, specified constants; positive Comfort + isSittable is insufficient without
ordinary cell/access/reachability checks. Do not match the source's Stloc_2 or assume native IsValidCell
proves all safety: inspected1.7 checks forbidden, avoidWander and CanReserve only. Recheck at use.

Do not port the global ReservationManager.Reserve prefix. Native1.7 already recognizes an adequate
reservation for the same pawn/job/target/layer; the source shortcut ignores job/layer/count and can
report success without acquiring the new job's reservation. If owned rest still needs a repair,
prove its specific workflow and guard that exact behavior rather than bypassing global reservation.

### Admission candidates, not executable checks yet

Every actual patch gets a stable target document and executable validator before code. The table
describes semantic dependencies, not permission to PatchAll or a claim they are validated.

| Candidate boundary | Minimal consumed semantics and atomicity |
| --- | --- |
| Ownership lookup/claim/unclaim + building assignment | Correct pawn/bed and map/kind classification; native removal/addition sites and returned success; reader/writer/load group admitted together |
| Save/load reconciliation | Reference resolution, owned/native list updates and unrelated assignment reconciliation; never globally bypass all reconciliation |
| Despawn/reinstall/map-swap/destroy/faction | Distinguish actual committed transition, retained identity and notification scope; projection invalidation alone can use a callback shape if only observation is needed |
| Foundation/gravship transfer | Real footprint and destination state available before reconciliation; paused/suspended/committed phases distinguished; unsupported transfer disables that adapter |
| Bedroom/RestUtility partner selection | Main-information versus local job use; sleeper's map reaches the candidate check; preserve native medical/reachability predicates and no remote target |
| Relationship/status/life stage | Actual completed change and affected pawns; skip-original and foreign replacement effects accounted for; no cleanup from merely entering an event method |
| Dialog rows/search/sort | Actual candidate/owner collections, row reasons and scoped window geometry; no first-float20 replacement or uncontrolled replacement of third-party columns |
| Assignment menu/gizmos/navigation | Current selected pawns, ordinary eligibility and owner identity; add options without suppressing unrelated menus; nonnull iterable/result contracts |
| Seat ground-rest fallback | Actual fallback cell/result and successful eligible search; no assumed local number, preserve native validation/return behavior |
| S073 departure/temporary rest/reclaim | One shared authority and transition intent; no duplicate release settings or main-bed shortcut for recovery |

Match required call/data flow/cardinality in original and effective behavior as appropriate. Compatible
foreign owners are allowed. Account for skip-original prefixes, inherited methods and late repatching.
Each transpiler checks its actual incoming stream on every invocation and returns it unchanged on
failure; no speculative reruns of foreign transpilers. False/unknown/ambiguous predicates, exceptions
or install failures roll back only newly added owned members of the declared group. Emit one bounded
startup warning per skipped enabled patch with patch/spec/target/reason/behavior/fallback. Explicitly
disabled features are not failed candidates. Later invalidation stops new owned work until safe
revalidation; saved state is not a license to act with unknown authority.

Sensible Bed Ownership active by canonical package ID selects the original for overlapping behavior.
Keep owned overlap off; do not remove its patches or import its settings. Other ownership providers
need proven adapters or the affected owned authority stays off. A bare ReverseCommands.Tools type
lookup is not enough. No automatic compatibility patch for Reverse Commands is selected: first
identify its actual package, consumed collection contract and a demonstrated need in our own UI.

## Risks / Trade-offs

- Multi-map getters can affect distant room/royalty/love consumers → distinguish local actions from
  informational fallback and verify exact consuming workflows; no blanket external compatibility.
- Source support stops at1.6 → current1.7 semantic predicates and live workflows remain mandatory.
- More retained beds can reduce available beds for newcomers → ownership is intentional, explicit
  reassignment remains possible and optional release is visible; no new capacity/reward is created.
- Seat sleep increases ordinary comfort opportunities → bounded search, real seat properties and
  care/environment vetoes; compare outcomes to native ground/bed rest at implementation acceptance.
- Save/provider authority changes can invalidate reconciliation → preserve records read-only, stop
  affected work, explain limits; no safe removal or automatic rollback promise for unsupported states.

## Migration Plan

No migration runs in this docs phase. Future owned records use their own versioned save keys, not
source preferredBed or SleepInChairs aliases. Preserve existing valid native assignments when first
enabled without inventing extra historical beds. No automatic source configuration, preference or
corrupt-save import. Any later importer needs separate observable backup/atomicity/restoration and
verified postconditions. No source code/assembly/art reuse is selected; no explicit upstream license
was found in the inspected commit/payload. Native art reuse, if selected later, still requires the
repository asset provenance/live visual gate.

## Open Questions

- BO-Q1: exact beta effective ownership, transfer/foundation and load dependency groups, including
  later repatching and explicit native all-unclaim entry points.
- BO-Q2: current third-party assignment/room/level consumers and Reverse Commands need; unsupported
  bindings/migration remain explicit rather than being called drop-in compatibility.
- BO-Q3: native player workflows for home/camp, same-allowance temporary rest, carrier/world lookup,
  relationship/casket rules and collision resolution; no static-source result accepts them.
- BO-Q4: final seat/ownership playability, shared-index measurement, seven-language/contextual visual
  checks and implementation TDD/review evidence. Record answers later without an interview now.
