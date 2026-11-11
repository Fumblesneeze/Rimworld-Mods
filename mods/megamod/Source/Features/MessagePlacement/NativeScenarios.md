# Pending native S071 scenarios

## Revision20 current native layout, authored but not executed

`megamod.messages.native-automatic-layout` is executable in the owning E2E project. Setup provides
48 disposable unarmed colonists, no selection/expanded mood edge and one pinned fixture notification.
The causal player action is Escape → Options → Interface → UI scale →0.75x. The scenario captures
the native bar/message before and after, requires an actual row extent and message-position change,
then selects/deselects a native colonist to demonstrate the explicit pending-selection fallback and
fresh automatic resumption. It restores1x through Options, inspects the native developer console,
and retains exact cleanup. Posting a notification remains fixture setup, not a posting-parity claim.

The 900-second scenario budget comprises eight 1.7 screenshots at the provider's 30-second deadline
(240 seconds), six native window inventories plus six native clicks plus four pointer-scope
begin/end operations plus two console actions at their 15-second deadlines (270 seconds), and a
shared 390-second reserve for the synchronous Escape/menu/selection/cancel actions, bounded fixture
construction, assertion/checkpoint work, persistence and cleanup. Those synchronous calls do not
have a separate engine timing guarantee. The paused workflow uses `MaxFrames=int.MaxValue`, leaving
the finite wall limit authoritative while retaining its independent 4,800-tick stop. Use a launcher
allowance of at least 1,500 seconds: scenario 900 + startup/readiness 300 + cleanup 180 + 120 margin.
Both UI-scale labels use the native current-culture float-plus-`x` expression inspected in
`Dialog_Options.DoInterface`; the language alone does not determine decimal punctuation.
Fixture pawns have full mood before the first captured/action state and must report zero native edge
expansion, so a hidden mood preference cannot accidentally bypass the initial bounded predicate.

Actual bottom-right colonist-bar visibility control still needs a typed minimized native root-widget
or keybinding E2E capability. Current Gateway native clicks target captured Windows; this control is
root UI. Do not replace that action with direct showColonistBar mutation or maximizing OS input.
Full weapons/frames/mood/selected/world/S001 scenarios remain required, not accepted by this tracer.

## Implemented manual tracer, pending native execution

`megamod.messages.native-manual-reset-disable` is now executable in the Megamod E2E project.
Its preconditions are one disposable pinned native message and global offsets 10/10 with inherited
save values. Real native settings clicks disable, reenable and reset the global pair; each resulting
view retains screenshots and matching native message/window rectangles. Only those action-to-result
movements are claimed. Posting-source parity, automatic bar clearance, preview and native target/
quest/dismiss interactions remain outside this tracer. All game work stays with the orchestrator.

Owner: `fumblesneeze.megamod`. The remaining catalog below contains reserved workflow descriptions,
not executed acceptance. The native frame adapter, common offset editor and preview are integrated;
revision20's bounded native automatic adapter awaits root review and native acceptance.
Root must register executable
scenarios once those surfaces exist; direct store writes or synthetic moved rectangles are not proof.

## `megamod.messages.native-placement-interaction`

Prepare a disposable colony with a visible native colonist bar and a normal native message-producing
action with a target (for example, reject a real contextual command after verifying the exact native
path). Capture the bar and pre-action view. Perform the player action; inspect the resulting padded
message extent and preserved text/order. Hover and left-click its displayed target, then produce a
new message and right-click to dismiss it. Capture target highlight/navigation and disappearance.
The old coordinates must not retain a second hitbox. Repeat with actual native bar visibility/row
changes and an active lesson, confirming the first changed display reflects current prepared bounds.
Cover background-visible and background-hidden states, world view and admitted S001 filtered/weapon
geometry. Restore settings, camera, selection, speed and only test-owned state.

## `megamod.messages.native-offset-scopes`

Open the ordinary settings page. Through its native compound controls set +10/-10, verify effective
preview and actual gameplay messages, then change both coordinates by equal-and-opposite amounts.
Observe both axes changing independently. Set a save override, change the global pair, toggle Use
global twice and inspect dormant restoration. Save/reload and switch to another disposable save;
verify each pair/inheritance through the native page and messages. At the main menu, attempt the
disabled save/inheritance/reset controls and verify the global pair remains independently editable.
Reject an invalid complete edit without changing either stored coordinate. Preview must never post
a message, play a sound, add history or acquire a target. Verify scope-specific reset and cleanup.

## `megamod.messages.native-fitting-overflow`

