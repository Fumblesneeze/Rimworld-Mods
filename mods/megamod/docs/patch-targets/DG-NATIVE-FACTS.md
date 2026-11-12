# DG-NATIVE-FACTS: current native facts for C1 suggestions

Owner: `fumblesneeze.megamod`, S139. **Definition projection, detached F0 inputs and partial F1 contribution, pawn and
zero-range initialization body proofs implemented; complete effective admission and native finalized-fact/player
acceptance pending.** No mutation patch, runtime registration or player-facing invocation is added.
Index before wiring the runtime consumer.

## Ownership and traceability

- Owning [design](../../../../openspec/changes/specify-megamod-drug-policy-generation/design.md),
  requirements for established suggestions, disclosed population/exposure assumptions and current
  complete preview; task 1.6 remains open.
- Source: `Source/Features/DrugPolicyGeneration/Runtime/NativeDrugGenerationFacts.cs`.
- Focused fixture: `tests/Megamod.Defs/DrugPolicyGeneration/NativeDrugGenerationFactsTests.cs`.
- Kind: read-only definition adapter plus a generation-local partial C01 admission consumer, no patch
  of owned megamod code. XML cannot capture a transient current catalog or validate edited runtime
  outcomes. There is no call to the native safe-dose helper or source generator API.
- Exact read-only inspection: 1.7.5005 rev1082, Assembly-CSharp SHA256
  `82387F78262461E53BB06A67C3945912B876D1FDB07235122BBB6E59D073F125`, ILSpy11.2.0.10050.
  Hash/version identify research only. [Calculation research](../refinement/2026-11-11-drug-policy-generation-calculation-research.md)
  retains XML identities and original C1 derivation. Follow-up source inspection covers the members below.

## Target and minimal predicates

The implemented adapter consumes a caller-owned current S029 `IReadOnlyList<DrugCatalogEntry>` and
explicit `IReadOnlyCollection<ThingDef>` unresolved-partial identities, preserving order/reference
identity. It never scans DefDatabase or asks which mods are active. Invalid catalog handles/duplicates
are rejected; unsupported row shapes remain visible with their existing catalog/default identity.
Output copies numbers and booleans into read-only records; mutable native Def references are opaque
identities. No pawn, window or Game is retained. No native ingestion, random getter or effect runs.

Definition predicates, each exercised by constructed Defs tests:

1. Item + current drug category/joy/addictiveness flags consistent with the captured catalog; exact
   ThingWithComps; one exact CompProperties_Drug with compClass CompDrug. Auxiliary exact native
   Forbiddable/Rottable shapes only. Quantity defaults/max both1. Explicit partial identities remain
   UnresolvedPartialDose. Unknown subclasses/comps do not inherit support from a familiar name.
2. Nonnull chemical/addiction/tolerance references and exactly one exact GiveHediff for the actual
   tolerance; chance1, positive finite explicit severity, null/same toleranceChemical. Bind private
   instance GiveHediff.divideByBodySize as bool; unavailable field shape cannot default to false.
   The gene-factor path uses C1's disclosed neutral baseline, not a per-pawn safety inference.
3. Exact HediffWithComps tolerance, minSeverity0, positive finite maximum and finite initial/lethal
   metadata, no nonnegative lethal threshold, chemical need, abilities or remove-with-tags effect.
   Nonempty onlyLifeThreateningTo is unsupported for tolerance and side highs: GiveHediff creates
   a whole-body instance, but native PostMake dereferences Part.def for that metadata.
   One exact SeverityPerDay/compClass with finite negative rate, zero range/reversal/minAge,
   mechanitor1 and every stage severityGainFactor1. Stages begin at zero with finite increasing
   minima. Optional single exact DrugEffectFactor uses the same chemical. Other comps are unknown.
