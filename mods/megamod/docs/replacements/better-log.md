# Better Log - Fix your errors

## Identity and provenance

- Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
- Origin: replacement, queue **S035 / P1-27**. Source **`bs.betterlog`**, author bradson,
  [Workshop 2772559481](https://steamcommunity.com/sharedfiles/filedetails/?id=2772559481).
- Inspected **2026-11-03**: newest supplied **1.5 BetterLog.dll 1.1.0.0**, SHA-256
  `937AEFA6C5692E9633A5A3885CCC99AAEA426B57983CED5728B3EFEF0BF8C815`, byte-identical to
  [pinned upstream](https://github.com/bbradson/BetterLog/tree/4e30d7202c5dca2f2c7cd5bb3112439cae58ead7).
  Installed About advertises only 1.3–1.5; no source beta support is claimed.
- [Full research](../refinement/2026-11-03-better-log-research.md): 11 C# files / 21 matching text
  inputs, 17 patch classes, compiled/native boundaries, source defaults, hashes and consumer scan.
  MPL-2.0 source provenance retained; no foreign code, DLL or art copied into product output.
- Owning [proposal](../../../../openspec/changes/specify-megamod-debug-log/proposal.md),
  [design](../../../../openspec/changes/specify-megamod-debug-log/design.md),
  [observable spec](../../../../openspec/changes/specify-megamod-debug-log/specs/megamod-debug-log/spec.md)
  and [unchecked tasks](../../../../openspec/changes/specify-megamod-debug-log/tasks.md).

## Behavior inventory

Rows are **specified**; the narrow implemented portions are listed below. No row is natively accepted.

| ID | Action → intended result | Defaults, edge cases and source comparison | Owning requirement |
| --- | --- | --- | --- |
| BL-01 | Top debug entry → one native log window | Reuse native 1.7; no source bottom/menu additions | Diagnostic access follows the developer UI |
| BL-02 | Toggle severity visibility → retained rows hide/reappear | All initially shown; native 1.7 already supplies filters; hidden selection clears | Filters change the visible log without deleting evidence |
| BL-03 | Inspect/resize/copy/clear → native full evidence workflow | Native rolling history, copy includes hidden rows; no file deletion; native auto-open/pause retained | Inspection and copy retain complete native diagnostic content |
| BL-04 | Configure severity palette → display changes | White/yellow/native red; Ignore embedded colors=true, raw evidence preserved unlike source | Severity colors affect presentation rather than source text |
| BL-05 | Explicitly mute a severity → future ordinary channel occurrences discarded | All off; not a display filter; outside writers excluded | Muting is an explicit loss of future diagnostics |
| BL-06 | Select capture policy → native/custom/unlimited stopping | Game policy default; Custom dormant 2000, range 1..10000; one mute/cap-exempt warning notice without announcements; unchanged rolling retention | Capture limits are separate from retained history |
| BL-07 | Enabled patch fails admission → one startup warning | Survives owned mute/cap; still ordinary display filtering and native retention | Required compatibility warnings survive owned logging controls |
| BL-08 | Accepted events → optional top debug announcements | Message/Warning off, Error on; sound on, once/severity/real second, two-line summaries; same embedded-color preference as rows | Optional announcements belong to the top debug controls |
| BL-09 | Activate announcement → reveal/select/acknowledge | Safe missing-details result after native eviction; no stale replay | Announcement acknowledgement selects evidence safely |
| BL-10 | Suppress Harmony output → future writes stop | Off rather than source on; preserve files/debug flags; bounded discard, no replay | Harmony diagnostic-file suppression preserves existing evidence |
| BL-11 | Disable pseudo-translation → readable developer fallback | Off; actual translations and missing-key evidence unaffected | Developer fallback text can remain readable |
| BL-12 | Configure/reset → stated global or current-save scope | Common model; process capture exception; dormant overrides retained | Configuration uses common scope controls with explicit process exceptions; Reset operations respect their stated scope |
| BL-13 | Changed target/source coexistence → bounded fallback | Independent admitted groups; suppress owned overlap with active original | Independent providers and changed game versions fail visibly |
| BL-14 | Emit sample error → one ordinary labeled diagnostic | Top debug output only; source indefinite rainbow stream omitted | Diagnostic probes are finite and explicitly requested |

## Interactions and composed result

Native filters and retained history are reused. S034 Readable Numbers excludes diagnostic text,
traces and copied raw evidence; owned log code must not apply quantity formatting to it. Required
foundation patch warnings bypass owned mute/cap rules. [S094 definition diagnostics](definition-diagnostics.md)
now specifies direct one-occurrence context enrichment: preserve original severity/text/trace and
capture policy, with context in native details/copy/file; no self-patching or duplicate notifications.
Diagnostic data is process-scoped, with no Thing-index candidate cache or world scans.

## UI audit and relocation

| Source surface | Purpose/class | Owned destination and difference |
| --- | --- | --- |
| DebugLog minimized MainButtonDef, zero-size MainTabWindow_Log | Debug | Omitted; reuse top developer log button/shortcut |
| Injected pause/main-menu Open Log plus menu-height patches | Debug navigation | Omitted; top native entry also works at entry screen with developer mode |
| Native EditWindow_Log modifications | Diagnostic inspection | Native top-opened window; retain current native filters and trace controls |
| Top-left MessageTypeDef notifications | Diagnostic announcement | Top developer control counts/summary/sounds during play, never gameplay message feed |
| Source global patch-toggle settings/palette | Configuration | Ordered Diagnostics / Log common settings; plain behavior labels, not patch names |
| Rainbow-error toggle / single error debug output | Diagnostic probes | One labeled sample action in top debug output; unbounded stream omitted |

Presentation, announcements and developer fallback preferences inherit global defaults with save
overrides. Capture/mute/Harmony policy are global-only because they control shared startup/process
evidence across saves. Common loader/renderer still applies; main-menu save mutation is disabled.
Window filters/selection, counts and capture intervals are transient diagnostics, not gameplay state.

## Contracts consumed by other mods or saves

| Consumer/evidence | Contract | Treatment |
| --- | --- | --- |
| RimPy database/rules text | `bs.betterlog` metadata and ordering before HugsLib | Not reproduced or spoofed; megamod has its own package identity |
| Source own Defs/settings | DebugLog/MainTabWindow_Log, three MessageTypeDefs, static public classes, deep patch keys | Internal source bindings inventoried; no binary identity emulation or automatic import |
| Source Prepatcher callback and RocketMan reflection | Removes native subscriber; patches foreign transpiler | Not adopted; actual provider overlap requires narrow admitted integration |
| Other installed binary/reflected consumers | Not established by bounded text scan | BL-Q2 open; no unqualified drop-in compatibility claim |
| Native game and Harmony logging callers | Real diagnostics, retained/copyable text and file output | Preserve native channel identity and independent provider behavior; only explicit admitted controls discard future diagnostics |

No source save component or gameplay data requires migration. Future configuration migration needs
an explicit backup/atomicity/restore/postcondition contract. No source file or normal save is changed.

## Implementation and coverage

**Implemented portion: provider policy, protected startup output and warning-only ordinary muting.** Exact active `bs.betterlog`
suppresses the owned debug-log decision independently of S094; the provider test family is green.
Pure shared startup admission now has sixteen green cases; the full-core review's cleanup-warning
finding is fixed and passed reviewer recheck. The shared Harmony receipt also passed independent
review with four scoped-host cases; that initial milestone did not register an actual S035 game target.
The protected sink now composes guarded direct native Warning-category output, a uniquely owned
session fallback file and stderr last resort. Four Unit and ten native-shaped/filesystem host cases
pass; independent protected-output review found no issues, while native delivery acceptance remains pending. See
[LOG-PROTECTED-WARNING](../patch-targets/LOG-PROTECTED-WARNING.md).

The next warning-only slice adds `log.muteWarnings=false` through common global-only settings and a
seven-language row showing requested and effective state. One shared Warning adapter gates before
category output and native effects; disabled/provider/unavailable states fail open, a capture fault
does not disable independent S094 projection, native Once identities survive and protected category
output bypasses the gate. The shared reporting composition now keeps S035 Warning independently
admitted while S094 still publishes only with both severities ready. Three Unit option cases and
37 affected Harmony cases pass after the reviewer-confirmed P1 capture/context coupling fix; that
fix passed independent recheck, retained in the root part ledger's 2026-11-07 16:46 UTC section.
Root has since connected the
registered global setting, process-lifetime warning owner, existing UI and safe reporting refresh
in the product bootstrap. That small composition passed independent review after actual XML-loader
RED; it does not itself re-review every adapter or accept mute behavior. The authored native log
selection/copy scenario and exact expected-fixture-error accounting still require their reviewed
isolated run. Native warning-mute actions and the full S035 contract remain unverified.
See [LOG-CAPTURE-WARNING](../patch-targets/LOG-CAPTURE-WARNING.md) and the
[native action handoff](debug-log-native-scenarios.md). Message/Error muting, limits, announcements,
presentation, Harmony-file suppression, pseudo-translation and probe UI remain unimplemented.
Local RED/GREEN receipts are in `artifacts/Implementation/megamod-p0-a/tdd-ledger.md`.
All inventory rows require their corresponding tasks and final evidence. Intentional deviations:
top-only diagnostic UI, native filters/trace controls, raw-preserving colors, bounded announcements,
Game-policy default, non-destructive default-off Harmony suppression and common scope controls.
No expanded history, automatic repair, universal file suppression, endless spam or source migration.

Active `bs.betterlog` suppresses owned overlapping enhancements with a warning, without removing
foreign patches/configuration. Independent native logging remains native; arbitrary source/provider
breakage is not repaired. Future hooks require Harmony; optional providers are not dependencies.
No XML patch is currently needed. Any later XML change requires XML Extensions existence/shape/
package guards and merge-preserving edits. Product must remain independent of the Gateway and all
other repository mods.

## Verification and acceptance

Prose validation/review is recorded in the [progress ledger](../refinement/specification-progress.md).
BL-Q1–4 retain effective backend admission, binary/provider contracts, bounded threading/UI/S094
composition and implementation/native acceptance. Source analysis alone cannot prove file
suppression, callback safety or beta compatibility.

Future acceptance must use native top entry/settings/action workflows, personally inspected
before/action/after evidence, exact reviewed package identity and mod list, supporting file/log
observations, configuration hashes and cleanup. Diagnostic flood testing is bounded and disposable.
Semantic pass: no game economy, job or progression changes; native pause-on-error and explicitly
discarded diagnostics remain visible consequences of the selected controls.
