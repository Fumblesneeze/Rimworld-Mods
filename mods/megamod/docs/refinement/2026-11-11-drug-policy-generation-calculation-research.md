# S139 — native calculation research, C1

Date: 2026-11-11. Sole owner: Make Rimworld Great Again, `fumblesneeze.megamod`.
Revision20 scope: resolve DG-Q1–3 into an implementable, deliberately limited suggestion model.
This is read-only native/source research and mathematical evaluation, not implementation, tests,
finalized-Def inspection or live acceptance. No game, build, install or commit was performed.
The [2026-11-08 source research](2026-11-08-safe-drug-policy-generator-research.md) remains the
upstream provenance. Its 1.7.5000 native identity is historical; the evidence below is 1.7.5005.

## Exact evidence

Installed root: `F:/Steam/steamapps/common/RimWorld`; `Version.txt`: **1.7.5005 rev1082**.
`Assembly-CSharp.dll` SHA256: `82387F78262461E53BB06A67C3945912B876D1FDB07235122BBB6E59D073F125`.
Inspection used retained `artifacts/Tools/ilspycmd/ilspycmd.cmd` 11.2.0.10050 with exact `-t`
requests, stdout only. `operation_list` was inspected first: `gateway_decompile` needs a live lease,
which is outside this lane. No replacement orchestration or copied upstream implementation was made.

Read-only XML identities, relative to the installed root:

| Input | SHA256 |
| --- | --- |
| `Data/Core/Defs/Drugs/DrugBases.xml` | `76916DB512008E5A8D82D28E918BBC89C317F62D8AC63BDC2E8E796CEDC2837B` |
| `Data/Core/Defs/Drugs/Alcohol_Beer.xml` | `23D8A68BB90075DE77C019FB964DBE122798049B9BFB7F5DAAD4EEADC9A1B211` |
| `Data/Core/Defs/Drugs/Psychite_Tea.xml` | `34A7BF3AFFCA00E20405F1DA862F6414865838F6B2F8ECD2B19435DEE469A769` |
| `Data/Core/Defs/Drugs/Smokeleaf.xml` | `0B72183B56B59320B08F624BD4F1CAB2151E6B26B5553C3D7CB6354AF3E194BB` |
| `Data/Core/Defs/Drugs/Ambrosia.xml` | `3DABDBEE40CC5DB3F977BDA58587B11888DD2A6FF4352500436AD9FC6B1B4F6D` |
| `Data/Core/Defs/Drugs/Psychite_Yayo.xml` | `123308BD67B4E25832A964EFA67C05106D63AD6C461D7C264131E5F1BA85A5A8` |
| `Data/Core/Defs/Drugs/Psychite_Flake.xml` | `70AA39D9E8832575E31777DE0F13AD4423E8DD6606F3AC6E412B50E75E0142AB` |
| `Data/Core/Defs/Drugs/WakeUp.xml` | `8F10EEBD7BD5A5C5CEE95573AA2E80A3B0C610A50515727E402FB2EA29C85866` |
| `Data/Core/Defs/Drugs/GoJuice.xml` | `477A975458AE6A0AFB78071913F87A049704F727DF6A958BF68BD9EAFB05489A` |
| `Data/Core/Defs/Drugs/Luciferium.xml` | `03724E316919FA1BDE6ADE0C07277DFFA2949D2863BFD36D139DF1A76C70128E` |
| `Data/Core/Defs/Drugs/Penoxycyline.xml` | `D53DB62EF325CE0B189885395DA0927A29DA56CCC62C561A456BC3BC3FCEB23B` |
| `Data/Core/Defs/ThingDefs_Races/Races_Humanlike.xml` | `D95BB2236294115B32EEEA1CB0F2EDAD0D255E7530B9A8C209A7E6C365A80BBF` |
| `Data/Core/Defs/Misc/LifeStageDefs/LifeStages.xml` | `9E64CA6431D6685B342F3BEC90166A911948AEB364A232FF8EB2FE69ED321FBC` |
| `Data/Core/Defs/HediffDefs/Hediffs_Global_Misc.xml` | `D3D38552D52B19273EEF855E2D62E3155267B8176F640E6018C7D9A04C919D57` |
| `Data/Biotech/Defs/GeneDefs/GeneTemplateDefs.xml` | `2A2ABB1AF3179EAAEE9E15B7AC0D263498C7E7BCF920B30EE8288CED3E769764` |
| `Data/Odyssey/Defs/Drugs/Psilocap.xml` | `7D17A709811003F57AA5ABDD2A407FE9CC0C5040C186022F3A3324DAAFEEB7CB` |

