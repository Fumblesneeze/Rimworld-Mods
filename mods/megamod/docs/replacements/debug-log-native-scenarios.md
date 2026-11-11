# S035 warning-only native scenario handoff

Owner `fumblesneeze.megamod`. **Authored, not executed or accepted.** This narrow tranche covers
Mute warnings and protected startup output; it does not accept all S035 behavior. Root orchestrates
the integrated P0 gate using the reviewed package and an isolated minimized process.

## `megamod.diagnostics.warning-mute-reload` (pending registration)

Use the real [diagnostics XML fixture](../../../../tests/Fixtures/Megamod.Diagnostics/README.md).
Exact ordered packages: `brrainz.harmony`, `ludeon.rimworld`, `fumblesneeze.megamod`,
`fumblesneeze.megamoddiagnosticsfixture`, development Gateway. Start a disposable loaded colony
with developer mode on. Retain normal configuration hashes, process/build/fixture identity and
initial log file. The real fractional displayOrder loader warnings must first be confirmed;
do not substitute synthetic logger calls for these actual conversion results.

1. Open the native top log entry, select the actual First/Second fixture warnings and capture their
   visible details. Record native repeat counts and Copy all. Close via native controls.
2. Open megamod's native mod-settings page. Capture the Diagnostics / Log — Capture row with
   Mute warnings off, the effective ordinary-capture status, global-only explanation and disabled
   save controls. Click the global checkbox through the native widget path. Observe saved On plus
   effective discarded-warning status; close settings through native WriteSettings lifecycle.
3. Invoke the exact enabled `General / Hot reload Defs` native debug action
   (`Verse.DebugActionsMisc.HotReloadDefs`), not a direct loader call. Wait for the queued long event
   and ordinary player control. Retain the action receipt and before/after views.
4. Open the native log again. Select/copy retained fixture warnings: their earlier evidence and repeat
   counts remain, while this reload added no ordinary fixture warning. Compare the complete flushed
   isolated file at the corresponding action interval as supporting evidence. A successful action
   receipt or absence in JSON alone is not acceptance.
5. Return to settings, switch Mute warnings off, observe effective ordinary capture, close, and invoke
   the same native reload action again. Inspect actual newly accepted warnings/native repeat-count
   growth, full details and file output. Discarded muted occurrences never reappear.
6. Leave the requested setting at its initial value, close only owned windows, restore developer mode,
   selection/camera/speed and gracefully stop the exact disposable process. Retain cleanup evidence.

Native loading may recreate objects or combine repeated rows; compare occurrences and full selected
details, not just row count. The invalid-type fixture uses ErrorOnce and must not be reset/re-emitted
to manufacture a warning result. S094 context can stay enabled here; a separate off run proves that
muting does not require context hooks. Main-menu editing, saved process restart, global-only save
switching and seven-language rendering remain separate required checks.

## Focused isolated variants

- With `mlie.logafterdeferror` or `ordpus.logafterdeferror` genuinely active, verify its bounded
  S094 explanation, no owned context observers, and warning-mute action/result where the real shared
  route remains compatible. No installed-only simulation or foreign unpatching.
- With exact `bs.betterlog` active, preserve requested On but visibly report provider unavailability;
  owned warning muting stays off and original behavior remains intact. No compatibility claim before
  that separate supported native provider run.
- Start with an enabled target deliberately unavailable through a dev-only owned fixture variant,
  and Mute warnings enabled in disposable config. The protected startup warning must reach the native
  retained/file path, or the named owned fallback file if native file delivery is unavailable. Retain
  exact unavailable predicate and sink receipts. Do not invent a missing target on the product object.
- Observe native warning auto-open effects with the native preference enabled before muting and after
  unmuting. Muted ordinary warnings must not trigger them. Independent direct file writers stay outside
  the control. Once-only identity is supporting host evidence until an actual native Once workflow is run.

## Executability boundary

The registered initial XML preflight is only supporting evidence. NativeWindowClickActionStep is
available for screenshot-established widget coordinates, but the current E2E contract still lacks
native debug-action and log select/copy steps. Root owns those typed surfaces and central registration.
This file is an exact action authoring handoff, **not an executable/finished E2E test**. Until those
steps are composed and the acting orchestrator personally observes the reviewed action-to-result
views, these native obligations stay open. No direct setter, loader call or fake end state substitutes.
