## Context

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
Source S050 is Simple Stockpile Presets (Continued), Workshop2189006791. The
[research](../../../mods/megamod/docs/refinement/2026-11-03-storage-presets-research.md) pins all six
source types, the installed1.7 DLL and matching repository binary, native filter/storage code and
consumer boundaries. This design is for later implementation; no executable patch is accepted.

The source already adds **one** zone gizmo opening15 FloatMenu choices. Its only Harmony patch is
Zone_Stockpile.GetGizmos; the constructor captures stockpile.settings.filter, so no shelf/group/comp
context is supported. Each action calls SetDisallowAll followed by category/Def/special-filter edits.
No options, custom-preset data, save types or placement patch exist. The user's reported placement
problem has not been reproduced, and a source-name bug diagnosis is not needed to specify correct
native placement and policy application.

Native1.7 ThingFilter.SetDisallowAll also removes configurable disallowed special filters; it does
not reset quality, condition or mental-break-risk ranges. SetAllow(category) changes descendant
ThingDefs **and configurable descendant special filters**, with several callbacks. CopyAllowancesFrom
copies ranges, special filters and other allowance metadata, then invokes its callback. ITab_Storage
resolves an IStoreSettingsParent from a Thing, comp, zone or consistent selected StorageGroup, and
performs bill-output validation around filter editing. These details constrain the proposed native
integration; copying the source loop would not preserve the intended unrelated policy.

## Goals / Non-Goals

**Goals:** all15 named shortcuts, explicit content/state meaning, useful options for actual storage,
atomic contextual application, one S005 effective filter, retained S049 policy and native workflow.

**Non-goals:** another bottom tab; per-storage global settings; a custom-preset editor/import/export
from the separate fork; stored preset links that reapply on load; source package spoofing; new storage
capacity; rewriting Def category lists; content unlocks; runtime implementation in this change.

## Decisions

### 1. Integrate with contextual storage configuration

Choose one Presets selector in the native storage configuration, rather than extending the source's
zone-only command or adding many gizmos. This is an explicit proposal resolving the user's two UI
sketches, not a claim that they selected an exact widget. Use the current effective IStoreSettingsParent
and S005 policy scope, including supported groups and component-backed storage. Unknown contexts are
unavailable. Do not assume every object with an inventory has an editable storage filter or ordinary
hauling semantics. Keep native filter/priority controls and labels; entity state does not use the
global/save settings renderer and this feature introduces no actual configuration rows.

Menu entries use fixed semantic IDs, localized names/explanations and the source order. They state
item-list replacement and preserved restrictions. Explain partial applicability and retained filters;
do not advertise the preset as complete unconstrained acceptance. Hide choices proven useless, show
an empty-state explanation when none apply, and distinguish unsupported meaning from an ordinary
no-match result. No source texture is needed for a text selector. If later art is desired, the asset
skill and its native visual gates apply before selecting or shipping it.

### 2. Express the catalog as resolved sets and named state changes

Resolve current registered categories, named items and state filters after Def initialization.
Enumerate current descendants for the positive type sets, then subtract explicit exclusions; do
not replay ordered source category toggles against the live filter. Negative selections win for
multiply categorized content, an intentional clarification of the preset names over incidental
source operation order. Do not infer inclusion from English labels, nutrition, deteriorates flags,
recipe uses or a ThingDef's unrelated tags. In particular outside materials remains the source's
stone-block category plus four metals, not a generic weatherproof classifier.

Exact source-to-owned catalog bindings, in menu order:

| Choice | Positive category/ThingDef bindings | Negative type bindings | Named SpecialThingFilterDef assignments |
| --- | --- | --- | --- |
| General freezer | Foods; Ambrosia, Beer, Wort, MedicineHerbal; PlantMatter, CorpsesHumanlike, CorpsesAnimal | EggsFertilized category | AllowFresh=true, AllowRotten=false |
| Freezer (no meals) | Same positives as General freezer | EggsFertilized, FoodMeals categories | AllowFresh=true, AllowRotten=false |
| Meals | FoodMeals category | None | AllowFresh=true, AllowRotten=false |
| General storage | Manufactured, ResourcesRaw, EggsFertilized, Items, Weapons, Apparel, Buildings, CorpsesMechanoid | Ambrosia, Beer, Wort; PlantMatter category | None |
| General medical | Medicine, Drugs, BodyParts; Neutroamine | Ambrosia, Beer | None |
| Drugs | Drugs; Neutroamine | Ambrosia, Beer | None |
| Materials (outside) | StoneBlocks; Plasteel, Silver, Steel, Uranium | None | None |
| Materials | ResourcesRaw | PlantMatter | None |
| Various others | Manufactured, Items, Weapons, Apparel, Buildings, CorpsesMechanoid | Medicine, Drugs, BodyParts; Neutroamine, Wort | AllowNonDeadmansApparel=true, AllowDeadmansApparel=false |
| Shells | MortarShells | None | None |
| Weapons | Weapons | None | None |
| Clean apparel | Apparel | None | AllowNonDeadmansApparel=true, AllowDeadmansApparel=false |
| Tainted apparel | Apparel | None | AllowDeadmansApparel=true, AllowNonDeadmansApparel=false |
| Chunks | Chunks | None | None |
| Rotting | Apparel, CorpsesHumanlike, CorpsesAnimal | None | AllowRotten=true, AllowFresh=false, AllowDeadmansApparel=true, AllowNonDeadmansApparel=false |

