# S099 — Spines Matter More Now research

Owner **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
Origin replacement P2-28. Inspected **2026-11-05**. Documentation/source/native analysis only;
no upstream execution, runtime implementation, game launch or live compatibility acceptance.
[Owned change](../../../../openspec/changes/specify-megamod-spine-manipulation/) separates observable
behavior from design and future verification work.

## Documentation and exact provenance

Read the [Workshop page](https://steamcommunity.com/sharedfiles/filedetails/?id=2971629404) first.
It describes making Manipulation depend on spinal efficiency as Moving does. The page credits
Jdalt40, advertises through 1.6 and explicitly reserves reuse/reupload rights. Independently implement
the described behavior; do not copy or redistribute the source code, DLL or preview. A historical
comment describes a custom spine class, but the selected code is a Harmony postfix, so that comment
is not the implementation contract. Comments about working versions do not prove beta acceptance.

| Input | Inspected identity |
| --- | --- |
| Installed root | `F:/Steam/steamapps/workshop/content/294100/2971629404` |
| Title / author | Spines Matter More Now / About Jdalt, Workshop Jdalt40 |
| Package | `Jdalt.SMMN`, canonical `jdalt.smmn` |
| Supported / selected | About 1.4/1.5/1.6; no load-folders declaration or 1.7 directory; native fallback selects 1.6 |
| Selected assembly | `1.6/Assemblies/SMMN.dll`, assembly/file 1.0.0.0 |
| DLL SHA256 | `5F98A1F32CD8916315591DC03EB689366E8FC921872E319E6171C1BA7A21613D` |
| Complete behavior source | `Source/SMMN/HarmonyPatches.cs`, SHA256 `F171FA321DE4BE297DDA32730BB12E5A1290C500F6D447E459E05FE5E13C5899` |
| About SHA256 | `B8568F61D0215320518F06AA7F36FF8C3405E1ABC6528CEAC32C78AC15840B9A` |
| Native target | 1.7.4998 rev1299; Assembly-CSharp SHA256 `FE9B615CF5918955AB750F9D627FB0082FD23C556BDFDEE410F88857C67A081B` |

Read the complete source, project/packages/assembly metadata and selected compiled type. Source and
binary behavior agree. Project targets .NET Framework 4.7.2, references Harmony 2.2.2 and private-false
game/Unity dependencies. About only load-orders after Core and omits actual Harmony dependency/load
order; the owned package must declare its real Harmony use. No XML, language catalog, settings,
custom body/hediff Def or world/entity persistence is supplied. Shipped obj/Debug outputs are not
the selected runtime input. Evidence is under
`artifacts/Refinement/megamod-spec-20261105/spine-manipulation/`.

## Complete source behavior

Internal static `SMMN.HarmonyPatches` initializes at StaticConstructorOnStartup and registers one
unguarded postfix on PawnCapacityWorker_Manipulation.CalculateCapacityLevel using Harmony owner
`Jdalt.RimWorld.SMMN.Main`. There is no original/effective semantic admission, coexistence handling
or feature configuration in the source.

Its public method within that internal type is
`SMMN_ManipulationCapacityLevel(ref float, HediffSet, List<CapacityImpactor>)`. It obtains native
Spine tag efficiency, passes through the optional impactor list and multiplies the existing result
by the returned factor. The selected DLL's IL confirms maximum float.MaxValue, default FloatRange,
and a sixth argument of -1 for bestPartEfficiencySpecialWeight. C# decompilation suppresses the
optional argument; this is not evidence of a mismatched five-argument call. Debug logging is only
commented source. No pawn race/faction restriction, new cache, job patch, custom UI or clamp is added.

This is an additional factor at the **worker** boundary. It does not multiply the final capacity
after all health modifiers. A second provider doing the same operation could square the factor;
original coexistence must prevent a second owned contribution rather than unpatch the original.

## Native calculation and anatomy

Full relevant reads: PawnCapacityWorker_Manipulation, PawnCapacityWorker_Moving,
PawnCapacityUtility's worker/part/tag calculations and impactor types, PawnCapacitiesHandler,
PawnCapacityDef, AddedBodyPartProps, plus HealthCardUtility's capacity tooltip path.

- Manipulation worker: native manipulation limb efficiency (digit weight 0.8) multiplied by current
  Consciousness. No spinal factor exists in the inspected 1.7 worker.
- Moving worker: leg efficiency and functional-limb threshold, pelvis, Spine tag efficiency,
  partial breathing/pumping contributions and capped consciousness. Overall Moving is therefore
  not a substitute for the Spine helper; the source does not modify Moving.
- Outer capacity calculation: wakefulness short-circuit, worker result, then—only if positive—
  hediff/gene offsets and post-factors plus the tightest maximum. It finally applies minValue and
  rounded hundredths. Native Manipulation has zeroIfCannotBeAwake and default zero minValue/
  minForCapable. Positive offsets do not ordinarily rescue a worker result already zero.
- Tag helper: iterate every body record carrying the Spine tag, including missing records; average
  their native part efficiencies at special weight -1. No tagged records returns 1. Multiple spinal
  parts are neither multiplied nor best-only. No new 100% ceiling is imposed.
- Part helper: added ancestors can supply their efficiency; missing parents return zero; direct
  added parts, actual health and stage efficiency offsets/ignore-missing-HP rules determine the
  result. Preserve the native health-loss transform rather than equating it to an invented simple
  HP ratio. The helper floors negative part efficiency at zero.
- AddedBodyPartProps.partEfficiency defaults to 1. The betterThanNatural flag is not a numeric
  efficiency bonus by itself.
- PawnCapacitiesHandler caches final values, marks them uncached through Notify_CapacityLevelsDirty,
  returns zero for dead pawns and detects recursive evaluation. CapableOf uses the capacity's native
  threshold. Reuse this health lifecycle rather than storing an extra multiplier.
- HealthCardUtility.GetPawnCapacityTip calculates through the same outer path with an impactor list.
  It renders relevant native health contributors and deduplicates hediff/body-part/gene identities.
  Source helper contribution can therefore explain the spinal part through existing UI.

These are source-derived facts, not a tested patched runtime. Exact effective provider interaction,
cache invalidation and player-visible work consequences remain implementation acceptance tasks.

## Definitions and balance comparators

Inspected Core inputs: `Bodies/BodyParts_General.xml`, `Misc/BodyPartTagDefs/BodyPartTagDefs.xml`,
`PawnCapacityDefs/PawnCapacity.xml`, `HediffDefs/Hediffs_Local_Chronic.xml`,
`HediffDefs/BodyParts/Hediffs_BodyParts_Bionic.xml` and `Stats/Stats_Pawns_WorkGeneral.xml`.

| Comparator | Inspected semantics | S099 consequence |
| --- | --- | --- |
| Healthy natural Spine | BodyPartDef has 25 base HP and Spine tag; ordinary efficiency 1 | Neutral factor; no HP/damage rewrite |
| Damaged or missing existing spinal part | Native part helper determines reduced/zero efficiency, retaining tagged anatomy | Corresponding body-based Manipulation loss; absent anatomy still neutral |
| BadBack | Native Moving offset -0.30 and Manipulation offset -0.10; no partEfficiencyOffset | Preserve later direct penalty, no invented additional spine factor from condition name |
| Core BionicSpine | Added-part efficiency defaults to 1; item costs 15 plasteel/4 advanced components; native install recipe consumes one implant | Restores neutral spinal function without adding a new implant bonus or changing surgery/progression |
| Optional RBSE BionicSpine / AdvancedBionicSpine | Installed 1.6 XML uses part efficiency 1 with later Manipulation/Moving offsets +0.10 / +0.20 | Preserve direct offsets once after the neutral spinal factor; not 110%/120% part efficiency |

RBSE is a narrow definition comparator: package `rah.rbse`, Workshop 850429707, no load-folder
declaration, selected 1.6 definitions `Defs/HediffDefs/HediffDefs_Bionics.xml` and
`HediffDefs_AdvancedBionics.xml`. Root legacy copies and translation files were not treated as selected
definitions. No whole-RBSE compatibility or final loaded inheritance claim follows from this read.

Native MiningSpeed and DeepDrillingSpeed use Manipulation with weight 1; MiningYield uses weight 0.3
and maximum 1 for that contribution. Thus enhanced Manipulation need not improve every stat above
normal and a reduction is not a universal same-percentage task penalty. S097 selects by actual
current yield and must not add another spine criterion. S045/S046's owned Consciousness still enters
once; no independent final-capacity clamp or threshold is added here.

The draft preserves the source's full anatomical factor, including above-normal supported parts,
with no speculative new setting or balance tuning. It changes derived manipulation-dependent
performance by design, while preserving part HP, Moving, costs/research, surgeries, native stat
weights and job reachability rules. Finalized anatomy/implant values and actual native work outcomes
are required by SPM-Q2–3 before any balance or compatibility acceptance.

## Contracts, UI and shared state

A completed bounded ordinary-string `rg -a -l -i` scan of installed `.dll/.cs/.xml` for package,
patch method and full internal type matched only this package's About/source/selected and legacy
assemblies. No external callable consumer was found in that scope; no claim covers all UTF16,
reflection or uninstalled consumers. No public-type facade, source assembly alias or package spoofing
is selected. The source has no custom settings/save keys to import.

Existing Health/Manipulation details are the only relevant UI, inherited through native impactors.
There is no bottom tab, settings page, operational toggle or debug action to relocate. Owned S007
and later HUD views consume the same native value; use localized names and seven-language owned
diagnostics without a parallel health panel. Individual spinal injuries/implants are pawn state.
No map/Thing cache is required, and no owned source port patches another owned port.

## Remaining work

SPM-Q1–3 retain semantic worker/helper admission, duplicate/provider handling, current anatomy/
explanations, finalized comparators and focused native acceptance. Future implementation must prove
actual injury/health inspection, manipulation-dependent work and ordinary healing/replacement,
with exact reviewed build, causal observations and provider groups. No runtime task is checked and
no source binary/art/code is copied into the product.

Independent behavior/composition and source/native/provenance reviews both found no issues. The
provenance pass also repeated the bounded consumer scan and checked the optional RBSE definitions.
Strict typed OpenSpec validation passed **121 items, zero failures**. Draft: **5 requirements,
20 scenarios and 14 unchecked implementation tasks**. Documentation review and schema validation
do not constitute runtime compatibility or gameplay acceptance.
