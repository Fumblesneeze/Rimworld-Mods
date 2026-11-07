# Governing implementation tasks

Owner: **Make Rimworld Great Again**, `fumblesneeze.megamod`, RimWorld **1.7 beta**.
Plan revision **1**. Read [design](design.md), [roadmap](roadmap.md) and the assigned feature changes.
This is the aggregate coordination checklist. Each chunk's linked feature tasks hold its behavior
slices; expand assignments into focused spec → RED/GREEN → code → review work before dispatch.
A handoff gate below verifies that work; it does not replace those tasks or accept gameplay.

Documentation completion and future implementation are separate. All runtime tasks remain open.
The orchestrator owns dispatch, central integration, canonical installation, part acceptance and
replanning. Implementers own their leased code, focused tests, review fixes and executable scenario
authoring. Composite feature tasks requiring native acceptance stay unchecked until the final gate.

## 1. Planning artifact acceptance

- [x] 1.1 Complete the shared architecture, full S001–S137 allocation, prerequisite milestones, narrow handoff protocol and agile revision authority; align root/mod instructions and refinement entry points.
- [x] 1.2 Independently review the scoped documentation, resolve findings, check feature/link/dependency coverage and run strict typed OpenSpec validation. Record that no runtime behavior was implemented or accepted.

Evidence: [planning audit](../../../mods/megamod/docs/refinement/2026-11-07-implementation-planning-audit.md)
and local scoped snapshots/checks under `artifacts/Refinement/megamod-parallel-plan-20261107/`.

## 2. Execution setup and recurring dispatch rule

- [ ] 2.1 At implementation start inspect the current repository/tooling state, read all governing artifacts, confirm a clean ownership boundary around existing work and register the current plan revision. Do not assume historical blockers or old builds are current.
- [ ] 2.2 Create the first part manifest with stable feature/requirement/test/scenario IDs, contract versions, active edit leases, exact optional-mod claims and later-composition obligations. Inspect referenced sources and existing helpers before coding.

Repeat the dispatch manifest for every part. Every ready handoff must satisfy design section 7:
nonzero focused host evidence, real provider checks, independent review dispositions, target/spec
traceability, executable native workflows and honest unresolved status. The orchestrator records
failed/revised assignments in the roadmap log and updates affected tasks before proceeding.

## 3. P0 — Activation, settings and content

- [ ] 3.1 Orchestrator: Recheck exact 1.7 tooling/profile/package gates; freeze early settings/admission/loader-hook contracts, stable catalog and applied-policy publication with S029 drug maintenance; assign central writers and honest test registrations. Record requirement slices, leases, source references and scenario IDs in the part manifest.
- [ ] 3.2 Receive and inspect the **P0-A — Runtime admission and diagnostics** handoff for S035, S094. Require actual provider milestones, focused tests, independent review and executable native scenarios; keep gameplay acceptance open.
- [ ] 3.3 Receive and inspect the **P0-B — Common settings and presentation** handoff for S034, S047, S071. Require actual provider milestones, focused tests, independent review and executable native scenarios; keep gameplay acceptance open.
- [ ] 3.4 Receive and inspect the **P0-C — Content selection** handoff for S020, S029, S038. Require actual provider milestones, focused tests, independent review and executable native scenarios; keep gameplay acceptance open.
- [ ] 3.5 After all selected P0 chunks are ready, integrate and independently review the candidate, resolve findings, rerun affected checks and validate the exact package. Freeze runtime edits and record build identity plus the selected individual/combined scenarios from the roadmap.
- [ ] 3.6 Orchestrator: run the P0 isolated loader checks and native player workflows on that reviewed build; personally inspect causal results. Delegate product/scenario/tooling failures to their owners, integrate reviewed fixes and rerun affected scenarios on the new build. Retain exact-process action, screenshots, mod order, config hashes and cleanup evidence.
- [ ] 3.7 Complete the part playability/balance pass and coverage reconciliation; update only supported feature tasks and replacement states. Record later-composition obligations and accepted scope; do not close required blocked/unrun cases or use old-build evidence.

