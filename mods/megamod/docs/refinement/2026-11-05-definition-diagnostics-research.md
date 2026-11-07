# S094 — Log After Def Error (Continued) research

Owner **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
Origin: replacement, P2-23. Inspected 2026-11-05. Public documentation first, then all selected
source/DLL behavior and native read-only decompilation. No foreign code executed or live acceptance.

## Identity and evidence

| Input | Inspected identity |
| --- | --- |
| Source | Log After Def Error (Continued), metadata ordpus / continuation Mlie; `Mlie.LogAfterDefError`, canonical `mlie.logafterdeferror`, Workshop 3550944748 |
| Original | ordpus, Workshop 3291238448; canonical `ordpus.logafterdeferror` in installed replacement metadata |
| Root/selection | `F:/Steam/steamapps/workshop/content/294100/3550944748`; no LoadFolders.xml found; matching standard `1.7/Assemblies` folder |
| About | Supports 1.5/1.6/1.7; Harmony required/load-after; modVersion and Manifest version 1.7.1 |
| Selected DLL | LogAfterDefError.dll, assembly 1.7.0.0, file 1.7.1, Release; informational `1.0.0+7fcf77776356d7f66b7d629bf099b1c732832a3d` |
| DLL SHA256 | `3295F97EEB8E1828DF055273556BD3FD0454757EE9EE78DD65A7EFE7F5C82A80` |
| About SHA256 | `D4F0874D6ECC988F9347E7DF56B124276BD57C5BB22A83E373D6D7D52EBF1976` |
| Public repository | [emipa606/LogAfterDefError](https://github.com/emipa606/LogAfterDefError), commit `da32ba86e330ed5f4cf4982fa52c65d5a96603a7`; packaged 1.7 DLL byte-identical to installed selected DLL |
| Source | Four C# files plus csproj; relevant methods agree with the selected decompilation |
| Native game | 1.7.4998 rev1299; Assembly-CSharp SHA256 `FE9B615CF5918955AB750F9D627FB0082FD23C556BDFDEE410F88857C67A081B` |
| Ignored evidence | `artifacts/Refinement/megamod-spec-20261105/log-after-def-error/{upstream,binary,native}` |

Installed LICENSE.md is MIT, copyright Mlie 2020; selected assembly metadata credits ordpus 2026,
MIT. Retain provenance if any permitted source is reused, but this work specifies independent owned
behavior and copies no source DLL into a product. The source project targets net10.0, RimWorld ref
1.7.4990-unstable and Harmony 3.0.0-rc.3; its BeforeTargets Clean/Build kills all RimWorld.exe processes.
That foreign project was **read only, never built or executed**. The ancillary VersionFromManifest.dll
supplies a settings version label and is not required by the owned feature. Hashes identify evidence,
not compatibility admission.

## Public intent versus selected implementation

The [continuation Workshop page](https://steamcommunity.com/sharedfiles/filedetails/?id=3550944748)
describes adding definition, mod and XML-file information after parsing diagnostics. Its October 28
author response says the reported 1.7 failure was fixed. The [original page](https://steamcommunity.com/sharedfiles/filedetails/?id=3291238448)
describes the same locator purpose. Public safe-add/remove and support statements are not runtime
acceptance evidence. The current author also distinguishes definition conversion from configuration
and unrelated assembly errors.

The selected binary actually hooks **Log.Warning(string) only**, emits a separate ordinary
Log.Message before the warning, and includes the warning text plus mod display name/path. It does
**not** capture the defName promised by the public example and does not augment Log.Error. These are
facts from all four matching source files and selected decompilation, not inferred from the title.

## Complete source inventory

| File | Actual behavior |
| --- | --- |
| DirectXmlLoader_DefFromNode.cs | Targets DirectXmlLoader.DefFromNode and DirectXmlToObjectNew.DefFromNodeNew. ThreadStatic currentDefLoadDepth/currentLoadingAsset. Prefix saves previous asset, replaces it only when new asset is nonnull, increments depth. Finalizer decrements with Math.Max and restores prior asset. Public TryGetCurrentLoadingAsset succeeds for positive depth/non-null asset |
| Log_Warning.cs | Prefix returns for disabled or blank text, or no active asset. Gets loadingAsset.mod/RootDir/FullFilePath; uses string.Replace to remove root text, then Log.Message with `[Def Error]:` + original warning + newline + mod name/path. No Def identity, Error hook, accepted-event check or own source-message capture exception |
| LogAfterDefErrorMod.cs | Reads settings, unguarded PatchAll owner `ordpus.logafterdeferror`, reads manifest version; one mod settings checkbox and optional gray installed-version label; no bottom/debug/gameplay controls |
| LogAfterDefErrorModSettings.cs | Static Enabled=true, global key `enabled`, Scribe default true; no save override or other configurable parameter |

Language keys cover settings title, Enabled and Installed mod-version. Installed translations include
English, French, German, Chinese Simplified and Traditional; owned seven-language support remains
required. No source Def/PatchOperations, jobs, gameplay state, texture-driven control or data cache
is needed for this feature. VersionFromManifest is a packaged source helper, not an owning behavior.

## Native paths and consequences

- LoadedModManager.LoadAllActiveMods creates Mod classes before LoadModXML/Combine/ApplyPatches and
  ParseAndProcessXML. Default parsing calls DefFromNodeNew; `legacy-xml-deserializer` selects the old
  DefFromNode. Both receive the current XML node and its assetlookup value, which can be null.
- Both loaders skip non-element/abstract definitions, consult inheritance and parse the actual Def.
  Invalid types log ErrorOnce; caught conversion exceptions log Error with filename context. The
  new loader generates field parsers and can report errors during conversion. Preserve native
  skip/null/fallback/exception outcomes; attribution does not repair them.
- ParseHelper.ParseIntPermissive attempts an integer, otherwise parses/casts a float and logs
  `Parsed ... as int.` as a Warning. This proves the public fractional-integer example is a warning,
  not evidence that the source currently observes all errors.
- LoadableXmlAsset has readonly mod/name/fullFolderPath, with FullFilePath combining folder/name.
  Its constructors log XML-read warnings before Def conversion and can have no mod for generated
  XML. Cross-reference validation happens later. These are distinct stages, not stale-context cases
  to annotate with the last converted definition.
- Log.Warning/Error first respect PreventLogging (native cap or LogLock), then write through the
  Brutal logging category and native side effects. Once methods use usedKeys before forwarding.
  Source's prefix runs before that ordinary admission and its extra Message has independent policy;
  under a custom source-composed cap it can take the final slot before the warning. This consequence
  is a static inference, not a live reproduction.
- Threaded callbacks build LogMessage objects and queue off-thread messages for later main-thread
  draining. Source context must be captured before deferred presentation, never looked up afterward.
  Native LogMessage.CanCombineWith compares text and severity; LogMessageQueue retains 1,000 rows
  and caps consecutive repeat count at 99. A retained row's details cannot represent two different
  definitions if context was discarded before combination.
- PlayDataLoader.HotReloadDefs re-enters LoadAllActiveMods(hotReload:true) through a queued native
  long event, then cross-reference and other stages. Only conversion-scoped diagnostics get context.

Source's thread-local frame restoration handles ordinary nested known assets, but a different
nested definition with null loadingAsset inherits its parent's asset. The owned design explicitly
keeps that source unknown. No shared world data or duplicate Def/Thing index is justified: context
is a small immutable per-invocation snapshot, discarded after reporting/scope exit.

## Consumers and direct composition

Bounded installed Workshop/local text searches for LogAfterDefError and ordpus.logafterdeferror found
the source's metadata/translations/README and Use This Instead (Workshop 3396308787) replacement
metadata. Its mapping replaces `ordpus.logafterdeferror` with `Mlie.LogAfterDefError`; that is a
recommendation, not an API binding. A corresponding DLL string scan found only the source's 1.5,
1.6 and 1.7 assemblies. No external consumer of TryGetCurrentLoadingAsset, source static Enabled,
assembly identity or the exact extra-message format was established. These searches are bounded,
not proof that no other consumer exists.

S035 owns native diagnostic inspection and capture controls. S094 enriches one admitted original
Warning/Error event through direct owned composition, preserving original evidence, severity,
once-only identities, mute/limit accounting and native effects. It does not patch our owned logger
or port the source's uncounted/independently filtered white-message channel. Distinct definition
contexts remain distinguishable in retention; identical text/context uses native repeats. Required
startup warnings and limit notices retain their separate protected semantics.

Original or continued LADE suppresses owned overlap with an explanation. Other independent S035
features can remain active if admitted. S094 does not require S035 presentation to be active; native
logging is sufficient with an admitted reporting boundary. Unknown external logging suppression is
not bypassed or declared compatible merely because this source used a prefix. No source package
spoofing, global settings import, log upload or source assembly dependency.

## Deliberate corrections and remaining work

The owned observable contract adds the missing definition name/kind and stable package identity,
covers ordinary accepted Errors as well as Warnings, and keeps context within one occurrence.
It preserves missing provenance, avoids inherited outer-source guesses for distinct nested nodes,
uses actual root containment instead of string replacement, does not assign causal blame, and
retains original diagnostics when annotation fails. Global-only scope is intentional because these
startup/process diagnostics precede and span saves; it still uses the common settings mechanism.

Static playability pass: no costs, research, yields, jobs, availability or parsed values change.
The relevant boundaries are truthful information, unchanged failure handling, accessible native
diagnostics, no surprise uploads and no recursive log storms. Final native evidence must include
actual conversion followed by player log selection/copy and inspected context; test-generated Log
calls, a marker or a log file alone do not prove this feature's source attribution and UI workflow.

| ID | Remaining work | Contract |
| --- | --- | --- |
| DDI-Q1 | Exact effective converter/reporting/file/trace/deferred callback and reload semantics | Minimal per-patch admission, immutable scoped context, atomic groups and ordinary fallback |
| DDI-Q2 | Nested/generated/inherited/patched/exception/concurrent and S035 policy fixtures | Accurate unknowns, one occurrence, preserved once/LogLock/mute/cap/notice/announcement behavior |
| DDI-Q3 | Actual consumers/providers, global settings/localization and final native acceptance | No facade or compatibility claim without evidence; real conversion → native inspect/copy/file evidence on reviewed build |

[Proposal](../../../../openspec/changes/specify-megamod-definition-diagnostics/proposal.md),
[spec](../../../../openspec/changes/specify-megamod-definition-diagnostics/specs/megamod-definition-diagnostics/spec.md),
[design](../../../../openspec/changes/specify-megamod-definition-diagnostics/design.md),
[tasks](../../../../openspec/changes/specify-megamod-definition-diagnostics/tasks.md),
[coverage](../replacements/definition-diagnostics.md), [progress](specification-progress.md).
