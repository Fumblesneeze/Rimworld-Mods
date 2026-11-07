# [Kit] Graze up

## Identity and provenance

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
Origin: replacement, **S043 / P1-34**, package `kittahkhan.grazeup`, Workshop **2302739121**,
author KittahKhan. LoadFolders selects `Current` for 1.7. Selected assembly/file version 1.0.0.0,
DLL SHA-256 `6A6B5918B0D533C680FE9326C497BB4D0500B264AA9F76BDEE50F662D1ECFF90`.
No source license established; no source code, assemblies or assets copied into the product.

[Research](../refinement/2026-11-03-graze-up-research.md) retains public docs, complete source/DLL
inventory, exact native inspection, settings/formulas, three static Core comparators and consumer
boundaries. [Proposal](../../../../openspec/changes/specify-megamod-grazing/proposal.md),
[design](../../../../openspec/changes/specify-megamod-grazing/design.md),
[grazing spec](../../../../openspec/changes/specify-megamod-grazing/specs/megamod-grazing/spec.md),
[lifespan spec](../../../../openspec/changes/specify-megamod-grazing/specs/megamod-plant-lifespan/spec.md)
and [tasks](../../../../openspec/changes/specify-megamod-grazing/tasks.md) own the draft.
**Nothing implemented or accepted.**

## Behavior inventory

| ID / observable capability | Native input/action → result | Retained / corrected / added | Status |
| --- | --- | --- | --- |
| GU-01 Grazing | Grazer bites plant → one current-state food/growth or health result | S087 now separates crop-health consumption and adds pasture loss; growth plants retain8% when they survive | Draft |
| GU-02 Grazing | Eat regrowing plant → extra nutrition | Retain +250% extra /350% total; S087 substitutes edible-yield nutrition and consumption cost where applicable | Draft |
| GU-03 Grazing | Listed species wants more than nondestructive bite → one consumed plant | Strict greater-than and +50% full-growth bonus on growth route only, capped to request; no second pasture roll | Draft |
| GU-04 Grazing | Consider plant → growth/edible/burning rules plus diet/access | Retain 40% global minimum for trees too, and leafless/harvest-readiness override | Draft |
| GU-05 Grazing | Graze through burial → snow and sand handled consistently | Retain actual source scope, rename misleading snow-only policy | Draft |
| GU-06 Grazing | Empty/invalid/changed bite → no food or successful eating effect | Correct invented 0.1 food, negative nutrition/growth increase and zero-progress eligibility | Draft |
| GU-07 Grazing | Tool user with valid plant diet → approach/chew rooted plant | Retain source route without pickup/table/extra-plant inventory; native cancellation | Draft |
| GU-08 Lifespan | Age under configured scale/floor → ordinary damage at adjusted lifespan | Retain all finite plants, half-gap floor and unscaled >1-day exception; no age reset | Draft |
| GU-09 Both | Edit/save/reset policy → correct effective scope | Common global/save settings, valid combinations, clear numeric labels and retained species IDs | Draft |
| GU-10 Both | Provider/target unsupported → explained affected fallback | Atomic grazing group, independent lifespan group and per-patch admission | Draft |

Numeric defaults/ranges and actual conditions are in the specs. Source nonpositive-nutrition
deletion/0.1-food fallback, discarded unknown species and global-only custom settings are not adopted.
No new plant Def, content, per-plant food pool or plant index is introduced. Partial grazing already
exists in the inspected base game; source's retained floor/bonuses/eligibility are additional behavior.

## Interactions and composed result

S087 [Grazing Lands](grazing-lands.md) now owns the shared consumption calculation after complete
selected source/binary research. This unimplemented S043 draft is revised in place: edible-yield
nutrition, crop-health consumption and one pasture-loss chance compose directly with S043 access,
growth, bonuses and species policy. GL-Q1 retains explicit proposed balance/rounding decisions;
GU-Q3 retains effective provider and S077 lifecycle verification. S088 concerns child growth points,
not plants; its earlier forward reference is withdrawn. No self-patch stack.