## 4. P1 — Things, storage and physical cargo

- [ ] 4.1 Orchestrator: Freeze C04/C05/C06 contracts; schedule facts and storage-independent portions before storage, then hauling. Record requirement slices, leases, source references and scenario IDs in the part manifest.
- [ ] 4.2 Receive and inspect the **P1-A — Shared Thing facts and queries** handoff for S033, S114, S121. Require actual provider milestones, focused tests, independent review and executable native scenarios; keep gameplay acceptance open.
- [ ] 4.3 Receive and inspect the **P1-B — Linked storage and refill** handoff for S005, S049, S050, S098, S107. Require actual provider milestones, focused tests, independent review and executable native scenarios; keep gameplay acceptance open.
- [ ] 4.4 Receive and inspect the **P1-C — Physical portions, cargo and ingestion** handoff for S021, S041, S085, S126. Require actual provider milestones, focused tests, independent review and executable native scenarios; keep gameplay acceptance open.
- [ ] 4.5 After all selected P1 chunks are ready, integrate and independently review the candidate, resolve findings, rerun affected checks and validate the exact package. Freeze runtime edits and record build identity plus the selected individual/combined scenarios from the roadmap.
- [ ] 4.6 Orchestrator: run the P1 isolated loader checks and native player workflows on that reviewed build; personally inspect causal results. Delegate product/scenario/tooling failures to their owners, integrate reviewed fixes and rerun affected scenarios on the new build. Retain exact-process action, screenshots, mod order, config hashes and cleanup evidence.
- [ ] 4.7 Complete the part playability/balance pass and coverage reconciliation; update only supported feature tasks and replacement states. Record later-composition obligations and accepted scope; do not close required blocked/unrun cases or use old-build evidence.

## 5. P2 — Production and bills

- [ ] 5.1 Orchestrator: Freeze logical bill, material-plan and browser contracts; publish bills before materials before browser integration. Record requirement slices, leases, source references and scenario IDs in the part manifest.
- [ ] 5.2 Receive and inspect the **P2-A — Production authority and linked bills** handoff for S014, S017, S018. Require actual provider milestones, focused tests, independent review and executable native scenarios; keep gameplay acceptance open.
- [ ] 5.3 Receive and inspect the **P2-B — Ingredients, workers and recipes** handoff for S015, S019, S026, S059, S122. Require actual provider milestones, focused tests, independent review and executable native scenarios; keep gameplay acceptance open.
- [ ] 5.4 Receive and inspect the **P2-C — Integrated production browser** handoff for S009, S010, S011, S012. Require actual provider milestones, focused tests, independent review and executable native scenarios; keep gameplay acceptance open.
- [ ] 5.5 After all selected P2 chunks are ready, integrate and independently review the candidate, resolve findings, rerun affected checks and validate the exact package. Freeze runtime edits and record build identity plus the selected individual/combined scenarios from the roadmap.
- [ ] 5.6 Orchestrator: run the P2 isolated loader checks and native player workflows on that reviewed build; personally inspect causal results. Delegate product/scenario/tooling failures to their owners, integrate reviewed fixes and rerun affected scenarios on the new build. Retain exact-process action, screenshots, mod order, config hashes and cleanup evidence.
- [ ] 5.7 Complete the part playability/balance pass and coverage reconciliation; update only supported feature tasks and replacement states. Record later-composition obligations and accepted scope; do not close required blocked/unrun cases or use old-build evidence.

## 6. P3 — Construction and item actions

