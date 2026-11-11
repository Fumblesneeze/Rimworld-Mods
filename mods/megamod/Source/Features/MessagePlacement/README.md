# Message placement

S071, owned by `fumblesneeze.megamod`. The 2026-11-11 batch wires manual offsets, viewport fitting
and common enable/disable controls for orchestrator integration and native verification. Automatic
bar geometry, inert preview and full native interaction acceptance remain open. The settings page
explicitly reports unavailable automatic clearance. It introduces no renderer, persistent message
list, cache or persistence authority. Earlier tranche notes below retain their historical scope.

`MessagePlacement.TryCalculate(newestFirst, viewportWidth, viewportHeight, visibleBarBottom,
offsets, out translation)` takes current logical UI coordinates. Each `MessageBounds` is a padded
native rectangle already containing baseline, lesson and spacing; index zero is the newest message.
The nullable bar bottom means no reserve when absent, not a guessed portrait count. The future
adapter must distinguish hidden content from unavailable automatic clearance in its status UI.
All inputs belong to one current prepared draw; no previous-frame geometry is retained.

The policy first clears the first padded message by the named `BarClearance` constant (4), preserves
an already-lower native position, adds each percentage once, then fits the aggregate stack by one
translation. An oversized horizontal extent starts at the left edge. Oversized height fits the
newest message alone, or aligns its top when even that message is too tall. Other relative positions
remain untouched. The result does not change the input rectangles or the saved offsets. An empty
stack yields zero movement. Non-finite/invalid geometry, unusable viewport or invalid offsets return
false and zero movement; an eventual admitted caller must preserve native placement and report the
appropriate bounded unavailable state, not draw at an invented fallback origin.

`MessageOffsets` is an immutable pair in percentage units, not fractions. Its validator accepts
finite inclusive -100..100 values on each axis. `MessagePlacementSettings.PositionOffset` uses
`messages.positionOffset`, default 0/0, category `interface.messages`, order 0,
`GlobalAndSave` and `NextDisplay`. `Register(store)` registers it once with the common store.
`MessageOffsetsCodec` writes an invariant JSON object with exactly `HorizontalPercent` and
`VerticalPercent`. Decode rejects missing, duplicate, extra or nonnumeric fields; common validation
rejects out-of-range/non-finite decoded numbers. Common stored-value handling supplies the complete
0/0 fallback plus an error key without rewriting the payload or another scope.

No feature-specific reset or inheritance implementation exists. Use the existing scoped edit APIs:
save reset writes the default pair only with an active non-inherited binding; global reset writes
the global default. Common snapshots retain the dormant pair and reject stale/menu save callbacks.
The future compound editor needs seven complete language catalogs, including
`Megamod_Messages_InvalidOffsetPair`, before central registration. This slice adds no placeholder UI.

Tests: `tests/Megamod.Unit/Features/MessagePlacement/`, genuine pure calculations and common-store
behavior without Verse/Harmony. Native proof obligations are in [NativeScenarios.md](NativeScenarios.md).
Exact RED/GREEN evidence and hashes are local under `artifacts/Megamod/P0-B/messages-handoff.md`.

The next tranche adds `MessagePlacementBatch.TryApply`: pass-local observed native rectangles and
write/restore actions receive one common translation. Failure restores exact pre-pass state, which
may differ from the unshifted measurement on an input event. It neither measures nor draws messages.

`Harmony/MessageDrawPatch` owns the transparent native window-registration observer. It takes exact
draw/measurement/registration/forwarding MethodInfo values and exposes `Candidate`, `Available` and
the admitted captured-message/rectangle/callback `Shape`. It validates actual incoming IL on every
transpiler run, forwards native arguments unchanged and changes only the registration call site.
Host Harmony fixtures exercise owned patches with guaranteed cleanup. It is not installed by the
bootstrap and does not yet have a native frame/window adapter, prepared-bar provider or UI. See
`docs/patch-targets/MSG-NATIVE-DRAW.md`; this observer alone does not move or accept any message.
