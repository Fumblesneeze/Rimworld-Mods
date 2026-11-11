# Native definition-diagnostics scenarios

Owner: `fumblesneeze.megamod`, S094 with the shared S035 reporting channel. These are isolated
game scenarios. They must not run in Unit, Harmony or constructed-Defs host environments.

`megamod.diagnostics.initial-xml-preflight` is supporting loader evidence only. It reads messages
already emitted by the real XML loader; it does not select, copy or prove player-visible behavior.

`megamod.diagnostics.definition-initial-legacy` is authored and unrun. It declares native legacy
loading before process startup and delegates to the same stateful native selection/details/Copy
workflow as `megamod.diagnostics.definition-initial-default`. Both verify the actual mode before
Arrange changes view or speed state and record requested/observed mode with the initial evidence.
The fixture, error accounting, native log geometry and clipboard helper are unchanged. See
[LEGACY-PLAN.md](LEGACY-PLAN.md) for exact selection and pending verification. This composition
requires a fresh reviewed default run as well as the separate legacy run; the current default Copy
obligation remains open. No richer clipboard format is cleared or silently skipped to obtain a pass.

`megamod.diagnostics.definition-toggle-reload-default` is the next authored workflow. It clicks the
real global context checkbox off/on around two native Hot reload Defs actions, selects the newly
emitted plain/contextual warnings, preserves the original ErrorOnce, and opens native Escape →
Options for an unrelated late diagnostic. Its separately selected XML-only late-options fixture,
occurrence accounting, geometry and limits are documented in [RELOAD-PLAN.md](RELOAD-PLAN.md).
The case has passed a reviewed163-step native run on the revised converter family, with root's
personal image inspection recorded in artifacts/Implementation/megamod-p0/native-reload-family-reviewed.md.
This establishes only the selected default-loader tracer, not all diagnostics scenarios. It uses the shared
`NativeDiagnosticLogView` extracted from the initial case; the initial case's action sequence is
unchanged but its affected helper path requires a fresh reviewed native rerun. The initial fixture
and reviewed clipboard helper are unchanged. The reload case never touches the clipboard.

`megamod.diagnostics.definition-initial-default` is the first authored native player workflow:

1. Record the real startup messages before interaction. Require two distinct Warning records whose
   original text is `Parsed 37.5 as int.` and the fixture's actual missing-type Error record. Require
   one complete definition/package/file context on each record, with native repeats and severity.
2. Record whether the ordinary developer log is already open, then open it through
   `NativeDeveloperLogActionStep` (an existing automatically opened log is retained). Capture the
   exact window handle and a screenshot before using coordinates; do not create a duplicate window.
3. Use the native Trace small button, observe the current filters/scroll and measure the real row
   from the current retained list. Retain geometry and a screenshot before any visibility rejection.
   For an offscreen row, use at most three measured outside-thumb clicks on the actual native
   scrollbar track, observing real vertical movement and recapturing the row after each click.
   Click each First/Second/MissingType row with `NativeWindowClickActionStep`, then verify that the
   native selected-message reference is that exact retained record. No selection field is assigned
   to produce a test result.
4. Click native Trace big, capture its details and verify the same selected record, unchanged full
   text/severity/available trace, and enough pane height to show the whole context. The orchestrator
   must personally inspect these screenshots; private-field observation is only corroboration.