Hashes identify evidence only. They are not runtime admission checks. XML inheritance/defaults were
read alongside native field defaults; these are source-derived comparator values, not a claim that
the installed active mod list has those finalized values.

## Native facts that change the calculation

| Inspected type/method | Observed fact and consequence |
| --- | --- |
| `DrugStatsUtility.GetSafeDoseInterval` | Returns 0 for missing/non-addictive drug comps; -1 for addictiveness at least 1, missing tolerance giver or zero body factor. Otherwise uses the greater of overdose offset and a gain/absolute-decay ratio, divided by body factor. It does not validate deterministic negative decay, all outcomes or population assumptions. It is not the C1 calculation. |
| `CompDrug.PrePostIngested`, `Thing.Ingested` | Addiction compares existing tolerance with `minToleranceToAddict` **before** the current dose's outcome doers. An existing addiction instead gains severity. There is no proof of safety from comparing one dose to a threshold without considering prior exposure. |
| `CompDrug.PostIngested` | Ordinary and random large overdose are inside its Addictive/flesh branch in this build; non-addictiveness still does not establish harmlessness of other outcomes. C1 declines any nonzero overdose fields rather than exploiting that branch. |
| `IngestionOutcomeDoer_GiveHediff` | Positive severity overrides `initialSeverity`; a nonpositive value falls back to `initialSeverity`. The doer applies one effect per invocation, ignoring `ingestedCount`; the private `divideByBodySize` defaults false. C1 requires an explicit positive whole-dose effect and a proved quantity path. |
| `AddictionUtility.ModifyChemicalEffectForToleranceAndBodySize` | Existing hediffs can modify the effect; active relevant gene factors multiply it when requested, then body-size division applies when requested. A name or `IsDrug` cannot establish an upper bound. |
| `HediffComp_DrugEffectFactor` | Its native curve falls from 1 at severity 0 to 0.25 at severity 1; for admitted nonnegative tolerance this is non-amplifying. Other effect modifiers need separate admission. |
| `HediffComp_SeverityPerDay`, properties, `HediffWithComps.PostTickInterval` | The decay can be randomized, reversed, age-gated, mechanitor-scaled or stage-scaled. C1 requires a single deterministic negative contribution with no such variation. Native severity arithmetic is binary32, clamped by the Hediff setter. |
| `HediffComp_SeverityModifierBase.CompPostTickInterval` | Adds `severityPerDay * 0.0033333334f` on 200-tick hash intervals. A day is 60,000 ticks; 300 decay events represent one day before dispatch delay. |
| `Thing.DoTick`, `Pawn.TickInterval`, `Pawn_HealthTracker.HealthTickInterval`, `Gen.IsHashIntervalTick` | Normal ticking uses batches up to 15 ticks. A 14-tick dispatch allowance models that batch size, but camera-driven transitions with negative hash phases can skip whole decay events; it is not a universal clearance bound. See the independent-review correction below. |
| `Pawn_DrugPolicyTracker.AllowedToTakeScheduledNow`, `DrugTakeRecord.LastTakenDays` | For frequency at least 1, eligibility compares integer day indices and rounds frequency to an integer. It does **not** require that many elapsed 24-hour periods. An N-day setting can become eligible after almost N−1 actual days. `ShouldTryToTakeScheduledNow` adds timing deferral, which must not be treated as a guaranteed extra minimum. |
| `GenDate.DaysPassedAt` | Uses `FloorToInt((float)gameTicks / 60000f)`. Binary32 conversion/division also shift day boundaries slightly; the derivation below retains a bound across the nonnegative signed-int game-clock domain. |
| `TickManager` | The saved game-clock field is a signed int, starts at its zero default and normally increments by one. `DebugSetTicksGame` and fast-ecology's 2,000-tick jump are explicit alternatives, not ordinary clearance simulation. C1 uses the nonnegative, non-wrapping normal-clock domain; it does not promise protection after a clock rewrite or integer wrap. |
| `Dialog_ManageDrugPolicies.DoEntryRow`, `Widgets.FrequencyHorizontalSlider` | Ordinary frequency editing offers 0.1–25 days, integral for frequencies at least 1. The underlying float can store larger values, but the native editor cannot choose them. C1 keeps outputs inside the admitted editor range and reports larger required values unavailable; it never shortens a result to 25. |
| `JoyGiver_Ingest.CanIngestForJoy` | Recreation checks `allowedForJoy`, with native drug-desire/mental-state exceptions, and does not consult scheduled frequency. C1 therefore does not pair its interval with autonomous recreation. |
| `Pawn.BodySize`, `LifeStageDef`, Human XML | Body size is race base size × life-stage factor. Current Human base is 1 and adult factor defaults to 1; teenage/child/baby factors differ. The adult baseline is measured, never guessed from a drug helper's default argument. |

