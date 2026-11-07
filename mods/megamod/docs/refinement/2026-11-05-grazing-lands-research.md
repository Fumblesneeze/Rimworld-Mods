# Grazing Lands research — 2026-11-05

Owner **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
S087/P2-16. The user explicitly corrected the note to **Grazing Lands**, not Grazing Area.
This is source research and a proposed combined draft, not implementation or live verification.

## Evidence and identity

Read public documentation first: [Workshop1770268130](https://steamcommunity.com/sharedfiles/filedetails/?id=1770268130)
and its linked [repository](https://github.com/catgirlfighter/RimWorld_GrazingLands).
The page describes retained pasture, a nutrition control, a configurable 5% depletion-loss default
and crop damage at a 200% default. It also claims mid-save removal; that claim is not adopted as
owned compatibility evidence. Comments are leads, not verified animal-food-policy conflicts.

| Input | Exact inspected identity |
| --- | --- |
| Installed root | `F:/Steam/steamapps/workshop/content/294100/1770268130` |
| Package/author | About `avilmask.GrazingLands`, canonical `avilmask.grazinglands`; avilmask, Workshop display avil |
| Versions/dependency | About supports 1.0–1.6, required external `brrainz.harmony`; no selected 1.7 branch |
| Selected DLL | `1.6/Assemblies/GrazingLands.dll`, assembly/file version 1.0.0.0 |
| DLL SHA256 | `47A9358401315F5D9EBF26F246B3F3162576DA25696513E95D3E77709B4B76FC` |
| About SHA256 | `88E3BFC4C0DDAD2AE9FEE951C6C8AADFF60B60CAEE1A1829065ACA39B62225E0` |
| Upstream commit | `9886905a7c6297a78300d42068ab6e6169079dbc`; committed 1.6 DLL matches installed hash |
| Active source | `source/GrazingLands16/{GrazingLands.cs,Settings.cs,PlantPropertiesPatch.cs}`, project and assembly metadata inspected |
| Native input | Root `Assembly-CSharp.dll`, 1.7.4998 rev1299, SHA256 `FE9B615CF5918955AB750F9D627FB0082FD23C556BDFDEE410F88857C67A081B` |
| Local evidence | `artifacts/Refinement/megamod-spec-20261105/grazing-lands/{upstream,binary,native}` |

The active project targets net472 and external Harmony2.2.2. Selected source and all three logical
binary classes were inspected read-only. Old version folders, old VanillaGrass.xml and bundled
historical Harmony are not selected 1.6 behavior. No license/affirmative reuse grant was found by
the repository license-file scan; no source, binaries or art are copied into the product. Public
source is an inspection input, not a permission assumption. Both English and Spanish keyed files
contain only the three settings labels.

## What the selected source actually does

`GrazingLands` installs one unconditional Harmony prefix on `Plant.IngestedCalculateAmounts` under
`net.avilmask.rimworld.mod.GrazingLands`. No plant eligibility, job, harvest, lifespan, health-regrowth,
Def or cache patch exists in this selected assembly. It is not animal-only: `ingester` is unused.
Ordinary caller diet/eligibility therefore matters. No new content, bottom tab or in-play gizmo.

The complete amount algorithm in [PlantPropertiesPatch.cs](https://github.com/catgirlfighter/RimWorld_GrazingLands/blob/9886905a7c6297a78300d42068ab6e6169079dbc/source/GrazingLands16/PlantPropertiesPatch.cs):

1. If a harvested Def exists but is not nutrition-giving, defer to native amounts.
2. Yielding means positive harvestYield plus a harvested Def. Wild/non-crop means not sowable OR
   no harvested Def, additionally edible and foodType **exactly** Plant (not flag membership).
   The damage branch is HarvestDestroys AND not wild/non-crop.
3. Yielding nutrition comes from harvested-item CachedNutrition. Available units are the ceiling
   of nominal yield times Lerp(.5,1,H/Hmax), but H/Hmax is **integer division**. Any damage therefore
   drops that estimate to half before ceiling. Current growth is not part of this yield estimate.
4. Non-yielding plants use 100 synthetic units and plant Nutrition/100 times the nutrition slider.
   Available units come from growth on the growth branch or the same health interpolation otherwise.
5. Requested units are rounded upward. On the damage branch, credit min(available,requested) units;
   damage is maximum health times consumed fraction of nominal yield times YieldDamage. A lethal
   amount returns numTaken=1; a survivor receives native Bite damage and numTaken=0. Growth is unchanged.
6. On the growth branch, non-yielding food credits consumed units directly. Yielding plants take
   min(available,requested*YieldDamage), credit take/YieldDamage units and lose take/nominal-yield growth.
7. Growth below .08 can destroy **non-yielding** plants only, using ConsumeChance. A survivor is set
   to .08; spawned survivors refresh the mesh. Integer Random.Range(1,100) has 99 outcomes, making
   a nominal 5% roll actually 5/99. 100% is special-cased. Yielding plants do not perform this roll.

Derived defects/limits, not live repro claims: rounded units can exceed requested food; growth-floor
restoration can increase an already-low plant; yielding growth availability is not bounded to actual
growth above .08; zero/invalid values and no-food effects are not guarded; health-based availability
has a discontinuity. Damage rounding and the outer native ingestion effects need separate analysis.
No source-specific remaining-food reservoir is saved.

## Settings and first-run distinction

| Source control | Effective range | Serialized default | Raw initial value / first fresh object |
| --- | --- | --- | --- |
| Grass nutrition | 100–500% | `multiplier=0` →100% | 0 →100% |
| Grass destruction | 0–100% | `consumeChance2=.05` →5% | 0 →0% |
| Crop damage | 100–500% | `yieldDamage=.25` →200% | 0 →100% |

All are global static fields behind normalized sliders; no inherited/save scope. The source public
static `GrazingLands.Settings` field is not assigned by its constructor; `GetSettings<Settings>()`
creates the separately held mod settings object. `Save()` calls that object's Write(). The selected
source does not migrate an old consumeChance key.

On the inspected beta, `Mod.GetSettings` calls `LoadedModManager.ReadModSettings`, which calls
ExposeData only while reading an existing file and returns `new T()` when no file exists. Thus a
fresh source settings object on this inspected path has 100%/0%/100%, unlike the advertised and
serialized-default 100%/5%/200%. This is a read-only source/native inference, not execution of the
foreign mod on either 1.6 or 1.7. Owned defaults explicitly use the documented 100%/5%/200% from
first initialization. The change log confirms past formula/default corrections, not proof they
initialize every fresh object correctly.

## Native beta and surrounding contracts

Fresh read-only decompilation of `Plant`, `PlantProperties`, `Mod` and `LoadedModManager` is retained.
Native `Plant.IngestedCalculateAmounts` already caps food to growth*plantNutrition and retains growth
on smaller bites; it destroys when all available nutrition is taken. Native eligibility checks
plant harvest minimum, a .1 growth floor, leaflessness and burial for non-trees; trees have an early
pass after base eligibility. S043 deliberately replaces these with its explicit common policy.

Native `YieldNow()` uses CanYieldNow, growth-dependent .5–1 yield, **floating** health-dependent .5–1
yield, optional difficulty cropYieldFactor, then randomized rounding. It cannot be called as a pure
grazing quotation. Nominal edible yield is a separate declared source-derived input. Actual
harvesting remains native and can produce a partial yield after grazing; no conservation promise
equating all food from a plant to one nominal harvest is made.

S043's inspected early-effects issue remains: native ingestion can record thoughts/history before
amount calculation. A late zero return alone cannot implement rejected-bite semantics. Existing
plant state and successful rooted-plant effects must be preserved once; edible harvest nutrition
does not mean ingesting a spawned harvested item or its drug/food effects. S085 excludes rooted
plants. S077 longevity is a separate lifecycle input, not a second grazing prefix. **Correction from
S088 research:** Growth Speed Makes Sense Now adjusts children's growth points; it does not affect
plant growth. The earlier forward reference based on its name is withdrawn.

## Proposed combination and deliberate deviations

See the [observable contract](../../../../openspec/changes/specify-megamod-grazing-lands/specs/megamod-grazing-plant-damage/spec.md)
and [design](../../../../openspec/changes/specify-megamod-grazing-lands/design.md) for complete rules.
S043 is revised directly; no mod-algorithm selector and no precedence between two owned patches.

| Concern | Proposed combined choice / status |
| --- | --- |
| Eligibility, rooted jobs, ordinary effects | S043; retain diet/access, 40% minimum, 8% effective remainder eligibility, buried/leafless policy and early no-bite rules |
| Growth plants | Nominal edible yield nutrition when present, otherwise plant nutrition; common scalar, S043 regrow extra, crop cost on edible-yield plants |
| Crop-health plants | Sowable + normally harvest-destroyed + edible yield; nutrition proportional to current growth and health, with consumption cost |
| Health rounding | Round positive crop cost up to native whole health; no separate fractional-food/health ledger; tiny bites can waste more crop |
| Pasture depletion | One exact 5% default roll for no-positive-yield growth plants at retained floor; no extra food or replay |
| Species destruction | S043 only on growth route, before pasture roll; crop-health route has no species bonus |
| Nutrition controls | One shared 100% scalar, retaining S043 10–1000% range; add 5% loss (0–100%) and 200% crop cost (100–500%) |
| Scope/UI | Common global/save/inheritance settings, fresh defaults, seven languages; no bottom tab |
| Non-food harvest/tree grazing | Keep S043 supported tree diet/growth rather than revert native whole-tree behavior solely because wood is non-food |
| Native crop harvest | No definition change, extra tax, auto-harvest or harvest-output effects on the grazer |
| Sources together | Original source active owns overlap unless a separately verified adapter exists; owned composed policy is inactive coherently |

The crop growth scaling, whole-health ceiling, combined yield/regrow/cost arithmetic and one broader
nutrition scalar are **proposed design/balance decisions**, not verbatim upstream promises or user
confirmed constants. GL-Q1 retains their review/playtest evidence. No assumption is hidden only in code.

## Static balance anchors, not finalized/live values

Read Core `Plants_Bases.xml`, `Plants_Cultivated_Farm.xml`, `Plants_Wild_General.xml`,
`Plants_Wild_Temperate.xml` and `Items_Resource_RawPlant.xml`. PlantFoodRawBase and RoughPlantBase
supply .05 nutrition per applicable output. Compare one mature, full-health rooted plant at default
policy, before regrowth and without other providers:

| Plant | Source XML nutrition / harvest / health | S043 before composition | Proposed combined grazing |
| --- | --- | --- | --- |
| Grass | .50 plant; no yield;85 health | .46 then8% growth | .46, then8% growth or5% loss |
| Rice | .175 plant;6×.05=.30 harvest;85 health | .161 then8% growth | .15 at full health, with health consumption |
| Haygrass | .30 plant;18×.05=.90 harvest;85 health | .276 then8% growth | .45 at full health, with health consumption |
| Berry bush | .50 plant;10×.05=.50 harvest;120 health;regrows | 1.61 then8% growth | .805 then8% growth |

This changes pasture/crop incentives: harvesting mature rice/hay remains more food-efficient than
fully grazing them under these defaults, while partial grazing followed by harvest uses native
remaining health. A berry bush still offers repeated growth-based food with no harvest labor.
Whole-health rounding can lower total grazing food across many tiny bites. Regrowth, travel,
work, hunger, weather, food policies, difficulty and actual finalized Defs must be measured before
balance acceptance. No exact pen carrying-capacity or performance claim follows from this table.

## External contracts and inspection limits

Read-only bounded installed text search across `.cs`, `.xml`, `.json`, `.md` and `.csproj` for the
package, namespace/public types and DLL name found the source About and RimPy database identity;
no executable consumer was found in that text scope. Compiled-only/reflection consumers elsewhere
remain unproven. The source exposes public settings getters, Mod class/Save and native plant state;
we preserve native state and inspect actual consumers before adding a facade. No package spoof,
source settings import or inherited Harmony owner identity is promised.

## Open questions and acceptance

| ID | Remaining work |
| --- | --- |
| GL-Q1 | Validate the explicitly proposed combined balance, whole-health rounding, yielding-plant bonuses and grazing-then-harvest economy; change the observable contract first if evidence calls for another rule |
| GL-Q2 | Prove effective native/provider amount, early-effects, damage veto/scaling, single-destruction and later-repatch contracts with per-patch target documents; unsupported shapes fail the affected group closed |
| GL-Q3 | Complete real-consumer/provider and lifecycle/shared-index composition proof; retain no-source-import/no-safe-swap limits without a separate verified migration |
| GL-Q4 | Implement only through vertical TDD, common localized settings, scoped review/package checks and fresh minimized native player workflows with personally inspected causal evidence |

Source inspection and document validation alone close none of those runtime obligations. No mod
execution, game launch, package install, normal save/config edit or Workshop mutation was performed.