- [ ] 6.1 Orchestrator: Freeze placement/replacement ownership and shared native-command registrations; publish construction contracts before dependent quality/removal work. Record requirement slices, leases, source references and scenario IDs in the part manifest.
- [ ] 6.2 Receive and inspect the **P3-A — Construction planning and access** handoff for S028, S048, S060, S063, S129. Require actual provider milestones, focused tests, independent review and executable native scenarios; keep gameplay acceptance open.
- [ ] 6.3 Receive and inspect the **P3-B — Quality, repair and safe removal** handoff for S013, S054, S061, S082, S097. Require actual provider milestones, focused tests, independent review and executable native scenarios; keep gameplay acceptance open.
- [ ] 6.4 Receive and inspect the **P3-C — Designations and Architect controls** handoff for S008, S016, S036, S067, S136. Require actual provider milestones, focused tests, independent review and executable native scenarios; keep gameplay acceptance open.
- [ ] 6.5 Receive and inspect the **P3-D — Item processing and material counts** handoff for S022, S023, S032, S096. Require actual provider milestones, focused tests, independent review and executable native scenarios; keep gameplay acceptance open.
- [ ] 6.6 After all selected P3 chunks are ready, integrate and independently review the candidate, resolve findings, rerun affected checks and validate the exact package. Freeze runtime edits and record build identity plus the selected individual/combined scenarios from the roadmap.
- [ ] 6.7 Orchestrator: run the P3 isolated loader checks and native player workflows on that reviewed build; personally inspect causal results. Delegate product/scenario/tooling failures to their owners, integrate reviewed fixes and rerun affected scenarios on the new build. Retain exact-process action, screenshots, mod order, config hashes and cleanup evidence.
- [ ] 6.8 Complete the part playability/balance pass and coverage reconciliation; update only supported feature tasks and replacement states. Record later-composition obligations and accepted scope; do not close required blocked/unrun cases or use old-build evidence.

## 7. P4 — Supplies, physiology and medical care

- [ ] 7.1 Orchestrator: Freeze supply, capabilities, care and effect-matching contracts; publish supply/capabilities before care/effects, then errands and health UI. Record requirement slices, leases, source references and scenario IDs in the part manifest.
- [ ] 7.2 Receive and inspect the **P4-A — Practical supplies and opportunistic work** handoff for S024, S055, S057, S073, S111, S123, S125. Require actual provider milestones, focused tests, independent review and executable native scenarios; keep gameplay acceptance open.
- [ ] 7.3 Receive and inspect the **P4-B — Needs, physiology and apparel rules** handoff for S003, S025, S045, S046, S099, S103, S106. Require actual provider milestones, focused tests, independent review and executable native scenarios; keep gameplay acceptance open.
- [ ] 7.4 Receive and inspect the **P4-C — Medical authority and automatic care** handoff for S042, S058, S062, S080, S086, S124, S134. Require actual provider milestones, focused tests, independent review and executable native scenarios; keep gameplay acceptance open.
- [ ] 7.5 Receive and inspect the **P4-D — Health inspection and explicit surgery** handoff for S007, S056, S089, S137. Require actual provider milestones, focused tests, independent review and executable native scenarios; keep gameplay acceptance open.
- [ ] 7.6 After all selected P4 chunks are ready, integrate and independently review the candidate, resolve findings, rerun affected checks and validate the exact package. Freeze runtime edits and record build identity plus the selected individual/combined scenarios from the roadmap.
- [ ] 7.7 Orchestrator: run the P4 isolated loader checks and native player workflows on that reviewed build; personally inspect causal results. Delegate product/scenario/tooling failures to their owners, integrate reviewed fixes and rerun affected scenarios on the new build. Retain exact-process action, screenshots, mod order, config hashes and cleanup evidence.
- [ ] 7.8 Complete the part playability/balance pass and coverage reconciliation; update only supported feature tasks and replacement states. Record later-composition obligations and accepted scope; do not close required blocked/unrun cases or use old-build evidence.

## 8. P5 — Animals, growing and environmental safety