## Chosen C1 objective and admitted facts

**Decision:** suggest a conservative schedule based on modeled clearance of one ordinary whole
dose's tolerance before another scheduled dose, for a disclosed healthy adult baseline.
This is an independently chosen risk-reduction objective, not the least restrictive usable policy,
a guarantee of actual clearance or against addiction/harm, or parity with the source generator. Native
camera-driven batching can leave residual tolerance at the suggested next dose; the preview must
disclose this. The model intentionally forgoes
more frequent use that might remain below an addiction threshold. Other drug effects remain real.

The current S029 finalized catalog supplies rows, identities and generation. A detached fact
projection must prove the following before a row becomes a candidate; no Def-name whitelist is used:

- An ordinary native drug comp and whole-dose ingestion path; `defaultNumToIngestAtOnce` and
  `maxNumToIngestAtOnce` both 1. Non-medical routine use with positive joy is the initial purpose.
- Finite `0 < addictiveness < 1`, finite positive minimum addiction tolerance, an actual chemical
  and its actual tolerance HediffDef. Both ordinary overdose offset endpoints and large-overdose
  chance are zero. Negative/malformed probability/range values are unavailable, not harmless.
- Exactly one native GiveHediff outcome for that tolerance, with chance 1, positive explicit
  severity, a known body-size flag and known non-amplifying modifier path. Initial baseline
  normalized gain `g` is positive, finite and **strictly below** the addiction threshold and at or
  below the hediff maximum; its minimum severity is zero. Saturation is not used to make a drug fit.
- One native deterministic negative SeverityPerDay comp; zero random range/reversal probability,
  minAge 0, mechanitor factor 1 and stage severity factors 1. No other contribution can increase or
  replace that tolerance, disable its clearance or change the admitted tick cadence.
- Other outcomes/callbacks are inspected native effects known not to increase the modeled exposure
  or alter its decay: the measured native high/Alcohol and Rest-offset shapes qualify only after
  their finalized references/comps are checked. Unknown doers, overridden drug callbacks or altered
  relevant methods are unsupported until separately researched. Known impairment/rest effects are
  shown as remaining risks, not silently certified harmless.

