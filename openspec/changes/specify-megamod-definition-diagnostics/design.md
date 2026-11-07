## Context

Owner **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
S094/P2-23. [Research](../../../mods/megamod/docs/refinement/2026-11-05-definition-diagnostics-research.md)
pins the selected 1.7.1 file/1.7.0.0 assembly, matching public source and native 1.7.4998. No runtime
code, target admission or compatibility acceptance is delivered by this change.

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

Each genuinely new nested conversion replaces current context even with a null asset; restore the
outer frame afterward. The source instead inherits a nonnull outer asset across null nested inputs,
which can misattribute a different generated definition. A proven forwarding wrapper for the same
node/asset can share the logical identity, but only one context section is attached to the accepted
occurrence. Handle entry failure, skip-original paths, exceptions, stack mismatch and reload cleanup
without suppressing the original exception or retaining a dangling frame. Do not log from cleanup
through an unguarded decorated path.

### Enrich one accepted event, preserving original diagnostic content

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

### Common settings with an explicit exception

One definition, `diagnostics.definitionContext`, defaults true and is global-only through the
shared settings path. Startup parsing precedes any save and a process log interleaves shared loading
evidence; a per-save override would ambiguously change process-wide capture decoration. Explain
that reason on the common row. No separate ModSettings implementation/static source Enabled field,
save migration or per-pawn/entity state. Edits affect subsequent occurrences immediately, including
a later native reload, without restarting/resuming logging or deleting previously enriched records.

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

Canonical `mlie.logafterdeferror` and `ordpus.logafterdeferror` suppress this owned feature's overlap.
Do not remove source patches or spoof either package/public type. The source's public
TryGetCurrentLoadingAsset API has no established external installed consumer; no facade is selected.
S035 stays independently owned by the same product: call its accepted-event/capture policy directly,
never patch our own logger. If S035 presentation is inactive, this feature can still use the admitted
native path. An external logger remains subject to exact effective-path admission, not blanket
compatibility based on its name. Neither original provider nor Better Log is required.

No XML mutation is needed: this is an observer of conversion, not a PatchOperation or repair pass.
Any later declarative patch would require its own XML Extensions guards and dependency. No game
world/Thing cache, source DLL, foreign process runner, shared runtime mod or Gateway dependency.

## Risks / Trade-offs

- **Origin mistaken for blame** → explicit current-definition wording, known/unknown fields and
  no causal patch claims; preserve original details for real diagnosis.
- **Native logging backend changes** → semantic pre-output admission and focused trace/file/count
  evidence; fall back to native logging rather than reconstructing events.
- **Context capture outlives the scope** → snapshot immutable metadata at reporting time, scoped
  finalization and no long-lived XML graphs or cross-thread last-asset field.
- **Source parity differs** → document warning-only/extra-Message source behavior and the deliberate
  broader severity coverage/one-event correction; no unproven source parser consumer is promised.

## Migration Plan

No source settings, logs or save data are imported or rewritten. The new global field uses the
documented enabled default when missing/invalid. Turning it off or losing admission changes only
future decoration; already captured log records remain intact. No files are deleted or uploaded.

## Open Questions

DDI-Q1 retains exact effective converter/reporting/trace and deferred-callback admission on the
implementation build. DDI-Q2 retains inherited/patched/generated/nested/exception/concurrent and
once/mute/limit fixtures with direct S035 composition. DDI-Q3 retains original/external-provider
consumer checks, settings/localization and scoped native acceptance. Record findings without
interrupting sequential refinement or declaring static logs to be accepted behavior.
