# Initial preset native controls

Owner fumblesneeze.megamod; S029 task2.5.3. **Draft scenario, not compiled or executed.** Root owns
shared-step availability, fixture/project registration, review, canonical deployment and live runs.

Exact test: `megamod.drug-policy.initial-preset-native-controls`.
Ordered packages excluding appended Gateway: Harmony, Core, megamod,
`fumblesneeze.megamoddrugpresetsfixture`. Use a fresh default English profile and the default native
XML loader. The fixture declares no expected errors. No clipboard is read or changed.

Initial scenario budget: 10 screenshots at the existing 30-second allowance plus 22 native
operations at 15 seconds (8 clicks, 8 captures, 2 tab actions, 2 log actions and 2 pointer-scope
actions) total 630 seconds. The 660-second overall cap reserves another 30 seconds for its seven
assertions, three checkpoints, one synchronous cancellation, arrangement and persistence/iteration
overhead. These are scenario allowances, not engine timing guarantees. `MaxFrames = int.MaxValue`
leaves wall time as the governing deadline rather than assuming a display refresh rate. The paused
workflow retains the separate 3600-tick limit; cleanup has its own 30-second deadline.

The initial native Game constructor consumes the fixture before Arrange. Steel is a real Core
non-drug first entry, followed by distinct Beer and Smokeleaf choices; Yayo checks the independent
broad pleasure permission. Arrange only reads that already-created policy and creates a disposable
colonist for a later native assignment. It does not construct, copy, repair, replace or seed policy
data. Native New/Duplicate controls would exercise MakeNewDrugPolicy instead of the preset factory
and are deliberately absent from this test.

The workflow opens Assign through the proposed `NativeMainTabActionStep(name,"Assign",open)`,
clicks the native Manage drug policies column header, selects the constructor-created preset,
visually exposes its valid subsequent choices, toggles/restores Beer recreation using the actual
checkbox, then assigns/restores the policy on the generated colonist using native dropdown options.
Screenshots bracket selection, checkbox changes and assignment. Supporting reads retain policy
identities/order/default status and all explicit choices, without replacing player-action evidence.

`NativeDrugPolicyView` reads the actual rendered table/window fields and computes only exact inspected
native control rectangles. It refuses dirty, scrolled, grouped, multi-column-menu or clipped layouts
outside this initial scenario; it does not write scroll state, invoke draw/action callbacks or
substitute direct assignments when a control is inaccessible. Required initial dimensions must fit
the five-policy list and Core drug rows. A failure retains evidence for a later native scroll action.

Successful execution restores the edited permission and pawn assignment through native controls,
closes the tab and releases the pointer scope. Independent deferred cleanup closes only retained
scenario windows, restores the disposable pawn/permission if interrupted, destroys only the generated
pawn and restores speed. Those cleanup writes are never claimed as action evidence. The runner owns
the isolated profile, ordered package staging, screenshots, logs, process and configuration hashes.

Exact read-only research: installed1.7.5000rev1275 Assembly-CSharp SHA256
`D55AA8E47FA3C5463CA85B0ECCD51F195B4BBCD04F56DA3AF21C2F6688A81254`;
Core `DrugPolicyDefs/DrugPolicyDefs.xml`, Beer/Smokeleaf/Yayo drug XML; retained factory/policy/dialog
sources under `artifacts/Megamod/P0-C/native-1.7.5000/`; additional UI contracts under
`artifacts/Megamod/P0-C/native-drug-preset-scenario/native/` (Assign/PawnTable, generic policy dialog,
DrugPolicyUIUtility, FloatMenu/FloatMenuOption and Widgets). No upstream mod code is copied.

Remaining before native execution: root supplies the reviewed shared main-tab action, registers
`../Fixtures/Megamod.DrugPresets/fixture.json`, compiles and dry-runs this exact group, independently
reviews scenario/product composition, then performs and personally inspects the native workflow.
No host TDD or gameplay acceptance is claimed by authoring a scenario. Full repair, null storage,
alphabetical presentation, saves/content changes, original-provider suppression and legacy-loader
coverage remain separate unaccepted obligations.

## Preset save/reload controls

Exact test: `megamod.drug-policy.preset-save-reload-controls`. **Authored, not built or executed.**
This separate S029 scenario covers the existing preset-construction-before-save-choices and
customized-preset preservation requirements, plus the unchanged-catalog portion of ordinary policy
persistence. It does not complete general reconciliation, content changes, dose-history behavior,
original-provider suppression or the broader save-continuity tasks.

Use the same exact package order, unchanged XML fixture, English/default-loader profile and empty
expected-error list as the initial scenario. Its cap is 900 seconds and 4800 game ticks, with
`MaxFrames = int.MaxValue` so a machine-dependent frame rate cannot preempt the wall allowance.
The actual sequential allowances are 10 screenshots at 30 seconds (300 seconds), plus 32 native
operations at 15 seconds (480 seconds: 10 clicks, 12 captures, 4 tab actions, 2 log actions and
4 pointer-scope actions). The remaining 120 seconds cover native save/load, eight synchronous
assertions, two checkpoints, two cancellations, arrangement and persistence/iteration overhead.
Save/load has no independent step cap; this is an explicit shared scenario allowance, not a promise
about engine performance or an additional product timing assertion. Cleanup has its own 30-second
deadline. A combined initial-plus-persistence run must budget the 1560-second scenario sum plus
60 seconds for their cleanup and an explicit startup/host allowance. The orchestrator owns the
selected exact IDs, build, independent review, deployment and personally observed native acceptance.

Direct setup reads the policy already constructed by the native Game, records all policy choices,
IDs/order/default and unchanged preset entries, pauses play and creates one disposable colonist with
a unique native-saved name marker.
It does not create or repair policies. Native Assign/Manage controls enable Beer recreation and
assign the fixture policy to that colonist. The existing `SaveLoadActionStep` calls RimWorld's native
save/load commands using one unique, absent-before save name. The installed native load path creates
fresh Games and drug databases before loading saved components, consuming the same unavailable Steel
entry before Beer/Smokeleaf again; the test never invokes those constructors or the preset factory.

After native loading, the scenario validates the unique marker and saved map/pawn/policy identities
before binding a replacement Game; coincident map or Thing IDs alone are insufficient. It reacquires
the owned policy/pawn, reopens Assign and Manage Drugs, captures the retained assignment,
checked Beer permission and unchanged following choices, then restores both edits through the loaded
native controls. Screenshots and checkpoints connect pre-save edits to post-load controls. The last
native console capture supplements the orchestrator's complete flushed Player.log inspection.

Window ownership is registered once per exact object. Old window handles are retired before load;
each reopened window is captured anew. Pointer scopes end before loading and after the second UI
pass. Deferred cleanup checks the saved marker and identities before any mutation of a replacement
game, window or saved file, rather than writing through discarded policy/pawn references. Permission
restoration, assignment restoration, pawn removal, camera/speed restoration and deletion of only the
unique owned save are independent actions. The marked pawn is removed last, retaining the ownership
proof for the other cleanup actions. A failed or unrelated game transition fails cleanup instead of
guessing a replacement target. Direct writes are confined
to setup/failure cleanup and are not evidence of persistence or native UI behavior.
