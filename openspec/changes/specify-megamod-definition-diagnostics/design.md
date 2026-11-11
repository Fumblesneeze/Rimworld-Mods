## Context

Owner **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
S094/P2-23. [Research](../../../mods/megamod/docs/refinement/2026-11-05-definition-diagnostics-research.md)
pins the selected 1.7.1 file/1.7.0.0 assembly, matching public source and native 1.7.4998. Current
implementation targets the inspected 1.7.5000 native shapes. Core/adapters and central startup are
implemented-unverified; native loading, UI/copy, reload and target-admission acceptance remain open.

The continuation maintains a thread-local asset/depth around both DefFromNode loaders, then emits
a new Log.Message from a Log.Warning prefix. Despite its public format promise, it includes no
defName, and no Log.Error hook. The additional Message precedes the Warning, has independent capture
policy and can consume the last slot or leave orphan context. Preserve the useful attribution
intent, with deliberate warning-and-error coverage and one-occurrence enrichment.

## Goals / Non-Goals

**Goals:** truthful current-definition context, unchanged original evidence/severity, exception-safe
scope, direct S035 capture composition and a clear global-only setting.

**Non-goals:** repairing bad values, changing loaders or fallback results, diagnosing causal patch
ownership, tracking every XML modification, annotating XML-read/assembly/cross-reference errors
outside conversion, intercepting arbitrary direct file writers, uploading logs or adding another UI.

## Decisions

### Scope context to actual conversion execution

Native LoadedModManager creates Mod classes before XML conversion. Its ParseAndProcessXML selects
DirectXmlToObjectNew.DefFromNodeNew by default and DirectXmlLoader.DefFromNode for the legacy command
line mode, passing XmlNode and LoadableXmlAsset from assetlookup. Mod settings and hook admission
can therefore be prepared before these conversions. Do not claim diagnostics emitted before owned
initialization are retroactively covered. HotReloadDefs re-enters the loading path; callbacks must
remain idempotent rather than accumulate on each reload.

Use a small execution-local stack of immutable context snapshots and invocation tokens, released
by guaranteed finalization. Current native loading is synchronous on its caller, so thread-local
scope is adequate only after effective execution confirms no asynchronous handoff. Future async
loaders require an explicitly admitted context handoff; never read a global last-loading asset from
the deferred log/UI callback. Snapshot primitive strings, not long-lived XmlNode/ModContentPack/Def
references. This is ephemeral diagnostic context, not a Thing or Def database cache.

Capture declared node kind/Class, direct available defName and known asset provenance. If the actual
name can only be known after conversion, do not manufacture it before that point or rerun parsing
to obtain it. Unknown values remain explicit. Native inheritance/patch provenance is not a causal
blame graph. The existing assetlookup can omit nodes introduced by patches; do not guess an owner
from the first running mod, a directory substring or the nearest previous asset.

The implementation's bounded source snapshot examines at most 4,096 direct XML children. It retains
a direct defName consisting of one text/CDATA node; structured, absent or beyond-bound values remain
explicitly unknown. It does not recurse through nested names. Each unavailable metadata field falls
back independently, with one protected process-lifetime failure explanation. Generated asset names
without a source folder remain relative/unresolved; no current-directory origin is inferred.

The chosen converter bridge wraps the actual incoming original body with an owned outer try/finally,
after validating direct node resolution and the exact synchronous parser delegate invocation. Entry
inside the original body handles skip-original prefixes without artificial scope. Original returns
and escaping exceptions leave through token disposal on the same call stack, including existing
native catch/finally behavior. Tail/jmp, filters/fault handlers, argument reassignment/address-taking,
unbalanced exception regions and returns inside an exception region are unsupported. Incoming anchor
entry edges cannot substitute a different node after the validated argument load. Arbitrary foreign
asynchronous/deferred conversion and unknown foreign lock ordering remain outside this support.

