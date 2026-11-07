# Routine personal meals — implementation tasks

Owner **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`, target1.7 beta.
Specification only. All implementation tasks remain unchecked. Use focused vertical RED/GREEN
slices and truthful test environments; document validation is not native gameplay acceptance.

## 1. Finalize exact targets and proposed defaults

- [ ] 1.1 Reinspect exact1.7 ThinkTree, packing, nutrition, native stock/unload and checked transfer paths; bind the MC01 eligibility and MC-Q1 proposed defaults to explicit native examples.
- [ ] 1.2 Create target documents for every actual opportunity, transfer, retention and player-intent patch, including atomic dependency, effective data flow, benign edits, later repatch and fallback.
- [ ] 1.3 Select actual XML Extensions operations and required metadata if modifying the ThinkTree; preserve siblings and implement no resolved-Def mutation shortcut.
- [ ] 1.4 Inspect actual optional inventory/food/stock providers and future S132 UI bindings; define one packing authority and explicit unsupported contracts without inventing an original source mod.

## 2. Reserve quantities and ownership in vertical slices

- [ ] 2.1 RED/GREEN appetite-based target,40% boundary, whole-unit floor, empty-reserve oversized-meal exception, partial useful pickup and zero/invalid measurements.
- [ ] 2.2 RED/GREEN personally available versus cargo/transport/bill/current-use quantities, native explicit stock and kept excess with one S021 ownership authority.
- [ ] 2.3 RED/GREEN split/merge/removal and automatic retention, newest-first surplus release, changed diet/target and unavailable storage without pickup/unload churn.
- [ ] 2.4 RED/GREEN common target setting, finite range/increment validation, inherited/current-save resolution, disabled main-menu callbacks and safe effective-value changes.

## 3. One ordinary packing opportunity

- [ ] 3.1 RED/GREEN colonist/self-packing eligibility and priority boundaries, including children, secure slaves, dependent patients, drafting, forced jobs and absent food needs.
- [ ] 3.2 RED/GREEN prepared-food suitability, actual nutrition and applicable thoughts/title/ingredient permissions,20-cell access and packing during communal shortages without a nutrition-total or days-of-food cutoff.
- [ ] 3.3 RED/GREEN one composed food score with S024/S057, stable ties and no S111 room or S055 donor/shuttle refill expansion.
- [ ] 3.4 Implement minimal checked physical pickup through native approach/reservations and S021 mass+volume bounds; confirm accepted portions and recover failed splits without food loss.
- [ ] 3.5 RED/GREEN changing stock/capacity/permission, concurrent claims, interruption and failure retry bounds; retain active-use and protected medicine/bedroll/baby ownership.
- [ ] 3.6 RED/GREEN atomic admission failure, original/effective target drift, compatible foreign changes and bounded per-patch startup warnings with preserved independent fallback.

## 4. Player control, shared eating and persistence

- [ ] 4.1 RED/GREEN per-pawn pause/resume and explicit pending-removal suspension; actual player removal pauses, cancellation before removal preserves prior state, automatic consumption/sharing does not pause.
- [ ] 4.2 Add contextual inventory reserve/pause/status controls without a new tab; use common settings UI for configuration and translate all new text into seven baseline languages.
- [ ] 4.3 RED/GREEN S055 shared meal use, S021 cargo-first unspecified removal and S024 interrupted-meal recovery; no reserve recollection of an eater's current-use portion.
- [ ] 4.4 RED/GREEN save/load/map transfer and feature disable/re-admission with actual item/provenance conservation, no replayed pickup and no automatic source-settings import.

## 5. Review and native acceptance

- [ ] 5.1 Run strict typed OpenSpec validation and only affected focused tests, owning build and package/dependency checks, retaining nonzero RED/GREEN evidence.
- [ ] 5.2 Independently review the scoped implementation and resolve findings with evidence; repeat affected checks after fixes.
- [ ] 5.3 On the reviewed build, run fresh minimized isolated native workflows: adult and child packing, ordinary eating, partial capacity, insufficient stock, explicit stock/cargo mix, oversize retention and changed diet. Personally inspect actual approach/pickup, inventory/stack changes and ingestion outcomes through the Dev Gateway.
- [ ] 5.4 Exercise native sharing, manual unload/cancel/resume, shortage/replenishment, save/load, inherited settings and supported provider-present/absent/changed cases; retained screenshots must connect actual inputs to results, not just setup or logs.
- [ ] 5.5 Complete final semantic balance/playability review of reserve quantities, communal availability, self-care priority, native nutrition/thoughts, capacity and hauling throughput. Retain reviewed-build/mod identity, actions, before/after observations, normal-config hashes and cleanup under the root acceptance gate; update coverage only for personally verified behavior.
