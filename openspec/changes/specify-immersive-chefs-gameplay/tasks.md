## 1. mods/ImmersiveChefs - Persistent model and classification

- [x] 1.1 TDD RED: Add failing serialization, stack split/merge, material classification, and absent-optional-mod tests for sanitation, embedded ware, preparation, and meal-state components.
- [x] 1.2 TDD GREEN: Implement the smallest versioned component models and explicit classifiers that pass those tests.
- [x] 1.3 REFACTOR: Separate Def extensions, runtime snapshots, and compatibility adapters while preserving unknown ThingComps and `CompIngredients`.
- [x] 1.4 BUILD: Add settings persistence/UI with bounded defaults and clearly label restart-required settings.

## 2. mods/ImmersiveChefs - Kitchenware and production

- [x] 2.1 TDD RED: Add failing tests for Stuff-aware cookware, plates, silverware, chef's knives, fixed-adobe recipes, primitive stone durability/performance, product-aware fabrication tiers, trade-only self-cleaning glitterworld cookware, porous-material hygiene profiles, resource costs, and derived stat calculations; prove ceramic/porcelain content remains absent.
- [x] 2.2 TDD GREEN: Add Defs, tiered crafting-spot/smithy/machining recipes, belt equipment behavior, material-category mappings, glitterworld acquisition, and color-from-Stuff rendering needed to pass them.
- [x] 2.3 TDD RED: Add failing reservation and exact vanilla recipe-classification tests for strict/prefer/off ware modes, urgent starvation fallback, imported meals, exclusions, Simple/Fine/Lavish Def tables, unchanged unclassified recipes, and complexity multipliers.
- [x] 2.4 TDD GREEN: Implement ware-aware cooking and nutrient-paste job/recipe hooks without changing excluded food behavior.
- [x] 2.5 REFACTOR: Isolate recipe classification and reservation seams so food mods can extend them without replacing core methods.
- [ ] 2.6 IN-GAME: Verify primitive stone, soft, smithy, machining, adobe, and glitterworld acquisition paths; ceramic/porcelain absence; Stuff retention/color; belt equip without mutable knife sanitation; clean preference; emergency fallback; exact vanilla complexity; unchanged unclassified mod recipes; excluded foods; and supported nutrient-paste paths.

## 3. mods/ImmersiveChefs - Sanitation and dishwashing

- [x] 3.1 TDD RED: Add failing lifecycle tests for dirty transitions after actual work/ingestion, per-serving Def/Stuff/quality/hit-point bindings, unconditional expiry recovery, non-fire destruction recovery, effective-flammability fire loss/survival, glitterworld self-cleaning, silverware, same-schema save/load, and interrupted jobs.
- [x] 3.2 TDD GREEN: Implement the sanitation transitions and Cleaning work giver needed to pass them.
- [x] 3.3 TDD RED: Add failing source-priority, atomic single-debit Dubs water admission, insufficient-water and changed-shape fail-closed behavior, connection, capacity, identity round-trip, power/water pause-resume, cancellation/ejection, research-gate, actionable-alert, hauling, and bounded live/restart settings tests for hand washing and both dishwashers.
- [x] 3.4 TDD GREEN: Implement water-source adapters, the identity-preserving Processor Framework adapter plus absent/incompatible local cycle, preferred hauling, exact interruption state, `Dishwashing`/`Professional Kitchens` research, and aggregated missing/backlog alerts.
- [x] 3.5 REFACTOR: Bound source searches and expose stable adapter interfaces for Dubs Bad Hygiene and Gastronomy clearing.
- [ ] 3.6 IN-GAME: Verify powered/plumbed connection and consumption rules, automatic pause/resume from unchanged progress, cancellation recovery, source fallback order, 16/64-unit capacity, dirty stacks, terminal plate conservation/fire loss, research locks, alert drill-down/clearing, same-build save/load, and supported integration paths.

## 4. mods/ImmersiveChefs - Prepared food

- [x] 4.1 TDD RED: Add failing tests for provenance, nutrition, broad dietary flags, 0–100 preparation quality, default ingredient quality, and the bounds/application timing of rot multiplier and work reduction settings.
- [x] 4.2 TDD GREEN: Add the `Professional Kitchens`-gated prep station, recipes, and prepared-food components with per-serving preservation needed to pass them.
- [x] 4.3 TDD RED: Add failing tests for on-demand nutrient-paste preparation, bounded live paste-quality settings, exact-source hiding, broad ideology/diet safety, zero paste poisoning, and mixed-recipe quality.
- [x] 4.4 TDD GREEN: Implement vanilla and supported Vanilla Nutrient Paste Expanded adapters.
- [x] 4.5 REFACTOR: Keep preparation transformations independent from meal recipe ownership and variety calculations.
- [ ] 4.6 IN-GAME: Verify preparation bills, fast rot, cooking acceleration, paste output, inspect strings, dietary restrictions, and save/load.

## 5. mods/ImmersiveChefs - Cooperative cooking

- [x] 5.1 TDD RED: Add failing state-machine tests for early assistant requests, applicable linked stations, exclusive claims, simultaneous-work accrual, skill weighting, caps, and cancellation.
- [x] 5.2 TDD GREEN: Implement `Professional Kitchens`-gated sauce, meat, vegetable, and pastry station Defs plus bounded lead/assistant work coordination.
- [x] 5.3 REFACTOR: Move assistant assignment, contribution snapshots, cooldowns, and station applicability behind small interfaces.
- [ ] 5.4 IN-GAME: Verify assistants depart during lead hauling, contribute only during overlapping cooking ticks, release claims, respect priorities, and affect speed/quality.