4. Def/stage secondary givers are inactive only through the exact native RandomDrugEffect early
   return, or an unreachable later stage. Native returns when severity <= first curve X, or
   minSeverity without a curve. Require that bound >= the addiction threshold; C1 separately
   requires normalized gain STRICTLY BELOW the threshold. Curves require nonempty strictly
   increasing nonnegative finite X and positive finite nonincreasing Y. No-curve baseMtb is finite
   positive. Unknown/reachable/malformed effects fail; no random probability is treated as zero.
5. Distinct exact High/Alcohol side outcomes have explicit positive finite dose, no body division,
   same/null chemical modifier and plain deterministic negative decay. Active mental-state or
   unknown secondary effects fail. Alcohol's CurStageIndex>=3 hangover path is admitted only below
   that maximum modeled index. Exact Effecter comps require an unreachable stage interval; the
   adapter never instantiates or certifies the referenced visual effect. Active branches fail.
6. Side-high binary32 recurrence clears within297 events:200*297+415=59815 fits the earliest modeled
   one-day elapsed gap of C1's minimum two-calendar-day suggestion. No rounding epsilon or rate
   ratio shortens this bound. This is the same disclosed cadence model; actual accumulation can
   violate its range because native camera batching may skip events.
7. Exact OffsetNeed accepts only exact Need_Rest, finite offset, chance1, perIngested=false and
   same/null chemical modifier. The native perIngested branch multiplies by remaining stack count.
   Other need/outcome callbacks are unsupported. Rottable can cause food poisoning; fresh-dose and
   remaining-harm disclosure are required, not inferred from its definition type.

## F1 contribution-only admission milestone

`DrugGenerationDecayGuard` binds the actual nongeneric declarations of these four methods, retaining module,
metadata token and exact declaring type rather than relying on reflected lookup identity:

- instance `HediffComp_SeverityModifierBase.CompPostTickInterval(ref float,int) : void`;
- inherited instance `HediffComp.get_Pawn() : Pawn`;
- static `Gen.IsHashIntervalTick(Thing,int,int) : bool`;
- virtual instance `HediffComp_SeverityModifierBase.SeverityChangePerDay() : float`.

The inspected native body calls the pawn getter on the same comp, passes that pawn, integer200 and
the incoming delta to the interval check, and skips the complete contribution on false. On true it
calls the bound virtual daily-rate slot exactly once, stores that rate into a Single local,
multiplies by binary32 `0.0033333334f`, stores the product into a Single local, and only then reads
the original ref accumulator and adds the rounded product exactly once to that value
before writing that same reference. Moving the read across the virtual call is not equivalent:
the callee may change a field aliased by the supplied reference. The guard recognizes this bounded
conditional data flow, including harmless local aliases/nops. It refuses changed calls, constants,
receiver/ref provenance, arithmetic/order/rounding, duplicate contributions, unknown calls, other
branches and exception regions; it is not a general IL evaluator or pristine-body hash check.

`DrugGenerationDecayAdmission` delegates to the feature-local finite `DrugGenerationObservedBodies`
bundle for final observation of the actual incoming root and consumed callback bodies. The extracted
bundle owns no opcode semantics and does not change shared C01 or constructor proof. Its declared
root set is fixed; runtime declaration handles canonicalize inherited reflected lookup aliases,
and duplicate declarations are rejected before acquisition. Multiple roots share one callback
observation. Later-root failure rolls back only the attempted bundle through the existing candidate
lifecycle, preserving a separately owned bundle. Overlapping installed claims fail ownership;
eventual native integration of a union of claims remains root-owned.
`Available` is inspection-only; `Revalidate` explicitly acquires new
callback observers and removes obsolete ones using a coherent dependency snapshot. Actual outer
and selected inner callbacks can qualify as static-void scalar observers through the existing
scalar callback proof; constructor grammar is unchanged. By-ref/native-reference writes or escapes,
skip/replacement, factories and unproved bindings/effects are refused. Callback body publications
are part of currentness, not signatures alone. A changed root/callback publication is rechecked;
foreign transpilers are never rerun as inspection probes. Limits remain256instructions/body,
32observed methods and eight callback levels. Failed revalidation rolls back newly acquired
observers; rollback attempts all owned members and retains failed removals for retry.

