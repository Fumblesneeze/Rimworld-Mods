# LOG-PROTECTED-WARNING: protected startup evidence

Owner `fumblesneeze.megamod`, S035 foundation. **Invocation adapter composed and independently
reviewed; dedicated native protection/fallback acceptance remains pending. This installs no Harmony patch.**

## Consumed route and scope

Inspected 1.7.5000 `Verse.Log.logCategory` references `Brutal.Logging.LogCategory`. Its exact public
instance Warning method consumes `LogString<Warning>, string, string, int`. The implicit string
conversion returns that closed generic byref-like value. `IsEnabled(LogLevel.Warning)` establishes
category readiness. The direct call reaches Waylith formatter file output and subsequent retained
warning publication without invoking `Verse.Log.Warning`, PostMessage or warning auto-open.

`ProtectedNativeWarning` resolves these exact callable shapes at construction and emits a small
owned Action<string>; no ref-struct boxing, source-code copying, native flag mutation, foreign probes
or global backend patch is involved. The `Waylith.Logging.FileLog` public static Disabled/StartedRoot
getters and private static bool `_shutdown` must match. Unstarted, disabled, shut-down or disabled
category output does not acknowledge delivery. Availability is checked again after emission because
the inspected native file failure handler sets Disabled. Primary failure/unavailability selects the
independent fallback. It cannot prove an arbitrary modified provider wrote bytes silently.

Future owned mute/capture/enrichment gates must remain at ordinary Verse entry points. Protected
warnings bypass those gates, ordinary counts, S094 decoration, announcements and explicit UI effects.
They still use native warning severity/filtering/rolling retention. Native LogLock-related retention
rejection and unknown foreign suppression are not bypassed by changing native global state; the file
and fallback guarantees are separate. Native observed UI/file behavior remains a required gate.

## Runtime composition and failure ownership

Construct `ProtectedDiagnosticsRuntime(activeSaveDataRoot)` once before admission. Pass `.Write` to
the shared AdmissionCoordinator and DefinitionDiagnosticsRuntime. It resolves the already loaded
Waylith file backend, composes `ProtectedDiagnosticSink`, and exposes fallback path, static native
unavailability reason, complete-delivery-failure count/details and reentrancy count.

The independent `StartupWarningFile` accepts only an absolute active save-data root and creates a
unique file under its Logs directory. CreateNew plus the retained write handle prevents adopting
other evidence; readers can share read access with an active writer. Writes are serialized only
on its own file lock and flush to disk; that lock never calls a native logger. The runtime uses
Console.Error only after the file route fails. No global stdout/stderr redirection is installed.

The sink bounds individual messages to 4,096 characters and failed-sink reasons to 256, normalizing
controls to keep records single-line. It never drops later per-patch delivery attempts because a
receipt list is full; only detailed failure receipts stop at64 while the total keeps increasing.
Thread-local recursion protection suppresses reentrant notifications and releases in finally.
No additional failure warning recurses through logging. Partial native write followed by failure
may also leave a fallback copy; exact once across failed sinks is not claimed.

## Evidence and remaining work

Four Unit cases cover fallback, complete failure, recursion and concurrent bounded receipts. Seven
native-shaped host cases cover ref-struct category output despite a scoped ordinary-entry suppressor,
unavailable/failed backend state and unknown shape. Three filesystem cases cover80 concurrent records,
distinct session files, relative-root rejection and preservation of a collided preexisting path.
These are host fixtures, not Verse/provider initialization or native player acceptance.

The reviewed-build native scenario must open/select/copy the protected warning through the native
top log UI and correlate it to Player.log while owned ordinary warning mute/capture stop is active.
A separate unavailable-native-route case must inspect the session fallback file and retained failure
status without manufacturing a successful player outcome. S035 mute/cap and those native scenarios
remain pending; no feature task is accepted by these host results.