Each genuinely new nested conversion replaces current context even with a null asset; restore the
outer frame afterward. The source instead inherits a nonnull outer asset across null nested inputs,
which can misattribute a different generated definition. A proven forwarding wrapper for the same
node/asset can share the logical identity, but only one context section is attached to the accepted
occurrence. Handle entry failure, skip-original paths, exceptions, stack mismatch and reload cleanup
without suppressing the original exception or retaining a dangling frame. Do not log from cleanup
through an unguarded decorated path.

### Enrich one accepted event, preserving original diagnostic content

Resolve category outputs by their complete consumed callable shape before checking uniqueness:
instance non-generic void output, the caller-metadata parameter tail, a closed generic value-type
message and its public static non-generic implicit conversion from string. The native Error category
also exposes Error(Exception, string, string, int); that unrelated overload cannot make the consumed
string route ambiguous. Preflight conservatively requires one viable output in the reflected category
inventory and one string conversion for that message type. Two viable overloads still reject even
if only one is consumed by the body; this predicate does not prove competing consumed semantics or
support every benign unused overload combination. The
transpiler independently requires the selected exact conversion/output operands and supplied argument
flow in its actual incoming stream; reflection alone cannot authorize a changed effective body.

Compose an immutable context snapshot into the existing owned diagnostic envelope at the reporting
boundary, before deferred UI work. Keep original text and available trace separately from the
clearly delimited context section; no numeric formatting, color stripping, text shortening or
exception substitution at capture. The envelope is an event in the existing native-backed channel,
not a second history store or general logging framework.

The preferred native projection appends the context block to the same diagnostic's text for native
retention/copy and the same emitted file record. Preserve the original text verbatim as an identifiable
section and retain native trace provenance; do not reconstruct the original diagnostic by calling
Log.Error again. If the effective logging boundary cannot preserve those guarantees, disable that
enrichment group instead of replacing evidence with a guessed stack. The S035 display-only color
rule remains intact: context is captured diagnostic metadata, not rewritten cosmetic presentation.

Preserve original severity. Run capture/mute/limit admission once for the original occurrence, then
project original plus context as one accepted event. The default/native logging route must also
avoid the source's extra Log.Message. Respect LogLock and existing once-only forwarding; hook the
actual accepted Warning/Error path, not both Once and its inner method as independent occurrences.
The native usedKeys bookkeeping remains untouched. Identical original text with different context
must not collapse into one indistinguishable row; native text/type combination naturally retains
distinct projected text. Identical text/context remains eligible for native combining (99 repeats
per row in the inspected baseline), with the unchanged 1,000-row history.

The emitted occurrence keeps ordinary error auto-open/pause and warning-open effects. Accepted
event observation for S035 announcements happens once, using original severity and text; context
does not create a second sound/count or bypass a muted severity. Preserve native file output and
copied native record framing with the complete context. No UI calls occur on loading/worker threads.
Source metadata extraction must be bounded and no-throw; failure keeps the original event and uses
a separate reentrancy-protected one-shot diagnostic. Foundation startup warnings and S035's limit
notice stay on their existing protected paths, outside this ordinary context decorator.

### Render paths and unknowns without overstating provenance

Retain `[Def Error]` as a recognizable search marker, but show explicit field labels so readers do
not infer warning severity from the marker. Include stable package identity in addition to mod display
name, since different mods can share a display name. Preserve diagnostic strings/identifiers verbatim;
escape control characters in contextual metadata so a filename cannot manufacture another field.

Derive root-relative paths only after normalized path containment with a separator boundary; do not
use source string.Replace(rootDir, "") which can remove repeated or unrelated substrings. No file
read is needed to describe an already loaded asset. Known external paths can remain labeled external;
missing asset/mod/path/name fields are represented independently. Never promise line numbers or
patch authors absent from native evidence. Use common localized setting/status UI while keeping
machine identifiers and original messages unchanged.

