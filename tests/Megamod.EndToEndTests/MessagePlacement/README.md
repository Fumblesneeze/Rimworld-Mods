# Manual message positioning native tracer

`megamod.messages.native-manual-reset-disable` was run and personally inspected by root on the
reviewed 1.7.5005 build on 2026-11-11. Exact identities and captures are in the local revision19 ledger.
It requires only Harmony, Core and megamod; Gateway is appended by the runner. Root owns native
execution and observation. No automatic clearance, source-created-message, preview or complete
interaction claim is made by this tracer.

Setup pins one disposable native message and sets global 10/10 offsets with inherited save values.
The native settings checkbox disables then reenables placement; the actual Reset button restores
both offsets to zero. Screenshots and native message/window rectangles connect each native action
to its rendered result. The message is not posted by a player action, so posting is explicitly a
precondition, not an accepted workflow. Setup keeps it alive beyond the bounded scenario and cleanup
removes only that message, closes only owned windows, restores the original globals/view/speed and
checks message removal and exact global restoration, attempting each action independently.

Deadline calculation: 11 screenshots × 30 seconds = 330 seconds. Up to 12 native window captures,
12 clicks (three scroll attempts plus one control action per phase), six pointer-scope actions and
two native console actions × 15 seconds = 480 seconds. The remaining 90 seconds of the 900-second
wall allowance cover three synchronous settings-open commands, three cancellations, arrangement,
checkpoints/assertions, iteration and rendering overhead with no individual provider deadline.
Cleanup has the harness's independent 30-second deadline. MaxFrames is int.MaxValue so the paused
UI test has no assumed frame-rate ceiling; MaxGameTicks remains independently bounded at 4800.
Use a launcher allowance covering 900 + 30 seconds plus explicit startup/host time.
# Inert preview continuation

`megamod.messages.native-preview-reset` starts with an inherited nonzero offset pair and empty native
live-message list. It opens actual settings, scrolls the actual track to expose the complete labeled
preview, captures it, clicks the native global Reset button, captures the changed preview, and closes
settings. It checks common effective values and unchanged live/archive identities as supporting
evidence; root must inspect the causal screenshots. No production message is posted by the fixture.
It accepts neither automatic geometry nor all preview/save/menu/locale behavior. Root owns dry-run,
build and execution. Root executed and inspected the English 1.7.5005 reset/close slice on 2026-11-11;
the revision19 integrated-verification ledger retains exact build/run identities and the first
screenshot-timeout failure before the passing retry. Cleanup restores prior globals, scroll and time speed,
and the runner owns native pointer scope cleanup.

## Startup-off admission

Run `megamod.messages.native-startup-off-enable` alone with the checked-in `PrelaunchDisabled`
settings XML staged by root beneath `artifacts/` and passed through `prelaunchConfigDirectory`.
The process must start with explicit placement off and 10/10 offsets; the test refuses an already
constructed runtime or a previously attempted admission. It checks native and owned fallback logs
for failed MSG-patch warnings, prepares one disposable native message, and observes its original
rectangle. The actual global checkbox enables placement; ordinary lifecycle admission and visible
movement are then observed without direct setup of those results. The native console is captured.
Cleanup restores exact globals, removes only the fixture message, restores scroll/speed and closes
only owned windows. Root executed and inspected a real RED before the startup guard and GREEN after
it on 2026-11-11; the revision19 integrated-verification ledger records the bounded accepted scope.
This does not accept posting or automatic geometry.
Its 600-second budget covers five screenshots, bounded capture/track-click actions, one checkbox,
the 30-second admission wait and native console observation; cleanup has its separate runner budget.
