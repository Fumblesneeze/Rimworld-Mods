# SS-FOREIGN-OBSERVATION

Owner: `fumblesneeze.megamod`; capability: `megamod-settings-search` SS-01/03/05/06.
Target runtime: inspected RimWorld1.7.5005 rev1082. Native assembly SHA256
82387F78262461E53BB06A67C3945912B876D1FDB07235122BBB6E59D073F125 identifies research, not admission.

The implementation follows the inspected Needle capture/tab/scroll approach independently. The
source DLL hash and read-only decompilation are retained under the local chooser-integration
research artifacts. No Needle assembly, code or settings schema is used by the product.

`SettingsForeignRecorder` resolves exact declared signatures for native CheckboxLabeled,
RadioButtonLabeled, ButtonTextWorker, HorizontalSlider, GUI TextField/TextArea, TabRecord.Selected
and Draw, GUI BeginScrollViewImpl/EndScrollView, and TooltipHandler.TipRegion(Rect,TipSignal).
The signature inventory is the constructor's Bind calls, including exact argument, result,
static/instance and visibility shapes. Methods must have ordinary nonabstract, nongeneric bodies.
One owned widget postfix per target records only a normally executed original call during the active
bounded observation scope. Inactive callbacks do nothing. No editor callback or lazy tooltip getter
is invoked to discover values; foreign settings fields are not read or copied into our store.

Acquisition is atomic through the existing admission coordinator and owned patch receipts. Current
ownership must remain exact; failure rolls back only acquired owned hooks, reports a bounded
diagnostic and leaves ordinary pages available. Native control/reader patch composition must be
verified in the integrated game process; signature admission alone is not acceptance.

Text/help observations and widget completion bounds form detached options. Scroll contexts retain
current native identity and ancestry; targets retain page/tab/control identity, never cached screen
coordinates. Identical unresolved controls remain ambiguous. Unpaired readable text is searchable
through the chooser with page/manual fallback rather than a fabricated control target.

Native pending-scroll writes are confined to a fresh ordinary reveal with matching current visible
and hidden scroll structures. Unknown transforms or mismatches refuse automatic positioning. The
reader may uncull a layout after the completed GUI root, never recursively inside the visible draw.
Tab discovery uses observed native callbacks and restores changed tabs within that bounded step.
The concrete callback/count/depth/retention bounds are specified in the owning capability.

Logical tab groups follow the inspected Needle boundary: a nongeneric TabRecord.Draw
prefix/finalizer tracks tab drawing, while the active page recorder observes successful forwarded
rendering outside those calls. Consecutive tab draws without intervening outside rendering form
one group, including the inspected native multirow/overflow paths. Group identity uses distinct
labels in stable order because native selected tabs draw last. Missing, ambiguous or overlapping
group evidence leaves navigation incomplete while retaining independently readable controls.
No closed-generic TabDrawer detour is acquired. An object-typed callback would not isolate those
shared native generic bodies; the repository's existing sibling-sharing restriction still applies.

Verification: focused detached identity checks plus the combined native search workflow exercise
real PUAH controls and separately identified test-only tab/duplicate/conditional rows. Final build,
review, native observations and cleanup remain required. Custom provider-window hidden rendering
is still an explicit reader gap; no blanket compatibility or complete-capture claim follows here.

Implementation and executable admission: [recorder](../../Source/Features/SettingsSearch/Runtime/SettingsForeignRecorder.cs),
[bounded page reader](../../Source/Features/SettingsSearch/Runtime/SettingsForeignPageRead.cs)
and [fresh reveal](../../Source/Features/SettingsSearch/Runtime/SettingsForeignReveal.cs).
Checks: [detached projection](../../../../tests/Megamod.Unit/Features/SettingsSearch/SettingsForeignControlsTests.cs)
and [native workflow](../../../../tests/Megamod.EndToEndTests/SettingsSearch/NativeSettingsSearchTests.cs).
Contract: [SS-03/05/06/08](../../../../openspec/changes/specify-megamod-settings-search/specs/megamod-settings-search/spec.md).