- [ ] 8.1 Orchestrator: Freeze environmental and animal/growing contracts; publish environment before hunting integration; schedule pen/room wandering composition. Record requirement slices, leases, source references and scenario IDs in the part manifest.
- [ ] 8.2 Receive and inspect the **P5-A — Hunting, companions and slaughter** handoff for S064, S065, S066, S117, S118. Require actual provider milestones, focused tests, independent review and executable native scenarios; keep gameplay acceptance open.
- [ ] 8.3 Receive and inspect the **P5-B — Environmental safety, children and beds** handoff for S030, S031, S044, S088, S115. Require actual provider milestones, focused tests, independent review and executable native scenarios; keep gameplay acceptance open.
- [ ] 8.4 Receive and inspect the **P5-C — Plants, growing and terrain** handoff for S006, S043, S069, S076, S077, S087, S104, S105, S112. Require actual provider milestones, focused tests, independent review and executable native scenarios; keep gameplay acceptance open.
- [ ] 8.5 After all selected P5 chunks are ready, integrate and independently review the candidate, resolve findings, rerun affected checks and validate the exact package. Freeze runtime edits and record build identity plus the selected individual/combined scenarios from the roadmap.
- [ ] 8.6 Orchestrator: run the P5 isolated loader checks and native player workflows on that reviewed build; personally inspect causal results. Delegate product/scenario/tooling failures to their owners, integrate reviewed fixes and rerun affected scenarios on the new build. Retain exact-process action, screenshots, mod order, config hashes and cleanup evidence.
- [ ] 8.7 Complete the part playability/balance pass and coverage reconciliation; update only supported feature tasks and replacement states. Record later-composition obligations and accepted scope; do not close required blocked/unrun cases or use old-build evidence.

## 9. P6 — Colony behavior, combat and information

- [ ] 9.1 Orchestrator: Freeze current fact consumers and presentation registrations for the three independent colony/combat/information lanes. Record requirement slices, leases, source references and scenario IDs in the part manifest.
- [ ] 9.2 Receive and inspect the **P6-A — Mood, social behavior and autonomy** handoff for S001, S002, S037, S051, S068, S070, S092, S100, S119, S120. Require actual provider milestones, focused tests, independent review and executable native scenarios; keep gameplay acceptance open.
- [ ] 9.3 Receive and inspect the **P6-B — Combat, fire and classification** handoff for S072, S075, S079, S081, S090, S091. Require actual provider milestones, focused tests, independent review and executable native scenarios; keep gameplay acceptance open.
- [ ] 9.4 Receive and inspect the **P6-C — Power and world information** handoff for S052, S053, S074, S084, S093, S101, S102, S110, S113. Require actual provider milestones, focused tests, independent review and executable native scenarios; keep gameplay acceptance open.
- [ ] 9.5 After all selected P6 chunks are ready, integrate and independently review the candidate, resolve findings, rerun affected checks and validate the exact package. Freeze runtime edits and record build identity plus the selected individual/combined scenarios from the roadmap.
- [ ] 9.6 Orchestrator: run the P6 isolated loader checks and native player workflows on that reviewed build; personally inspect causal results. Delegate product/scenario/tooling failures to their owners, integrate reviewed fixes and rerun affected scenarios on the new build. Retain exact-process action, screenshots, mod order, config hashes and cleanup evidence.
- [ ] 9.7 Complete the part playability/balance pass and coverage reconciliation; update only supported feature tasks and replacement states. Record later-composition obligations and accepted scope; do not close required blocked/unrun cases or use old-build evidence.

## 10. P7 — Arrivals, transport and generation

