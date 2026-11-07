## Context

Owner **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
S107/P2-37 replaces `longwater.smartstockpilecreation`, slofa, Workshop1742151109. About declares
1.1–1.7 but latest folder is1.6; native fallback selects1.6 and root. Selected DLL1.0.0.0,
MVIDa17b9727-952f-4658-a24f-f05ee5e3668a; informational suffix87d40dee4e2a5e8d392e5cf4750bf5ec6cfec9d9
is provenance, not a verified public revision. All28 runtime decompiled files were inspected.
[Research](../../../mods/megamod/docs/refinement/2026-11-05-quick-stockpile-creation-research.md)
separates docs, source facts, native contracts and proposed corrections. No code is implemented.

## Goals / Non-Goals

**Goals:** Item-derived zones and four native storage-building templates; supported categories/state
variants; coherent preview/placement/filter inheritance; predictable priorities; contextual exclusions
and precise bounded undo; direct composition with S005/S049/S050 and shared data.

**Non-Goals:** Generic blueprint cloning, auto-building from workbench ingredients, automatic hauling,
new storage capacity, arbitrary container discovery, world-grid spoofing, a private Thing cache,
source binary identity, permanent exclusion rules, unlimited/saved undo, a new bottom tab or source
settings import. Native cheat modes remain native explicit choices, never enabled by this feature.

## Decisions

### Freeze bounded item-derived intent and keep authority explicit

Source `Thing.GetGizmos` postfix appends zone/exclusion/building gizmos per haulable Thing. Helpers
cast every selected object to Thing and unwrap MinifiedThing; category traversal takes only the first
category, removes root unsafely on an empty list and has no cycle bound. Owned code safely extracts
current visible map-owned item selection, deduplicates inner storable Defs while retaining outer
physical Thing references for location/current storage, and traverses all category parents with a
visited set. Reject invalid/cyclic wrapper identity; do not mutate or unpack the item.

One contextual Create storage command retains fast left-click exact-type zones; right-click provides
category/state and supported building choices. Do not preserve the source's simultaneous popup and
designator selection on left-click, or create one global bottom tab. Preview text identifies content,
storage kind, effective priority and meaningful restrictions. Material, active plan, current storage
and undo are contextual state, not common setting rows.

An open menu captures selection/map/scope generation and rechecks before accepting a choice. Once a
creation plan is deliberately selected, copy immutable Def/state/priority intent into the tool;
moving/destroying the sample stack no longer rewrites it. Revalidate current definitions, target map,
research, material and placement at use. Cancel/map switch/game teardown invalidate the pending tool
and material selection. Never follow Find.CurrentMap blindly from a stale callback.

### Build explicit new-filter policies without changing the definition catalog

New zones/buildings replace their initial type allow-list with the selected distinct types or usable
category descendants. Source `SetDisallowAll` clears configurable special disallows; retain that
new-filter baseline intentionally: configurable states allowed, with only named selected state
changes, while intrinsic/nonconfigurable rules and initial ranges survive. This is initialization
of a newly created policy, not destructive replacement of existing categories/filters or XML lists.
No item-state inference from a particular quality/Stuff/condition beyond an explicitly selected
special variant. Native `ThingFilter.SetAllow` can expand virtualDefs; preserve its semantics.

Source's zone special choices inspect configurable workers matching every sample. Its eight known
complement pairs are smeltable/non-smeltable weapons, burnable/non-burnable weapons, biocoded/non-
biocoded weapons, the same three apparel pairs, clean/tainted apparel and fresh/rotten. Fresh excludes
samples lacking CompRottable or using rotDestroys. Retain those meaningful choices after actual
worker/Def admission. Do not reproduce the fallback which disables every sibling special filter:
sibling membership is not proof of mutual exclusion (book skills and other providers can overlap).

Represent an admitted special variant as an explicit filter-edit recipe: allowed/forbidden states,
relevant type domain, exact worker predicates and expected sample acceptance. Known complementary
pairs can be owned descriptors; other groups require proven complete semantics or a narrow provider
mapping. Unknown variants are unavailable with a reason while unqualified choices remain usable.
Never derive semantic opposition by English label trimming. Source gives special variants to zones,
not building menus; building creation retains exact types/categories rather than claiming universal
special-filter menus. Existing S050 presets offer their own named policies afterward.

