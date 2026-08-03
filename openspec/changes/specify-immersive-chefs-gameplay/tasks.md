## 1. mods/ImmersiveChefs - Persistent model and classification

- [x] 1.1 TDD RED: Add failing serialization, stack split/merge, material classification, and absent-optional-mod tests for sanitation, embedded ware, preparation, and meal-state components.
- [x] 1.2 TDD GREEN: Implement the smallest versioned component models and explicit classifiers that pass those tests.
- [x] 1.3 REFACTOR: Separate Def extensions, runtime snapshots, and compatibility adapters while preserving unknown ThingComps and `CompIngredients`.
- [x] 1.4 BUILD: Add settings persistence/UI with bounded defaults and clearly label restart-required settings.

## 2. mods/ImmersiveChefs - Kitchenware and production

- [x] 2.1 TDD RED: Add failing tests for Stuff-aware cookware, plates, cutlery, chef's knives, fixed-adobe recipes, primitive stone durability/performance, product-aware fabrication tiers, trade-only self-cleaning glitterworld cookware, porous-material hygiene profiles, resource costs, and derived stat calculations; prove ceramic/porcelain content remains absent.
- [x] 2.2 TDD GREEN: Add Defs, tiered crafting-spot/smithy/machining recipes, belt equipment behavior, material-category mappings, glitterworld acquisition, and color-from-Stuff rendering needed to pass them.
- [x] 2.3 TDD RED: Add failing reservation and exact vanilla recipe-classification tests for strict/prefer/off ware modes, urgent starvation fallback, imported meals, exclusions, Simple/Fine/Lavish Def tables, unchanged unclassified recipes, and complexity multipliers.
- [x] 2.4 TDD GREEN: Implement ware-aware cooking and nutrient-paste job/recipe hooks without changing excluded food behavior.
- [x] 2.5 REFACTOR: Isolate recipe classification and reservation seams so food mods can extend them without replacing core methods.
- [ ] 2.6 IN-GAME: Verify primitive stone, soft, smithy, machining, adobe, and glitterworld acquisition paths; ceramic/porcelain absence; Stuff retention/color; belt equip without mutable knife sanitation; clean preference; emergency fallback; exact vanilla complexity; unchanged unclassified mod recipes; excluded foods; and supported nutrient-paste paths.
  - [x] 2.6a Observe native bills produce one Stuff-retaining granite cookware set at `CraftingSpot` and one steel cookware set at a powered `TableMachining`, then compare their ordinary inspector-visible quality, cleanliness, comfort, speed, and culinary modifiers.

## 3. mods/ImmersiveChefs - Sanitation and dishwashing

- [x] 3.1 TDD RED: Add failing lifecycle tests for dirty transitions after actual work/ingestion, per-serving Def/Stuff/quality/hit-point bindings, unconditional expiry recovery, non-fire destruction recovery, effective-flammability fire loss/survival, glitterworld self-cleaning, cutlery, same-schema save/load, and interrupted jobs.
- [x] 3.2 TDD GREEN: Implement the sanitation transitions and Cleaning work giver needed to pass them.
- [x] 3.3 TDD RED: Add failing source-priority, atomic single-debit Dubs water admission, insufficient-water and changed-shape fail-closed behavior, connection, capacity, identity round-trip, power/water pause-resume, cancellation/ejection, research-gate, actionable-alert, hauling, and bounded live/restart settings tests for hand washing and both dishwashers.
- [x] 3.4 TDD GREEN: Implement water-source adapters, the identity-preserving Processor Framework adapter plus absent/incompatible local cycle, preferred hauling, exact interruption state, `Dishwashing`/`Professional Kitchens` research, and aggregated missing/backlog alerts.
  - [x] 3.4a TDD RED/GREEN: Reject local dishwasher admission and `Doing dishes` destination selection while required power is off, while preserving admission when the same appliance is operational.
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

- [x] 7.1 TDD RED: Add failing tests for cutlery acquisition, one combined thought, pawn categories, the deterministic colony/vanilla-title threshold tables, stainless availability, deferred ceramic/porcelain absence, satisfiable vanilla substitutes, and independent toggles.
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