5. Click native Copy to clipboard. Native Copy copies **all retained messages**, not only the selected
   row. Immediately before this action require an empty or plain-Unicode-text-only Windows clipboard:
   enumerate at most the four text/locale formats (including Windows' synthesized text aliases),
   reject every image/file/rich/unknown format without reading its payload, and check sequence stability
   around a success-reporting Unicode read while the clipboard inspection lock is held. Recheck that
   snapshot immediately before yielding the native click. Require a successful `GetClipboardData` /
   `GlobalLock` read with a terminator within the allocation and 262,144-character limit, then require the
   retained snapshot and all three complete diagnostic texts/traces, and retain that copied output.
   The prior clipboard is held only in memory, never logged or checkpointed.
6. Restore the prior clipboard only if its current value still exactly matches the expected owned
   copy, with a fresh supported-format observation. Preserve a later external edit, including rich
   formats whose text happens to equal the owned copy. Require private readback of the restored text
   or truly empty format inventory before reporting restoration and releasing ownership. A setter or
   readback failure leaves restoration pending for deferred cleanup; a changed ambiguous value after
   that failure remains untouched and cleanup is reported incomplete. Empty-state restoration uses
   `EmptyClipboard` only while holding the native lock and verifying the observed sequence.
   Release the native pointer scope and restore the log's prior open state.
   Deferred cleanup also restores the disposable view's prior private selection/scroll/pane values
   and time speed. Those cleanup writes are not execution or acceptance inputs.

The initial preflight (`megamod.diagnostics.initial-xml-preflight`) and native initial case
variants (`megamod.diagnostics.definition-initial-default` and
`megamod.diagnostics.definition-initial-legacy`) declare Harmony, Core, megamod, then
`fumblesneeze.megamoddiagnosticsfixture`. The reload case
(`megamod.diagnostics.definition-toggle-reload-default`) appends
`fumblesneeze.megamoddiagnosticslateoptions` to that list. Project-owned XML fixture items and the typed
selected-fixture staging pipeline supply the corresponding `tests/Fixtures/` packages; no product content,
new dummy assembly, installation script or Workshop mutation is required. The Gateway is appended
by the isolated runner. Do not invoke the scenario against an ordinary user game.

The coordinate contract comes from the installed 1.7.5000 `LudeonTK.EditWindow_Log`, `EditWindow`,
`DevGUI` and `Verse.Window` implementations, inspected read-only with the repository's ilspycmd:
EditWindow margin 8; titled content origin `(window.X+8, window.Y+41)`; content size
`(window.Width-16, window.Height-49)`; toolbar height 26; row count column 28; scrollbar reservation
16; row height is the native Tiny-font `CalcHeight` capped by its native 30/line-height rule.
Toolbar button widths use the current native font measurement plus 10, with a 4-unit gap. Rendered
click coordinates multiply the captured GUI coordinates by its current UI scale. This is inspected
game-version-specific scenario geometry, not a new product UI adapter or a general layout API.

The inspected 1.7 `UnityStubs.GUI.BeginScrollViewImpl/HandleScrollbars` uses a 16-unit vertical track
with no arrow buttons. Its thumb is `max(20, viewportHeight²/viewHeight)`; clicking outside the thumb
centers it at that track position. The scenario requires that provider/callable shape, checks the
rendered `listingViewHeight` against measured rows and includes the native extra 100 units in the
scrollable height. It selects only a track point outside the current thumb predicted to expose the
entire row, then checks actual scroll movement. Changed layout, filtered/missing records, no safe
track point or three unsuccessful attempts stop with preceding screenshots/geometry. The minimum
thumb size can make a sufficiently precise click unavailable for very large retained histories;
this case then needs a separately supported native drag/scroll action.

Initial constraints are explicit: the three rows must become wholly visible after Trace small and
the bounded native track actions;
no category filter is changed; the Copy
button must fit the toolbar; each context must fit Trace big; the Windows clipboard must satisfy the
empty/text-only precondition and remain stable during its checks; both prior clipboard and copied
output must fit the bound. A prior clipboard already identical to the expected output cannot demonstrate
the copy's changed result, so that run stops before copying. Clipboard text observation uses the
native allocation size and copies at most 262,145 UTF-16 code units (the text limit plus its terminator)
while holding the inspection lock. Missing data, failed locks, missing bounded terminators and changed
sequences fail closed. A genuinely empty format inventory yields verified empty state without calling
any text getter; a failed GLFW text read cannot masquerade as valid empty Unicode text.
This does not provide an OS clipboard reservation across the asynchronous native input operation;
the format/sequence guard is a precondition immediately before yielding it. An external clipboard
edit during native input is outside that snapshot, so the isolated scenario should not be run while
another task is using the clipboard. No unsupported payload is backed up or declared restored.
The narrow Windows inspection follows Microsoft's
[clipboard format enumeration](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-enumclipboardformats)
and [synthesized text formats](https://learn.microsoft.com/en-us/windows/win32/dataxchg/clipboard-formats),
with [clipboard-owned handle lifetime](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-getclipboarddata)
and [successful final unlock semantics](https://learn.microsoft.com/en-us/windows/win32/api/winbase/nf-winbase-globalunlock).

If the live viewport/scroll violates a constraint after the bounded native track actions, inspect the preceding screenshot/checkpoint and
add the narrow native scrolling/viewport capability needed by that observed case. Do not clear logs,
set scroll/selection during execution, increase the window through direct state assignment or choose
unverified row coordinates merely to make the scenario pass.

**Status:** native geometry and the final clipboard helper passed independent review. After the
reviewed Error-overload correction, run `20261107T220657360Z-f54092e5c6cf4c9da37937299acdf225`
passed all66 native steps and scenario cleanup; root personally inspected all12 captures. Initial
loader context, native row selection/scrolling, complete details and Copy were observed on that build.
The outer run failed because its intermediate log reader collided with the open output writer;
post-shutdown exact expected-error accounting passed. The sharing/tail correction passed focused
host tests and independent review. Fresh run `20261107T221824752Z-f0469e9c06ba45738ebb7a1c4022b2cd`
then passed the complete runner, all66 steps and cleanup; root personally inspected its own12
captures and full flushed diagnostic output. Exact identities, actions, observations and cleanup are recorded in local
`artifacts/Implementation/megamod-p0/early-diagnostics-composition.md`; no complete feature is accepted.
Those initial Copy runs predate the revised converter family and current shared scenario. They do
not close either fresh initial-case obligation; a clipboard-preflight refusal also leaves Copy open.

Root owns central diagnostics settings/runtime composition and subsequent reviewed build/launch.
This case corroborates the three initial native records against ordinary file output. It does not
prove the legacy loader, hot reload, option changes, general file-output parity,
mute/filter/capture limits, once-only reload behavior, provider coexistence, seven rendered locales,
or the remaining S035/S094 tasks. Final acceptance needs the exact reviewed build, selected fixture
identities, native action receipts, personally inspected screenshots, copied text, corresponding
ordinary Player.log evidence and complete exact-process/fixture cleanup.