This receipt proves only the caller's contribution flow. It **does not prove** the pawn getter's
body, interval/hash/calendar behavior, actual virtual rate body/initialization, other comp
contributions, outer `HediffWithComps` accumulation or the final severity setter/clamp. Those
dependencies and the complete F0–F5 fact gate remain required. The helper is unregistered and no
preview, creation or ordinary-Core support claim is enabled by these host cases.

The next bounded pawn projection is implemented by `DrugGenerationPawnBodyGuard`: the actual
incoming `HediffComp.get_Pawn` must return `this.parent.pawn` through the bound nonstatic reference
fields, without a substituted receiver/result, native-reference write or escape. Harmless reference
locals/nops are supported. The same finite observation bundle provides callback/currentness and
cleanup semantics. Six focused host cases cover the current reference result, actual replacement
and escape, inherited alias/local compatibility, duplicate declaration refusal, later-root rollback
and a shared scalar observer retained until its final root use disappears. The full affected set
including the thirteen contribution cases passes19/19. This helper is unregistered: its result is
the current field projection, not proof of parent/pawn initialization, permanent field stability,
the hash helper or actual game binding.

## F1 conditional zero-range initialization body proof

`DrugGenerationSeverityInitializationGuard` supplies seven finite root validators to the existing
`DrugGenerationObservedBodies`; it adds no observer graph, registration or general IL evaluator.
The binding names the actual nongeneric declarations of `Rand.Range(float,float)`,
`Rand.Chance(float)`, `FloatRange.get_RandomInRange`,
`HediffCompProperties_SeverityPerDay.CalculateSeverityPerDay`, private
`HediffComp_SeverityPerDay.get_Props`, `HediffComp.CompPostPostAdd(DamageInfo?)` and the concrete
SeverityPerDay override. Dependencies are observed before their callers. Duplicate method/field
roles, changed callable shapes and unknown roots refuse the claim.

The claim requires a valid same nonnull comp/properties pair, a finite negative properties rate,
both range endpoints and reversal equal to zero, and already naturally initialized runtime types.
F0/F2 must establish these premises; the body guard does not read them or manufacture a comp.
Either sign of zero is accepted. The two helpers' actual initial `bgt.un` comparisons fall through
to the original minimum/false return for those inputs, without reaching random sampling. The
range getter reads min then max from the same managed value-type receiver without writing or
escaping it. The calculator reads the original properties rate before the range-address call,
adds the helper result, stores to a Single local, reads reversal and calls Chance, then returns
that same stored local on false. It preserves the native store and read timing rather than
reassociating a real-number formula. Unreachable random/reversal tails are not evaluated; the
existing bundle still conservatively checks all selected inner callbacks in the captured body.

The properties getter reads the same comp's `props` field and casts to the bound concrete type.
The actual base hook is an empty return. The concrete hook forwards its original this/nullable
input once to that direct base call, obtains this comp's properties, performs one `callvirt` to
the actual nonvirtual calculator and writes one Single result into this comp's `severityPerDay`.
Labeled nops and proved scalar diagnostic callbacks remain eligible. Changed receivers, fields,
address writes/escapes, call counts, precision stores, branches or callback effects refuse the
affected current claim; inspection never invokes a foreign callback or replays its transpiler.

The focused host cases execute signed-zero returns, actual calculator and instance-field writes,
and changed helper/calculator/hook bodies before asserting refusal. Observer acquisition/repatch
and harmless labeled nops use the unchanged finite-bundle lifecycle. Native runtime call edges and
repatch/inlining, Rand's real type initialization, F0 current input identities and F2 actual
creation/parent/props/PostAdd dispatch remain unproved. Save/load and later field writers are not
covered by this initialization claim. Concrete daily-rate, hash/calendar/batching, other
accumulators and clamp/removal obligations remain open. No preview or complete native facts are
authorized; eventual runtime union-bundle integration remains root-owned and unregistered.

## Effective native semantics still required before exposure

