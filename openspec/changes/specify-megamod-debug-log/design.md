## Context

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), target **1.7 beta**.
S035 Better Log is a historical 1.5 reference: pinned source and installed DLL match, but the current
game already has its severity filters and uses a changed logging backend. The [research inventory](../../../mods/megamod/docs/refinement/2026-11-03-better-log-research.md)
records every source file/patch/default, hashes, historical inspection evidence and unverified boundaries.
This change creates documents only. No runtime admission or native acceptance is claimed.

## Goals / Non-Goals

**Goals:** readable native diagnostics, lossless presentation, explicit evidence-discarding controls,
bounded announcements, protected startup warnings, scoped settings and semantic admission.

**Non-goals:** repairing underlying mod errors, replacing the native log with another history store,
universal host/file suppression, automatically importing source settings, emulating source package
or assembly identities, retaining source internal self-patches, foreign-patch removal, or changing
gameplay. S094 Log After Def Error has its [separate source analysis and contract](../specify-megamod-definition-diagnostics/design.md).

## Decisions

### 1. Keep the native log and add only missing behavior

Use native DebugWindowsOpener/EditWindow_Log. Native 1.7 supplies filter controls, details, trace
sizes/divider, copy, clear, auto-open and pause-on-error; preserve those paths. A second full window
would duplicate queue state and third-party assumptions. Do not port source MainTabWindow_Log,
DebugLog MainButtonDef, menu additions or menu-sizing patches. Debug UI belongs at the top.

Log presentation must use raw record identity/text/trace separately from a rendered view. Ignore
embedded colors at the display seam, never by rewriting Log.Message/Warning/Error arguments.
Severity color configuration can use a narrow guarded Color getter or explicit owned draw boundary;
if foreign rendering prevents a safe display-only path, disable that presentation group with a
warning. Do not patch RocketMan's transpiler. Preserve native copy and detail selection behavior.
The existing Readable Numbers contract excludes diagnostics; direct owned calls must respect that.

Native queue retention remains 1000 on the inspected baseline, consecutive combination remains
native, and no enlarged history setting is invented. The specification's configurable capture
stop concerns future events reaching the controlled channel, not queue capacity. Explain this in UI.

### 2. Separate capture policy from presentation and preserve native semantics

Use a small process policy for ordinary Verse diagnostic entry points and one independent protected
path for required owned startup diagnostics and bounded capture-control notices. Capture group admission must prove pre-output control
of both retained-log and file consequences. A callback that already received emitted text cannot
provide full muting. Trace the current effective Brutal/Verse bridge and legacy limit caller before
choosing hooks; the old integer replacement is not a valid design for 1.7.

The planned controlled channel is ordinary `Log.Message(string)`, `Warning(string)`, `Error(string)`
and native forwarding into them, not arbitrary application file writes. Preserve native once-only
identity bookkeeping and temporary LogLock semantics. Test off-thread events without touching UI,
duplicate forwarding, skipped originals, errors while logging, and cap concurrency. Count each
accepted ordinary occurrence exactly once before native duplicate-row combination. Owned control
must not reinterpret source color tags, exceptions or object conversion as different severities.

Game policy makes no stronger stopping claim than native behavior. Custom/Unlimited require an
admitted path which can coordinate any native channel stop without changing unrelated emitters or
global host loggers. A control must be visibly unavailable if that guarantee is absent. Default
Game policy avoids silently imposing a historical 2000 cap. Custom range 1..10000 and dormant 2000
come from the source's upper bound/default, with zero/fractions rejected as unusable input.

Policy edits, Resume and Clear reset the owned interval; native history and once-only identities
remain independent. Preserve Clear's actual native consequences. Changing saves cannot alter
process capture. Publish current policy/limit state without causing log writes from redraws.

S094 supplies immutable current-definition metadata to this same accepted-event path. Preserve the
original message, severity and available trace, and project its clearly delimited context as part
of that one record into native details/copy/file. Count, mute, stop and announce the original
occurrence once; never port its source's separate Log.Message from a Warning prefix. Distinct
definition contexts must remain distinguishable in retained repeats, while native once-only keys
remain unchanged. Capture the context at reporting time before deferred UI work. This is diagnostic
content enrichment, not a relaxation of the display-only embedded-color rule. Protected startup
warnings and limit notices remain outside ordinary definition decoration.

Required startup warnings use the foundation warning recorder directly, exempt from owned mute/cap
gates. Keep identifiers, reason and fallback intact, write once, and do not recursively announce a
notification failure. The fallback recorder must remain independent of the feature it is reporting
as unavailable; arbitrary foreign suppression is not a compatibility guarantee.

