# FoodTracker

## Identity and provenance

Owner **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
S085/P2-14, FoodTracker by B0ARL0CK, `b0arl0ck.foodtracker`,
[Workshop3795037221](https://steamcommunity.com/sharedfiles/filedetails/?id=3795037221).
Installed DLL declares1.6; SHA256
`ECA6DD778A9B245B5BCED51909F157829ADF5B94D4061DE6EF68E72784217A43`.
[Research](../refinement/2026-11-05-food-tracker-research.md) records public-first evidence,
active-source analysis, selected binary/native checks and source permissions. Independent implementation;
no foreign code, assets or assemblies copied into the product.

[Proposal](../../../../openspec/changes/specify-megamod-partial-ingestibles/proposal.md),
[design](../../../../openspec/changes/specify-megamod-partial-ingestibles/design.md),
[tasks](../../../../openspec/changes/specify-megamod-partial-ingestibles/tasks.md).

## Specified scope

| Behavior | Owned draft |
| --- | --- |
| Interrupted food/drink use | Credit actual consumed amount once, preserve usable remainder and scale remaining work |
| Supported drug effects | Original-dose quantities, native recipient modifiers and harmful signs; unresolved risk/categorical schedules explicitly gate affected paths |
| Stacks and condition | Preserve individual fractions, ingredients/diet, poison/rot/HP/quality and physical limits |
| Save/holder lifecycle | Mid-consumption progress, no stale game state, no resurrection or silent invented recovery quantities |
| Capacity, economy, recipes | Actual selected mass/value/nutrition, no whole-product conversion from tiny count-based ingredients |
| Travel and UI | Selected supplies and direct caravan eating; contextual per-item/total amounts in seven languages |

## Intentional differences and conflicts

Do not copy source filter bypasses or food-policy broadening, generated ThingDef identities,
condition-resetting replacement items, current-attempt drug scaling, reversed addiction offsets,
display-only accounting, separate transfer caches or unguarded PatchAll/transpilers. Keep the normal
full-item duration; omit source beer120/go-juice100/ambrosia100 overrides and40-tick partial floor.
Keep positive remainders instead of the source4.5% discard threshold (public text instead claims
0.01nutrition). These are explicit owned corrections, not assertions that the source behaves this way.

No new bottom tab or food dashboard. Item quantities are entity state; ordinary UI stays contextual.
Source's developer logging preference is not imported. Any diagnostic is transient top-debug state,
defaultoff and unavailable outside developer mode. No empty gameplay settings framework is added.

Native/shared quantities compose directly with search, hauling, supplies, medical/drug/poisoning and
travel features. Physical stack thresholds remain distinct from usable full-meal/dose targets. No
runtime dependency on other repository mods. With original FoodTracker active, original owns overlap;
existing owned partial state cannot be reinterpreted or handed over without a separate migration.

## Contracts and remaining work

**Implemented: nothing.** No beta compatibility acceptance, source save/config migration, package
spoof or universal custom-outcome support. Living plants/corpses, non-ingestion medicine and instant
special-use items retain ordinary behavior. A bounded installed consumer search found no incoming
bindings; this is not proof that reflected/UTF-16 consumers do not exist.

FTR-Q1–6 retain effect/risk schedules, provider/path completeness, batch timing/persistence, transfer/recipe/supply
composition, UI/performance, target checks and final native evidence. All actual patches require
semantic admission with safe existing-state handling, bounded startup warnings, owned-only rollback
and later revalidation. External Harmony is required at first use; guarded XML integration requires
XML Extensions. Required dependency metadata belongs with implementation, not this prose draft.

Partial multi-item batch timing and exact unequal-portion quotes combined with S041's fair finite-taint
selection are explicitly unresolved and gated. This draft does not choose a new batch-time penalty,
reroll quoted deliveries or override the existing taint contract.