Path containment is lexical after platform-native absolute normalization, with a separator boundary;
it does not query the filesystem or resolve symbolic links. A relative source name or invalid path
is shown as an explicitly unresolved path without inventing a current-working-directory origin.
Missing paths remain unknown. Escape metadata control characters and Unicode line/paragraph
separators; this does not alter the original diagnostic, truncate context, or translate identifiers.

### Common settings with an explicit exception

One definition, `diagnostics.definitionContext`, defaults true and is global-only through the
shared settings path. Startup parsing precedes any save and a process log interleaves shared loading
evidence; a per-save override would ambiguously change process-wide capture decoration. Explain
that reason on the common row. No separate ModSettings implementation/static source Enabled field,
save migration or per-pawn/entity state. Edits affect subsequent occurrences immediately, including
a later native reload, without restarting/resuming logging or deleting previously enriched records.

If the initial option is disabled, leave candidates silent and unattempted. The first later enabled
choice can make the guarded registration attempt on the serialized settings owner before a later
native reload. This is initial admission, not retry of a rejected group: C01 retains rejection and
the stored preference, and no worker callback subscribes to or mutates the settings store.

Place the row in S035's diagnostics configuration. Its Reset follows global scope; Current-save
Use global for all does not touch this global-only row. The source version-label helper is not
feature behavior and needs no VersionFromManifest dependency. Existing top debug entry/automatic
native startup window suffices; no bottom or gameplay control is added.

### Minimal executable admission and provider composition

Register actual per-patch target documents before coding; these are candidate seams, not completed
validators. Pure observation need not match complete IL, but correct dynamic context lifetime and
pre-output logging semantics require more than a method name.

| Candidate | Minimal consumed semantics | Failure domain |
| --- | --- | --- |
| DDI-CONVERT-NEW | DefFromNodeNew receives the current node/asset, performs conversion on the admitted execution scope, and exits through guaranteed restoration even on exception | Default conversion scope |
| DDI-CONVERT-LEGACY | Equivalent actual node/asset/execution/lifetime contract for DefFromNode | Legacy conversion scope |
| DDI-REPORT-WARNING / DDI-REPORT-ERROR | Original text reaches the native severity/file/retention channel after actual admission; accepted context can be projected once without replacing trace, original effects, once-only or LogLock semantics | Shared ordinary event enrichment |
| DDI-LIFECYCLE, if needed | Reload/teardown cannot leave or stack duplicate callbacks/context frames; prefer owned lifecycle calls | Required context lifetime |

Admit a converter scope only together with its required reporting/lifetime guarantees. An unavailable
legacy path need not disable a supported default path; a failed shared reporting group disables all
owned context while preserving native diagnostics. No source PatchAll. Check effective code and
relevant prefix/transpiler skip/replace behavior and required text dataflow, not merely original IL
or a version/hash. If a prefix changes text, establish that the effective method consumes that text
for the expected output; if a transpiler is used, validate its actual incoming stream each time and
return it unchanged on failed/ambiguous predicates. Never rerun foreign transpilers as a probe.

False/unknown/exception/install failure skips the affected atomic group and rolls back only newly
added owned work. Compatible foreign patches and unrelated edits remain eligible. Supported later
repatches trigger revalidation/invalidation without deleting original evidence. Emit one bounded
startup warning per skipped enabled patch with package, patch/spec ID, target, failed predicate,
unavailable context and native fallback. The warning recorder cannot depend on the failed logger
decorator or recursively acquire definition context.

The reporting callback and availability reads must not acquire Harmony's registry lock while native
logging holds its lock: a foreign transpiler can log while holding the opposite lock. Each actual
transpiler execution invalidates publication and atomically records a fresh immutable validation
identity. A narrow explicit refresh outside logging locks checks final owner/order/factory metadata
and republishes only an unchanged, accepted identity. It never enables a group that its owner has
not enabled. Benign rebuilds therefore pause context until that safe refresh, while rejected input
or final ordering remains inactive. No foreign transform is executed for this inspection.

