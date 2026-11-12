## Checkpoint and evidence boundaries

This change spans multiple implementation checkpoints. Native main-tab action components can be
committed before their central bootstrap, MCP projection and E2E composition. Those components
alone register no action in a running game. Checked tasks below refer to the explicitly identified
broader reviewed build/run, not to every intermediate source checkpoint. Use its package and test
assembly hashes to identify the tested composition; never reuse that evidence for changed runtime.

## 1. Exact provider and first native input slice

- [x] 1.1 Inspect current input-queue consumption, frame/GUI scale mapping, window handles and existing asynchronous operation/E2E lifetime. Record the exact provider contract and honest host-test environment.
- [x] 1.2 Add focused RED/GREEN for exact-dialog click admission, stale/replaced/obscured context, invalid coordinates and ownership cleanup. Implement the minimal shared input operation and typed discovery/dispatch without desktop fallback.
- [x] 1.3 Author a real native checkbox scenario with captures, disabled-control case, context replacement and cleanup; independently review the first slice and resolve findings.
- [x] 1.4 On the reviewed build personally verify actual native checkbox change in an exact fresh minimized1.7 process, retaining normal-config hashes, process/window identity, action-to-result captures and cleanup. Endpoint success alone cannot close this task.

## 2. Text, scroll and main-menu settings

Task1.1 evidence is the source-level phase/identity/GUI findings in design.md and ignored
artifacts/Implementation/gateway-native-window-input/{tdd-ledger,click-ledger,deferred-ledger}.md.
Host geometry, click phases, deferred lifecycle, retained-run API/CLI, native automation registration
and shutdown cleanup passed independent review. The first fresh minimized diagnostic observed native
GUI dispatch but no input-reset callback; discovery correctly refused input. The provider's reset
observation seam was corrected from that evidence; that diagnostic itself sent no input.
Executable E2E click integration and the `gateway.native-window-checkbox` scenario are authored.
E2E review found missing cleanup-receipt and timeout-evidence cases; focused corrections passed
27 affected host tests and independent recheck. The exact checkbox scenario also passed build and
dry-run discovery without launching a game. Native reset admission now rejects unproven downstream
inner patches; its independent recheck passed. The first checkbox run reached the probe but failed
at capture before input. Inspection found that ordinary window ID0 was incorrectly excluded; the
correction and capture-failure evidence retention passed focused tests and independent review.
The separate launcher failure-evidence correction passed review. The next fresh run,
`20261107T165454816Z`, passed: root personally inspected three exact-process screenshots showing
enabled unchecked → checked once, with the disabled checkbox unchanged after its click. Exact
process/window/build, input receipts, configuration hashes and completed cleanup are retained in
the local root-registration ledger. Task1.4 has this basic causal evidence; native console inspection
was not included and remains before final tooling acceptance. Native context-change/cancellation,
alternate-scale and menu/text/scroll scenarios remain open. No product feature is accepted by it.

- [ ] 2.1 Use vertical RED/GREEN to add native key/text and scroll phases with focus, text/surrogate bounds, neutral-input admission, frame transitions, cancellation and owned release. Reuse the first operation's lifecycle rather than adding another queue.
- [ ] 2.2 Add focused RED/GREEN for settled-main-menu mod-settings navigation while preserving exact-mod selection and loaded-game behavior. Resolve runner lifetime/menu-transition requirements through supported native commands.
  - Entry prerequisite is implemented as observed native Options and DevQuickTest capture/click on the existing input lane. The independently reviewed correction retains captures provisionally through enclosing dispatch completion and preserves failed capture evidence. All 129 affected Unit and six Harmony checks pass; owning builds, strict validation and exact discovery pass. Reviewed native group `20261112T163926526Z-72135e429f00492c9b38908aa5cdfbad` passed32 steps: root personally inspected all seven captures showing native Quit to Entry, Options opening/closing, and DevQuickTest creating a different playable colony. The complete logs were classified, including a recovered shutdown retry; graceful exit, unchanged normal-config hashes and owned stage/credential cleanup passed. The exact lease, TDD and native ledgers are local under `artifacts/Implementation/gateway-entry-menu/`. This accepts the Entry prerequisite only; main-menu product settings remain a separate workflow. Authored `gateway.native-entry-menu` must run singular because it is process-owned. The two affected ordinary window scenarios have counted 360s/600s budgets and no assumed UI refresh-rate ceiling.
