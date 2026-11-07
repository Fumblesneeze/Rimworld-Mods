## Context

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
S043 owns the selected Graze up behavior. The [research](../../../mods/megamod/docs/refinement/2026-11-03-graze-up-research.md)
pins the complete included source/1.7 DLL and inspected 1.7.4995 rev309 native contracts. No runtime
code, tests, migration or native acceptance accompanies this documentation change.

## Goals / Non-Goals

**Goals:** partial plant consumption with retained growth, controlled destruction, buried/leafless
grazing, rooted tool-user ingestion, safe current-state calculations, plant longevity and common
configuration. Preserve source scope and document numerical/gameplay corrections.

**Non-goals:** new plants, harvested-item nutrition changes, harvesting replacement, caravan abstract
forage changes, accurate pen stocking forecasts, independent plant indexes, upstream package/API
impersonation or automatic source config migration. S087's
[combined consumption design](../specify-megamod-grazing-lands/design.md) now owns the shared
amounts/damage calculation; S077 retains its separate plant-lifecycle contract and verification.
S088 concerns child growth points, not plant growth; the earlier naming-based reference is withdrawn.

## Decisions

### Keep one plant model and two independent feature groups

Use existing `Plant.Growth` and native age/save state. No per-plant comp, duplicate nutrition reservoir
or extra saved crop object is needed. Native plants, including supported modded subclasses, provide
current finalized properties. No hardcoded grass list. A subclass overriding consumed methods needs
supported effective semantics; inheritance alone is not proof of support.

Group A owns grazing eligibility, amount calculation and rooted ingestion preparation/commit checks.
Admit these together so widened eligibility never feeds an unsupported amounts path. Group B owns
finite lifespan interpretation and can remain active if A is unavailable. Declarative XML cannot
express eater-dependent bite arithmetic, temporal preparation or per-save computed lifespan; narrow
Harmony adapters are justified. Keep domain policy local to this feature, called directly by later
owned consumers; never patch our own feature classes. XML/Defs remain preferable for any later truly
declarative addition, with XML Extensions guards and required dependency then.

### Compute one finite bite from current state

Use the single formulas and classification in S087's combined design. Edible-yield growth plants
use nominal harvest nutrition and crop-consumption cost; other growth plants keep base plant
nutrition. Regrowing extra and selected-species destruction apply there as specified. Cultivated
edible-yield plants normally destroyed by harvest instead use finite current-health consumption,
with unchanged growth/age. Non-yielding growth plants can be lost at depletion with one configured
roll. No additive second S043 bite, extra harvested items or second destruction event.

Verify actual alive/spawned/reachable/access-allowed target and native eating eligibility at use
time, including supported provider rules. Check finite growth, nutrition, request and policy values.
Require growth at least the effective minimum and strictly above the effective remainder, including
listed destructive races and crop-health plants; equality remains unavailable under this shared
eligibility policy. Require representable positive progress and recheck current state before effects.
A zero/negative/nonfinite request or changed/invalid plant produces no bite, invented food,
destructive fallback or successful-eating effects. End/reselect through the normal job path.

Floating-point bounds are numerical hygiene, not new nutrition. Growth-route credit must agree with
actual growth reduction within a documented numerical tolerance; crop-route credit must pay the
specified health cost. Rounding cannot credit food while leaving both growth and health unchanged.
Whole consumption remains a separate explicit result.
Do not clamp invalid foreign state into apparently valid extra food or silently restore native
destructive ingestion after a handled invalid bite. Unsupported-provider fallback is selected before
the owned group handles it, not after partially mutating growth.

Native `Thing.Ingested` checks `IngestibleNow`, then records thoughts/history/drug notifications
before `IngestedCalculateAmounts`; its later steps notify hediffs/genes, run outcome doers, destroy
whole-consumed objects and return nutrition. `Toils_Ingest.FinalizeIngest` can also record dining
thoughts before calling it, then credits food need/records. Thus returning zero only from the amount
prefix does not implement a no-bite contract. Add/reuse a narrowly admitted early guard in the actual
job finalize path before effects, with direct-call guard where supported callers bypass that path.
Actual target IDs/shapes must be resolved during implementation rather than guessing an inner lambda
name. Keep normal successful ingestion effects once, including dangerous-food risks and provider
callbacks; non-destructive plants report zero whole items exactly as native partial plant ingestion.
S041 finite food accounting must not invent a whole plant unit from a fractional bite.

