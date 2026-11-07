# S115 — Sensible Bed Ownership research

Owner **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`, **1.7 beta**.
Origin **replacement**, inspected2026-11-06. Documentation/source analysis only.
[Change](../../../../openspec/changes/specify-megamod-bed-ownership/) ·
[coverage](../replacements/sensible-bed-ownership.md) · [ledger](specification-progress.md).

## Public documentation first

The [Workshop page](https://steamcommunity.com/sharedfiles/filedetails/?id=3328702448) describes
per-map bed claims, right-click assignment, multi-selection, searchable assignment, partner-first
ordering, unassignment, other assignable buildings and optional chair sleeping. Its dated update
notes add owner navigation, gravship/ground allowances and preference, main-bed bedroom calculations,
child crib cleanup and several compatibility/save fixes. The page and installed metadata advertise
1.5/1.6, not1.7. Comments about Reverse Commands, null menu results and gravship/larger-bed issues are
investigation leads, not reproduced failures or extra user requirements.

The source link leads to [1trickPwnyta/SensibleBedOwnership](https://github.com/1trickPwnyta/SensibleBedOwnership).
Pinned commit **6395d20a39320aa689380c9c689f11d7a9f895a1**, committed2026-04-23T01:19:17Z.
The GitHub web reader failed on the commit tree, so the public API tree and raw text were retained.
All31 selected text files match their Git blob identities and the installed text after CRLF/LF
normalization; raw byte hashes differ. This is not a claim that rebuilding yields identical binaries.
The selected installed binary was separately decompiled offline and its hook/helper behavior checked.

No explicit license was found in the inspected commit tree or installed inventory; public availability
does not authorize copying. Independently implement behavior. Attribute reference author1trickPwnyta
and the publicly credited inspirations Hultis, The Village Guy and legodude17; icon credit mouse_draws
does not authorize asset reuse. No assembly/source/texture is selected for the product.

## Exact inputs and evidence

Installed root `F:/Steam/steamapps/workshop/content/294100/3328702448`.

| Input | Identity |
| --- | --- |
| Metadata | Sensible Bed Ownership; sensiblebedownership.1trickPwnyta;1trickPwnyta; modVersion1.3.0; required Harmony |
| Selected payload |1.6/Assemblies/SensibleBedOwnership.dll,55296bytes; assembly/file1.0.0.0 |
| DLL SHA256 |7EF8EFCF54C0BD4DB2FEA1748CFDE4E7009049C2AFB24CD3FBE54959C5BA699D |
| DLL MVID |84128cec-f0ce-4e28-8a64-3d8b466626bd |
| Native input |1.7.5000rev1275; Assembly-CSharp.dll15636480bytes |
| Native SHA256 |D55AA8E47FA3C5463CA85B0ECCD51F195B4BBCD04F56DA3AF21C2F6688A81254 |
| Version.txt SHA256 |BAD5BED449D17B2AF39925A3A25007164B2B1F89B29A4E24AFA3B505EDCC535A |

Payload inventory has65 files across1.5/1.6, including source, projects, languages and icons; no
selected XML PatchOperations/Defs or LoadFolders.1.6 has29 source/project files and11 English keys
(ShowNonLovers is unused by the inspected implementation). Assembly references: mscorlib,0Harmony,
Assembly-CSharp,System.Core,UnityEngine.CoreModule. No assembly was loaded or executed.

Ignored evidence: `artifacts/Refinement/megamod-spec-20261106/sensible-bed-ownership/` contains public
description/changelog HTML, commit/tree and31 public texts,36 hashed installed/native inputs,
PE metadata, full selected decompilation, nine current native type extracts, bounded consumer scans
and subsequent document/review/validation evidence. About/Preview/ModIcon and UI textures were
inventoried only. No player-visible art selection or game launch occurred.

## Complete selected behavior inventory

Selected DLL:45 decompiled source-level classes,35 attribute-marked patch classes,42 total
Prefix/Postfix/Transpiler methods including manually registered constructor and optional adapter.
Initial registration uses unguarded PatchAll plus those manual hooks, not owned admission rules.

| Source area | Actual inspected behavior |
| --- | --- |
| Utility / cache | Static all-map HashSet of colonist Building_Beds rebuilt from native listers when dirty; filters current OwnersForReading per query; dirtied by bed spawn/despawn/building faction and world clear |
| Utility / classification | All footprint foundations exactly Substructure means ship allowance; no ship identity. Normal versus deathrest uses exact DeathrestCasket Def equality, not all special subclasses |
| Utility / choice | Preferred bed weight maximum; otherwise ground+1, substructure-1. Current-map main candidate can include caskets; fallback home map with lowest uniqueID, then any lowest-ID map. Single assigned bed returned even if remote |
| Utility / current map | Handles direct map, containing Thing/ThingComp and carried pawn. AssignedDeathrestCasket picks first local casket rather than weighted preference |
| Pawn_Ownership getters | OwnedBed replaced by GetMainBed; AssignedDeathrestCasket replaced by current-map casket lookup |
| ClaimBedIfNonMedical | Prefix evicts a nonpartner first if full, otherwise last owner; transpiler limits old bed/casket removal to new bed's map and substructure class |
| ClaimDeathrestCasket | Prefix unassigns existing occupant; transpiler limits casket removal to local allowance |
| Pawn_Ownership.ExposeData | Suppresses selected native bed unclaim/reclaim reconciliation; postfix saves preferredBed reference, clears destroyed preference on saving |
| UnclaimAll / guest status | Postfix removes all discovered bed/casket claims; status changes remove mismatching prisoner/colonist claims across maps |
| Building_Bed.RemoveAllOwners | Replaces native removal with direct target-comp unassignment and normal lost/destroyed messages for each owner |
| CompAssignableToPawn_Bed | TryUnassignPawn removes from that comp, not remote main bed; updates rooms and uninstalled list. AssignedAnything uses current-map bed. PostPostExposeData entirely skipped |
| Deathrest comp | Direct target removal, room notifications; its postload cross-check entirely skipped |
| Pawn.ExposeData / MemoryUtility | Creates missing ownership tracker at postload; dirties cache and clears preferences on world clear |
| Generic assignable gizmos | Under ordinary assignment visibility: Unassign all, per-owner preference for single-selected bed with multiple assignments, and per-owner Go to selection/camera |
| Assignment dialog | Shared static QuickSearchWidget, constructor reset, desktop focus except Steam Deck; unassigned candidates require CanAssignTo and matching short label; owners remain; lovers first/name order and relationship labels; native geometry altered |
| FloatMenuOptionProvider | Drafted/undrafted/multiselect. Uses selected player pawns in actual candidates. Groups filter CanAssignTo, check total slots, ideology and pair willingness; remove end owners until room, then assign each. Single rejects with reasons/already assigned omitted |
| FloatMenuMakerMap | For selection with no ordinary orderable pawn, builds context and queries own provider while skipping original GetOptions; postfix initializes result if null and appends options |
| RestUtility | Replaces ownership reads and resolves lover's assignment using sleeper's map; uses an assumed relation local in transpiler |
| LovePartnerRelationUtility | Main-bed disliked nonpartner restricted to same-map owners; after share attempt, claims free-slot beds from both partners across maps if willingness passes, then reports true |
| Relationship workers | Breakup/divorce, proposal without surviving love relation, and old-lover removal call all-map shared-bed cleanup; random50/50 owner removal per shared bed |
| Ideology / child life stage | Unassign forbidden normal beds on ideology change; child transition removes beds below life-stage bodySizeFactor threshold |
| Chair-rest helper | SleepInChairs true and comfort need; tries4 then12 nearby cells, edifice isSittable/positive Comfort/native IsValidCell; changes assumed local2 in ground-spot search |
| ReservationManager | Any reservation by same pawn on a bed returns success, ignoring job/layer/count; not restricted to one owned rest operation |
| Reverse Commands adapter | Type-name detection of ReverseCommands.Tools.GetPawnActions; inserts ListFullCopy after first FreeColonists call; does not check missing/ambiguous match or canonical package |
| Settings/debug | One global SleepInChairs=true saved key; Debug.Log body compiled only under DEBUG; no bottom tab or debug gameplay command |

Representative source anchors: [Utility](https://github.com/1trickPwnyta/SensibleBedOwnership/blob/6395d20a39320aa689380c9c689f11d7a9f895a1/1.6/Source/Utility.cs),
[ownership](https://github.com/1trickPwnyta/SensibleBedOwnership/blob/6395d20a39320aa689380c9c689f11d7a9f895a1/1.6/Source/Patch_Pawn_Ownership.cs),
[context assignment](https://github.com/1trickPwnyta/SensibleBedOwnership/blob/6395d20a39320aa689380c9c689f11d7a9f895a1/1.6/Source/FloatMenuOptionProvider_AssignToAssignable.cs),
[dialog](https://github.com/1trickPwnyta/SensibleBedOwnership/blob/6395d20a39320aa689380c9c689f11d7a9f895a1/1.6/Source/Patch_Dialog_AssignBuildingOwner.cs),
[seat search](https://github.com/1trickPwnyta/SensibleBedOwnership/blob/6395d20a39320aa689380c9c689f11d7a9f895a1/1.6/Source/Patch_JobGiver_GetRest.cs).
Remaining paths are retained in the complete public/installed inventory, not assumed absent.

## Current native1.7 findings and source risks

Static findings, **not reproduced runtime bugs**:

- Native Pawn_Ownership still has one intOwnedBed and a separate AssignedDeathrestCasket. Claiming
  can remove a prior claim and a full-bed owner; load reconciles references against comp owner lists.
  CompAssignableToPawn_Bed/Casket cross-check those pointers. Multi-map ownership needs coherent
  readers/writers/load handling, not just patched getters or disabled cleanup.
- Generic owner lists are sorted by thingIDNumber; list end is not evidence of latest assignment.
  Generic PostDeSpawn stores reinstall candidates, PostSpawnSetup can reclaim, and PostSwapMap
  removes destroyed or unspawned owners. Exact moving-bed phase handling remains BO-Q1.
- Native dialog includes disabled candidates/reasons. Source filters CanAssignTo out before drawing,
  so it hides useful failures. Its one global search widget and static focused window cross contexts.
  Owned search retains disabled native candidates and uses window-local transient state.
- Source group assignment silently removes ineligible candidates and can remove a selected existing
  owner from the end while making room. Revalidation/atomicity and precise affected identities are
  required. Skipping GetOptions leaves __result null until source postfix; foreign order matters.
- Source TryToShareBed_Int postfix can report true when no bed was shared and performs sequential
  choices in both directions without a stable per-allowance plan. Owned sharing keeps one chosen
  destination and never treats false/no-effect as successful reassignment.
- Source casket choice ignores its own preferred weighting, and fallback ties can depend on cache
  order. Owned choices use stable identity and one local preference policy; ordinary bed/casket
  permissions remain distinct. Preference never bypasses native use checks.
- Child cleanup compares life-stage multiplier, while current native CanAssignTo uses actual
  Pawn.BodySize. Owned validity uses current eligibility, preserving valid claims rather than
  copying a scale mismatch. Other changes receive event-driven validation, not per-tick cleanup.
- Native Reserve already succeeds for an adequate same pawn/job/target/layer reservation. Source
  bypass is broader and can claim success for a different job without adding its reservation.
  It is deliberately not ported.
- Native ground rest checks own cell, searches4/12 and finally a local fallback. IsValidCell checks
  forbidden, avoidWander and CanReserve, not every reachability/environmental condition. Source local
  index matching, first float replacement and unbounded operand assumptions are not admission checks.

Native extracts: Pawn_Ownership, CompAssignableToPawn, CompAssignableToPawn_Bed,
CompAssignableToPawn_DeathrestCasket, Dialog_AssignBuildingOwner, LovePartnerRelationUtility,
JobGiver_GetRest, ReservationManager and Building_Bed. Runtime compatibility remains unverified.

## Observable choices and direct composition

These are **owned draft decisions**, not statements that the source already implements them:

- Retain the source's map/substructure scope and native restrictions; keep generic throne/grave
  scope native. Preference is one pawn choice, meaningful only among applicable owned candidates.
- Single/group assignment is explicit and rechecked, preserves selected incumbents, and never
  silently applies to only part of a named group. Keep meaningful disabled rows and nonowned menus.
- Preserve physical bed identity through load/pack/move. A same-allowance transfer collision leaves
  existing destination ownership active and the other intent visibly pending, with choose/cancel.
- **Resolve the real S073 conflict:** its initial source-derived release default true becomes false.
  Retained ownership is default; optional release uses existing guarded home memory per allowance.
  A same-map temporary bed is a rest/custody target without a second ordinary claim. A deliberate
  assignment on another map no longer cancels the remote remembered intention.
- Bed/deployment precedes seat fallback.4/12 bounds and actual comfort are retained; normal care,
  access, safety and reservations remain authoritative. No new comfort/healing/research/material stat.
- Source optional SleepInChairs becomes a common global/save inherited row. Per-pawn owners and
  preferred beds remain contextual state. All seven languages and native context are required.

## External contracts and search bounds

The full installed text scan for SensibleBedOwnership/package identity across cs/xml/json/md found
only the source and **Use This Instead**, `Mlie.UseThisInstead`, Workshop3396308787, replacements.json.
That data recommends this package as successor to `legodude17.bba`/Workshop2417077360. It is package
recommendation metadata, not a call to an API we can satisfy by matching a method name. A bounded
ASCII binary-name scan found no DLL consumers outside the source's two version DLLs. These scans
do not prove no compiled, dynamically constructed or external consumer exists; no drop-in claim.

Public exposed surfaces include Utility's ownership/preference helpers and AssignOwnerSearchWidget,
settings type/static field, patch types and assembly identity SensibleBedOwnership. Native owner
lists persist in their ordinary keys; source preference is pawn ownership key preferredBed and the
global setting is SleepInChairs. No replacement assembly facade, identity spoof or automatic key
import is selected. Existing source-specific preferences/corrupt-save repair need a separately
specified migration if wanted. Source-provider presence turns owned overlap off with bounded warning.

Reverse Commands is an optional upstream type-detected target, not a demonstrated consumer of
SensibleBedOwnership's API. Do not copy that collection workaround into our direct UI without a
proven need/package/shape. Royalty/Ideology/Biotech/Odyssey are optional content branches. Custom
beds, Hospitality-style assignments, androids/levels and other ownership mods need exact adapters
and separate native verification, not compatibility inferred from comments or class inheritance.

## Open questions retained for implementation

| ID | Work still required | Current draft boundary |
| --- | --- | --- |
| BO-Q1 | Effective1.7 ownership/load/map-swap/foundation semantics, atomic grouping and later repatch predicates | No runtime checks implemented; missing authority stops affected work |
| BO-Q2 | Actual compiled/current optional consumers, Reverse Commands need and any migration | Bounded scans only; no facade, source import or safe removal claim |
| BO-Q3 | Causal native travel/temporary-rest/collision/partner/casket/UI workflows | Source/static native evidence is not live acceptance |
| BO-Q4 | Seat comfort/playability, shared-data cost, seven-language UI, TDD/review/package evidence | Physical capacity and normal restrictions preserved; measurements/acceptance pending |

## Verification status

Source inventory, public/installed text equivalence, selected binary inspection and native shape
research completed read-only. Independent provenance review found no issues. Behavior review's two
S073 findings were corrected: explicit release-enabled scenario preconditions and displayed-order
reclaim victim selection, with a distinguishing scenario and planned test. Focused re-review found
no remaining issues. Strict typed validation passed137 items, zero failures. The ledger retains the
checkpoint and limits. Every implementation/acceptance task remains unchecked. No build, install,
game run, normal-save/config change, Workshop mutation, source execution, asset selection or
publication is part of this work.