The protected startup route directly invokes the existing Verse native category's exact Warning
output through a guarded emitted delegate. Its native `LogString<Warning>` is byref-like and cannot
be boxed into reflection invocation. This is owned forwarding code, not a Harmony patch or a second
history. It bypasses ordinary Verse entry-point capture/enrichment and its UI-opening methods; the
native formatter still supplies file framing and ordinary warning retention. Admission checks the
actual category/conversion/readiness shapes and file backend availability; no native flags or
foreign subscriber lists are modified. Arbitrary provider replacement or silent foreign suppression
is not asserted to preserve delivery. Existing native temporary retention suppression remains native.

If native delivery is unavailable or fails, the process-lifetime recorder creates
`<active savedata>/Logs/Megamod-startup-<unique session id>.log` lazily with CreateNew and retains the
owned handle. It never adopts, truncates, rotates or deletes a preexisting path. Each warning is
single-line/bounded to 4,096 characters (normal C01 field bounds fit below this), appended under a
private file lock and flushed to disk. There is no total warning-count cap. Stderr is last resort;
all-three-sink failures retain a total plus the first 64 details with 256-character failure reasons.
Thread-local recursion protection does not serialize unrelated workers or acquire Harmony metadata.
The file lock never calls native logging. A native exception after partial emission can cause a
fallback copy; without a transaction/acknowledgement API, physical exactly-once delivery across
failed sinks is not claimed. No retries or recursive failure warnings are emitted.

The Custom limit notice uses that independent recorder once after the Nth admitted ordinary event:
Warning severity, retained log plus file, exempt from owned mute/cap and ordinary counting. It must
not enter the announcement, sound, auto-open or pause paths. Display filters and native retention
remain effective. Test the boundary with warnings muted and Warning announcements enabled, not
only under default preferences.

### 3. Place bounded announcements beside the native top log entry

Receive only accepted events; process thread-originated data through bounded main-thread work,
not native window/message calls from worker threads. Maintain one count/latest-record reference
per enabled severity, with no duplicate history. On flush/update, recheck current game/developer
mode/settings. Occurrences that arrive while unavailable are not queued for later announcement.
Changing saves clears pending announcement state before using the next save's preferences.

Use one real-time sound allowance per severity per second, independent of tick speed/pause. Update
counts and latest summaries under flood without allocating unbounded work. If events are combined
by native queue logic, resolve the retained combined record without inventing a different full
trace. If native retention evicts it, show unavailable details. Selecting an announcement reveals
its severity via the same native filter state and acknowledges only that severity.

Build summaries from visible logical lines without mutating the source record; preserve embedded
color runs when Ignore embedded colors is off, and apply the severity palette when it is on, exactly
as for log rows. Use tag-free text for line measurement, not as an unconditional replacement of the
rendered color policy. Support LF/CRLF and preserve the character before a cutoff.
Maximum two logical lines is source intent; clipping long lines is
presentation only. No Messages.Message calls or source MessageTypeDefs are necessary for owned
announcements. Native sounds can be reused through admitted current content. A failed sound/widget
must not turn into recursively generated errors; record at most one independent failure diagnostic.

The one-shot sample error is a real native debug action and ordinary diagnostic event, distinguishable
from a real failure. Do not implement the indefinite source onBeforeRender spam subscription.

### 4. Keep Harmony suppression isolated and non-destructive

Source default-on suppression calls Reset and clears Harmony.DEBUG. Current FileLog.Reset deletes
the desktop file; this is deliberately not adopted. The owned option defaults off and governs only
future admitted FileLog output. Cover direct Log, buffer production/FlushBuffer and helper forwarding
without repeated double suppression. Drop pending buffer at activation only as the explicitly
documented diagnostic-data loss; never delete existing files or replace external LogWriter objects.

Identify whether custom writers can safely be treated as diagnostic-only. Unknown or mixed-use
writers are an unavailable adapter, not justification for replacing them. No environment-variable
rewrite, global Harmony.DEBUG mutation, unbounded hidden buffer or replay on disable. This adapter
has independent admission/rollback and cannot disable ordinary native logging if it fails.

### 5. Use common settings, with a narrow process-wide exception

Order Diagnostics / Log as Presentation, Announcements, Capture, Developer text. Planned stable
definition IDs below are design details, not source migration bindings.

