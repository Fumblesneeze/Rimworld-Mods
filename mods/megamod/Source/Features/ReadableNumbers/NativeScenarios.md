# Readable-number native scenario obligations

Owner `fumblesneeze.megamod`, S034. Authored steps, not executed acceptance. Root owns the reviewed
build, isolated minimized run and personally inspected action/result captures. The generic native
GUIStyle hook is still absent; these cases exercise the owned common settings and deliberate preview.
They do not prove formatting on resource/stat/quest/map labels or compatibility with another provider.

## Grouping and small decimals

Reserved ID: `megamod.numbers.grouping-small-decimals`.

Preconditions: a disposable new colony, admitted save lifecycle, the feature's ordinary Register
calls plus the explicit punctuation definition/row registration. Retain exact build/mod list/process
identity and initial global/save preferences for cleanup. Open Megamod settings with the native
ModSettingsActionStep. Capture the real window and use its fresh exact-instance handle and observed
scale for every native input. Scroll to each target rather than clicking offscreen coordinates.

1. Using native global controls, enable formatting, select comma/period punctuation and threshold4.
   Leave grouping at Three digits and the small-number option on. Capture the large preview
   `123,456,789.25`, negative preview `-1,234,567.00`, and small preview `9.18`.
2. Click the global grouping control once per mode, capturing the large preview after each redraw:
   Three then two → `12,34,56,789.25`; Four → `1,2345,6789.25`; None → `123456789.25`;
   Three → `123,456,789.25`. Signs, fractional digits and zeros must remain unchanged.
3. Choose period/comma punctuation and None grouping. With small decimals on, capture `9,18` and
   `123456789,25`. Turn the small-number checkbox off through native input: only the small preview
   becomes `9.18`; the qualifying large preview remains `123456789,25`. Re-enable it and observe
   `9,18` returning. This is an actual checkbox action, not a direct setting write.
4. For the save, uncheck Use global for grouping and small decimals. Choose Three then two and on;
   set globals to Four and off. Capture both preview columns: save `12.34.56.789,25` / `9,18`,
   global `1.2345.6789,25` / `9.18`. Check each Use global box and observe the save follow globals.
   Uncheck them and observe both dormant choices return without modifying global settings.
5. Close the native dialog, save and reload through SaveLoadActionStep, obtain the new window/binding
   identity, reopen, and repeat the scope comparison. Switching to another owned save must display
   that save's own inheritance and choices. Main-menu global controls remain usable; save controls
   are visibly inactive and clicks cannot change the prior save's stored choices.
6. Hover/press/release the inherited or menu-disabled grouping control. It must keep the static dim
   appearance, produce no active button feedback, and make no choice change. Confirm the adjacent
   enabled global control still behaves normally. Repeat representative narrow layouts in all seven
   languages, checking labels, help, preview columns and scroll reachability.
7. Restore every changed preference and inheritance flag through native controls, close owned windows,
   and remove only the disposable save. Retain before/action/after captures and cleanup evidence.

Each grouping/Boolean row is 160 GUI units high (28-unit editor). Relative to its visible row top,
the save/global editor center is at `y+68`; Use global is at `y+46`. The three columns follow the
common renderer's `(viewWidth-16)/3` layout. Translate through the captured native contents origin and
UI scale to rendered-frame pixels; never assume scale1 or reuse a handle after loading. The punctuation
preview is above these rows and may require native scrolling back into view before capture. Restore
scroll position as needed, without replacing native input with product-state mutation.

Current gaps: executable click/scroll/text step composition and actual rendered observations must be
connected by root to this owned workflow. Existing page-navigation/save-load code supplies those
supported native steps only; host tests cover the pure preference/projection contract, not the UI.