### Eligibility deliberately matches the chosen source rules

For supported rooted grazing: edible Def, positive finite branch nutrition, not burning, growth ≥ M
and > R. Source replaces plant-specific harvest minimum and leafless exclusion, and imposes its M on
trees too; retain those explicit policy choices. When buried access is enabled, ignore snow/sand
depth for grazing only. When off, max(snowDepth, sandDepth) must be ≤ the plant's hide depth.
Retain native eater diet, food-policy, reservation, forbidden/access and reachability checks. Edible
plant content does not grant carnivores a new diet. Leafless plants remain subject to normal growth,
damage, age and survival; eating them does not revive them or make winter growth possible.

The patch's source scope is all supported plant ingestion, not animals exclusively. A tool user
with a native/provider-approved plant diet uses rooted ingestion without pickup, extra-food
inventory collection or a dining destination. Native `MakeNewToils` currently creates chewing,
yields preparation, then chewing/finalization. Preserve that outer sequence, touch checks and
reservation release on cancellation/destruction/despawn. Do not replace the complete ingest job.
Non-plants, ordinary harvested-food handling and harvesting jobs remain untouched.

### Common settings with validated effective combinations

Use one shared settings definition/renderer/store, with independent global defaults and save overrides.
Proposed IDs under `plants.grazing.*`: `buriedAccess`, `minimumGrowth`, `remainingGrowth`,
`nutritionMultiplier`, `regrowExtra`, `destructiveExtra`, `destructiveSpecies`; S087 adds
`pastureLossChance` and `cropConsumptionCost` to that same namespace/section. Under
`plants.lifespan.*`: `multiplier`, `softFloorDays`. UI order: Animals and plants / Grazing (access,
growth, nutrition, destructive species), then Plant lifespan. No extra tab or operational toggle.

Defaults and ranges follow research: buried true; M .40 in [.05,.95]; R .08 with editor range
[.01,current M]; S 1 in [.1,10]; U 2.5 in [1,10]; destruction bonus .5 in [0,10]; empty species set;
lifespan scale 1 in [.1,10], floor 0 in [0,60] days. Show U as **extra** percentage with the resulting
total (default +250%, total 350%). Destruction bonus is a full-growth-equivalent addition, not a
percentage of hunger or remaining growth. Do not copy unverified source 'Vanilla =' tooltip claims.

Validate individual values in the common path on load/commit, not only while rendering. Reject bad
interactive input; absent values get defaults, invalid persisted scalar/range values get bounded
defaults/normalization with one diagnostic. Resolve coupled M/R **after inheritance**: effective
R = min(requested valid R, effective M). If a global M reduction caps a save's stored R override,
show requested and effective R clearly but do not overwrite that stored override or global R.
This also preserves dormant overrides and restores intended R when M permits it again. New explicit
R edits must fit the current M for the edited scope. Checking Use global retains a dormant override;
unchecking restores it or seeds from the current global value. Main-menu save callbacks are disabled.

Species selection is one set-valued policy setting, not per-pawn gameplay state. Offer searchable,
label-sorted current plant/tree-eating races, with identity disambiguation for equal labels. Deduplicate
IDs and retain unresolved selections as inactive/unavailable entries until explicitly removed or a
scope-specific reset. Rendering cannot clear unknown entries. Match native race identity, not label.
No eager external assembly reference. Reset global values affects inheriting saves when resolved;
reset current-save overrides restores inheritance only in that save; dormant values follow common
reset semantics stated in the observable spec. No reset at main menu may mutate a save.

Commit policy atomically for a bite. Later settings changes apply to the next food selection/bite
or lifespan query, including already-existing plants; selection does not reserve stale policy.
Changing settings never instantaneously changes growth, age, hit points or food need. No global Def
mutation or stale cached value tied to a previous save; no active game uses global policy only.

### Lifespan preserves native age and limited-lifespan behavior

Take the supported incoming native lifespan L, scale by P. When L > 1 day and L×P < floor F, the
effective value is (L×P + F)/2; otherwise L×P. The threshold refers to unscaled incoming L, not final
value. LimitedLifespan remains native: do not turn immortal plants mortal or alter growth/research/
harvest requirements. Invalid incoming lifespan/provider behavior fails this optional group closed;
do not inject NaN or enormous tick overflow. Guard supported finite conversion to native age ticks.
Shortening lifespan can make old plants begin ordinary age damage on the next native update; it
does not directly destroy plants. Increasing lifespan neither resets age nor heals past damage.

