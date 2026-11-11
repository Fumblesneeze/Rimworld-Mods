# Native tolerance persistence scenarios

Owner: `fumblesneeze.megamod`, S138. These scenarios are authored, not accepted, until the
orchestrator reviews the build and personally observes the retained action/result captures.
They use the exact ordered Harmony, Core, megamod list with the Gateway appended by the harness.
No original provider or fixture mod is required.

`megamod.drug-tolerance.native-copy-save` opens Assign and native Manage drug policies, selects
a disposable source, clicks the native Duplicate icon and changes the copy's Beer recreation
checkbox. It saves/reloads through the native command and reopens the copied row. Encoded numeric
0, numeric 1 and an unknown schema are arranged on the source before the action; exact restored
encodings and distinct row objects are supporting component assertions. The native Copy command
and serializer produce the copy/restored state. This does not accept tolerance editing or routine
enforcement, neither of which is enabled by the support group.

`megamod.drug-tolerance.save-failure-preserves-file` runs the same baseline, records the successful
save SHA256, then adds a temporary test-owned Harmony prefix to `Scribe_Values.Look<string>`.
Only a Saving call for the owned `megamodTolerance` field throws. The actual native Save menu and
the matching disposable file's Overwrite button trigger serialization. Native Scribe catches the
row exception; the product finalization guard must still abort, leave the existing file unchanged,
remove its `.new` file, and show the localized restart notice. A second native Overwrite must abort
without repeating the notice or owned diagnostic. The test patch is removed in guaranteed cleanup.
The product failure latch deliberately remains set: **run this ID alone in a fresh process**.

`megamod.drug-tolerance.late-load-failure` creates a successful native save/load baseline containing
one disposable policy, then injects an error only for the owned string during PostLoadInit. An
actual native Load-menu/file-row click reaches that later phase inside ScribeLoader.FinalizeLoading.
The first error is swallowed there, so the completion guard must reject the load. This scenario
requires the owned notice, native failed-load/nonplayable state and unchanged source-file SHA256.
It deliberately ends at native load recovery and runs alone in a fresh process. Cleanup removes
its unique saved file, closes exact captured windows and removes only its fixture patch. It does
not write through disposed Game/policy objects or reset the product latch.

The save failure scenario intentionally produces the exact fixture error, a native per-row serialization
error, a bounded `DT-POLICY-SCRIBE` warning and native SaveGame finalization errors. These are expected
fault evidence, not an excuse to waive other log failures. The root must inspect the notice and
console screenshots and the complete flushed log. The ordinary success scenario expects no errors.

Native view geometry is read-only and follows the inspected 1.7.5005 native windows. Existing
`NativeDrugPolicyView` supplies policy controls. Duplicate is immediately left of Delete in the
native manager. Save is the first available non-permadeath menu option. Overwrite is selected from
the file list by the exact disposable absolute path, with its actual scroll offset; a clipped row
fails rather than choosing another file. No direct click callback/window construction creates the
tested result. File hashes, raw choices and failure counts are diagnostic observations.

Cleanup binds the exact initial game or a single verified loaded replacement using saved source
policy and pawn markers, removes only disposable policies/pawn/save, closes owned windows, restores
speed and pointer state, and never resets the product failure latch. Native save/load restores the
camera from the unchanged saved game; these scenarios do not move it or change selections/dev mode.

Timeouts are bounded at 1,200 seconds for success and 1,800 for the fault scenario. The success path
has five screenshots at 30 seconds (150 seconds) and 24 native operations at 15 seconds (360
seconds: eight captures, six clicks, four tab actions, two log actions and four pointer actions).
The remaining 690 seconds cover native save/load, synchronous assertions/checkpoints, cancellations
and arrangement/iteration overhead. Within the total 1,800-second save-fault scenario, the fault
extension adds four screenshots (120 seconds), eight captures/seven clicks/ten pointer actions/three
log actions (420 seconds), two 90-second waits (180 seconds) and three menu actions (45 seconds).
The complete success-plus-fault fixed allowance is 1,275 seconds, leaving 525 seconds for native
save/load and synchronous overhead. Exact native IDs distinguish and acknowledge the SafeSaver
error box and the owned notice; type-only message-box selection/acceptance is not used.
The late-load scenario has a 900-second cap: three screenshots (90), three captures/three clicks/
four pointer actions (150), one 180-second wait and a menu action (15), leaving 465 seconds for the
initial save/load and other overhead. Cleanup retains its separate 30-second deadline. These are
harness allowances, not product timing promises.
`MaxGameTicks=4800` is a secondary guard while the colony
stays paused, and frame count is not treated as elapsed-time evidence. Root should retain actual
timing and lower bounds if execution evidence demonstrates a tighter reliable budget.

Integration requires `NativeDrugTolerancePersistence.Create(diagnostics.Write)`, admission of its
Early group and `Enable()` only after successful admission. Root's native lifecycle pump calls
`NativeDrugTolerancePersistence.ProcessPendingFailure()` after it reaches the main thread. The
ordinary enable setting does not gate this persistence support. The future editor/enforcement
group must depend on its `Available` result.
