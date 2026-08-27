## Context

`Invoke-GatewaySmoke.ps1` is the single isolated-launch backend reused by direct Gateway checks, leased MCP game runs, and each child of the grouped E2E runner. It already writes a minimal disposable `Prefs.xml` with background execution, deterministic window settings, and `volumeMusic=0`, but it omits RimWorld's `volumeMaster` setting. RimWorld therefore defaults the remaining sound channels to audible values.

The change crosses PowerShell launchers and the `RimWorldModding.Mcp` typed surface. It must keep the user's normal `Prefs.xml` untouched, preserve the existing exact-process/evidence lifecycle, and avoid adding a product-mod or Gateway runtime dependency.

## Goals / Non-Goals

**Goals:**

- Silence all RimWorld audio for every automated isolated launch by default.
- Provide one obvious opt-in for sound implementation or verification.
- Propagate the choice through both public MCP launch operations and their PowerShell backends.
- Make dry-run and completed evidence disclose the requested mode and effective RimWorld master volume.
- Verify both default and opt-in behavior through public launcher/MCP interfaces and a fresh live RimWorld process.

**Non-Goals:**

- Changing the user's normal RimWorld preferences.
- Muting Windows, the audio device, the RimWorld process through the operating system, or any other application.
- Making music audible during sound-effect work; `volumeMusic` remains zero.
- Adding a mutable runtime Gateway audio endpoint or changing packaged gameplay behavior.

## Decisions

### 1. Use RimWorld's isolated master-volume preference

The launcher writes `volumeMaster=0` into the disposable `Prefs.xml` by default. This is RimWorld's own master-volume setting and covers game, ambient, UI, and music output without changing operating-system mixer state.

The explicit opt-in writes `volumeMaster=1`. Writing the effective value in both modes is preferred over omitting the element during opt-in because the isolated launch must not depend on an engine default or the user's normal preferences. `volumeMusic=0` remains explicit in both modes so a sound-effect test is not masked by background music.

Rejected alternatives were setting every subordinate channel individually, which is easier to drift as RimWorld evolves, and muting the Windows process/session, which would not satisfy the requirement to use RimWorld settings.

### 2. Expose one positive opt-in through every launch layer

The direct launchers accept `-EnableAudio`; MCP `game_run_start` and `e2e_run_start` expose nullable `enableAudio` with omission equivalent to `false`. A positive opt-in makes exceptional sound work conspicuous and keeps existing callers silent without migration.

The grouped E2E runner records the mode in its plan and forwards the switch to each Gateway smoke child. The leased game-run planner forwards the same switch to its one smoke process. Other internal callers retain the default because new optional parameters default to `false`.

Rejected alternatives were a negatively named `muteAudio` parameter, which makes defaults and omission harder to read, and environment variables, which are undiscoverable in the typed MCP schema and can leak across runs.

### 3. Retain truthful evidence at the launcher and orchestration layers

Gateway smoke dry-run and completed results record `AudioEnabled`, `MasterVolume`, and the existing `MusicVolume`. Grouped E2E dry-run plans and aggregate/group results record `AudioEnabled`. MCP operation plans are covered by focused public-planner tests that assert omission and explicit propagation.

The live gate uses a fresh isolated Core-plus-Gateway process. The acting agent opens RimWorld's native Options surface and inspects the exact run to confirm the master slider is muted by default; a separate opt-in run confirms the slider is restored while music remains muted. Normal preference hashes and exact-process cleanup must pass for both runs.

## Risks / Trade-offs

- **[Risk] Sound tests silently run muted because a caller forgets the opt-in.** → The MCP schema and launcher help name `enableAudio`/`-EnableAudio`, and evidence reports the effective mode and value.
- **[Risk] The opt-in accidentally inherits an unknown value.** → The launcher always writes deterministic `volumeMaster` values (`0` or `1`).
- **[Risk] A child E2E launch drops the parent choice.** → Focused runner tests cover both dry-run command projection and real child argument construction.
- **[Risk] Existing internal release or verification callers change behavior unexpectedly.** → Optional parameters default to silent mode, matching the new repository-wide safety policy, while no call site is required to opt in.

## Migration Plan

Add the preference and evidence behavior first, then thread the optional flag through E2E and MCP orchestration. Existing callers require no changes and become silent automatically. Rollback removes the optional arguments and the `volumeMaster` element; no user data migration is needed because every affected `Prefs.xml` is disposable and the normal preference file is hash-protected.

## Open Questions

None.
