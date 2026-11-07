# S093 — Labels on Floor (continued) research

Owner **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
Origin: replacement, P2-22. Inspected 2026-11-05. Public documentation first, then read-only selected
binary, public source and native decompilation. No foreign code executed; no live acceptance.

## Identity and evidence

| Input | Inspected identity |
| --- | --- |
| Source | Labels on Floor (continued), metadata author Falconne, current Workshop continuation iShot; `falconne.LabelsOnFloor`, canonical `falconne.labelsonfloor` |
| Installed root | `F:/Steam/steamapps/workshop/content/294100/3546003256`; LoadFolders 1.7 selects `/` and `1.7` |
| Metadata | Supports 1.5/1.6/1.7; Harmony required and load-after; no HugsLib dependency |
| Selected DLL | `1.7/Assemblies/LabelsOnFloor.dll`, assembly/file 1.7.5.0; adjacent `.old` file not selected |
| DLL SHA256 | `D5CDD90F850B4BF497F82534B9CC436BB2B185BE69922F1A522B287306DFF163` |
| About SHA256 | `01015D76189844711B56DF9CA8748E69367155791F8AFCD7E6EAF474C8197F68` |
| Public source | [iShotFT/LabelsOnFloor](https://github.com/iShotFT/LabelsOnFloor), commit `5e5b25fe528f672c7a3ea9182db74a77a8886b2f`; AssemblyInfo 1.6.4.0, only 1.5/1.6 packaged DLLs |
| Native game | 1.7.4998 rev1299; Assembly-CSharp SHA256 `FE9B615CF5918955AB750F9D627FB0082FD23C556BDFDEE410F88857C67A081B` |
| Ignored evidence | `artifacts/Refinement/megamod-spec-20261105/labels-on-floor/{binary,upstream,native,progress-renderer}` |

The linked public source is older than the selected 1.7 binary, but already persists per-entity
color, visibility and offsets. Unified editing, orientation/travel fields, CJK and overlap modes
are newer additions. Its separate rename windows and
old rename-button/constructor hooks are not evidence that the current binary contains those hooks.
Its README's VS2017/HugsLib guidance is historical. Selected binary behavior wins where they differ.
The public tree has GPL-3.0 LICENSE.txt. Installed CJK README credits Noto Sans CJK, SIL OFL 1.1.
No code, assembly, font or raster asset is selected for redistribution here; independent behavior
implementation and explicit future resource licensing/asset acceptance are required. Hashes identify
inspection inputs, not runtime compatibility predicates.

## Public intent

The [current Workshop description](https://steamcommunity.com/workshop/filedetails/?id=3546003256)
describes map labels, crop names, contextual customization, R editing and L visibility cycling. Its
1.7.5 notes include Classic/Smart placement, script fallback, screenshot hiding and gravship naming.
Its advertised performance/save compatibility is not measured or accepted by this investigation.
The first sharedfiles URL returned 429; the workshop/filedetails route loaded successfully.

## Selected binary inventory and observed static behavior

| Component | Behavior established by inspection |
| --- | --- |
| LabelsOnFloorMod / Main | Global static mod/settings facade; unguarded Harmony owner `LabelsOnFloor`; lazy label/mesh/placement resources; native map view and cutscene checks; inactive Draw calls SetDirty repeatedly |
| LabelMaker / RoomRoleFinder | Custom text first; otherwise room role, growing crop, other zone label, uppercase. Role None never eligible; generic Room only when customized; other roles eligible. Fogged rooms omitted |
| Label / LabelDrawer | Floor or MoteOverhead layering; global opacity also applied to custom color; transformed footprint; source culls by anchor cell rather than full visible footprint |
| LabelHolder | Actual room-zone footprint intersection, HideRoom/HideZone/ShowBoth; no same-kind collision policy |
| Designator_Rename / DialogFactory | Orders action and hover choose zone first, otherwise eligible room; no room chooser when a zone covers it. Orders action is added only while feature active |
| MapInterface input | R only without selected pawns; L cycles Visible/DrawOnTop/Hidden and writes global settings. No source-specific new bottom tab |
| Dialog_RenameAreaUnified | Paused/draggable window, local edit fields but direct entity-data preview writes; uppercase, trimmed on Apply; accepts length less than 28 UTF-16 code units; Enter Apply, Escape/Cancel revert |
| Dialog closing/reset | PostClose only refreshes; X/outside dismissal does not revert direct previews. Confirmed Reset clears name, color, show, offset and orientation in preview; Cancel can revert reset |
| Data adapters | Default/custom color, Default/Show/Hide, Auto/Horizontal/Vertical, X/Y offsets -10..10. Zone name preview writes Zone.label; saves old native label for reset. Native external rename does not by itself clear stale source custom text |
| Room records/manager | WorldComponent `roomLabels`; primary map+keyCell, transient RoomObject; first matching list entry wins merge; valid map keeps temporarily missing-room records; ship/local-cell references saved |
| Zone records/manager | WorldComponent `customZoneLabels`; native ZoneId plus map/ship, remembered VanillaLabel; cleanup every 300 ticks; identity lookup by native game-wide zone ID |
| Override serialization | Nullable color saved with white sentinel and reconstructed only when not white: explicit white loses its distinction. Zero offset versus unset becomes equivalent. Text/show/orientation have separate fields |
| MigrationHelper / legacy WorldObject | Schedules legacy room manager copy/removal on world finalize; copies only when current manager empty. No source-to-megamod migration contract follows from this |
| PlacementDataFinder* | Forced orientation first, but failed result falls through to configured automatic algorithm; per-entity visibility can override category settings. Room fog guard; Classic zone rejects any fogged cell; Smart zone lacks that equivalent guard |
| EdgeBasedAlgorithm / EdgeFinder | Bottom-row/left-column space comparison, horizontal ties; source uses edge counts and bounds, not guaranteed contiguous space. Room edges exclude walls/player buildings. Scale accounts for 0.4-cell total inset and omits below minimum |
| GridBasedAlgorithm / ChunkManager / ChunkScorer | 5x5 chunks, scores area cells, bottom/left edges, walkability and room buildings; bedroom bed-foot and hospital/prison door preferences; Growing and stockpile edge bonuses; Classic fallback. Dining scorer exists but is not registered |
| Chunk / Smart output | Fit uses chunk bounding dimensions rather than full footprint/holes; final inset scaling can fall below minimum. Shape cache keyed by ID/hash and cell count can survive equal-count changes without complete invalidation |
| LabelPlacementHandler | Native room/zone enumerations; dirty zones coalesced; map changes rebuild. General dirty clears placement caches, but ordinary dirty-zone refresh does not necessarily clear algorithm chunk caches |
| FontRegistry / GenericFont | Discovers Fonts/<name>/Font across all running mods; six known fallbacks. Atlas.json searched independently; fixed 16x16 Latin/Cyrillic mapping or legacy 69-column texture mapping, optional preview and metadata |
| FontRenderer / CjkFallbackFont | Selected font, different default font, then CJK glyph page fallback; per-char loop silently drops unsupported chars. CJK manifest supplies five 4096-pixel pages with 80-pixel cells; pages loaded lazily |
| MeshHandlerNew / FontMaterials | Cached text meshes per font, texture/color/opacity materials; cleanup hooks exist. Need bounded resources and language/provider generation identity rather than blanket unbounded retention |
| ScreenshotDetector | Native screenshot mode plus reflected ProgressRenderer.MapComponent_RenderManager.Rendering; type detection not package-gated. Mixed-case dictionary keys combined with lowercased lookups omit some displayed aliases |
| Gravship hooks | Prefix before TransferZones boards room anchors and intersecting stockpile/growing customizations; landing postfix remaps records and refreshes. Prefix has no success/failure transaction; offsets/orientation stay map-relative |

Source Smart weights are implementation evidence: bottom edge +20, left +15, all-walkable +5,
room with building -20, 2 per area cell, neighboring clear chunks times 2; bedroom bed-foot +15,
hospital door +10, prison door -15, growing +3 and stockpile edge +5. The owned spec preserves
preference direction and valid placement, not exact weighting or the misleading generic center
tooltip. These are layout choices, not game balance modifiers.

The selected patch inventory covers DrawDynamicThings; Orders resolved designators; low-priority
input; room MakeNew/Notify_RoomShapeChanged/Notify_BedTypeChanged/RemoveDistrict; Building_Bed
FacilityChanged; ListerBuildings Add/Remove; GatherSpotLister RegisterActivated/RegisterDeactivated;
Zone AddCell/RemoveCell, ZoneManager RegisterZone/DeregisterZone and crop changes; Map/World
FinalizeInit, ClearAllPlayData; gravship transfer and landing. Some class filenames retain old method
names, so filenames/Analyzer.xml are not authoritative target signatures. No selected current
Dialog_RenameZone constructor or RenameUIUtility button hook was found in that inventory.

## Settings, keys and defaults

The source's global keys are enabled, visibilityMode, selectedFont, defaultLabelColor, opacity,
showRoomLabels, showZoneLabels, showGrowingZoneLabels, showStockpileZoneLabels, maxFontScale,
minFontScale, maxAllowedZoom, hideLabelsInScreenshots, placementAlgorithm and overlapMode.
Initial/reset values are Visible, Classic, white, 75%, all four categories on, maximum 1.0,
minimum 0.2, Furthest zoom, screenshot hiding off, EdgeBased and HideRoomLabel. Missing-field
load instead uses JetBrainsMono and opacity 30; owned creation/load/reset consistently use Classic/75.
Bounds are opacity 1–100, maximum 0.1–5.0, minimum 0.1–1.0. Source validation is UI-only and
does not enforce minimum <= maximum. Source general zone-off destructively sets both subtype flags
false; owned settings retain them while disabled. Owned minimum/maximum use one compound setting
and one inheritance choice, so a global edit cannot form an invalid pair with half a save override.

Keybinding Defs initially R and L belong to the source LabelsOnFloor category. Owned bindings can
have distinct identities and preserve the gestures when available; no source keybinding identity
consumer was established. Per-label values and native zone names belong to contextual state, while
display defaults use the common global/save inheritance model. The small bottom-right mode control
and zone/room chooser are intentional navigation additions; no bottom tab is copied.

## Native and optional-provider findings

Native Zone allocates ID through Find.UniqueIDsManager.GetNextZoneID and serializes it. IRenameable
RenamableLabel reads label or baseLabel. TransferZones captures only ship-intersecting cells but
deletes the whole original stockpile/growing zone when any are transferred. Native MoveableStockpile
and MoveableGrowZone retain original ID/name/color and recreate zones with that ID at landing.
Consequently, do not invent split-zone ID collisions or a guaranteed departure remainder. Preserve
the native lifecycle while carrying label records. Room anchors use actual ship-local rotation.

Native UIRoot owns ScreenshotModeHandler; screenshotMode.Active is the source visibility input.
The [Progress Renderer Workshop page](https://steamcommunity.com/workshop/filedetails/?id=2010777010)
links [JonathanTroyer/Progress-Renderer](https://github.com/JonathanTroyer/Progress-Renderer), archived
May 2026. Read-only clone commit **faa380c19b1f88ac631468c266b3585629911eaa** identifies canonical
neptimus7.progressrenderer and a public bool Rendering getter on the named map component. Its
coroutine sets Rendering before temporarily switching to the capture map, then clears it after
capture. That snapshot's About supports 1.4–1.6, not proof of installed 1.7 support. No matching
installed About was found by the bounded Progress Renderer metadata search. Only this concrete
provider contract is a candidate adapter; the source's other alias strings remain unverified.

## Consumers, boundaries and deliberate deviations

An installed Workshop/local DLL text scan for LabelsOnFloor and the two current manager names
matched only the source's 1.5/1.6/1.7 assemblies. Installed text matches included Use This Instead
(Workshop 3396308787, Mlie.UseThisInstead) replacements.json recommending the continuation for
the same source package ID; this is recommendation metadata, not a callable API binding. The bounded
Atlas.json filename scan found only the source's six font providers. No external LabelsOnFloor
assembly/type/IFont consumer is established, though these searches are not proof none can exist.

Owned corrections are explicit: all-route edit cancellation; atomic stale-edit rejection; no
native-name preview mutation; coherent ordinary zone rename/reset; explicit-white persistence;
text-element-aware limit and visible missing-glyph markers; consistent defaults and validated scale
pairs; retained zone-subtype preferences; room access beneath a zone; inherited scoped display
mode; valid contiguous/whole-footprint placement; forced-direction fidelity; Smart fog protection;
equal-count invalidation; viewport footprint culling; deterministic merge/reset; successful-transfer
ownership rather than an unconditionally mutating prefix. Source coordinate-based room continuity
is retained with an explicit merge policy. Source shader/font layout bugs are not UI contracts.

Original source presence suppresses owned overlap without removing the source or consuming its
save keys. Before owned records with active custom-name overrides resume, current native names
changed during suppression replace their stale custom names and reset baselines; other appearance
remains intact. Appearance-only records retain automatic names/crops without creating a text override. This observes native
state rather than importing source records. No automatic migration, legacy WorldObject destruction, source package spoofing, font
facade or general screenshot compatibility claim. Use the owned Architect and zone paths directly;
no internal patch stack or private Thing cache. No source-supplied assembly enters the package.

## Static playability pass and remaining questions

The feature changes information presentation and explicit zone names, not production, resources,
research, work time, movement or storage constraints. Fog protection, truthful crop/room names and
current-target editing are the relevant information/reachability boundaries. Off-map/cutscene/input
guards and bounded redraw resources preserve ordinary play. This static pass is not visual quality,
frame-time evidence, live compatibility or the final balance/playability acceptance gate.

| ID | Remaining work | Current contract |
| --- | --- | --- |
| LFL-Q1 | Effective hooks, native rename/result transaction, fog/role/shape/language invalidation and resource teardown | Minimal per-patch targets, native authoritative reread, atomic edits, shared owned callbacks |
| LFL-Q2 | Room anchor/merge/reset, zone deletion, successful/failed/in-transit gravship persistence fixtures | Deterministic anchor records, native zone identity, no guessed transfer or silent data loss |
| LFL-Q3 | Exact installed optional capture provider and actual font/API consumer evidence | Native screenshots plus only admitted package/shape adapters; original overlap suppressed; no facade assumed |
| LFL-Q4 | Licensed six styles, real glyph coverage, editor/setting localization and live visual review | Seven baseline languages, explicit supported script fallback/missing markers, common settings and contextual state |
| LFL-Q5 | Focused TDD, reviewed-build causal workflows, bounded cache/performance and final playability evidence | Nothing implemented or accepted until root acceptance and asset gates are satisfied |

[Owning proposal](../../../../openspec/changes/specify-megamod-floor-labels/proposal.md),
[observable spec](../../../../openspec/changes/specify-megamod-floor-labels/specs/megamod-floor-labels/spec.md),
[design](../../../../openspec/changes/specify-megamod-floor-labels/design.md),
[tasks](../../../../openspec/changes/specify-megamod-floor-labels/tasks.md),
[coverage](../replacements/floor-labels.md), [progress](specification-progress.md).
