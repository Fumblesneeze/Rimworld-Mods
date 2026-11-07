## Context

Owner **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`, target **1.7 beta**.
S132 is documentation only. [Research](../../../mods/megamod/docs/refinement/2026-11-07-rpg-style-inventory-research.md)
pins complete installed1.6 source assemblies/XML, beta Gear semantics and actual consumer paths.
Public root/README available; public source commit/license and current CE/RWoM providers unavailable.

## Goals / Non-Goals

**Goals:** complete interchangeable equipment views, faithful current native/provider actions,
truthful stats/statuses, direct owned composition, shared settings/palette and bounded guarded fallback.

**Non-goals:** inventory/equipment simulation, new carrying capacity, new apparel layers, changed
combat math, a second hauling/supply cache, generic plugin UI framework, legacy package spoofing,
automatic foreign settings import or copying upstream art/code/assemblies.

## Decisions

### Read the actual subject and compose existing owners

Use current native gear/inventory containers and supported corpse-inner-pawn selection. Preserve
native/provider visibility and control predicates rather than cloning source-era checks. Source
misses current CarriedBy, destroyGearOnDrop, bandwidth confirmation and updated ingest eligibility.
Build a small per-draw presentation snapshot from current subject/containers; action callbacks retain
identity then revalidate current holder, control and eligibility through the native/provider path.
Do not invoke obsolete private helpers just because the name still exists. Show disabled reasons
and call native confirmations/jobs, not direct state mutation to mimic their final result.

S021/S126 provide physical cargo identity/quantity and real capacity. S062/S123/S124 provide effective
personal minimums, contributors and contextual controls; S127 provides shared selected subject,
S130 remains ordinary policy data. S131 owns quality palette/editor. Consume direct functions/data,
not Harmony patches between owned modules. Preserve existing personal-supply target-adjustment
semantics on real drops. One physical item can have several meaningful classifications, but its mass
and quantity occur once. No new map scan/index: use native containers and the already owned shared
index only for existing supply catalogue/search consumers. No speculative cache abstraction.

Use current mass/capacity, temperature and conditional vacuum stat with actual applicability.
Preserve S075's native capped overall armor metric; individual values can exceed200%. Explain
individual layers/coverage without labeling arithmetic sums as protection probabilities. CE and
RWoM summaries require their own current-provider semantics. No native formula forced onto them.

### Separate layout from physical inventory

Own declarative presentation layout records with region/layer match lists, deterministic priority,
anchor and default/placeholder intent. Preserve the source's useful any-within-list, both-lists
matching semantics, but resolve actual selected body coverage rather than BodyDefOf.Human for all.
Keep source catalogue correspondence in research for ports; do not publish Sandy's fully qualified
ItemSlotDef or fake source package/assembly discovery. No actual external XML consumer was found in
the bounded scan, so a broad compatibility type hierarchy is not justified. A future proven consumer
gets an explicit port or narrow adapter and updated coverage before a drop-in claim.

Work from immutable loaded definitions into owned layout state. Never mutate foreign Def hidden,
listid or coordinates while compacting. Validate finite/bounded integer coordinates, unique IDs,
anchor dependencies and required layers/groups; use sparse positions, not arrays sized by unchecked
coordinates. Reject invalid entries with bounded diagnostics; cycles/duplicates cannot make loops
unbounded. At most one actual tile per item; ghosts hold coverage explanation only. Collisions and
unmatched items go to one deduplicated fallback list. Missing anatomy/groups must not throw.

Compute scroll extent for every subject/layout and reflow when viewport, UI scale, language, subject,
contents or effective preferences change. Bounds clamp rendering, not persisted preferences. Tight/
Wide fit explicitly commits the chosen scoped dimensions. Simple and list paths share actual item
actions and stat sections; avoid source's early nonhuman scroll return and short-grid overlap.
Source incidental64px icon/74px stride/128px panels are reference measurements, not acceptance pixel
contracts. Any new/reused raster assets require the asset skill and live independent visual gate;
no provider textures have been selected or copied during research.

### Shared quality and separate condition/status rendering

Use S131's current effective palette, including its Default initial preset; original RPG palette
remains an available shared preset. No additional per-quality settings/picker. Reuse S131's scoped
transaction/cancel validation rather than source Close/PostClose commit and texture allocation.
Quality frame enable/thickness are local presentation settings; condition/status/cargo are separate
layers. Restore GUI/text state in guaranteed cleanup and retain neutral descriptions for low contrast.
Condition source thresholds are specified in GP02; they are not apparel-thought policy. Keep HP bars
on hover, handle non-HP/zero-max cases, and show inspection statuses independently of command rights.

### Settings versus operational view state

Common setting definitions own dimensions, default layout, one per-race layout-default collection,
empty-slot/background choices, temperature line, condition mode and frame choices. Missing race
entry uses the common effective layout default. Whole-collection inheritance avoids a partially
global collection whose scope is unclear. Source's initializer versus Scribe discrepancies for
temperature/HP are resolved explicitly to true in GP06. Prefer one common settings renderer/storage
path; no global static provider settings or implicit mod-settings reload from drawing.

Use global stays enabled in a loaded save, including while inheriting; only the save value editor
is disabled then. No save editing/callback/reset at main menu. Common atomic persistence/backup,
captured save identity and value validation preserve the last complete setting on failure. Do not
write foreign keys/files. Seven-language labels, scope and failure explanations are required.

Current play layout overrides are a small per-game/per-race transient map; hats/rotation are view
state, reset on game change/reload. Bottom-right contextual Gear view menu exposes layout/hat
switches and return to configured default. Race default choices are in settings; no per-pawn
inheritance. Portrait rotation remains an ordinary inspection manipulation. Move native developer
equipment options to the top developer toolbar only. Keep contextual gameplay supply/loadout controls
in Gear; no new bottom main tab or unrelated settings shortcut duplicated in every row.

### Optional provider contracts and consumer limitations

| Boundary | Selected approach and limits |
| --- | --- |
| Deferred CE package ceteam.combatextended | Isolate types behind a lazy narrow adapter; package+actual type/member/Def semantic admission. Read provider CompInventory/weight/bulk/armor and use native switch/reload/held/loadout paths. Complete exact release inspection before implementation; reference DLLs shipped by unrelated mods are not provider evidence |
| Deferred CE item menu | Preserve actual action eligibility/confirmations. Selected weapon identity must survive put-away; do not copy source Primary bug or inventory=false context. Held-state changes accompany successful provider action only. Drop-and-haul label requires a genuine supported haul path |
| Deferred CE create loadout | Capture previous assignment and new record identity, create/assign through provider operations, verify postconditions, open native editor. If any step fails, restore assignment and remove only the newly created record; failure restoration also verified. No replacing existing records |
| RWoM torann.arimworldofmagic | Actual enchantment component/string and alignment stat; isolate optional types to avoid eager class loading. Missing stats/components omit only decoration. Source-only method references are research leads, not accepted current signatures |
| Fortified AOBA.Framework | Inspected native CanControlColonist postfix supplies same special subject permission as RPG-specific postfix. Reuse admitted effective native decision while preserving action-time rules; do not recreate species privilege or fake RPG getter |
| Smart Medicine Continued | External PatchRPG reflection is not promised to bind owned UI. Owned S062 controls compose directly in every layout; native external-provider access requires separate actual adapter proof and original-source overlap policy |
| Sidearms / multiplayer addon | Author claims only; no current compatibility contract or binary facade selected. RPG-Q3 retains evidence/acceptance work |

If essential provider controls cannot be preserved, retain/delegate to its original Gear tab for
that context; do not offer partial owned interception. Keep cosmetic adapter failures independent.
No generic provider registry before these concrete adapters need it. Original/Revamped RPG present
suppresses owned tab/layout overlap and leaves its source files/patches/settings intact.

### Declarative integration and runtime target admission

Use XML Extensions guards to substitute only the expressly selected native Gear inspectorTabs
entry, preserve sibling tabs and avoid duplicates. Optional CE entry substitution and layout
extensions additionally require canonical package via FindMod(packageId=true), actual node/Def
shape and provider admission. No entire inspectorTabs replacement. Do not add source helper apparel
layers/body groups just for UI; resolve existing content and omit absent slot definitions. Own
presentation defs are additive and cannot rewrite physical apparel/coverage.

XML loads before full C# semantic admission. Keep the original target type/context recorded and
provide an owned wrapper capable of delegating to that original implementation when admission
fails; no runtime mutation of finalized Def tab lists as a repair. For a target where safe
delegation cannot preserve all required behaviors, do not select its declarative replacement.
RPG-Q2 must prove this construction/visibility/lifecycle path before implementation acceptance.
Unknown custom inventory tabs are never substituted by name guessing.

Before each actual patch, create stable indexed target document, executable predicate and tests.
Candidate boundaries are design leads, not installed patches:

| Boundary | Minimum semantics / group |
| --- | --- |
| Native Gear tab selection/delegation | Correct selected pawn/corpse, visibility and live effective control/action paths. Essential layout/action group fails together and delegates to original workflow |
| Native action access if a hook is needed | Specific item/pawn operand, holder, eligibility, confirmation and native operation; original execution/skip-prefix semantics must match consumed contract |
| Third-party tab/provider callbacks | Exact active package and callable/content/data-flow semantics, subject/action identity and provider units; essential provider group or independent decoration group |
| Def/tab XML | Required type entry/content and optional canonical package, sibling preservation, no duplicate insertion, all dependent declarations admitted atomically |
| Invalidation/view lifecycle if hooked | Only required callback/subject/effect; no broad per-tick observer if direct owned reads suffice |

Allow benign edits/foreign owners. Effective code and actual incoming transpiler stream checks,
cardinality/data flow, skip-original/order and supported repatch invalidation matter; hashes and
exact IL text do not decide compatibility. Do not speculatively rerun foreign transpilers. False/
unknown/exception/install failure rolls back only newly installed owned group, preserves external
patches, and logs one bounded warning per skipped enabled patch with IDs/target/reason/behavior/
fallback. Optional absent decorations follow their declared inactive policy without failure spam.
Repeated startup cannot duplicate tabs/actions. Harmony and XML Extensions become hard dependencies
with this implementation, never bundled. Product never references/ships the Dev Gateway.

## Risks / Trade-offs

- Complete native/provider item workflows evolve → share current native authority, inspect actual
  releases and preserve provider Gear on incomplete admission; source menus are not a permission spec.
- Layout flexibility can hide equipment or consume excessive space → validate, bound, deduplicate,
  reflow and retain a complete list fallback, including nonstandard bodies and narrow screens.
- XML admission precedes runtime checks → prove original-tab delegation before selecting replacement;
  no partial UI that merely starts without exceptions.
- Shared colors/indicators can be ambiguous → distinct symbols and neutral text, native hover/action
  observation at multiple scales, no promise that color alone communicates every status.
- Provider legacy discovery cannot bind a new package → document omissions; no unsafe broad facade.

## Migration Plan

No item/save conversion. Common settings use new owned IDs and inherit defaults; no automatic source
settings import or legacy race-key interpretation. Gameplay orders remain with existing owners.
Disable/remove owned UI on a fresh session to restore underlying Gear without changing equipment.
Only future explicit migration may read source preferences, with its own backup/preview/atomicity
contract. No deployment or migration performed during this documentation stage.

## Open Questions

RPG-Q1 inventory resolved. RPG-Q2 exact beta/delegation/admission, RPG-Q3 external provider/consumer/
license proof, RPG-Q4 direct composition and RPG-Q5 visual/native/performance acceptance remain in
the refinement ledger. They are implementation evidence gates, not requests to explain source mods.


## Initial support scope — decision2

Multiplayer and Combat Extended adapters, synchronized sessions, CE-only controls/properties and
provider acceptance are deferred. Any source CE/MP implementation details above are retained research
for a future scoped change, not initial work. Implement and accept the single-player non-CE contracts.
Ordinary save/load of colonies started with megamod remains required.