This is semantic admission, not a blanket rejection of mod-added content or foreign Harmony owners.
Compatible native-shaped added drugs may qualify. A changed high/tolerance callback that affects the
modeled channel cannot qualify merely because its base type or field names still exist.

## Units, discrete arithmetic and calendar conversion

Inputs are the actual binary32 severity values from admitted finalized facts. Severity is a
dimensionless fraction (0.03 = 3 percentage points), not a percent integer. Negative `s` is severity
change per 60,000 simulation ticks. The baseline `g` is the explicit giver gain after the admitted
body-size operation using the finalized adult Human baseline B (currently 1), and native rounding.
The initial baseline has no relevant gene/effect multiplier; 1 is the neutral factor, not a
population-wide assumption. C1 does not calculate a shortened interval for resistant/immune genes.

1. Let `q = binary32(s * binary32(0.0033333334))`. Require finite `q < 0`.
2. Starting at `x = g`, perform the native binary32 recurrence
   `x := max(0, binary32(x + q))`. Let `k` be the first event count reaching zero. Reject a nonfinite
   value or non-decreasing step; do not use a guessed epsilon or a ratio rounded toward shorter use.
3. Model kth decay at `200*k + 14` ticks. The 14 is an allowance based on the inspected maximum
   interval batch minus 1, not an added-delay preference or a proven worst-case native bound.
4. Bound calendar rounding separately. For nonnegative signed-int game ticks, conversion to float
   contributes at most 64 ticks of error below 2^31 and division rounding contributes at most
   117.1875 ticks (half a day-count ULP at the upper bound × 60,000). Their sum is below 200 ticks
   per endpoint. Reserve 400 ticks for the pair, plus 1 for strict action ordering.
5. `requiredTicks = 200*k + 14 + 400 + 1`; suggest integer
   **`N = 1 + ceil(requiredTicks / 60000)` days** using checked integer arithmetic.
   The extra whole calendar day accounts for the lost fractional day in native scheduling.
   This is the smallest integer satisfying this deliberately conservative modeled-clearance budget,
   not a mathematical claim that no shorter policy could be reasonable.
6. With the inspected native editor maximum of 25, at most 7,197 decay iterations can produce an
   admitted suggestion: `floor((24*60000−415)/200)`. This bounded calculation follows the UI
   representability boundary. Still positive after that count means unavailable; do not clamp,
   substitute 25, wrap integers or loop without a bound. No extra delay setting is added in C1.

The normal, nonnegative game clock and unsuspended normal pawn ticking are model assumptions.
Clock rewrites, signed-clock overflow, custom tick suspension/cadence, fractional-dose pipelines and
unproved effective-body changes need separate analysis; a policy is not a monitoring/enforcement
service. Native suspension/history adjustment must be verified before extending the clearance claim
to a suspended or off-map path. Opening/editing the generator in world view still works.

### Independent-review correction: ordinary batching can skip events

The independent review found a concrete flaw in the initial claim that `200*k + 14` universally
bounds native clearance. `Gen.IsHashIntervalTick(Thing,int,int)` evaluates
`Math.Abs((TicksGame + HashOffset()) % interval) < delta`. Thing ID1 has hash−1471036369.
At tick368, close-view delta1 gives remainder−1 and no decay. Moving out of view at tick369 crosses
a 200 boundary without a 15-tick callback; at the next callback, tick379, delta11 and remainder−190
also produce no decay. Repeating such normal camera transitions can skip two of three events.
Beer can then reach its three-calendar-day eligibility with about200 events instead of301.
This is a source-derived counterexample, not an executed gameplay observation.

Disposition: retain the useful positive C1 outputs as an explicit cadence-based model, remove the
universal clearance claim, and require visible residual-tolerance/timing disclosure before Create.
Do not add a monitoring service or silently strengthen S138. Bounded native scenarios must record
their actual camera/batching conditions and cannot certify all ordinary timing sequences.

### Source-derived comparison, not finalized-game results