- [x] 9.1 SPEC: Define caravan cooling and exact ware conservation, automatic wild-water travel washing, Hospitality guest source priority, independent-child and patient-feeding rules, missing-cutlery dirt, and clean/dirty storage and forbiddance behavior.
- [ ] 9.2 TDD RED: Add failing pure tests for wash provenance, risk deltas, caravan ambient selection, service-ware source priority, diner/feeder consequence ownership, self-feeding eligibility, and storage-filter predicates.
- [ ] 9.3 TDD GREEN: Extend serialized sanitation and culinary state plus selection/outcome policies with the smallest implementation that passes the new pure tests.
- [ ] 9.4 TDD RED/GREEN: Add guarded runtime patches for caravan ingestion/cooling, map eating and patient feeding, bounded vanilla dirt creation, exact holder returns, and storage notifications; add loaded-game integration tests for concrete Def and Harmony shapes.
  - [x] 9.4a Implement covered caravan ingestion, live world-tile cooling, clean-first loose ware acquisition, exact return with wild-water provenance, cancellation rollback, positive-ingestion commit, and animal/travel-food exclusions with loaded-game tests.
  - [x] 9.4b Implement the remaining map/patient/dirt runtime behavior and its loaded-game tests.
  - [x] 9.4c Prove the existing non-humanlike exclusion through the regular map ingest-job boundary: no ware session or custom dining consequence, exact clean plate recovery, and untouched nearby cutlery.
  - [x] 9.4d TDD RED/GREEN: Create exactly one native dirt event at the completed map eating location when an eligible self-diner actually eats without required cutlery; prove Off mode, excluded diners/foods, aborted ingestion, and world-holder ingestion remain unchanged.
  - [x] 9.4e TDD RED/GREEN: Extend the native `FeedPatient` job so the feeder reserves and carries cutlery while the fed pawn owns dining effects; conscious patients receive the missing-cutlery consequence, unconscious patients and feeders do not, and completed no-cutlery feeding creates one native dirt event at the patient location.
- [x] 9.5 COMPATIBILITY: Inventory locally downloaded Hospitality Continued, implement its package-and-shape-gated guest adapter without a hard reference, and verify absent, inactive, active-compatible, and changed-shape behavior.
- [ ] 9.5a COMPATIBILITY: Implement the specified `avilmask.CommonSense` post-dining adapter without a hard reference: self-diners or patient-feeding nurses claim their exact dirty plate and cutlery after completion, prefer an accepting dishwasher, fall back to normal hand washing, and safely release the ware when no cleaning path exists; verify absent, inactive, disabled, active-compatible, Gastronomy-coexistence, and changed-shape behavior.
- [ ] 9.6 IN-GAME: Through actual pawn jobs and player-observable behavior, verify a caravan meal cools and returns exact clean wild-water-provenance ware; a Hospitality guest prefers colony then inventory cutlery; an independent child dines normally; a nurse feeding conscious/unconscious patients assigns thoughts correctly; missing cutlery creates bounded dirt; and clean/dirty stockpile filters and forbiddance affect hauling/cleaning.
  - [x] 9.6a Observe native caravan auto-eating from the selected world caravan: before eating the visible inventory contains the meal and cutlery while its embedded plate is not a separate row; afterward the meal disappears, the exact plate becomes visible beside the cutlery, and RimWorld reports the caravan is out of food. Retain the before/after screenshots. Loaded integration evidence separately proves exact Thing identity, world-tile temperature progression, and wild-water provenance.
  - [x] 9.6b Observe native animal caravan eating: before eating the visible inventory contains the intentionally cold/poor/contaminated meal and loose cutlery with no separate plate row; afterward the meal disappears while the returned plate and untouched cutlery remain. Loaded integration evidence separately proves exact identity, unchanged safe sanitation, and no custom food poisoning.
  - [x] 9.6c Observe native regular-map animal eating: the animal chooses the plated meal through its ordinary ingest job, never collects or moves adjacent cutlery, and leaves the exact embedded plate clean at the eating location after the meal disappears.
  - [x] 9.6d Observe a native colonist ingest job without available cutlery: the plated meal disappears, the dining thought reports missing cutlery, and one visible vanilla dirt event appears at the actual eating location.
  - [x] 9.6e Observe native conscious and unconscious `FeedPatient` jobs: the nurse performs any required cutlery pickup, each meal is fed at the patient's location, only the conscious patient receives the missing-cutlery thought, the nurse receives neither patient's result, and one visible vanilla dirt event appears for each completed no-cutlery feeding.
  - [ ] 9.6f With Common Sense active, observe an ordinary diner and a patient-feeding nurse claim the exact resulting dirty plate and cutlery after dining completes, deliver them to an accepting dishwasher before any hand-washing source, and release them to ordinary cleaning when no route is viable.
  - [x] 9.6g Observe clean and dirty plates route to their mutually exclusive stockpiles, a player-forbidden dirty plate remain untouched by hauling and `Doing dishes`, and the same exact plate enter a powered dishwasher only after the player allows it.
  - [x] 9.6h Observe two native arrived-Hospitality guest ingest jobs after player Space/speed input: reachable golden colony cutlery is used and returned dirty while that guest's personal setting remains clean; the isolated fallback guest retains the exact personal cutlery in the native Gear inventory after eating. Loaded state evidence separately proves that fallback item became dirty and both meals were consumed.
  - [x] 9.6i Observe a real Biotech child complete the exact plated-meal ingest job after native Space/speed input: the meal disappears, the exact golden plate and cutlery return visibly dirty, and the native Needs panel shows legendary cooking, steaming-hot food, and a proper place setting. Loaded integration evidence separately proves that the full vanilla child think tree chooses the exact meal through `JobGiver_GetFood`, a toddler's full think tree does not issue `Ingest`, and the shared map is not ticked by the fixture.
