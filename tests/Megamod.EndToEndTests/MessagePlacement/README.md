# Manual message positioning native tracer

`megamod.messages.native-manual-reset-disable` is authored for the reviewed integrated build.
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