Explicit complementary true assignments replace the source's incidental reset-to-allowed side effect.
All other special filters remain unchanged, including configurable provider restrictions. Source
DefOf members AllowSmeltable, AllowNonSmeltableWeapons, AllowCorpsesColonist and AllowCorpsesStranger
have no preset action and do not justify new controls. Native fresh/rotten workers distinguish rot
stages and rottable definitions; fresh also matches non-drug ingestibles without CompRottable, and
rotten does not match rotDestroys items. Native apparel workers use WornByCorpse, not condition or
current wearer's health. Preserve those actual meanings rather than treating Clean as repaired.

Known absent optional positive/negative branches contribute nothing; missing descendants never
justify enabling all categories. Missing state filters needed to express named meaning disable
only those presets, not a broadened approximation. Unknown resolution/worker semantics fail closed.
Ordinary supported registered category additions are included; S038-removed content is not recreated.
This feature changes player filter data, not Defs, so no XML/category-patch fallback is appropriate.

### 3. Determine meaningful intersection without a map scan

Use intrinsic fixed/parent acceptance and supported provider semantics, independently of current
editable choices and current map inventory. For S005 mixed groups use the union of member potential
acceptance for UI options and retain member-specific rules at actual use. Do not inherit native
StorageGroup's first-member parent filter as a complete mixed-group restriction. A type-level match
is insufficient when a fixed special filter excludes every state allowed by the preset, such as
clean-only storage versus Tainted apparel. Require a sound possible-state check for supported
state/quality/provider constraints; don't call arbitrary workers with a fabricated spawned object.

Provider-opaque possibility is not false stock and is not safely true: mark that context/preset
unavailable. Current editable quality/special restrictions can remain visible as limiting the new
selection but do not hide otherwise useful shortcuts. A physically full or empty storage still
offers its applicable catalog. Preview the final types and retained restrictions from the same
resolved plan used at commit, avoiding separate menu and application classifiers.

Native Def/category enumeration suffices for this small menu. If a reusable resolved catalog is
needed, bind it to Def-generation and invalidate it correctly; no per-mod map Thing cache. Stock,
hauling and S049 refresh still use their native/shared S033 ownership. Do not scan colony inventory
to decide whether Meals should be offered. Measure catalog and context evaluation in representative
mod lists before introducing caching or claiming performance improvement.

### 4. Stage, validate, commit and verify one filter edit

Capture the intended stable policy scope and group membership when opening. Recheck scope identity,
membership and ownership at action time. A different selected member of the same unchanged group
still denotes the same scope; a different group or changed membership is stale. Reopen rather than
silently retarget. Re-read the latest current filter and applicable catalog so intervening non-preset
edits survive. Reject empty/unsupported final plans before any mutation.

Create an unattached candidate from the current filter, preserving all fields/ranges/provider state
that remain owned by native policy. Change only its allowed ThingDef set and explicit special-filter
assignments. Avoid SetDisallowAll/category mutation unless its collateral changes are explicitly
prevented: enumerate selected type differences or use a small owned filter-edit path. Candidate
callbacks must not notify a live storage while staging. Do not replace StorageSettings as a whole,
which would overwrite priority or S049 fields, or use a global temporary suppression flag.

Retain the prior complete filter for rollback, commit to the intended native/shared authority, then
notify normal hauling and bill validation once for the final policy. Verify final allowed types,
named states and preserved fields. No transient empty-list notifications. Exact API choice depends
on target admission and tests: native CopyAllowancesFrom on a staged clone may be suitable when
its effective callback is known. Arbitrary foreign callbacks with irreversible effects cannot be
assumed transactional; reject unsupported contexts rather than promise universal rollback. On
failure restore the prior filter and supported derived state, verify restoration and report the
failed action. Never roll back later unrelated edits or retry blindly against another selection.