| Group / keys | Built-in defaults | Scope / effect |
| --- | --- | --- |
| log.messageColor, warningColor, errorColor | Native white, yellow, LogError | Inherited, next redraw |
| log.ignoreEmbeddedColors | true | Inherited, next redraw; raw text untouched |
| log.announceMessages, announceWarnings, announceErrors | false, false, true | Inherited, subsequent accepted events while Playing + DevMode |
| log.announcementSound | true | Inherited; native category sounds, bounded |
| log.muteMessages, muteWarnings, muteErrors | false | Global-only; next eligible event |
| log.capturePolicy | Game policy with dormant Custom=2000 | Global-only composite mode/value; atomic validation; mode/active value edit starts interval |
| log.suppressHarmonyFile | false | Global-only; safe future output after admission |
| log.disablePseudoTranslation | false | Inherited, subsequent rendering |

Capture affects startup and shared process evidence, not a colony's simulation. Consequently only
capture/Harmony rows are exempt from save inheritance. The common renderer must explain global-only
scope instead of constructing disabled fake save values. Ordinary rows still have Use global,
Current save and Global default with disabled callbacks at the main menu. First overrides seed from
the current effective value, dormant overrides survive inheritance and scope reset. Global Restore
defaults and Current-save Use global for all must have distinct common operations.

Filter flags, selected record, divider size, pending announcements and interval are transient UI/
diagnostic state, not entity gameplay data or new save settings. No live log references are serialized.
Future strings need all seven baseline languages; source English literals are not copied verbatim UI.

### 6. Admit narrow behavior groups, not a historical blanket patch set

Before implementing each hook, create stable per-patch documents under mod docs/patch-targets and
link scenarios, predicate implementation, tests and acceptance. This table is a plan, **not** a
claim that predicates have already been implemented or effective targets fully mapped.

| Planned group | Minimal evidence needed | Failure scope |
| --- | --- | --- |
| Native presentation | Record severity/text/color route, display-only tag handling, unchanged copy/details and current UI draw path | Disable palette/embedded-color enhancement; keep native inspection |
| Capture mute | Callable standard channel shapes and pre-output text/severity path; skipping preserves stated side-effect boundary and native bookkeeping | Disable relevant mute controls if their promise cannot hold |
| Capture limit | Effective counter/stop/forwarding paths, callback ownership/lifecycle, thread-safe once-per-occurrence counting, independent native lock/once rules | Atomic limit group; retain Game policy and ordinary native logging |
| Protected warnings | Independent startup output and routing that bypasses only owned suppression/cap; no recursive failure path | Required foundation work before any owned suppression can activate |
| Announcements | Accepted-event observation, retained-record identity, main-thread boundary, top toolbar availability and native sound content | Disable announcements without swallowing native records |
| Harmony file suppression | Current direct/buffer/writer paths and exclusive diagnostic ownership; bounded no-replay discard | Disable only suppression adapter |
| Pseudo-translation | Fallback-only method semantics and placeholder preservation | Disable preference; ordinary localization remains |
| Diagnostic actions | Native debug action registration/top menu and ordinary error entry | Omit sample action if unsupported |

A pure observer can need only callable shape when original execution is irrelevant; accepted-event
observation is more demanding because skip-original prefixes and pre-output capture matter. Any
argument/body replacement needs semantic data-flow predicates, not method presence or a matching
integer/string offset. Validate actual incoming transpiler streams every time, preserve them on
failure, allow benign edits/other owners, and never speculatively execute foreign transpilers.
Document effective versus original IL, ordering and supported repatch invalidation. Roll back only
newly installed owned members of a failed group. Settings must distinguish requested from admitted
effective behavior. Absent optional providers do not produce warnings unless explicitly requested.

Detect active `bs.betterlog`, emit a bounded startup warning and attempt best-effort suppression
through the ordinary settings/effective-off path for the corresponding owned Log features. Match
manual-off behavior and timing without rewriting stored preferences or modifying the original;
installed-but-inactive sources do not trigger this policy. No original API/consumer/facade work or
coexistence guarantee/test matrix is required. Prepatcher/RocketMan are not mandatory integrations.
Narrow external adapters actually used still require package/member evidence and must not copy the
source's foreign patch passthrough or global callback removal. Ordinary owned diagnostic consumers
compose directly; game/effective-target admission remains mandatory.

## Risks / Trade-offs

### Implemented warning-only capture slice, 2026-11-07

`log.muteWarnings` is a common global-only Boolean, built-in false, applied from the loaded global
snapshot or next eligible occurrence after an owner-thread edit. `WarningCaptureOptionsSource`
publishes immutable requested/provider snapshots; log workers never read SettingsStore. A feature-local
editor uses the common renderer/storage/scope/reset path and shows saved On/Off separately from the
effective state. Seven localized catalogs explain permanent loss, existing evidence, process scope,
independent direct writers and the protected-warning exception. Native rendering remains unverified.

