# Food Poisoning Stack Fix research — 2026-11-03

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), target **1.7 beta**.
S041/P1-32. **Source researched; owning specification drafted for review.** This record
establishes the inspected source/native behavior, not a working fix or reproduced gameplay bug.

## Documentation and pinned input

Read the [Workshop description](https://steamcommunity.com/sharedfiles/filedetails/?id=2843483188),
[maintainer README](https://github.com/emipa606/FoodPoisoningStackFix), installed README/About,
changelog/license and [merged PR 2](https://github.com/emipa606/FoodPoisoningStackFix/pull/2) before
native analysis. The advertised behavior is finite tainted-meal counts with random selection when
splitting, instead of a diluted chance shared by every meal. PR 2 describes a partial-merge
duplication fix and a developer diagnostic, merged on 2026-02-06. These descriptions and source
comments are not proof that the installed algorithm conserves counts. No reported bug was reproduced
in a game here. Steam's generic page-status banner is not treated as compatibility evidence.

| Input | Inspected identity |
| --- | --- |
| Installed root | `F:/Steam/steamapps/workshop/content/294100/2843483188` |
| Title/package | Food Poisoning Stack Fix (Continued), `Mlie.FoodPoisoningStackFix`, Workshop **2843483188** |
| About/manifest | Version **1.7.0**, supports 1.3–1.7, author Dyrewulfe; Harmony dependency/loadAfter |
| Selected DLL | `1.7/Assemblies/Food_Poisoning_Stack_Fix.dll`, assembly **1.7.0.0**, informational `1.0.0+dc261ee3c7fbc29780780467eee44302d69ce4a8` |
| DLL SHA-256 | `F34E9B83F0243F70DAA73AB6C57CFD32AD5D10171DD6A9A6E7270F203261498D` |
| About SHA-256 | `7E87A6726D76D3A19D4136311186D53726A0F82AF4C2FE962317CF5E33DEA537` |
| License | Installed MIT, copyright 2020 Mlie; assembly credits Dyrewulfe 2026; retain required notices if source is ever reused. No source/DLL/art copied into product |
| License SHA-256 | `0C853DC417F477E3CE493D3DEE8A8F5E482968A3E87C2CDB2977A302BF5F1D1F` |
| Native input | Assembly-CSharp **1.7.4995 rev309**, SHA-256 `56399B6144D9F1695F9C53D054DC82972F7AB35E68D1A19A7129F2F5860CCD21` |

No installed C# source, Def/XML patches, language directory, settings class or LoadFolders file was
found. Older 1.3–1.6 assemblies are also present, but this analysis uses the 1.7 DLL. Public Source
directory retrieval failed; no public source checkout/revision comparison is claimed. Read all
**four compiled runtime types** and AssemblyInfo using ILSpy **11.2.0.10050**, retained under
`artifacts/Refinement/megamod-spec-20261103/food-poisoning-stack-fix/compiled`.

## Complete compiled feature inventory

1. Static startup creates Harmony owner **`rimworld.dyrewulfe.fpstackfix`** and unguarded PatchAll.
   There are **three attributed patches**, no settings or own saved component/schema.
2. `CompFoodPoisonable.PreAbsorbStack` prefix suppresses native logic. It obtains the other stack's
   component through TryGetComp and reads PoisonPercent directly; cause access uses reflection and
   the recipient field is Harmony-injected. No missing-comp/field guard exists. It treats the
   float field **poisonPct as an integer count**, while leaving the public property/native save key
   unchanged. Full absorption adds the donor's integer count. Partial absorption samples units using
   remaining donor count/remaining donor size. It then assigns the computed donor remainder to
   **the recipient's** `___poisonPct`, adds the moved count, and never updates the donor's field.
   Cause handling is one cause for the whole resulting stack, not per-tainted-unit provenance.
3. `CompFoodPoisonable.PostSplitOff` prefix suppresses native logic. Same-Thing-ID whole-stack moves
   return without changes. Otherwise it casts the source float to integer, samples the new piece,
   writes the selected count/cause to the piece only when at least one was selected, and writes the
   remaining count to the source. During sampling it both decrements the remaining tainted count
   and subtracts the already-selected count again in the next probability numerator.
4. `ThingComp.CompInspectStringExtra` postfix appends hard-coded English diagnostics **only in
   developer mode**: clean, integer tainted count/cause, fractional native-format chance or stale
   cause. It introduces no ordinary settings or bottom tab. This debug inspect placement conflicts
   with the user's top-debug-toolbar rule and must be deliberately relocated in an owned draft.

All private-field reflection and patch application are unchecked. The source does not patch
SetPoisoned, recipe creation, ingestion, native public PoisonPercent, save serialization or load
conversion. Its use of the native float/save key means old-format probability and new-format count
are ambiguous, particularly a value of 1. No automatic migration or safe-removal claim is established.

## Code-derived arithmetic findings

These are traces of the inspected bodies, **not live reproductions**:

- **Partial merge overwrites recipient state and retains donor taint.** For recipient 8 clean meals,
  donor 5 meals with 2 tainted, and transfer 2: whatever count `k` is sampled, the recipient's new
  field becomes `(2 - k) + k = 2`; the donor stays 2. Physical stacks become 10 and 3, but total
  recorded taint becomes 4. Any prior recipient taint was overwritten in the partial branch.
- **Zero-transfer path can still mutate taint.** Native ThingWithComps invokes PreAbsorbStack with
  the computed transfer count even when zero. For a full recipient and nonempty donor, the partial
  branch runs zero draws, replaces recipient taint with donor taint and leaves physical quantities
  unchanged. A no-op must be explicitly covered in the replacement.
- **Split sampling double-subtracts prior selections.** Splitting 2 from 3 all-tainted meals first
  selects one with probability 1, then selects the second with probability `(2 - 1) / 2 = 1/2`.
  Failure leaves 2 tainted units recorded in a 1-unit source stack. Total count conservation alone
  is therefore insufficient; each count must also remain between zero and its physical stack size.
- **Fractional state is silently excluded from discrete selection.** A native probability below 1
  casts to zero; split returns without native probability/cause propagation. This is not a proven
  conversion to a finite population preserving the old risk.
- **Cause selection is not selected-unit attribution.** A partial draw need not move any tainted
  unit, but cause selection occurs before that draw from whole-stack values. A later notification
  can therefore name a cause unrelated to the consumed unit. Exact intended cause behavior remains
  to specify; do not advertise complete provenance parity.

The public merged PR documents the intended repair, but the installed code still has the assignments
above. Do not claim the PR fixed the currently inspected binary solely from its title/changelog.

## Native creation, stack and ingestion boundaries

Read complete native CompFoodPoisonable and selected relevant portions of Thing, ThingWithComps,
FoodUtility and GenRecipe, retained under the sibling `native` artifact directory.

| Native path | Inspected behavior / implication |
| --- | --- |
| SetPoisoned | Writes probability 1 and cause; no unit-count argument |
| Notify_RecipeProduced | Rolls room food-poison chance first, then cook chance; on success calls SetPoisoned |
| GenRecipe.MakeRecipeProducts | Sets the product's entire stack count before calling Notify_RecipeProduced once for that product; interpreting probability 1 as one unit changes multi-unit batch semantics |
| Native PreAbsorbStack | Weighted average of recipient probability and transferred donor probability; cause uses expected tainted quantities; native float remains a probability |
| ThingWithComps.TryAbsorbStack | Checks CanStackWith, computes count, calls every comp's PreAbsorbStack, then base checks/computes/transfers; side effects before a changed/failed base transfer need coherent transaction handling |
| Thing.TryAbsorbStack | Directly increments recipient/decrements donor; does not call SplitOff for partial absorption; return false means donor remains, not necessarily no transfer |
| Thing.SplitOff | Whole-stack returns the same object; partial creates piece and decrements source; ThingWithComps then invokes source PostSplitOff(piece) |
| Thing.Ingested | Computes actual numTaken; for partial consumption invokes SplitOff(numTaken) but then calls PostIngested on the original Thing, now the remainder; for full consumption it can destroy before PostIngested |
| CompFoodPoisonable.PostIngested | Rolls poisonPct times GetFoodPoisonChanceFactor and calls native AddFoodPoisoningHediff; source does not replace this probability consumer |
| GetFoodPoisonChanceFactor | Difficulty factor plus hediff/gene factors, and relevant mutant immunity; do not multiply by tainted stack count accidentally |
| DangerousFoodType | Separate native raw/type/trait food-risk path in Thing.Ingested; not the cooked-taint accounting feature |
| Native saves | Scribe float `poisonPct` and enum `cause`; no discriminator for the source's integer reinterpretation |

Consequences needing a real owning spec: track the **consumed** portion rather than the remaining
stack; preserve actual transferred/remaining quantities across partial merges, whole moves and
zero/failed transfers; decide explicit new-batch/legacy-state policy; retain difficulty/immunity and
independent dangerous-food risk. Splitting single meals first often masks the partial-ingestion
hazard but does not prove every native/provider path does so.

## Consumers, UI and composition research

A bounded Workshop/local-Mod text scan for source package/assembly/Harmony owner found only its
own metadata, a mod database and layout test mod lists. An ignore-disabled DLL literal scan found
only its five versioned source DLLs. No external binding to the four source classes was established;
dynamic/uninstalled consumers remain possible. This does **not** establish that changing native
poisonPct semantics is safe: the public/native contract has unrelated users.

Concrete repository consumer: **Immersive Chefs**, `mods/ImmersiveChefs/Source/Dining/DiningHarmonyPatches.cs`,
UnifiedFoodPoisoningPatch prefixes CompFoodPoisonable.PostIngested, reads poisonPct as a probability,
combines it with serving/sanitation/temperature risk, temporarily writes a probability, and restores
the original value in a finalizer. MealInspectionAndPoisonAttributionPatches separately suppresses
latent inspection for covered meals and supplies actual poisoning cause attribution. These files
were inspected read-only. PreparedFoodRuntime's component query currently sums zero per entry;
do not misdescribe it as a PoisonPercent consumer. Neither mod may gain a runtime dependency on
the other; any interaction needs a narrow optional package/shape/ownership contract and native proof.

Owned S021 hauling, S027 deliveries and future S123 meal carrying can
split/merge/move physical items. A taint accounting feature must compose directly with these common
physical transfers, not keep one extra competing inventory/candidate cache. Taint is hidden state:
ordinary inspect/search/sort must not identify contaminated units. A debug-only readout belongs in
the top debug toolbar under the user's policy; source developer-mode inspect text is not copied.

## Additional native and provider evidence

Core `ThingDefs_Items/Items_Food.xml` declares CompProperties_FoodPoisonable on
MealBaseIngredientless and Pemmican. The meal base limits ingestion to one unit; Pemmican's own
entry does not declare that limit. Actual finalized inheritance/ingestion still needs a game run,
but multi-unit supported food is a concrete Core case, not just a hypothetical mod. XML SHA-256:
`9B111FCDFD43FE6BAD8E14C0F8CFB6D723365FB82C7732245DF82DD2A49FBDC4`.

Further read-only repository inspection: CompCulinaryState transfers serving records from the end
of its list in split/absorb and consumes one record in PostIngested. DiningIngestionOutcomePatch
snapshots a serving before Thing.Ingested, independently of UnifiedFoodPoisoningPatch's later peek.
A probability-only adapter therefore cannot establish agreement on the consumed serving for every
partial/multi-unit path. Current source hashes (not installed/provider acceptance):
DiningHarmonyPatches.cs `AF2E831F855AA6406CBC23707E148D79FD4EEFE57E1C65906F45B2D1F4F0BE5C`;
CompCulinaryState.cs `CD9BF6D147453ED0FD99A6843848476E9A26096DDF3E77E8AADB412EE656EDF2`.

## Proposed owned contract and remaining work

The [owning change](../../../../openspec/changes/specify-megamod-food-taint-accounting/proposal.md)
now specifies finite cause-aware quantities, fair transfers and actual consumed portions while
retaining native probability semantics. Native whole-batch taint and one native-modified illness
check per ingestion remain authoritative. Known-native probability-only food gets a stable one-time
per-unit assignment; ambiguous source formats are not guessed. The source debug inspect text moves
to a read-only top-debug action. These are documented proposed corrections, not user-confirmed
extra requirements or claims that the source already behaves this way.
Independent draft review required an explicit cross-boundary transfer contract: known-native
unassigned food normalizes first, whereas a real tracked/unsupported merge retires tracking on both
operands/results/remainders and preserves unsupported provenance. No-op attempts do not retire it.
The draft also now states the deliberate illness-distribution differences of finite exposure; native
calculation/illness handling is preserved, not every averaged-stack probability.

| ID | Work still required before implementation/acceptance |
| --- | --- |
| FP-Q1 | Exact native allocation/actual-delta/consumed-portion seams, bounds/fairness and cause handling; drafted scenarios require real implementation proof |
| FP-Q2 | Establish trustworthy saved-format recognition, stable assignment, atomic save backup/recovery and suspended-state reconciliation; source converter deliberately not supplied |
| FP-Q3 | Validate native probability projection and complete Immersive Chefs serving/risk/finalizer ownership, original-provider suppression and atomic effective admission |
| FP-Q4 | Top-debug UI/privacy, focused RED/GREEN, measured costs and final native cook/haul/consume/save/provider evidence; no runtime acceptance yet |

Independent source/native arithmetic review found no substantive arithmetic or lifecycle errors.
Its one factual wording finding was corrected: component/PoisonPercent access is direct, while
cause uses reflection. This review accepts only the research description, not gameplay or a feature spec.

No runtime code, source mod, game installation, normal configuration or save changed. No game ran.
