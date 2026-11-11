# Common settings contract

Owner: Make Rimworld Great Again (`fumblesneeze.megamod`). P0-B owns C02 and the common
settings presentation part of C10. Status: implementation in progress; no native acceptance.

## Current bootstrap API and native seams

Current inspected runtime is 1.7.5000 rev1275, Assembly-CSharp SHA-256
`D55AA8E47FA3C5463CA85B0ECCD51F195B4BBCD04F56DA3AF21C2F6688A81254`.
Read-only native decompilation is retained under `artifacts/Megamod/P0-B/native-1.7.5000/`.
`Verse.Mod.GetSettings<T>` reads the single ModSettings object; native settings-window PreClose
calls `Mod.WriteSettings`. `Scribe_Collections.Look` supports Value/Value dictionaries whose data
is available during LoadingVars. `GameComponent` supplies FinalizeInit but no game teardown callback.

Root's product Mod subclass creates the store, calls `ReadableNumbersSettings.Register(store)`
and the other feature owners' concrete registrations, then calls
`SettingsRuntime.Initialize(store, GetSettings<MegamodGlobalSettings>())`. This loads globals before
hook admission and exposes no implicit active save. Create `SettingsRenderer(runtime)`, register the
actual rows with `ReadableNumbersSettingsUi.Register(renderer)` and owner-specific AddBoolean or
AddInteger calls, then delegate the native mod settings contents to `renderer.Draw(rect)`.
Global-only rows require a localized reason. Rows are ordered by stable category ID, order and ID;
all save writes retain the binding captured for that row. Integer input uses a provisional native
text field plus whole-number validation, avoiding the native numeric widget's silent float clamp.
Root's WriteSettings override calls `runtime.CaptureGlobals()` before `base.WriteSettings()`.

The root-owned Current.Game observer supplies the actual post-setter value to OnGameChanged; its
guarded ObserveCurrent handles initial observation. Admit it together with
`SettingsLifecyclePumpPatch(typeof(LongEventHandler), settings.ProcessPendingLifecycle,
settings.ReportLifecycleFailure)`. After atomic admission, enable capability before enabling either
callback. Root supplies the protected diagnostic sink through Initialize's optional Action<Exception>.
Observer, pump and direct UI getter failures revoke/disable before one notification. Unavailable
UI paths never retry the failed getter. The renderer keeps globals usable with an explanation.

Worker observer/finalization callbacks publish only immutable owner/generation/snapshot requests.
Identity changes atomically revoke old tokens, without Store or Changed access. The admitted pump
asserts the native main thread, captures/detaches old state and activates only the exact finalized
game after all long events finish in Playing state. UI entries and save callbacks reconcile again.
Reconciliation captures the mailbox receipt before reading the native owner and applies that read
only while the same receipt is current. A newer worker owner/finalization publication survives a
stale getter result. The main-thread entry also suppresses nested reconciliation from Changed reads;
CurrentBinding stays null during candidate activation and publishes only after owner/receipt checks.
Same-owner observations preserve binding; no selected map is needed. ExecuteWhenFinished is not a
general dispatcher and is not used. See `docs/patch-targets/settings.lifecycle-pump.md`.

`MegamodSaveSettings.Snapshot` is an immutable reference from construction, replaced after LoadingVars
before world/maps initialize. FinalizeInit publishes a later UI-readiness candidate. Main-thread
Changed publishes edited save state to the component; ExposeData(Saving) serializes that snapshot
without Store access. Unknown IDs and dormant overrides remain intact.

`PublishedGlobals` atomically pairs immutable Values and GlobalRevision, with stable reference across
save activation. `LifecycleReceipt` identifies exact owner and generation. C03 prepares registered
definitions/catalog inputs on their serialized owner and resolves workers through pure
`SettingsSnapshotReader.ReadRequested(definition, globals, detachedSave)`. Its Boolean/no-validator
definitions are worker-safe; the API does not make arbitrary codecs/validation callbacks thread-safe.

Store access is serialized during initialization before publication and game-thread-owned afterward.
`Changed` is a narrow synchronous integration notification after committed state/revision changes.
Consumers must not throw or reenter mutation from it. The diagnostics owner publishes its immutable
primitive option snapshot atomically once before hooks and again on these notifications; worker hooks
never read mutable store dictionaries. Such snapshots are derived views, not persistence authorities.
The runtime copies global persistence data only when the global revision changes.

Pure definitions, codecs, snapshots and resolution belong in `Core/` (`Megamod.Settings`).
`Runtime/` contains the Verse persistence and native settings renderer adapters. Feature-owned
definitions stay with their feature. The orchestrator owns bootstrap and central registration.

## Dynamic Boolean rows and nonpersistent sections

The concrete Content Selection consumer can register an existing GlobalAndSave Boolean definition
with `AddDynamicBoolean(definition, label, categoryKey, help, status, visible, unavailableReasonKey,
displayOrder)`. Label/help/status delegates return already-localized text; category and unavailable
reason are translation keys. A null reason means eligible. A non-null reason disables inheritance,
save and global controls; all three actual commit callbacks independently recheck the current reason
through SettingsEditGuard before the ordinary runtime/store checks. This does not relax main-menu,
inherited-editor or stale-binding restrictions, nor add another settings authority.