The adapter's type/field checks do **not** prove effective method bodies. The future C01 consumer
must admit only the consumed semantics, including compatible foreign edits, and invalidate its
receipt after relevant supported repatching. No blanket foreign-owner or whole-body-hash ban.
The following inspected members establish the remaining data-flow obligations. The F1 contribution
subclaim above does not authorize their behavior:

- `Thing.Ingested(Pawn,float) : float` and protected virtual instance
  `IngestedCalculateAmounts(Pawn,float,out int,out float) : void`: one whole item under admitted1/1
  limits; native pre-ingestion callback before outcome loop, then post callback. ThingWithComps
  forwards protected PrePostIngested/PostIngested(Pawn) to its concrete comps.
- `CompDrug.PrePostIngested(Pawn)`/`PostIngested(Pawn)`: existing tolerance/addiction threshold read
  precedes outcomes; dose/history notifications and overdose behavior preserve the C1 assumptions.
- `IngestionOutcomeDoer.DoIngestionOutcome(Pawn,Thing,int)` and protected GiveHediff/OffsetNeed
  `DoIngestionOutcomeSpecial(Pawn,Thing,int)`: chance, explicit-severity, quantity and modifier flow.
- `AddictionUtility.ModifyChemicalEffectForToleranceAndBodySize(Pawn,ChemicalDef,ref float,bool,bool)`:
  existing hediff/gene factors and body division, with the independently admitted non-amplifying
  DrugEffectFactor curve and neutral/no-prior-effect baseline.
- Hediff severity setter, merge/add/post-make lifecycle, HediffWithComps comp tick/chemical-effect
  forwarding, SeverityPerDay initialization/rate and SeverityModifierBase 200-tick recurrence.
  Consumed native callbacks must retain the inspected field semantics; changed referenced content
  needs current finalized validation rather than a type-name shortcut.
- `HediffGiver_RandomDrugEffect.OnIntervalPassed(Pawn,Hediff)`, `HediffDef.StageAtSeverity(float)`,
  `Hediff_Alcohol.TickInterval(int)` and `HediffComp_Effecter.CurrentStateEffecter()`: exact
  comparisons/branch entry used by the modeled unreachable-range proof.
- Native scheduled day-index/editor and game-clock/batching semantics remain the original C1 gate.

Actual finalized Human-adult baseline, relevant gene/body/health/quantity inputs and the intended
ordered mod list remain task1.6. Before Create the owner must validate every consumed fact/shape
input and currentness receipt, not just equality of these projected numeric fields. This adapter
does not publish a digest, manage generation or authorize preview/creation. Unknown callback bodies
must be refused by that future gate even when this static shape projector returns OrdinaryWholeDose.

## Failure, composition and diagnostics

F0 consumed-input continuation: `DrugGenerationFactInputs` is a preview-local detached comparison
over the same `DrugCatalogRead`, explicit current partial-dose membership, freshly read Human-adult
inputs and a supplied current effect-factor curve. It snapshots the declared known field/reference
shape with exact identity and binary32 values; equal projected numbers are insufficient. A failed
comparison permanently disposes this receipt. It adds no Def database scan or mutation observer
and cannot detect an unobserved change-and-restore cycle. Root must separately bind the finalized
native Human fields and actual static curve, current Game and applied drug availability; neither
an incident-only receipt nor a supplied detached object proves those premises. Unknown reachable
shapes still require explicit refusal by the fact classifier/effective-body gate.

The synchronous read has fixed-depth typed traversal, at most4096 entries per collection and65536
collected entries in total, counting repeated references. Exceeding either limit refuses the whole
capture/currentness check with an explicit reason; it never truncates an available-drug list.
These work bounds protect the game-thread read and are not dose, risk or drug-count balance caps.
Constructed-Defs tests prove snapshot/currentness behavior only; the runtime binding and complete
F0–F5 footprint audit remain prerequisites before any preview is exposed.