- [ ] 9.7 REVIEW: Review only this incremental diff for correctness, Harmony compatibility, state conservation, test honesty, and KISS; resolve findings and rerun the affected and full matrices.
  - [x] 9.7a Complete the focused caravan review, resolve result-aware commit, transactional contamination, lifecycle rollback, animal exclusion, exact-holder conservation, and pre-registration findings, then rerun affected and full tests.
  - [x] 9.7b Complete the focused cutlery-free map-dining review, restore the shared map's exact pre-test dirt state, and rerun focused, full, loaded-game, and observed-behavior verification.
  - [x] 9.7c Complete the focused dishwasher-admission and storage-policy review, replace synthetic power state with a real charged power net, keep fixture cleanup scoped to created Things, constrain randomized workers by capability, and rerun the loaded test and observed scenario.
  - [x] 9.7d Complete the focused Hospitality review, preserve explicit colony-versus-personal provenance before server transfers, conserve sanitation state when splitting personal stacks, correct the fully initialized guest fixture, and rerun the active-mod loaded suite plus observed guest workflow.
  - [x] 9.7e Complete the focused independent-child review, target the scenario meal by its exact fixture cell, exercise both child and toddler vanilla think-tree boundaries without global map ticks, preserve the clean plate service snapshot before marking the returned physical plate dirty, and rerun the exact active-mod loaded suite plus observed child workflow.
- [x] 9.8 PRERELEASE TERMINOLOGY (`fumblesneeze.immersivechefs`): Rename the former table-utensil gameplay concept to cutlery, including public DefNames, settings/policies, thoughts, filters, runtime identifiers, integration contracts, scenarios, labels, and documentation; do not retain obsolete aliases before release.
  - [x] 9.8a Update every affected OpenSpec contract and design note to use cutlery consistently.
  - [x] 9.8b TDD RED/GREEN: Make tests require the cutlery API and finalized Defs before renaming production code, then pass focused and full matrices with no tracked obsolete-term reference.
  - [x] 9.8c IN-GAME: Observe the renamed cutlery item and missing-cutlery thought through ordinary player-visible UI and native dining behavior.
  - [x] 9.8d REVIEW: Independently review the terminology slice for incomplete schema/API renames, accidental material changes, compatibility leakage, and truthful evidence; resolve findings and rerun affected verification.