### 5. Compose owned policy, persistence and downstream work directly

Use S005 effective shared filter once. Do not capture zone.settings.filter directly for a linked
zone, clone a policy per member or patch the owned linking implementation. These are filter-only
presets: S049 quantities, stack choices and cycles remain its responsibility. Changing the filter
can alter qualifying stock and consequently open/close demand under S049's existing rules; it is
not X/Y editing, source cycle copying or a reset to default. S021 hauling reads the final updated
policy and the same capacity/demand constraints.

Retain native priority and notifications; S005/S014 govern output-destination invalidation so linked
whole-list bills preserve an unresolved choice and block new starts, while ordinary bills follow
native handling. ITab_Storage's before/after BillUtility checks are a relevant candidate seam, not
permission to clear whole-list linked scope prematurely. Existing stock follows ordinary changed
filter work, with no teleportation/destruction from this feature. Retain native zone creation,
painting and resizing; the source provides no demonstrated defect in those paths to replicate.

Persist the resulting normal filter through native serialization. No preset selection is serialized
as a live rule, so manual refinements survive reload and changing catalogs never reapply a preset
automatically. Copy/paste and unlink inherit the applicable complete-policy contracts. There is no
source custom preset schema or settings migration; future custom authoring would be separate scope.

### 6. Target-spec admission and original providers

Document stable patch IDs for contextual selector insertion, effective selected storage resolution
and any required filter commit/notification adapters. Observation-only hooks can require callable
shape; any interception altering arguments/filter/state must prove the consumed values reach the
intended complete native edit with correct group ownership and downstream notifications. Verify
effective ordering, skip-original behavior and late repatches. Each transpiler validates its actual
incoming instructions and leaves them untouched on failure. Admit compatible foreign patches and
benign code changes; never require exact method bytes, assembly hash or zero other Harmony owners.

Treat selector/edit/ownership/rollback as a declared coherent context. Validate before applying;
rollback newly installed owned patches only on failure. Never leave a clickable selector with an
unknown write target. Emit one bounded startup warning per skipped enabled patch with package,
patch/spec ID, target, failed predicate, unavailable behavior and fallback. Catalog branches also
have explicit meaning/presence checks and bounded diagnostics, separate from patch admission.
Original Mlie.SimpleStockpilePresets suppresses the whole owned preset integration; don't unpatch
the source or promise to repair its captured-filter, callback or other shortcomings. Other forks
require their own package/consumer research before a co-installation claim.

Core and Harmony required when implemented. Optional storage/content adapters are package-resolved,
absent-safe, shape-guarded and narrow. No XML patch is proposed: selected native categories and
source assets remain unchanged. Future XML work must use XML Extensions guards and add its hard
dependency on first use; never overwrite woody/fuel categories to make a preset apply. The bounded
installed binary scan found no inbound consumer beyond the source's own versioned assemblies, so
no assembly/type facade or package spoofing is selected. Do not copy its old bundled1.0 Harmony.

## Risks / Trade-offs

- A preset name hides historical oddities → retain exact category/exclusion table and explanations;
  do not infer new weatherproof, drug-medical or item-quality rules.
- Native filter helpers reset extra fields → stage from current data, explicitly constrain writes,
  verify final/preserved fields and rollback in admitted contexts.
- Type compatibility differs from possible state → validate fixed state constraints and explain
  unknown contexts without scanning real inventory or exposing empty false results.
- Group UI, ordinary bills and linked bills differ → direct effective policy integration and exact
  native acceptance for each downstream path; no parallel group state.

## Migration Plan

No source mod-specific persisted data exists to import. Existing native storage filters remain
valid and unchanged until the player chooses a preset. Removal/deactivation leaves the last native
filter intact; there is no load-time reset or automatic reapplication. Original co-installation
keeps owned controls inactive without editing source configuration. A later custom-preset importer
would require separate proposal, preview/backup, atomic migration, restoration and postconditions.

## Open Questions

SP-Q1: exact beta contextual/effective policy/commit/rollback/bill targets and guards. SP-Q2: complete
catalog and fixed-state applicability across actual content/provider contexts, including missing
meaning and category overlaps. SP-Q3: compact seven-language native UI and S005/S049/S014 lifecycle
composition. SP-Q4: honest-tier TDD, focused package/review, measured query cost and personally
observed native creation/edit/hauling/save/provider workflows. The source needs no behavior interview;
remaining questions are deferred research/proof obligations, not completed compatibility claims.