`AddSection(id, categoryId, order, categoryKey, height, draw, reset)` registers only UI content.
Height is the callback's exact contents height, with four additional units of row spacing. Section
and setting IDs share one namespace and duplicate IDs reject. Sections participate in category/rank/ID
ordering and the existing activation reset. Their native drawing restores font, anchor, wrapping and
color in finally. They never register a persisted setting or create a save.

At each draw, visibility is read once for each row and rank once for each visible row. Detached
placements are sorted without invoking delegates from comparisons. A section's filter edit affects
the next draw. Dynamic reason/error/status are also captured for that draw and each measured at the
actual width before being drawn in a separate wrapped text block; an edit's resulting status/error
appears on the next draw. Static translated APIs retain their existing controls and row behavior.
Delegates are serialized UI-thread projections and must not mutate registration or settings while
being evaluated. Native rendering/input acceptance remains pending.

## Authority and persistence

- Definitions identify a stable ID, typed built-in value, codec/validation, scope, category/order
  and application timing. Decode only with an explicitly registered typed codec; serialized data
  never names a type to activate or a method to invoke.
- Global mod configuration stores global values. Save configuration stores each row's inheritance
  flag and optional dormant override. Missing save rows inherit the current global value; missing
  global rows use the built-in value. There is no copied initial-global snapshot.
- Checking Use global retains the dormant override. Unchecking restores it, or seeds the first
  override from the current global value. Global edits never overwrite save records.
- Save callbacks require a current binding, identified by save identity and runtime activation
  generation. No active save, a stale binding, or an inherited editor rejects a save-value write.
  Loaded world view still has a save; selected-map presence is irrelevant.
- Unknown IDs remain dormant through persistence, including content choices for temporarily absent
  providers. Returning definitions revalidate those payloads. Known malformed values use the
  owning definition's documented whole-value fallback without modifying another scope.
- Detached requested-value resolution is separate from active UI binding. C03 may resolve a fully
  loaded save snapshot before activation. C03 owns applied policy publication and pending-state
  presentation; settings edits never silently publish its policy during play.
- Global, save and activation revisions distinguish effective-value lifetimes. Consumers invalidate
  presentation together with dependent widths; no cache may key only on displayed text or save name.

## First consumers

| Stable ID | Type and built-in value | Scope and application |
| --- | --- | --- |
| `numbers.enabled` | Boolean, true | Global/save, next display |
| `numbers.punctuation` | Preset plus dormant custom pair `,` / `.` | Global/save, one atomic preference, next display |
| `numbers.grouping` | Three digits; alternatives Three then two, Four digits, None | Global/save, next display |
| `numbers.minimumIntegerDigits` | Integer 4, valid 4 through Int32.MaxValue | Global/save, next display |
| `numbers.smallDecimals` | Boolean, true | Global/save, next display |
| `numbers.exclusions` | Exact ordinal string list, empty | Global/save, complete-list override, next display |
| `messages.positionOffset` | Finite X/Y percentages, 0/0, each −100 through +100 | Global/save, atomic pair, next display |
| `diagnostics.definitionContext` | Boolean, true; definition owned by P0-A | Global-only, next eligible occurrence |
| `content.allowed/<encoded-key>` | Boolean, true; definition owned by P0-C | Global/save, new-game/load boundary |
| `content.preset-active/<encoded-key>` | Boolean, false; definition owned by P0-C | Global/save, new-game/load boundary |

Content keys use length-prefixed canonical package ID, full CLR Def type name and exact Def name.
Only package identity is case-normalized. Keys hold no live Def reference. P0-A owns the additional
S035 log definitions and their specified process-wide exceptions; C02 does not duplicate them.

S047 chooser sorting is deliberately outside persistent settings: author mode and singleton grouping
start true for each process and persist only across chooser openings. Search is dialog-local. Browsing
never calls global/save writes or reads opaque foreign settings values.

## Required vertical evidence

The first setting is the real S034 enable preference. Establish global read/edit without a game,
then inherited updates, independent overrides, dormant restoration, main-menu/stale binding guards,
typed persistence and invalid payload handling one behavior at a time. Host tests use ordinary values;
they do not initialize Verse, Harmony, loaded mods or representative Def databases.

Runtime persistence must load globals in the mod constructor before initial XML diagnostics. Save
overrides must be available at the documented game load boundary before C03 applies its policy.
Only isolated native loader/save/UI scenarios prove that wiring. The orchestrator performs those
scenarios after the integrated review; source-linked host tests and authored scenarios do not accept it.

The renderer uses one ordered native mod-settings page with global/save/inheritance columns, localized
disabled explanations, and callback enforcement. Full S034 formatting, S047 chooser/provider behavior
and S071 placement/preview remain separate outstanding obligations beyond this first provider slice.