One `NativeLogPatch` for `Verse.Log.Warning(string)` serves S035 capture and S094 decoration.
After the native suppression getter and WarningOnce eligibility, the incoming stream gets a no-throw
capture decision before category/message projection. A rejected occurrence branches to the native
suppressed return/leave so it skips category/file/retention and subsequent PostMessage/auto-open
effects while preserving lock finalization. Input instructions are cloned before label changes.
A capture exception permanently disables only this instance's capture decision, fails open and
attempts one bounded protected explanation; S094 projection remains available. No counters, limit
notice, Message/Error mute, palette, announcements or Harmony-file suppression are added by this slice.

The reviewer-confirmed P1 shared-route coupling is corrected: each incoming-validation generation
retains baseline context acceptance and a separate capture-exit failure. A non-returning suppressed
continuation omits only the unsafe capture gate; it no longer rejects the physical context receipt
or poisons C01's shared admission cache. CaptureAvailable requires the same published generation,
compatible capture semantics and no prior capture-callback fault. The owner reports a semantic
failure once only while eligible muting is requested; disabled/provider-blocked choices do not
generate a dormant capture warning. Rebuild validation can restore capture semantics independently,
but the instance's callback-fault latch remains permanent. Focused shared-composition tests use the
real common SettingsStore, WarningCaptureRuntime and package policy in the Harmony host, not a
null delegate standing in for a dormant feature.

`NativeDiagnosticReporting` owns real physical groups `diagnostics.warning-channel` and
`diagnostics.definition-error`; there is no second warning patch, fake candidate or self-patch.
S094 context publishes only when both real routes are admitted and currently available. Error
failure does not remove the useful warning route. S094 original-provider suppression never prevents
eligible S035 warning muting, and `bs.betterlog` never disables independent S094 decoration.
Provider notices preserve requested values and leave original patches untouched. Disabled choices
are not failed admissions. Per-target IDs remain DDI-REPORT-WARNING/ERROR; LOG-CAPTURE-WARNING
documents the warning consumer and shares the one physical Warning receipt.

Rebuilds invalidate the existing immutable publication immediately. `RefreshReporting()` is an
explicit runtime-owner maintenance boundary outside the inspected native Log lock, also used before
the converter scope. Root must call it during owner-thread maintenance so warning-only operation
does not rely on S094 being enabled. It never runs in a log callback, never probes by re-executing
foreign transpilers, and only refreshes already-admitted routes. Incompatible compositions remain
inactive. Arbitrary foreign logging locks/concurrent patch ownership are outside this guarantee.

This tranche is implemented-unverified and awaits independent review/build/native integration.
Host tests prove scoped fixture control flow, failure isolation, lock release, Once identity and
protected direct-category coexistence; they do not establish actual native files/UI or acceptance.

### P0-A current-backend inspection, 2026-11-07

Read-only inspection of installed **1.7.5000 rev1275** is retained under ignored
`artifacts/Implementation/megamod-p0-a/native-1.7.5000/`. In the inspected native backend,
`WaylithLogFormatter.Write<TState>` formats the diagnostic, obtains its native trace, writes through
`Waylith.Logging.FileLog.Write`, then calls `LogStream.Publish`. `UnityEngine.Application` bridges
that publication into its threaded callback, including Verse's retained-log observer. Thus callback
observation is after file emission and cannot implement the specified muting promise.

The ordinary Verse string methods still check native suppression before category output.
Brutal `LogCategory` consumes `LogString<TLevel>` via implicit conversion; the readable decompilation's
apparent string call must not be mistaken for the actual target signature. Guard the real consumed
operand/data flow. The legacy Verse count callback remains present, but the constructor subscribes
the different retained-log callback; no active native stop policy is inferred from its integer.
These are original-body research facts only. Effective-chain guards, thread behavior, protected
warning delivery and native player acceptance remain open; no capture feature is enabled by this pass.

The refreshed native presentation path builds each list preview from `LogMessage.ToString()`
(timestamp/repeat framing included), truncates only that display string, and sends it to
`DevGUI.Label`. Full details use raw `text` plus `StackTrace`; `CopyAllMessagesToClipboard` iterates
all retained raw records, ignoring display filters. A display-color transform therefore belongs in
that listing projection, not in Log arguments or the general LogMessage.ToString result. The native
`LogMessage.Color` getter still selects white/yellow/LogError by severity. A palette result override
need not invent a body predicate when it uses only the record's severity and supplies the result.