The arithmetic below was independently evaluated with binary32 rounding (`Math.fround`) from the
inspected values. It is calculation research, not an executed product test or game observation.
Later Unit tests must test properties and boundary behavior, not merely repeat this table.

| Core drug | Chemical / tolerance | Gain / day decay | Addiction threshold | Nominal gain/decay | k | Modeled decay ticks + clock/order budget | C1 days |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Beer | Alcohol / AlcoholTolerance | 0.016 / −0.016 | 0.25 | 1 day | 301 | 60,615 | 3 |
| Psychite tea | Psychite / PsychiteTolerance | 0.03 / −0.015 | 0.10 | 2 days | 600 | 120,415 | 4 |
| Smokeleaf joint | Smokeleaf / SmokeleafTolerance | 0.03 / −0.015 | 0.15 | 2 days | 600 | 120,415 | 4 |
| Ambrosia | Ambrosia / AmbrosiaTolerance | 0.032 / −0.020 | 0.15 | 1.6 days | 481 | 96,615 | 3 |

Beer needs 301 modeled events because binary32 subtraction leaves a small positive residue at 300;
tea and smokeleaf reach zero at 600. The extra dispatch/calendar budget is why their 2-day nominal
clearance maps to 4 policy days. These conservative values reduce availability of drug joy; no price,
research, stock, nutrition, effects or ordinary manually edited policy is changed to compensate.
All four retain normal acquisition/research constraints and native eligibility; no supply is created.

## DG-Q3: population, quantity and multiple channels

The preview must state the baseline before Create: healthy adult human with actual BodySize at
least the displayed B, initially no relevant tolerance, addiction/dependency or interfering drug
effects, ordinary whole-dose normal ticking, and no extra chemical exposure. The strongest initial
claim is for neutral drug metabolism; known weaker native multipliers do not justify shorter output.
Small pawns, children, modified metabolism, an existing addiction/dependency or a treatment need
are outside this baseline. A shared policy cannot enforce those assumptions for every present or
future assignee. Show the limitation; do not hide a pawn selector or silently rewrite assignments.

C1 uses actual chemical identity **or** actual tolerance-Hediff identity to connect exposure rows;
the transitive connected group is one exposure channel. Same chemical with different givers and
different chemicals with the same tolerance therefore cannot evade grouping. If two otherwise
eligible candidates share a channel, withhold both autonomous schedules and explain the conflict;
do not pick by translated name, market price or catalog order. Unsupported rows remain off, so the
presence of unsuggested Flake/Yayo does not alone suppress tea. Their possible manual/medical use
is nevertheless explicitly outside tea's single-source assumption.

If the player enables recreation or another scheduled row on the same channel, mark the affected
rows as **manually edited; combined exposure not calculated**. Creation remains ordinary editable
policy creation after clear disclosure; do not relabel such choices as a C1 result. A user's shorter
interval or Any/numeric tolerance choice is also a manual choice, not a stronger calculation.

S085 partial ingestion is unresolved for stochastic/categorical drug effects. An affected row gets
no C1 suggestion while partial handling is active without an admitted full contract proving this
same quantity/timing bound. Do not assume that half an item is half an addiction roll or that native
GiveHediff scales by count. Unaffected ordinary full-dose paths can still qualify. S124/S134 actual
medical exposure, pending claims and history stay with those owners; C1 never reads a schedule as
a dose, resets history, blocks necessary treatment or changes default addiction permission.

## Recreation and explicit unavailable reasons

C1 deliberately proposes **no autonomous recreation**. Native joy ignores frequency and its desire/
mental-state exceptions are not changed by the generator. The smallest positive deliverable is the
four source-supported scheduled comparisons above, subject to finalized semantic admission. A future
recreation model needs its own measured positive case; a zero helper return is insufficient.