- [ ] 2.3 Author native text selection/editing, rejected-value recovery, scrolling and main-menu-disabled-control scenarios. Record actual player actions, required preconditions, captures and cleanup; keep product setting setters out of actions.
- [x] 2.4 Add the native developer-log toolbar action to shared typed automation/E2E control, with exact callable admission, context/argument/idempotence/outcome tests, independent review and personally inspected native console capture. Keep diagnostics and developer mode unchanged.
- [x] 2.5 Review and run `gateway.native-window-float-menu` to distinguish button activation from native popup closure; retain pointer/window lifecycle evidence and a screenshot before the terminal assertion. Reproduce actual RED before changing pointer lifetime.
- [x] 2.6 If the native reproduction confirms the pointer-restoration mismatch, implement and independently review explicit workflow pointer ownership on the existing lane, preserving standalone version1 behavior and honest failure/cancellation/expiry/shutdown restoration. Prove a real popup remains available for follow-up input and workflow cleanup restores its original pointer on the reviewed build.

Task2.5 evidence: root's reviewed run `20261107T190444973Z-b2380515117242448fdb7da433120f3f`
retained button activation1, exact menu ID1 opening at frame979/pointer577,252, then native closure
in that same frame with restored pointer51,560 and zero selections. Root personally inspected both
causal screenshots. Task2.6 implementation has focused host coverage and a scoped native probe, but
independent review and reviewed native GREEN remain open; host success does not accept popup behavior.

Tasks1.4/2.4 now have current reviewed native evidence from
`artifacts/EndToEndRuns/Grouped/20261107T181928426Z-52180318cc514846b59391553fcc98ab/`.
Root personally inspected enabled unchecked→checked once, unchanged disabled checkbox after its
click, and the actual developer log with its retained entries. The native command then closed it;
the process exited gracefully, both normal config hash pairs stayed unchanged, and stage/credential
cleanup completed. Full log warnings were classified in the local root ledger. This proves these
scoped workflows only; replacement-context, cancellation, alternate-scale and text/menu/scroll gates
remain distinct. No megamod product behavior is accepted by a Gateway-only run.

Tasks1.2/1.3 also retain the reviewed click-core, deferred lifecycle, provider, typed driver and
final60-test provider evidence in the local ledgers. The first native replacement run failed a
fixture's positive-control-ID assumption. Exact source showed valid negative named-control IDs;
the reviewed fixture correction now requires nonzero ID and exact replacement ownership, with
one-shot setup and passive retained observations. Run
`20261107T182434919Z-844e07c2a933480ab865de56501440b2` passed both press/release replacement paths,
preserved native control `-2147483648`, and then performed fresh native clicks. Root inspected all
seven captures and the native console. This closes the first click slice, not section3's wider gate.

- [x] 2.7 Implement the native bottom-tab desired-state action through the existing registry, typed MCP/CLI and E2E driver. Use focused vertical RED/GREEN for exact target selection, satisfied requests, wrong-tab close, visibility/disabled/modal restrictions, cancellation, changed context, command failure and truthful outcome evidence. Root owns shared project/bootstrap registration unless explicitly leased.
- [x] 2.8 Independently review the tab action and use it in the exact S029 preset fixture workflow: observe Assign opening, then click Manage drug policies and operate native controls. Retain native screenshots/console/cleanup; host checks alone cannot close this task.

Tasks 2.7/2.8 evidence: independently reviewed runtime/driver 32 focused checks and MCP projection
11 checks, followed by group `20261108T101457394Z-77b75d40df7043a793a9594469afc0a5` with 43
passed native steps. Root inspected the Assign opening, policy management/edit/assignment and
restoration, and native developer console in ten captures. Exact process/configuration/package and
cleanup audit passed. Local ledgers: `artifacts/Implementation/megamod-p0/main-tab-projection/` and
`artifacts/Implementation/megamod-p0/drug-native-preflight/verification.md`. This positive native
use does not close section 3's adverse/context/scale matrix or all product feature acceptance.

## 3. Integrated tooling acceptance

Task2.6 has scoped reviewed native evidence in
`artifacts/EndToEndRuns/Grouped/20261107T194544522Z-37b8743c67f04c349620e3ab1064c991/`.
Root inspected all five causal captures: ordinary menu opens and persists, first option is selected
once through native input, and pointer returns to its original position before native log inspection.
Normal configuration hashes match, graceful shutdown and all cleanup completed. Independent provider,
shared-step/bootstrap and runner review corrections and focused RED/GREEN are retained in the local
pointer ledgers. This positive popup workflow does not close section3's wider native matrix or accept
product behavior; adverse pointer ownership/reentry cases currently have host evidence only.

