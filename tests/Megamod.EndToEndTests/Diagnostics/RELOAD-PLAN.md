# Native setting/reload workflow

Owner: `fumblesneeze.megamod`, S094. The authored scenario passed independent behavioral review;
its first native run timed out after the first real Hot reload Defs action. No acceptance checkbox
is closed. The initial selection/copy scenario now
delegates its unchanged geometry actions to the shared `NativeDiagnosticLogView`; it is affected
and must be rerun after review. Its clipboard helper and the initial XML fixture remain unchanged. The orchestrator
reported that exact workflow passed in grouped run
`20261107T221824752Z-f0469e9c06ba45738ebb7a1c4022b2cd`, child `smoke-001/20261107T221909510Z`;
its personal image/cleanup inspection and scoped disposition remain orchestrator-owned.

New scenario: `megamod.diagnostics.definition-toggle-reload-default`.

1. Retain the actual initial First/Second contextual warnings and MissingType error with references,
   text, traces and repeats. Require enabled context and disabled warning muting in the fresh profile.
2. Open the native mod-settings dialog, measure the actual layout and scroll through native track
   clicks as necessary. Click the global `diagnostics.definitionContext` checkbox off. Observe the
   checkbox and effective setting, unchanged save settings and unchanged historical records.
3. Discover the current immediate debug action with source `Verse.DebugActionsMisc`, category
   `General`, path `Actions\Hot reload Defs`, and invoke its exact registry handle through the new
   typed E2E step. Record bounded read-only native queue/current-event flags and source text key,
   capture the in-flight native view, then record those flags again. These observations do not
   request a Repaint or prove reload execution. The action queues native work; wait boundedly for two additional fractional
   warning occurrences and no current/queued long event. Do not call a loader directly.
4. Reacquire the log window and select the newly emitted plain warning. Observe no context and
   retain details screenshot. Sum repeats rather than require two rows: native adjacent identical
   warnings can combine. Historical contextual records stay unchanged.
5. Reopen settings, click the same global checkbox on, close it, rediscover the action and reload.
   Select and capture the newly emitted First and Second contexts. Previously plain records stay
   plain. This case does not touch the clipboard; the initial case owns Copy proof.
6. After the second reload, open Escape → Options through native controls and inspect one unrelated
   native error supplied by the separate late-options XML fixture below. Require no stale context.

Inspected 1.7.5000 facts: HotReloadDefs queues `doAsynchronously:false`, and ParseAndProcessXML calls
the converters sequentially. The completion callback closes all windows, followed by
`Log.ResetMessageCount`; reacquire every window and debug-action handle after reload. `Loaded` alone
does not identify reload completion. ErrorOnce uses a process-persistent used-key set; neither
ResetMessageCount nor Log.Clear clears it. The missing-type error remains one total across both
reloads. Ordinary `Parsed 37.5 as int.` warnings should total six occurrences across three loads.
No history clearing, once-key reset, direct logging or settings writes may manufacture the result.

The extra fixture `fumblesneeze.megamoddiagnosticslateoptions` is a separately selected XML-only package
defining one unofficial `Verse.OptionCategoryDef` with a unique defName and label. Installed
`RimWorld.Dialog_Options.PostOpen` logs `Unofficial OptionCategoryDef: <label> found, ignoring`;
the native drawing path ignores unofficial categories. Opening Options once after the enabled
reload therefore supplies a same-main-thread ordinary error after conversion. Give its manifest
that exact expected error count one. Existing intermediate accounting permits zero and final
accounting requires exactly one. Do not alter the successful initial fixture or its count-one
missing-type declaration. No fixture assembly or product Def is needed.

The E2E immediate debug-action catalog/driver is implemented and independently reviewed. The public
typed standalone query/invoke projection is separately root-owned. Existing mod-settings, native-window, developer-log, Escape-menu and
screenshot steps cover the other actions. Settings and menu geometry must be measured and checked;
capture and fail if the intended control cannot be established. Use bounded waits and scrolling,
fresh handles and explicit cleanup; normal execution restores the initial enabled option via its
checkbox. Each reload has a 90-second/6,000-frame wait and the complete scenario has a five-minute/
18,000-frame bound. Settings measurement is limited to 2,048 owned rows; each target has at most
three native track attempts. No direct settings, selection or scrolling writes establish proof.
Root added the project fixture item and owns reviewed build/deployment, exact-process run and final proof.

The first actual run, `20261107T231843476Z-6157ecc6c89c4396b2130e5eee70a71a`, reached the disabled
checkbox state and returned from the native action, then exhausted the unchanged 90-second wait.
The Gateway continued advancing frames; the final ordinary log retained only the initial two
warnings and one missing-type error. Exact-process/fixture cleanup completed. No post-action
long-event state or view was retained by that revision. The added checkpoints/capture are diagnostic
instrumentation, not a claimed fix: inspected synchronous long events await `alreadyDisplayed`,
which native drawing sets on Repaint. The existing screenshot path reads an end-of-frame capture;
it does not itself request a native Repaint. A renderer capability diagnosis remains separate.
No loader/cache, warning-count or time-bound assumption has been weakened.

The explicit non-Gateway order is Harmony, Core, megamod, the existing diagnostic fixture, then the
late-options fixture. The runner appends Gateway. Expected errors remain the existing MissingType
count one plus the exact late Options error count one. No dummy fixture project or new staging path
is introduced. XML-only fixture files and native scenario compilation are preflight, not loader proof.

This plan does not close legacy loading, exception/nesting/concurrency, original-provider coexistence,
seven rendered locales, capture/mute limits or remaining S035/S094 obligations.
