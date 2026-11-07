## Context

Sole owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
S076 references `Yakobo.LinkableHydroponics`, Workshop3535816519. Full supplied source and selected
hydro.dll were read alongside native1.7.4998rev1299 and the selected VFE Farming/Framework code.
[Research](../../../mods/megamod/docs/refinement/2026-11-04-linkable-hydroponics-research.md) records
hashes, lookup limits and source behavior; [coverage](../../../mods/megamod/docs/replacements/linkable-hydroponics.md)
records deliberate differences. Nothing is implemented or live-verified.

The source adds comps through XML, stores global static groups, broadcasts crop setter arguments,
merges entire represented groups and does not initially synchronize crops. Native buildings store
one crop and expose no zone-style allowSow/allowCut fields. Native crop commands combine temporarily
selected growers and filter through effective native/provider eligibility. Their constructor assumes
a non-null crop: a missing-content recovery view cannot simply call it with all-null choices.
Native and inspected VFE grower inspect strings also call GrowthSeasonNow with that crop, which
dereferences it when a room is available. Both inspection and command creation require recovery.

## Goals / Non-Goals

**Goals:** coherent persistent building crop groups, explicit source/membership, contextual controls,
native crop/work preservation, bounded lifecycle, and direct composition with S006.

**Non-goals:** mixed building/zone membership, new basin work switches, new crops/buildings, growth
or cost changes, extra content dependencies, a general-purpose linking framework, foreign group
import, automatic adoption of arbitrary external writes, or claims that 1.6 source works on beta.

## Decisions

### One small owned grouping policy, two concrete member kinds

Share proven operations for group membership, naming, affected-member deduplication and command
transactions with S006. Keep building groups crop-only and zone groups crop/sow/cut. Do not introduce
a runtime library mod or patch our zone implementation. Native crop commands spanning both kinds
resolve the union of affected owned groups plus independent selected growers once; permissions stay
with zones. Reject mixed membership rather than silently inventing work flags for buildings.

Align link gestures with S006: first selected eligible source shown before activation, immediate
crop synchronization, existing source group retained, selected incoming members moved. Source whole-
group implicit merge is deliberately replaced; players can select all intended members to move them.
Disband remains whole-group and clearly names its scope. Naming is an explicit UX extension.

### Native crop state, map-owned membership, no static comps required

Use a map-owned record of stable group identity, members and saved crop identity; native building
fields remain the crop state read by ordinary jobs. Retain a small per-building unresolved-crop
record when necessary after unlinking. Store only real membership/recovery state, not a private
all-Thing cache or duplicate map inventory. Resolve references through native save references or one
shared map lookup after load; never scan every map's AllThings for every saved integer.

The source comps carry only group IDs and helpers; no static Def contribution is needed for our
map-owned representation. Therefore no XML patch is selected. If implementation demonstrates a
necessary static addition, express it with guarded additive/idempotent XML Extensions, package/Def/
shape predicates and hard dependency/load-order metadata; do not mutate Defs from C# as a shortcut.

Persist through the map component's native lifecycle, not SaveGame patches or paused OnGUI snapshots.
Remove members on actual despawn/destroy/map transfer, not a 300-tick sweep. Distinguish saved placed
member restoration from later reinstall/respawn; clear game/map references on teardown. An object's
former identity cannot restore membership after uninstall. Same-map/player-control validity is
rechecked at command time. Audit faction/control changes as part of invalidation.

### Eligibility and mutation are one admitted transaction

Use the effective `PlantUtility.ValidPlantTypesForGrowers`/`CanSowOnGrower` and
`Command_SetPlantToGrow.IsPlantAvailable` behavior for the full affected set, not sowTag equality.
The current IsPlantAvailable short-circuits true when sowResearchPrerequisites is null; do not
silently fix or reimplement native availability under this feature. Retain skill/info/roof/light
warnings and tutor semantics once per user action. Expand affected owned groups before menu
construction and revalidate their membership, control, current eligibility and choices at activation.

Only then commit native crop writes plus owned membership on the Unity thread. Snapshot the owned
transaction's prior state and guarantee no partial owned commit on failure. Admit setters only where
the effective path provides the required successful crop assignment and a safe failure boundary;
do not promise to roll back arbitrary foreign side effects. An incompatible setter/provider disables
that integration before any group command. Reentrant callbacks and combined gizmos must not multiply
the edit. Never broadcast an unchecked incoming argument after a foreign prefix skipped/changed it.

Native `Building_PlantGrower.SetPlantDefToGrow` is a simple assignment and native GetPlantDefToGrow
returns that field. These facts identify a candidate seam, not proof about patched execution.
Do not poll every building to repair unobserved external field writes. Audit actual consumers; add
narrow admitted mutation paths only for demonstrated contracts. Load-time disagreement marks a
group dormant and requires explicit relinking instead of arbitrarily choosing one member's crop.

### Preserve jobs and recover absent content without null UI failures

Native WorkGiver_GrowerSow first consults CanAcceptSowNow and refuses a null wanted crop; native basin
power still controls sowing and damages existing plants when unpowered. Harvesting has its own
plant eligibility and zone-specific allowCut checks. No new scheduler, job cancellation policy,
power override or harvest switch is selected. Existing job validation governs already assigned work.

Retain stable missing-crop identity independently of native Def resolution. Present a null-safe
crop recovery command and inspect state before native command construction and the native/provider
GetInspectString-to-GrowthSeasonNow path. The VFE override needs its own package/shape-admitted
inspect integration; a hook only on the native base method cannot cover that override. Do not select
a default merely to avoid either dereference. Ensure no new sowing at missing-choice members
and preserve normal harvesting. Determine the narrowest proven native seam during LH-Q2; an ordinary
new building with a valid native default must not be mistaken for missing saved content. Independent
unresolved members keep the same recovery facility until a valid crop is explicitly selected.