- [ ] 3.1 Independently review final Gateway/shared/MCP wiring and affected native scenarios, resolve findings, rerun only affected tests/builds and validate OpenSpec/package boundaries.
- [ ] 3.2 Personally exercise and inspect the reviewed operation through real native dialogs in fresh minimized1.7 runs, including cancellation/partial completion, context replacement, distinct supported UI scales and main-menu restrictions. Retain exact-build evidence and cleanup under root AGENTS.
- [ ] 3.3 Document the typed operation, supported runtime/limits, retained evidence and remaining limitations. Reuse it in the megamod P0 scenarios without claiming those product workflows accepted by this tooling run.

### 1.7.5005 projection correction — 2026-11-11

The native mouse origin changed to height-minus-one-minus-y. The correction admits either inspected
projection, applies its exact inverse, and retains bounded input-phase diagnostics. Independent review
found no actionable issue; 89 focused host checks passed. On the reviewed build, native checkbox run
`20261111T153957927Z-d4d062a74ed94ff99f1257d9a921b50a` passed. Root personally inspected all four
captures: initial unchecked controls, one native change, enabled checked with disabled unchanged, and
the native console. Normal ModsConfig/Prefs hashes were unchanged and owned cleanup completed.
Evidence: `artifacts/Implementation/megamod-p0/revision19-gateway-verification.md`.
This renews only the basic click/console scope for 1.7.5005; wider section3 gates remain open.

### Focused text prerequisite — 2026-11-11

- [x] Implement exact focused-control capture and bounded native-window.replace-text through native queues and post-layout validation, with owned cleanup and focused host RED/GREEN.
- [x] Expose the same action through existing typed automation and E2E deferred lifecycle; author native numeric/invalid/name and stale-focus scenarios.
- [x] Independently review the frozen provider/contract/step changes and personally verify the reviewed minimized native scenario before accepting text delivery.

Root verified the scoped text workflow on 2026-11-12 local time: reviewed checkpoint2f076a0,
group20261111T232206299Z-54797f07e99c4115b81195499cbf2f8c,38steps and six personally inspected
causal captures. Native invalid/valid Apply, empty deletion, accented name input, stale-focus
refusal and fresh-focus recovery were observed. Graceful exact-process cleanup, staging/credential
cleanup and unchanged normal configuration hashes completed. Exact build, warning classification,
actions and limitations are retained in
`artifacts/Implementation/gateway-native-text-input/native-verification.md`.
This closes only these focused text prerequisites; section3's broader native matrix remains open.

### Native caravan prerequisite — 2026-11-12

- [ ] 4.1 Implement the reviewed native World view lease and exact same-Game/World camera/view restoration with focused vertical RED/GREEN and retained cancellation/partial-cleanup evidence.
- [ ] 4.2 Implement exact caravan capture/selection and tutorial-aware native Items/Health commands; preserve input restrictions and stale object/scene refusal.
- [ ] 4.3 Add the narrow observed native Health-row target on the existing minimized input lane with complete layout provenance, transient-window identity, stale rejection and owned cleanup tests.
- [ ] 4.4 Expose the same named automations through existing gateway_mutation and host-safe E2E steps; author the singular Gateway native scenario with repeated-same-caravan detail retention and counted budgets.
- [ ] 4.5 Independently review the frozen compiled slice, resolve findings, and run the reviewed exact Gateway scenario in a fresh minimized process with root's personal causal screenshot inspection and complete cleanup evidence.

The reviewed artifact plan is `artifacts/Implementation/gateway-world-controls/plan.md`, SHA256
`09934902E20EA4A5CD694738D275604D8DBBF8330296FBD10DED69012C8C9DC4`.
The implemented Gateway sequence has46 steps and885s allowance under900s, including explicit
final native detail closure; the reviewed proposal had840s. The separate future S138 consumer
allowance is1365s under1500s. Implementation, review and live acceptance remain unaccepted here. Product
runtime and its ingestion fixture are outside this Gateway implementation lease; the tooling
scenario cannot close the product's normal-world-tick behavior.

The five accepted implementation-review findings are being corrected within4.1–4.5: unlimited
total owned camera-history length with bounded per-advance draining; foreign-world-activity
admission before restoration;15s input/10s view cleanup shares within the existing30s aggregate;
individual known-clipped-row exclusion; and completed exact detail-contents callback proof before
publication. Focused production-lifecycle RED/GREEN and revised source hashes support re-review.
All five tasks remain unchecked until the reviewed native acceptance evidence exists.
