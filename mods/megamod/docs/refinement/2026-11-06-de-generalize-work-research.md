# S122 — De-generalize Work research

Owner **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), target1.7 beta.
Documentation/source analysis on **2026-11-06**, not runtime observation or compatibility acceptance.
Queue **S122/P2-52**; [change](../../../../openspec/changes/specify-megamod-specialized-crafting-speed/)
and [coverage](../replacements/de-generalize-work.md). Existing behavior was researched without an interview.

## Primary documentation and immutable inputs

Read [Workshop](https://steamcommunity.com/sharedfiles/filedetails/?id=2011655761),
[change notes](https://steamcommunity.com/sharedfiles/filedetails/changelog/2011655761),
[repository](https://github.com/Alias44/Degeneralize-Work),
[wiki](https://github.com/Alias44/Degeneralize-Work/wiki) and
[modding usage](https://github.com/Alias44/Degeneralize-Work/wiki/Modding-usage) before source/decompilation.
Public intent is restoration of task-specific speeds for consumers such as Additional Traits;
the installed 1.7 implementation is more precise than the broad public description.

- Package `Alias.DegeneralizeWork`, Workshop2011655761, author Alias, modVersion2.0.1;
  metadata declares1.1–1.7. Input root `F:/Steam/steamapps/workshop/content/294100/2011655761`.
- Pinned [revision9ab0e3024fa547e85ab3be728a12243f0e855d74](https://github.com/Alias44/Degeneralize-Work/tree/9ab0e3024fa547e85ab3be728a12243f0e855d74),
  2026-10-23T22:04:43Z, 1.7 update. GPL-2.0 license. This draft independently describes behavior;
  no upstream code or binary is incorporated into the product.
- Selected1.7 `DegeneralizeWork.dll`:4608bytes, SHA256
  `E14E433A730B9E593701F642D448DFCBB5D735F54460215BFFA8CFDF4DD1580E`.
  `AliasDynamicPatch.dll`:14848bytes, SHA256
  `C3DB1A078D6EF82EE0C1E175D8C310D1F2C2CC00CEAEBDD2386701730923AE44`.
  Both match the pinned Git blobs, independently of shared assembly/file version1.0.0.0 and
  informational build revision `cfa6b21fd1d63876476d50408bae8cfdc97e410c`.
- 29 pinned text/source files retained;17 installed counterparts match after CRLF/LF normalization,
  12 are source-only.22 installed non-image files inventoried. Read all active XML, settings code
  and both selected assemblies through offline ILSpy; no assembly execution or loading.
- Native inputs: RimWorld **1.7.5000rev1275**; Assembly-CSharp.dll15636480bytes, SHA256
  `D55AA8E47FA3C5463CA85B0ECCD51F195B4BBCD04F56DA3AF21C2F6688A81254`.
  Read native `StatWorker`, `LoadedModManager`, `Verse.AI.Toils_Recipe` and Core work-recipe stats.
- Local ignored evidence: `artifacts/Refinement/megamod-spec-20261106/de-generalize-work/` contains
  public snapshots/manifests, pinned upstream files, installed and provider/native manifests,
  complete main/shared decompilation, relevant VSE decompilation and native extracts. Hashes identify
  research inputs; they are not runtime compatibility predicates.

## Exact source behavior

### Load and stat surface

`LoadFolders.xml` selects Common+1.7. VSE folders require `vanillaexpanded.skills`; bionics folders
add either `FrozenSnowFox.VanillaBionicsExpansion` or `FrozenSnowFox.AdvancedBionicsExpansion` with VSE.
The Vanilla bionics provider declares the Advanced provider incompatible; do not infer simultaneous support.

`Common/Defs/Stats/DW_WorkRecipes.xml` creates **three**, not four, stats: `SculptingSpeed`,
`TailoringSpeed`, `SmithingSpeed`. Each inherits GeneralLaborSpeed's display/default/minimum fields,
replaces its factor list with GeneralLaborSpeed alone, and clears inherited capacity factors.
`Parent_Patch.xml` adds the XML parent name to native GeneralLaborSpeed. Core1.7 already supplies
SmeltingSpeed; the source does not add or redefine it.

Core GeneralLaborSpeed has base1, minimum0.1, WorkSpeedGlobal, Manipulation weight1 and Sight
weight0.5 capped1. The child stats therefore account for those inherited *effects* once, rather than
applying the native capacities again. Their descriptions distinguish speed from product quality.
No new skill need, work priority, WorkTypeDef or WorkGiverDef is introduced.

### Recipe mapping

`Common/Patches/Recipes_Patch.xml` selects raw XML `ThingDef/recipeMaker` and `RecipeDef` nodes:

| Incoming speed | Working effect | Replacement |
| --- | --- | --- |
| GeneralLaborSpeed | Smith, Smelt **or Cook** | SmithingSpeed |
| GeneralLaborSpeed | Tailor | TailoringSpeed |
| GeneralLaborSpeed | Sculpt | SculptingSpeed |
| Any other speed, or an unlisted activity | Any | Unchanged by this base mapping |

Tailor/Sculpt rows apply with VSE absent or LightTouch selected; BestGuess has its own wider mapping.
The Cook entry means general-labor processing with that effect, **not ordinary meal cooking**:
native CookSpeed recipes are unaffected. Core Recipes_Production.xml explicitly assigns both
Make_ComponentIndustrial and Make_ComponentSpacer GeneralLaborSpeed with the Cook effect,
providing concrete machinery-production examples of that rule. Native SmeltingSpeed recipes
likewise stay unchanged.
S013 Bulk Smelting's inspected `Recipes_BulkSlag.xml` already uses SmeltingSpeed with Smelt.
No product-category, workbench-name or work-skill inference appears in these selectors.

Native `LoadedModManager` applies patches to combined raw XML before `XmlInheritance.Resolve`.
Consequently the source can miss a recipe whose speed and effect come from different inheritance
levels; a patched parent can also affect a child that overrides its activity. Owned classification
will use effective recipe meaning while preserving explicit custom speeds and child overrides.
This is an intended correction, not a claim that raw XPath already resolves inheritance.

`Toils_Recipe.DoRecipeWork` reads the recipe's workSpeedStat from the acting pawn, multiplies by the
worktable speed when applicable and advances workLeft. Recipe workSkill still drives training;
faster production can reduce time-based XP per completed item. No guarantee of unchanged XP per
product is made. Work amount, inputs, outputs, quality rules and worker eligibility are untouched
by the source. The owned spec preserves this distinction.

### Optional integration modes

`VE_Skills/Base/Patches/VE_Skills_Patch.xml` always changes the GeneralLaborSpeed factor of
VSE_WeaponCreationSpeed and VSE_ComponentCraftingSpeed to SmithingSpeed. LightTouch similarly
parents VSE_ArtSpeed through SculptingSpeed and VSE_TailoringSpeed through TailoringSpeed, retaining
the separate VSE stats. VSE-specific bonuses remain local to recipes using the corresponding VSE stat.

BestGuess instead removes VSE_ArtSpeed/VSE_TailoringSpeed, renames their ExpertiseDef statOffsets
to the owned names, and maps GeneralLaborSpeed or the matching VSE speed to the owned speed when
the activity is Sculpt/Tailor. It does **not** comprehensively repair every external reference.
HeavyHand's recursive rename and registration are commented out. Text and an enum value still
describe it; it is not a selectable working third integration in the inspected patch set.

`FSF_Skills_Patch.xml` parents FSFBionicsCraftingSpeed through SmithingSpeed with VSE+either FSF
provider. In BestGuess with **Advanced** FSF specifically, `VE_Skills_HediffDef_Patch.xml` renames
matching art/tailoring modifiers in every HediffDef stage's statOffsets/statFactors. The selector
is not restricted to FSF-owned hediffs. Advanced FSF was not found as an installed provider; no
claim of its current runtime contract or acceptance follows from this adapter's source.

**Confirmed source risk:** all factor replacements use `text()["GeneralLaborSpeed"]`, a truthy
constant predicate. An inert System.Xml example with GeneralLaborSpeed+UnrelatedSpeed selects both;
`text()[.="GeneralLaborSpeed"]` selects just the former. The owned operation must match the actual
factor, preserve independent factors and refuse cycles/ambiguous duplicates. Alias' patch sequence
also supplies no whole-feature transaction or rollback guarantee if a later operation fails.

### Settings, UI and persistence

The main Mod delegates to AliasDynamicPatch; there are no Harmony job patches, maps, save components,
thing caches or bottom tabs. Settings are global `compatibilityMode`, a package-ID→PatchMode
dictionary. Missing values mean LightTouch. Modes are registered as XML operations are encountered;
only VSE supplies selectable LightTouch/BestGuess here. The settings window explains restart
requirements; without registered modes it reports no options. There is no per-save setting.

PackageMode resolves actual loaded package IDs; absent packages are normally ignored, and
requireAllLoaded changes that to a failure. Missing VSE therefore admits the base LightTouch branch.
Enum values are LightTouch0, BestGuess1, HeavyHand2. Invalid enum values lack robust recovery;
the settings dictionary uses default case-sensitive lookup while some package comparisons do not.
These are research findings, not desired persistence behavior. No automatic settings migration is
proposed; common owned settings will use stable validated choices, active/pending display and the
documented global-only exception for startup-wide Def composition.

## Actual external consumers and providers

Public descriptions were checked before the narrower installed-contract reads:
[VSE](https://steamcommunity.com/sharedfiles/filedetails/?id=3400246558),
[FSF Vanilla Bionics](https://steamcommunity.com/sharedfiles/filedetails/?id=1419675146),
[Additional Traits](https://steamcommunity.com/sharedfiles/filedetails/?id=1621571024) and
[Primitive Tools](https://steamcommunity.com/sharedfiles/filedetails/?id=3751288694).
Steam's generic page banners are not evidence of removal or real incompatibility.

| Provider/consumer | Inspected contract | Intended treatment and limits |
| --- | --- | --- |
| VSE, vanillaexpanded.skills, Workshop3400246558,1.7 folder | Four named stats, recipe patches, ExpertiseDef offsets/factors; decompiled ExpertiseTracker and ExpertisePatches | Preserve original expertise level/settings and single application; support both admitted modes. Do not replace VSE's passions, progression or other expertise. |
| FSF Vanilla Bionics, FrozenSnowFox.VanillaBionicsExpansion,1419675146,1.7 | FSFBionicsCraftingSpeed base1/min0/GeneralLaborSpeed factor, FSFBionics expertise offset0.05 and conditional recipe assignments | Parent only the general factor through smithing; keep bionics expertise local and numeric values unchanged. |
| Additional Traits, Gewen.AdditionalTraits,1621571024, declared through1.6 | GAT_ProfessionalTailor and GAT_AvidArtist whole Defs MayRequire Alias package; Apollo sculpting and Hephaestus smithing offsets also MayRequire; conditional folder removes Hephaestus general-labor offset0.5 | Stable stat names alone cannot activate filtered nodes. No package spoof or broad trait recreation. Preserve existing admitted direct modifiers; package-gated parity needs a separately proven narrow adapter, currently an explicit limitation. |
| Primitive Tools Continued, zal.primitivetools,3751288694, declared1.6 | ReturnOfWorkStats.xml supplies all three fallback stats only if none of the three exists; also maps recipes and supplies separate stonecutting changes | Avoid duplicates; admit compatible existing stats or skip the coherent overlap. Do not import unrelated stonecutting/tool content. Partial-name collisions need failure tests. |
| Vanilla Skills Rexamined, Gravenwitch.VSRexamined,3235834179, declared through1.6 | Expertise XML adds VSE_ArtSpeed/TailoringSpeed offsets among unrelated expertise effects | Recognized consolidated contributions must move once without losing other bonuses; actual1.7 presence remains unverified. |
| Numbers,1414302321 | Pawn-table default references Numbers_RimWorld_StatDef_SmithingSpeed | Preserve available stat observation; this literal is a column identity, not proof the consumer directly requires Alias' assembly. Native column generation still needs live verification. |
| Installed XML/C# literal search | AliasDynamicPatch or DegeneralizeWork.* hits confined to the source's own files | No demonstrated external XML-framework consumer. This bounded search does not establish absence of reflection or compiled consumers. No facade for the entire framework is justified. |

VSE's inspected ExpertiseTracker adds `offset × level × provider multiplier`, and separately applies
expertise factors through its patched native stat pipeline. A modifier moved to an owned stat must
not remain active at the VSE child too. Numeric readouts, recipe timing and explanations must agree.
Consolidation cannot be implemented safely by blind rename, blanket transfer, duplicate factor or
removing the old Def. Unsupported providers remain explicit gaps, not claims based on filenames.

## Proposed owned differences and composition

1. Preserve all three names with native general-labor effects once, neutral base100% and floor10%.
   No new hard-coded crafting skill curve. Keep native recipe costs, quality and assignment paths.
2. Resolve effective recipe activity, including inheritance; exclude explicit custom speeds except
   named admitted VSE consolidation mappings. The source's general-labor Smith/Smelt/Cook mapping
   is retained pending DGW-Q1 rather than silently becoming a broad category guess.
3. Default **Conservative** mirrors LightTouch intent. Optional **Consolidated** mirrors BestGuess's
   single art/tailoring modifier domain but retains old readable identities, maps known modifiers
   once and refuses unknown contributions whose meaning cannot be preserved. A full preflight
   must prove a supported acyclic graph before any edits. Never silently change the selected mode.
4. Show global startup mode in shared settings; save overrides are inapplicable because all saves
   in one process share the patched Def graph. Invalid settings use the built-in conservative
   choice with a diagnostic; saved pending/active differences are visible until restart.
5. Co-installed original owns this overlap; owned recipes/stats/adapters do not apply twice. No
   deletion of foreign definitions, replacement of entire lists or automatic user configuration edits.
6. S009–S019 bill controls and S121 queries read the actual chosen recipe/stat through direct owned
   composition. No new Thing index or stat-result cache. S013 native smelting remains unchanged.
   S135 Basic Caravan Loading changes work assignments; **S122 does not own Work-tab categories**.

## Semantic balance and remaining evidence

The three relevant comparators are native GeneralLaborSpeed, native SmeltingSpeed and VSE's
specialized art/tailoring stats. The source's factor1 baseline retains speed without a specialized
modifier, while modifiers intentionally distinguish production speed. Native smelting has different
Sight weighting and must not be casually replaced. Conservative and consolidated modes combine
modifiers differently: e.g. general1, owned tailoring offset+0.5 and VSE offset+0.5 produce2.25 with
the conservative child chain but2.0 when both offsets occupy one consolidated stat. This is an
explicit observable difference, not a promised algebraically identical reorganization. Source
BestGuess already changes that composition. No item costs, research, quantities or skills are added;
speed can affect time-based XP and output per day. Finalized stats and live crafting remain pending.

| Question | Remaining issue | Handling |
| --- | --- | --- |
| DGW-Q1 | General-labor Cook/Smelt activity maps to smithing even for processing rather than literal smithing | Preserve source mapping explicitly for now; compare actual finalized recipe examples and retain any proposed taxonomy change as a spec revision. |
| DGW-Q2 | Non-destructive consolidated aliases, numeric explanations, clamps and unknown external modifiers | Prove supported alias evaluation and exactly-once modifier transfer; fail the atomic integration closed otherwise. Do not delete provider stats to make the feature work. |
| DGW-Q3 | Additional Traits package-gated nodes; Advanced FSF and older optional providers on1.7 | Record unsupported bindings; inspect exact active inputs and minimal adapter before any parity claim. Never spoof package identity or resurrect disabled traits. |
| DGW-Q4 | Effective XML inheritance, operation ordering, idempotence and rollback | Select actual XML Extensions operations and design a bounded preflight/commit; no runtime Def mutation shortcut. Native final-Def and player tests required. |
| DGW-Q5 | Final playability/visual validation of both modes, settings and co-installation | Separate exact-provider runs, native crafting progress/products and stat explanations; all future tasks remain unchecked. |

Document review and strict validation are recorded in the progress ledger. No gameplay tests,
build/install, launches, Workshop mutation, save/config edits or publication occurred in this slice.
