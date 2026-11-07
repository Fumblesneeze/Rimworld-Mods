## Context

Owner **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
S087 analyzes `avilmask.grazinglands`, Workshop1770268130, selected 1.6 DLL and matching upstream
source. [Research](../../../mods/megamod/docs/refinement/2026-11-05-grazing-lands-research.md) records
the exact inputs and native 1.7.4998 rev1299 inspection. This is documentation only.

## Goals / Non-Goals

**Goals:** one S043/S087 grazing policy, source-derived pasture loss and crop damage, yield-aware
nutrition, finite hunger-capped bites, explicit balance corrections and shared settings.

**Non-goals:** source-identical defective arithmetic, source mode selectors, an internal Harmony
stack, new crops/food, alternate harvesting, automatic config import, per-plant nutrition reservoirs,
independent map indexes, accurate pen-capacity forecasts or accepted compatibility claims.

## Decisions

### Compose once rather than select a mod algorithm

S043 continues to own shared eligibility, early no-bite guards, rooted ingestion preparation and
the independent lifespan group. This change owns the combined amounts/damage policy called directly
by that same grazing group. Revise S043's unarchived draft, not a second overlapping published delta.
The source algorithms cannot both run: both replace `Plant.IngestedCalculateAmounts`.

Classify current supported plant properties once at commit:

- Edible-yield plant: finite positive `harvestYield`, non-null nutrition-giving harvested Def and
  finite positive unit nutrition. Yield is nominal finalized Def output, not a call to `YieldNow()`.
- Crop-health route: edible-yield plant, `Sowable`, `HarvestDestroys`. Zone membership is irrelevant.
- Growth route: every other supported edible rooted plant, including regrowing food plants,
  non-sowable plants and edible trees whose harvested material is non-food.
- An actually absent/non-food yield uses plant nutrition. Malformed numeric yield/nutrition, missing
  required content or an unsupported provider is not converted into a guessed valid yield branch.
  Reject/disable the affected supported scope before selection/effects with documented diagnostics.

Do not inherit the source's exact `FoodTypeFlags.Plant` equality as a way to misclassify supported
combined food flags. Native diet permission remains authoritative; a nutrition-giving harvest
does not confer permission to eat the rooted plant. No new race, crop or harvested Def is created.

### One amounts contract

All values are read from the live target and committed settings, never cached pawn eligibility.
Let G be growth, R the effective S043 remainder, W requested nutrition, N plant base nutrition,
Y nominal edible yield count, n harvested unit nutrition, S the shared scalar, U regrow extra,
D crop-consumption cost, H current integer health and Hmax maximum health. Fractions use 1=100%.
Validate positive finite quantities, actual current target and S043 eligibility before any effects.

**Growth route:**

1. B = Y*n*S/D for edible-yield plants, otherwise N*S.
2. Multiply B by 1+U if normal harvesting leaves the plant alive.
3. Available A = B*(G-R), with G strictly greater than R and at least the effective minimum.
4. Apply S043 selected-species destruction only on this route: if normal harvest destroys the plant,
   the species is selected and W>A, consume it once and return min(W,B*(G+destructiveExtra)).
5. Otherwise return q=min(W,A), reduce growth by q/B and refresh native visible state. Do not round
   food to whole harvest items or synthetic percent-growth units. No positive credit without progress.
6. If a successful non-species-destructive bite exhausts A and the plant has no positive harvest
   yield, perform one pasture-loss roll. Exact displayed probability, including 0 and 1 endpoints;
   don't use Unity's integer `Range(1,100)` (upper exclusive). Loss adds no food/bonus. A survivor
   remains at R. Comparing exhaustion must tolerate representation without turning an above-floor
   bite into an extra roll. Loading, UI reads and zero bites never roll.

**Crop-health route:**

1. B = Y*n*S*G/D is nutrition represented by full health at the current growth. No regrow bonus,
   retained-floor subtraction, species destruction bonus or pasture roll on this route.
2. Available A = B*H/Hmax. Return q=min(W,A).
3. Consume min(H,ceil(q/B*Hmax)) whole health points. Use stable boundary arithmetic so an exactly
   integral expected cost does not become an extra point through floating error. Any representable
   positive bite costs at least one health point; upward rounding wastes food capacity, never creates it.
4. If health is exhausted, arrange one native consumption/destruction result, not damage-destruction
   followed by a second `numTaken=1` destruction. Surviving crops retain G and age and show new health.

This deliberately uses whole native health points, not a new fractional-health save ledger. Split
tiny bites can be more destructive; the spec exposes that cost. It is a reviewable balance choice
in GL-Q1, not an incidental epsilon or a promise of split-invariant feeding. No repeated free-food
loop from native stochastic damage rounding. Exact native damage notification/destruction ordering,
provider damage vetoes/scaling and early effects must be proved in GL-Q2 before implementing a hook.
Unsupported damage semantics cannot silently credit a planned bite which paid no actual cost.

The source estimates available yield from an integer H/Hmax followed by Lerp(.5,1), does not scale
crop nutrition to growth, and can return more than requested because of ceiling. We intentionally
replace those with proportional available health, current growth and a hunger cap. On growth
plants we preserve S043 bonuses while substituting edible-yield nutrition and D. These are proposed
combined rules, not exact parity with either original. See the comparator table in research.