The proposed runtime trigger is the inspected synchronous converter entry before establishing its
logging context, with serialized main-thread maintenance if required. Its safe phase must be
established before wiring. Arbitrary foreign calls into conversion while holding logging locks are
outside that trigger's support; do not silently move metadata inspection back into logging.
The suppression getter must dominate every accepted output path; reject incoming branch/handler
entry after that getter through the consumed output except the established suppression branch edge.

C01 establishes each candidate's owned cleanup boundary **before** attempting installation, since
an installer may throw after adding part of its work. Failure unwinds that current candidate and
prior newly installed members in reverse order. The adapter identifies exact preexisting versus
new owned members; a whole-owner unpatch is not an acceptable cleanup receipt. Cleanup failure is
reported without publishing a successful capability or attempting to remove foreign work.

The inspected Harmony 3 provider's method-only unpatch removes that callback across owners, so it
cannot serve as the rollback receipt. Use the stable package-ID owner plus exact target and patch
kind, admitting installation only when that owned slot was initially empty. Rollback addresses only
that slot; foreign owners, other owned targets/kinds and preexisting members remain untouched. An
occupied owned slot makes safe acquisition unavailable rather than authorizing its removal. If two
owned features need the same slot, they share one composed adapter/group instead of competing for it.
This receipt design still requires focused real Harmony tests; metadata inspection is not proof.

C02 publishes an immutable derived diagnostics settings snapshot on its owning main thread, first
after initial global load and before hooks, then after valid settings/lifecycle changes. Worker or
loading-thread diagnostics atomically read that snapshot; they do not concurrently traverse the
mutable SettingsStore. The snapshot is a projection of common settings, never another persistence
store or independently editable authority. A context option change affects subsequent reporting
without resetting capture intervals or native once-only bookkeeping.

Active canonical `mlie.logafterdeferror` or `ordpus.logafterdeferror` triggers a bounded startup warning
and best-effort suppression through the ordinary context-feature off path, with manual-off behavior
and timing. Keep stored preferences and the originals untouched; inactive installed sources do not
trigger this policy. Original API/consumer audits, TryGetCurrentLoadingAsset facades and drop-in or
coexistence work are excluded. No complete suppression or source coexistence guarantee follows.
S035 stays independently owned by the same product: call its accepted-event/capture policy directly,
never patch our own logger. If S035 presentation is inactive, this feature can still use the admitted
native path. An external logger remains subject to exact effective-path admission, not blanket
compatibility based on its name. Neither original provider nor Better Log is required.

No XML mutation is needed: this is an observer of conversion, not a PatchOperation or repair pass.
Any later declarative patch would require its own XML Extensions guards and dependency. No game
world/Thing cache, source DLL, foreign process runner, shared runtime mod or Gateway dependency.

## Risks / Trade-offs

### Shared warning consumer revision, 2026-11-07

S035's concrete warning-mute consumer supersedes the former physical two-target reporting group.
`NativeDiagnosticReporting` admits one shared `diagnostics.warning-channel` and a separate
`diagnostics.definition-error`; it publishes S094 context only after both are admitted and available.
The warning adapter remains useful for S035 after Error rejection. The runtime checks this complete
publication in converter entry and decoration, so no warning-only S094 context can leak out.
The earlier implementation emitted bounded per-reporting-target explanations for an active S094
original without installing owned converter observers, while retaining the shared Warning channel
for S035. Better Log suppression was separately projected by `WarningCaptureRuntime`. That historical
description is not acceptance of the 2026-11-08 policy: reconcile it with ordinary feature-off timing,
best-effort scope and a bounded startup warning without an original-provider compatibility promise.
Keep the independent S035 Warning channel available under its own settings and target admission.

Root constructs WarningCaptureRuntime after common global initialization, passes it into the existing
DefinitionDiagnosticsRuntime, and calls RefreshReporting from safe owner-thread maintenance.
This is also necessary when S094 is disabled or source-suppressed and there are no converter triggers.
Physical receipts, actual incoming semantics and protected startup output remain independently
guarded. The previous physical pair rollback statement is superseded only by this shared-consumer
ownership revision; atomic S094 publication and ordinary native fallback remain mandatory.

