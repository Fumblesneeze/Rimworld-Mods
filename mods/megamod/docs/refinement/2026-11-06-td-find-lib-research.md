# S121 — TD Find Lib Continued research

Owner **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`; target **1.7 beta**.
Replacement **S121/P2-51**. Documentation-only source evidence for the
[owned draft](../../../../openspec/changes/specify-megamod-query-library/), **not implementation
or a compatibility claim**. Independent draft reviews and strict validation are complete below.
[Ledger](specification-progress.md) · [Predicate inventory](2026-11-06-td-find-lib-catalogue.md).

## Public documentation first and exact inputs

Read [Workshop 3529443295](https://steamcommunity.com/sharedfiles/filedetails/?id=3529443295)
and its [changelog](https://steamcommunity.com/sharedfiles/filedetails/changelog/3529443295)
before inspecting the installed library. The browsing service could not retrieve those pages;
separate public HTTP reads succeeded and are retained locally. The public description presents
reusable queries, a saved-search library, clipboard exchange and custom inspection, rather than
an independent gameplay feature or a bottom-bar tab. Its examples are discovery aids, not a
promise that every example remains correct in the installed build.

The [public repository](https://github.com/MemeGoddess/RimWorld-TDFindLib/tree/6718073a19d3c7c8dc412bee28e488dab2be8e64)
was pinned at `master` commit **6718073a19d3c7c8dc412bee28e488dab2be8e64**, dated
2026-11-02. The installed DLL is authoritative for compiled behavior. **Its Git blob differs from
the DLL in that public commit**, so source correspondence is not binary provenance. The retained
source/metadata/XML files individually match the pinned tree; corresponding installed files were
also compared. The MIT license identifies copyright 2025 Alex Tearse-Doyle. Preserve attribution;
independently implement the specified behavior and do not redistribute upstream DLLs or artwork.

| Input | Identity |
| --- | --- |
| Display name / authors | TD Find Lib - Continued; Uuugggg, continuation Meme Goddess |
| Package / Workshop | `Memegoddess.TDFindLib` / `3529443295` |
| Installed root | `F:/Steam/steamapps/workshop/content/294100/3529443295` |
| Declared game versions | 1.4–1.7 |
| Required source dependency | `Memegoddess.TDSBugFixes`, Workshop 3529433984 |
| Main assembly | `1.7/Assemblies/TD_Find_Lib.dll`, 371,712 bytes; assembly/file version 1.0.0.0 |
| Main SHA256 | `A151E6D3AF87B4C02A344B1ED4F093D5E24A5199F9E33EBBAC74E38F6405E4CB` |
| Installed main Git blob | `55ff7f36e7eae234423135d2b917e1c098d161b0` |
| Pinned public main DLL blob | `5263d7932bcf9079aeecfb78b01e17e45459b85d` — different |
| CAI adapter | `1.7/ModSupport/CAIFOW/Assemblies/TDFindLib_CAIFOW.dll`, 4,608 bytes, assembly 1.0.0.0 |
| CAI SHA256 | `2A5763AA8A756CBDC5FAEB964C5B536E189F9601277D989B393E3AD0AA52486C` |
| LTO adapter | `1.7/ModSupport/LTOColonyGroups/Assemblies/TDFindLib_LTOColonyGroups.dll`, 5,632 bytes, assembly 1.0.0.0 |
| LTO SHA256 | `F429164C8E9DF5C32A654D44B51336B3689A162220BFC5DB043DD19474B45947` |
| Native game | 1.7.5000 rev1275; root `Assembly-CSharp.dll`, 15,636,480 bytes |
| Native SHA256 | `D55AA8E47FA3C5463CA85B0ECCD51F195B4BBCD04F56DA3AF21C2F6688A81254` |

Evidence: ignored `artifacts/Refinement/megamod-spec-20261106/td-find-lib/`, including
17 primary input identities, seven additional consumer/provider inputs, five primary and two optional
provider public responses, 72 pinned source/metadata files, full main
and two optional assembly decompilations, native `Verse.GenTypes`, and the 127-record on-disk
query/preselection inventory. No inspected assembly was executed or loaded into a game.

## Loaded scope and dependencies

For 1.7, LoadFolders selects root and 1.7 plus the Royalty, Ideology, Biotech and Odyssey data
folders. Those expansion filters are compiled into the main assembly; their visibility additionally
depends on DLC activation. The library's source makes unavailable DLC entries developer-only,
which is not a suitable way to admit an unavailable expansion in the owned ordinary query system.

Conditional folders are LTO Colony Groups (`DerekBickley.LTOColonyGroupsFinal`), CAI FOW
(`Krkr.rule56`), and Float Sub-Menus (`kathanon.FloatSubMenu` or its `_steam` identity).
No 1.7 FloatSubMenu adapter folder/DLL is present in the inspected installation, despite the
LoadFolders declarations. Settings checks for the loaded adapter assembly and disables that
optional presentation if it is absent. The 1.6 adapter is not evidence of a usable 1.7 adapter.

The on-disk RealFOW definition is **not selected by 1.7 LoadFolders**. Its compiled main-library
predicate simply returns true. Do not claim working Real Fog of War support from the package's
load-after hint or that orphan definition. CookieCutter files are author templates, not loaded
features. The source alert query is commented out; no active Query_Alert definition was found.

The inventory has 127 on-disk query/preselection records: 90 root 1.7 entries, 12 Biotech,
10 Ideology, eight Royalty, four Odyssey, one each CAI/LTO, and one unselected RealFOW.
Thus 126 declarations are potentially selected, including five shortcuts and conditional modules;
this is **not** a claim that 126 ordinary menu entries are visible at once. Obsolete, developer-only,
inactive DLC and generated category-helper distinctions matter.

The inspected main and optional module code does not install its own Harmony patches. Referencing
Harmony in a project is not a patch inventory. The owned implementation should first compose directly
with S033 and S018, without adding patches merely to reproduce a library's structure.

## Boundaries with already drafted work

- S033 owns map search, physical result identity, listing choices, ordinary knowledge boundaries,
  wrapper/inner matching, Boolean groups, query error behavior and the Ctrl+F workspace. Its basic
  name/type/material/zone filters and temporary refresh are not redefined here.
- S018 owns population-sensitive production and its query editor/consumer. A population query is
  bill gameplay state. S017's bill controls and S014's linked bills compose with that consumer.
- S121 must cover the remaining predicate catalog, saved-library editing, complete persistence,
  library/group/query exchange, source samples and debug inspection. Referencing S033 alone does
  not complete this entry.
- The shared physical Thing index remains the single candidate source for search, hauling and
  relevant counts. Pawn-specific eligibility is checked at use time. This library does not justify
  separate caches for each filter, feature or source port.

## Editor, library and persistence findings

The source's global settings contain `onlyAvailable=true` and `QueryCustomSetup=false`, warning
acknowledgements and the saved groups. Only-available is inverted while Shift is held; at the main
menu all choices are available. In the generic dropdown, availability filtering only takes effect
when the full option list exceeds ten entries. Available values are collected from **all loaded
maps**, independent of the current query's selected maps. Helpers traverse known held contents and
exclude trade ships, unknown caskets and native fog in ordinary mode. Several individual option
collectors do not repeat their predicate's hidden-condition checks; the owned boundary must cover
suggestions as well as results.

`QueryCustomSetup` is a startup prewarm of reflection suggestions, not a gameplay rule. The owned
design can build that information lazily and omit a player setting for an implementation detail.
If only-available remains configurable, it uses the common global/default/save/Use-global path.
Saved queries, group ordering, selected operands and map/entity bindings are reusable user data or
contextual gameplay state, not configuration values with inheritance.

Source library operations:

- Create/rename/delete groups and searches; reorder groups and searches; move searches between
  groups; Ctrl-drag clones instead of moving. Group and search delete prompt unless Shift bypasses.
- Opening an existing saved search edits an inactive clone. Confirmation replaces the chosen saved
  entry. Source saves can also ask overwrite/cancel/save-as-copy on same-name search collision.
- A newly created search is added before its editor opens. Copy suffixes are not proven unique;
  group import appends groups. These paths need an explicit owned collision/cancellation contract.
- Closing the library writes settings. Empty loaded libraries acquire a default saved-search group.
  `SearchGroup.ExposeData` drops null entries after load; unknown query/Def losses must not silently
  broaden an owned query or erase the user's original data.
- Source global clones omit actual map references; a use-clone with empty chosen maps substitutes
  the current map. The owned S033 contract instead requires explicit rebinding and no silent scope
  broadening or same-label substitution.
- Many source references use translated labels: areas, policies, ideologies, groups and category
  helpers. A language change, duplicate label or renamed entity can therefore alter resolution.
  Owned stable identities and deliberate rebinding are a necessary correction, not package parity.

The source library can also show registered current-game refreshers and open their owning UI. Its
game component schedules each query by `TicksGame % period`, with no positive-period check. It saves
only permanent refreshers, but writes a collection and reads a deep RefreshGroup; its refresher tag
uses deep serialization for a string. None of those shapes is accepted as an owned persistence design.
S033's manual/off-by-default temporary 60-tick refresh contract remains authoritative. Any additional
owned consumer scheduling must retain that consumer's lifetime and never turn saved templates into
background alerts or automatic commands.

## Exchange and external contracts

The actual public source interfaces are `ISearchProvider`, `ISearchReceiver`, `ISearchGroupReceiver`
and `ISearchLibraryReceiver`. Providers advertise None/Single/Group/Library and a display/source
name. Receivers advertise display/source name, clone arguments and `CanReceive`, and accept the
appropriate search/group/list. The registry adds each implemented interface and removes those entries
on deregistration. `ISearchReceiverFloat` and `ISearchGroupReceiverFloat` add optional submenu output.

`SearchStorage` offers only appropriate provider shapes, clones before delivery and excludes matching
source tags. Comma-separated tags suppress intersecting sources. An other-source tag beginning `=`
is restricted to the named requesting source; null matches nothing. Empty non-null tags can throw
because the source indexes character zero. Owned adapter inputs need validation.

`QuerySearch.CloneArgs` distinguishes Save/Edit (both inactive and map-reference-free) from Use
(active with optional explicit map/maps and optional rename). SearchTransfer alone is not an
assembly-compatible facade. External mods compiled against `TD_Find_Lib` need that actual library
identity and its object graph; our package must not impersonate or bundle it. The narrow S018/S033
adapter should use the real provider when present and supported, and stay absent-safe otherwise.
Current actual consumers were rechecked below; do not infer compatibility with every advertised
consumer from the interface names.

Clipboard representations use roots `TD_Find_Lib.QuerySearch`, `TD_Find_Lib.SearchGroup` and
`List_TD_Find_Lib.SearchGroup`. Recognition merely checks the starting text. Loading delegates to
Scribe and can force-stop a different active Scribe operation. Error messages include the entire
payload. The replacement needs bounded, non-instantiating validation and atomic conversion of the
whole selected transfer. It must not execute arbitrary imported class names, abort save loading,
partially import unsupported filters or log whole imported content. Export/import into an external
receiver must preserve every supported operand and make unsupported content explicit.

## Samples and optional modules

`About/DefaultSearches.xml` provides five groups and 40 named searches: six general samples,
15 suggested alerts, five Everybody Gets One examples, eight expansion examples and six custom
inspection examples. First-use code clones them into the global library; these are queries, not
automatic enabled alerts. Merely displaying the library must not create jobs, bills or notifications.

The samples contain exact Def IDs, English category labels, empty chosen-map bindings, literal
slider floats and defaults dependent on omitted fields. Some names overstate what their predicates
do: “Low Power/Fuel” combines both component tests with All; “Good Loot” relies on an omitted
inventory-direction field whose load default differs from a fresh query. The complete
[40-example analysis](2026-11-06-td-find-lib-samples.md) now records expressions, scopes and defaults.
The owned library/debug capabilities separately specify34 ordinary and6 debug examples, supplied
only through explicit Add examples; exact imported source definitions are not silently rewritten.

CAI's selected adapter returns true only when provider FOW is enabled and its fog grid reports the
held cell visible; a missing grid is treated as visible. Disabled FOW returns false, not universally
visible. The owned ordinary visibility rule must never leak hidden entities when a provider cannot
be evaluated. LTO's adapter matches exact selected group membership; its None path requires a
successful group lookup and zero PawnGroups. It saves/resolves the selected group by name, so
unassigned behavior and stable binding need explicit treatment rather than copying that lookup test.

## Corrections to carry into the observable draft

These are explicit proposed choices in the owned draft, **not runtime acceptance**:

1. Known ordinary information remains known-only even when god mode is enabled. Hidden fields,
   fog and arbitrary custom inspection belong to a separate top-debug entry point, never production
   population counts or ordinary saved queries.
2. Every saved operand survives copy/edit/export/reload. In particular, source permit faction and
   ready-only flags, and ideology-xenotype preference mode, lack their own clone/save overrides.
3. Health severity should inspect any matching visible occurrence, rather than only the first.
   Health category matching must not reveal hidden hediffs. Selectable severity/part-count semantics
   require concrete UI and repeated-body-part examples.
4. Category choices for policy-item predicates must still answer questions about pawns' policies.
   The source reuses a Thing category helper that instead compares the candidate Thing's own type.
5. Nearby uses a square radius 0–10, includes the center/self and forces its lower bound to zero in
   the editor. It is not an ignored user-selectable annulus. Preserve the declared square metric,
   clip to map edges and respect knowledge boundaries; do not invent a minimum-distance feature.
6. No malformed/unresolved binding, exception, unsupported filter or incomplete multi-map evaluation
   may silently become a passing predicate or a partial authoritative result. Missing optional
   providers leave saved definitions intact and visibly unavailable.
7. Library mutations need explicit atomicity, copy independence, collision choices and cancellation.
   Global template management can work at the main menu; game-specific previews/binding edits cannot
   touch a previous save. No startup recreation of deleted examples.
8. Native helpers and provider APIs are observation inputs, not permission to bypass bills, jobs,
   medicine, hauling, reservations or visibility. No duplicate per-port cache or self-patching stack.

## Consumer and editor closeout

Current Ctrl F Continued3529520961 DLL hash is
`B363D2418BD892718996DF86FDEB271CCE3CD41D7CF302AEF34340187AB25480`, matching the S033 decompile.
Its `CtrlFReceiver` uses tag `Ctrl-F`, Use clone, CurrentMap-dependent CanReceive and
`MainButtonWorker.OpenWith`. Current Everybody Gets One3530806680 DLL hash is
`51259BBC102B2566066CFC956A9B7C6E7F74CB686F0433B33E2FF4A927C8C433`, matching S018's decompile.
`PersonCounterEditor` uses tag `TD.EGO`, Use clone and open/close registration lifetime. The source
editor changes its original query live; revert imports listing/children but not the entire map/name
state. Owned S018 already requires whole working-copy Apply/Cancel. Bill→query storage belongs to
the source world component; an empty chosen map list is filled from the bill's map upstream.

Bounded installed About and DLL literal scans found only TD Find Lib, Ctrl F and Everybody Gets One
roots for `TD_Find_Lib`/TDFindLib. The repeated retained binary scan had35 matching paths across those
three roots. This is not proof against reflection, differently named references or uninstalled mods.
Evidence includes `consumer-provider-inputs.json` and `consumer-scan-summary.json`.

Remaining editor checks found a normal-lock import bypass (`DrawIcons` checks `permalocked` rather
than `locked`), no unique copy-name guarantee, and creation/import before final name resolution.
Owned atomic/working-copy/lock requirements deliberately correct these. Source drag prevents moving
or copying a nested group into itself/descendants; retain that property. `MaskFilterDef<T>.Clone`
correctly copies both mask lists; do not misreport it as a source shallow-copy bug. Generated
Accessible categories group prospective outputs as textiles/raw/manufactured/body parts/other.
No other unregistered compiled ordinary feature was found beyond generated category helpers.

## Optional provider availability and research review

The actual installed [CAI5000 continuation3673768803](https://steamcommunity.com/sharedfiles/filedetails/?id=3673768803)
has package `Krkr.rule56`, declares1.6 only and selects1.6/CombatAI.dll; its metadata requires Harmony
and Prepatcher. Installed [Float Sub-Menus2864015430](https://steamcommunity.com/sharedfiles/filedetails/?id=2864015430)
declares through1.6 and requires Harmony. Neither proves1.7 compatibility. Bounded metadata inspection
found no actual package matching the LTO/RealFOW providers, only dependency/order mentions. These
provider pages and metadata are retained; no provider implementation or loaded behavior is accepted.

Independent provenance review checked all17 primary hashes,72 pinned source files, Git blobs and
five primary public responses: no issues found. Independent catalog/sample review confirmed122
queries plus5 preselection definitions and found three documentation corrections, all applied:
Zone includes stockpile/growing/any extras with an unreselectable source Any-zone default; To Convert
defaults to Current map; Class-type availability scans all loaded maps. These reviews concern source
facts, not the subsequently authored observable draft.

## Draft review and remaining implementation work

Three independent document reviews covered ordinary/expansion predicates, library/debug behavior
and S018/S033/foundation composition. They found three actionable draft issues: preserve inclusive
thought-stage ranges, list colony-specific prisoner/slave choices separately, and express sample
fraction units explicitly. All were fixed and focused re-reviews found no unresolved issue. The
composition review found no issues. These are documentation reviews, not runtime evidence.

Typed `openspec_validate` passed **143 items, zero failed** after the corrections. The four
capabilities contain **34 requirements and59 scenarios**, with **36 unchecked implementation tasks**.

- Resolve TDL-Q1–6 in their future implementation slices; they retain effective native/provider
  admission, repeated-part semantics, bounded persistence/inspection and real native acceptance.
  The source catalogue and40 sample analyses are complete; these are not questions asking the user
  to explain existing mod behavior.

The reviewed-draft count is now **122/137**, with **516 recorded question rows**. The historical
resolved CH-Q1 mention is not an additional open question row. All implementation and verification
tasks remain unchecked. Next source is S122 De-generalize Work.

No product code, tests, build/install, game process, Workshop content, normal configuration/save,
art, commit or publication was changed by this research checkpoint.
