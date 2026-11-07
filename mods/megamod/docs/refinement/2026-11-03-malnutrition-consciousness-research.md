# S045 — Consciously Malnourished research

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
Inspected **2026-11-03**, origin replacement, handwritten **P1-37**. Research and observable draft
independently reviewed and validated; not implementation or a live defect reproduction. The separate
[change](../../../../openspec/changes/specify-megamod-malnutrition-consciousness/proposal.md) owns the draft.

## Documentation first and complete source

[Workshop](https://steamcommunity.com/sharedfiles/filedetails/?id=2935404464) direct reading returned
Steam's rate-limit page. A later cached primary search result and the complete installed About
description were available. The author describes replacing early malnutrition's consciousness
deductions with maximums, to avoid additional low-consciousness deaths, and claims vanilla/Combat
Extended compatibility. Those claims are not accepted beta compatibility evidence. Cached page
status banners and subscriber/comment data are not used to infer actual support or behavior.

Installed root `F:/Steam/steamapps/workshop/content/294100/2935404464` contains exactly About.xml,
PublishedFileId.txt, preview.png and `Patches/MalCon.xml`. Author metadata is **PsiGoatAryn**; Workshop
creator is displayed as ProfileName. Package **`ProfileName.MalCon`**, supported versions **1.4–1.6**,
empty dependency list, Core/Ideology/Royalty/Biotech load-after hints. No 1.7 declaration, binary,
settings, Def additions, translation catalog, source version or license file was found. No code,
art or XML copied into the product. Source behavior is fully inspectable in its four XML operations.

| File | SHA-256 |
| --- | --- |
| Source MalCon.xml | `27DBBFB600E07ECAADC5BAEA567FBA6630E316727581FA8441C0E2F87C2F11E5` |
| Source About | `3AAAA23F6D08DDCE33F4CE44274FF14CC17B648DA3488AD1242FAFA0EC0653B7` |
| Native Hediffs_Global_Needs.xml | `6DFA56F30AB51B2E107675D82BE2DD17819F41DF47AFA1D9A1C6E5C91710B5A6` |
| Native Assembly-CSharp | `56399B6144D9F1695F9C53D054DC82972F7AB35E68D1A19A7129F2F5860CCD21` |
| Inspected XML Extensions 1.6 DLL | `1E64E318C905B94DE3EF05186D82E2067933238453D818366EAC73D688464B35` |

All four source operations use vanilla PatchOperationReplace. They select
`Defs/HediffDef[defName="Malnutrition"]/stages/li[N]/capMods/li/offset` for N=1,2,3,4 and replace each
matched offset element with setMax=.95,.90,.80,.70 respectively. They **do not select capacity by
identity** and do not check stage semantics, duplicate/existing caps or original-provider presence.
Every offset in a selected stage can be replaced, not just Consciousness. The operations are
independent, so a missing later target can leave earlier changes applied. There are no rollback,
own diagnostic, optional-provider or runtime-method patches.

## Inspected 1.7 native behavior

Core `Data/Core/Defs/HediffDefs/Hediffs_Global_Needs.xml` defines one Malnutrition HediffDef:

| Stage | Minimum severity | Native Consciousness | Source replacement | Hunger-rate offset | Social-fight factor |
| --- | --- | --- | --- | --- | --- |
| Trivial | 0 (implicit) | -0.05 | maximum .95 | +.5 | 1.5 |
| Minor | .2 | -.10 | maximum .90 | +.6 | 2 |
| Moderate | .4 | -.20 | maximum .80 | +.6 | 2.5 |
| Severe | .6 | -.30 | maximum .70 | +.6 | 3 |
| Extreme | .8 | maximum .10 | **unchanged** | +.6 | no explicit stage factor |

Lethal severity remains **1**, and extreme remains life-threatening. No source edit changes severity,
initial state, food need, recovery, thoughts or the normal native condition identity. Because it edits
the shared Def, the source applies to every eligible pawn carrying that condition, not only colonists.
It grants neither a need for food nor Malnutrition to a race that does not otherwise have them.

ILSpy 11.2.0.10050 native decompilation from **1.7.4995 rev309** is under ignored evidence
`artifacts/Refinement/megamod-spec-20261103/consciously-malnourished/native`. Read:
PawnCapacityUtility, PawnCapacityModifier, PawnCapacityDef, PawnCapacitiesHandler,
Pawn_HealthTracker, PawnCapacityWorker_Consciousness and Need_Food.

- Capacity calculation first uses the worker. When positive, it adds hediff/gene offsets, multiplies
  their post-factors, then applies the smallest evaluated maximum; it applies the capacity minimum
  and rounds to hundredths. `setMax` defaults to 999; a curve override can supersede a scalar cap.
  A ceiling is not a minimum and never adds lost consciousness back.
- Consciousness's native worker starts with brain/tag efficiency, subtracts pain loss, and combines
  blood pumping, breathing and blood filtration with each of those contributions capped at 100%.
  This feature does not modify that worker. The separate S046 analysis now specifies its replacement;
  see [physiological contributions](2026-11-03-physiological-consciousness-research.md).
- Core Consciousness is lethal for flesh and mechanoids when they cannot sustain the capacity;
  ordinary capability requires level above the capacity's minimum-for-capable. Native wakefulness
  normally requires Consciousness ≥.3, unless always-awake/deactivation rules say otherwise. Health
  still checks lethal hediffs, required capacities, core-body efficiency, lethal damage and native
  death prevention. This mod cannot promise survival from arbitrary injury/starvation combinations.
- Need_Food updates every 150-tick need interval: while starving it adds a pawn-seeded malnutrition
  amount; while fed it subtracts that amount. Base is .0011325 per interval (.453/day), scaled by a
  seeded .8–1.2 factor. Normal freezing/deathrest rules apply. Food fall includes native malnutrition
  hunger-rate effects, age/race/metabolism and other native factors. None is changed by source XML.

### Static balance comparisons, not finalized or live evidence

For isolated trivial malnutrition with no other post-factor and an otherwise fixed consciousness C:

| C before this malnutrition contribution | Native flat -5 points | Draft ceiling 95% | Implication |
| --- | --- | --- | --- |
| 100% | 95% | 95% | Same ordinary healthy baseline |
| 60% | 55% | 60% | Does not add a new deduction to an already impaired pawn |
| 34% | 29% | 34% | Preserves native wakefulness above 30% when no other rule prevents it |
| 120% | 115% | 95% | Stronger restriction on boosted pawns; deliberate cap semantics |
| 4% | 0% after native floor | 4% | Avoids this particular zero-capacity cause, not unconsciousness or other lethal causes |

The same comparison uses 90/80/70% for later non-extreme stages. Extreme still caps at 10%; lethal
severity remains 100%. These are source/native arithmetic comparisons only. Actual finalized capacity
providers, health consequences, native tooltips and feeding/recovery need reviewed-build acceptance.

## Concrete provider/consumer evidence

Bounded installed text/binary scans found no executable binding to `ProfileName.MalCon` or its Workshop
ID. The source has no assembly/API/own saved type. Native Malnutrition is referenced by ordinary
scenarios, achievement Defs and medical logic; preserve that identity and severity. No source-package
facade or spoof is justified, and author CE compatibility is not a tested claim.

Three actual installed XML examples show why semantic selection matters:

- **Malnutrition Reworked**, `GodlyAnnihilator.MalnutritionReworked`, Workshop 2898001855:
  `Defs/HediffDefs/Hediffs_Malnutrition.xml` supplies the same named Def with thresholds 0/.5/1/2/3,
  lethalSeverity=-1, extra stat/rest effects and multiple BloodFiltration/Talking/Consciousness entries.
  Blind source ordinal selectors can alter the wrong capacities. This is an unsupported alternative
  malnutrition model, not a second selected implementation to silently compose.
- **Better GC: Mothballed and World Pawns**, `GwinnBleidd.MothballedAndDeadPawns`, Workshop 2982026860:
  `Patches/Core.xml` adds allowMothballIfLowPriorityWorldPawn to Malnutrition. This unrelated addition
  should survive; presence of another patch to the Def is not incompatibility by itself.
- **Simply Expansive**, `joshjosh100.SimplyExpansive`, Workshop 3422548588:
  the inspected **1.5** `Patches/Patches_Hediff.xml` replaces the stages list with extra stages and
  capacities, including positive consciousness/eating offsets. It demonstrates a structural conflict;
  no claim is made that this historical folder is selected or operational on the installed beta.

These are bounded compatibility examples, not full audits or user-selected additions. Original
Consciously Malnourished active suppresses the owned overlap. S046 Consciousness Patch is analyzed
separately: its physiological result precedes the native final-capacity maximum phase, preserving
these ceilings. The combined contract is resolved; exact beta/provider runtime verification is open.

## XML Extensions, merge policy and unresolved implementation

Primary [Conditional](https://github.com/15adhami/XmlExtensions/wiki/XmlExtensions.Conditional),
[FindMod](https://github.com/15adhami/XmlExtensions/wiki/XmlExtensions.FindMod) and
[Log](https://github.com/15adhami/XmlExtensions/wiki/XmlExtensions.Log) documentation establishes
node-existence branches, package-ID selection (`packageId=true`) and warning logging. These do not
establish atomic rollback. The installed 2574315206 About and LoadFolders advertise only through 1.6;
Harmony is its required dependency. A working 1.7 framework selection must be proven before coding.
No framework assembly is bundled or modified for this research.

Adopt XML Extensions guards plus narrowly scoped Consciousness-only field changes. Identify stages
by their severity intervals and unique relevant capacity, not ordinal alone or localized label.
Retain all other stages/capacities/fields and stronger existing scalar caps; reject ambiguous or
incompatible changes instead of inventing a merged hunger model. Replacing the negative offset with
a maximum is intentional scalar semantic replacement; it does not authorize authored replacement of
the stages/capacity lists. Atomic group publication and effective/finalized checking are implementation
work, not properties assumed of a vanilla sequence or XML Extensions case container.

No settings exist in the source and none are introduced: this is a fixed selected behavior, not a
global-only configuration exception. Current condition/severity is native pawn state. No new UI,
language catalog, Thing index or per-port health cache is justified. Native health/inspect and
S007 Compact Hediffs must present the actual remaining capacity/condition through existing surfaces.

| ID | Remaining work |
| --- | --- |
| ML-Q1 | Prove exact beta XML Extensions selection, effective content/capacity prerequisites, semantic stage mapping and atomic merge/rollback/finalized validation; create stable patch targets |
| ML-Q2 | Verify the now-specified S046 direct composition, actual supported CE/health providers and consumers, original coexistence and stricter caps; unsupported alternative models stay unchanged |
| ML-Q3 | Focused XML/domain tests, exact loaded-Def fixtures in isolated game, finalized balance/health inspection, independent implementation review and real starvation/feeding/recovery workflows |

No implementation or gameplay acceptance. The semantic goal is avoiding additive early-starvation
loss at low consciousness; it is not immunity, raising capacity to the ceiling, slower hunger or free
medical treatment. Removing the old deduction can legitimately preserve or restore native wakefulness
at a boundary such as 34%, without overriding any wakefulness/downing rule. High-capacity pawns
deliberately face absolute ceilings. Native progression and lethality
remain, and current beta/provider compatibility is still unproved.

## Documentation checkpoint

Independent source/native/contract review verified the recorded hashes and found one wording defect:
removing a deduction can legitimately preserve native wakefulness near 30%. The corrected contract,
34%/29% scenario, comparator and task now preserve native rules instead of promising no wakefulness
change. Focused re-review found **no issues**. Eight scoped docs passed local-link/whitespace checks;
typed strict OpenSpec validation passed **67 items, 0 failed**. The capability contains **5 requirements
and 15 scenarios**, with all **15 implementation tasks unchecked**. No runtime change or game launch.