The building catalog is ShelfSmall/Shelf/BookcaseSmall/Bookcase, not every storage Def. Canonical
Core identity, actual finalized fixed filters, class/blueprint/settings shape and research are checked.
For exact types require acceptance of every sample; category recipes are intersected with intrinsic
accepted content and explain narrowing. Current item occupancy and current editable filter are not
intrinsic limits. For existing linked storage use S005's any-member usable policy plus per-member
physical restriction model, not native StorageGroup.GetParentStoreSettings's first-member shortcut.

### One priority policy for every creation route

Four common settings retain source defaults: Preferred priority, higher-than-source true, shelves
true, bookcases true. For each outer physical sample resolve its real current haul destination and
effective group priority. Promote one enumerated legal level capped at Critical if enabled and
stored; otherwise use configured default. Take maximum over all samples, consistently for direct
and menu routes. Source direct path uses the first unwrapped sample while menus take a maximum;
correct both order dependence and loss of minified outer storage. Respect its non-minimum behavior:
all samples in Low promote to Normal even if the fallback default is Preferred.

Prepare a cap explanation once; menu construction must not emit repeated messages. No global
priority mutation or source-storage edit occurs. Use common global/save loading, validation and
row rendering; dormant overrides/main-menu callbacks follow foundation rules. Invalid persisted
priority uses Preferred, invalid booleans their true defaults; missing overrides inherit. Settings
changes affect the next prepared command, not an already frozen plan or existing storage. No custom
reset UI is required here; any shared reset retains its established scope and disabled-menu behavior.

### Create fresh zones and name each exactly once

Native Designator_ZoneAdd can select a touched existing zone and extend it; source only supplies
new-filter values in MakeNewZone. The quick command promises new storage, so use an owned constrained
zone plan that excludes existing zone cells and partitions accepted cells into connected components,
then invokes ordinary zone registration/AddCell/finalization safely. A previously selected zone is
not an implicit target. Preserve native fog/edge/terrain/CanOverlapZones/tutorial rules and notification
effects. Preview includes only eligible free cells; cancellation/no valid cells has no mutation.

Keep current-gesture created-zone receipts, not source's cumulative `_createdZones` list. Name each
new zone once from localized item/category/state description and a meaningful room role only if its
cells share that room. Use native cell→room lookups, not a scan of every room's cell enumerable.
Unique positive suffix starts1, avoids occupied labels and numeric overflow. Later gestures never
rename previous receipts or overwrite manual names. Ordinary rename/link/save lifecycle owns names.

### Plan buildings locally; configure the actual creation receipt

Source bulk preview puts newly made fake blueprints in a static list and patches every
ThingGrid.ThingsListAt call while rendering. It also temporarily clears shared blueprint replaceTags,
uses static material across all maps/types, and installs a global PostPostMakeHandler for any
Blueprint_Storage created during a drag. None is a valid owned architecture.

Use one tool-local plan shared by preview and commit, deterministic from drag anchor/direction,
rotation, legal material and candidate cells. Track planned footprints/interaction cells in a small
local occupancy set; query the real map without mutating it. Preview starts with the first candidate,
including one-cell gestures. Block existing structures/frames/blueprints rather than invoking native
replacement cancellation. Recheck on commit and skip changed invalid footprints with feedback.
Do not promise the mathematically densest packing; deterministic valid native placement is the goal.

Use the native construction path and exact owned creation receipt. Preserve tutorial, style, material,
place-worker and native finalize notifications. Native Designator_Build invokes WipeExistingThings
and cancels matching frames before PlaceBlueprintForBuild, so admission must establish that this
quick path cannot destroy or replace existing objects. If normal placement would require such
replacement, reject that footprint. Loose-item hauling/construction clearing remains native, never
implemented as deletion to make room. No broad private-field reflection for `stuffDef` is assumed.