Use native settings to exercise extreme valid offsets at supported UI scales/window dimensions.
Generate real messages whose measured stack fits, then a stack/text extent that cannot fit. Inspect
whole-stack translation, unchanged relative spacing/order, fitting priority over bar clearance,
newest-message vertical priority and ordinary clipping. Verify that viewport changes never rewrite
the stored percentages. Test the actual native input hitboxes at fitted positions, not a preview.

All scenarios need exact reviewed build/PID/mod order, before/action/after captures personally
inspected by the orchestrator, normal-config hashes, classified complete logs and cleanup evidence.
Original-provider active/installed-only and unsupported optional-bar cases remain separate exact-mod
workflows; no compatibility claim follows from the pure policy or these descriptions.

## Concrete targeted-message action and timing fallback

Current 1.7.5000 source provides a concrete native action for the eventual integrated scenarios:
prepare two transport pods on disconnected fueling-port groups, select them and press their native
loading command. `RimWorld.Command_LoadToTransporter.ProcessInput` can post
`MessageTransportersNotAdjacent` with a real LookTargets target. Validate that disposable setup reaches
this branch; do not post the expected message directly. Use the warning's native hover, left-click
target navigation and right-click dismissal for before/action/after captures at moved coordinates and
an old-coordinate negative check, after setting a nonzero offset through the eventual real settings UI.

Once runtime composition exists, add a separate exact test-fixture run that calls the real collision
query early inside native message collection. The action must still produce the normal targetable
warning, with the owned contribution vetoed and no partially shifted member/ghost hitbox. Native
callback objects, return/out values and input remain authoritative. An admitted normal-order run
provides the moved comparison. Fixture setup/diagnostic reads are not acceptance; inspect the actual
native action and rendered/interactable results. Remove only that run's fixture owner during cleanup.

This remains an authored protocol, not an executed E2E. Current consumer observation retains early
history through the outer UIRoot envelope; native composition, automatic bar geometry, central
registration and the actual player workflows still gate acceptance.

## `megamod.messages.native-menu-window-fallback`

Scope: the initial exact UIRoot_Entry/no-current-Game fallback only. This does not accept gameplay bar
clearance, S001, settings editing, target navigation or right-click dismissal (the native message
button is conditional on Playing).

Root runs the reviewed package in fresh disposable minimized profiles, using existing typed Gateway
native-window capture/click and screenshot operations. The current E2E readiness requires a playable
map, so do not disguise this menu protocol as an executable map E2E or extend the runner here.

1. Prepare an isolated profile with the common global messages.positionOffset pair10/0 and no save;
   retain its exact configuration and build hash. Configuration preparation is an input precondition,
   not a native settings-edit action or a direct modification of message/window coordinates. Keep
   normal configuration hashes and only the declared required package list. A separate zero-offset
   control profile may provide a same-build comparison at identical language, viewport and UI scale.
2. At the actual main menu, confirm exact current UIRoot_Entry and Current.Game null diagnostically.
   Open ordinary Options. Capture its exact native window handle and screenshot; navigate to Gameplay
   using its native controls. The inspected Dialog_Options.DoGameplayOptions contains ResetAdaptiveTutor.
3. Capture before, click its Reset control, capture the actual confirmation dialog, and click Confirm.
   Native Dialog_Options invokes Messages.Message with AdaptiveTutorIsReset then
   PlayerKnowledgeDatabase.ResetPersistent. Never invoke that callback or Messages.Message directly.
   The learning-helper reset affects only this disposable profile.
4. Capture the first resulting rendered message and a following input/repaint view. Personally inspect
   its visible placement, text, padded background and unchanged window layout. Supporting reads may
   correlate native message.lastDrawRect, windowRect and the admitted captured rectangle; they do not
   replace the screenshot/action evidence. If using the zero-offset control, compare the actual logical
   horizontal change to10% of the same logical viewport, with viewport-fitting rules respected.
5. Close only scenario-owned dialogs through native cancel, retain request IDs, captures, complete logs,
   exact build/PID/mod/config identity, and gracefully close that isolated process. The reset learning
   data is disposable; do not restore it into or touch the normal profile.

Before execution, root must supply central definition/UI/runtime registration and verify native
menu-window capture/click capability for that exact process. If the Options control is offscreen and
the admitted minimized input surface cannot reach it, report that exact action gap; do not substitute
direct callback execution or desktop input. Native text editing of the offset pair remains a separate
Gateway/workflow obligation and is not claimed by this preconfigured-message scenario.