## 6. mods/ImmersiveChefs - Meal quality, temperature, and reheating

- [x] 6.1 TDD RED: Add failing calculation tests for the bounded weighted quality formula, labels, missing-data defaults, poison cap, mood scale, and disabled-feature behavior.
- [x] 6.2 TDD GREEN: Implement culinary snapshots and ingestion effects without replacing vanilla food-poisoning ownership.
- [x] 6.3 TDD RED: Add failing deterministic tests for 70 °C completion, temperature bands, lazy ambient cooling, refrigerator/freezer rates, microwave threshold/60 °C output/quality loss/risk, and stack compatibility.
- [x] 6.4 TDD GREEN: Implement temperature state, the vanilla-`Electricity`-gated microwave building/jobs, and Gastronomy pre-delivery reheating.
- [x] 6.5 REFACTOR: Cache lazy thermal calculations and expose bounded modifier hooks rather than patching per-tick Thing updates.
- [ ] 6.6 IN-GAME: Verify inspect gauges, cooling across holders, mood/poison outcomes, microwave jobs, stack splits, save/load, and restaurant delivery.

## 7. mods/ImmersiveChefs - Dining standards and optional integrations

- [x] 7.1 TDD RED: Add failing tests for silverware acquisition, one combined thought, pawn categories, the deterministic colony/vanilla-title threshold tables, stainless availability, deferred ceramic/porcelain absence, satisfiable vanilla substitutes, and independent toggles.
- [x] 7.2 TDD GREEN: Implement eater/server service jobs, comfort aggregation, expectation/title evaluation, and thought workers.
- [x] 7.3 TDD RED: Add contract tests for every supported package-ID/shape adapter and its absent, inactive, disabled, compatible, and changed-shape states.
- [x] 7.4 TDD GREEN: Implement Expanded Materials, ABS, Dubs, Gastronomy, Variety Matters, Vanilla Food Variety Expanded, Vanilla Expanded Framework, Vanilla Nutrient Paste Expanded, and guarded Processor adapters.
- [x] 7.5 REFACTOR: Audit Harmony ownership, prepare guards, optional reflection isolation, Def patch XPath scope, and no-op fallbacks.
- [ ] 7.6 IN-GAME: Verify the base mod alone and each supported local integration set, including Royalty tiers with no ceramic provider, registered stainless alternatives, and food-variety provenance. Do not claim Vanilla Cooking Expanded complexity integration in this change.

## 8. mods/ImmersiveChefs - Release verification

- [x] 8.1 BUILD: Run a clean Release build and guarded unit suite against the configured RimWorld 1.6 installation with zero warnings, errors, and zero-test false positives.
- [ ] 8.2 IN-GAME: Run isolated minimal, all-supported, and incompatibility smoke matrices through the repo's gateway/FlaUI tooling, retaining state/log/screenshot evidence and restoring any normal configuration in `finally`.
- [x] 8.3 REFACTOR: Perform independent correctness/test and KISS/compatibility reviews, fix accepted findings, and rerun the complete matrix.
- [x] 8.4 DOCUMENT: Update player settings, compatibility, current-schema save behavior, known limitations, and troubleshooting documentation; state that pre-release migration/uninstall cleanup, ceramics/porcelain, Vanilla Cooking Expanded classification, food waste, and preservation are deferred.

## 9. mods/ImmersiveChefs - Travel, guests, assisted feeding, and storage

- [x] 9.1 SPEC: Define caravan cooling and exact ware conservation, automatic wild-water travel washing, Hospitality guest source priority, independent-child and patient-feeding rules, missing-silverware dirt, and clean/dirty storage and forbiddance behavior.
- [ ] 9.2 TDD RED: Add failing pure tests for wash provenance, risk deltas, caravan ambient selection, service-ware source priority, diner/feeder consequence ownership, self-feeding eligibility, and storage-filter predicates.
- [ ] 9.3 TDD GREEN: Extend serialized sanitation and culinary state plus selection/outcome policies with the smallest implementation that passes the new pure tests.
- [ ] 9.4 TDD RED/GREEN: Add guarded runtime patches for caravan ingestion/cooling, map eating and patient feeding, bounded vanilla dirt creation, exact holder returns, and storage notifications; add loaded-game integration tests for concrete Def and Harmony shapes.
- [ ] 9.5 COMPATIBILITY: Inventory locally downloaded Hospitality Continued, implement its package-and-shape-gated guest adapter without a hard reference, and verify absent, inactive, active-compatible, and changed-shape behavior.
- [ ] 9.6 IN-GAME: Through actual pawn jobs and player-observable behavior, verify a caravan meal cools and returns exact clean wild-water-provenance ware; a Hospitality guest prefers colony then inventory silverware; an independent child dines normally; a nurse feeding conscious/unconscious patients assigns thoughts correctly; missing silverware creates bounded dirt; and clean/dirty stockpile filters and forbiddance affect hauling/cleaning.
- [ ] 9.7 REVIEW: Review only this incremental diff for correctness, Harmony compatibility, state conservation, test honesty, and KISS; resolve findings and rerun the affected and full matrices.
