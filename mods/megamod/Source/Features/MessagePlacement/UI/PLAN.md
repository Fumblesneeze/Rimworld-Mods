# S071 paired offset editor

2026-11-11 continuation: registration now additionally takes the common SettingsStore and a
read-only placement-admission callback, and appends the stateless MessagePlacementPreviewUi.
The diagram recalculates every draw with current effective values, viewport and native/lesson
anchor; it neither persists transient state nor posts a native Message. Current automatic-clearance
unavailability is explicit. Its detached model has focused scope/coordinate/availability tests;
native preview/reset/close was subsequently run and personally inspected by root in English on
1.7.5005. Wider rendered-language, scope and interaction gates remain open. The revision19
integrated-verification ledger retains the exact build and causal evidence. Earlier references
below to an unimplemented preview or unregistered editor describe the preceding editor tranche.

Owner fumblesneeze.megamod. This slice is unregistered and awaits native acceptance. Its first
observable result is one Interface / Messages compound row: editable horizontal and vertical
percentages, one Apply pair action and one Reset to 0 / 0 action in each scope. Use global, scope
labels, lifecycle reset, error presentation and current-save/main-menu restrictions remain owned by
SettingsRenderer.AddComposite and SettingsRuntime/Store. No private persistence or preview.

Reference inspected: Better_Message_PlacementMod.DoSettingsWindowContents in the retained upstream
source (Workshop2259516339; assembly SHA91B07FC549323BA0EB794999539E9C88F16DC89B4B87A2F4B85ABC70BCC862B1).
It has independent sliders, compares x+y, emits live sample messages, and resets both source values.
Those are source observations, not parity requirements: our existing spec requires one atomic scoped
pair, independent coordinates and an inert future preview. No upstream code is copied.

Current public integration: MessagePlacementSettingsUi.Register(SettingsRenderer, SettingsStore,
Func<bool> placementAvailable). Root separately registers
MessagePlacementSettings.PositionOffset before common settings initialization. The editor implements
ISettingsCompositeEditor<MessageOffsets>, uses MessageOffsetInput for parsing and calls only the
provided scoped commit delegate. MessageOffsetDraft owns transient text/error state only. A rejected
commit retains drafts; new stored values/lifecycle resets replace drafts. Invalid pairs never call
commit. Reset writes one default pair only when the scope is enabled, never changes inheritance or
another scope. Disabled text fields/buttons use visibly inactive native labels/outline drawing without
editable/button event paths; method guards also reject disabled callbacks.

No native renderer/helper edits or feature bootstrap activation in this slice. Seven keyed catalogs
cover only this row, its error/help and the authored integration explanation keys. A later independently
owned preview consumes effective native anchor/bar inputs; this editor does not fabricate a preview.

Translations were authored contextually for this row in all seven baseline languages. “Pair” means
the horizontal/vertical percentages committed together; “this column” means exactly the common
Current save or Global default scope. Reset help uses the existing common Use global label in each
language. Numeric0/0, percentage units and canonical keys remain invariant. Rendered locale fit is
still a native gate. Disabled outlined labels are the bounded choice approved by root; a shared
inactive-control helper should be considered if another actual consumer needs that implementation.

## Reserved native workflows (not executed)

1. Open actual mod settings from the main menu. Capture before; edit both global coordinates, Apply;
   capture both new values. Attempt current-save typing/Apply/Reset and Use global: those controls are
   visibly disabled, with the common explanation; no save value or inheritance changes.
2. In a disposable new colony, inherited row: save controls remain inactive. Change globals, Apply,
   observe inherited pair; turn off Use global, enter12/-12 and Apply once. Observe complete save pair
   and unchanged globals. Enter a valid first coordinate plus invalid second coordinate: Apply is
   visibly inactive, both stored coordinates unchanged, localized error shown. Correct and Apply.
3. Reset current-save pair: observe0/0, unchanged Use global and unchanged global pair. Reset globals:
   observe0/0 globals while a different active override stays unchanged. Re-enable Use global and
   confirm dormant override retained after turning inheritance off again.
4. Save/reload/switch disposable saves and reopen settings; drafts follow actual scoped values and
   do not leak between saves. Inspect all seven languages at the narrow supported settings width and
   representative UI scales. Retain real input/capture/cleanup evidence from root's exact reviewed build.
5. Actual relocated-message behavior and preview/bar interactions remain separate mandatory S071
   scenarios. Editor-only success cannot accept placement, bar compatibility or the missing preview.

Native scenario source/registration and execution remain root-coordinated; these reservations are
not executable E2E or evidence that the controls have rendered in game.