### Preserve native lifecycle and effects

The original plant remains the only persistent food state: growth, integer health and age. No
spawned harvest, alternate stack or source FoodTracker state. S085 excludes rooted plants.
Ordinary harvesting later reads the remaining native health/growth; do not subtract earlier bites
again. Native harvest's half-yield health floor remains; grazing is not a promise that subsequent
harvest nutrition plus earlier grazing equals a fixed resource budget. Growth/healing supplied by
ordinary game or a supported external provider can restore future availability; this feature does
not heal crops or reset age. S077 lifespan must compose directly. S088 adjusts child growth points,
so the earlier plant-growth forward reference is withdrawn; it has no plant lifecycle role.

Reuse S043's early guard: `Thing.Ingested` and `Toils_Ingest` can emit effects before amounts are
calculated, so returning zero from a late prefix alone is insufficient. Validate before effects and
recheck current state at commit; ordinary successful plant effects occur once. Reading nominal
harvest nutrition does not invoke harvested-product effects, create a product or call randomized
`YieldNow()` during previews. Clarify unfamiliar direct callers before supporting their scope.
Source supply/taint calculations must not turn a fractional plant bite into a consumed item stack.

### Common settings and data

Extend `plants.grazing.*` with `pastureLossChance` (.05, 0..1) and `cropConsumptionCost` (2, 1..5).
Keep `nutritionMultiplier` (1, .1..10) as the one shared scalar; do not add the source's separate
grass slider. Existing regrow/destruction and eligibility options keep S043 defaults/ranges.
Initialize defaults explicitly, independent of whether a settings file exists. Common settings
handle global/live inheritance, dormant override, save isolation, scoped resets, validation and
main-menu callback restrictions. Order access, growth, nutrition/crop cost, pasture loss and species.
Explain consequences and rounding in all seven languages; no new tab, gizmo or status-button toggle.

Current native listers and the common map index provide candidates/facts for food search and later
Ctrl+F. Recheck growth, health, policy and eligibility at use time. Existing growth/health/destruction
notifications must invalidate any actual shared derived facts. Add no per-source scan/index/cache.

### Admission and dependencies

This extends S043 group A atomically, not an independently fallible second amount prefix. Publish
stable target documents before implementation for every actual hook, including any extra health
notification seam. Minimal predicates must cover W's nutrition meaning, current growth/health,
returned nutrition's single credit, count-driven destruction, native effects/early guard ordering,
and unchanged ordinary harvest. An existence-only predicate cannot prove these data flows.

Inspect effective/incoming semantics, supported skip-original interactions and later repatches;
never use exact assembly hashes or absence of other owners as compatibility checks. Every transpiler
validates its own incoming stream every run and returns it unchanged on failed admission. Do not
speculatively replay foreign transpilers. Guard exceptions/install failures fail closed; roll back
only newly installed owned group patches. One bounded startup warning per skipped enabled patch
names package/patch/spec, target, failed predicate, lost behavior and fallback; settings show status.

Original `avilmask.grazinglands` suppresses owned grazing overlap, as does `kittahkhan.grazeup`,
whenever the original provider is loaded. Do not disable/unpatch foreign owners. Unrelated
lifespan retains its own admission policy (original Graze up also overlaps that separate group).
Other providers require narrow package-ID/Def-resolved, absent-safe adapters only where consumed.
The installed text consumer search found metadata, not a proven public API dependency; this is
not a whole-ecosystem binary scan. Do not spoof package IDs or publish speculative source facades.

Core and Harmony are required when implemented. This is dynamic per-bite behavior, not static Def
mutation; no XML operation is proposed. Any later declarative change uses XML Extensions with the
first-use dependency, content/node/package checks and additive/idempotent operations. No foreign
assembly or Gateway dependency is shipped.

## Risks / Trade-offs

- Combined yield/bonus/cost balance differs from both sources → explicit formulas/examples and
  GL-Q1; compare finalized Core grass, rice, hay and berry plus selected providers before acceptance.
- Upward health rounding penalizes tiny bites → document the cost, test tiny/large species and
  repeated meals, revise the spec first if playtesting warrants a different observable rule.
- Damaged crops remain harvestable with native minimum yield → intentional ordinary harvest rule,
  measure grazing-then-harvest economics; no invented conservation guarantee or silent second tax.
- Settings/candidate changes between selection and consumption → one current-state commit and early
  no-bite effects guard; unsupported paths remain outside admission, never an endless retry loop.
- Steam mid-save-removal language exceeds evidence → no owned migration or compatibility claim.

## Migration Plan

No upstream settings import, save rewrite or plant conversion. Existing plants retain native state
and use current settings on the next eligible bite. Removing/withholding the owned group preserves
that state; source swapping and exact save round trips remain unverified. Any future migration needs
an explicit backup/atomicity/restoration contract rather than interpreting source private keys.

## Open Questions

GL-Q1 retains combined balance (including rounding and grazing-then-harvest); GL-Q2 exact effective
native/provider commit and guard semantics; GL-Q3 real consumers, lifecycle/shared-index composition
and migration limits; GL-Q4 implementation, languages, focused tests/review and final native workflows.
These questions are recorded for later, not sent back as requests to explain source behavior.