The shared route's incoming validation separates S035 capture-exit semantics from the baseline
S094 observation predicate. An unsupported capture-only exit omits the gate and exposes inactive
muting without rejecting working context, including while mute is disabled or Better Log owns it.
Later settings edits cannot turn that independent failure into a cached S094 admission rejection.

### P0-A implementation inspection, 2026-11-07

The installed native input is now **1.7.5000 rev1275**. Its `Assembly-CSharp.dll` SHA256 is
`D55AA8E47FA3C5463CA85B0ECCD51F195B4BBCD04F56DA3AF21C2F6688A81254`; this identifies the
inspection input, not an admission predicate. Read-only ILSpy output is retained under ignored
`artifacts/Implementation/megamod-p0-a/native-1.7.5000/`. `LoadedModManager.LoadAllActiveMods`
still creates mod classes before loading/converting XML, and the default/legacy entry points remain
`DirectXmlToObjectNew.DefFromNodeNew(XmlNode, LoadableXmlAsset)` and
`DirectXmlLoader.DefFromNode(XmlNode, LoadableXmlAsset)`, returning `Def` synchronously.

Current `Log.Warning(string)` and `Log.Error(string)` still gate native suppression before their
Brutal category output; `WarningOnce`/`ErrorOnce` update native identity bookkeeping before forwarding.
The native constructor subscribes `Notify_LogMessageReceived`; the legacy count callback's mere
existence does not prove an active subscription. These original-body facts do not prove the effective
patched chain. C01's first host slice consumes explicit active canonical package identities; later
target guards and isolated loader/native acceptance remain required. No runtime hook is accepted by
this inspection.

- **Origin mistaken for blame** → explicit current-definition wording, known/unknown fields and
  no causal patch claims; preserve original details for real diagnosis.
- **Native logging backend changes** → semantic pre-output admission and focused trace/file/count
  evidence; fall back to native logging rather than reconstructing events.
- **Context capture outlives the scope** → snapshot immutable metadata at reporting time, scoped
  finalization and no long-lived XML graphs or cross-thread last-asset field.
- **Source parity differs** → document warning-only/extra-Message source behavior and the deliberate
  broader severity coverage/one-event correction; original API/consumer compatibility is outside scope.

## Migration Plan

### C01 published reporting generation, first consumer

The reporting adapter now captures immutable emitted-operation metadata and a unique body token
on each actual transpiler invocation. A narrow Harmony 3 adapter reads published PatchInfo version
and inner selectors under the inspected PatchProcessor publication monitor. Refresh refuses
same-thread monitor reentry and defers without waiting when another compilation owns it. It requires
the expected next published version, unchanged body generation and exact final ordinary ordering;
the volatile publication is written while that monitor is held. Output callbacks compare body tokens
and lock-free publication only. Failed compilation and old executing bodies cannot borrow permission.

Normalized inner selectors match methods/generic families, constructors, fields and exact constant
values/positions against the emitted stream, including Project/ShouldCapture and their tokens.
Required reporting members and the conversion-to-output provenance operands are protected. Proven
observations are void/no-argument callbacks or void inner postfixes taking one by-value primitive
or enum __result (not out/ref); mutable/unknown result bindings remain unavailable at protected sites.
Unrelated selected effects remain eligible. Outer bindings and unchecked
bindings are explicitly unproven, not interpreted by a general binding framework. Metadata reads
do not instantiate foreign attributes, run factories, or invoke foreign callbacks as probes.
This proves supplied-operation/binding boundaries, not arbitrary foreign global side effects.

Capture-only helper incompatibility disables capture independently of context. S094 still needs
both severity routes. The inspected synchronous targets do not require generated async/iterator
persistence rewriting; outer persistent bindings are not yet admitted by this first narrow classifier.
This slice does not update converter Enter/Exit admission, inspect private layout algorithms, support
raw detours/cross-provider monitors, or accept native behavior. A changed/unavailable provider metadata
shape fails closed. An unrelated no-op observer throwing or mutating globals is foreign behavior,
not behavior this metadata inspection can sandbox.