Global/save policy is resolved at query time through the common mechanism, with any cached computed
value invalidated by policy/save changes. The source pass-through postfix's first `value` parameter
is the incoming result convention, not an original method argument. Prefer a narrow result adapter
which validates the actual effective result and preserves compatible earlier contributions once.

### Target specifications and provider ownership

Before implementing each hook, publish a stable patch-target document with exact consumed callable
shape, minimum semantic predicates, tests, owner, fallbacks and later live evidence. Candidate seams:

| Seam / group | Minimum semantic issue to prove |
| --- | --- |
| Plant eligibility / A | Native edible/burning and consumed growth/visibility semantics; preserve permitted foreign restrictions, not blanket true after another owner rejects |
| Plant amount calculation / A | W is requested base nutrition, result count drives actual destruction, returned nutrition drives one native credit, and G can be changed safely |
| Tool-user preparation / A | Plant target and rooted preparation connect to the existing chew/finalize sequence; native reservation/touch/cancellation semantics remain |
| Early bite checks / A | Runs before dining/ingestion side effects; direct callers and changed targets cannot bypass current-state validation |
| Lifespan result / B | Incoming value is days, finite-lifespan distinction survives, scaling occurs once and native tick conversion is representable |

Original IL alone cannot establish effective semantics after other patches. Tolerate benign changes,
other owners and unrelated instructions; reject missing/ambiguous relevant data flow, skip-original
interactions or unsupported replacements. Avoid replaying foreign transpilers speculatively. Every
owned transpiler must validate its incoming stream each execution and return it unchanged on failure.
Startup predicates/installations are idempotent, exceptions fail closed, rollbacks remove only newly
installed owned group patches. Supported later repatches must invalidate/revalidate the affected group.
One bounded startup warning **per skipped enabled patch** identifies package/patch/spec ID, target,
failed predicate, lost behavior and fallback. No tick/job warning spam or exact hash/version gating.

Active original Graze up suppresses both owned groups before patching. Active original Grazing Lands
suppresses owned group A whenever the original provider is loaded, while unrelated group B can remain.
Never unpatch/disable the foreign source. This co-installation policy is separate from the owned
S043/S087 combined calculation. Native/provider behavior remains responsible for suppressed paths; settings
explain the unavailable owned policy. Other plant/food/lifespan providers need narrow absent-safe,
package/shape-guarded adapters when required. There is no global ban on patched methods.

Use native plant collections and shared map data if a real consumer needs derived facts. Invalidate
public growth/destruction facts for shared search; do not create another Graze up cache. No new pen
capacity forecast is claimed: native estimates do not model these bonuses, leafless access and
destructive policies. Existing displays must not be relabeled as exact megamod stocking advice.
Any future estimate must use the same admitted policy, not duplicate source arithmetic.

## Risks / Trade-offs

- Retained remainder and default regrow bonus change pasture capacity → document explicit formulas,
  compare Core grass/berry/haygrass and measure finalized Def/native feeding before balance acceptance.
- Widened eligibility could expose unsupported jobs → atomic group A, real rooted tool-user and
  cancellation workflows, effective-provider checks and native fallback on unsupported adapters.
- Invalid or zero food could repeat attempts/effects → positive-progress eligibility plus early
  commit guard; test ordinary selection and direct supported ingestion callers, not just math.
- S087 changes source-only arithmetic → use its one reviewed combined contract and comparator table;
  GU-Q3/GL-Q1 retain effective-provider and balance proof, not a second runtime algorithm.
- Lifespan interacts with later growth/tree features → compose once and preserve immortality and
  native age; no runtime patch stack against our own feature and no unexamined broad compatibility claim.

## Migration Plan

No upstream config import, plant conversion or on-disk save rewrite. Existing plants keep native
growth, age and hit points; missing owned settings inherit global defaults. Valid owned global/save
settings use the common persistence path. Missing source types/keys are not impersonated. Disabling
an owned group leaves current native plant state intact; original-provider state remains untouched.
Safe mid-save source swapping/removal is unproved until exact round-trip runs. A future migration
requires its own backup, atomic publication, failure restoration and verified postconditions.

## Open Questions

GU-Q1–4 retain effective seams/positive-progress and caller proof, common-policy implementation,
selected-provider composition, finalized balance and native acceptance. Draft choices above are
explicit source-derived rules or documented corrections; later source composition may refine them.
