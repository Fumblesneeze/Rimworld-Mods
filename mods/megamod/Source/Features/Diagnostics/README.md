# Diagnostics implementation state

Owner `fumblesneeze.megamod`, S035/S094. The reporting and converter adapters have scoped host tests;
`DefinitionDiagnosticsRuntime` composes the actual native types and root bootstrap registration is
implemented-unverified. Converter/runtime and shared warning-mute reviews passed. Actual initial XML
loading exposed an Error-overload admission defect; its focused correction awaits review and a new
native run. Native acceptance remains pending.

`DiagnosticSettings.Register(store)` registers the process-wide S094 option before initial common
settings loading. Construct `DiagnosticOptionsSource` after that load and before any diagnostic hook.
Its constructor publishes the initial choice; common `SettingsStore.Changed` notifications publish
new immutable options from the serialized owner. Worker callbacks read only `Current`, never the
store. The source has the same process lifetime as its store. It is derived state, not persistence.

An enabled option is the player's preference, not successful admission. Actual callbacks must also
require their complete loader/reporting admission gate. In particular, failed cleanup cannot leave
an active callback behind. Provider selection, target predicates and publication are wired, with
actual loader/UI effects still requiring the integrated native gate.

`DiagnosticSettingsUi.Register(renderer)` uses the common Boolean row. Its global-only explanation
states why definition loading and diagnostic evidence cannot follow a save-specific value. Its
keys in each feature-local `Diagnostics.xml` were authored from this screen's meaning in English,
German, Spanish, French, Simplified Chinese, Russian and Japanese; no upstream localization was
copied. Package IDs, diagnostic identifiers and paths remain invariant. Catalog source inspection is
supporting evidence only; native setting actions and rendered seven-language checks remain open.

`DefinitionContextScope` holds immutable primitive snapshots per execution thread. Nested unknown
source fields replace the outer source, successful/exceptional cleanup restores the outer frame,
repeated cleanup is inert, and out-of-order cleanup discards the thread's context rather than
resurrecting an exited definition. Tokens must be finalized on their entering thread; actual native
admission must establish synchronous invocation. Snapshot data can be retained for deferred display.

`DefinitionContextFormatter` appends one clearly separated context block without changing original
text, rich text or length. It distinguishes contained, external, unresolved and unknown paths using
lexical platform path normalization without filesystem access. Metadata controls are escaped; the
original message remains verbatim. Eleven focused host cases and independent review cover this
formatter, including distinct escaping of literal backslashes versus actual controls.

`Patching/Harmony/NativeLogPatch` implements the Warning/Error output adapter. Its 25 scoped Harmony
cases cover actual incoming-stream checks, suppression, ordering/factory rejection, failures and
rollback. Review's lock-inversion and suppression-bypass fixes passed recheck. Logging reads only
publication snapshots; rebuilt streams require explicit `RefreshPublication` outside logging locks.
That refresh cannot enable an unadmitted group. Root composes the shared reporting group, scope,
settings snapshot and protected failure sinks before native conversion begins. Category overloads
are counted only after validating their message/string-conversion shape: the native Exception
overload cannot reject the supplied-string Error route, while genuine viable ambiguity still fails.

`NativeDefinitionConversionPatch` has eight scoped host cases proving actual-body entry, result and
exception preservation, nested null-source restoration, node-binding rejection, skip-original and
supported final-order refresh. `DefinitionContextCapture` has six pure DOM/source cases. These tests
do not execute Verse, native inheritance or full mod loading.

Root startup wiring: call `DiagnosticSettings.Register(store)` before common settings initialization;
after globals load, construct `DefinitionDiagnosticsRuntime(sharedAdmission, store, activePackageIds,
protectedWarningSink)` and register `DiagnosticSettingsUi` with the common renderer. Retain the runtime
for process lifetime. It subscribes on the settings owner for first later-enabled admission; rejection
is cached by C01. It constructs one reporting pair and independently admits the actual default/legacy
converters. Native lock ownership is checked before safe body-entry refresh. Worker scope/project
callbacks read immutable options only. The supplied warning sink must protect durable output from
future S035 muting; runtime notices suppress their own decoration.

`ProtectedDiagnosticsRuntime(activeSaveDataRoot)` supplies that foundational `.Write` sink. Construct
it once before creating AdmissionCoordinator, then pass the same sink to coordinator and definition
runtime. Its native category call bypasses ordinary Verse entry-point gates and UI-opening paths;
failure uses a unique owned Logs/Megamod-startup file, then stderr. No native flags or foreign patches
are changed. Inspect this sink's UndeliveredCount/DeliveryFailures/ReentrantCount separately from
C01's callback-delivery receipts, because its public Write handles delivery errors internally.
The native adapter and fallback file have ten host cases; the pure routing/recursion receipt core has
four. Host review passed; actual native file/UI acceptance remains pending. No ordinary capture control is
activated by constructing it. Dispose the runtime only at process-lifetime teardown.

The next integration gate is review of the native-discovered overload correction and a fresh root
build/run, then actual loader/native log selection/copy and hot reload. Focused host receipts are retained under
`artifacts/Implementation/megamod-p0-a/`; executable native scenarios and final acceptance remain
the orchestrator's integrated part gate obligations.

## Current warning-only composition

Register both DiagnosticSettings and WarningCaptureSettings before common initialization. After
globals load, construct ProtectedDiagnosticsRuntime, the shared coordinator and
`WarningCaptureRuntime(store, activePackageIds, protectedSink.Write)`. Pass that warning owner as
the optional fifth DefinitionDiagnosticsRuntime argument, and register WarningCaptureSettingsUi with
the common renderer. Retain all process-lifetime owners. Root's owner-thread maintenance must call
`RefreshReporting()` outside native logging locks, including while S094 is disabled; this is a safe
refresh, never a worker-side Store read or foreign-transpiler probe.

One physical Warning patch serves warning capture and context. Error admission is separate, with
S094 publication gated by both. A failed Error route leaves Warning muting usable. Requested mute
is false by default, global-only and immutable on worker threads. Its row distinguishes active,
off, provider-blocked and unavailable states. Seven catalogs were authored from this actual scope,
not copied upstream. Capture callback failure latches capture off but preserves working projection;
structural/projection failure makes the shared route unavailable. Protected startup output bypasses
the ordinary gate. Native captured/file effects, layout and provider coexistence remain unverified.