Configure only the exact freshly created Blueprint_Storage (including supported bookcase subclass),
with independent policy ownership before publishing success; do not overwrite an existing group
through automatic provider membership. An admitted result-capture callback may observe the actual native return
within an owned synchronous scope; it must prove map, action, building, material and receipt identity,
exclude nested unrelated creations, and clear in finally. Prefer direct returned native object where
faithful. Never identify the result as merely the newest blueprint at a cell or intercept all
PostMake calls globally. Existing god-mode immediate building creation needs its own admitted receipt
branch with the same policy, not an unfiltered shortcut; the feature never enables god mode.

For one failed receipt, cancel only that command's unconsumed newly created object through native
cleanup after confirming identity; never delete pre-existing or already externally claimed objects.
Prepare/admit before mutation and treat inability to prove safe cleanup as an unavailable integration.
Each footprint is its own creation unit, not a rollback of earlier successful placements. No stale
session delegate survives cancellation, exceptions, map/game switches or nested calls.

Native Blueprint_Storage saves settings, copies effective settings/group into Frame, and Frame
copies into the completed IStorageGroupMember and failure-recreated blueprint. Use this lifecycle;
do not reapply original template on completion and erase edits made during construction. Native
Bookcase interfaces and Blueprint_StorageWithRoomHighlight inheritance are inspected; exact effective paths still
need executable validation/live proof. Source's stockpile naming is not copied onto buildings.

### Exclude through the shared effective-policy edit path

Source supports only Zone_Stockpile: direct action walks selected items, category action edits the
first item only; it changes `zone.settings.filter` directly. Owned zone/shelf/supported-container
contexts resolve the actual editable S005 effective policy and physical membership before acting.
Use the shared storage editor/transaction seam from S005/S050, not a second filter model or patches
against those owned features. Dedupe by effective policy identity plus requested Defs; a category
choice is valid only for the intended shared category and explicit captured target set.

Exclude only item allowance bits, expanding actual finalized descendants/virtualDefs as necessary.
Do not invoke category SetAllow(false) without accounting for its native side effect of disabling
descendant special filters. Preserve special bits, quality/condition/risk, priority, group/name and
S049 thresholds/stack settings. Revalidate source storage and group membership; stale menus refuse
rather than follow a moved item. Stage all affected policy deltas, record current complete rollback
snapshots, commit in one bounded Unity-thread action, notify native/shared consumers and verify all
postconditions. Any failure restores all batch policy state; no physical-item mutation is involved.
Expose only adapters whose rollback/callback contract is safe; unknown foreign setters remain absent.

### Undo precisely with three bounded session records

Source `RecentDisallows` is a process-static three-entry array of Def/category+Zone references;
every selected stack can consume a slot. Reallow simply SetAllow(true) on a type or entire category,
does not restore prior exclusions, is not consumed, and UI can disappear when the sample is hauled.

Owned history stores the newest three successful exclusion **commands**, one batch per record:
game/map/policy/group membership identity, exact changed allowance deltas, pre/post values and the
relevant mutation generations. Empty/no-op/failed commands do not enter history. No live references
survive game teardown; no history serialization/import. Native filters persist independently.
Expose Undo exclusions in existing affected-storage configuration; list only relevant records but
identify every scope of a multi-policy batch before undo. Original item existence is not needed.

Use per-touched-allowance edit observation to reject intervening intent, including ABA changes. A
current value equal to recorded post-state alone cannot prove eligibility. Existing common storage
mutation tracking should be extended only for this concrete consumer, with minimal native/provider
observer target specs; unknown observation means undo is unavailable, not a guessed safe restore.
Unrelated current policy edits survive an inverse-delta transaction. Group membership changes,
deleted/recreated policies, changed definitions or an edited touched field invalidate the entire
record. Successful undo consumes it. Failed undo leaves every policy unchanged. Owned cancellation
of the feature clears history without silently reversing committed edits.

### Runtime target groups and compatibility

Create/index target-spec documents and executable validators before patches. Planned IDs/roles:

| Group / planned IDs | Minimal consumed semantics and failure behavior |
| --- | --- |
| Creation UI / `megamod.quick-storage.commands` | Thing/current selection and gizmo enumeration reach real eligible item context; preserve existing commands; no throwing casts; missing shape disables its owned command contribution |
| Zone creation / `megamod.quick-storage.zones` and any necessary native observers | Authoritative eligible cells, fresh registration receipts and notifications; no selection-driven extension; fail only zone path |
| Building creation / `megamod.quick-storage.blueprints` and immediate-mode receipt hook | Real native creation/setting/cancellation lifecycle and exact owned receipt, material/style/placement side effects, frame/completion; group inactive without safe configuration/cleanup |
| Exclusion/undo / `megamod.storage-exclusion.commands`, `.edits`, `.history` | Current physical→effective policy identity, actual edit semantics/callbacks, observed touched-field generations and atomic multi-policy rollback; coherent failure unit for edit/undo |

These are planned identities, not implemented documents or admission claims. If a native method is
only observed, validate required callback shape/identity; if arguments/results affect placement or
settings, validate their minimal semantic data flow in original and effective code. Consider relevant
skip-original/foreign-prefix/transpiler behavior and later supported repatching. Never gate on exact
hash/version/whole method or no other owners. Never speculatively execute foreign patches. Every
owned transpiler validates each actual incoming stream and returns it unchanged on failure.

False/unknown predicates, validator/install failures or partial group activation roll back only owned
patches in that group, disable its commands/pending intent and warn once per skipped enabled patch
with package/patch/spec/target/predicate/unavailable behavior/fallback. Explicitly disabled template
families or absent unrequested adapters are not failures. Original canonical package owns overlap
when active; suppress all owned overlapping actions, no uninstall/foreign unpatch/package spoof.
Supported later invalidation clears affected pending tools/history but leaves committed native state.

No XML edits are planned. Any future provider XML requires XML Extensions package/content/node guards
and its hard dependency at first use. No source assemblies or Gateway dependency may ship.

## Risks / Trade-offs

- **Source API exposure without discovered consumers:** selected scan finds only source versions
  and YART sample mod lists; no facade or package alias. Reflection/archives/remote consumers remain
  a bounded research gap, not an absence proof.
- **Public claim versus observed code:** original removal safety is plausible from native policies
  but not live-verified; creation bugs and special-filter approximation are corrected explicitly.
- **Undo concurrency/provider callbacks:** touched-field history/atomic rollback must be proven;
  keep unsupported contexts unavailable rather than overwrite newer edits or corrupt linked policy.
- **Preview side effects:** no ThingMaker/ThingGrid spoofing, no global Def mutation or real-map
  temporary occupancy. Measure large drags only after truthful preview/action parity exists.
- **Balance:** creation shortcuts preserve costs/research/capacity and hauling; initial priority is
  a convenience, not guaranteed relocation. Native/material/provider finalized comparisons remain
  future evidence; no optimization or compatibility claim from static code.

## Migration Plan

No runtime edits now. Implement one reviewed vertical slice at a time. Use native zone/building/filter
persistence; transient tools/material/history clear on unload/load. Feature removal leaves actual
storage policies untouched and ordinary editing usable. No source settings/history import. Any future
import needs its own backup, atomicity, failure restoration and verified postconditions. Localization
is English/German/Spanish/French/ChineseSimplified/Russian/Japanese. Source icon names are research
only; selecting/authoring product visuals must follow the asset skill and native visual gate.

## Open Questions

- **QSC-Q1:** Exact effective beta selection/creation/preview/receipt/cleanup/god-mode/lifecycle targets.
- **QSC-Q2:** Additional meaningful special-filter recipes, optional storage/content contracts, real consumers and license/source discovery.
- **QSC-Q3:** Common atomic storage edit and touched-field observation, rollback, ABA detection and undo identity lifecycle.
- **QSC-Q4:** Finalized construction/storage comparators, seven-language/native-art UI and bounded placement cost.
- **QSC-Q5:** Focused TDD, reviewed implementation/package and causal native placement/construction/filter/haul/undo/save/provider acceptance.