`DebugWindowsOpener.DrawButtons` is drawn through a developer-mode-gated ImmediateWindow in
`DevToolStarterOnGUI`; its cached final width controls centering. Announcement integration must
respect that layout boundary and the loaded-game restriction, not draw into an assumed fixed screen
rectangle. Native log entry and clipboard actions have exact private callable seams, while row
selection is inline in the native listing. Faithful minimized Gateway actions still require an
owned surface before these UI workflows can be accepted.

The selected Harmony 1.7 file remains SHA256
`C8EF7856B2BE3CF24B345C9B5C2F197F68FCF92649635064FCE7B3CCC8F77CB0`.
Refreshed `FileLog` inspection additionally makes its mutable-buffer exposure explicit:
`GetBuffer(false)` returns the live list and `SetBuffer(List<string>)` can replace it. Future
suppression admission must account for both methods, both LogBuffered overloads and pending-list
aliasing, in addition to direct Log and FlushBuffer. A Log/Flush-only hook cannot claim bounded
buffer suppression or no later replay. No file suppression has been implemented by this research.

- Pre-output capture in the beta differs from the old Unity callback: resolve BL-Q1 before code;
  unsupported controls remain unavailable instead of falsely claiming suppressed disk writes.
- Muting/limits discard future evidence: defaults keep native capture, UI explains scope, and
  protected startup warnings cannot be lost through our own controls.
- Announcement rendering can become another error source: bounded main-thread aggregation,
  no top-left message spam, no recursive logging, and native flood/exception tests are required.
- Global-only capture is an intentional inheritance exception; save presentation changes remain
  independent so switching saves never resumes or silently mutes the process log.
- Original UI/API identity and drop-in/coexistence support are outside scope; research observable
  behavior independently without consumer audits or facades, and retain native target admission.

## Migration Plan

### Published inner-effect admission for the warning route

The first C01 reporting consumer binds publication to the actual Harmony published version and a
unique emitted body token. The provider monitor is acquired only at safe owner refresh; reentrant
refresh and a busy monitor defer. Native output callbacks remain lock-free. Published inner selectors
are assessed against the emitted operations, including the owned projection/capture helpers and
their identity operands. Mutating required suppression or message/provenance operations disables
the route; capture-helper-only incompatibility disables capture while independently safe S094 context
remains viable. Removing an incompatible effect requires a newly published validated generation.

Void/no-argument observations, void postfixes reading a single primitive/enum __result by value,
and unrelated selected effects remain eligible without an owner ban. Ref/out or mutable results
do not receive that scalar-observer permission.
Outer/unchecked binding semantics remain unproven in this initial classifier. Metadata inspection
does not run factories/callbacks or instantiate foreign attributes, and does not claim to sandbox
arbitrary foreign side effects. This is implemented host scope awaiting independent review and
affected native evidence; converter lifecycle and generated persistence remain separate obligations.

No automatic source-config/save import, file cleanup or persistent diagnostic history is planned.
Fresh owned values use the defaults above. Disabling/removing owned features leaves native records
and existing files intact. A future import requires its own backup, atomicity, restoration and
verified postconditions; it cannot be smuggled into startup detection.

### Publication-reader correction

The shared reporting reader uses the inspected provider's read-only published state entry, not
GetPatchInfo's attribute-constructing deserialization validation. Lock order is PatchProcessor monitor
then state dictionary monitor; refresh never waits or enters from a native output callback. The copy
is bounded to 4 MiB. Exact envelope/version/backend checks precede provider-owned ValidateJsonEnvelope
and DeserializePayload; supported formats are plain JSON and HARMONY-INFIX versions 1–5/backend 1.
Unknown format/provider shape or contention leaves the route unavailable. There are no state writes,
copied serializers or foreign constructor/factory/callback probes. Expected-version reads share this
boundary. Full-name CustomAttributeData detection accounts for alternate-assembly HarmonyOuter markers.
The bound is owned inspection policy, not a provider limit: a measured 698-byte host composition has
over 6,000-fold headroom. Real 4,190,477/4,194,573-byte published fixtures prove both sides and the
explicit unavailable explanation. NativeLogPatch's optional OwnedHarmonyPatch slot reader covers
its factory/ownership preflight too; unrelated receipt consumers keep their existing default reader.
Normal provider composition may execute attributes; owned inspection does not. Signed-byte literal
decoding follows provider selector semantics, with the current emitter's boxed-byte limitation
explicitly retained rather than claiming a runnable bypass.

## Open Questions

BL-Q1 through BL-Q4 in the linked research/progress ledger retain effective native admission,
deliberately used external integrations, threading/UI/S094 composition and implementation/native evidence.
No question requires interrupting specification to ask the user to explain existing mod behavior.
