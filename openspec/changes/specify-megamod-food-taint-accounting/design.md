## Context

Owner: **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`; **1.7 beta**.
S041 replaces Food Poisoning Stack Fix's finite-taint behavior. Read the [proposal](proposal.md),
[observable spec](specs/megamod-food-taint-accounting/spec.md) and
[source/native research](../../../mods/megamod/docs/refinement/2026-11-03-food-poisoning-stack-fix-research.md).
The inspected four-type source has three patches, reuses poisonPct as a count and leaves creation,
ingestion and serialization consumers otherwise native. Independent review confirmed its arithmetic
and lifecycle hazards; that is not a live defect reproduction.

## Goals / Non-Goals

**Goals:** conserve finite taint through actual physical operations; preserve native batch/illness
rules, probability interfaces, save boundaries, provider ownership and hidden-state privacy.

**Non-Goals:** new recipes/illnesses/user-tunable multipliers, a food-selection cheat, a second inventory, source
DLL/package impersonation, guessing ambiguous historical state, or changing another repository mod.
No source-safe-add/remove claim or universal compatibility claim follows from this draft.

## Decisions

### Store owned counts separately from native probability

Keep a small owned record with physical-count correspondence, tainted counts grouped by actual
FoodPoisonCause, schema/provenance and assignment state. Clean quantity is the physical total minus
the tainted total. Enforce nonnegative quantities and sum(tainted) <= actual total. No per-unit
object identity is required merely to sample identical units; cause-group counts suffice unless a
proven optional provider supplies distinct serving identities that must remain correlated.

Native CompFoodPoisonable.poisonPct/PoisonPercent remain probability contracts. For a quiescent
owned stack, publish its tainted fraction, never an integer count. Retain the native scalar Cause
as a representative cause for ordinary readers: greatest tainted quantity, stable enum ordering for
ties, Unknown when none is known. This projection is not the consumed portion's cause. During
ingestion, the admitted path must use the actual consumed exposure/cause instead of interpreting
that aggregate as another random population. Never expose these hidden values in ordinary owned UI.

Prefer per-existing-component owned state with guarded serialization over mutating food Defs or
adding a competing CompFoodPoisonable. A weak side table plus namespaced data through the component's
serialization seam is a candidate, subject to exact save/load/teardown proof. It must cover spawned,
inventory, caravan, world/container and temporarily unspawned food, not only map-listed Things.
Record a save-level format/epoch sufficient to detect suspended tracking and stale records. No
static cross-game references, worker-thread Verse access or persisted live pointers.

No static Def change is needed by this design. If implementation instead requires an owned Comp,
its static addition belongs in guarded merge-preserving XML with XML Extensions and metadata in
that same slice, including inherited/modded food shapes. Do not mutate Def.comps at runtime to avoid
those requirements. Resolve this storage choice before coding rather than shipping two state owners.

### Sample a population without replacement

Use one allocation operation for splits, partial absorption, direct consumption and unlabelled
partial loss. Sample tainted/clean cause groups from the remaining population, decrementing each
selected quantity exactly once. Tests need conservation, individual bounds and unbiased allocation,
including all-clean/all-tainted populations, rather than only a few seeded examples. Full transfer
and zero transfer are deterministic and consume no allocation randomness.

Do not copy the source's double subtraction in PostSplitOff or recipient overwrite in partial
PreAbsorbStack. Do not infer transfer success from TryAbsorbStack's bool: false can mean a successful
partial transfer whose donor remains. Snapshot before mutation, prepare the allocation, reconcile
the actual physical count/identity changes and commit each owned transfer once. Keep unused/random
draw state uncommitted on a true no-op. Preserve foreign component changes; rollback is only for
owned records, never fabricated restoration of native food or another provider's state.

ThingWithComps computes count and calls PreAbsorbStack before base Thing rechecks stacking and
transfers. Therefore a prefix alone cannot prove the actual quantity. An admitted completion/finalizer
path must reconcile partial mutation and exceptions. Whole-stack SplitOff returns the same object
and must not create a second record. Destroyed donors may still need their prepared transaction
until the physical transfer is reconciled; then release references. Unrecognized external raw count
mutation must invalidate the affected ownership rather than silently clamp records and declare success.

Use native/shared transfer notifications and direct calls for owned S021/S027/future S123 paths,
not patches to our own hauling code. Do not add a separate food/Thing lookup cache or contaminate
search indexes with latent taint. Counts belong to physical food; reservation/receipt ownership
remains with its existing feature.

### Preserve native creation and identify the consumed portion

Native GenRecipe sets output stackCount before Notify_RecipeProduced. The native successful
SetPoisoned sets probability 1 for that whole product. New admitted production therefore gives all
produced units that cause, preserving the original room-then-cook chance sequence. Do not change
this into one tainted meal per multi-unit batch or reroll contamination per produced unit. An admitted
explicit SetPoisoned on existing food similarly updates the actual current population.

Thing.Ingested can SplitOff(numTaken), discard the returned piece from its local flow, and then
invoke PostIngested on the original remainder. It can also destroy the whole consumed Thing before
that callback. Capture a scoped ingestion transaction tied to the exact Thing, eater and actual
numTaken; associate any internal split with that transaction once. Carry the selected exposure/cause
through callbacks without reading the remainder as the eaten portion or double-retiring its taint.
Nested/provider ingestion requires a properly scoped stack of contexts, guaranteed finalizer cleanup
and exact identity checks, not one mutable global current-meal flag.

For one ingestion event, zero selected tainted units gives base cooking-taint exposure 0; at least
one gives base exposure 1. Preserve the native GetFoodPoisonChanceFactor and one ordinary illness
check, regardless of how many tainted units that event consumes. This deliberately preserves the
native one-check-per-ingestion boundary rather than multiplying the chance by a count. Independent
DangerousFoodType/trait/provider risks and AddFoodPoisoningHediff behavior remain intact. Among
consumed cooking-tainted causes, sample one proportional to the actual consumed cause counts; no
uneaten cause can be selected. Unknown stays Unknown.

This does not preserve the old averaged-stack illness distribution. For known-native p=0.1 and ten
units consumed together after independent initial assignment, factor 1 gives
P(any tainted)=1-(0.9^10), about 65.1%, versus native's one 10% probability check. For one unit at
factor 2, the finite model gives 10% exposure followed by certainty when exposed, hence 10% illness,
versus native's 20% check. These are deliberate consequences of finite actual-unit exposure, not
new hidden tuning constants. A modifier cannot make a selected clean unit cooking-tainted. Keep
these cases in the semantic review and tests; do not summarize this design as unchanged odds.

Prefer an exact operand/call boundary that supplies consumed exposure without overwriting persistent
probability. If a supported adapter needs a temporary field value, restore the current post-operation
projection in guaranteed cleanup, not a stale pre-split snapshot. Admission must account for foreign
prefix/finalizer ordering and which owner supplies the final chance/cause. A compatible prefix
cannot be ignored merely because the owned context exists.

### Normalize known native state once, without inventing source provenance

Known native probability p over N units is materialized as N independent Bernoulli(p) assignments,
then stored as an ordinary finite population. This preserves the prior per-unit probability and
does not claim to reconstruct a historical count. Use stable, replayable assignment entropy tied
to the input save identity and food identity so the same pre-assignment save cannot reroll on load;
persist the completed assignment and its schema. Do not derive randomness from UI inspection, a
filename change or the order of incidental enumeration. Exact p=0/1 cases are deterministic.

Format classification is required first. Fresh native production and valid owned records have
known origins. For imported saves, inspect actual save/provider metadata and explicit input provenance;
neither a value between 0 and 1 nor absence of a currently active provider proves historical native
format. Original count saves can also contain unconverted native fractions or invalid counts from
source defects. A value of 1 is inherently ambiguous. FP-Q2 retains the exact supported provenance
recognition; do not advertise all old saves as convertible before it is established. Unsupported
inputs retain their format/state and clear explanation, with no automatic source conversion.

Normalizing state occurs in a staged in-memory transaction. Validate food identities/counts/causes,
probability projections and schema together before publication. Do not rewrite the input file while
loading. A later normal save that replaces an existing file must retain a complete backup and use
verified atomic publication/failure restoration; inspect the real game save seam rather than assuming
its backup satisfies the contract. An explicit source-format converter is separate future work and
would need its own preview, backup, unit-bound/cause policy and verified postconditions.

### Failure and recovery do not manufacture continuity

Creation, allocation, actual ingestion, serialization and probability projection are an atomic
behavior group per supported route. If any essential boundary cannot be admitted, withhold that
route before new owned conversion/mutation; use the valid native probability projection or the
unmodified provider's behavior and explain the absence of finite-count enforcement.

Keep minimal format/recovery handling separate from new scheduling where its own semantics can be
admitted. Mark suspended ownership before untracked operations occur. Retained old records are
historical, not authority over changed food. On restoration, reconcile current trustworthy native
state through the same explicit normalization contract; never reuse stale counts. If provenance,
projection or serialization cannot be established, leave the affected route unsupported instead
of silently repairing or accepting that save as a correct finite population. Required recovery
checks are implementation prerequisites, not runtime promises proved by this design.

Original `Mlie.FoodPoisoningStackFix` active means the conflicting owned route is withheld with a
startup warning. Its deliberate count reinterpretation is concrete overlap, not an absence-of-other-
patches rule. Do not migrate/unpatch the original or claim that installing it on an owned save is
safe. Historical mixed-provider saves require explicit reconciliation. Never ship the source DLL,
spoof its package or introduce an unconsumed facade for its four patch classes.

### Optional provider ownership is narrow and evidence-based

A transfer across formats requires an explicit ownership decision. Known-native unassigned operands
are normalized first and then handled entirely by the owned allocator. If either operand is ambiguous
or provider-controlled without an admitted joint route, delegate the physical operation to its
native/provider handling. Stage retirement of **both** operands' owned records, with the known
operand's valid pre-transfer probability already published and the unknown operand untouched. This
atomic boundary avoids presenting one side of an unowned population transformation as accepted
finite accounting. It applies to full/partial transfers in either direction, including a known donor
remainder; it is an explicit degradation, not an optimization or claim of preserved exact counts.

Commit suspension/provenance markers for every surviving/result stack only when actual food moves;
if it does not, discard the staged retirement and preserve the valid owned record. Reconcile exceptions
against actual movement as elsewhere. Propagate unsupported provenance across subsequent native
splits/merges/save/load, even if the resulting native value lies in [0,1]. No automatic re-adoption
from a plausible fraction. The tracking/provenance seam must itself be admitted before this mixed
route is supported; if it cannot safely mark the result, the route remains an explicitly unsupported
integration rather than a proven handoff. Do not invent counts, delete food or alter stackability
to conceal that failure. FP-Q3 retains the exact handoff and callback ordering proof.

The repository's Immersive Chefs UnifiedFoodPoisoningPatch reads the private probability field,
combines it with serving/sanitation/temperature risk, writes a temporary final probability, and
restores its snapshot in a finalizer. Its CulinaryState component transfers serving records from
the end of its list; its PostIngested consumes one record. DiningIngestionOutcomePatch also snapshots
a serving before Thing.Ingested. These facts mean a shared field value alone does not prove that
both features agree on the actual eaten serving, especially for direct multi-unit ingestion.

An optional adapter must resolve `fumblesneeze.immersivechefs` by package ID, validate the actual
types/fields/ordering and correlate the same physical portion and serving. Feed only the selected
base cooking exposure into the provider's one risk calculation; preserve its custom contributions,
attribution, cleanup and serving state. Do not mutate the other repository mod or add a runtime
project/assembly dependency. If an adapter cannot establish this complete contract, leave covered
food under provider control and expose that unsupported route; ordinary supported food remains
eligible. No blanket compatibility claim from the inspected repository source.

Other modded food with ordinary native semantics needs no title allowlist. Provider-specific raw
field writes, custom ingestion, serving identity or saved representations need exact adapters. Scan
actual consumers before adding a facade or migration. Raw type-name scans found no source API
consumer, but do not establish safety for changing the native probability contract.

### Privacy and developer UI

The source has no settings and no bottom tab. Do not create feature-specific toggles/chance sliders
or a global/save preference for per-food state. Use a read-only top-debug-toolbar Food taint action
for selected food. It reports owned tainted quantities/causes or an honest unavailable/untracked
format; it never labels zero tracked cooking taint as universally safe. No latent inspection text,
search predicate or food-priority sorting in ordinary play. Keep native known food-type risk and
actual illness messages. Developer mode off closes the view and rejects stale callbacks.

Use the common native debug entry point and seven translated catalogs, not another inspection
patch merely to append English strings. No new artwork is selected in this design; any later raster
icon requires the asset skill. Debug inspection reads state without allocating assignments or using RNG.

### Semantic admission and target documents

Before implementing any patch, add its stable-ID target document and executable validator with
owning scenario, implementation, tests and eventual live evidence. Candidate seams are:

| Boundary | Required minimal semantics |
| --- | --- |
| Creation/explicit poisoning | Actual output quantity established before whole-stack contamination; native cause and chance sequence preserved |
| Merge | Relevant caller/other stack, count calculation, pre- versus post-transfer order, actual deltas and bool meaning |
| Split/loss | Same-object whole moves, partial source/piece quantities, callback timing and no replay of an ingestion split |
| Ingestion | Actual numTaken and consumed-portion identity, partial remainder callback/full destruction, one native illness calculation and relevant foreign risk ownership |
| Persistence/projection | Probability float semantics, namespaced owned state, map/world holders, load stages and supported recovery epoch |
| Optional adapter | Canonical package plus complete callable/field/Def shape, exact serving/risk/cleanup ownership |
| Debug action | Existence/callable shape only where sufficient, selected-object/mode lifetime and read-only behavior |

A signature or field name cannot prove these state transformations. Validate actual required data
flow/cardinality and original/effective code, including skip-original prefixes. Compatible foreign
owners and unrelated edits are allowed; hashes identify research inputs rather than admission.
Every transpiler validates its actual incoming stream each run and returns it unchanged on failure.
Do not repeatedly execute foreign transpilers as speculative probes. Support later repatch
invalidation/revalidation. False/unknown/throwing predicates or install failure leave the atomic
route inactive and roll back only newly installed owned hooks. Emit one bounded startup warning per
skipped enabled patch naming package/patch/spec ID, target, reason, unavailable behavior and fallback.

No XML patch is currently selected. If one becomes necessary, use XML Extensions package/content/
shape guards, preserve existing comps/list entries, avoid duplicates and declare its dependency.

## Risks / Trade-offs

- **Source counts and native probabilities are indistinguishable** → explicit format provenance;
  no automatic source migration or clipping an invalid count into a probability.
- **Partial failure leaves accounting ahead of physical food** → staged allocation, actual delta
  reconciliation, idempotence and finalizer cleanup without rolling back other owners.
- **Provider risk sees the wrong serving or stale field** → complete consumed-portion/ordering adapter
  or affected-route fallback; no compatibility claim from one getter match.
- **Large stacks make allocation expensive** → aggregate cause counts, finite sampling and measured
  large-stack/combined-hauling cost; no unmeasured per-unit object table or parallel Verse access.
- **Debugging accidentally initializes or reveals food** → read-only diagnostic and strict developer
  entry/mode lifetime; no ordinary taint search or risk-based sorting.

## Verification and Open Questions

FP-Q1–4 now map to drafted behavior but retain exact runtime seams, input provenance/recovery,
optional serving contracts, native workflows and performance evidence. Pure population/transaction/
format decisions fit Unit; owned patches with owner cleanup fit Harmony; constructed lightweight
Defs fit Defs. Loaded food Defs, real cooking, path/holder transfers and active providers require
fresh isolated RimWorld, not fabricated host mod state.

After vertical RED/GREEN and independent review, personally observe native cooking, hauling to a
nearly full store, full/partial pickup/placement, eating actual food, save/load and focused optional
provider cases on the reviewed build. Arrange known-taint preconditions only for the transfer/eating
tests; actual cooking must still exercise the real production path. Use the real top-debug action
to observe quantities alongside physical stacks and illness outcomes. Read-only field assertions
or a synthetic poisoned end state are not acceptance. Retain build/mod identity, causal actions,
before/after screenshots, logs, configuration hashes and cleanup. Semantic balance review must
check native cooking opportunities, batch exposure, modifiers, quantities and route reachability.
