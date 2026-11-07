# Nice Bill Tab — recipe inspection reference

## Identity and provenance

- Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), target **1.7 beta**.
- Origin: **replacement reference**, S010 / interview:P1-09. The user named this and its Expansion
  as UI references, not a request for every source function; see the [interview](../refinement/2026-11-01-handwritten-notes.md).
- Source: **Nice Bill Tab**, author Andromeda, `Andromeda.NiceBillTab`, Workshop **3520130671**.
  Inspected 2026-11-02; About supports 1.5/1.6/1.7, requires Harmony. Assembly/file version 1.0.0.0.
- Read-only input: `F:/Steam/steamapps/workshop/content/294100/3520130671`.
  `1.7/Assemblies/NiceBillTab.dll` SHA256
  **DD6E002AC3936D7513DB576D01AFF8B8D37F15CACBC66005F1AE14E81FDD85DB**.
- Public evidence first: [author description](https://steamcommunity.com/sharedfiles/filedetails/?id=3520130671&l=english)
  and [change notes](https://steamcommunity.com/sharedfiles/filedetails/changelog/3520130671).
  Source paths, bundled FloatSubMenu identity, defaults, outgoing adapters and incoming-consumer
  hashes are in the [research journal](../refinement/2026-11-02-nice-bill-tab-research.md).
- No Nice Bill Tab source repository or reuse license was established; no source code/assets/DLLs
  are copied into megamod. Bundled library copyright is not a reuse license. Resolve provenance
  before any reuse; independently engineered behavior is the current plan.

Static source facts below are distinct from proposed behavior. **Nothing is implemented or verified
in game.** The owning change is [specify-megamod-recipe-inspection](../../../../openspec/changes/specify-megamod-recipe-inspection/),
with its [observable contract](../../../../openspec/changes/specify-megamod-recipe-inspection/specs/megamod-recipe-inspection/spec.md).

## Behavior inventory

| ID | Source action → result | Defaults / edges and chosen disposition | Evidence | Owning contract | Status |
| --- | --- | --- | --- | --- | --- |
| NB-01 | Open workbench Bills → replaced queue/list plus expanding detail panes | Retain richer inspection inside **Add bill**; ordinary queue remains outside this slice | ITab_Bills_FillTab_Patch, TabBillsDrawer.DrawLeftPart/DrawRecipeInfo | Inspect recipes in the workbench Add bill view | Specified, adapted |
| NB-02 | Search names or @/%/# filters; choose Wide/Compact/Categorized | Source filters both queue and recipes, categories chiefly single-product. User wants search/category pills in S012, not copied special syntax or three layouts | FilterMappingHelper, CategoryUtils/TreeBuilder, DrawRecipeList | S012; current contract preserves distinct recipes | Composition deferred |
| NB-03 | Select recipe → output, description, ingredients, work, skill/mech information | One execution; all outputs/requirements; variable output unknown; correct all-skills indication and preserve independent ingredient roles | RecipeSelection.DoCache, DrawIngredients | Explain outputs; Present ingredient requirements; Separate work and skill requirements | Specified, corrected |
| NB-04 | Shift/select material/lock → up to two panes and colored comparisons | Explicit two-choice comparison, no cross-bench pin; third replaces partner; Normal basis, same-unit/meaning pairs | SelectRecipe, RecipeComparator, StatRecordBar | Compare two explicitly chosen recipe variants | Specified, adapted |
| NB-05 | Material menu → select/maximize candidate, possibly edit existing bill | Preview is read-only; shared S009 material modes and explicit add action; source raw map sums/first ingredient check not retained | RecipeSelection.OpenIdeaMenu/SelectBetterMaterial, StatCache.QueryMaximize | S009 material-aware comparison plus S010 inspection | Specified, corrected |
| NB-06 | Stat panels → weapon, food/drug, apparel, medicine, implant, mech and general facts | Retain supported native facts/info cards. Omit guessed combat/actor outcomes, fixed fire bonus, temporary mech generation, silent default-Stuff retry and profit-like cost row | StatCache, StatRequestWorker, stat records, StatMechanoidWorker | Show meaningful product facts with their basis | Specified subset; contextual extras omitted |
| NB-07 | Ingredient menu → info card, map jump or production on another bench | Retain explicit info and visible same-map navigation; no hidden objects or automatic ingredient-production bills | RecipeSelection.OpenIngredientMenu, Utils.QueryCraft | Inspection actions have limited explicit effects | Specified subset; cross-bench production omitted |
| NB-08 | Make or double-click → native bill with style/material and optional name | S009 owns creation; S012 chooses gestures. Automatic renaming not adopted; each add is explicit and affects one selected variant | HandleRecipeClick, TryAddBillToQueue, RenameBill, SetMaterialToBill | S009 plus Compare two explicitly chosen recipe variants | Specified composition; gestures deferred |
| NB-09 | Queue row → delete/suspend/repeat/count/details/drag/clipboard | Source has filtered-drag/full-index and unequal paste-validation paths. Whole queue replacement not selected here; later BWM/whole-list owners define changes | DrawBillQueue/Preview, HandleBillDrop, count helpers, Utils.InsertBillBizarre | Ordinary controls preserved; S014/S017/S018 later | Deliberately omitted from this slice |
| NB-10 | Active queue rows animate; pin/cache survives some transitions | Source worker detection is limited; no adopted work-status diagnosis. Browsing resets on context change and refreshes stocks/skills while paused | GetBillStatus, IsAnyColonistWorkingOnBill, ClearCache, ModIntegration | Keep inspection state local and fresh | Reset/freshness specified; queue animation omitted |
| NB-11 | Mod settings → visual/layout/experimental options; in-tab enable/settings/log controls | Global source settings, enabled flag not serialized; no import. S010 adds no separate setting; S012 shared settings and contextual choices | Settings, FillTab patch | Fit the contextual workflow and preserve ordinary controls | Adapted; source option suite omitted |
| NB-12 | Optional CE/BWM/RR/VEF paths and source-targeting consumers | No blanket support; native/provider fallback, isolated unknown facts, semantic admission. Expansion composes directly after its own S011 change | Adapter files and concrete consumer audit below | Degrade visibly when enhanced inspection cannot coexist | Specified safety; adapter work deferred |

The entire upstream option/default table is retained in the journal rather than silently treating
omitted switches as megamod settings. In particular the source's new-object Compact default versus
missing-field Wide default, nonserialized enable flag, fixed DPS options, 480 height and extra-width
controls are facts about that source, not values adopted for the final responsive browser.

## Interactions and composed result

[Quartermaster comparison](quartermaster.md) owns the single authoritative material/stat/allocation
result and native add action. This view consumes it; it does not introduce a separate cache or an
alternative meaning of craftability. S011 Nice Bill Tab - Expansion is a known modifier and has
only been inspected here as a consumer. Its full functionality will receive its own change next.
S012 will compose all three sources into the requested Add bill search/pills/stat-sort workflow.
No patches against owned code survive as an internal stack of imported mods.

Whole-list workbench linking, Better Workbench Management and Everybody Gets One retain their
separate queued owners. Existing queue controls/restrictions must remain intact until those owners
specify a change. No speculative precedence conflict is declared merely because source features touch
the same window. The concrete source mutation/consumer risks are documented instead.

## UI audit and relocation

No MainButtonDef was found in the source. It replaces the selected workbench's Bills inspector.

| Source entry | Purpose / classification | Megamod destination | Deliberate change / consequence |
| --- | --- | --- | --- |
| Bills recipe list and expanding detail panes | Contextual recipe browsing | Workbench Add bill view | No separate bottom tab or global crafting window |
| In-tab enable switch and settings shortcut; global settings tabs | Configuration | Common mod settings where S012 selects controls | Global default/Use global/current save, save controls disabled at main menu; no duplicated source-settings suite |
| Source developer logging button | Debug | Top debug bar if later retained | No diagnostic control is proposed in Add bill |
| Material/recipe/style/compare choices | Temporary browsing state | Contextual Add bill controls | Not global settings and not implicit existing-bill edits |
| Info cards and ingredient menu | Inspect or explicitly navigate | Ordinary cards and contextual Find on map | No implicit recipe/subtask creation |
| Queue controls and BWM buttons | Bill entity state | Existing native/owned bill UI | No queue redesign or individual-link policy imported by this slice |

S010 has no new persistent setting or inheritance exception. S012 owns eventual persistent browser
configuration and must use common load/store/render and clearly separate global/current-save values.
Current recipe choices clear on close/bench/map/save changes; ordinary info-card closure preserves
them. Translation/overflow, main-menu setting guards when composed, developer mode and native actions
remain future UI acceptance cases.

## Contracts consumed by other mods or saves

| Consumer / evidence | Binding and consequence | Planned treatment / verification |
| --- | --- | --- |
| Expansion, `HICON.NiceBillTabExpansion`, 1.7 DLL hash in journal | Hard source dependency; reflected NiceBillTab types, fields, constructor/FillTab patch methods, recipe/ingredient/queue internals | No binary emulation. Selected effects become direct owned S011/S012 behavior. Installed original companion still needs its own source package. |
| Colony Manager Redux, `ilyvion.colonymanagerredux`, conditional 1.7 integration and compiled patches | Package-gated assembly calls NiceBillTab.TabBillsDrawer and RecipeSelection.SelectedBill; checks ManagerJob_Production.FindManagingJob to suppress protected-bill controls/material changes | Keep native queue and read-only previews; verify managed-bill restrictions with provider present. No claim its NiceBillTab assembly loads against megamod. |
| Show Possible Materials, public source revision 340dff41c64c69f565420da17d863ada357be0c7; not installed at checked path | Prepare checks Andromeda.NiceBillTab, TargetMethod DrawBillPreview, prefix consumes Bill and Rect; separate native bill path exists | Ordinary queue should retain its native path, but present-provider action test required. Do not spoof source identity or claim already working. |
| Nice Inventory/Plants/Research | Source-package load-folder conditions; some bundled-library paths, not demonstrated direct replacement API consumption | Respect actual package identity and avoid bundling FloatSubMenu. Verify any supported 1.7 combination separately. |
| Source saved/configured state | Global ModSettings options; no inspected recipe-selection ExposeData; native bills carry real selected filter/style/name edits | No automatic settings or selection import. No claim that provider-specific saved bill types migrate. Ordinary native bills remain native on removal. |

The incoming scan covered installed Workshop/local Mods text and DLL name candidates, followed by
concrete source reads. It is not an exhaustive guarantee against computed bindings. Proven consumers
do not justify exposing every upstream private field as a speculative API. A future facade requires
an actual retained consumer, license/identity decision and tests; package impersonation is excluded.

## Implementation and coverage

**Implemented portion: none for NB-01 through NB-12.** The proposed inspection subset has
observable requirements; implementation, executable target checks and native verification remain open.
No runnable patch-target document is claimed by this prose. The design records candidate boundaries
and minimum proof obligations; actual stable target IDs and validators precede hooks.

Original-provider co-installation policy: retain another active bill-view replacement when its
effective behavior prevents safe integration; skip our affected enhancement with a startup warning.
Do not reject compatible foreign patches simply because an owner or source package exists. Do not
remove other mods automatically. Isolated stat-provider failure yields unknown facts; if the whole
entry cannot preserve ordinary/provider-only actions, keep its ordinary UI instead.

Source mods/library are references, not required dependencies. Optional outgoing CE, VEF, Research
Reinvented and BWM support needs canonical package and consumed-shape checks and absent-safe behavior.
No XML modification is selected. Future XML edits use content guards and merge-preserving operations;
Harmony/XML Extensions must be declared required when their code/operations are used. Product
dependencies and packages exclude the Dev Gateway and all downloaded assemblies.

Shared recipe/stat/map data belongs to the composed consumer system, using S009 and the foundation
contracts; no port-local Thing cache. Navigation visibility is different from crafting eligibility,
and neither is a reservation or a promise that a pawn can execute the job.

## Verification and acceptance

This draft is supported by primary documentation and bounded read-only source inspection, not by a
live run. [Tasks](../../../../openspec/changes/specify-megamod-recipe-inspection/tasks.md) remain
unchecked. Structural validation and independent document-review outcomes are recorded in the
[progress ledger](../refinement/specification-progress.md).

Future evidence must cover native Add bill → select/compare/info → explicit add → actual production;
quantities/Normal previews versus actual quality; read-only existing/managed bills; unsupported/special
recipes; pause/context/save changes; long translated details; optional-provider absence/presence/drift;
compatible foreign patches and competing native views. Use honest test environments, reviewed build
identity, fresh minimized Gateway-controlled runs, causal screenshots and cleanup under root AGENTS.md.

Semantic pass: information can improve material choices but does not change recipe costs, yields,
research, skill requirements, carrying rules or quality rolls. Removing guessed outcome/profit numbers
avoids misleading planning. No economy, combat-balance, performance or compatibility claim is accepted
from this draft. Open work remains **NB-Q1–4** in the journal and ledger.
