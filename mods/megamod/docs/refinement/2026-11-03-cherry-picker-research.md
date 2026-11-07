# Cherry Picker research — 2026-11-03

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`). S038, P1-30.
Target **RimWorld 1.7 beta**. Documentation and complete installed-source inspection only;
no source mod or owned feature was run in game. Proposed corrections below are not observed fixes.

## Evidence and identity

Public documentation was read before decompilation: the [installed fork's Workshop page](https://steamcommunity.com/sharedfiles/filedetails/?id=3521312241),
its [changelog](https://steamcommunity.com/sharedfiles/filedetails/changelog/3521312241), the
[original description](https://steamcommunity.com/sharedfiles/filedetails/?id=2633276599), the
[1.5 fork](https://steamcommunity.com/sharedfiles/filedetails/?id=3230046902), and the
[legacy repository](https://github.com/Owlchemist/cherry-picker). The original documents selective
content exclusion and mod-authored lists; the installed binary supplies the detailed evidence below.
Generic Steam compatibility banners and user comments are not runtime proof.

| Input | Inspected identity |
| --- | --- |
| Installed root | `F:/Steam/steamapps/workshop/content/294100/3521312241` |
| About | Cherry Picker; author Owlchemist; package `Owlchemist.CherryPicker`; supports 1.3–1.6 |
| Workshop publisher | ajperson1927; this is the current installed fork, not the original Workshop item |
| Selected assembly | `1.6/Assemblies/CherryPicker.dll`; assembly 1.0.62.0, file 1.0.62, informational `1.6+7e77b7fdba2daa472a5b7c2b35ac17c149dffa99` |
| DLL SHA-256 | `4F4AB849B5A3F5BF9FDC497607DE05B03035ED920E2C955FE6660C89BDAF73E1` |
| About SHA-256 | `1B0DEB10BEF55A8732A25B822F119EC9D9D4AE95CD1EA48E0749CB43FF2D2C7A` |
| Legacy Git checkout | `b9581d21a5da7636967fb3b28e4ae61a0442a38e`; source plus 1.3/1.4 binaries, not identical to the installed 1.6 fork |
| License | Legacy repository MIT, copyright 2020 jptrrs. Installed fork contains no inspected license/source; do not assume the legacy grant proves provenance of every later change |
| Native reference | `F:/Steam/steamapps/common/RimWorld/Assembly-CSharp.dll`, 1.7.4995 rev309, SHA-256 `56399B6144D9F1695F9C53D054DC82972F7AB35E68D1A19A7129F2F5860CCD21` |

Ignored local evidence: `artifacts/Refinement/megamod-spec-20261103/cherry-picker/compiled`,
`upstream`, and `native`. ILSpy 11.2.0.10050 produced the decompilation. Read all fourteen runtime
files, including the complete large utility, both settings classes, DefUtility/DefList/DrawUtility,
dummy interaction worker, six attributed patch classes and dynamic patch class. Read installed
About, published ID and English keyed descriptions. Earlier S020 source evidence agrees with the
pinned assembly. Nothing downloaded is copied into the product.

Harmony is required by the source. About loads before VEF, ResearchPal Forked, ResearchPowl and
Mod Medicine Patch, and after Clothing Sorter. No LoadFolders file was supplied. English, Russian
and Japanese keyed files exist; this does not satisfy megamod's seven-language requirement.
Changelog entries contain timestamps rather than a useful behavior history.

## Catalogue and ordinary UI

`CherryPickerUtility.Setup` concatenates **45 types** including its own DefList, then three
optional type families. ThingDef includes items, buildings, plants and pawn races, excluding
blueprints, frames and unfinished things. Male/Female body types and the Colonist pawn kind are
protected. Factions require positive `maxConfigurableAtWorldCreation`. Quest roots directly linked
from incidents are omitted from the independent quest list. The research predicate tests whether
*any* other project does not directly depend on the project; it is not a reliable leaf-only test.
Previously processed definitions are kept in the displayed catalogue even after database removal.

The only entry point is mod settings, not a bottom tab. Type and source FloatMenus plus native
QuickSearchWidget filter a scrollable checkbox list. Type/source/defName, label, optional buildable
or recipe-product icon and a Type/Mod/Def/Filename tooltip identify a row. Checkboxes are paintable.
Main search is one lowercased substring of concatenated defName, label, source name and type name;
there is no tokenized AND search. Source filtering compares display names, so equal-name mods merge.
The S020 draft deliberately fixes that identity problem and owns searchable responsive selectors.
The installed 1.6 page has no click-to-open info-card action. Disabled rows sort before enabled rows
after processing; there is no general alphabetical main-list sort.

Type choices cover distinct catalogue types; source choices cover active mod packs independently
of the other filters. There is no source preset editor/export/import/reset-file UI. Authored DefLists
are the actual preset mechanism. Scroll, main text and selector state are transient.

## Persistence, lifecycle and restoration in the source

`ModSettings_CherryPicker.allRemovedDefs` is a static global HashSet serialized under `keys`.
Built-in default is empty: everything available. ExposeData during Saving calls ProcessList before
serializing. Ordinary row clicks edit a working set; DefList clicks immediately call WriteSettings.
Static initialization applies choices, then a MainMenuOnGUI postfix runs a second setup and removes
only that postfix for the original owner. The source uses unguarded PatchAll.

Key syntax is `TypeName/defName` for Verse/RimWorld types and
`TypeName/defName/Namespace` for external types. There is no source package in the key. Type caching
by simple name can collide. Malformed keys can throw through blind splitting; unavailable saved
keys remain stored. `GetDef` falls back to already-processed definitions removed from the database.

ProcessList marks a definition processed **before** removing it. A failed operation can remain
marked and partly mutated. PostProcess runs on every application, even without a new exclusion.
Nested lists use recursive reprocessing, not independent exclusion reasons. Re-enabling a list
removes all member keys, including independently selected or overlapping-list members.

Immediate restore exists only for database-removed traits, memes, ritual patterns, hair, beard,
tattoos and backstories; MainButton/Biome/Storyteller/Scenario visibility is set true rather than
restoring its prior value; Hediff restore just stops suppression. Other restoration generally needs
a restart. The restart accumulator assignment can reset a previously true flag on a later item;
it is not a reliable monotonic "any restart needed" calculation. Restore can remove the processed
marker before the original field mutations are undone, producing an enabled-looking row with
pending effects. Apparel/category changes may request a save reload while playing. No transactional
rollback or true per-save isolation exists in this implementation.

## Complete type/action inventory

This table records **source actions**, not adopted implementation. Field names stay in this research
and the design; the capability specs describe player-observable results.

| Type/family | Installed removal behavior |
| --- | --- |
| ThingDef, all admitted categories | BaseMarketValue 0, tradeability None, categories/set-maker tags cleared, deep mining commonality/count/lumps zero; ingestible nutrition 0 and NeverForNutrition; mark excluded Stuff |
| Building ThingDef | Clear designation category, minification/research prerequisites/building tags; unclaimable, mining zero, not natural rock |
| Item ThingDef | Clear recipe-maker users, MedicalPotency stat, tech-hediff tags and Seed-named extension; Stuff commonality zero; weapon tags/classes cleared for primary equipment |
| Apparel ThingDef | Clear tags/default outfit/ideology desire tags; remove every comp except exact Drug and Styleable comp-property types; request reload |
| Drug ThingDef | Drug category None, tech level Archotech; prune DrugPolicyDef preset entries, not existing live policies |
| Plant ThingDef | Clear sow tags/wild biomes, cave plant false |
| Pawn-race ThingDef | Clear trade tags, migration false, wildness 1, wild biome commonality zero |
| PawnKindDef | Combat power float.MaxValue; manhunter arrival, sapping and mech-cluster eligibility false; minimum generation age zero; does not intercept every explicit generator |
| TerrainDef | Clear designation category and **all costs**; resolve references |
| RecipeDef | Remove from listed benches and recipe caches; clear research/work-type links; remove from DefDatabase |
| ResearchProjectDef | Remove from database; splice direct/hidden parent prerequisites into dependants, then remove selected links; hidden branch has unsafe/wrong-list deduplication; flag terrain cleanup |
| TraitDef; MemeDef; RitualPatternDef; HairDef; TattooDef; BeardDef; BackstoryDef | Remove from respective databases |
| DesignationCategoryDef | Remove from database; reload notice while playing |
| ThingStyleDef | Remove first matching style pair/designators per category; this direct exclusion does not remove the style from DefDatabase; baked save styles may survive |
| QuestScriptDef | Root/decree selection weights zero; runtime prevention of new quest addition/availability letter |
| IncidentDef | Chances zero; extreme thresholds for day/population/threat |
| ThoughtDef | Duration zero, memory classification reset, unattainable minimum expectation; no explicit existing-memory deletion |
| TraderKindDef | Commonality zero |
| GatheringDef; InspirationDef | Selection/commonality zero |
| WorkTypeDef | Hide work column and remove matching PawnColumnDefs/table entries; **does not disable the work or its jobs** |
| PreceptDef | NPC selection disabled, invisible, weight zero, database removal |
| RaidStrategyDef | Selection/points curves zero |
| MainButtonDef | Hide button |
| AbilityDef | Level maximum; remove grants from precepts and royal titles |
| BiomeDef | Mark not implemented |
| MentalBreakDef | Base commonality zero |
| SpecialThingFilterDef | Prune recipe special allow/disallow/hidden lists and category children |
| GenStepDef | Remove from MapGenerator.genSteps |
| HediffDef | No direct mutation; prefix blocks new AddHediff instances |
| StorytellerDef; ScenarioDef | Hide from their selection UI |
| DesignationDef | Flag removal of matching special designators and resolved controls |
| FactionDef | World-creation max -1, starting/required counts zero |
| BodyTypeDef | Flag backstory body-type replacement: sex-specific defaults or null |
| PawnsArrivalModeDef | Archotech minimum; zero selection/points curves |
| GeneDef; XenotypeDef | Database removal when Biotech is **installed**; xenotypes also flag reference cleanup |
| WeatherDef | Temperature range -999 to -998, bad-weather flag false |
| ScatterableDef | Set scatter type to literal "null" |
| RaidAgeRestrictionDef | Selection chance zero |
| WeaponTraitDef | Commonality zero |
| RulePackDef | Flag culture name/title-maker cleanup |
| InteractionDef | Replace worker with dummy whose random selection weight is zero |
| CherryPicker.DefList | Queue member keys for recursive application |
| VFEAncients.PowerDef | Optional `vanillaexpanded.vfea`, assembly VFEAncients; database removal |
| VEF.Abilities.AbilityDef with VPE extension | Optional `oskarpotocki.vanillafactionsexpanded.core`, assembly VEF; flag Psycasts cleanup; modExtensions is accessed without a null guard |
| VanillaPsycastsExpanded.PsycasterPathDef | Optional `vanillaexpanded.vpsycastse`; database removal and path cleanup |

The legacy repository uses an older VFECore binding; installed 1.6 uses VEF. Active optional package
and type/assembly presence are checked, but subsequent reflective members are not robustly guarded.
No absence-safe claim follows merely from that initial package check.

## Post-processing and patch inventory

All PostProcess branches were read. They prune ThingCategory children; scenario starting-item parts
and xenotype overrides; recipe ingredient/filter sets; Thing costs, difficulty costs, butcher/killed
products, cached recipes, research and spawners; selected trader SingleDef/MultiDef generators;
pawn required apparel/tech hediffs and VPE paths; biome plants/animals/pollution commonalities;
scatter groups/clusters; faction memes and basic members; Biotech gene/xenotype chances and boss-wave
escorts; Ideology styles whose associated **ThingDef** is processed (then removing those styles
from DefDatabase), building chances and role-apparel requirements; special designators;
backstory body types; terrain research; culture naming rules; selected nested ThingSetMaker options;
and Psycasts UI paths, abilities and prerequisites.

Specific hazards relevant to the replacement:

- Removing costs can make surviving recipes/buildings cheaper or free. A recipe is removed for
  ingredient exhaustion only when *all* ingredient groups are empty after a change, so one empty
  required group can leave an impossible recipe. No general recipe-product pruning was found.
- Existing things still reference mutated definitions: zero nutrition/value or removed apparel
  comps can affect physical items. Source's public pseudo-removal description is not a guarantee
  that all types remain in their DefDatabase or existing instances remain behaviorally unchanged.
- Disabling a race makes its pawn kinds expensive/ineligible for some generators rather than
  removing every hard reference. Faction basicMemberKind may become Colonist. Not every stock,
  quest, raid or direct-spawn provider is covered.
- ThingStyle removal takes the first matching pair in one path; special-designator removal compares
  a newly instantiated designator against resolved entries. Neither is universal cleanup proof.
- ThingSetMaker traversal covers Sum → RandomOption and selected conditions; one xenotype branch
  tests the wrong nullable weight before changing it. Do not claim arbitrary nested traversal.
- VPE removes paths from shared UI lists, replaces ability slots with Blank, and splices removed
  ability prerequisites into remaining nodes without cycle/deduplication handling.
- Reflection also recognizes a `FactionExtension_SenatorInfo.senatorResearch` shape without a
  package gate. This is an inspected source seam, not a verified optional-provider contract.

| Patch | Source effect and boundary |
| --- | --- |
| DebugThingPlaceHelper.IsDebugSpawnable postfix | Hide excluded ThingDefs from that debug-spawn selector |
| MainMenuDrawer.MainMenuOnGUI postfix | Second setup then remove this source-owned postfix |
| Pawn_HealthTracker.AddHediff(Hediff,BodyPartRecord,DamageInfo?,DamageResult) prefix | Block excluded new hediff instances |
| QuestManager.Add(Quest) prefix | Refuse excluded quest roots |
| QuestUtility.SendLetterQuestAvailable prefix | Suppress availability letters for those roots |
| Pawn_ApparelTracker.Wear prefix, priority 800 | Block equipping excluded apparel, including an existing garment; not limited to new pawn generation |
| Dynamic ThingDef.IsStuff transpiler | Return false for processed Stuff only during MapInitializing; not a universal IsStuff override; source enumerates incoming instructions twice and lacks semantic validation |

These are **six attributed patch classes plus one dynamic patch**. Future owned patches need their
own minimal semantic target documents and startup admission; none of these source hooks is copied
or accepted merely because its method exists.

## Proven consumer contracts

| Consumer / evidence | Binding and scope |
| --- | --- |
| S020 Cherry picker scroll bars, `lucius.cherryscroll` | Compiled reflection/patch against `CherryPicker.Mod_CherryPicker.DoSettingsWindowContents(Rect)` and two FloatMenu constructions; owned UI composes directly, external add-on remains attached to original |
| Too Many Mods - Compats and Rebalances, `Wara.toomanymods`, Workshop 3250762483 | Actual `CherryPicker.DefList` nodes with `MayRequire="Owlchemist.CherryPicker"`, defName/label/defs; patches append member strings using original node name/XPath |
| Smart Minify, `Jaxe.SmartMinify`, Workshop 2815301242 | About loadAfter original package; no callable Cherry Picker API established by the bounded scan |
| Use This Instead, RimPy catalogue, LayoutHarness sample mod list | Identity/catalogue references, not demonstrated callable contracts |

Too Many Mods installed 1.6 `Defs/CherryPicker.xml` has `TMM_CherryPicker_Basics` with **zero base
members** and `TMM_CherryPicker_All` with **443 raw members**. Its
`Patches/ZZ9_AutoCherryPicker.xml` and other conditional patches append members, including duplicate
additions and settings-controlled branches. Do not equate raw counts with unique, active or resolvable
content. Do not copy this provider's hundreds of names into megamod. Inspected hashes:

- Def XML: `7CF7B3386D5B30B444F11473455C2082B5A5305D4E96E8F9530D4018C4BB95F4`.
- Auto XML: `9F7DD0EED80C0F3E9BC18F6F9C261A7C2E7A538379177F60AB899D2CCE2A8F4F`.

The [provider's description](https://steamcommunity.com/sharedfiles/filedetails/?id=3250762483)
identifies Cherry Picker as the consumer of its removal lists. This is a real drop-in target;
the entire provider's unrelated rebalance suite was not audited. Literal source/XML/JSON scans and
bounded DLL string scans cannot rule out dynamic or uninstalled consumers. No public runtime API
emulation beyond a proven use is justified by the existence of public/static members alone.

## Native XML adapter feasibility

Read native LoadedModManager, DirectXmlLoader, DirectXmlToObject, DirectXmlToObjectNew and
XmlInheritance. Patches run before inheritance/Def parsing and before MayRequire/MayRequireAnyOf
pruning. The **default 1.7 new deserializer** resolves the Class attribute before type lookup;
the legacy DefFromNode path instead looks up node.Name despite reading Class.

This supports a proposed narrow adapter: retain the `CherryPicker.DefList` XML element and all
provider-added members/XPaths, set an owned Class, and replace a sole original-package MayRequire
condition with a gate accepting the owned provider. Do this only when the original provider is
absent, the exact authored node/gate shape is understood, and the active parser honors Class.
Retain unrelated DLC gates and existing MayRequireAnyOf semantics; unsupported composite gates
must be skipped rather than weakened. Legacy-parser support is unresolved, not assumed.

This avoids duplicate `CherryPicker.DefList` CLR types and does not spoof PackageId. Original
package checks elsewhere, early provider patches gated on that package, hard dependencies or
assembly-specific calls remain unsatisfied. Preserve the original node name because real provider
patches target it. Attribute-only changes must be atomic and provider members merge-preserving.
Implementation must inspect the exact XML Extensions operation bodies, ordering and warning path;
the public Conditional docs and installed operation type inventory are supporting design evidence,
not acceptance of a composed adapter. No XML has been changed in this specification pass.

## Proposed owned behavior and intentional deviations

1. Keep all 45 source families and three optional families in scope, with human-readable effects.
   Do not turn the catalogue into an arbitrary Def editor. Show unavailable integrations honestly.
2. Common global/default and per-save choices, all available by default. Apply one complete policy
   at new-game/load boundaries; edits during play are visibly pending until save/reload. Main-menu
   choices use global policy. Avoid process-global destructive Def edits so saves remain isolated.
3. Individual exclusions and active preset exclusions are independent reasons. Nested membership
   resolves once; duplicate/cyclic/missing keys cannot loop or erase unrelated choices.
4. Exclude future selections/production through explicit behavior boundaries while preserving
   existing physical items/pawns/worlds, prices/nutrition/components and ordinary work costs.
   Existing excluded apparel cannot be newly equipped; already worn apparel is retained. Existing
   hediffs/quests/genes remain; newly applied excluded conditions are refused where supported.
5. Block a recipe with a disabled product or any exhausted required ingredient group. Never delete
   a required cost. Disabled research is bypassed explicitly with its surviving ancestral requirements
   preserved; cycles/unresolvable dependencies leave the affected choice unavailable with a reason.
6. Hiding a work column does not disable jobs; hiding a tab does not remove its mechanic. No source
   visibility default is overwritten on restoration. Debug spawn hiding stays a developer surface.
7. S020 selectors, S029 drug policy, S012 bill browser, S014 whole-list workbench linking, S036
   Architect and shared search/hauling data compose directly. Physical stock remains indexed even
   if future acquisition is excluded. Blocked linked bills retain work/quota state without a false
   completion or duplicate credit.
8. Original-provider presence suppresses owned application and adapter work with a bounded warning.
   No original settings are changed. Automatic source-settings import is deferred, with no data-loss
   claim or invented public migration feature.
9. Source-supported fixed scenario supplies are omitted when excluded, with a setup explanation
   and no replacement goods. Butchering/destruction byproducts omit excluded results without
   increasing other yields; a butchering operation with no allowed result is unavailable. These
   deliberate reductions do not alter base yield stats, existing stock or ordinary work costs.
10. Drug exclusion depends on admitted and successful owned S029 maintenance. Original
    `Mlie.DrugPolicyFix` causes S029 to withhold owned maintenance; its source tests the drug's
    ingestible/category fields, which our preservation rule leaves intact. In that combination,
    withhold drug-item exclusions with a reason while keeping other families active. No claim that
    original Drug Policy Fix understands the owned policy. Application failure must not publish
    mismatched exclusions/policy rows.

The choices above are proposed corrections under the user's replacement/settings rules, not new
user-confirmed numbers. There are no balance multipliers. Exclusion itself changes progression,
loot and threats; those consequences must remain explicit rather than hidden by fallback content.

## Recorded questions and verification boundary

| ID | Remaining work |
| --- | --- |
| CK-Q1 | Exact effective 1.7 seams per family, load-boundary policy installation, dependent-choice handling, atomic groups and later repatch admission; no blanket source-patch reuse |
| CK-Q2 | Authored-preset adapter on the selected parser, XML Extensions operations/transactionality, composite dependency gates, actual provider callbacks and unsupported package/assembly bindings |
| CK-Q3 | Review proposed reload-only timing, reason-aware preset restoration and safe dependency behavior; implementation must preserve the observable contract or revise it explicitly |
| CK-Q4 | Optional Ancients/VEF/VPE/Senator consumers, current member shapes, DLC active-versus-installed handling and exact absent/present/changed-provider native evidence |
| CK-Q5 | Potential future opt-in source-config migration: backup, preview, unresolved keys, atomic owned write and restoration; deliberately not promised by this change |
| CK-Q6 | Focused native workflows for every family, save switches, composed policies/UI, unsupported choices and measured shared-data costs; nothing implemented or accepted |

Semantic balance review: preserve material/work/price/nutrition values and existing goods; block
impossible orders instead of granting free production; preserve surviving research ancestors and
expose the intentional unlock consequences; decline impossible generation instead of endlessly
rerolling, inventing resources or silently re-enabling a disabled dependency. This is a prose pass,
not finalized-Def or gameplay balance evidence.
