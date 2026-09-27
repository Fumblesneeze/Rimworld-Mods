## 1. mods/ThinWalls — Manual appearance controls

- [x] 1.1 Record focused RED/GREEN for exact shrink cycle, independent offset cycle and transform/default rules.
- [x] 1.2 Add persistent per-building state, independently configurable localized gizmos, and native cached/realtime geometry transforms with unchanged gameplay.
- [x] 1.3 Add focused native E2E for gizmo actions, neighbors/rotations/materials, compact U, settings, save/load and minification; inspect supporting geometry measurements.
- [x] 1.4 Resolve independent review, run affected tests, package and strict OpenSpec validation.
- [ ] 1.5 Personally inspect reviewed-build native action/result captures at representative zooms, classify logs, retain identities/cleanup, and complete semantic playability review.
- [x] 1.6 Regression: transform every attached realtime mesh within the building's draw scope, including direct Comp DrawMesh submissions, without scaling nested independent Things or applying the transform twice.
- [ ] 1.7 Replace generic appearance gizmo icons with native-texture compositions; focused mechanical RED/GREEN, independent code review, reviewed-build native action/captures at two UI scales, and blind correct-identity quality scores of at least 8/10 per icon.

Evidence: local artifacts/ThinWalls/2026-09-16-building-appearance/review-and-verification.md. Native
behavior, seven-locale UI and mechanical multipart/zoom checks passed on the reviewed build. Task 1.5
remains open because the context-free visual reviewer could not confidently identify the tiny casket's
precise function at distant zoom; no art/publication acceptance is claimed.

Icon evidence: local artifacts/ThinWalls/2026-09-16-gizmo-icons/review-and-verification.md. Reviewed
build native clicks, two UI scales, larger text, five focused host tests and package checks passed.
Both icons received blind visual-quality8/10 at both sizes. Task1.7 remains open: Offset's blind
semantic clarity was6/10 (move versus send/export), below the stricter specification's8/10 threshold.

## 2. mods/ThinWalls — Unified automatic offset

- [x] 2.1 Host RED/GREEN: perimeter-to-preset selection, opposing-axis cancellation, automatic/manual state and legacy-save interpretation.
- [x] 2.2 Host implementation: replace automatic rendering overrides with local lifecycle assignment to the existing appearance state; update tooltip contracts and remove obsolete code/tests.
- [x] 2.3 Independent review, affected host tests, local-only build, package and strict spec validation; retain evidence without deployment.
- [x] 2.4 After explicit user continuation, verify reviewed-build native placement and wall changes, U/corner rotations, multi-part rendering, override-to-center, hidden gizmos, save/load/reinstall and logs in a fresh minimized Gateway run.

The user-directed stop was respected during 2.1–2.3. The user's 2026-09-27 `continue` authorizes
isolated section 2.4 verification after confirming their game is closed.

Host evidence: `artifacts/ThinWalls/2026-09-27-unified-offset/tdd-ledger.md`. Final native evidence:
`artifacts/ThinWalls/2026-09-27-live-followup/verification.md`. All 16 contact masks, native Claim,
deconstruction, cancellation, manual/automatic persistence, multipart geometry and rotated compact Us
passed on the reviewed build with personally inspected screenshots/console and classified logs.
This closes section 2 only, not the separate publication/visual-identity/icon gates in section 1.