Native diet/food-policy and access rules remain effective. S041 food handling preserves ordinary
successful-ingestion effects without treating a fractional plant bite as a whole food-stack transfer.
Shared search data may reflect native growth/destruction; no independent Graze up cache. Ordinary
harvest items, work and caravan abstract forage are outside these hooks. Source does not update pen
capacity estimates; no exact stocking forecast or unchanged grazing economy is claimed.

## UI audit and relocation

| Source surface | Purpose | Owned destination / deviation |
| --- | --- | --- |
| Global Graze up settings page | Nine policy values | Common Animals and plants / Grazing and Plant lifespan sections |
| Destructive-race checkbox grid | Species-level policy | Searchable identity-safe set editor within common scoped settings |
| Reset button | Reset global defaults | Explicit global reset versus current-save restore-inheritance/reset |
| No bottom tab/gizmo/debug action | None | None introduced |

Every real setting has global default/Use global/current-save controls; no scope exception. Plant
growth/age and individual pawn state remain ordinary gameplay state. Main-menu save mutation is
disabled, inherited editors inactive, dormant overrides retained. Minimum/remainder combinations
are resolved after inheritance; a capped stored remainder remains recoverable and clearly labeled.
Show additive and total bonuses accurately, retain unavailable species IDs and all seven languages.

## Contracts consumed by other mods or saves

| Contract / evidence | Decision and boundary |
| --- | --- |
| Native Plant.Growth, age, hit points and save state | Preserve ordinary plant identity/state; no conversion or duplicated saved food reservoir |
| Native amount count/returned nutrition and outer ingestion | One actual result; early invalid-bite guard before side effects; ordinary successful effects retained once |
| Native PlantProperties lifespan and limited-life distinction | Adjust supported finite result once; preserve native age/damage and unlimited plants |
| Source public static settings/helper/types and global keys | No executable consumer found in bounded installed text/DLL scans; no speculative facade, package spoof or config import |
| Original providers | Graze up active suppresses both owned groups; Grazing Lands suppresses owned grazing whenever that original provider is loaded; leave foreign owners untouched |

Original name/package metadata or matching methods alone do not prove provider semantics. Optional
adapters must be package/shape guarded and absent-safe. Existing unresolved states/identities are
not silently converted. Safe provider switching/removal remains unproved; future migration needs its
own backup, atomicity, restoration and verified postconditions.

## Implementation and coverage

| Scope | Implemented | Deviations / risks | Missing |
| --- | --- | --- | --- |
| GU-01–10 | Nothing | Safe no-bite semantics, strict remaining-growth eligibility and settings corrections | All code/tests/targets/native acceptance |
| Grazing/lifespan hooks | Nothing | Atomic effective admission with independent lifespan | Exact current-build and provider interaction proof |
| Selected-source composition | Nothing | S087 defines combined grazing; S077 owns plant-lifespan effects; S088 is unrelated child growth | GL-Q1 balance and effective/native composition proof |
| Performance/balance | Nothing | Retained source defaults change pasture economy; no exact pen forecasts | Finalized comparators, measured costs and live food/age workflows |

Core and Harmony required for the proposed implemented hooks; original sources/DLC optional. No
XML use presently; any later XML requires XML Extensions, content/package/shape checks and
merge-preserving changes. No upstream, private Harmony or Gateway DLLs in the product.

## Verification and acceptance

GU-Q1–4 retain native caller/admission, common-policy and selected-source composition, finalized
balance and reviewed-build acceptance work. Host math/transition tests cannot establish native job
selection or medical/dining effects. Final evidence must show real grazing/harvesting/tool-user
actions, changed targets, burial, policy/reset/save flows and native age damage in fresh minimized
isolated games, with personally inspected causal screenshots and exact optional-provider runs.
