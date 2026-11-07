# S104 — No Burn Metal research

Owner **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **RimWorld 1.7 beta**.
Origin replacement **S104/P2-34**. Inspected2026-11-05. This is source/native research and proposed
composition, not implementation or live acceptance. [Coverage](../replacements/no-burn-metal.md),
[proposal/design/tasks](../../../../openspec/changes/specify-megamod-metal-fire-resistance/) and
[observable contract](../../../../openspec/changes/specify-megamod-metal-fire-resistance/specs/megamod-metal-fire-resistance/spec.md)
separate evidence, intent, technical approach and requested behavior.

## Exact sources and inspection order

1. [Steam description](https://steamcommunity.com/sharedfiles/filedetails/?id=1923990111) and
   [author repository](https://github.com/Nonunon/NoBurnMetal): Steam initially returned429; the
   later ordinary-URL retry succeeded. The author README was available before code inspection.
   Public claims: familiar metals and wiring stop burning; other mod metals are discoverable;
   per-item immediate settings since1.9/for1.6+; mid-save add/remove and preserved source attribution.
   These claims are not native acceptance. Rendered Steam page boilerplate is not evidence of a
   removal or incompatibility decision.
2. Installed root `F:/Steam/steamapps/workshop/content/294100/1923990111`, package
   **`Unon.NoBurnMetal`**, About authorUnon, Workshop creatorsNonu/Dr Zhivago. About supports1.0–1.7,
   has no modVersion, runtime dependencies or load hints. `About/Manifest.xml` is **1.9.2**.
3. Public revision **`2c8981324be33d662b6292545ad89b09ac9a8ac0`**. Read README, LICENSE,
   `Source/NoBurnMetal/{NBMTargets.cs,NBMTargetsDef.cs,NBMSettings.cs,NoBurnMetalMod.cs,NoBurnMetal.csproj}`,
   selected target XML and English/Russian keys; compare selected DLL via offline decompilation.
4. `loadFolders.xml` selects **Common +1.7**. Root/1.4/1.5 historical PatchOperations are not selected
   in1.7. The current mod is C# settings plus Def mutation, not the old XML-only patch set. In
   particular, a commented old bioferrite patch does not override the current curated target list.
5. Selected `1.7/Assemblies/NoBurnMetal.dll`: AssemblyVersion **0.0.0.0**, FileVersion **1.9.2.0**,
   InformationalVersion **1.9.2**, MVID **`b1efb808-d3da-4952-94b0-8947f6c477c0`**, SHA256
   **`B0DCC1905D8B5031E5A200F2B352CC9E22B4CAC35B3016E6241E84FFAA5177C5`**. The public committed1.7
   DLL has the same hash. No Workshop assembly was executed or copied into a product package.
6. Native **1.7.4998 rev1299**, Assembly-CSharp SHA256
   **`FE9B615CF5918955AB750F9D627FB0082FD23C556BDFDEE410F88857C67A081B`**. Read relevant decompiled
   `StatWorker`, `StatPart`, `StatRequest`, `StatDef`, `Fire`, `FireUtility`, `Thing`,
   `DamageWorker` and `DamageWorker_Flame` paths and native material/power/stat/structure XML.

Public source has GPL-3.0 LICENSE. Preserve Unon/Nonu, Dr Zhivago's earlier Expanded Materials work
and Antyhc's Russian translation attribution. Independently implement the observable contract;
this change copies no upstream code, binary or art. Local ignored evidence lives under
`artifacts/Refinement/megamod-spec-20261105/no-burn-metal/`: public checkout, selected DLL decompilation,
native method decompilations, `source-manifest.json`, `native-manifest.json` and
`provider-xml-observations.json`. Hashes identify the inspected artifacts, not runtime admission.

## Current source discovery and defaults

`NBM` initializes once after Def loading. It unions all `NBMTargetsDef.defNames`, then discovers
every ThingDef that is curated or whose Stuff categories include native Metallic. Any Stuff target
uses its material Flammability factor; a non-Stuff curated target uses its base Flammability.
The first matching explicit entry is captured as nullable `original`. Only an explicit zero is
skipped. Missing material factors are therefore included; missing direct bases can also be included
even when their effective default is already zero. Duplicate/nonfinite inputs are not guarded.

`Common/Defs/NBM_Targets.xml` contains **36 curated names**:

| Expected provider | Source identifiers | Default and evidence |
| --- | --- | --- |
| Core (`ludeon.rimworld`) | `Steel`, `Silver`, `Gold`, `PowerConduit`, `WaterproofConduit`, `PowerSwitch` | On if discovered; exact Core1.7 XML inspected |
| Anomaly (`ludeon.rimworld.anomaly`) | `Bioferrite` | Curated on; local Anomaly content unavailable, optional gate open |
| Alpha Animals (`sarg.alphaanimals`) | `AA_SkySteel` | Curated on; source-declared mapping, provider absent locally |
| Medieval Overhaul (`DankPyon.Medieval.Overhaul`) | `DankPyon_IronIngot` | Curated on; source-declared mapping, provider absent locally |
| VFE Classical (`OskarPotocki.VFE.Classical`) | `VFEC_Bronze` | Curated on; installed2787850474 selects root+1.6 for1.7 |
| More Useful Materials (`CN.MoreUsefulMaterials`) | `MUM_CarbonFiber`, `MUM_HeavyDUComposite` | Curated on only if contribution not already zero; installed3734307960 advertises1.6 only |
| Cupro's Alloys Continued (`Mlie.CuprosAlloys`) | `CAL_Plastin`, `CAL_BlackSteel`, `CAL_Brass`, `CAL_ChromePlatedSteel`, `CAL_Aurichalcum`, `CAL_SterlingSilver`, `CAL_RoseGold`, `CAL_Electrum`, `CAL_Plumchalcum`, `CAL_Duralumin`, `CAL_Zamak`, `CAL_BismuthBronze`, `CAL_Bronze`, `CAL_Cupronickel`, `CAL_StainlessSteel`, `CAL_Hepatzion`, `CAL_Copper`, `CAL_Lead`, `CAL_Tin`, `CAL_Aluminum`, `CAL_Nickel`, `CAL_Zinc`, `CAL_Bismuth`, `CAL_Chromium` | Curated on; source-declared mapping, provider absent locally |

Everything else discovered is **off**, even if its provider is in `knownMods`. That six-entry list
only supplies inactive provider headers: Expanded Materials, Alpha Animals, Classical, Medieval
Overhaul, Cupro's Alloys and More Useful Materials. Curated lookup itself is DefName-only, not
package-qualified: our provider/shape guards intentionally improve that ambiguity.

Current installed provider observations, **raw XML rather than finalized active-game results**:

| Input | Material factor / own base | Consequence under inspected source |
| --- | --- | --- |
| Core steel, silver, gold | Factor0.4; own base absent, native default0 | Ordinary made-from objects affected; loose resources are not additionally changed |
| Core plasteel, uranium, jade | Factor0 | No source target row requiring protection |
| Core wood | Factor1, own base1 | Not a metallic or curated target |
| VFE Classical bronze | Factor0.4; own base absent | Default-on material; no loose-resource base rewrite |
| MUM carbon fiber composite | Factor0.3; own base absent | Default-on if usable content is actually selected/admitted |
| MUM tempered steel | Factor0.1; own base absent | Discovered but default-off, despite being from an advertised provider |
| MUM heavy DU-composite steel, advanced ceramics, apex alloy | Factor0; own base absent | Already zero, skipped by source |
| Expanded Materials1.7 iron, mild steel, tempered steel, lead, bronze, copper, stainless steel, titanium | Factor0; own base absent | Already zero, skipped; advertising this provider does not imply new enabled effects |

Expanded Materials' installed3333419387 remains `Argon.ExpandedMaterials.Metals`, supports1.7 and
selects its1.7 folder plus conditional patches. Its coal/charcoal are not metallic Stuff targets.
More Useful Materials' `loadFolders.xml` contains only1.6; no accepted1.7 compatibility or finalized
selection is inferred. The native assembly has DLC types but the local Data directory lacks Anomaly
content. Missing optional providers are research/acceptance gates, not reasons to ask the user to
explain their behavior.

## Mutation, persistence and UI facts

`NBMTarget.Apply(true)` sets the first selected factor/base entry to0 or appends it if missing.
`Apply(false)` restores the startup-captured value, or removes the current entry if originally absent.
It preserves other list entries but can overwrite later legitimate changes. It does not alter a
Stuff material's own base, floor terrain, `burnableByRecipe`, armor, fuel eligibility or power logic.
After startup or every `NBM.SetOn` it clears the Flammability worker cache; bulk loops clear repeatedly.
No Harmony patches or runtime dependency exist in this selected source.

`NBMSettings` is global `ModSettings`: `enabled`, `disabled` DefName sets, `collapsed` provider IDs
and `hideInactiveMods` defaulttrue. Enabled wins if both sets contain a name; ordinary writes remove
the opposite entry. Missing keys use curated status. Inactive content's keys remain stored. There
is no save component. Turning source settings on/off mutates process-wide Defs for all saves.

Source `NoBurnMetalMod.DoSettingsWindowContents` provides:

- Mod settings entry only; **no bottom tab, bottom-right switch, entity command or debug action**.
- Label/DefName/provider-name case-insensitive substring search; actual originating provider groups,
  Core then DLC then other active mods, alphabetic labels; optional inactive known-provider headers.
- Per-row click and checkbox; native Def icon, current label, small stable identifier, On/Off state
  and tooltip with source, factor/base and captured numeric or unset value.
- Global Enable all/Disable all and per-provider All on/All off affect **all respective targets**,
  regardless of the search. Source summary/provider counts also count all their targets.
- Individual and all-group collapse/expand; search shows matching rows despite collapse without
  rewriting collapsed state. Hide inactive defaults on. Search/scroll transient; collapse global.
- English/Russian keyed localization only. Fixed row/toolbar widths can collide with narrow layouts
  or long translations; owned rows must not copy that layout or reset unrelated GUI state blindly.

The source UI says Fireproof for On and Default flammability for Off; its public README describes
Off as Yes Burn. Fireproof and Yes Burn are overbroad when independent offsets remain or the
ordinary value is already zero. Our settings explain the selected contribution, current resulting
effect and actual admission separately. The user-required common global/save model deliberately
supersedes the source's global-only gameplay choices. UI navigation preferences remain common
global-only exceptions with reasons; no separate settings framework is introduced.

## Native fire and stat facts

`StatWorker.GetValueUnfinalized` starts with `GetBaseValueFor`, applies the material factor under
native sign conditions, then adds Stuff offsets and other contributors. `FinalizeValue` runs
StatParts, optional postprocessing, scenario factors, rounding and final clamp. A zero material
factor is not a guaranteed zero final value. Flammability's inspected default/minimum is0, maximum2;
minified objects inherit their inner Thing's value. Native explanation paths display these stages.

`Thing.FlammableNow` rejects values below0.01 before other native location/extinguishing rules.
`FireUtility` uses Thing stats for attachment and separate terrain/foundation flammability for ground
fire. `Fire.DoComplexCalcs` re-evaluates fuel every150ticks: it excludes objects below0.01, destroys
an ordinary fire with no object/terrain fuel, otherwise damages a selected object or its attached
parent and emits ordinary heat. No settings callback needs to scan fires or manufacture extinction.

Direct damage is distinct. `DamageWorker_Flame.Apply` applies damage before ignition.
`DamageWorker.Apply` scales qualifying building damage by at least0.05 even with zero Flammability.
Burning pawns can inflict direct damage on worn apparel. Zero flammability therefore does not imply
invulnerability, wearer protection or zero floor/room heat. Fire response and damage integrations
must preserve these boundaries instead of inventing immunity.

Static comparators (finalized native runs remain required):

| Object/material | Inspected ordinary flammability | Other inspected native anchors | Intended change |
| --- | --- | --- | --- |
| Standard wall base / steel / wood / plasteel | Base1; factors0.4 /1 /0 | Wall baseHP300/work135; material properties still apply | Steel contribution0; wood and plasteel unchanged |
| Power conduit | Base0.7 | HP80/work35/steel1 | Base0; all other properties retained |
| Waterproof conduit | Inherits conduit base0.7 | Steel10 | Base0 without mutating shared parent data |
| Power switch | Base0.5 | HP120/work200/steel15/component1 | Base0, ordinary switch operation retained |
| Hidden conduit | Explicit base0 | HP100/work280/steel2, additional native protections | No redundant protection choice or rebalance |

This makes cheap metal infrastructure less vulnerable to ordinary fire, as the selected source
intends. It adds no research, material cost or HP compensation. Common choices allow opting out;
source-default bioferrite/carbon fiber and cheap-wire advantages need final playtesting. No new
recipe, trader stock, yield, stack size or economic production loop is introduced.

## Contracts, consumers and composition

Public source surfaces include `NoBurnMetal.NBMTargetsDef` (`defNames`, `knownMods`),
`NBMKnownMod` (`packageId`, `label`), `NBM.Targets`, `Sections`, `IsOn`, `SetOn`, `NBMTarget`
(`def`, `viaFactor`, `curated`, `original`, `Key`, `Entry`, `Apply`) and the global settings types.
Source Common XML instantiates its custom Def. External authors could rely on those names, but
no such external caller is established by the bounded installed scan.

ASCII searches across installed XML/C#/DLL for package and `NoBurnMetal.NBM`/`NBM_Targets` found
the source and only YART (`seohyeon.yart`,3745434121) evaluation fixtures
`Source/LayoutHarness/eval/modlists/rimsort_{1,2,3}.xml`, which contain the package name in sample
load lists. These are not runtime calls or a requirement to emulate an assembly. This search does
not prove all reflection/UTF16 strings or future consumers absent. No package identity, public
class facade, global settings-file import or source configuration migration is promised.

Original provider present: source owns the full overlap; owned preferences stay dormant and UI
explains that our Off cannot undo source On. No foreign unpatching or snapshot restoration.
Optional XML content uses package/content/shape guards and merge preservation; all other material
stats, category membership and fuel lists remain untouched.

Owned composition: S072 structural damage still scales its eligible damage once; S079 fire response
and S091 corpse burning retain their pawn/corpse rules; S096 processing orders still consume eligible
items; material/recipe inspection uses current resulting stats. Implement direct owned composition,
not a stack of patches on our own features. Definition descriptors can share an existing catalogue;
no new map/Thing/fire cache or hauling index is needed for this feature.

## Proposed differences and open work

| Difference | Reason and contract |
| --- | --- |
| Common per-save gameplay overrides | User requirement; dynamic contributions replace process-wide mutable Def values |
| Package-qualified curated choices and finite/unambiguous guards | Avoid activating unrelated content with a colliding name or ambiguous semantics |
| No redundant effective-zero/nonpositive control | Source tests only explicit0; owned UI must not promise a useful toggle for an absent direct base whose ordinary default is0 |
| Off reveals current other-provider values | Avoid restoring a stale startup snapshot over later legitimate changes |
| Accurate contribution/availability help | Do not claim direct damage immunity or that Off forces burning |
| Explicit bulk scope, atomic commit and one invalidation | Preserve all/provider convenience without hidden target ambiguity or partial updates |
| Seven languages and responsive common layout | Replace source's English/Russian-only fixed geometry |

| Question | Type | Remaining work |
| --- | --- | --- |
| NBM-Q1 | Runtime admission | Prove exact effective factor/base/explanation/cache/lifecycle seams and coherent failure/repatch groups |
| NBM-Q2 | Optional contracts | Resolve missing1.7 provider/content shapes, finalized compositions and actual public/reflection consumers |
| NBM-Q3 | Balance/UI/performance | Finalized fire/damage comparisons, source-default optional choices, crowded seven-language UI and bounded stat/catalogue work |
| NBM-Q4 | Acceptance | Focused nonzero RED/GREEN, reviewed implementation and causal native fire/construction/inspection/settings/save/provider evidence |

No implementation task can close on this research. Native acceptance must use the reviewed build,
fresh minimized isolated process, ordinary player construction/settings/inspection/fire/processing
paths, personally inspected action/result views and complete root evidence/cleanup requirements.
