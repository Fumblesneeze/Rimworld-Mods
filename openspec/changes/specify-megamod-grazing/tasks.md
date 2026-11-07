# Implementation tasks — grazing and plant lifespan

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
Specification only. All runtime tasks remain open; source inspection is not RED/GREEN or acceptance.

## 1. Establish supported native and provider boundaries

- [ ] 1.1 Resolve exact current-build eligibility, plant amount/count, tool-user preparation, early commit and lifespan seams, including derived plant overrides and native effects before amount calculation.
- [ ] 1.2 Publish per-patch target specs and implement/test minimal effective admission, incoming transpiler validation where used, duplicate initialization, owned group rollback, later repatch handling and per-skipped-patch diagnostics.
- [ ] 1.3 Implement only the composed S043/S087 contract after its GL-Q1 balance inputs are resolved; prove GU-Q3's effective provider and S077 lifecycle composition without stacked self-patches. S088 concerns child growth points, not plants.
- [ ] 1.4 Establish original Graze up/Grazing Lands suppression and supported provider adapters with absent/present/changed-shape cases; leave unrelated features and foreign owners intact.

## 2. Vertical grazing behaviors

- [ ] 2.1 Focused Unit RED → GREEN for partial/exhausting bites, proportional growth, regrowing bonus, capped nutrition and unchanged age, with numerical conservation checks.
- [ ] 2.2 Focused Unit RED → GREEN for destructive species, strict greater-than boundary, destruction bonus and normally regrowing plant exclusion.
- [ ] 2.3 Focused RED → GREEN for minimum/remainder boundary, positive representable progress, invalid/zero nutrition or request, and changed policy/target at commit; reject zero-test runs.
- [ ] 2.4 Implement admitted native eligibility and amount adapters with current diet/access/reservation checks; test real Def/provider state in the honest isolated-game tier, not a mocked loaded mod.
- [ ] 2.5 Implement rooted tool-user preparation and early no-bite guards; prove no duplicate successful-ingestion effects, no artificial food and cancellation/reselection without an unchanged unusable-target loop.
- [ ] 2.6 Verify ordinary item ingestion and harvesting stay native, including physical quantities and source/native side effects for successful partial/destructive bites.

## 3. Lifespan and common configuration

- [ ] 3.1 Focused Unit RED → GREEN for lifespan scaling, unscaled one-day boundary, halfway floor, finite conversion and native unlimited-lifespan preservation.
- [ ] 3.2 Implement admitted independent lifespan group using current effective save policy without Def mutation, age reset or direct damage/healing.
- [ ] 3.3 Add the nine S043 common settings plus the two S087 grazing options, defaults/ranges, coupled-limit resolution, commit timing, dormant overrides and main-menu guards; verify inheritance across save switching.
- [ ] 3.4 Add identity-safe searchable species selection, retained unavailable IDs, duplicate normalization, accurate bonus/total labels and explicit scope-specific resets in all seven baseline languages.
- [ ] 3.5 Prove native growth/age/health round trips and common setting persistence without source config imports, synthetic nutrition pools or automatic plant conversion.

## 4. Review and focused verification

- [ ] 4.1 Measure finalized Core grass/berry/haygrass and supported modded plant comparators, feeding economy and lifespan consequences; distinguish native estimate limitations from actual bite results.
- [ ] 4.2 Measure combined candidate/ingestion/lifespan costs and shared-data invalidation, without another per-port map index; validate ordinary job availability under pasture pressure.
- [ ] 4.3 Run affected focused tests, owning build/package checks and strict OpenSpec validation; require Harmony only when implemented and keep source/Harmony/Gateway DLLs out of the product package.
- [ ] 4.4 Independently review the scoped implementation; resolve findings and rerun affected checks, retaining nonzero RED/GREEN and reviewed package identity.

## 5. Final reviewed-build native acceptance

- [ ] 5.1 In a fresh minimized isolated game, observe real animal grazing, partial growth, regrowth, destructive species and exact equality/zero-progress cases through native jobs; personally inspect before/action/after evidence.
- [ ] 5.2 Exercise rooted tool-user grazing, changed/removed targets, interrupted jobs, snow/sand policy and leafless plants through actual workflows, plus ordinary harvesting/item-eating regressions.
- [ ] 5.3 Use real settings and save/load workflows to prove global/override/capped remainder/reset/unavailable species behavior and lifespan age/damage effects; verify optional providers in exact separate active-mod runs.
- [ ] 5.4 Complete root AGENTS.md acceptance and semantic balance pass on the reviewed build with exact player actions, observed results, screenshots/logs, mod order, hashes, configuration protection and cleanup; only then close gameplay tasks and update replacement coverage.
