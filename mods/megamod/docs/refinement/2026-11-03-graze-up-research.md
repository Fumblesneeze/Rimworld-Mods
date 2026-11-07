# S043 — [Kit] Graze up research

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
Inspected **2026-11-03**, origin replacement, handwritten **P1-34**. **Source/native research and
observable draft independently reviewed and validated.** Source findings below are
static evidence, not live defect reproductions. Adopted rules and corrections are in the owning
[change](../../../../openspec/changes/specify-megamod-grazing/proposal.md), separately from this evidence.

## Documentation first and selected build

[Workshop description](https://steamcommunity.com/sharedfiles/filedetails/?id=2302739121)
describes non-destructive grazing, growth-based nutrition, configurable buried-plant access,
lifespan/nutrition changes and destructive species. It distinguishes grazing from harvesting.
Search returned different cached revisions; the English URL failed to open directly. A
[newer language-page result](https://steamcommunity.com/sharedfiles/filedetails/?id=2302739121&l=koreana)
includes the author's 1.7 update and new minimum/remainder controls. Installed files, rather than
web dates or tooltip assertions about vanilla, establish the analyzed build.

Installed root `F:/Steam/steamapps/workshop/content/294100/2302739121`:

| Item | Evidence |
| --- | --- |
| Identity | `[Kit] Graze up`, author KittahKhan, package `kittahkhan.grazeup` |
| About | Supports 1.2–1.7; Harmony required |
| LoadFolders | 1.7 selects root plus `Current`; older version directories are not the selected code |
| Selected DLL | `Current/Assemblies/GrazeUp.dll`; assembly/file 1.0.0.0, informational 1.0.0, Debug |
| Selected source | `Current/Source` includes mod, settings/helper, setup, and three patch files (four patches) |
| Localization | Current English, French, Korean catalogs; minimum/remainder English text inspected |
| License | No LICENSE/COPYING discovered; no code, DLL or art reused |

| File | SHA-256 |
| --- | --- |
| Current DLL | `6A6B5918B0D533C680FE9326C497BB4D0500B264AA9F76BDEE50F662D1ECFF90` |
| About | `65D0E97A6EDAA6F20B89F04D43DF71AF15F11A59E7C06C97B2D949D6009C679A` |
| LoadFolders | `9BCFF5848FC893A58B7A72C3F2DB367F808C4A3DB489388349578E4111FDF0E8` |
| PatchPlant.cs | `C9EB59125EA20F3BD163BFF12C9518B0CA6812325AC6CA25DDC14B68AD4DB8A7` |
| PatchPlantProperties.cs | `9A8C4C3AADD6986B9E5605900B457547ADE64F8A5A64923DDA32F605DBD752E6` |
| PatchJobDriver_Ingest.cs | `6C68810D7F2905801CF88033714247CDF0091C58B1D85A334126A2830C4DF4EC` |
| GrazeUpSettings.cs | `6B8696A21DED656563A63895D0D102A0E695B3555D9AE9DBE868B4ACDEDE23B7` |
| English keyed XML | `E8782A176A3D5094B67A5CDE47734432B0B64710C4FC96360CAA626A99477FCC` |

ILSpy 11.2.0.10050 decompiled the selected DLL to ignored local evidence
`artifacts/Refinement/megamod-spec-20261103/graze-up/compiled`; native inspection is in sibling
`native`. The complete mod/settings/helper/setup/four patch bodies were read and match the included
source on the behavior described below. Current ships no Harmony DLL in its selected Assemblies
folder; some historical directories do, and must never be copied into our product.

## Settings, defaults and source UI

All source settings are global ModSettings, serialized under the field names below, read directly
by patches. Values change during UI rendering; there is no separate commit/apply method. No new
bottom tab, contextual gizmo, debug action, job or plant Def is supplied.

| Field | Default | Source UI range / meaning |
| --- | --- | --- |
| `grazeInSnow` | true | Checkbox; actually bypasses both snow and sand burial depth |
| `grazeMinimum` | 0.40 | 0.05–0.95, global growth threshold including trees |
| `grazeRemainder` | 0.08 | 0.01–minimum, remaining growth after exhausting available grazing |
| `lifespanScalar` | 1 | 0.1–10, plant lifespan multiplier |
| `lifespanFloor` | 0 | 0–60 days, soft floor, not a guaranteed minimum lifespan |
| `nutritionScalar` | 1 | 0.1–10, plant base Nutrition multiplier for grazing |
| `nutritionRegrowBonus` | 2.5 | 1–10, **additional** multiplier for plants not destroyed by harvesting; default total 3.5× |
| `nutritionDestroyBonus` | 0.5 | 0–10, additional growth-equivalent nutrition on destructive grazing |
| `destructiveGrazers` | empty | Def-name list of races which can eat plants/trees; not individual animal state |

Reset reflects over public fields and copies fresh defaults. The species list is populated once
from non-corpse race Defs whose diet includes Plant/Tree; it is not explicitly Animal-only. Rendering
the checkbox list rebuilds the saved selection from available options, dropping unknown names;
duplicate names in persisted selection can throw when added to its lookup. No sorting/search is
provided. Remainder is capped to minimum only when drawing settings, not during load. A null species
list is repaired to empty. Source text calls the regrow slider nutrition but displays 250% for an
effective 350% total; its vanilla comparison text is not evidence for the inspected game.

The replacement needs common global/save policy and scoped reset semantics, clear bonus versus total
labels, safe cross-option validation and retained unresolved species IDs. These are design issues
to specify, not implemented corrections. No setting-scope exception has been justified.

## Complete four-patch behavior

`GrazeUpSetup` creates Harmony owner `kittahkhan.grazeup` and unguarded `PatchAll`. No target-spec,
effective-code validation, atomic feature admission or per-skipped-patch warning exists in source.

### Plant amounts

The prefix wholly replaces `Plant.IngestedCalculateAmounts`. Define current growth G, desired
nutrition W, remainder R, plant Nutrition N and configured scalar S:

- Effective full-growth grazing nutrition B = N × S. If harvesting would **not** destroy this plant,
  multiply B by **1 + regrow bonus**. It uses plant Nutrition, not harvested item nutrition/yield.
- If B ≤ 0, the source reports one plant taken and **0.1 nutrition**, unconditionally; this is its
  fallback against repeated grazing, not measured food content, and it is not capped to W.
- Otherwise available non-destructive nutrition A = B × (G − R).
- Destruction occurs only when the plant is normally harvest-destroyed, **W > A**, and the eater's
  race is in the destructive list. Equality W = A leaves the plant alive. Regrowing harvest plants
  are not destroyed by this branch even for listed races.
- A destructive bite offers B × (G + destroy bonus), capped to W; reports one plant taken.
- Otherwise nutrition is min(A, W); subtract nutrition/B from growth and dirty the plant mesh.
  Reports zero plants taken, leaving the same plant instance. A small bite need not reach R.
- No explicit Animal-only test; tool users with a supported plant diet can enter this path too.
  No comp/save record is introduced. Native ingestion handles the reported count and nutrition.

Boundaries needing correction/specification: the source does not clamp negative/invalid available
nutrition, guard nonpositive W or enforce fresh eligibility at this method. With invalid persisted
R > G, arithmetic can produce negative nutrition and increase growth. With R = minimum and G at
that boundary, it reports a zero-nutrition non-destructive bite; candidate selection can still
accept the plant. Real job-loop consequences are **not reproduced**. Do not solve this by inventing
food or deleting a zero-nutrition plant; a finite no-progress/reselection contract is needed.

### Eligibility

The `Plant.IngestibleNow` prefix suppresses the native getter and returns true when the Def is
ingestible, growth meets the configured minimum, the plant is not burning, and burial permits it.
If `grazeInSnow` is false and the plant is spawned, the **maximum of snow and sand depth** must not
exceed `hideAtSnowOrSandDepth`. If true, both depth checks are bypassed. It ignores native leafless
state and plant-specific harvest minimum; it does not bypass the eater's separate diet, forbidden,
reachability, reservation or ordinary food selection rules itself. These are distinct semantics
to preserve or deliberately revise, not consequences of a snow-only checkbox name.

### Tool-user preparation

For a Plant target only, the source prefix replaces
`JobDriver_Ingest.PrepareToIngestToils_ToolUser` with reflected invocation of private
`PrepareToIngestToils_NonToolUser`. Non-plant targets run native code. No check on eater species.
Purpose: reserve/reach a rooted plant without picking it up, carrying it to a dining spot or taking
extra plants into inventory. Exact 1.7 target includes `Toil chewToil`; source uses only instance and
result, not that argument. Verify the outer chew/finalize sequence and cancellation before adopting.

### Lifespan

Pass-through postfix on `PlantProperties.LifespanDays` scales the received value by lifespan scalar.
When the **unscaled received value > 1 day** and the scaled result is below the configured floor,
add half the gap to the floor. This does not simply clamp to the floor. It affects all queried plant
lifespans, not just grazed plants. Native limited-lifespan policy and damage timing are not replaced.
The first `value` argument is the pass-through result convention, not a claimed getter parameter.

## Exact native comparison and provisional balance examples

Inspected `F:/Steam/steamapps/common/RimWorld/Assembly-CSharp.dll`, **1.7.4995 rev309**, SHA-256
`56399B6144D9F1695F9C53D054DC82972F7AB35E68D1A19A7129F2F5860CCD21`.

- Native Plant amounts already allow a **partial** bite: give min(W, G × N), destroy on depletion,
  otherwise subtract nutrition/N from growth. Source's retained remainder and regrowing multiplier
  remain differences; do not describe all partial grazing as newly introduced by the mod.
- Native Plant eligibility preserves base edible/not-burning checks, lets trees through before
  other plant checks, and otherwise checks plant harvest minimum, growth at least 0.1, LeaflessNow
  and burial depth. Source deliberately changes this whole set, not just growth 40% or remainder 8%.
- Native `LeaflessNow` is true for 60,000 ticks after its leafless event; it is not synonymous with
  snow burial. Source grazing does not itself cause plants to regrow under unsuitable conditions.
- `PlantProperties.HarvestDestroys` is `harvestAfterGrowth <= 0`; `LifespanDays` is growDays ×
  lifespanDaysPerGrowDays (default factor 8), while `LimitedLifespan` is independent. Native dying
  damage checks age against lifespan; source does not reset plant age when grazed.
- Native food selection still requires nutrition-giving ingestible content, eater willingness/diet,
  freshness and the relevant access/reservation/reachability checks. Source does not patch pen food
  estimates, general nutrition stat display, selection estimates or caravan abstract forage.
- Native tool-user preparation picks up/carries food, whereas non-tool preparation reserves and
  approaches it. Source's reroute is separate from the actual nutrition calculation.

Three Core XML comparators were read with their parent defaults: `Plant_Grass` in
`Plants_Wild_General.xml`, `Plant_Berry` in `Plants_Wild_Temperate.xml`, `Plant_Haygrass` in
`Plants_Cultivated_Farm.xml`, plus `Plants_Bases.xml`. These are **unmodified XML/native calculations,
not finalized Def measurements or live balance acceptance**.

| Comparator at full growth | Base Nutrition | Harvest regrows? | Default source B | Nutrition to 8% remainder, sufficiently hungry non-destructive eater |
| --- | --- | --- | --- | --- |
| Grass | 0.50 | No | 0.50 | 0.46 |
| Berry bush | 0.50 | Yes (harvestAfterGrowth 0.30) | 1.75 | 1.61 |
| Haygrass | 0.30 | No | 0.30 | 0.276 |

For the same fully grown grass, a desired 0.10 bite leaves growth 0.80; a listed destructive eater
wanting more than 0.46 can destroy it and receive up to 0.75, still capped to its requested amount.
At W = 0.46 it survives at 0.08. These source formulas change pasture economy; no claim of identical
native odds/yields or of sufficient grazing for any particular herd. Harvest work/items are untouched.
Life-span multipliers/floor alter all finite-lived plants, including crops and trees, so future
S077 Trees Don't Just Die must share one composed plant-lifespan result. **2026-11-05 correction:**
S088 Growth Speed Makes Sense Now adjusts children's growth points, not plants; the earlier
naming-based plant-growth reference is withdrawn.

## Consumers and selected-source interaction

Ignore-disabled scans of installed text XML/C#/JSON and DLLs for package/settings/type identifiers
found only source versions/obj output, intake metadata, RimPy catalogue/translation dependency data,
and RimSort fixture mod lists. No executable external caller established. Dynamic or uninstalled
bindings remain unproved. Source static settings/type IDs and GU_* translations are not automatically
promised API facades; native Plant/growth/ingestion contracts are the meaningful shared surface.

The user selected **Grazing Lands**, not Grazing Area. At this 2026-11-03 checkpoint S087 remained
queued. **2026-11-05 update:** the [complete S087 research](2026-11-05-grazing-lands-research.md)
now records its source/binary/native analysis and proposed combined consumption rules. The tables
above retain the original S043-only comparison as historical evidence; current observable behavior
is in the revised S043 draft and S087 contract. The following paragraph records the earlier limit.
A bounded documentation/metadata intersection check used its
[Workshop page](https://steamcommunity.com/sharedfiles/filedetails/?id=1770268130) and installed
`1770268130/About/About.xml` (`avilmask.GrazingLands`, supports through 1.6). Its public description
mentions a chance of grass destruction and damage to yielding plants; it is **not** merely another
name for this source's all-plant remainder/destructive-species policy. No binary intersection has
yet been examined and no precedence is selected. GU-Q3 records that concrete overlap for later
composition; do not stack both internal patches or mark S087 specified from this narrow check.

## Remaining specification work

| ID | Work |
| --- | --- |
| GU-Q1 | Prove drafted positive-nutrition/growth/destruction/eligibility and rooted tool-user contracts, including early rejection before native eating effects and effective subclass/provider semantics |
| GU-Q2 | Implement drafted common settings with coupled minimum/remainder validation, bonus labels, species identity retention, resets, persistence/save-switch timing and finite lifespan semantics |
| GU-Q3 | Resolve effective targets and atomic patch groups, actual native consumers/estimates and original-provider suppression; compose S087 grazing and S077 plant lifespan without duplicate effects; S088 is unrelated child growth |
| GU-Q4 | Independent source/spec review, meaningful tests, finalized comparators and native grazing/harvesting/snow/sand/save/provider workflows; no beta gameplay acceptance yet |

Additional native consumer/commit check: `JobDriver_Ingest.MakeNewToils` checks current IngestibleNow,
then yields preparation, chewing and `Toils_Ingest.FinalizeIngest`. Plant amount calculation is not
the first ingestion action: `Thing.Ingested` records thoughts/history/drug effects before it, and
FinalizeIngest can record dining thoughts even earlier. A zero amount result alone is insufficient
to reject a bite without successful-eating effects. The drafted design therefore requires early
current-state checks, plus actual count/nutrition consistency and native reservation cleanup.
`FoodUtility.NutritionForEater` reads a general stat and possible raw-food factor, not this source's
plant growth/bonus model; no changed food-ranking/pen-estimate parity is claimed.

The draft retains source leafless/snow-and-sand scope and strict destructive hunger threshold;
corrects zero/invalid bite handling and requires growth strictly above remainder even when a
destructive species is selected; makes scope/bonus/reset semantics explicit; and retains separate
finite-lifespan behavior. No upstream/source configuration import or automatic plant conversion.
GU-Q3 preserves the demonstrated selected-source overlap for later analysis; it is not a request
for the user to explain existing mods. No gameplay implementation or upstream edits.

Independent source/native review corrected one ambiguous regrowing-plant scenario: a small bite
leaves at least the remainder, not necessarily exactly the remainder. Focused re-review and the
separate observable-contract review found no remaining issues. Strict typed OpenSpec validation
passed **65 items, 0 failed**; two capabilities contain **14 requirements and 46 scenarios**, with
all **23 implementation tasks unchecked**. This validates the draft, not in-game behavior.
