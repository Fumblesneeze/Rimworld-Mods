# Quick Stockpile Creation replacement coverage

Owner **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
Origin replacement **S107/P2-37**. Source **slofa**, `longwater.smartstockpilecreation`,
[Workshop1742151109](https://steamcommunity.com/sharedfiles/filedetails/?id=1742151109).
Selected1.6-folder DLL1.0.0.0 inspected; About declares1.7. **Nothing implemented or verified in game.**

[Research/provenance](../refinement/2026-11-05-quick-stockpile-creation-research.md),
[proposal](../../../../openspec/changes/specify-megamod-quick-storage-actions/proposal.md),
[design](../../../../openspec/changes/specify-megamod-quick-storage-actions/design.md),
[creation spec](../../../../openspec/changes/specify-megamod-quick-storage-actions/specs/megamod-quick-storage-creation/spec.md),
[exclusion/undo spec](../../../../openspec/changes/specify-megamod-quick-storage-actions/specs/megamod-storage-exclusion-undo/spec.md),
[unchecked tasks](../../../../openspec/changes/specify-megamod-quick-storage-actions/tasks.md).

## Behavior inventory

| ID | Input/action/result | Selected boundaries and deviations | Evidence/status |
| --- | --- | --- | --- |
| QSC01 | Select items → fresh filtered stockpile tool | Distinct types, packed inner type/outer location, safe mixed selection; left-click direct, right-click options | Full selected binary/native zone paths; specified |
| QSC02 | Choose common category or matching state → derived filter | All category paths; usable descendants; admitted complement semantics; no guessed sibling exclusion | Source helpers/filter/native special Defs; specified |
| QSC03 | Source storage → initial priority | Preferred default; promotion on; consistent maximum per-item result/cap for all routes | Source defaults plus first-item/menu correction; specified |
| QSC04 | Paint free cells → new named zones | Native eligible cells, fresh connected regions, names only for new zones; no accidental old-zone extension/rename | Source/native zone logic; specified |
| QSC05 | Choose shelf/bookcase → filtered ordinary build | Four templates, intrinsic/research/material rules, normal costs/work/capacity; real blueprint/frame/building persistence | Source + native/Def trace; specified |
| QSC06 | Preview/rotate/drag → same valid placement plan | No fake-grid objects/global Def changes; one-cell and immediate-mode coverage; exact receipt cleanup | Source bulk builder defects/native placement; specified |
| QSC07 | Exclude selected types/category → effective policy delta | Supported shelves/linked scopes as well as zones; preserve unrelated fields and physical items; atomic batch | Source exclusion/native settings + owned composition; specified |
| QSC08 | Undo recent exclusion → restore actual prior allowances | Three session commands; exact delta, existing exclusions and unrelated edits preserved, stale/ABA rejection | Source history limitations + selected correction; specified |
| QSC09 | Settings, lifecycle and failure | Four common global/save rows; per-group executable admission, original suppression and bounded diagnostics | Foundation contract; no runtime implementation |

## Interactions and composed result

Use S005 effective storage/group policy; preserve member-specific restrictions. S049 refill choices
and stack controls survive all edits. S050's single Presets selector retains its own intentional
replacement semantics; quick exclusion/undo does not add a second preset system. Native manual
edits/copy-paste/link/unlink and construction-time edits remain authoritative. S021 hauling and S033
search use real native/shared item facts and invalidation, never a fake preview Thing or per-feature
cache. Filtering does not move/delete stock or force jobs; linked bill destinations keep their
existing invalid-scope rules. Direct owned calls replace an internal stack of patches.

## UI audit and relocation

| Source entry point | Purpose | Owned destination / change |
| --- | --- | --- |
| Create stockpile plus up to four building gizmos | Item-derived storage construction | One compact contextual Create storage command; fast exact-type zone path and explicit content/building choices |
| Item category/special menus | Select initial allowed contents | Contextual menu with meaningful categories/states and unavailable reasons; no English-only label surgery |
| Material window | Current construction material | Contextual legal-material control with current availability; no cross-game static material |
| Disallow/Reallow gizmos | Edit current storage | Identified effective storage/group exclusion; precise Undo exclusions in existing storage configuration, usable after sample moves |
| Global priority radios and three checkboxes | Configuration | Storage / Quick creation common global/save/Use global rows, Preferred/true/true/true defaults and no-save write guards |
| Bottom tab or debug menu | None in source | None introduced; native god mode remains an existing explicit debug choice |

Selected items, policy, active placement/material and transient history are contextual state, not
per-item mod settings. Seven languages required. Source two custom icons and native zone/building
icons are only research references; product visual selection/creation requires the asset workflow.

## Contracts consumed by other mods or saves

| Binding / evidence | Preserve / unsupported |
| --- | --- |
| Source public Settings/Get, commands/designator/AllowedThingsDefs, material field and helper classes | No real exact-name API consumer found in bounded installed scan; no invented facade/assembly or package spoof |
| YART four sample mod lists reference source package | Example input, not runtime dependency/integration; no product alias needed |
| Four source global settings keys | No implicit import or mutation; future import requires backed-up atomic migration with verified postconditions |
| Native zone/blueprint/frame/building/filter persistence | Preserve actual stored policies through native lifecycle; transient history/plan is not saved |
| Optional special workers/storage setters and linked policy | Narrow supported semantics and rollback/history observation required; unsupported choices remain unavailable with a reason |

Consumer search limits include reflection/UTF16/archives/remote callers. Public source/license not
found in inspected material; independently implement and credit slofa, copy no code/text/art/binary.

## Implementation and coverage

**All features remain unimplemented.** Retain item-derived creation, four native templates, priority
defaults, intelligent naming, exclusion and bounded recovery. Correct first-item priority/category
bugs, partial category ancestry, cumulative rename, nonpaired-state guesses, broad category reallow,
global preview/Def/callback mutation and missing immediate-build configuration. Extend contextual
filter operations to admitted shelves/linked policies and materials to legal future-supply planning.
Do not claim generic container creation, workbench-ingredient planning or permanent/saved undo.

Core/Harmony required at implementation; no XML patch/new XML Extensions dependency here. Original
provider owns overlap when active; skip owned groups with per-patch diagnostics, never uninstall or
unpatch another owner. Provider/failure groups preserve ordinary native controls. Product packages
exclude source/Gateway assemblies and all local research evidence. QSC-Q1–5 retain open work.

## Verification and acceptance

Source/binary/native checks and static ground/shelf/bookcase cost/capacity comparisons are evidence
for the draft only. Two independent reviews found no issues; strict validation passes129 items,
zero failed. The ledger retains the scope and remaining limitations. Future
acceptance needs focused honest-tier TDD, reviewed build/package and native item selection → zone
painting or construction → correct policy/physical result, then exclusion → ordinary hauling →
precise undo. Include packed/mixed selections, category states, changed cells/menus/maps, zero/one/many
placements, rotation/materials, construction-time edits/reload, linked/group ABA/rollback/history,
original and changed providers, settings scopes and seven languages. Personally inspect causal
screenshots and keep exact identities/config hashes/cleanup. Static passing checks accept no gameplay.
