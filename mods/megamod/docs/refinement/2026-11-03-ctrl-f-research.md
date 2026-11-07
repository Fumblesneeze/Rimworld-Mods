# Ctrl F - Continued: source research and draft decisions

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`). S033, target **1.7 beta**.
Inspected **2026-11-03**. Research and future specification only; no gameplay implementation or live
acceptance. [Owned change](../../../../openspec/changes/specify-megamod-thing-search/proposal.md).

## Documentation first and identity

Read the [current Workshop description](https://steamcommunity.com/sharedfiles/filedetails/?id=3529520961),
[change notes](https://steamcommunity.com/sharedfiles/filedetails/changelog/3529520961), current
[source repository](https://github.com/MemeGoddess/RimWorld-CtrlF), then the consumed
[TD Find Lib description](https://steamcommunity.com/workshop/filedetails/?id=3529443295).
Ctrl F advertises new/resume/selection search shortcuts, manual/continuous refresh, detachable
results, library integration and Enter-to-first-result. Its change notes record 1.7 support on
2026-10-16 and a show/hide search-button option on 2025-12-11. Public comments about RimThemes or
FloatSubMenu are compatibility leads, not demonstrated owned compatibility or reproduced defects.

- Installed reference: `F:/Steam/steamapps/workshop/content/294100/3529520961`.
  Title **Ctrl F - Continued**, package **Memegoddess.CtrlF**, About author **Uuugggg**; current
  continuation published by Meme Goddess. About declares 1.4, 1.5, 1.6 and 1.7.
- Dependency/loadAfter: **Memegoddess.TDFindLib**. About's dependency Workshop URL is still the
  original library item 2895299310; the inspected continuation is **3529443295**. Match package and
  current installed contents rather than trusting that stale link.
- `LoadFolders.xml` uses root plus the matching version directory. Inspected selected 1.7 XML/keyed
  files and `1.7/Assemblies/Ctrl_F.dll`, assembly/file version **1.0.0.0**.
- Current source checkout: `artifacts/Refinement/megamod-spec-20261103/ctrl-f/upstream`, commit
  **6691fcfb1f7f7715ad98f299dbbf48c6eea86597**, committed 2026-10-17 +11:00, message `1.7`.
  All **14** selected source/project/About/load-folder/1.7 XML/keyed inputs agree with installed
  files after normalizing line endings/BOM; raw bytes differ. README still links original Ctrl F.
- Installed `LICENSE.txt`: MIT grant, copyright **[2025] [Alex Tearse-Doyle]**, with notice retention
  for copied substantial code. This project independently specifies/implements behavior; no source
  assemblies or art are copied. Preserve provenance, and reassess attribution if code reuse is proposed.
- `Ctrl_F.csproj` targets net10.0/1.7, uses Krafs publicizer/references and the continuation's TD Find
  Lib package. Wrapper source does not install Harmony patches. UI/helper/publicized access still
  needs current native shape checks in our implementation; this is not a claim that no guards are needed.

| Input | SHA-256 |
| --- | --- |
| Ctrl F About/About.xml | `CACF88EC8B96FC700BB0D595FA56501EBF60E813D513D47E1A96BE5522C53F70` |
| Ctrl F LoadFolders.xml | `A0D9033F65102A9DA23DAFB7E241B1EF997A04BE46266A0DC702088268F97FDB` |
| Ctrl F 1.7 DLL | `B363D2418BD892718996DF86FDEB271CCE3CD41D7CF302AEF34340187AB25480` |
| Current TD Find Lib 1.7 DLL | `A151E6D3AF87B4C02A344B1ED4F093D5E24A5199F9E33EBBAC74E38F6405E4CB` |
| Native 1.7.4995 rev309 Assembly-CSharp.dll | `56399B6144D9F1695F9C53D054DC82972F7AB35E68D1A19A7129F2F5860CCD21` |

Read all **eight Ctrl F C# files** including AssemblyInfo, project, selected metadata/Defs and English
keys. Decompile the selected DLL and consumed library read-only with ILSpy 11.2 to this slice's
`compiled-ctrl-f` and `compiled-library` artifact directories. The library hash **differs** from
the `1A9218...` binary inspected for S018 on 2026-11-02: current compiled consumed paths were read
again, rather than inheriting a claim about the old assembly. The whole library was decompiled
for access, but only the consumed search/editor/list/refresh/exchange primitives were analyzed here.
Its full predicate/add-on catalogue remains **S121**, not complete by transitive dependency.

## Wrapper behavior: source and compiled evidence

`CtrlFGameComponent`, `CtrlFSearchWindow`, `CtrlFListWindow`, `CtrlFThingListDrawer`,
`MainButtonWorker_ToggleCtrlFWindow`, `CtrlFReceiver`, `CtrlFRefresh`, Settings and helper types:

- `OpenCtrlF` defaults to **F**; the game component explicitly requires Control. Shift wins first
  and reopens; otherwise a new active query is created. Alt asks for selected-thing/zone criteria;
  no supported selection falls back to a focused empty name criterion. Plain new text search is
  not immediately evaluated; selected-object queries are. Ctrl+Shift+Alt therefore resumes.
- Single selected Thing supplies exact Def, and exact Stuff when non-null. Multiple selections
  produce distinct Def/Stuff alternatives (outer Any, inner All). No quality/HP equality is added.
  Native **RimWorld.Selector.SingleSelectedThing** is genuinely null unless exactly one object is
  selected; do not infer that its source same-Def check collapses a multi-selection's materials.
- Zone selection creates an exact-zone clause. The fallback casts other selected objects to Thing
  and dereferences without null protection. Native 1.7 permits Plan selection, so the unsupported
  selection path needs a safe owned fallback. This is a code-derived risk, not a reproduced crash.
- Editor and list are paired draggable/resizable windows (source initial 600x600 and 360x720).
  Keep-open is initially false; closing the editor closes a connected list. Closing the list closes
  the editor and removes that query's refresher. Right-click keep-open reopens the editor.
- Manual refresh sets changedSinceRemake before rebuilding. Continuous refresh is a **60-tick**,
  **non-permanent** library registration; paused tick processing does not refresh. It is initially
  off. SetSearch looks up a refresher for the incoming search, but does not remove a previous
  different search's refresher before replacing its drawer. Owned replacement/teardown must not
  leave orphan work. The compiled current library tick path has no per-refresh logging, unlike
  the installed source's debug logger call; do not claim runtime log spam from the source text.
- `GoToFirst` exists, but the wrapper's accept override is commented out and **absent from its
  compiled class**. The consumed base QueryDrawerWindow has closeOnAccept=false and no accept
  override. No active first-result caller was found. Restore the advertised behavior as an explicit
  owned correction, not a verified upstream Enter workflow.
- MainButtonDef **TD_Ctrl-F** is minimized, order199, validWithoutMap=true, with the custom worker;
  it toggles these popups. `MainButtonWorker...window` is static. The wrapper has no workspace
  save data or explicit game-switch reset; owned lifecycle must not reuse that static game context.
- Source Settings has just **ShowButton=true**, global key **TD.ShowSearchButton**. Query/window/
  refresh/separation controls are operational state; the source's visibility setting becomes the
  common inherited Show search control preference, applied to its relocated entry point.
- `CtrlFReceiver` registers an **ISearchReceiver** on static startup: Source/transferTag **Ctrl-F**,
  translated ReceiveName `TD.ViewInCtrlF`, CloneArgs.use, CanReceive only when Find.CurrentMap exists,
  Receive calls OpenWith. The import UI sets searches locked; Receive itself opens through OpenWith's
  default unlocked parameter. Owned incoming transfers consistently start locked, an explicit correction.

## Consumed library mechanics

Current library About: **Memegoddess.TDFindLib**, 3529443295, author Uuugggg, versions1.4–1.7;
requires **Memegoddess.TDSBugFixes**, 3529433984. That dependency is upstream, not justification for
requiring it in our owned query core.

- `QuerySearch` defaults Selectable/current-map. Listing flags distinguish Pawn, HaulableAlways,
  artificial buildings excluding rubble-producing rock, selectable plants, rock, other HaulableEver,
  All and Inventory. All/Selectable traverse known holder contents too. Specific categories require
  explicitly adding Inventory. Source sorts each map by **Def shortHash, Stuff shortHash, x+1000*z**;
  it does not sort by an understandable name. Source total is **sum(stackCount)**.
- `ContentsUtility` recurses through holder contents, skips undisclosed caskets outside god mode,
  and excludes direct inner objects of corpses/minified things from extra result entries. Holder
  roots are fog-filtered. The general candidate concatenation has no overall Distinct guard; owned
  shared enumeration explicitly deduplicates physical identity across overlapping traversals.
- `ThingQuery.AppliesTo` tries the wrapper, then the minified inner Thing/corpse inner Pawn if the
  first did not match, before applying include/exclude. `ThingQueryName` uses Label with
  **OrdinalIgnoreCase** substring matching; empty means unrestricted. Type query has an inclusive
  stack range. Stuff supports exact/any/not-Stuff/category and uses a construction plan's chosen
  Stuff. Zone uses PositionHeld/MapHeld, exact zone or any stockpile/growing/any zone.
- Exact zone reference resolution uses **zone label** per searched map. Missing references can
  disable criteria, and skipped disabled criteria can broaden results. Owned exact-identity bindings
  and explicit unavailable state deliberately differ; do not describe same-label resolution as safe.
- Root HeldQueries defaults All. Nested AndOrGroup defaults Any and supports positive anyMin.
  Disabled children are skipped; empty All passes and empty Any fails. Disabling because a reference
  is missing is not distinguished from a deliberate user disable by the filter traversal.
- Map scope is current/all loaded/explicit. Any CurMapOnly predicate forces current-map behavior;
  empty chosen-map clones can fall back to current map. Current map can be null in world view.
  Owned UI explains missing scope and does not silently retarget a required exact zone/map.
- RemakeList normally runs once per game tick unless changedSinceRemake is true. A broad catch
  around the map loop logs an error and then computes a total from any accumulated subset. Owned
  publication is all-or-unavailable. Manual refresh and reopen also need a real fresh boundary,
  including same-tick paused changes.
- Result drawer renders only visible rows, labels/icons, unit total, selected highlight, hover
  target highlight, native selection, drag and camera actions. **Compiled selection modifier is
  Shift**, whereas the library Workshop description says Ctrl. Double-click selects same Def on
  current map; Select all also restricts current map. Native selector currently caps at **200** and
  changes viewed map for a valid single cross-map selection. Owned bulk actions honor the current
  native cap rather than extending it. Source unspawned/unselectable rows navigate instead of select.
- Source left-click on an already selected item jumps; Shift toggles; right-click/drag jumps.
  Source Shift handling precedes right-click handling. Owned right navigation remains right navigation
  regardless of Shift, a small deliberate gesture clarification. Source hover behavior lacks an
  explicit off-map/holder explanation; owned rows show those locations clearly.
- `QueryDrawerWindow` exposes listing/match/map choices, clear, lock, library and import/export.
  Query edits call Changed. Ctrl+C/Ctrl+V are whole-query operations in its window handler; lock
  suppresses keyboard paste but does not suppress its import icon. Owned lock covers replacement,
  and explicit query-transfer controls avoid stealing normal focused text editing.
- SearchTransfer has Register/Deregister and concrete provider/receiver lists. ISearchReceiver
  consumes **TD_Find_Lib.QuerySearch** plus clone semantics, not a generic string. ClipboardTransfer
  uses Scribe XML for single query/group/library; library storage lives in TD Find Lib global
  ModSettings. CloneInactive drops map references; CloneForUse can retarget and evaluates. Full
  codec/catalogue/migration support is not proven by the existence of those public interfaces.

## Contracts, consumers and source boundaries

The bounded installed text search for Ctrl_F/Ctrl-F/Memegoddess.CtrlF in C#/XML/JSON/config found
the source itself, replacement metadata (3396308787) and an unrelated **Ctrl-F12** Hospitality
description. It found no direct external Ctrl_F type call; this is not a DLL-wide/reflection audit.
Replacement metadata identifies original **Uuugggg.CtrlF**, Workshop2895300634. Both identities
belong in the no-duplicate workspace policy; no package-ID spoofing.

TD Find Lib's transfer menu is an actual caller of registered receivers. S018's inspected
Everybody Gets One editor is a separate concrete ISearchReceiver participant (`TD.EGO` while its
editor is open). Preserve supported copy/receive meaning through an optional adapter, not by
duplicating the third-party library's assembly or depending on it for our own bill/search logic.
No external consumer of Ctrl F's MainButtonDef/keybinding/save key was established. Do not promise
binary compatibility, foreign saved-query import, automatic settings migration or universal modded
predicate support. Source and provider presence must be distinguished.

## Owned decisions and composition

Confirmed user intent: independent observable specification, own implementation, shared Ctrl+F/
hauling/construction index, no internal self-patches, UI classification and common settings.
Proposed defaults/corrections are normative in the draft and remain distinguishable from source facts:

- Keyboard shortcuts retained. Search is a current-play information view: toggle it through a
  small bottom-right control, with no copied MainButtonDef tab. Visibility is common configuration;
  entity/map/query choices stay in the workspace. Native keybindings are an explained global exception.
- Ordinary search never reveals fog just because god mode is on. Debug query/custom-code/engine
  listings belong to the future separately specified S121 debug surface; no ordinary cheat entry.
- Basic predicate/group/listing semantics are explicit here, and S018/S121 will use the same owned
  query values. Advanced filters, saved-search library CRUD/grouping/default examples and complete
  external codecs are S121 work, not omitted silently or marked implemented. This change specifies
  their workspace boundary; its dependent tasks remain unchecked until those contracts are ready.
- Source label/hash order, ambiguous count text, missing-clause broadening, partial totals, same-tick
  freshness, stale selections, orphan refresh and static cross-game state are not compatibility goals.
  Specify clear map/type/material/location ordering, separate object/unit totals and safe lifecycle.
- Root All/new-group Any/first threshold two retain inspected group defaults; basic type/material/
  exact-zone additions request an explicit choice rather than silently adopting source defaults
  such as wood or steel. Lock covers current-query edit/import/clear; explicit New search starts a
  new unlocked working copy. Closing a separated editor keeps the results view's refresh active.
- One candidate owner serves S033, S032 stored counts, S021/S125 hauling and relevant S017/S018
  production queries. Visibility is not a reservation; a stored-only count is not an all-map search.
  No click-to-search behavior is inferred from the handwritten arrow P1-25.

## Open research and verification

| ID | Topic | Remaining work; do not interrupt the user |
| --- | --- | --- |
| CF-Q1 | Exact native UI/admission | Resolve effective 1.7 input/window/bottom-right targets, focus/Enter behavior, source co-installation and later repatching before implementation; minimal semantic guards, not pinned hashes |
| CF-Q2 | Shared candidates/visibility/performance | Prove holder traversal/ordinary knownness, map lifecycle, per-consumer freshness and current criterion fields; benchmark combined search/count/hauling update work and memory |
| CF-Q3 | S121 catalogue/library/external exchange | Complete separate S121 source/library/codec work, inspect actual binary/reflected consumers and round-trip every claimed clause; no partial-import broadening or source identity spoof |
| CF-Q4 | Implementation and native acceptance | Focused honest-tier TDD, seven-language UI, settings/save isolation, paused changes, multi-map/contained/stale results, actual selection/camera workflows and reviewed-build acceptance remain pending |

All four are implementation/source/acceptance work, not requests to have the user explain an existing
mod. Semantic balance pass: this slice creates no resources, jobs, research bypass or capacity;
the material-count/search/hauling disagreement can be legitimate and is explicitly explained.
Native action restrictions and ordinary knowledge remain authoritative. No optimization or compatibility
claim is accepted from static inspection. Document review and strict validation are tracked in the
[progress ledger](specification-progress.md).
