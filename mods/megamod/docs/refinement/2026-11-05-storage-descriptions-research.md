# S098 — Storage Stats in Description research

Owner **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
Origin replacement P2-27, inspected **2026-11-05**. Documentation and read-only source analysis only;
no upstream execution/build, game launch, ordinary configuration edit or gameplay acceptance.
[Owned change](../../../../openspec/changes/specify-megamod-storage-descriptions/) separates intentions,
observable behavior, design and unchecked implementation work.

## Published behavior and provenance

Read the [Workshop description](https://steamcommunity.com/sharedfiles/filedetails/?id=3740185419)
before code: it advertises generated storage statistics for building descriptions and replacement
of prewritten statistics such as Reel's Expanded Storage. It credits ferny and Taranchuk for C# and
lists Harmony as required. Direct page retrieval initially returned HTTP 429; the same Steam page's
search-indexed description supplied the public text. User comments about ASF settings and bedding
are investigation leads, not verified runtime defects or authority for the implementation below.

| Input | Inspected identity |
| --- | --- |
| Installed root | `F:/Steam/steamapps/workshop/content/294100/3740185419` |
| Name / author | Storage Stats in Description / ferny; published C# credit Taranchuk |
| Package | `ferny.StorageStatsinDescription`, canonical `ferny.storagestatsindescription` |
| Advertised support | About 1.6 and 1.7; no LoadFolders.xml, only a 1.6 assembly/source folder |
| Selected DLL | `1.6/Assemblies/StorageStatsinDescription.dll`, assembly/file 1.0.0.0 |
| DLL SHA256 | `4E8828464343A741B25CC207619175F10569C1EF7440315D987E1FEC98BF1E11` |
| About SHA256 | `E6991DBCF86AF9072E4A125AC115B6048B29F2D8F3B763A0E935EF2CD14F3C24` |
| Native input | 1.7.4998 rev1299; Assembly-CSharp SHA256 `FE9B615CF5918955AB750F9D627FB0082FD23C556BDFDEE410F88857C67A081B` |

Native ModContentPack.InitLoadFolders selects the closest suitable version folder when no exact
folder or load-folder declaration exists, then Common/root. Thus 1.6 is the inspected selected
runtime input for this package on 1.7; this is not a claim that the build was run successfully.
Source project targets net48 with a 1.6 game reference and Harmony package reference. About only
load-orders after Core/DLC/Harmony and declares no hard modDependencies, unlike the Workshop list.
Compiled active code has no Harmony use or patch registration; selected assembly references include
game, Unity and System.Core, not Harmony. Do not inherit a dependency from a label alone.

Read both source files, project/assembly metadata, English keys and every compiled behavior type.
Compiled behavior agrees with the shipped source; no public commit is supplied by the installed
metadata. No license file or grant was found in that inventory. Independently implement the behavior;
source availability is not permission to copy code/art. Decompilation and native/provider reads are
retained locally under `artifacts/Refinement/megamod-spec-20261105/storage-descriptions/`.

## Exact source behavior

`Startup` has a StaticConstructorOnStartup that iterates every ThingDef and assigns
`def.description = ProcessDescription(def, def.description)`. There is no per-feature guard, saved
entity state, Harmony patch, XML patch, bottom tab or map/Thing cache. Exceptions are not isolated
per definition by this source loop.

`ProcessDescription`:

1. Returns null immediately when original text is null.
2. Finds either literal `<color=#E5E54C>Storage stats:</color>` or its unclosed-header variant.
   If found, keeps only the prefix before the first occurrence, trimming trailing whitespace.
   This occurs **before** testing whether the definition is storage, so all following unrelated
   paragraphs can be removed even from a nonstorage definition. It is not bounded block replacement.
3. Classifies a definition as storage if building metadata exists and either its thingClass
   implements IStoreSettingsParent, fixedStorageSettings exists or defaultStorageSettings exists.
4. Computes total as integer `building.maxItemsInCell × size.Area`; per-tile value is the same field.
   It does not resolve nonuniform cells, instance quality, specialized holders or provider limits.
5. Picks fixed filter first, otherwise default filter. Missing filter means Everything. For each
   root child category it includes the category if **any** descendant ThingDef passes Allows(def).
   If zero root categories or all root categories match, it again says Everything. Partial category
   coverage is not disclosed and item-instance restrictions are not inspected.
6. Appends a colored localized heading and three lines for total, per-tile capacity and allowed
   items. The source calls the total Stack size total, although it calculates stack positions, not
   individual stack size or item quantity. The built text is installed once at static startup.

Null text omission, empty-as-Everything, blanket classification, potentially overflowing rectangle
capacity, partial-category overstatement and tail deletion are source-derived findings, not
reproduced game failures. Reinvoking the public function on its own generated heading can append
another block because the generated heading differs from the old literal it removes.

## Settings and public contracts

`StorageStatsinDescriptionSettings.titleColor` and Scribe key `titleColor` default to **#E5A84C**.
The source global settings window uses an unrestricted TextEntry and its Reset button writes
**#E5E54C**, a different yellow. No validation, explicit refresh or global/save inheritance exists.
Because the generated descriptions are already stored, subsequent edits do not rerun generation.
Only English keyed labels are shipped. No storage control, operational toggle or debug UI is added.

Public surfaces: `Startup.ProcessDescription(ThingDef,string)`; the public settings class, field
and ExposeData; public Mod class/static settings field, constructor, settings renderer and category.
The source has no external assembly/Def save identity other than global ModSettings. A completed
bounded ordinary-string `rg -a -l -i` scan of installed `.dll/.cs/.xml` for the namespace/package
matched only this package's own About, source and assembly. No external consumer was found in that
scope; UTF16/reflection and uninstalled consumers are not disproved. No facade or package alias is
selected, and no automatic settings migration is promised.

## Native 1.7 evidence

- **Building_Storage:** native slot cells are occupied cells; fixed parent settings fall back to
  EverStorableFixedSettings, while editable settings can come from a StorageGroup. PostMake copies
  defaults into editable policy. That policy is not the building's complete physical acceptance.
- **Building_Bookcase:** a separate Building subtype implementing IThingHolder and storage interfaces,
  with ThingOwner<Book>, MaximumBooks = maxItemsInCell × area and actual Book/capacity checks.
  Thus an interface-only classifier is too broad, but excluding every non-Building_Storage is too narrow.
- **BuildingProperties:** maxItemsInCell defaults to 1. ResolveReferences resolves fixed filters and
  copies fixed settings into missing defaults. A bare default is not proof of unlimited storage.
- **ThingFilter:** Allows(ThingDef) checks allowedDefs membership. Allows(Thing) additionally evaluates
  the inner minified item, hit points, quality, special filters and anomaly book risk. Broad category
  labels are not complete item acceptance contracts.
- **Presentation:** Designator_Build.Desc returns entDef.description. Thing.DescriptionFlavor
  returns def.description; ThingDef.DescriptionDetailed separately caches expanded text.
  Dialog_InfoCard calls StatsReportUtility, whose DescriptionEntry(Def/Thing) uses def.description
  or thing.DescriptionFlavor with hyperlinks. DrawStatsReport caches entries, so a current UI
  contribution needs correct context and targeted refresh, not just a startup rewrite or an
  unrelated detailed-description postfix.

All of these are static native reads. Exact effective runtime semantics, finalized filters and
cache behavior with active providers require implementation-stage admission and native evidence.

## Optional boundary checks

These are narrow compatibility reads for S098, not claims of complete analysis or replacement of
the optional mods.

| Provider | Evidence and implication |
| --- | --- |
| Reel's Expanded Storage, `reel.expanded.storage`, Workshop 3237638097 | Its explicit loadFolders.xml declares only 1.5/1.6, so native version fallback selects the 1.6 content on 1.7 before considering directory fallback. Selected XML includes `ReelStorageVerticalWeaponRack`, a two-cell ASF weapon rack with maxItemsInCell 8 and a yellow Storage stats block describing 16 total, 8 per tile and weapons. The exact named lines are a candidate bounded legacy grammar, not permission to delete all later text or every matching heading. |
| Adaptive Storage Framework, `adaptive.storage.framework`, Workshop 3033901359 | Selected 1.6 DLL plus shipped source: ThingClass.TotalSlots sums initialized cell limits; MaxItemsInCell is an override, quality can affect limits and CurrentSlotLimit is a distinct current limit. StatDrawEntries describes nonuniform per-cell data. A raw rectangle product is insufficient. Definition/instance projection and configuration invalidation remain SSD-Q2. |
| Relevant Stats In Description, `mlie.relevantstatsindescription`, Workshop 2692669482 | Selected 1.6 binary includes ShowStorageSpace=true and calculates raw rectangle capacity when fixed/default storage metadata exists. Designator_Build_Desc.Postfix can replace the entire result when UseTooltip is false; alternative extra-GUI routes exist. Its description contribution overlaps directly, but other stats must remain. |
| Soft Warm Beds (Continued), `mlie.jptsoftwarmbeds`, Workshop 3006469889 | Selected 1.6 Patches/Patches.xml adds fixed/default Textiles filters to beds. CompMakeableBed implements IStoreSettingsParent and ITab_Bedding derives from ITab_Storage. This proves consumption-specific settings can trigger the source's metadata classifier without making the bed a general storage shelf. |

Read-only provider DLL identities:

- ASF: `28DAA37ADE4144CAD2B7669EDFDA2201F9C0E8E99D4639853131E066026935B9`.
- Relevant Stats: `4F6448955BB2243CA5585EEDF77188FC47E22389ADA3F85424DA7AAFD05B7A2F`.
- Soft Warm Beds: `0EEF4040D40F8AF4C44B7A7F920B4D318A8D01D1DC69BD8F2EC153D9CB0D245A`.

The selected Reel's input is
`F:/Steam/steamapps/workshop/content/294100/3237638097/1.6/Defs/ThingDefs_Buildings/WeaponRack/LargeLocker_ThingDef.xml`.
Its SHA256 is `BCB0F6CD60BFAD421D3F25A3A5E5D9DA4F1783A84A2CC79DFF2A089B6801CED0`.
The initially inspected 1.7 copy is byte-identical, but is not the selected version under the current
load-folder declaration. This attribution correction does not change the capacity/block findings.

## Semantic and balance comparators

These are inspected source/native inputs, not finalized loaded-Def or live gameplay evidence.

| Comparator | Capacity unit and base evidence | Constraints to preserve |
| --- | --- | --- |
| Core Shelf | 2 × 1 footprint, 3 stacks/cell → 6 stack slots | Fixed exclusions include chunks/plants/buildings and large corpses; defaults are narrower editable policy |
| Core ShelfSmall | 1 × 1, 3 stacks/cell → 3 stack slots | Same inherited storage restrictions; not three individual resources |
| Core Bookcase | 2 × 1, 5 books/cell → 10 books | Books-only native holder and actual capacity check |
| ReelStorageVerticalWeaponRack | XML 2 × 1, 8/cell, advertised 16 stacks | ASF model and finalized/provider configuration can supersede a raw XML projection; weapon types only |

S098 changes explanation, not capacity, stack size, costs, research, work, food preservation,
beauty or haul permissions. Preserve native costs/progression; do not turn stale source prose into
a gameplay rebalance. S005 linkage and S049 refill still own entity policy/group totals. S050
presets and this feature can share physical acceptance facts; S034 supplies number formatting and
S036 supplies direct owned Architect presentation. No per-port inventory cache is necessary.

## Selected corrections and remaining work

The owned draft specifies stack/book units, actual model-aware capacity, physical rather than
default/current filter scope, empty/partial/special restriction truthfulness, bounded text
preservation, current contextual rendering and validated common heading color. Reset is consistently
orange; all seven languages are required. Original presence suppresses owned overlap; an unadmitted
competing description contribution is preserved and the owned one deferred on that surface.

SSD-Q1–4 track exact effective seams/block grammar, native/provider facts, provenance/UI lifecycle,
honest tests and reviewed native acceptance. All implementation tasks remain unchecked. No source
code/art/binary copying, source package impersonation or universal optional-mod compatibility claim.

Independent behavior/composition review found no issues. Provenance review corrected the selected
Reel's load-folder attribution documented above; focused re-review found no issues. Strict typed
OpenSpec validation passed **120 items, zero failures**. The draft contains **7 requirements,
26 scenarios and 17 unchecked implementation tasks**. These are documentation checks, not runtime
or gameplay acceptance.
