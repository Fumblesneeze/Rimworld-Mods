## 1. Gateway launcher policy

- [x] 1.1 Capture focused RED evidence that a default Gateway smoke dry-run does not write or report RimWorld master-volume muting, then implement `volumeMaster=0` plus truthful audio evidence and rerun GREEN.
- [x] 1.2 Capture focused RED evidence for the missing explicit audio opt-in, then implement `-EnableAudio` with deterministic `volumeMaster=1`, retained `volumeMusic=0`, and truthful dry/completed evidence and rerun GREEN.

## 2. Orchestration propagation

- [x] 2.1 Add focused RED/GREEN coverage proving the grouped E2E runner records its audio mode and forwards `-EnableAudio` to every child only when requested.
- [x] 2.2 Add focused RED/GREEN coverage proving MCP `game_run_start` and `e2e_run_start` expose `enableAudio`, default it to false, and propagate an explicit true value through their public operation plans.

## 3. Contract and verification

- [x] 3.1 Update launcher, MCP, Gateway, development, and E2E documentation for default muting, the explicit opt-in, and evidence fields; run strict typed OpenSpec validation.
- [x] 3.2 Run the affected focused host tests and zero-warning owning Release builds, then perform a scoped code review and resolve every finding.
- [x] 3.3 On the reviewed build, run fresh isolated default-muted and audio-enabled Core-plus-Gateway processes, use RimWorld's native Options workflow to observe the effective master/music settings, inspect exact-run screenshots/logs, and retain exact-process/config-hash/cleanup evidence as required by root `AGENTS.md`.