| Reason | Examples / required visible consequence |
| --- | --- |
| Unknown pharmacology or quantity | Missing comp/chemical/tolerance, duplicate givers, custom outcome or unresolved S085 path: both routine switches off, no fabricated number. |
| No established clearance | Missing/nonnegative/random/reversed/gated decay, nonfinite value, underflow/non-progress, positive hediff minimum: no suggestion. |
| Baseline dose outside model | Gain at/above threshold, invalid body baseline or unproved amplifying modifiers: no suggestion, not a claim that the drug is always unsafe. |
| Inherent or overdose risk | Zero addiction threshold, positive overdose fields or probability, mandatory-dependency behavior: no C1 routine suggestion. Flake/Yayo have threshold 0 and positive overdose risks; Wake-up/Go-juice have no current tolerance-gain outcome plus threshold 0/overdose; Luciferium has addictiveness 1 and mandatory dependency. |
| Medical purpose | Penoxycyline is non-addictive and helper returns 0, but it is medical prophylaxis with no joy. C1 does not invent its clinically useful administration period or enable recreation. |
| Non-addictive is not harmless | Odyssey psilocap has helper return 0, positive overdose metadata, tolerance and an independent native 1% breakdown outcome. No autonomous recreation or C1 schedule; Odyssey absence has no effect on other rows. |
| Shared exposure channel | Multiple otherwise eligible automatic candidates share chemical or tolerance identity: both remain off; manual combination is visibly uncalculated. |
| Native editor cannot represent interval | Computed N > 25: display required interval/reason where calculable, leave schedule off, never shorten to the editor maximum. |

## Remaining prerequisites and acceptance scenarios

DG-Q1–3 now have a concrete C1 design, not a completed feature. Independent review must assess the
native arithmetic/dispatch/calendar assumptions before implementation. Task 1.6 retains a separate
requirement to inspect actual finalized facts/effective methods on the intended ordered mod list;
source XML cannot discharge that requirement. DG-Q4 atomic creation and DG-Q5 UI/admission are
still prerequisites, followed by focused vertical TDD and the integrated live gate.

Required later observable scenarios:

1. Open Generate on the admitted Core fixture: explain the four intervals, visible separate medical
   permission and baseline. Remove/rename Beer in a separate admitted content fixture: other rows'
   calculations remain independent. Cancel changes nothing.
2. Use a supported native-shaped content fixture with very slow deterministic decay: required value
   outside 25 is unavailable, never clamped. Invalid/duplicate/unknown effects remain visible/off.
3. Add a second candidate using the same chemical but another giver, and one sharing tolerance with
   another chemical: connected-channel conflicts show no automatic schedules. Manual changes get
   honest provenance labels, remain editable and do not silently alter unrelated rows.
4. Create, inspect/edit/copy/assign/save/reload through ordinary controls; check complete publication,
   existing policy/default/assignment preservation and reset-to-current behavior under DG-Q4.
5. For a claimed clearance result, retain native first ingestion → visible tolerance → natural
   simulation decay → eligible later scheduled ingestion, including close-to-day-boundary input and
   camera/interval batching. Fixture setup may arrange the initial scene/history but cannot directly
   set the claimed clearance result or substitute a clock rewrite for simulated decay.
6. Exercise S138 Any/numeric/off and ordinary medical composition without making them a generator
   dosing service. Native support for any added population/quantity/off-map path needs its own
   observed workflow. No zero-random-event observation proves zero addiction probability.

Root retains independent review, integration and acceptance. These findings do not accept upstream
compatibility, arbitrary modded pharmacology, gene safety, illness safety or a universal safe policy.

Documentation verification: scoped `git diff --check` passed; the typed `openspec_validate` operation
returned exit 0, **167 passed, 0 failed** on 2026-11-11. This is schema validation only. Tasks 1.1–1.3
record static research completion. Independent review (1.7) found the timing issue documented above
and the contract was corrected; finalized native projection (1.6), all implementation/TDD and all
live tasks remain open. The corrected contract passed a fresh typed OpenSpec validation:167/167.
