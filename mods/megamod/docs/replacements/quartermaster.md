# Quartermaster — comparison inside Add bill

## Identity and provenance

- Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), target **1.7 beta**.
- Origin: **source-inspired replacement subset**, S009 / P1-09. The interview explicitly requests
  comparison inside Add bill, not a standalone automatic loadout recommender.
- Source: **Quartermaster**, `slim.quartermaster`, Workshop **3737474260**, installed author **SLIM**;
  Steam displays **Ad Matay**. About supports 1.6/1.7, declares no required mods and hints Core first.
- Documentation first: [author's Workshop description](https://steamcommunity.com/sharedfiles/filedetails/?id=3737474260)
  and [change notes](https://steamcommunity.com/sharedfiles/filedetails/changelog/3737474260), inspected
  2026-11-02. Description lists equipment/material comparison, set optimization, bill and policy actions;
  it explicitly warns that CE calculations are not directly supported. The fetched older change-log
  snapshot is not authoritative for the current installed UI. Generic Steam banners prove no incompatibility.
- Installed read-only `F:/Steam/steamapps/workshop/content/294100/3737474260`, root plus `1.7` selected.
  `1.7/Assemblies/Quartermaster.dll`, assembly version **0.0.0.0**, SHA256
  **C7091A86432ECB8ABBB77A14758BF5A955EEF94077F3EC5250D5B5700D202B82**.
- No shipped C# source/license was found and a targeted search did not identify an author repository.
  License remains unresolved; independently implement behavior, with no copied code, assemblies,
  art or translations. This report records attribution, not permission to redistribute source material.
- Static evidence and exact native game hash are retained in the
  [research journal](../refinement/2026-11-02-quartermaster-research.md). Ignored decompilation lives
  under `artifacts/Refinement/megamod-spec-20261102/quartermaster/{decompiled,game,consumers}`.
  No source UI or behavior was exercised in a running game for this prose draft.

Owning [proposal/design/tasks](../../../../openspec/changes/specify-megamod-quartermaster-comparison/)
and [observable capability](../../../../openspec/changes/specify-megamod-quartermaster-comparison/specs/megamod-material-aware-recipe-comparison/spec.md).

## Behavior inventory

Evidence paths below are relative to the decompiled `Quartermaster/` namespace unless marked native.
“Specified” means a drafted contract, never implemented or accepted.

| ID | Source input/action → result | Defaults and boundaries found | Evidence | Megamod disposition / owning requirement |
| --- | --- | --- | --- | --- |
| QM-01 | Open Quartermaster → armor, weapon and buff views | Icon-only bottom tab, MainButton order 18; movable/resizable window with global position/lock | Root `Defs/MainButtonDef.xml`; `MainTabWindow_BestArmor.PreOpen`, `DoWindowContents` | Adapted: **Comparison belongs to the selected workbench**; no owned bottom tab |
| QM-02 | View recipes → research/bench/material availability | All single-product apparel/weapons globally; duplicate product recipes collapsed; no skill check | `ArmorEvaluator.GetAllArmorRecommendations`, `GetAllWeaponRecommendations`, `IsResearchDone`, `IsBenchAvailable` | Specified correction: per-bench distinct recipes and native unlocks; **Highlight supplies and skill without promising immediate work** |
| QM-03 | Compare armor and utility → scores, material, tooltip | Combined armor weights; abstract armor/insulation; base/equipped/first-hediff-stage utility values | `ComputeScore`, `FillApparelStats`, `GetStatBase`, `ApplyWornHediffEffects` | Adapted: **Compare stated attributes at Normal quality**; no unlabeled score or predicted pawn total |
| QM-04 | Sort weapon table or choose range bands → reordered comparisons | Score descending; all four range bands on; at least one retained; AP-weighted estimated DPS | `FillWeaponStats`, `ApplyScoreBands`; window weapon headers/comparer | Adapted: **Rank by the chosen attribute and direction**; custom combat formulas/range-scoring controls omitted |
| QM-05 | Best possible toggle → compare unavailable materials | Off initially; source favors non-Gold/Jade/Silver before score and can use a global material pick | `PickBestStuff`, material resolvers; window `OnBestPossibleChanged` | Specified: **Available-material comparison is the default** and **All-material comparison supports future plans**, with user's checkbox polarity and stat-based choice |
| QM-06 | Select minimum copies / inspect shortfall → recipe/set quantities | Initial 1; product construction costs and stored resource counts; set budget can select other materials | `SetBudget`, `CraftableCount`, `FixedCostCopies`; window `EnsureSetNeeds` | Adapted: **Ingredient availability is a finite allocation** for one actual recipe execution; whole-set quantity optimizer omitted |
| QM-07 | Click material cell → Auto or material pick | Global per-product pick; menu includes materials counted for at least one copy and current missing pick | `OpenMaterialMenu`, `MaterialPickManager` | Specified: **Alternate materials are explicit bill choices**; no global pin or automatic import |
| QM-08 | Craft → queue bill; Craft all → queue set across benches | First matching colonist worktable, ordinary/unfinished bill only; repeat count at least 1; set can partially succeed | `TryCraftBill`, `DrawCraftButton`, `DrawCraftAllButton` | Adapted: **Adding a compared recipe preserves the chosen material** on opened bench via native bill behavior; Craft all omitted |
| QM-09 | Open item information → native information card; Focus → owned thing/pawn | Existing objects carry actual quality/material; Show crafted on, tainted/selected-pawn gear off | Window `OpenInfoCard`, `CaptureOwnedPool`, `DrawOwnedHeader`, `DrawCraftButton` | Product information retained in composed browser; owned/worn equipment pools, real-instance ranking and Focus omitted |
| QM-10 | Choose armor/preset/slider → compatible set and body-region diagram | Default armor cap 200%; at most 20 selected pieces; weighted coverage/agility rules | `RunGreedy`, `RunPresetGreedy`, `MainTabWindow_BestArmor` summaries/diagram | Deliberately omitted from confirmed scope: automatic set optimization and protection diagram |
| QM-11 | Select wearer/nudist profile → filter equipment | Adult/child/race profiles; optional reflected HAR checks; nudist/strict distinction | `WearerFilter`, `HarCompat`, preset editor | Omitted from this future-product comparison; no pawn-specific wearability claim or generic race adapter |
| QM-12 | Hide/pin items or materials → recommendations change | Empty global lists initially; exclusion unpins; category confirmations; explicit heavy/tag suggestions | `ExclusionManager`, `EnforcementManager`, `MaterialExclusionManager`, related dialogs/detectors | Omitted global exclusions/pins; recipe filters and explicit one-bill material choices use their own composed behavior |
| QM-13 | Create/edit/restore preset → saved weighted-stat selection | Global strings; first-load/default migrations; restore adds absent names, not overwrite | `CustomPresetManager`, `Dialog_BuffPresetEditor`, `PresetCategory` | Deliberately omitted role/preset manager; selected product-stat sort is not a role optimizer |
| QM-14 | Create policy → native apparel policy from current set | New unique name; apparel types allowed; no material/quality assignment | `CreateApparelPolicyFromSet`, `UniquePolicyName` | Deliberately omitted policy-creation workflow; no silent equipment orders |
| QM-15 | Search/filter/refresh → refreshed source lists | Buff search by stat/item/slot; type toggles; source requires explicit refresh for some stock changes | Window `DrawBuffSearchRow`, `FilteredBuffGroups`, `HardRefresh`, `PreOpen` | Specified freshness via **Refresh and revalidate without changing the player's intent**; S012 owns search/category/initial-order composition |
| QM-16 | Bad/missing data → source catches or empty/default fields | Some stat failures become zero; source recipe grouping drops special/multiple outputs | `ArmorEvaluator`, native `RecipeDef.ProducedThingDef` | Specified correction: **Unsupported comparisons and material locks fail visibly**, no hidden valid recipe |

## Interactions and composed result

S010 Nice Bill Tab and S011 Nice Bill Tab - Expansion are separate reference-source turns; S012 is
the original richer Add bill view. It will own search, category pills derived from bench products,
common-stat ordering, layout and the final alternate-material gesture. This source slice owns the
meaning of comparison, ingredient/skill status and material-to-bill choice. No internal Harmony
patches between these future owned features are allowed.

The shared map Thing index used by Ctrl+F and hauling must supply candidates here as well; no
Quartermaster-style parallel map resource/owned-equipment cache is ported. Whole-list workbench
linking (S014) will explicitly compose native add/edit propagation; this comparison must not
independently duplicate bills. Later material-selection providers must respect an explicitly
material-specific bill instead of silently defeating the displayed choice; inspect their real
behavior in their own turns before declaring a conflict.

## UI audit and relocation

| Source entry / purpose | Classification | Megamod destination and navigation change |
| --- | --- | --- |
| Quartermaster bottom tab / inspect and optimize gear | Mixed gameplay/comparison window | Selected comparison goes directly in Add bill; no copied tab |
| Armor/weapon/buff tabs, sort headers and material cells | Contextual comparison controls | Relevant stat sort, Use all materials and alternate choice in Add bill |
| Craft / one recipe | Contextual native gameplay order | Opened bench's ordinary bill list with material choice preserved |
| Craft all, policy creation, pins, wearer/set optimizer | Other gameplay management | Outside confirmed scope; not relocated or secretly added |
| Exclusion/preset editor, range-scoring preferences, window geometry | Source configuration | Not adopted; no source settings import. Any later adopted configuration requires its own common-scope contract |
| Item information / owned-item Focus | Inspection/navigation | Output information remains available; owned-gear Focus omitted |
| Refresh and Show unavailable/crafted | Source view controls | Automatic honest freshness; source display modes not copied wholesale |
| Recipe comparisons enabled | New actual configuration | Common mod-settings section Crafting / Add bill, default on, global default plus save override |

Use all materials, sort and pending alternate material are contextual state for one open view. They
reset on a fresh opening; Use all materials starts unchecked. Native bill ingredient choices are
entity gameplay state and persist with the bill. The enable setting uses the shared loader/renderer,
retains dormant overrides, disables save edits at the main menu and applies on next opening.
All labels/unknown explanations need the seven baseline languages during implementation.

## Contracts consumed by other mods or saves

| Evidence / consumer | Contract | Disposition and verification |
| --- | --- | --- |
| No confirmed incoming consumer in installed XML/C#/JSON/Markdown/text scan | Package/Workshop ID and selected public type names | No fabricated facade; broader/future reflective consumers remain QM-Q3 |
| DLL-string candidate `wowgag.RimPacts`, Workshop `3762723122` | Its own `RptWarOffice.Quartermaster` enum used by `Hediff_RptWarOffice`; no source-mod binding found | False-positive name match, not a reason to emulate the Quartermaster assembly |
| Quartermaster MainButton and public evaluator/managers | `Quartermaster` MainButtonDef, `MainTabWindow_BestArmor`, `ArmorEvaluator`, global managers | Not promised compatible callable replacements; original optional window remains independently owned |
| Quartermaster mod configuration | Global window/lock, exclusions, pins, material picks, preset strings, range flags and migrations | No automatic import or rewrite; selected settings follow megamod's common scope model |
| Native bill/save consumers | Bills use normal recipe/style/filter/repeat data; source-created policies are native objects | New compared bill uses native factory/defaults; preserve ordinary saved bills, but no automatic save migration or blanket uninstall claim |
| Optional HAR integration in source | Reflected static `CanWear(ThingDef, ThingDef)` | Not adopted; the selected comparison does not promise pawn wearability |
| Optional third-party recipes/stats/ingredient containers | Actual finalized recipe/stat/filter/worker/haul-source semantics | Narrow package-ID/shape-guarded support only after evidence; unknown comparison with ordinary bill fallback otherwise |

Source declares no required runtime mod. Metadata says 1.7 support but does not prove our new behavior
or coexistence. Quartermaster may remain installed with its separate UI; megamod does not hide or
patch it. Exact present/absent-provider native workflows are future acceptance tasks. CE was explicitly
unsupported for accurate source calculations and no new CE accuracy claim is made here.

## Implementation and coverage

**Implemented: nothing.** QM-01–08, the output-information part of QM-09, QM-15 freshness and QM-16
have an explicit adapted contract. Detailed search/layout is deferred to S012. QM-09 owned-gear
features and QM-10–14 set/role/policy workflows are deliberately outside the confirmed request.
No source replacement, saved-state migration or external binding is accepted by this document.

Intentional deviations include per-recipe costs instead of product construction costs; loose eligible
ingredients as well as storage; finite allocation; explicit same-colonist skill checks; Normal
comparison; selected-stat material ranking without precious-material demotion; native special bill
types; current-bench destination; unknown rather than zero; automatic freshness; common settings
scope; and explicit material choice per new bill instead of global product preferences.

Before any hook is implemented, add its documented/executable semantic target admission, failure group,
warning and native fallback. No blanket exact-body/version/no-other-patch test is allowed. XML is not
presently needed; if introduced, content/package/shape guards, merge preservation and XML Extensions
dependency rules apply. The source DLL remains inspection input and must never enter the package.

## Verification and acceptance

This is a source/contract review only. Implementation tasks remain unchecked. Required future evidence
includes nonzero focused RED/GREEN, native same-bench add and actual production of the chosen material,
precise resources consumed, Normal preview versus actual quality, fixed/overlapping ingredients,
missing skill/power, stale selection, unknown provider data, settings scope, map/save switching and
original-provider present/absent behavior. Perform these through native player paths on the reviewed
build in fresh minimized isolated game processes; retain causal screenshots and root-required evidence.

Semantic pass for this draft: comparison creates information, not resources or stronger items; choosing
gold can be expensive but is an explicit consequence of choosing that stat, with alternate material
available. All-material mode is a planning aid, not a material/skill/research bypass. Showing sufficient
supplies does not reserve them across bills or guarantee a reachable worker. Exact live economic,
production and performance outcomes remain unverified. QM-Q1–QM-Q4 are tracked in the research journal
and [sequential ledger](../refinement/specification-progress.md).
