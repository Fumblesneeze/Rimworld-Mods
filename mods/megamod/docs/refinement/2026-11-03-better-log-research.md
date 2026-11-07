# Better Log research — 2026-11-03

Owner: **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`. Target **1.7 beta**.
Queue **S035 / P1-27**. Documentation-first, read-only analysis; no source mod or game was run.
This evidence is separate from the [observable contract](../../../../openspec/changes/specify-megamod-debug-log/specs/megamod-debug-log/spec.md)
and [replacement coverage](../replacements/better-log.md).

## Identity and evidence order

1. Read the [Workshop description](https://steamcommunity.com/sharedfiles/filedetails/?id=2772559481)
   and [change notes](https://steamcommunity.com/sharedfiles/filedetails/changelog/2772559481).
   The author describes alternate log entry points, severity colors/filters, capture limits and
   muting. December 2022 changes added error messages/sounds and shorter summaries; April 2024
   added 1.5 support. A July 2025 author reply distinguishes file stopping from the old 200-row
   window history. That historical number is not the current native 1.7 capacity. Reports of 1.6
   problems are user reports, not reproduced failures or proof about our implementation.
2. Read installed README/About, then all **11 C# source files**, all three project files and build
   props, both Def files, English keys and license. Inspected **21 text inputs** match the pinned
   repository after newline/BOM normalization. The README repeats the same public feature summary.
3. Pin [upstream](https://github.com/bbradson/BetterLog) at
   `4e30d7202c5dca2f2c7cd5bb3112439cae58ead7`, dated **2024-04-15T11:23:22+02:00**,
   commit “1.5 support, mod icon”. Installed and pinned **1.5 BetterLog.dll are byte-identical**.
4. Decompile the complete 1.5 DLL read-only with ILSpy **11.2.0.10050**, compare consumed behavior
   to source, and inspect current native log/UI/translation and Harmony-file output types.

Installed root: `F:/Steam/steamapps/workshop/content/294100/2772559481`.
Title **Better Log - Fix your errors**, author **bradson**, package **`bs.betterlog`**.
About lists **1.3, 1.4, 1.5** only. There is no LoadFolders.xml, 1.6 or 1.7 assembly folder;
the newest supplied DLL is a **historical reference**, not a selected/verified 1.7 implementation.
Assembly/file version **1.1.0.0**, informational **1.1.0**, Release, net48. Source builds use
versioned RimWorld refs, Harmony 2.3 for 1.5, Fishery, Prepatcher API, PolySharp and Publicizer.
About requires Harmony; Core/Harmony/BetterLoading/Prepatcher load-after and DLC/Performance Fish/
HugsLib load-before hints do not make all those optional packages dependencies.

License: source and repository **MPL-2.0**, attributed to bradson/Bradson. Record provenance and
recheck obligations if any covered code is reused; this work independently specifies behavior,
copies no code, DLL or art into the product and grants no blanket asset reuse permission.

| Input | SHA-256 |
| --- | --- |
| Installed About.xml | `20E5EC43EDB3E195615EE1FD6B9C81AFD1FCCFC7A1C16995468DD907C6E9D57C` |
| Installed/pinned 1.5 BetterLog.dll | `937AEFA6C5692E9633A5A3885CCC99AAEA426B57983CED5728B3EFEF0BF8C815` |
| Native Assembly-CSharp.dll | `56399B6144D9F1695F9C53D054DC82972F7AB35E68D1A19A7129F2F5860CCD21` |
| Native UnityStubs.dll | `07B33DC47D1C5136B50793AE11F566261ED844CB4E7D9DBE7A441D0954F5A90C` |
| Harmony 1.7/Assemblies/0Harmony.dll | `C8EF7856B2BE3CF24B345C9B5C2F197F68FCF92649635064FCE7B3CCC8F77CB0` |

Native files are at `F:/Steam/steamapps/common/RimWorld/`, inspected baseline **1.7.4995 rev309**.
Harmony root is Workshop **2009463077**, with explicit 1.7 LoadFolders selection. Hashes identify
inputs, never substitute for semantic runtime admission. Evidence workspace:
`artifacts/Refinement/megamod-spec-20261103/better-log/{upstream,compiled,native,harmony}`.

## Complete source inventory

| File | Responsibility and evidence |
| --- | --- |
| BetterLog.cs | Mod constructor loads Settings then FishPatch.PatchAll; common settings callback/category only |
| Settings.cs | Global static scalar/palette state, patch-state serialization, scrollable settings/palette UI |
| Patches.cs | All 17 nested patch classes; menu, capture, notification, log UI, colors, Harmony file and pseudo-translation behavior |
| FishPatch.cs | Generic patch discovery/state/patch/unpatch framework, holder serialization, default enabled state |
| FreePatchEntry.cs | Prepatcher callback unsubscribes native `Log.Notify_MessageReceivedThreadedInternal` from the threaded log event |
| MainTabWindow_Log.cs | Zero-size bottom tab immediately closes itself and toggles native EditWindow_Log |
| LogMainButtonDef.cs | Startup finds `DebugLog` MainButtonDef and applies ShowMainButton visibility |
| DefOfs.cs | Three notification MessageTypeDefs |
| Strings.cs | Reflectively translates static strings once; most settings labels/descriptions remain literal English |
| DebugLog.cs | Conditional DEBUG wrappers around ordinary logging |
| DebugActions.cs | One synthetic colored error or indefinite onBeforeRender rainbow-error stream |

`Defs/MainButtons.xml`: `DebugLog`, label Log, order **592**, minimized, valid without map,
`BetterLog.MainTabWindow_Log`, custom log icon. `Defs/MessageTypeDefs.xml`: `LogMessageMessage`
with ClickReject; `LogWarningMessage` with Message_NeutralEvent; `LogErrorMessage` with
Message_NegativeEvent. No gameplay recipes, jobs, costs or save components.

## Every source patch and default

All entries are in `Patches.cs`; default true comes from FishPatch unless overridden.

| Nested patch | Default | Behavior established from source and compiled counterpart |
| --- | --- | --- |
| MainMenuDrawer_DoMainMenuControls_Patch | On | Enlarges menu rect; adds Open Log after Options by searching backward for its string around List.Add |
| MainMenuDrawer_DoMainMenuControls_Extra_Patch | On | Patches the preceding source helper to allow that button only while Playing; disabling allows the entry screen |
| MainTabWindow_Menu_Patch | On, hidden | Unconditionally adds one menu-option height to requested menu tab size |
| Log_Message_Patch | Off | Skips Log.Message(string), losing future ordinary messages |
| Log_Warning_Patch | Off | Skips Log.Warning(string) |
| Log_Error_Patch | Off | Skips Log.Error(string), including its native consequences |
| Log_Message_Message_Patch | Off | Postfix adds top-left gameplay message during Playing, unless source muting enabled |
| Log_Warning_Message_Patch | Off | Same for warnings |
| Log_Error_Message_Patch | On | Same for errors; catches notification failure and writes Unity error |
| Log_Notify_MessageReceivedThreadedInternal_Disable_Patch | Off | Skips native limit callback entirely |
| Log_Notify_MessageReceivedThreadedInternal_Modify_Patch | On | Replaces every operand equal to integer 1000 in callback with Settings.LoggingLimit getter |
| FileLog_Log_Patch | On | Prefix on FileLog.Log and FlushBuffer calls FileLog.Reset, sets Harmony.DEBUG=false, then skips original |
| EditWindow_Log_DoMessagesListing_Patch | On, hidden | Filters Log.Messages by static ShowMessages/Warnings/Errors, all initially true |
| EditWindow_Log_DoWindowContents_Patch | On, hidden | Replaces Trace big/medium/small buttons with severity toggles, using exact instruction offsets; translates nonempty string literals after replacing spaces with underscores |
| LogMessage_Color_Patch | On, hidden | Replaces severity Color result with settings color |
| Decolorize_Patch | On | Regex removes color tags from text arguments before ordinary Log calls; also replaces RocketMan's log-list transpiler with passthrough when reflected method found |
| Translator_PseudoTranslate_Patch | Off | Replaces PseudoTranslated body with original string return |

The menu extra patch is a **source self-patch**, not an external contract we should reproduce.
Menu sizing remains active even if the visible button patch is disabled. Settings toggle patch
installation at runtime. FishPatch sets its Patched flag before successful completion, catches
individual install exceptions, and does not implement semantic target admission or atomic rollback.
Do not transplant this framework or blanket PatchAll.

### Capture, presentation and notification distinctions

- Source display filtering neither removes messages from the underlying list nor filters native
  Copy all. Source Show flags are static process state, not serialized settings.
- Source color removal mutates the incoming text, so it also changes captured/copyable/file text.
  The regex is `<\/*color[^>]*>`; not a rich-text parser. Owned display-only color removal is an
  explicit improvement, not claimed source parity.
- Notification snippets strip tags, then search **Environment.NewLine** for two line breaks.
  Returning the slice before `i - 1` can lose a character; a Windows build can miss lone-LF lines.
  Notifications lack explicit storm throttling, and non-error variants have no local exception guard.
- Error announcements are source defaults even with developer mode off. Owned placement/developer
  gating intentionally changes this and avoids ordinary gameplay messages.
- Source max-message slider does not enlarge history. The literal substitution assumes an older
  callback and no meaningful relationship beyond operand 1000. A setting value of zero does not
  represent a useful positive threshold in an increment-then-equality implementation.
- The source Prepatcher callback removes a native subscriber globally; it is not a selective owned
  lifecycle hook. No equivalent unsubscription or foreign transpiler suppression is adopted.

These are code-derived results/risks, not reproduction in a running historical game.

### Persisted source settings

All global ModSettings; no save-scoped preferences or entity state.

| Setting / key | Built-in default | Source UI/behavior |
| --- | --- | --- |
| loggingLimit | 2000 | Slider 0..10000, steps rounded to 100; shown only when Modify is on and Disable limit is off |
| showMainButton | true | Bottom log button visibility; setter updates Def when loaded |
| logMessageColor | white | Palette from ColorDefs, ColorLibrary fields and red/yellow/white |
| logWarningColor | yellow | Same palette |
| logErrorColor | ColorLibrary.LogError | Same palette |
| Patches / patches / each nested class-name key | Per patch table | Deep holder/dictionary plus enabled flag under runtime type names |

Palette source discovery assumes reflected ColorLibrary fields are colors. No source import,
reset-all contract or public data exchange protocol was found. No full migration is proposed.

## Current 1.7 baseline and changed contracts

Inspected native `Verse.Log`, `LogMessage`, `LogMessageQueue`, `DebugWindowsOpener`,
`LudeonTK.EditWindow_Log`, `Verse.Translator`, and UnityStubs `Debug`, `Logger`, `Application`.

- **Native UI already supplies all three severity filters**, clears selection when its type is
  hidden, has trace-size buttons and a draggable detail divider. Do not add a duplicate filter row
  or remove useful trace-size buttons merely because the old source had to reuse their space.
- Native top debug button and Dev_ToggleDebugLog work when Prefs.DevMode; ToggleLogWindow toggles
  a single native window. Native auto-open is separate and can expose startup errors without dev
  mode. Native log controls also include Clear, Copy all, Auto-open and Pause on error.
- Native queue retains **1,000 rows**, combines consecutive equal text/type regardless of trace,
  and caps the displayed repeat count at **99**. Native full details retain the original selected
  text and trace; Copy all iterates all retained records, not the filtered list. List preview is
  bounded, with text shortened beyond 1,000 characters. No owned change to combination semantics,
  native count cap or queue capacity is specified.
- Log uses a lock, ConcurrentQueue for off-thread messages and main-thread draining. The log
  constructor subscribes **Notify_LogMessageReceived**, not the older limit callback.
  **Notify_MessageReceivedThreadedInternal still exists**, but compares to **10000**, and its full
  effective caller/subscriber lifecycle has not been established. Merely finding that method or
  changing its constant proves neither capture stopping nor file control.
- Verse ordinary calls write through **Brutal.Logging LogCategory**; UnityStubs Debug also uses
  Brutal logging. Application bridges `Waylith.Logging.LogStream.MessageReceived` to legacy events and guards recursive
  dispatch. Verse's ordinary write path does not become controlled merely by toggling the Unity
  logger flag. An observer after emission cannot retroactively prevent a file write.
- Native Clear empties retained/off-thread queues and resets message count; it does not clear
  usedKeys for WarningOnce/ErrorOnce. Muting/cap controls must preserve those once-only identities.
- Native PseudoTranslated still decorates developer fallback text and preserves format braces.
  Suppressing decoration is not supplying missing translations or fixing localization.
- Current Harmony FileLog.Log and FlushBuffer can write to custom LogWriter or configured/default
  path; LogBuffered builds a list, LogIL/LogBytes lead to output, and Reset deletes the desktop
  harmony.log.txt. Suppressing Flush alone leaves buffering, and the source also changes the global
  Harmony.DEBUG flag. The owned option needs complete admitted direct/buffer paths with no file
  deletion, global-debug changes or delayed replay.

No current logging-chain behavior or optional-provider compatibility has been accepted live.

## External contracts and bounded consumer search

Public source types include BetterLog, Settings, Patches/nested classes, FishPatch/holders,
MainTabWindow_Log, Strings, DefOfs and debugging helpers. Source Def and key identities above are
observable bindings, but not evidence that arbitrary other mods consume them.

A bounded installed `*.cs`, `*.xml`, `*.json` scan for `bs.betterlog`, named BetterLog APIs and
notification Defs found the source itself and **RimPy's metadata/rules database** (Workshop
1847679158), including a Better Log before HugsLib ordering rule and database metadata for a
Russian translation package. This establishes metadata consumers, not runtime API consumers or
an installed translation package. No inspected external source caller of the named APIs was found;
binary-only/reflected consumers remain BL-Q2. No drop-in binary or save compatibility claim follows.

The source's RocketMan reflection uses `RocketMan.EditWindow_Log_DoMessagesListing_Patch:Transpiler`
without package gating, and Prepatcher invokes its FreePatch entry. Installed Prepatcher metadata
identifies `zetrith.prepatcher`, Workshop 2934420800, advertised support through 1.6. No runtime
provider compatibility was tested. Owned integration will not patch RocketMan's patch method,
remove a global callback subscriber or infer support from dependency mentions in other About files.

## Owned decisions and composition

See the contract for complete scenarios. Deliberate changes from the source:

- Native top debug access only; no bottom MainButtonDef or extra menu entries. Native severity
  filters, trace controls, auto-open and pause behavior reused. Developer-off automatic native
  error reports are not suppressed by the navigation policy.
- Colors change presentation only; raw log evidence stays raw. S034 Readable Numbers already
  protects diagnostics; do not run quantity formatting over messages, traces, IDs or copied output.
- Announcements move to top developer controls, only during play with dev mode on. Source
  severity defaults off/off/on and sounds are retained; one sound per severity per real second,
  count/latest summary, two logical lines, explicit acknowledgement and evicted-detail handling
  are owned usability decisions. No hidden later replay or source endless rainbow spam.
- Capture defaults to **Game policy**, with dormant Custom **2000**, accepted integers **1..10000**,
  and Unlimited. This deliberately avoids imposing an old 2000 cap on a new native pipeline by
  default. Retained history stays native. Mode/active-value changes start a fresh interval without
  clearing evidence; save switching does not. Unlimited applies to the controlled channel, not all
  possible external file writers or unbounded retained history.
- Custom's single limit-reached notice is a Warning record in retained history and file, exempt
  from owned mute/cap and ordinary counts, without announcements, sounds, auto-open or pause.
  This explicit control-notice policy prevents silently hidden capture stops; display filtering
  and native eviction still apply. Announcement summaries follow the same embedded-color option
  as log rows instead of unconditionally stripping requested color presentation.
- Muting remains opt-in, distinct from filtering, scoped to standard game diagnostic calls. Owned
  required startup warnings bypass owned muting/caps. Their survival is a foundation requirement,
  not a promise to neutralize arbitrary foreign code that suppresses all logging.
- Harmony suppression is **off** by default instead of source **on**, preserves existing files,
  discards pending/suppressed buffers without replay, and never resets Harmony.DEBUG or other owners.
- Presentation/announcements/pseudo-translation use common inherited settings. Process capture
  and Harmony-write policy are documented global-only exceptions because they span startup and
  multiple saves. Filters, detail sizing, selection, counts and interval are transient diagnostics.
- At the S035 checkpoint, S094 Log After Def Error remained queued. Its subsequent
  [research and owned contract](2026-11-05-definition-diagnostics-research.md) now specify direct
  one-occurrence context enrichment through this channel, not self-patches or duplicate notices.
  Source differences and remaining implementation/acceptance are explicit; no runtime parity is claimed.
- No Thing-index dependency: use native retained diagnostic data and bounded presentation state,
  without another world scan, persisted runtime references or per-source cache framework.

## Open research and acceptance

| ID | Topic | Remaining work |
| --- | --- | --- |
| BL-Q1 | Effective native capture/admission | Trace current Brutal/Verse/legacy callback lifecycle and pre-output suppression/count control; establish minimal target docs, revalidation and protected startup-warning path before hooks |
| BL-Q2 | Providers and consumers | Inspect actual relevant binary/reflected consumers and exact co-installed logging providers, Harmony custom writers/buffers, Prepatcher and any RocketMan overlap without adopting foreign-patch suppression |
| BL-Q3 | UI, threading and composition | Verify native retained/copy/filter behavior, announcement grouping/eviction/thread dispatch, seven languages and future S094 composition; measure flood behavior and bounded resource use |
| BL-Q4 | Implementation and native acceptance | Honest-tier focused RED/GREEN, reviewed build, native top-entry/settings/copy/clear/notification workflows, admitted capture/file observations and package independence; all implementation tasks remain unchecked |

Semantic pass: diagnostics do not fix underlying errors or change research, costs, jobs, animal
behavior or item quantities. Explicit native pause-on-error is preserved; muted errors deliberately
skip ordinary error consequences and the setting must explain lost evidence. Required startup
warnings, unchanged file contents and absence of notification loops are acceptance conditions.
Documentation validation and independent review are tracked in the [progress ledger](specification-progress.md).
