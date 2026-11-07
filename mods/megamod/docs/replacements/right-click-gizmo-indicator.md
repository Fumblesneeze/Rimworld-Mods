# Right-Click Gizmo Indicator

## Identity and provenance

Owner **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
Origin **replacement**, queue **S110**. Source **CheaterEater.RightClickGizmoIndicator** by
CheaterEater, [Workshop3750529401](https://steamcommunity.com/sharedfiles/filedetails/?id=3750529401).
Inspected2026-11-05, About modVersion1.02, selected1.7 DLL version1.0.0.0, **7,680 bytes**, SHA-256
`CC34FDFCCBA080D5DA4980BFE9BE1219BA4D6BFDE4C255831324D772B0D53890`.

[Author source](https://github.com/CheaterEater-RM/Right-Click-Gizmo-Indicator/tree/557a05c5717a996aaf9bf1b65b5b649a1d92f90a)
contains the byte-identical1.7 binary and three inspected runtime files. MIT, copyright2026
CheaterEater, with explicit exception for RimWorld/separately licensed art. No code/assets/binaries
are copied in this documentation change. [Research](../refinement/2026-11-05-right-click-gizmo-indicator-research.md)
records public/installed differences, all source classes, eighteen native types, three optional
provider classes, source composition, exact manifests and bounded consumer evidence.
The game updated during this pass from1.7.4998 to1.7.5000. Both identities are retained, with all
eighteen inspected native decompilations and the menu-getter inventory unchanged on the pinned
newer input. This is scoped static evidence, not a compatibility acceptance claim.

## Behavior inventory

All rows **specified, not implemented** in the
[owned change](../../../../openspec/changes/specify-megamod-right-click-indicators/).

| ID | Native input → observable behavior | Rules / edge cases | Evidence / contract |
| --- | --- | --- | --- |
| RCGI-01 | View enabled command with a menu → native-style marker | Includes menu with disabled choices; excludes disabled command/empty/non-menu action | Source draw and native routes; [availability](../../../../openspec/changes/specify-megamod-right-click-indicators/specs/megamod-context-menu-indicators/spec.md#requirement-mark-available-right-click-menus-on-ordinary-commands) |
| RCGI-02 | Change selection/state/modifiers → current marker | Same-kind instances differ; actual representative/group route; reload/map freshness | Owned correction; [current context](../../../../openspec/changes/specify-megamod-right-click-indicators/specs/megamod-context-menu-indicators/spec.md#requirement-follow-the-current-button-and-selection) |
| RCGI-03 | Hover/view → no action or spurious message | Actual clicks keep messages/costs/actions; no suppression or speculative input | Source risks/native routing; [passive behavior](../../../../openspec/changes/specify-megamod-right-click-indicators/specs/megamod-context-menu-indicators/spec.md#requirement-discoverability-must-remain-passive) |
| RCGI-04 | View normal/compact/native-marked buttons → clear single cue | Preserve native build rules, labels/counters/tints/scaling and real retextures | Source/native geometry; [presentation](../../../../openspec/changes/specify-megamod-right-click-indicators/specs/megamod-context-menu-indicators/spec.md#requirement-fit-the-existing-command-presentation) |
| RCGI-05 | Owned/optional command has actual options → same indicator policy | Direct owned facts; Hide/Restore counts; custom renderer limitation; original owns overlap | S107/IABT boundary; [composition](../../../../openspec/changes/specify-megamod-right-click-indicators/specs/megamod-context-menu-indicators/spec.md#requirement-compose-with-actual-providers-without-inventing-settings) |
| RCGI-06 | Incompatible target/failure → affected cue unavailable, button retained | Semantic guards, bounded warning, compatible foreign patches, state cleanup | Owned correction; [admission](../../../../openspec/changes/specify-megamod-right-click-indicators/specs/megamod-context-menu-indicators/spec.md#requirement-admit-compatible-behavior-and-isolate-failures) |

## Interactions and composed result

S107 quick-storage commands already have contextual menus and source-native markers. Owned S107
and other selected commands supply current capability through direct code and use one marker path;
no self-patch or separate map cache. S036 Architect presentation and all command owners retain
their ordering, filtering, grouping and actual actions. Do not enumerate menu actions to paint a cue.

Optional `Taggerung.IAintBuildingThat` can add menu choices to inherited getters and gate them on
modifiers. An admitted adapter must honor that current behavior. Real Hide/Restore options count,
without a new blacklist. This reference does not add its complete hiding feature to the replacement
list or establish beta compatibility. The original indicator, if active, owns overlap; ours is
inactive with warning, without fixing or removing its cache/suppression behavior.

## UI audit and relocation

| Source surface | Purpose | Owned destination |
| --- | --- | --- |
| Native-style corner marker on Command buttons | Reveal existing right-click menus | Same contextual ordinary/compact button, no separate hitbox |
| Allowed-area special case | Reveal picker opened through normal input | Same area command; no synthetic click to discover it |
| Existing build/dropdown/provider cue | Existing alternatives | Preserve single original cue and native availability rules |
| DevMode startup log | Diagnostics | Bounded compatibility logs, no new product control |

No bottom tab, toggle, debug button or settings menu. No global/save inheritance exception: there
is no configuration setting. Markers are transient presentation, not saved gameplay state. Native
visual reference only; no artwork selected here. Seven baseline language/UI-scale workflows remain
required even if this feature introduces no translated strings.

## Contracts consumed by mods and saves

| Consumer / evidence | Contract | Preserve / unsupported |
| --- | --- | --- |
| Native and optional command providers | Current options/input/group path, disabled state, actual callbacks, messages and draw state | Preserve; read only admitted safe capability facts |
| Source public Analyzer.xml | Private profiling names, one stale HasMenuOnce entry; file absent from installed payload | No demonstrated consumer/facade; not a runtime dependency |
| Installed raw consumer scan | Only own About and two version DLLs matched | No proven external binding; scan limitations retained |
| Saves/settings | No source custom state or keys found | No source migration; no whole-megamod uninstall-safety claim |

## Implementation and coverage

**Implemented: nothing.** RCGI-01–06 await executable target specs, focused tests, implementation,
independent review and real native/visual evidence. Deliberate source corrections: current instance
and modifier state; inherited-getter support; no stateful probe or Messages suppression; existing-cue
deduplication; clear-corner placement and guaranteed drawing-state cleanup. No unmeasured zero-cost
claim. No custom mechanitor or arbitrary foreign renderer adapter, provider blacklist or alternative
texture preference is selected. Those are not silently claimed as covered.

Harmony required when implemented; no hard DLC/optional-menu-provider dependency. No XML changes;
future XML Extensions use requires a hard dependency and content/shape guards. No copied upstream
assembly, bundled Harmony, runtime art or Gateway dependency. Unknown provider routes leave the
button usable under its original rules and omit only the unsupported enhancement.

## Verification and acceptance

Documentation review/strict validation and RCGI-Q1–3 are recorded in the
[ledger](../refinement/specification-progress.md). Static method/metadata analysis is not live proof.
Future evidence must show actual command → visible cue → right-click → original menu/action result,
plus dynamic/disabled/grouped/modifier/state changes, unchanged messages, existing cues, optional
provider/failure combinations and real compact/scaled/translated UI. Retain causal screenshots,
reviewed build identity, normal-config hashes and cleanup. All implementation tasks remain open.