### Bounded native initial default/legacy scenario

`megamod.diagnostics.definition-initial-default` and the new
`megamod.diagnostics.definition-initial-legacy` are thin declarations over one internal stateful
`NativeDefinitionDiagnosticsLogScenario`. The shared body stays in the existing initial-test file;
the legacy file contains only its declaration and delegation. The legacy attribute sets
`LegacyXmlDeserializer = true`, using the separately owned `select-e2e-native-xml-loader` contract.
Before Arrange changes view/speed state, the body compares its expected mode with the actual native
command-line predicate; it records both in the initial checkpoint. It never changes the predicate or
invokes a converter. The Gateway's pre-construction mode admission remains independently required.

The two cases retain identical package order and the unchanged initial XML fixture. No new project
item, late-options fixture or error allowance is added. The fixture's two fractional displayOrder
values and missing Def type are expected to yield two distinct warnings and exactly one original
ErrorOnce with their own source contexts. Native source supports that expectation; a successful
legacy run is still required. Each available trace is compared with its own retained record, not
required to equal the other loader's trace.

All native row/track/Trace/Copy actions and cleanup remain shared. Copy has the same stable
empty/plain-Unicode-only clipboard precondition, bounded success-reporting read, immediate unchanged
snapshot check and privately verified conditional restoration. Richer formats and unsupported or
unstable observations stop before Copy; they are never cleared or silently skipped. Such a refusal
leaves Copy incomplete even if earlier row inspection succeeded. The reviewed clipboard/geometry
helpers and reload scenario remain unchanged. This composition is authored and unrun; root owns
compile/dry-run/review and fresh default plus legacy native execution. Earlier default Copy evidence
cannot accept the revised family/shared body. See the feature-local `Diagnostics/LEGACY-PLAN.md` for
the bounded sequence and remaining obligations. Initial legacy success does not prove legacy reload.

### Bounded native default-loader reload scenario

`megamod.diagnostics.definition-toggle-reload-default` uses the existing initial XML fixture plus
the separately declared XML-only `fumblesneeze.megamoddiagnosticslateoptions` fixture. The latter
defines one unofficial OptionCategoryDef. Inspected 1.7.5000 Dialog_Options.PostOpen reports it via
ordinary Log.Error, and drawing ignores unofficial categories before reading their icon. Opening
native Escape → Options once after both reloads supplies a same-main-thread late diagnostic. Its
exact manifest count is one; the initial fixture's missing-type ErrorOnce also stays exactly one.
Neither fixture is product content or a runtime dependency. Only the selected exact group stages
the late fixture, through the existing owner-bound XML fixture planner and publisher.

The scenario clicks the actual global Definition diagnostic context checkbox, using read-only
current renderer row ordering, visibility and height measurement to locate it. It never calls row
Draw/Reset/commit delegates. Native track clicks expose the editor and later log rows, with fresh
window captures, observed vertical movement and at most three attempts per target. The two native
Hot reload Defs actions come from the existing current registry catalog (General category,
`Actions\Hot reload Defs`, source Verse.DebugActionsMisc); the action handle is reacquired each time.
No direct loader call, setting write, log clearing, once-key reset or manufactured diagnostic is
used as proof. Shared test-local native-log geometry also serves the initial selection/Copy case;
that affected initial case must be rerun after this refactor's review.

HotReloadDefs queues synchronous native work, then completion closes existing windows and resets
the message counter, not ErrorOnce's used-key set. A returned action delegate or Loaded flag alone
does not prove completion. Each reload waits at most 90 seconds/6,000 frames for no queued/current
long event and the next two fractional warnings. Exact follow-up assertions require total warning
occurrences of four then six (summing repeats), old record reference/text/trace/repeats unchanged,
and the same one original missing-type record. The case has a five-minute/18,000-frame outer bound
for both reloads and measured UI/screenshot actions. Reopened log/settings windows and action handles
are reacquired rather than retained across reload. Parsed displayOrder 37 is corroborating evidence;
native selections and personally inspected full-details captures are the player-visible evidence.

