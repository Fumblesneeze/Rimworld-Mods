# S050 — Simple Stockpile Presets research

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
Inspected **2026-11-03**, origin replacement, **P1-44**. Complete selected-source/native analysis
supports an independently reviewed observable draft. Strict validation passed 72 items, zero failed.
The independent source/native/contract review found no issues. Nothing implemented.

The user's [refinement record](2026-11-01-source-audit.md) requires compact preset selection across
storage types, hiding choices whose contents the storage cannot accept. They left one dropdown gizmo
versus integration into storage configuration open. The draft explicitly proposes the latter.
They do not need to diagnose the source's reported stockpile-placement bug: its cause and live
reproduction remain unproven, while correct native placement is part of the owned contract.

## Documentation first and provenance

Read [Workshop2189006791](https://steamcommunity.com/sharedfiles/filedetails/?id=2189006791), installed
About/README/changelog and the linked [published repository](https://github.com/emipa606/SimpleStockpilePresets)
before completing source/decompilation. Public documentation describes fixed filter shortcuts in a
floating menu and points to a **different fork** for editable presets. That fork is not automatically
selected. The source introduces no content and its installed metadata advertises1.7; neither that
label nor the changelog's autotest statement proves our native/player compatibility.

| Identity | Inspected value |
| --- | --- |
| Selected mod | Simple Stockpile Presets (Continued) |
| Authors | About author Lanilor; continuation maintainer Mlie; installed license copyright2020 Mlie; assembly copyright Lanilor2026 |
| Package / Workshop | `Mlie.SimpleStockpilePresets` / `2189006791` |
| Installed root | `F:/Steam/steamapps/workshop/content/294100/2189006791` |
| About / Manifest |1.7.0; About supports1.0–1.7; Manifest identifier SimpleStockpilePresets |
| Selected DLL | `1.7/Assemblies/SimpleStockpilePresets.dll`, assembly1.7.0.0, file1.7.0, Release, net10.0 |
| Informational version | `1.0.0+cb97be637ca7e6ffa8ae973b9c4325ee5fad3592` identifies preceding1.6 commit, not current source HEAD |
| Published source HEAD | `6f048d0d858328f2b10f36781a7082242ab4dd32`; clean clone; selected repository1.7 binary is byte-identical to installed DLL |
| Dependency / order | Required Harmony `brrainz.harmony`, loadAfter Harmony; no other declared runtime dependency |
| Runtime assembly references | Assembly-CSharp, UnityStubs, Harmony and framework; no custom runtime library |
| License | MIT in installed/published LICENSE.md; preserve notices for any future copied/substantial source portions |
| Local evidence | `artifacts/Refinement/megamod-spec-20261103/simple-stockpile-presets/{source,compiled,native}` |
| Native comparator | Installed1.7.4995 rev309, Assembly-CSharp SHA256 below |

| SHA256 artifact | Hash |
| --- | --- |
| Selected installed DLL | `8E4256B6A68C4F617604369D0BA975A8379DB8D8D54364F34995A82D5AB8EF4D` |
| Source repository1.7 DLL | `8E4256B6A68C4F617604369D0BA975A8379DB8D8D54364F34995A82D5AB8EF4D` |
| Installed About.xml | `104C4CB189D6458F4D6BBE44CCDDA7117AD227A57E9400F2A540C82F7DAE56A8` |
| Source Command_StorageSettingsPresets.cs | `4EF8517CB763D444492FF707720D1D9AEB63E0BF3E245968637E54E4998276E3` |
| Native Assembly-CSharp.dll | `56399B6144D9F1695F9C53D054DC82972F7AB35E68D1A19A7129F2F5860CCD21` |

Installed inventory has versioned1.0–1.7 binaries, root metadata, five language catalogs (English,
German, French, ChineseSimplified, Japanese), command texture and preview art. No LoadFolders.xml,
Defs or XML patches. The old1.0 directory bundles Harmony; that historical artifact is not selected
or copied to our product. The current project references Krafs.Rimworld.Ref1.7.4982-unstable and
Harmony3.0.0-rc.3 as compile inputs. Its pre-build/clean target kills processes by image name;
**the upstream project was inspected only and never built/executed**. External contribution-policy
text is not an instruction to change this repository's authorized documentation workflow.

## Complete source and installed behavior

Read all six current C# files and the current project, then all six functional installed types
through full ILSpy11.2.0.10050 decompilation, including generated menu actions. No omitted behavior
module, source ModSettings or custom persistence type was found.

| Type / source | Behavior |
| --- | --- |
| Harmony | Static startup creates `rimworld.lanilor.simplestockpilepresets` and calls unguarded PatchAll on its assembly. No startup target specification or provider gating. |
| Harmony_Zone_Stockpile_GetGizmos | Only Harmony target: native Zone_Stockpile.GetGizmos postfix. Materializes returned gizmos with ToList, appends one command, returns list. It does not patch painting, creation or placement. |
| Command_StorageSettingsPresets | Constructor loads source command texture, translated label/description, captures `stockpile.settings.filter`. ProcessInput calls base, creates15 translated FloatMenu entries in fixed order, then opens menu. Every callback operates on that captured filter without selection/lifetime/group revalidation. No per-preset gizmo, no shelf/container/group hook and no mod-settings/bottom-tab entry. |
| AdditionalThingCategoryDefOf | Resolves MortarShells and EggsFertilized categories through native DefOf initialization. |
| ThingDefOf | Resolves Ambrosia, Beer, Neutroamine, Wort, Plasteel, Silver, Steel, Uranium and MedicineHerbal. |
| SpecialThingFilterDefOf | Resolves AllowRotten, AllowFresh, AllowSmeltable, AllowNonSmeltableWeapons, AllowNonDeadmansApparel, AllowDeadmansApparel, AllowCorpsesColonist and AllowCorpsesStranger. Four smelting/corpse-origin fields have no action reader. |

Source menu order and type selections are completely enumerated by the
[design's catalog table](../../../../openspec/changes/specify-megamod-storage-presets/design.md).
Each source action first calls SetDisallowAll. Its explicit state edits are **false only**:
General freezer, Freezer(no meals) and Meals disallow AllowRotten; Various others and Clean apparel
disallow AllowDeadmansApparel; Tainted apparel disallows AllowNonDeadmansApparel; Rotting disallows
AllowFresh and AllowNonDeadmansApparel. The other eight have no explicit state assignment.
Owned true assignments in the table are deliberate complementary choices that replace source reset
side effects. They are not falsely attributed as source statements.

Detailed source distinctions:

- General freezer includes Foods, ambrosia/beer/wort/herbal medicine, PlantMatter and human/animal
  corpses, excluding fertilized eggs. No-meals also excludes FoodMeals. Meals only allows FoodMeals.
- General storage includes manufactured/raw resources, fertilized eggs, Items, Weapons, Apparel,
  Buildings and mechanoid corpses, excluding ambrosia/beer/wort and PlantMatter in source call order.
- General medical includes Medicine/Drugs/BodyParts and neutroamine, excluding ambrosia/beer. Drugs
  keeps only Drugs plus neutroamine with those exceptions; it is not all medicine.
- Materials(outside) is StoneBlocks plus plasteel/silver/steel/uranium. Materials is ResourcesRaw
  without PlantMatter, not every craftable recipe's materials.
- Various others allows manufactured goods/items/weapons/apparel/buildings/mechanoid corpses while
  excluding medicine/drugs/neutroamine/wort/body parts and tainted apparel.
- Shells, Weapons and Chunks use their categories. Clean/Tainted apparel select Apparel with their
  corresponding exclusion. Rotting selects Apparel and human/animal corpses, excluding fresh and
  clean; no active smelting or colonist/stranger corpse preset exists.

Source order can re-allow a multiply categorized type after an earlier exclusion; owned exclusions
explicitly win instead. There is no source fit check, preview, transaction, undo, disabled-empty
choice, custom editor or explicit rollback. The native resulting filter persists through ordinary
storage serialization; the preset name itself is not saved or reapplied. No source settings defaults,
per-save config or custom-import format exists to migrate.

## Native filter and storage evidence

Inspected relevant native ThingFilter, StorageSettings, Zone_Stockpile and ITab_Storage bodies,
four fresh/rotten/tainted workers and Core category/special-filter definitions. Existing S005/S049
research supplies linked StorageGroup/Building_Storage ownership and member-specific constraints.

| Native seam | Consequence for specification/design |
| --- | --- |
| ThingFilter.SetDisallowAll | Clears allowedDefs and removes configurable disallowed special filters; with onlySpecialFilters it disables the root's specials. Does not reset quality, hit-point or mental-break ranges. Its callback can fire before later source additions. |
| ThingFilter.SetAllow(category) | Iterates descendant ThingDefs and configurable descendant special filters, with callbacks. Using it as a type-only edit changes more than type selection. |
| ThingFilter.SetAllow(special) | Adds/removes from disallowed list and notifies. Explicit opposite allowances must not defeat intrinsic fixed filters. |
| ThingFilter.CopyAllowancesFrom | Copies allowedDefs, special restrictions, ranges and other internal allowance data, then callback. Candidate staging must preserve current unrelated data; full StorageSettings.CopyFrom would also copy priority. |
| Fresh worker | Rottable objects match Fresh stage; non-drug ingestibles without rottable component also match. AlwaysMatches includes rotDestroys types and those non-rottable foods. |
| Rotten worker | Requires rottable component, not rotDestroys, with stage not Fresh; includes dessicated ordinary corpses. |
| Clean/tainted workers | Use Apparel.WornByCorpse; not item condition/repair/quality. CanEverMatch considers apparel's careIfWornByCorpse. |
| StorageSettings.AllowedToAccept | Own filter plus parent restrictions. A mutable filter change does not permit violating a fixed/intrinsic restriction. |
| Zone_Stockpile | Constructor establishes ordinary native preset and SlotGroup; GetGizmos returns native hide/base/copy controls; Notify_SettingsChanged notifies haulables. Source has no direct placement intercept. |
| ITab_Storage | Resolves Thing/comp or IStoreSettingsParent, including consistent selected group. Draws priority and filter with parent constraints; compares eligible production bills before/after the filter UI and reports lost destinations. Owned linked-row recovery must retain S005/S014 semantics. |

The source's grouped applicability cannot be inferred from1.7 metadata: its constructor still takes
a Zone_Stockpile and directly captures settings.filter. The owned extension must use the effective
group authority. Likewise a preset's category intersection is insufficient for clean-only storage
versus tainted apparel: fixed state restrictions can rule out all acceptable instances. None of this
was established through a live game run, so it remains static evidence and future acceptance work.

## Contracts, source compatibility and chosen deviations

Public source classes include the Command constructor, ProcessInput override, GetGizmos postfix and
DefOf fields. Bounded raw binary search across installed Workshop/local Mods for SimpleStockpilePresets,
Command_StorageSettingsPresets and its Harmony owner returned only its eight versioned source DLLs.
No actual inbound external consumer was discovered; uninstalled or string/reflection consumers
remain possible. No public facade, original assembly identity or spoofed package is selected.
Source textures and translated keys are external contracts only if a real consumer is later found;
the owned text selector need not import the source art.

| Source / requested behavior | Selected owned outcome |
| --- | --- |
| One zone menu gizmo | One compact selector within storage configuration, including admitted shelves/groups/containers; explicit proposal, no assertion that source had many gizmos |
| Always show15 choices | Hide impossible entries using intrinsic type/state restrictions; ignore current editable allow-list and map stock; partial intersection explained |
| Clear then set categories/specials | Intentional item-list replacement, explicit named state edits, preserve all unrelated filters/ranges/priority/refill policy |
| Captured zone filter | Validated current effective scope, membership-aware stale rejection, latest unaffected fields preserved, atomic result/restoration |
| Source reset permits opposite state indirectly | Explicit complementary fresh/rotten and clean/tainted assignments; no broad special-filter reset |
| Native filter persistence | Retain native save/copy/manual edits; no saved live preset or automatic reapplication |
| Source co-installation | Suppress owned preset integration when Mlie.SimpleStockpilePresets is active; no foreign unpatching or source repair claim |
| Custom-editable fork link | Not adopted; no custom catalog/import/export UI inferred from it |

Core/Harmony become required on implementation; optional providers need actual package/content/shape
evidence. No XML patch is proposed, and category lists remain untouched. Item-list replacement is a
deliberate player action with explicit scope, not a destructive Def patch. All internal composition
uses S005/S049/S014/S021 authority directly and native/shared data; no patch stack inside megamod.

## Remaining work and verification status

| ID | Work retained |
| --- | --- |
| SP-Q1 | Effective beta selector/target/complete commit/rollback/bill paths, incoming-stream checks, provider isolation and bounded startup diagnostics |
| SP-Q2 | Finalized catalog meanings, category overlaps, missing content, fixed-state feasibility and supported actual provider restrictions |
| SP-Q3 | Compact seven-language contextual UI and direct S005/S049/S014 ownership/lifecycle; exact stale action and preserved-field behavior |
| SP-Q4 | Honest-tier TDD, package/review, measured menu cost and personally observed native creation, selection, hauling, persistence and provider workflows |

[Proposal](../../../../openspec/changes/specify-megamod-storage-presets/proposal.md),
[observable spec](../../../../openspec/changes/specify-megamod-storage-presets/specs/megamod-storage-presets/spec.md),
[tasks](../../../../openspec/changes/specify-megamod-storage-presets/tasks.md),
[replacement record](../replacements/storage-presets.md). No product runtime changes, upstream
execution, build/install, normal configuration/save changes, Workshop mutation or game launch.