The F0 checkpoint has four retained RED/GREEN batches and60/60 constructed-Defs cases with an
independent no-finding review. The initialization checkpoint has62/62 affected Harmony cases
(43new plus19 contribution/pawn regressions); its review's five missing same-shaped call-operand
replacement cases were added and independently closed. Root verified exact source hashes and
nonzero TRX counts. Local ledgers are `s139-native-facts/fact-inputs/implementation.md` and
`s139-native-facts/initialization-checkpoint.md` under the ignored implementation evidence root.
These are unregistered implementation checkpoints, not native binding or gameplay acceptance.

The next F0 origin slice selects the unique actual `LifeStageDefOf.HumanlikeAdult` member of the
current `ThingDefOf.Human.race.lifeStageAges` list, retaining all ordered entries and current
race/stage operands. The first Adult developmental flag includes Core teenagers and is not the
full-grown baseline. Missing/ambiguous membership or malformed factors refuses this origin claim;
additional stages alone do not. Native field origin and Single multiplication do not prove actual
pawn stage reachability, cached/locked selection or BodySize execution. Effect-curve first-use
initialization and physical points/getter binding are separately pending, as are all controller
and applied-save content authorities. This slice must remain unregistered until those gates pass.

Unknown, UnsupportedEffects, UnsupportedTolerance, UnsupportedDecay and UnresolvedPartialDose are
explicit row classifications; raw nonfinite risk values survive for C1's numeric rejection. No row
is silently removed or assigned another drug's facts. No arbitrary drug-name table is used.

Caller-effective admission failures must leave Generate unavailable with a bounded reason and
ordinary controls usable. Example: `[fumblesneeze.megamod] Target DG-NATIVE-FACTS unavailable;
predicate=whole-dose-outcome-flow; reason=unproved-effective-body; unavailable=Generate;
fallback=native-policy-controls`. No per-tick/pawn log and no foreign unpatching. The implemented
read-only adapter itself performs no logging/registration. The separate unregistered F1 helper uses
the ordinary PatchCandidate rollback lifecycle and retains a target-level failure reason.

## Verification matrix

| Case | Expected result | Tier / retained state |
| --- | --- | --- |
| Four source-derived constructed comparator shapes | Positive3/4/4/3-day C1 outputs; medical permission retained | Defs passed; not finalized Core |
| Unknown quantity/classes/outcomes/tolerance/decay | Row retained, explicit unavailable support | Defs passed |
| Secondary-effect threshold equality and valid curve | Inactive within modeled range only | Defs passed |
| Reachable effects, malformed/nonmonotone curves and active Alcohol/effecter branches | No automatic suggestion | Defs passed |
|297/298 side-high events; gain equal addiction threshold | Exact model boundary, no shortening | Defs passed |
| Catalog order/channel identity, partial path and detached values | No rescan, immutable projection, actual reference channels | Defs passed |
| F1 contribution fixture; changed multiplier/ref consumer/duplicate contribution | Exact float contribution or unavailable receipt; native-shaped effects still execute | Focused host Harmony passed |
| F1 scalar outer/inner observers, callback repatch and cleanup | Explicit acquisition, body-current receipt, no inspection execution/probe or retained obsolete observers | Focused host Harmony passed |
| Current effective native bodies and benign/changed foreign patches | Semantic admission/invalidation before exposure | Pending C01/Harmony and native inspection |
| Actual finalized four Core profiles and inherited/modified content | Current facts match supported shape or explicit unsupported reason | Pending isolated exact-mod process |
| Preview/reasons/Create and later native dose behavior | Honest visible disclosure and ordinary composed policy behavior | Pending reviewed-build native player workflow |

Seven actual RED/GREEN slices and final84/84 focused Defs checks are retained under
`artifacts/Implementation/megamod-p0/s139-native-facts/implementation.md`. Tests register constructed
Thing/Chemical/Hediff/Need definitions in empty owned databases and clear them in teardown; no
Harmony, active mod, DefOf or loaded Core fixture is involved. Live acceptance still requires exact
build/package/mod-list identity, player actions, causal screenshots, complete relevant logs and cleanup.
