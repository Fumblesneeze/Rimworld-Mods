## Context

Sole owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), 1.7 beta.
S127 is a specification-only replacement of RimHUD. Read the [proposal](proposal.md), both capability
specs and [source research](../../../mods/megamod/docs/refinement/2026-11-06-rimhud-research.md).
The installed root assembly is 1.17.5 and advertises game versions through 1.6. Source inspection
and the native 1.7 `InspectPaneUtility` extraction are design evidence, not beta acceptance.

## Goals / Non-Goals

**Goals:** detailed selected-pawn inspection, trustworthy warnings, native contextual actions,
scoped configuration, reusable layouts, supported external widget/preset formats, localized failure
isolation and direct owned composition. Preserve consumed contracts where they are demonstrably
supportable, explicitly document bindings that are not.

**Non-goals:** gameplay-stat changes, a second pawn database, a separate bottom tab, wholesale
upstream code/assets, upstream package impersonation, automatic foreign-config migration, native
tutorial suppression, global unpatch/reset on failure, or acceptance of Multiplayer/1.7 from metadata.
S128 Passion Icons and S137 medical-condition filtering remain separate changes.

## Decisions

### Preserve native UI responsibility

Prefer retaining the native `IInspectPane` control/tab paths and inserting an owned summary over
copying `InspectPaneUtility` or all `MainTabWindow_Inspect` behavior. The inspected 1.7
`InspectPaneOnGUI` calls `pane.DoInspectPaneButtons(rect, ref lineEndWidth)`; skipping that call is
why upstream needs separate Useful Marks and SmashTools button adapters. Preserve native extension
execution exactly once, then add only genuinely missing owned shortcuts in a coordinated region.
Do not draw a second copy of a native medical/rename/response button. All callbacks capture the
identified pawn/map/policy/hour, revalidate at invocation and call native commands/setters with
their permission rules. Use available tab instances and visibility, not just type existence.

Docked summary and geometry form one admitted group; floating summary is separately admit-able
only if native pane access remains usable. Treat specialized pawn subclasses with custom inspection
conservatively; Vehicle Framework's inspected adapter explicitly asks to bypass RimHUD for its
VehiclePawn. Do not infer support from `is Pawn`. Use package/type admission for recognized optional
providers, and default native inspection for unadmitted specialized panes.

Source fixed coordinates are preferences, not unbounded draw rectangles. A single layout pass must
consider current screen/UI scale, native tab extents, text measurement and overflow/scroll access.
Letters use native bundling/actions only when there is an actual visible-HUD overlap. Do not copy
the source path that returns without drawing any letters when calculated excess is zero, or the
Lazy references to a previous game's letter lists. Do not suppress `TutorOnGUI`.

### One selected-subject snapshot, with fresh authority at action time

Read native pawn state on the UI thread. Share the selected subject/snapshot across HUD widgets and
other owned views where it is genuinely the same derived information. The 500 ms source cadence is
wall time, not ticks. Selection/action/config changes invalidate immediately; map/game teardown
clears subject, log, menu and widget references. Tooltip calculations use the same subject identity
and current native permissions. A cached display never authorizes an action.

No map-wide pawn/Thing scan per widget. Cargo reads S021/S126's actual held-stack authority; other
Thing facts use the shared index only if needed. Health/needs/skill queries do not justify a second
map cache. S034 supplies structured number formatting, S103 need overflow supplies truthful values,
S007 the compact native Health content, S128 the future passion presentation and S132 the future
Gear presentation. Each composes through owned calls, never Harmony patches against owned code.
Retain aggregate thought effects from native calculation instead of multiplying one instance by
count. Preserve native hidden-information checks and real self-tend efficiency.

### Common settings and transactional layouts

Use the foundation's common definitions, effective-value resolver, persistence and row renderer.
Each docked/floating layout is one collection-valued setting; per-entry editing operates on a deep
copy for that scope. Presentation is one three-choice setting (integrated, floating/modified pane,
floating/native pane), replacing the source's two coupled flags so mixed global/save inheritance
cannot form an invalid state. Dimension/text/color rows remain independent settings. Global reset
restores built-ins in the named sections; save reset switches those sections to inheritance while
retaining dormant values. Neither reset changes the reusable preset library. Preset application
explicitly resolves the target scope and included rows as one validated transaction, preserving
omitted fields. Do not share mutable HudArgs/template references between inserted entries.

Global named-preset storage is an explicit collection-library exception, not an excuse for all HUD
preferences to be global-only. Preset identity includes origin package and stable local name/ID;
display names cannot choose a file. User exports remain inside the owned configuration area with
filename validation, atomic replacement and recoverable destination backup. Provider files are
read-only. Screen-fit clamping changes the current rendered bounds, not stored preferred values.
Operational show/hide is save view state; diagnostics are temporary developer-only state.