When integration is inactive, persist dormant owned records without broadcasting; valid native crop
choices continue independently. Re-admission must compare saved intent with current native choices
and request relinking on disagreement. Full uninstall does not promise preservation of owned group
metadata or recovery UI; no safe-removal or automatic migration claim is made.

### Optional growers and authority

Core and content-only additions using compatible native growing-building behavior are admitted by
capability and effective crop rules, not a fixed name whitelist. Provider-specific executable adapters
first check canonical package IDs and then every consumed shape. VFE Farming's five inspected
growers use native Building_PlantGrower or an inspect-text-only subclass. VEF extraSowTags can admit
additional plants; preserve its effective predicate rather than replace it with normal tags.
VFEFarming and its Framework remain optional to megamod and require separate exact-provider evidence.

If `Yakobo.LinkableHydroponics` is active, disable owned building linking before installing its hooks
or loading groups as active. Leave foreign comps, XML and patches alone. S006 command composition
must not turn a shared crop menu into a backdoor group broadcast for source-owned buildings: isolate
the owned zone transaction and leave that source's building interaction to its provider, or withhold
the incompatible mixed command with an explanation. No foreign unpatching or false coexistence claim.

No text consumer was found beyond the source; this is not a compiled/reflection audit. No facade,
package spoof, source assembly shipping or automatic saved group converter is justified.

### Target specifications and admission before code

Create actual stable-ID target documents before implementing each chosen patch. Candidate areas:

| Candidate integration | Minimal evidence required; no patch is yet approved |
| --- | --- |
| Grower gizmos/inspection and member visualization | Consumed callable/enumeration/render shape; preserve unrelated output; null-safe recovery before unsafe constructor and native/provider inspect GrowthSeasonNow call |
| Combined crop command/menu/action | Effective affected-member collection, eligibility, successful assignment and one-action warning/tutor behavior; original-body existence alone is insufficient |
| Crop mutation and failure boundary | Actual effective setter use, skip-original/argument-changing prefixes and atomic owned rollback; do not probe by executing foreign transpilers |
| Lifecycle invalidation | Successful despawn/destroy/transfer/load distinction and reference ownership; mere method existence suffices only for genuinely passive notification |
| Missing-crop sowing restriction if needed | Exact current job-selection/use path and nullable choice semantics; preserve harvest and ordinary power rules |

Admit complete operational groups. False/unknown predicates, validator/install failure or supported
later repatch invalidation deactivate affected behavior, roll back only newly owned hooks and emit
one bounded startup warning per skipped enabled patch with package/patch/spec/target/reason/fallback.
Every actual transpiler checks its incoming stream on every run, returns it unchanged on failure and
tolerates irrelevant differences. Accept compatible foreign hooks; hashes/whole-body identity/absence
of other owners are not predicates. Keep technical log text invariant; localize player explanations.
If lifecycle/transaction safety cannot be proven, leave native independent behavior usable.

### UI and shared data

Contextual Link, Unlink selected, Disband group, Select linked, group name/count/crop and member
highlights; no bottom tab, debug control, global setting or inheritance exception. Group choices
are entity state. Use existing suitable native visual resources with readable non-color identity;
new raster art would require its separate asset gate. Seven-language UI from first implementation.
Draw only relevant selected groups, never rebuild membership from all map Things every repaint.
Use shared native/content information for crop candidates; lifecycle invalidation must not create
another feature-specific inventory cache. Measure joint S006/S076 update, draw, save/load costs.

## Risks / Trade-offs

- **Source and proposed gestures differ** → document immediate crop adoption and selected-only moves;
  show source/scope before activation and verify whole-group disband independently.
- **Provider callback has irreversible effects** → narrow admission before mutation; unsupported
  paths remain independent, not an attempted transaction with fictional rollback.
- **Missing content crashes native UI** → retain choice identity and route null-safe recovery before
  constructor and native/provider inspect execution; verify actual select/inspect/open/choose/sow
  and unlink/reload workflows.
- **Mixed menus reach source-owned links** → resolve provider authority before composing actions.
- **Group fallback after removal is uncertain** → no importer or safe-removal claim; retain explicit
  implementation questions instead of accepting source save behavior from static code.

## Migration Plan

New and existing independent buildings remain independent until explicitly linked. Preserve owned
groups across supported loads; refuse stale/cross-map references and conflicting choices. Do not
convert source `groupID`/`savedGroups`/`nextGroupID` or impersonate its saved types. A future importer
needs a separate backup, atomic conversion, failure restoration and verified-postcondition contract.

## Open Questions

- **LH-Q1 (proposed scope/UX):** initial sync, selected-only moves, group naming and crop-only building
  scope are explicit proposals; mixed zone/building membership and new basin work flags remain outside
  this draft. S006 is unchanged; inspect exact multi-group/native selection behavior before coding.
- **LH-Q2 (effective integration/lifecycle):** prove command transaction, setter/veto composition,
  missing-crop UI/work recovery, despawn/reinstall/faction changes and inactive/reactivated records on
  exact beta. Choose actual target specs and failure boundaries before implementation.
- **LH-Q3 (providers/contracts):** exact VFE/other grower eligibility and current original-provider
  mixed-menu authority, compiled/reflected consumers and source save types; no blanket compatibility.
- **LH-Q4 (implementation/acceptance):** honest focused RED/GREEN, seven languages, combined costs,
  independent review and reviewed-build native link/crop/work/unlink/load/provider evidence remain.
