## Context

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), target **1.7 beta**.
S035 Better Log is a historical 1.5 reference: pinned source and installed DLL match, but the current
game already has its severity filters and uses a changed logging backend. The [research inventory](../../../mods/megamod/docs/refinement/2026-11-03-better-log-research.md)
records every source file/patch/default, hashes, consumer evidence and unverified boundaries.
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

Use package-ID detection for `bs.betterlog` co-installation and suppress owned overlap without
unpatching the original. Prepatcher/RocketMan are not mandatory integrations. Narrow external
adapters require actual package/member evidence and must not copy the source's foreign patch
passthrough or global callback removal. No compatibility facade is justified by the bounded
source-consumer scan alone. All ordinary owned diagnostic consumers compose directly.

## Risks / Trade-offs

- Pre-output capture in the beta differs from the old Unity callback: resolve BL-Q1 before code;
  unsupported controls remain unavailable instead of falsely claiming suppressed disk writes.
- Muting/limits discard future evidence: defaults keep native capture, UI explains scope, and
  protected startup warnings cannot be lost through our own controls.
- Announcement rendering can become another error source: bounded main-thread aggregation,
  no top-left message spam, no recursive logging, and native flood/exception tests are required.
- Global-only capture is an intentional inheritance exception; save presentation changes remain
  independent so switching saves never resumes or silently mutes the process log.
- Existing source UI/API identities are not preserved by gameplay parity; document known metadata
  consumers and retain binary/reflected consumer research before any drop-in claim.

## Migration Plan

No automatic source-config/save import, file cleanup or persistent diagnostic history is planned.
Fresh owned values use the defaults above. Disabling/removing owned features leaves native records
and existing files intact. A future import requires its own backup, atomicity, restoration and
verified postconditions; it cannot be smuggled into startup detection.

## Open Questions

BL-Q1 through BL-Q4 in the linked research/progress ledger retain effective native admission,
optional providers/consumers, threading/UI/S094 composition and implementation/native evidence.
No question requires interrupting specification to ask the user to explain existing mod behavior.
