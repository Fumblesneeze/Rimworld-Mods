# S107 — Quick Stockpile Creation research

Owner **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), target **1.7 beta**.
Origin replacement **S107/P2-37**, researched2026-11-05. Documentation then full selected-binary
inspection, native flow and bounded consumer review; no runtime acceptance.
[Proposal](../../../../openspec/changes/specify-megamod-quick-storage-actions/proposal.md),
[design](../../../../openspec/changes/specify-megamod-quick-storage-actions/design.md),
[creation spec](../../../../openspec/changes/specify-megamod-quick-storage-actions/specs/megamod-quick-storage-creation/spec.md),
[exclusion/undo spec](../../../../openspec/changes/specify-megamod-quick-storage-actions/specs/megamod-storage-exclusion-undo/spec.md),
[tasks](../../../../openspec/changes/specify-megamod-quick-storage-actions/tasks.md),
[coverage](../replacements/quick-stockpile-creation.md).

## Documentation and provenance

- [Workshop1742151109](https://steamcommunity.com/sharedfiles/filedetails/?id=1742151109) identifies
  Quick Stockpile Creation by **slofa**. Public description covers item/category/multiple-item zones,
  useful naming, disallow/reallow and claims modded-content/save removal support. Installed About
  additionally names shelves and bookcases. Neither description replaces binary/runtime evidence.
- Public comments mention lost previews, shelf priorities and suspected hauling interaction. These
  are unverified reports, not accepted diagnoses or a reason to declare a hauling conflict. An
  ingredient-derived workbench storage request is not implemented by this binary or selected here.
- Installed root `F:/Steam/steamapps/workshop/content/294100/1742151109`, package
  **longwater.smartstockpilecreation**, PublishedFileId matches. About lists1.1–1.7 and requires
  Harmony (`brrainz.harmony`,2009463077). No LoadFolders.xml,1.7 or Common directory: native1.7
  fallback selects **1.6+root**. Old1.1 includes Rim.Common/0Harmony, not active selected assemblies.
- Selected `1.6/Assemblies/SmartStockpileCreation.dll`: Assembly/File **1.0.0.0**, Release,
  informational **1.0.0+87d40dee4e2a5e8d392e5cf4750bf5ec6cfec9d9**;
  MVID **a17b9727-952f-4658-a24f-f05ee5e3668a**; SHA256
  **95116E5400C9435E1C125A48C8AA1B19BBF09DCA1533E589A9A5DA29D46A0398** (57344bytes).
  Informational suffix is not a verified public-source revision. PE metadata read without loading it.
- No source repository/license found in package, primary page or bounded public source search.
  Assets.zip has two DDS icons plus bake/texture manifests, not source. Root PNG icons are source
  inspection references only; no product art was selected/copied. All **28 runtime C# decompilation
  files** plus assembly metadata were read. English/Russian/ChineseSimplified/Spanish/SpanishLatin
  keyed catalogs exist. No source Def or Patch XML exists.
- Independently implement observable behavior with author attribution; source availability/decompilation
  grants no copying permission. No assembly, original text, art or settings copied into the product.

Ignored evidence: `artifacts/Refinement/megamod-spec-20261105/quick-stockpile-creation/`, public HTML,
full decompilation, native extracts, **9 selected source inputs** and **9 native/consumer inputs**
with SHA256/size manifests. Native **1.7.4998 rev1299**, Assembly-CSharp SHA256
**FE9B615CF5918955AB750F9D627FB0082FD23C556BDFDEE410F88857C67A081B**. No binary was executed.

## Complete selected source surface

| Area / files | Actual behavior |
| --- | --- |
| Controller/Settings | Constructor calls assembly-full-name Harmony.PatchAll; global Preferred priority, higher-priority true, shelves true, bookcases true; radio priorities exclude Unstored; four Scribe keys, no save overrides |
| PatchThingGetGizmos / Helpers | Append zone, exclusion and building actions to haulable Thing gizmos; helpers cast all SelectedObjects to Thing then recursively unwrap minified objects |
| SmartStockpileHarmonyRegistration / Gizmo | Haulable source gate; direct input opens a FloatMenu and also selects exact-type designator; right-click supplies common-category, state/category, state/types and exact-type options; no shared-category message still leaves exact choice |
| SmartStockpileDesignator | Derived native zone adder; new-zone filter clears allowed types/configurable disallows, enables chosen types, sets priority; optional state disables known opposite or all other siblings; accumulated created-zone list renamed after every finalization |
| SmartShelvesHarmonyRegistration | Hardcoded ShelfSmall/Shelf and BookcaseSmall/Bookcase toggles; requires each candidate Def/fixed settings, completed research and acceptance of the individual Thing whose gizmos are queried |
| SmartStorageBlueprintGizmo / SelectMaterialWindow | Shared static material, initial default; chooser lists legal materials with map lister presence; left click opens menu plus exact-type bulk brush, category menu options; selected-def list can include items the original single-item visibility check did not validate |
| DesignatorBulkBuild / Blueprint_Storage_Patch | Reflect private stuffDef; drag-time global PostMake handler initializes every intercepted storage blueprint; temporary shared replaceTags clearing; custom packed ghost arrangement via fake ThingGrid entries |
| DisallowInStockpileHarmonyRegistration / RemoveFromStockpileGizmo | Only Zone_Stockpile; currently allowed item shows disallow. Direct action loops selected items/outer storage; category action uses first unwrapped item's destination only; raw zone.settings edit |
| RecentDisallows / ReallowInStockpileGizmo | Static last3 records, each one Def/category+Zone; additions per selected stack; reallow enables a full type/category, does not restore earlier choices or consume history; action appears only while eligible sample remains selectable in that zone |
| ThingExtensions | Current storage→priority+1 capCritical or global default; category/menu path takes maximum of per-item values; direct creation uses only first sample; inner/outer MinifiedThing traversal lacks cycle guards |
| ThingDefExtensions / ThingCategoryDefExtensions | First assigned category's ancestry only, remove root with RemoveFirst even if list empty; inherited/configurable special-filter enumeration |
| SpecialThingFilterDefExtensions / WorkerExtensions | Eight hardcoded reciprocal state pairs; remove English allow-prefix/apparel-suffix for labels; MatchesAll with additional Fresh+rottable/non-rotDestroys restriction |
| ZoneManagerExtensions / IEnumerableThingDefExtensions | Regex escaped names with optional numeric suffix; max suffix+1; set-like Def comparison and comma-separated unique labels |
| Pawn_JobTrackerExtensions / GeneralExtensionsMethods / EnumerableStringExtensions / Logger | Reflection/debug helpers and logging; no product feature beyond other paths. Trace/debug false; Error forwards into disabled Log. No public debug UI |

No bottom tab is created. Source has several item gizmos, context menus, material popup and mod
settings. Four persisted global keys: `UseHigherPriorityStockpiles`, `StoragePriority`, `UseShelves`,
`UseBookcases`. Created zones, blueprints and filters are native types; custom designators/history
are not serialized. Public exported Settings/Get, commands/designator/AllowedThingsDefs, static
material, utility extension and Logger names are recorded potential contracts, not proven consumers.

## Source details that matter to the new specification

1. **Priority:** selected outer stored item promotes one enum level; source helper returns fallback
   Preferred for no destination/promotion off. Max cap emits a negative message on each call.
   Menu aggregation calls it repeatedly; direct creation only uses first sample. Helpers already
   unwrapped minified items, so some priority/category-disallow paths lose their real storage.
2. **Exact type is not exact item:** source filter permits the ThingDef, not only the sample's Stuff,
   quality or condition. State filtering is a separate explicit zone menu choice.
3. **Special pairs:** weapons smeltable/non-smeltable, burnable/non-burnable, biocoded/non-biocoded;
   apparel equivalents plus clean/tainted; fresh/rotten. Nonpaired special filters instead disable
   siblings under their parent. That does not establish an exclusive partition for arbitrary
   workers, including overlapping book-skill filters. No generic opposite inference is accepted.
4. **New filter:** native SetDisallowAll removes type choices and configurable special disallows;
   it is not simply clearing categories. Source initializes unrestricted configurable states then
   disables explicit opposites. Range/nonconfigurable and physical restrictions remain native.
5. **Zone behavior:** source delegates to native zone expansion; only MakeNewZone installs the
   chosen policy. A touched old zone can therefore be expanded instead of receiving new content.
   `_createdZones` persists on the designator, so later finalizations revisit older created names.
6. **Naming:** localized template uses room role at zone.Position, unless None/Room, plus chosen
   contents and numeric suffix. Whole-room cell enumeration is used to locate the room. The first
   suffix is1; existing matching labels determine the next maximum. No automatic building naming.
7. **Building preview:** custom loop runs from dragCells.Count-1 while index>0, omitting index0.
   Footprint and interaction cells form its occupied set; GenConstruct checks use godMode:false,
   whereas native commit can use current god mode. Preview creates fake Blueprint_Build objects;
   patched ThingsListAt exposes them globally by cell, with no map identity in that static list.
   The list clears in finally. This is a source-risk finding, not a reproduced item-loss diagnosis.
8. **Building commit:** global handler is assigned by DragStart before try; callbacks initialize
   Blueprint_Storage only; drag finally restores replaceTags and clears the handler. It cannot
   configure an immediate god-mode Building result. Reflection/nested callbacks and partial paths
   need replacement, not a copied private state convention.
9. **Undo:** category SetAllow(true) re-enables previously excluded descendants and configurable
   special filters. There is no before-state/delta, no edit-version check, no consume/teardown and
   no native save persistence for history. Three-entry source bound is retained as three commands
   rather than three selected stacks, with precise inverse edits and explicit transient lifetime.

## Native1.7 trace

Fourteen successful types cover Designator/Designator_Build, ZoneAdd/ZoneAddStockpile,
Zone_Stockpile, Blueprint_Storage/Blueprint_StorageWithRoomHighlight, Frame, Building_Storage,
Building_Bookcase, StoreUtility, StorageGroup, StorageSettings and ThingFilter. Initial Verse.Frame
lookup failed; successful type is RimWorld.Frame. No native/game build or launch occurred.

- Native zone adder can select a touched zone, remove occupied cells from candidate set, expand
  selected adjacency and create new zones only for disconnected remaining cells. Stockpile
  validation applies fog/edge/terrain and CanOverlapZones. Owned quick creation explicitly targets
  fresh free regions instead, leaving earlier zones alone.
- Native Designator.DesignateMultiCell rechecks each cell then DesignateSingleCell. Build placement
  can cancel replace-tag-matching frames, WipeExistingThings and place a blueprint; god mode or
  zero work directly spawns the building. Styles, tutorial, place-worker and finalization effects
  matter. The owned route must reject destructive replacement and identify its exact new result.
- Blueprint_Storage.PostMake creates/copies default StorageSettings; GetStoreSettings uses group
  policy if linked. MakeSolidThing copies effective settings/group into Frame; Frame completion
  copies them into the built IStorageGroupMember, and failure recreation retains settings too.
  Blueprint_StorageWithRoomHighlight inherits that lifecycle and adds selection room highlighting.
- **Bookcases are containers:** Building_Bookcase derives Building, not Building_Storage; it holds
  books, implements IStorageGroupMember/IHaulDestination/IHaulSource, checks fixed/editable policy
  and capacity, and notifies haul-source changes. Native StoreUtility uses slot-parent at spawned
  position or ParentHolder as IHaulDestination for held items. Do not assume every storage is a slot group.
- Native Zone_Stockpile is not IStorageGroupMember; S005 supplies that owned integration. Native
  StorageGroup's parent rule uses first member, unsuitable as a substitute for S005 mixed member
  physical restrictions. Effective policy and membership are shared; actual capacity stays per member.
- ThingFilter.SetAllow(ThingDef) includes virtualDefs and notifications. Category SetAllow also
  changes descendant configurable special filters. SetDisallowAll clears configurable special
  disallows; CopyAllowancesFrom copies full ranges/state; Allows(Thing) unwraps and checks ranges,
  quality and special predicates. Exclusion must target exact allowance deltas, not blanket copy.
- StorageSettings edits notify owner; priority changes also update haul destinations/listers.
  Preserve native notifications plus any directly shared owned cache invalidation, with no new
  per-mod Thing scan. Exact effective paths/atomic callback contracts remain QSC-Q1/QSC-Q3.

## Consumer audit and composition

Bounded case-insensitive `rg -a -l`, XML/C#/DLL files under installed Workshop and local Mods,
searched canonical ID and SmartStockpileCreation. Matches: this package's current/older binaries
and About; four `seohyeon.yart` (Workshop3745434121) LayoutHarness/eval/modlists XML samples listing
the package ID. Those are test/example mod lists, not an API call, required dependency or runtime
compatibility adapter. No other exact-name consumer found. UTF16/constructed reflection, remote
consumers and unsearched archives are not disproven. No facade or identity alias is justified.

S005 owns effective mixed-group policy; S049 owns per-storage/group refill behavior; S050 owns the
existing Presets selector and its controlled replacement. Quick actions reuse those direct edit/
notification semantics and preserve their fields. Undo appears on the same native storage surface,
not a second presets system. S021/S033 hauling/search reuse native/shared actual item facts; creating
or excluding storage never supplies fake candidate items or teleports stock. Linked bill destination
and invalid-scope handling retain their own contracts. Missing/changed optional contexts fail only
their admitted actions; original source presence suppresses overlap without foreign unpatching.

## Selected corrections and UI

Fast exact-type creation plus explicit menus, not an unsolicited simultaneous popup. Four native
templates remain; one compact contextual command replaces source gizmo multiplication. Use all
valid category paths, bounded malformed graphs and meaningful state recipes. Names apply once to
new zones only; no implied extension of another stockpile. All creation routes use maximum per-item
priority based on outer physical location. Legal materials may be planned when absent, with honest
availability feedback, rather than a process-static material leaking across maps/buildings.

Preview and commit share one local plan with no fake Things, global grid patch, Def mutation or
unscoped PostMake callback. Per-receipt cleanup must be admitted before mutations. Ordinary and
already-enabled god-mode results both get intended settings without changing that mode.

Existing-filter exclusions preserve every unrelated field and change effective linked policy once.
Category exclusion only removes descendant type allowances. Three-command undo restores actual
removed choices, preserves pre-existing exclusions and later unrelated edits, detects touched-field
ABA and invalidates stale scope/history. Batch failure leaves every target policy unchanged.
History clears on load/teardown; native filters persist. Settings become four common global/save rows,
defaults Preferred/true/true/true, independent dormant overrides and disabled save edits at main menu.
All seven baseline languages and actual icon/ghost visual acceptance remain required future work.

## Static balance and playability review

Core Buildings_Furniture.xml gives these source comparators, before finalized Stuff/provider effects:

| Storage | Footprint / capacity source | Cost/work/research source |
| --- | --- | --- |
| Ground zone | Native one stack per cell; no shelf protection | No building cost/work/research |
| Small shelf / shelf | 1×1 /2×1; inherited maxItemsInCell3 and deterioration protection | 10/20Stuff;250/500work;ComplexFurniture;Metallic/Woody/Stony |
| Small bookcase / bookcase | 1×1 /2×1; inherited maxItemsInCell5, books-only fixed filter; native container count capacity | 10/20Stuff;250/500work;ComplexFurniture through ShelfBase; same Stuff categories |

StorageShelfBase excludes chunks/plants/packed buildings/wastepacks and large corpses through actual
fixed rules; BookcaseBase changes fixed category to Books and uses default Important priority.
Quick source explicitly replaces that initial priority with its own policy; owned behavior does too.
Source XML values are not finalized loaded-game facts. Preserve research, costs, work, actual
physical storage properties, filters and hauling capacity/access. Final review needs native
construction and delivery with cheap/common/expensive legal Stuff and admitted optional content;
no new build/profit loop or free placement is inferred. No performance improvement is claimed until
actual large-drag, selection, category and edit/notification costs are measured.

## Open work

| ID | Remaining work |
| --- | --- |
| QSC-Q1 | Exact effective beta selection/placement/receipt/cleanup/god-mode/construction and lifecycle targets |
| QSC-Q2 | Further state predicates, optional content/storage providers, actual consumers and source/license |
| QSC-Q3 | Atomic effective-policy edits, touched-field generations/ABA, rollback and precise undo lifecycle |
| QSC-Q4 | Finalized storage/material comparators, seven-language/asset UI and bounded preview cost |
| QSC-Q5 | Focused TDD, reviewed implementation/package and personally observed native causal workflows |

Two independent behavior/provenance reviews found no issues. All18 retained source/native/consumer
manifest hashes and sizes were verified; the repeated consumer scan matched the bounded findings
above. Strict OpenSpec validation passes129 items, zero failed. These are documentation checks;
effective runtime admission, creation receipts and safe edit/undo transactions remain open.

No runtime implementation, tests, build, install, game launch, normal save/config write, Workshop
mutation, product asset selection or publication occurred. Review/validation state is tracked in
the sequential ledger; every implementation/acceptance task remains open.
