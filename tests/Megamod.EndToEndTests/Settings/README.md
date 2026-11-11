# Settings native scenarios

Owner: `fumblesneeze.megamod`.

## Pending dynamic content-row integration

Reserved shared-renderer regression ID: `megamod.settings.dynamic-content-row`. The Content Selection
owner supplies the actual incident row/status source and native filter/selector actions. Use the
existing executable native page-navigation/save-load skeleton, extending it with the published
native widget steps once the caller is registered. Do not create a dummy setting or use Store writes
as input actions.

Open the native page and capture search/source/type section plus a real content row. Search through
native controls and observe filtered rows/rank on the next redraw without changing persisted values.
Edit global, save override and inheritance through their common controls, observing distinct requested,
applied/pending, scope-reason and validation text. Capture long localized status/reason/error text at
the actual available width and confirm it neither overlaps the following row nor hides another message.
Use an unavailable real feature/provider fixture to observe all three controls disabled; native click
attempts must preserve stored choices. Switch saves/menu and retry an earlier captured action: it must
not change the new save or enable a menu save editor. Native reload must apply only the content owner's
documented boundary; requested edits alone must never manufacture applied-policy evidence. Restore
preferences through native controls and close owned windows. Exact process/build, fresh window handles,
before/action/after captures and cleanup remain required. Host ordering/commit tests do not accept this.

`megamod.settings.native-page-lifecycle` is authored in `NativeSettingsLifecycleTests.cs`.
It covers native settings-page opening and closing, then save/load of the existing preferences.
It has not yet been executed for acceptance.

The reviewed lifecycle correction adds an acceptance obligation to this native workflow: successful
new-game/load initialization must produce the new binding only after the native long-event sequence
finishes; menu exit and failed load must leave save controls unavailable while globals still render.
Capture before/action/after views and correlate supporting thread/generation observations with the
actual native save/load/menu actions. The component snapshot must retain edited values through native
saving without worker access to Store. Host mailbox/Harmony passes do not establish these facts.
The existing executable scenario covers successful save/load browsing; failed-load/menu controls and
edited-value cases remain pending required Gateway/product capabilities.

The lifecycle review regressions also require that opening/rendering the settings page after a
successful load stays responsive and exposes the newly loaded save values. Correlate repeated native
save-load transitions with exact owner/generation receipts and bounded Changed notifications as
supporting diagnostics. A source-reviewed receipt race test and callback test protect deterministic
ordering; they do not substitute for the visible native action/result or prove a particular live race.

## Pending inheritance draft regression

Reserved scenario ID: `megamod.settings.inheritance-discards-invalid-draft`.

The native workflow must cover equal canonical values in both scopes:

1. In a loaded game with admitted save lifecycle, open Megamod settings using the native settings
   dialog. Through native widgets, set the global minimum integer digits to `4`, disable **Use
   global**, and set the save override to `4`.
2. Enter `3` in the current-save field. Capture the visible rejected draft and validation message;
   a supporting read must confirm the stored override and effective value remain `4`.
3. Check **Use global**. Capture the disabled current-save field displaying `4` with the rejected
   draft and its validation message removed. Confirm the dormant override remains `4`.
4. Uncheck **Use global**. Capture the enabled current-save field displaying its restored `4`,
   with no rejected `3` or stale validation message. Confirm inheritance is off and the effective
   value remains `4`.
5. Restore preferences through the same native controls and close the owned dialog; retain the
   exact action sequence, before/action/after captures, build identity, and cleanup result.

This scenario remains pending until the Gateway exposes minimized native settings-widget editing
and checkbox input. The existing visible OS input step is not suitable for that gate. Direct
`SettingsStore` writes must not replace the player actions. A host test of private editor reset
fields would not verify this rendered behavior.

Other pending native cases include creating nondefault global/save values through widgets,
main-menu save controls being disabled, and global persistence across a fresh process. These
remain distinct from the existing page-lifecycle scenario and from host settings tests.

## Pending punctuation workflow

Reserved scenario ID: `megamod.settings.punctuation-atomic-preference`. Requires the pending native
punctuation row/preview and minimized widget input, not direct product-state mutation.

