## 1. Major/minor game inputs
- [x] 1.1 Implement and test typed versioned DepotDownloader onboarding/acquisition with external-only paths and credential-free evidence.
- [x] 1.2 Download both major/minor branches, validate cache reuse and reject forbidden game pins.
## 2. Matrix build
- [x] 2.1 Test and implement explicit target declarations, isolated compilation and combined package validation.
- [x] 2.2 Generate version metadata and install the combined canonical package.
## 3. Acceptance
- [x] 3.1 Independent review, affected host checks, OpenSpec validation and clean builds.
- [x] 3.2 Exercise both target runners and retain exact input/build/workflow evidence.
- [x] 3.3 Publish the accepted combined package and verify the separate page update.

Both rolling major/minor caches and cached login were exercised. The accepted 1.6/1.7 bundle and separate page update are verified on Workshop item `3782589902`. Local ignored verification ledger: `artifacts/Releases/fumblesneeze.immersivechefs/acceptance/20261116-split-matrix/evidence.md`. The interrupted publisher-run cleanup exception remains explicitly open in `split-workshop-publication/tasks.md`.