- [ ] 10.1 Orchestrator: Freeze arrival-ready and transport-intent contracts before generation publication; investigate thread-affinity risks before dispatching dependent implementation. Record requirement slices, leases, source references and scenario IDs in the part manifest.
- [ ] 10.2 Receive and inspect the **P7-A — Arrivals, trading areas and vacuum protection** handoff for S004, S027, S039, S040, S083, S108, S109, S130. Require actual provider milestones, focused tests, independent review and executable native scenarios; keep gameplay acceptance open.
- [ ] 10.3 Receive and inspect the **P7-B — Caravans and transporter loading** handoff for S095, S116, S133, S135. Require actual provider milestones, focused tests, independent review and executable native scenarios; keep gameplay acceptance open.
- [ ] 10.4 Receive and inspect the **P7-C — Background generation** handoff for S078. Require actual provider milestones, focused tests, independent review and executable native scenarios; keep gameplay acceptance open.
- [ ] 10.5 After all selected P7 chunks are ready, integrate and independently review the candidate, resolve findings, rerun affected checks and validate the exact package. Freeze runtime edits and record build identity plus the selected individual/combined scenarios from the roadmap.
- [ ] 10.6 Orchestrator: run the P7 isolated loader checks and native player workflows on that reviewed build; personally inspect causal results. Delegate product/scenario/tooling failures to their owners, integrate reviewed fixes and rerun affected scenarios on the new build. Retain exact-process action, screenshots, mod order, config hashes and cleanup evidence.
- [ ] 10.7 Complete the part playability/balance pass and coverage reconciliation; update only supported feature tasks and replacement states. Record later-composition obligations and accepted scope; do not close required blocked/unrun cases or use old-build evidence.

## 11. P8 — Pawn inspection and accumulated integration

- [ ] 11.1 Orchestrator: Freeze final pawn presentation contracts and accumulated interaction coverage against all previous parts. Record requirement slices, leases, source references and scenario IDs in the part manifest.
- [ ] 11.2 Receive and inspect the **P8-A — Unified pawn inspection** handoff for S127, S128, S131, S132. Require actual provider milestones, focused tests, independent review and executable native scenarios; keep gameplay acceptance open.
- [ ] 11.3 After all selected P8 chunks are ready, integrate and independently review the candidate, resolve findings, rerun affected checks and validate the exact package. Freeze runtime edits and record build identity plus the selected individual/combined scenarios from the roadmap.
- [ ] 11.4 Orchestrator: run the P8 isolated loader checks and native player workflows on that reviewed build; personally inspect causal results. Delegate product/scenario/tooling failures to their owners, integrate reviewed fixes and rerun affected scenarios on the new build. Retain exact-process action, screenshots, mod order, config hashes and cleanup evidence.
- [ ] 11.5 Complete the part playability/balance pass and coverage reconciliation; update only supported feature tasks and replacement states. Record later-composition obligations and accepted scope; do not close required blocked/unrun cases or use old-build evidence.

## 12. Accumulated development completion

- [ ] 12.1 Audit all 137 allocations, foundation capabilities, target-spec records, provider suppressions, settings/languages and external contract claims against actual implementation and evidence. Keep omissions and deferred CE/multiplayer/import scope explicit; portal containers stay excluded.
- [ ] 12.2 Reconcile all later-composition obligations with the reviewed final candidate; run any still-missing affected native workflows and personally inspect them. Do not use this task to launch an unrelated full repository suite.
- [ ] 12.3 Complete independent accumulated integration review and the final semantic/playability review, resolve findings and repeat affected tests/package/native gates after fixes. Check every runtime revision's evidence and normal-configuration cleanup.
- [ ] 12.4 Publish the development completion record locally with exact supported scope, feature task/coverage status and remaining blockers. Leave release preparation, release regression matrix, presentation and Workshop publication to their separately authorized workflow.

## 13. Replanning record discipline

This section is a standing rule, not a one-time box to mark and forget. On each plan change append
evidence and old/new scope to the roadmap revision log; update designs, dependencies, task IDs,
assignment leases and scenario manifests; notify affected implementers; invalidate obsolete handoffs
and acceptance. The orchestrator may act on new implementation information within accepted intent.
Implementers report cross-boundary problems instead of broadening their own scope. Never remove a
user feature, loosen a test oracle or waive native acceptance merely to close a part.