Executable navigation skeleton: reuse the existing `NativeSettingsLifecycleTests` native
ModSettingsActionStep, ScreenshotStep, WindowCancelActionStep and SaveLoadActionStep sequence. It
currently proves only browsing/roundtrip of existing preferences; it must not be relabeled as the
punctuation edit case. Add native widget actions between opening and closing once the root publishes
the exact shared click/text/scroll step contracts. Keep this edit scenario unregistered meanwhile.

Click targets are derived from the actual visible punctuation row, not a fixed desktop position.
RimWorld.Dialog_ModSettings gives the product a contents rect starting at `(0,40)` within its native
window contents; the native close-button area is excluded. In the common scroll view, let `W` be
the contents width minus 20, `c=(W-16)/3`, and `y` be the punctuation row's top after category ordering
and scroll offset. Relative to that scroll view, target centers are:

| Action | Current save | Global default |
| --- | --- | --- |
| Use global | `(c/2, y+46)` | not applicable |
| Cycle preset | `(c+8+c/2, y+68)` | `(2(c+8)+c/2, y+68)` |
| Custom group field | `(c+8+(c-8)/4, y+128)` | `(2(c+8)+(c-8)/4, y+128)` |
| Custom decimal field | `(c+8+(c-8)/2+8+(c-8)/4, y+128)` | add one more `c+8` to save X |
| Apply pair | `(c+8+c/2, y+160)` | `(2(c+8)+c/2, y+160)` |

The preview starts at `y+204` and occupies 78 GUI units. Translate through the exact native window's
observed contents origin and UI scale to rendered-frame top-left pixels; bind every action to the
fresh exact-instance handle. Scroll until the whole row is visible and verify targets from its capture
before sending input. Do not use a sibling window, assume scale 1, or keep a handle across save/load.
Text entry must focus the field, select its full current draft and enter the new mark via native input.
The preset button advances through the seven specified modes; verify each resulting visible choice.

Disabled-control regression: with Use global checked, capture the dim static save preset and Apply
surfaces alongside the usable global preset. Move/press/release over each disabled surface through
native window input; compare the before/hover/press/after appearance and unchanged saved preference.
At the main menu repeat for the unavailable save column, retaining its visible scope explanation.
In enabled Custom mode enter an invalid pair, then inspect and click the inactive Apply surface:
the rejected draft/error must remain visible, the prior preview/payload must remain unchanged, and
the control must not show an active hover/pressed background. Correct the complete pair and observe
Apply becoming usable. Then click it and capture the actual committed preview. Check that subsequent
enabled controls retain normal tint/alignment/wrapping. Native hover/text-entry support and personally
observed rendering remain outstanding; static source inspection and host tests do not close this case.

Open settings, choose Custom and edit a complete `#/@` pair through native controls. Capture the
negative, large and small preview examples and the committed global/current-save scope. Try an
identical pair, digit, empty value, invisible format mark and multi-character input; capture the
validation explanation, unchanged prior preview and unchanged supporting stored values. Include an
attempt to use U+2800 Braille pattern blank and U+02D6/U+02D7 modifier plus/minus in each role;
all must reject the complete edit without changing the prior pair or its preview. Include an
eligible supplementary symbol as one character and inspect actual glyph rendering rather than relying
on host Unicode classification. Switch away from Custom, close/save/load, return to Custom and observe
the exact dormant pair. Repeat with an independent save override, switch Use global on/off, and
observe the whole pair/preset changing together. Restore both scopes through native controls, close
the owned windows and remove only the test save. Preserve build identity, causal captures and cleanup.

Reserved scenario ID: `megamod.numbers.native-measure-and-draw`. After the candidate shared-style
group is implemented/admitted, open native resource/stat/quest/map quantity surfaces, change format
through settings and compare visible text and layout across refresh. Then exercise numeric input,
selection/copy and date/history/file metadata. The input and protected text must remain native while
ordinary quantities follow the new format. Current research is in
`mods/megamod/docs/patch-targets/numbers.shared-style-text.md`; this scenario is not yet executable.