Preserve source `Preset`, `Docked`, `Floating`, group/row/widget IDs and documented optional sizing,
`Targets`, `FillHeight`, `DefName`, `BarColorStyle`, `TextSizes`, `Version`, `Requires` meanings for
supported imports. `Requires` source alternatives are OR package IDs, but also validate needed
content. Missing/unsupported widgets remain inert identifiable nodes. An import previews omissions
and normalizes to the owned schema; it never executes XML code or writes Workshop content. The
source's omitted-size reset and first-other-mod early-return behavior are not selected contracts.

### External API and actual consumers

The published v1 API has these callable shapes (nullable annotations do not alter CLR identity):

| Form | Required content/callable contract |
| --- | --- |
| `RimHUD.CustomNeedDef` | `NeedDef needDef`; optional static `string GetTooltip(Pawn)`; color style |
| `CustomBarDef` | Static `(string,string,float,float[],Func<string>,Action,Action) GetParameters(Pawn)`; tuple order label/value/fill/thresholds/tooltip/onHover/onClick |
| `CustomValueDef` | Static `(string,string,Func<string>,Action,Action) GetParameters(Pawn)`; label/value/tooltip/onHover/onClick |
| `CustomSelectorDef` | Static `(string,Func<string>,Action,Action,Color?) GetParameters(Pawn)`; label/tooltip/onClick/onHover/background |
| `CustomWidgetDef` | Static `bool OnDraw(Pawn,Rect)` and `float GetMaxHeight()`; false means no occupied content |

Source common fields are `apiVersion` (missing/0 defaults to 1 with warning), `defClass`, label and
defName, plus four bar color styles. The wiki promises Regular for custom bars and Small for
values/selectors. The inspected bar field has only `[DefaultValue(Regular)]`, no initializer, and
enum zero is Small; native XML construction does not turn that metadata into an initializer.
Select the published Regular default as an explicit correction, with Small for value/selectors.
The source
selector's Build swaps hover/click arguments and its widget omits the tooltip; implement documented
semantics instead. Admit static parameters/return tuple shapes and finite/bounded output before use.
Contain exceptions at prepare/draw/tooltip/hover/click boundaries and restore GUI state in finally.
Do not emulate failure exceptions by shutting down the entire product or rewriting config.

Prefer a narrow format/callback adapter. Before promising unchanged external XML type resolution,
prove Def-loading phase and type identity with a minimal separate provider fixture; a private
provider scan that duplicates loaded Defs is not acceptable. If a separately loadable owned facade
is needed, admit it only when original RimHUD is absent, before affected Def resolution, and ensure
no duplicate types/assemblies. This is an implementation research gate, not a fabricated resolved
binary-compatibility claim. An upstream package-gated XML file may not load without `Jaxe.RimHUD`,
and compiled references to `RimHUD.dll` need exact binding evidence; never spoof that package ID.
No installed custom-v1 Def/provider-preset consumer was found in the scoped text/path search;
published API compatibility still warrants representative provider fixtures.

| Actual consumer | Inspected binding | Owned disposition / limit |
| --- | --- | --- |
| Readable Numbers `7h0m4s.RimworldReadableNumbers`, 3762441264 | Package-gated `RimHUD.Extensions.TextExtensions.ToDecimalString(int,int)` postfix | S034 direct structured formatting; no obsolete formatter patch/facade for owned code |
| Vanilla Skills Expanded `vanillaexpanded.skills`, 3400246558 | `SkillValue` constructor/GetColor and Theme color access; passion indicator transpiler | S128 direct admitted passion-data mapping; no claim of private IL-layout parity |
| RimHUD Passion Icons `DanZinagri.RimHUDPassionIcons`, 3829500081 | Declared `Jaxe.RimHUD` dependency and optional VSE assembly | Separate S128 source analysis; no package dependency satisfaction claim |
| Useful Marks `Andromeda.UsefulMarks`, 3506573327, 1.7 assembly | Package AND assembly `RimHUD`, then `InspectPaneButtons.Draw(Rect,IInspectPane,ref float)` postfix | Preserve its native inspect-button route once; prove actual marking workflow; no old assembly-binding claim |
| Vehicle Framework `SmashPhil.VehicleFramework`, 3014915404, inspected 1.6 assembly | Package-gated patch of RimHUD inspect prefix, bypass for VehiclePawn | Native specialized-pane fallback; exact 1.7 adapter evidence still required |
| SmashTools, inspected VF-bundled assembly | Package-gated RimHUD buttons postfix invoking `IInspectable.DoInspectPaneButtons` | Preserve native extension path; inspect selected provider/version before claiming support |
| GUI Performance Patch, prior retained research | Reflection on `GUIPlus.GetTextSize/GetTextHeight`; other probe paths | Private performance hooks are not re-created; no measured optimization or exact-version compatibility claim |
| Orassans 1660622094, legacy 1.3 compatibility source | Hard reference to obsolete `RimHUD.Data.Models.PawnModel.GetRace` | Legacy private target unsupported; expose truthful race in owned identity display |

