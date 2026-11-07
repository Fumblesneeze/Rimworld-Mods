# S118 — Cooking-dependent slaughter recovery

Owner **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`; target **1.7 beta**.
Origin **original**, P2-46/P2-47. Research/design refinement on **2026-11-06**. No upstream mod is
invented for this feature. [S117 source research](2026-11-06-skilled-slaughtering-research.md)
establishes existing Animals/Melee timing; it does not supply Cooking or a yield implementation.

## Confirmed intent versus proposed decisions

The user's latest direction, retained in the [interview notes](2026-11-01-handwritten-notes.md):
Cooking contributes to existing slaughtering effectiveness; lower Cooking penalizes it and higher
Cooking improves it; Cooking10 is the neutral reference; skill affects output as well as duration.
Keep native base yield values and earn the change through the slaughter job, without raising every
carcass/hunting outcome. Numeric assumptions belong in the spec and preferably actual settings.
Earlier doubled-base/full-recovery-at16 suggestions are superseded.

| Decision | Status / rationale |
| --- | --- |
| Cooking10 neutral; no global base-yield increase | User direction |
| Cooking0/20 contribution75/125% | Proposed defaults anchored to current native butchery efficiency, which already has this neutral point |
| Animals/Melee normalization at the same neutral level | Proposed way to include existing slaughter proficiency while keeping all10 neutral; not a separate user-confirmed number |
| Recovery `1 + 50% × (relative proficiency - 1)`, bounds50–200% | Proposed moderation because the later butcher's efficiency still contributes; all tunable |
| Native meat/leather quantity paths only | Proposed meaningful bulk outputs; no invented extra discrete organs/horns/trophies; custom paths require actual evidence |
| Deliberate designated/auto slaughter earns a record, mental-state kills do not | Proposed intent boundary; S117 still applies timing to native Slaughter jobs, including mental episodes |
| Capture at handling start, commit on matching cut death, retain on corpse | Proposed causal/persistence contract; no death-time resource drop, retroactive inference or later reroll |
| Skills limited to0–20 for this added policy | Proposed bounded curve; no change to native skill data or S117's other stat factors |

These choices are specified as a concrete draft, not hidden assumptions or playtested conclusions.
SY-Q1 retains review of them for later; no interview interrupts the pass. No implementation follows
merely because the proposal is complete.

## Native documentation and read-only evidence

Start from the confirmed request and S117's documentation-first source analysis, then inspect
current player-facing native recipe/stat descriptions and their actual implementation. Native
ButcherCorpseFlesh describes obtaining meat, leather and body products, consumes one corpse, uses
Cooking work,450 work, ButcheryFleshSpeed and ButcheryFleshEfficiency, and disallows rotten/ordinary
nonmeat/mechanoid inputs under its native filters. This is not a generic harvest or animal-death hook.

Exact game input: **1.7.5000rev1275**, root Assembly-CSharp.dll15,636,480bytes,SHA256
`D55AA8E47FA3C5463CA85B0ECCD51F195B4BBCD04F56DA3AF21C2F6688A81254`;
Version.txt18bytes,SHA256 `BAD5BED449D17B2AF39925A3A25007164B2B1F89B29A4E24AFA3B505EDCC535A`.
S117's selected DLL remains SHA256 `C51BA483A3970248643A80766172AF91803206D7E9373F6DFF830CA0B3857294`.
These identities prove inspected inputs, not semantic compatibility.

| Inspected native surface | Relevant evidence |
| --- | --- |
| S117 JobDriver_Slaughter / ExecutionUtility extracts | Handling and actual native cut/death are distinct; other callers also execute cuts, so a wound or generic cut call cannot prove deliberate job origin |
| Pawn.MakeCorpse / Pawn.Kill | Actual corpse contains the same inner Pawn; some paths produce no retained corpse. Record the resulting exact body, not a future death of that pawn |
| GenRecipe.MakeRecipeProducts | Recipe worker efficiency, optional table efficiency, declared fixed products and separate special-product processing; Butchery delegates per ingredient |
| Corpse.ButcherProducts | Delegates to InnerPawn, then native blood/history; no owned preparation concept |
| Pawn.ButcherProducts | MeatAmount × efficiency → RoundRandom → race meat; base generic products; LeatherAmount × efficiency → RoundRandom → race leather; then discrete life-stage body-part products |
| Thing.ButcherProducts | Generic declared butcherProducts use efficiency and difficulty, independently; changing the whole efficiency argument would incorrectly affect them too |
| GenMath.RoundRandom | Whole part plus one with probability equal to the fractional part; preserve one native draw per existing call rather than round then multiply or consume randomness in preview |
| Toils_Recipe.FinishRecipeAndStartStoringProduct | Native work XP, materialize products, consume ingredients, notify iteration/records, then store/place; no second bonus/drop/iteration is needed |
| RecipeWorker.ConsumeIngredient | Native default destroys the consumed ingredient |
| RecipeWorkerCounter_ButcherAnimals | Target counts use actual MeatRaw resource counts; no prospective corpse multiplier is part of that accounting |
| Corpse.ExposeData / inspect / SpecialDisplayStats | Native corpse/inner-pawn serialization and contextual surfaces; add only guarded owned fields/text without replacing native contents |
| ResurrectionUtility.TryResurrect | Detaches inner Pawn and destroys its Corpse before restoration; corpse-scoped entitlement does not belong on the surviving pawn |
| ThingDefGenerator_Corpses | Generates implied corpse definitions from race content; avoid pretending all corpse content is static XML or mutating every race merely to hold a small runtime record |

Twelve successful new native type extracts are retained. One initial wrong namespace for the
butcher counter produced an empty unsuccessful file, explicitly excluded as evidence. S117's
already-retained job/stat/wait extracts supply the shared timing context; do not rerun or substitute
them for actual final gameplay acceptance.

## Existing yield semantics to preserve

Native ButcheryFleshEfficiency is base1 × (`0.75 + 0.025 × Cooking`) with Sight weight0.4 capped100%,
Manipulation weight0.9 and ordinary other modifiers; its parent max is150%. The proposed slaughter
factor is applied to resulting quantities, **not fed into this capped efficiency stat**, and does
not erase the butcher's separate effort. Native butcher speed uses `0.4 + 0.06 × Cooking` instead;
do not conflate its speed curve with efficiency.

MeatAmount base140 and animal LeatherAmount base40 remain unchanged. Native body-size, natural
remaining-part coverage, difficulty and relevant injury/malnutrition effects remain. Current
nonpermanent injuries other than ExecutionCut can cause the0.66 penalty. Both meat/leather have a
small-output postprocess curve `(0,0),(5,14),(40,40),(100000,100000)`; multiply the finalized native
quantity, not the unprocessed raw body-size product. Thus a healthy static adult rat's expected
pre-butcher base is approximately31.0857 meat/16.2286 leather, not28/8.

The meat/leather-specific adjustment does not affect generic extras or PawnKind life-stage discrete
body parts, whose native generation can actually add missing-part hediffs. **Never enumerate product
generation for an owned preview.** No global MeatAmount mutation, extra corpse nutrition, missing-
part restoration, raw corpse consumption bonus or change to existing bills' ingredient filters.

## Proposed formula and tuning

Let `N` be neutral skill level, `a(x)=0.64+0.09x`, `m(x)=0.95+0.0125x`. Cooking's contribution
interpolates between the configured low endpoint at0,1 atN and high endpoint at20. Relative
proficiency `P=a(A)×m(M)×C/(a(N)×m(N))`; recovery `Y=clamp(1+I×(P-1), minimum, maximum)`.
This uses source skill factors, not whole work speed/capacity. Body size still affects the native
quantity/timing; it is not multiplied a second time by the recovery policy.

| Shared setting | Proposed built-in | Valid range |
| --- | --- | --- |
| Neutral skill level |10|integer1–19|
| Cooking contribution at0 |75%|10–100%|
| Cooking contribution at20 |125%|100–200%|
| Yield influence |50%|0–100%|
| Minimum recovery |50%|10–100%|
| Maximum recovery |200%|100–300%|

The ranges keep all interpolation segments defined and bounds containing neutral1. Each row uses
shared Global default/Current save/Use global behavior, no exemption. Six effective values are
captured together per attempt. Dormant overrides, main-menu disabled callbacks, world-view binding,
save switching and current-global inheritance are explicit in the contract. Corpses and attempts
carry gameplay state; they are not per-entity settings.

## Static balance comparison, not live acceptance

Compare the same current Core rat/cow/thrumbo sizes as S117 (0.2/2.4/4), healthy adults, all native
other factors1 and default proposed tuning. The separate butcher has the same Cooking level in
this table only to show both stages' combined effect. Quantities are **expectations before native
probabilistic rounding**, derived from inspected XML/code; no finalized game measurement is claimed.

| All three slaughter skills / butcher Cooking | Slaughter recovery | Combined with butcher | Rat meat / leather | Cow meat / leather | Thrumbo meat / leather |
| --- | --- | --- | --- | --- | --- |
|0 /0|63.7723%|47.8292%|14.87 /7.76|160.71 /45.92|267.84 /76.53|
|10 /10|100%|100%|31.09 /16.23|336 /96|560 /160|
|20 /20|160.5406%|200.6758%|62.38 /32.57|674.27 /192.65|1123.78 /321.08|

This is a substantial labor/proficiency reward: expert preparation can raise later resource output,
and low skill can waste material. It is intentionally not described as neutral QoL. Default yield
influence50% moderates the raw skill product; actual combined progression, animal availability,
food/leather economy, injured carcasses and different butchers need final playability review.
No new research/cost/work recipe or source-base increase is introduced, and no infinite byproduct
cycle is created by corpse reuse: the ordinary real corpse is consumed by the existing recipe.

## Lifecycle, UI and compatibility decisions

Capture skills/tuning with S117 handling start; commit only to its actual matching deliberate-cut
corpse. Pending records survive valid job reload, not cancellation or another victim. Store the
factor/provenance on the corpse, so worker departure/death and new global settings cannot reroll it.
Ordinary hauling/containers/caravans/map transfer preserve that object; destruction/consumption or
resurrection cannot transfer preparation to another body. Old corpses remain neutral. A later lower
cap does not clamp a valid earlier factor; corrupt/unknown records never become a guessed bonus.

Use native pawn stat/description and corpse inspect/info surfaces. Settings go in the structured
mod-settings section; no bottom tab or debug override. All seven languages are required. S010
recipe inspection stays input-dependent; actual S014/S017 production, S021 hauling and S033 shared
Thing data see physical products, not prospective bonuses. No separate corpse index/cache, inventory
scan or product enumeration as a read operation.

S118 is an original capability with no upstream package/API to imitate. It directly composes with
owned S117. If S117 selects original Skilled Slaughtering, S118's Cooking/new-record/application
group is inactive and existing valid records can remain dormant; no hidden patch to the original.
Generic modded races using admitted native meat/leather behavior are ordinary inputs, not a species
allowlist. Nonstandard corpse stacking/replacement or alternate processors require package/shape/
identity evidence; no blanket compatibility claim. All actual patches require minimal target specs,
atomic failure, bounded diagnostics and later revalidation, not exact hashes or exclusive ownership.

## Evidence and open work

[Proposal](../../../../openspec/changes/specify-megamod-slaughter-yield/proposal.md),
[design](../../../../openspec/changes/specify-megamod-slaughter-yield/design.md),
[observable contract](../../../../openspec/changes/specify-megamod-slaughter-yield/specs/megamod-slaughter-yield/spec.md)
and [coverage](../replacements/slaughter-yield.md). SY-Q1–5 in the
[ledger](specification-progress.md) retain proposed choices, exact effective/persistence/provider
contracts, finalized economy/measurements and implementation/native acceptance.

Ignored `artifacts/Refinement/megamod-spec-20261106/slaughter-yield/` retains11 exact source/native
inputs, successful/failed native extract identities and reproducible proposed comparator values.
No source/game assembly was executed. No runtime code/test/build/install, normal config/save write,
Workshop edit, asset selection or game launch occurred. Future acceptance requires actual native
slaughter → earned corpse → butcher bill → physical quantities, not direct setup of a bonus result.
