# Native original-provider off probe

Owner: `fumblesneeze.megamod`, S094 definition diagnostics and C02 settings. Revision 17.
Scenario: `megamod.diagnostics.original-definition-off` in `NativeOriginalDefinitionOffTests.cs`.
Status: **authored, unbuilt and unrun**. Independent review, the exact reviewed build and the
orchestrator's personal native observation are still required. No existing gameplay task closes.
Both files were absent before this authoring lease; existing helper, fixture, project, registry and
product files are unchanged by this lease.

The first authored source (`66E2CEFF9C42719D0EBCD4CF82173D1C51E3C2724271FB488B76E57E45A7BDF8`)
incorrectly assumed that every protected warning becomes a native retained row. This revision fixes
that scenario premise only; it does not change the reviewed runtime or manufacture a log occurrence.

## Exact launch and prerequisites

Non-Gateway package order is Harmony, Core, actual installed `mlie.logafterdeferror`, megamod,
`fumblesneeze.megamoddiagnosticsfixture`. The runner appends Gateway last. No late-options fixture,
Better Log or fake original is admitted. The existing E2E project already declares the normal XML
fixture and includes the new C# file through its ordinary compile glob; no project edit is needed.
Root owns metadata discovery, exact dry-run, build/install, prelaunch seed and isolated launch.

Use English and the installed Mlie 1.7 original identified in
`artifacts/Implementation/megamod-p0/revision17-original-off-native-plan.md` (Workshop 3550944748).
The orchestrator retains that actual provider's build identity with the run. This is a focused
identity-to-ordinary-off probe, not a source compatibility or coexistence certification.

Prelaunch must explicitly seed ordinary isolated mod settings:

```xml
<SettingsBlock>
  <ModSettings Class="Megamod.Settings.MegamodGlobalSettings">
    <values>
      <keys><li>diagnostics.definitionContext</li><li>log.muteWarnings</li></keys>
      <values><li>true</li><li>false</li></values>
    </values>
  </ModSettings>
</SettingsBlock>
```

The file is `Mod_fumblesneeze.megamod_MegamodMod.xml`, supplied through the existing typed
`prelaunchConfigDirectory` capability. The scenario never writes a seed or reads the user's normal
configuration. It checks the explicit `savedatafolder` argument against the live save-data root,
then reads only this bounded XML file in that root's Config folder. DTDs are prohibited and the read
is limited to 1 MiB. Semantic dictionary equality tolerates Scribe formatting differences.
The scenario also reads only the existing protected sink's exact owned fallback file when present,
as described below; it does not enumerate logs or consult the normal profile.
The initial ordinary XML fixture's MissingType ErrorOnce allowance remains **exactly one**; no new
allowlist entry or allowance for a foreign error is introduced.

## Actions and observations

1. Supporting reads retain actual ordered packages, requested context=true/effective context=false,
   original-off reason, unrelated requested/effective mute=false, global and unrelated save snapshots,
   exactly one startup warning, two fractional-warning occurrences and one MissingType occurrence.
2. Require exactly one actual matching original-provider warning across native retained warnings and
   the existing protected runtime's owned durable fallback file. If a native row exists, open the
   developer log, select its measured row and expand Trace big for a complete-details screenshot.
   Otherwise retain the actual bounded fallback record, owned absolute path, byte length and SHA256
   as warning-delivery evidence. Both routes must identify Mlie's original, S094, best-effort ordinary
   off behavior and retained preferences. Then close any existing native log before settings. No
   Copy action or direct warning emission is permitted.
3. Open native megamod settings, use a measured native scrollbar click if necessary and capture the
   **entire** S094 row: requested global checkbox on plus the effective-off explanation. The local
   row-measurement adaptation handles the new dynamic message block instead of assuming height 160.
   It reads row metadata/measurements; it never calls Draw, Reset or editor commit delegates.
4. Close with native cancel, exercising ordinary `WriteSettings`. Verify every serialized global and
   the unrelated save snapshot are unchanged. Both diagnostic options are GlobalOnly; no save
   override is invented. The settings workflow intentionally does not toggle away the requested true
   preference whose preservation is under test.
5. Rediscover the exact active `Actions\Hot reload Defs` / `Verse.DebugActionsMisc` native action,
   invoke it, capture before/in-flight/after views and wait for completion. No direct loader/logger,
   settings setter or snapshot publisher is called.
6. Account for native warning-row coalescing by identity and repeat-count **delta**. The reload adds
   exactly two occurrences across one or two changed records. Select each actual changed row in the
   native log and capture full Trace big details. None may contain the owned exact suffix
   `\n\n[Def Error] Current definition context\n`. Separate original `[Def Error]:` messages are allowed.
   Historical texts/traces remain unchanged; fractional repeats may increase, while the original
   ErrorOnce and startup-warning identities and count-one values remain unchanged. Earlier fallback
   bytes must remain an intact prefix of the current file, allowing unrelated diagnostics to append.
   A duplicate matching warning in either destination fails. This proves only
   the selected ordinary-off behavior, not arbitrary foreign logging behavior.
7. Retain final requested/effective and serialized values, exact occurrence counts and package order.
   The final selected-warning screenshot is the required actual native developer-console capture.
   Root must also inspect the flushed Player.log and classify all errors/warnings, not rely on the
   scenario's assertions or the expected fixture error alone. When fallback is used, root must retain
   and inspect the exact `ownedFallbackPath` alongside its checkpoint hash/text before run cleanup.

## Protected warning destination