Normal execution returns the option to enabled through its checkbox. Cleanup restores owned views,
pointer scopes and speed; interrupted edits belong only to the disposable profile. The new case
does not copy or touch the clipboard. It requires context to fit Trace big and safe measured controls;
unsupported geometry stops with retained screenshots instead of assigning scroll/selection directly.
The scenario is authored/build-checked, not yet independently reviewed or natively executed. Legacy,
generated/inherited/patched/nested/exception/concurrent conversion, initially disabled startup,
main-menu edits, providers, locales and remaining S035 composition remain separate obligations.

No source settings, logs or save data are imported or rewritten. The new global field uses the
documented enabled default when missing/invalid. Turning it off or losing admission changes only
future decoration; already captured log records remain intact. No files are deleted or uploaded.

## Open Questions

### Reviewed publication-reader correction

The installed provider's GetPatchInfo reader is unsuitable for side-effect-free inspection: its
Deserialize validation constructs foreign parameter attributes through GetRequiredInfixVersion.
The narrow replacement reads only the published state entry while holding PatchProcessor's monitor,
then briefly acquires the state dictionary monitor in that order and copies at most 4 MiB. It never
writes shared state. Expected-version reads use this same boundary. Refresh remains nonblocking and
rejects same-thread reentry; unavailable monitor, field/method shape or format leaves reporting inactive.
Only the inspected plain JSON or HARMONY-INFIX envelope versions 1–5 with JSON backend 1 are supported.
The provider's ValidateJsonEnvelope and DeserializePayload helpers validate/read that payload; no
serializer is copied and the attribute-constructing version-inference pass is not called. The provider's
PatchInfo/Patch/InnerMethod/InnerTarget readers resolve metadata, not foreign attribute constructors,
factories or callbacks. Same-name HarmonyOuter markers are recognized by CustomAttributeData full name,
matching the provider's marker identity even when the marker originates in another assembly.
The 4 MiB bound is an explicit metadata-inspection limit, not a limit on required warning delivery.
It bounds startup/refresh copying and JSON work while leaving over 6,000 times the measured 698-byte
host marker-composition metadata size; this is owned policy, not a Harmony limit or a measurement of
every native inventory. Real published host entries of 4,190,477 and 4,194,573 bytes exercise the
admitted/rejected sides. Oversized metadata reports its limit and preserves ordinary native output.
NativeLogPatch's factory and ownership preflight, install and rollback inspection use this same
reader through its explicitly supplied OwnedHarmonyPatch slot reader. Other consumers retain the
receipt's default reader and are not claimed fixed. Normal provider Patch/Unpatch construction is
distinct from owned inspection. Literal matching treats a boxed byte on ldc.i4.s as signed, matching
the provider selector. This emitter rejects that boxed-byte IL form: the parity regression proves
the selector rule against published metadata, not an executable unsafe-admission exploit.

DDI-Q1 retains exact effective converter/reporting/trace and deferred-callback admission on the
implementation build. DDI-Q2 retains inherited/patched/generated/nested/exception/concurrent and
once/mute/limit fixtures with direct S035 composition. DDI-Q3 retains deliberately used external
integration checks, bounded original-provider warning/ordinary-off behavior, settings/localization
and scoped native acceptance. Original API/consumer/facade work is excluded. Record findings without
interrupting sequential refinement or declaring static logs to be accepted behavior.

### C01 converter publication migration (implemented incrementally; unverified natively)