Pawn Rules and Multiplayer are source adapters, not proven present functional consumers. Resolve
their canonical IDs and current entry points before enabling them. Do not repeat display-name
detection, unconditional foreign `HideGizmo`/`ReplaceFoodSelector` writes or the source `Enabled`
initialization defect. Admitted Pawn Rules navigation can expose its rule chooser alongside native
food policy without hiding the provider's controls. Multiplayer is deferred; a future separately
specified adapter would need all owned mutations, not merely the five upstream methods. No initial
MP implementation or acceptance is implied.

### Semantic patch admission

No target document is claimed implemented by this design. Each future patch needs its stable-ID
target document, executable guard, rollback and startup diagnostic; draft candidate groups are:

| Group | Candidate native surface | Minimal required semantics / fallback |
| --- | --- | --- |
| Docked inspection | `InspectPaneOnGUI`, `PaneSizeFor`, `PaneWidthFor`, `MainTabWindow_Inspect.PaneTopY`, necessary tab geometry | Same subject/rect space, native buttons/contents retained once, visible/hidden tabs honored, size/position consistent; reject atomic group to native pane |
| Floating display | `MapInterfaceOnGUI_AfterMainTabs` or admitted equivalent | Callable UI event with valid map selection and coordinate phase; postfix observation may need existence/call shape, not exact body |
| View toggle | `PlaySettings.DoPlaySettingsGlobalControls` | WidgetRow/world-view contract and operational-state isolation; no duplicated settings window |
| Tooltip/genes geometry if necessary | Relevant active tip or Genes sizing path | Only owned tooltip IDs/subject and actual pane-relative sizing consumer; do not replace every float 165 |
| Letter avoidance | Native letter layout/bundle/action path | Real live list, every letter reachable, no suppression on zero overlap; otherwise retain native letters |
| Lifecycle | Map switch/game load/clear callbacks | Actual owning state lifecycle; a skipped original cannot be assumed to clear state |
| Optional provider | Specific package plus types/members/Defs | Exact required callback/result/permission shape; isolate failed adapter to native/read-only fallback |

Validate effective code when another patch can change required behavior, not only original IL.
Transpilers validate their incoming stream on every invocation and return it unchanged on failure;
do not speculatively run foreign transpilers. Accept unrelated changes/compatible owners. Unknown
or ambiguous prerequisites fail closed with one startup warning per skipped enabled patch, atomic
rollback only of newly installed owned hooks and no `UnpatchAll` on foreign owners. Supported later
repatch invalidation must re-admit or withdraw the affected owned group. XML changes, if later
necessary, require XML Extensions package/content/shape guards and additive mutation by default.

## Risks / Trade-offs

- **Large configurable UI:** document validation cannot prove scaling/accessibility. Retain actual
  main-menu/global/save, long-label, screen-size and seven-language native workflows for acceptance.
- **Historical source defects:** findings are inspected control flow, not reproduced live reports.
  Test source-derived behavior and intentional corrections separately; do not preserve a bug as API.
- **Compatibility facade timing:** unchanged XML/binary loading may require a separate admitted
  facade. Resolve early with an honest isolated provider run; publish unsupported bindings instead
  of claiming universal replacement or adding a fake package.
- **Untrusted display callbacks:** no sandbox is claimed for third-party mod code. Narrow admission,
  captured context, GUI restoration and exception isolation protect our renderer, not all game state.
- **Performance:** finite selected-pawn work and shared facts are design choices, not benchmark
  results. Measure selected/hidden HUD, repeated selection, custom callbacks and shared consumers.

## Migration Plan

No runtime or data changes in this slice. Later implementation starts with default inherited
configuration, safe native fallback and explicit source-config/preset import only. Import backs up
the destination, parses and validates without mutations, commits atomically and verifies the result;
failure restores it. Preserve foreign inputs and unknown owned fields for recovery. On removal,
native pawn policy/save semantics remain native; owned presentation preferences may remain inert.

## Open Questions

RH-Q1 records the completed source inventory, not runtime readiness. RH-Q2–RH-Q6 in the refinement
ledger retain exact 1.7/native geometry, external provider/facade resolution, multiplayer/Pawn Rules,
screen/localization measurements and cross-feature acceptance. Do not ask the user to explain
upstream behavior; resolve source/runtime questions during their owning implementation slice.


## Initial support scope — decision2

Multiplayer and Combat Extended adapters, synchronized sessions, CE-only controls/properties and
provider acceptance are deferred. Any source CE/MP implementation details above are retained research
for a future scoped change, not initial work. Implement and accept the single-player non-CE contracts.
Ordinary save/load of colonies started with megamod remains required.