`docs/Gateway.md` documents that the 1.7 launcher uses `-NoFileLog`, retaining stdout as Player.log and
stderr separately. `ProtectedNativeWarning.TryWrite` requires an enabled warning category, Waylith
file logging not disabled, a started root and no shutdown. Its category call bypasses ordinary
`Verse.Log.Warning` and native UI-opening behavior. Consequently the launcher flag makes the native
durability check fail even when ordinary fixture warnings still appear in the native console.

The current `ProtectedDiagnosticsRuntime` then calls its existing `StartupWarningFile`, whose unique
`Logs/Megamod-startup-<guid>.log` is opened CreateNew, held with FileShare.Read and flushed durably after
each append. Only if that fallback fails does the runtime use stderr. This scenario does not accept
a sink receipt, a successful call or stderr alone as proof of the required durable warning record.
It never constructs another sink and never calls Write/TryWrite or the logger to force a UI row.

The scenario reads the actual `MegamodMod.diagnostics` instance solely to obtain its exact
`FallbackFilePath`, construction diagnostic and delivery-error counters. It validates the file's
parent against this process's disposable Logs directory and its GUID filename, and reads at most
1 MiB through a read-only handle sharing the existing writer. UTF-8 decoding is strict; a changing
length during observation fails instead of accepting a torn snapshot. Checkpoints record the actual
warning text, destination, native/fallback counts, file identity/hash/size and `NoFileLog` argument.
Native selected-row inspection remains available if that real destination is present. Missing
delivery, duplicates, undelivered/recursive sink activity, changed warning text or removed history
fail in either branch. The file branch is supporting warning evidence; **the actual native settings
view/close and Def-reload console selections remain the player-workflow proof**.

Screenshots use the existing typed native screenshot step. Captured window handles are reacquired
after native view changes and the reload. Each row can receive **at most one** measured scrollbar
correction; no unbounded retries or direct scroll/selection setters act as the tested workflow.
If the complete row/details cannot be exposed by that bounded native interaction, the scenario fails
with retained views rather than declaring acceptance from state reads.

## Setup, cleanup and bounds

Direct setup only pauses the disposable game after `Playing`/`PlayerHasControl`, preserves its exact
Game/WindowStack/TickManager references, native log view state, requested global/save snapshots and
settings scroll. No pawn/item/map content, clipboard, developer mode, camera or selection is edited.
The existing native pointer-scope driver owns guaranteed pointer restoration, including failed steps.
Scenario cleanup restores captured view fields, auto-open request, settings scroll and speed; each
owned window and each restoration is attempted independently and errors are aggregated. It closes
only observed scenario-owned windows (including a native auto-opened log) and restores the prior log
window when necessary. A replacement Game/window stack/tick manager is refused before any cleanup
mutation. There is no save/load or ID-based ownership inference in this case. Interrupted writes stay
inside the runner's disposable profile; no preference setter is used in cleanup.

Worst-case sequential provider allowances, from current native drivers and the 1.7 screenshot branch:

| Operation | Maximum count | Provider cap | Subtotal |
| --- | ---: | ---: | ---: |
| Screenshots | 20 | 30 s | 600 s |
| Native window captures | 15 | 15 s | 225 s |
| Native pointer clicks | 13 | 15 s | 195 s |
| Native log open/close | 8 | 15 s | 120 s |
| Pointer scope begin/end | 8 | 15 s | 120 s |
| Explicit reload completion wait | 1 | 90 s | 90 s |
| **Bounded provider subtotal** | | | **1350 s** |

Counts allow three console inspections (startup plus at most two reload records), each with four
screenshots/four captures/four clicks/two log actions/two pointer actions, one settings inspection
with three screenshots/two captures/one click/two pointer actions, five outer screenshots, the
pre-settings log close and final log restore plus optional capture. The fallback-file branch omits
the startup console inspection, so its maximum is 16 screenshots, 11 captures, nine clicks, six log
actions and six pointer actions: 1050 s including the 90 s reload wait. Branches without a track
correction or with one coalesced reload row execute fewer steps. The full native branch still governs
the shared attribute; do not substitute the shorter observed branch for the worst-case budget.

The **1500 s** scenario budget adds **150 s** shared reserve for synchronous native settings-open,
native cancel/write, native debug-action dispatch/catalog lookup, assertions/bounded XML and fallback reads, screenshot
persistence and scheduling. These synchronous engine calls have no individually preemptive timing
guarantee; the reserve is an explicit allowance, not an assertion about engine speed. Frames use
`int.MaxValue` because no frame-rate ceiling is established. The independent 6000-game-tick guard
remains; the scenario pauses simulation and its reload wait has the same tick guard plus 90 s.
The runner's separate cleanup deadline is 30 s. Root must give the launcher at least the full 1500 s
scenario allowance plus startup, stage/dispatch and cleanup headroom; recommended exact single-case
launcher allowance is **2100 s**, with no promise that a stalled engine can be preempted in managed code.

Provider sources inspected: `GatewayEndToEndNativeWindowDriver` (15 s automation runs),
`GatewayEndToEndGatewayBackend` (1.7 screenshot capture 30 s, no legacy retry),
`GatewayEndToEndDebugActionDriver` and `GatewayEndToEndNativeStepDriver` (synchronous native commands),
`GatewayEndToEndExecutionStateMachine` (independent test/cleanup limits). This plan does not launch or
change those providers.

## Remaining scope

Independent review and exact native execution remain pending. Root owns personal inspection and
evidence linking the native controls to the retained requested preference and plain reloaded warning
details, with actual startup-warning delivery in its observed protected destination. This case does
not claim a native startup-warning row when the file fallback was used, already-off startup, inactive-original
startup, removal/restart restoration, Better Log, original aliases, legacy XML loading, Copy,
general inherited settings, or complete S094/C02 acceptance. Host tests and separate selected cases
cover their own obligations; no original API/facade/coexistence matrix is required.