The next bounded consumer uses the reviewed publication reader and existing synchronous wrapper.
Every actual transpiler execution invalidates publication, records the expected published version,
its immutable emitted operations and a unique body token. Actual Enter refresh checks the token,
version, exact owned slot and final ordinary ordering under the provider monitor, outside native
Log.logLock. Public GetPatchInfo is removed from this consumer and its explicit receipt reader.
Exit always disposes an acquired token even after admission revocation; it never borrows current
publication. No converter body/layout is regenerated as an inspection probe.

Conversion-specific effect checks distinguish phase: a prefix on Exit can skip/throw before disposal;
a postfix on Enter can throw after acquisition but before the result reaches the owned token store.
Both are unsafe even when a reporting observer signature alone would pass. An Enter prefix or Exit
postfix with void/no-argument observation can preserve acquisition/cleanup respectively; arbitrary
foreign callback exceptions/side effects are not sandboxed. Required node/parse operations, helper
identity/body-token provenance and helper results are checked against the actual emitted stream.
Inner finalizers at ownership boundaries remain unsupported until their cleanup semantics are proven.
Unrelated effects and compatible observation are not rejected merely because another owner exists.
This is not a general C03 completion abstraction. Host fixtures and independent review precede new
native evidence; no prior native result accepts this changed converter revision.

### Review correction: atomic coverage of both native entry paths

Independent converter admission is superseded for S094 context: current and legacy body adapters
and their native-entry guards form one atomic context capability. Shared Warning capture remains
independent. Guards are void observational prefixes on the two external native converter methods,
never patches on owned helpers. Exact owned guard presence AND first-prefix ordering are required,
with factory preflight before sorting. Both targets are read in one provider monitor interval using
InspectPair; a sequential pair of snapshots is insufficient for a shared coverage decision.

When rejected nested work is about to run, mark the active diagnostic frame non-visible without
popping it. This intentionally suppresses the remaining parent frame until its original token
unwinds; no child suppression token depends on the rejected child's cleanup. Projection reads only
thread-local visibility and the frame's captured immutable pair validity under Log.logLock. Unsafe
refresh phase invalidates visibility without any provider read. Removed converter transpilers are
detected on fresh guard inspection, required at every nested entry. The unchanged top-level fast
path can defer inspection; mere guard invocation does not prove removal has been observed.
Removal combined with a callback ordered ahead of
that guard leaves an unsupported interval: the removed transpiler no longer revokes the old receipt,
and that earlier callback can report before the guard detects removal. Complete removal of all owned
observers is likewise unsupported. No extra observer/provider hook or log-path inspection is claimed.

The unchanged top-level fast path reuses only the two captured admitted body-generation identities;
there is no ancestor frame to borrow there. A nested entry always rechecks both target versions,
exact receipts and first-prefix order in one InspectPair interval, including when a child body
transpiler was removed and cannot invalidate its former record. Busy or unsafe nested inspection
conservatively hides the active frame for its remainder. This is deliberately stricter than reusing
single-converter permission for an unchanged top-level call. Guards never read provider metadata from
Project or native Log callbacks. Initial validation rejects prefix factories before installation;
post-install per-target ordering failure occurs inside the C01 receipt transaction and rolls back
all four context members. Production now uses diagnostics.definition-family for those four members.

The family authority is one immutable pair receipt, published atomically. Availability compares BOTH
captured body-generation identities by reference with their current admitted identities, rather than
merely requiring two non-null individual publications. A guard-only removal still runs the body
transpiler and changes its identity; individual body refresh cannot revive the old pair. Successful
refresh of the unchanged pair preserves receipt identity. Changed pairs receive new receipts.
Each acquired frame captures its exact pair's lock-free validity check; it never follows a replacement
receipt. A normal rebuild revokes active parent context before a newly earlier prefix can report.
Invalid visibility remains latched until that frame's original disposal, even if fresh nested work
is subsequently admitted. A rejected actual-body Enter also hides the current frame, covering retained
older wrappers and rebuilds between guard and Enter. Existing native results/exceptions and disposal
of already acquired tokens remain unchanged. Initially incompatible prefix order still rejects the
whole group. Ordinary supported failures preserve S035 capture independently.
