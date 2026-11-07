# Growth Speed Makes Sense Now research — 2026-11-05

Owner **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
S088/P2-17, independently specified replacement. Documentation only; no runtime acceptance.

## Meaning and public source

Read [Workshop2969141187](https://steamcommunity.com/sharedfiles/filedetails/?id=2969141187) before
source. It describes making children's growth-point rate respond to Global Learning Factor and
requires Biotech. **This is child growth, not plant growth.** Earlier S043/S087 forward references
based on the name were incorrect and are explicitly corrected in their proposals/designs/tasks,
coverage and refinement records. No plant requirement is added under this source.

The author reserves rights and prohibits reuploads without permission. We inspect read-only and
independently specify behavior; no source, DLL or art is copied into the product. The only installed
license file belongs to the vendored Harmony development package, not an affirmative GSMSN grant.
No source repository is linked from the inspected About/page; the complete included source is used.

## Exact inspected inputs

| Input | Identity |
| --- | --- |
| Installed root | `F:/Steam/steamapps/workshop/content/294100/2969141187` |
| Title/author/package | Growth Speed Makes Sense Now; Jdalt / Workshop Jdalt40; About `Jdalt.GSMSN`, canonical `jdalt.gsmsn` |
| Metadata | Supports1.4/1.5/1.6, requires Biotech, loadAfter Core/Biotech; no declared1.7 support |
| Latest shipped runtime branch | `1.5/Assemblies/GSMSN.dll`; no1.6/1.7 folder or LoadFolders file found |
| Selected1.5 DLL SHA256 | `1AD1AA65E3CF1465DD2BF76976C828BBDFB17CBACB156F234FEF0A271E07C1B2` |
| Earlier1.4 DLL SHA256 | `8D9C0532CC7DFD60F95403E5037D01EEBEC57CD05A0F8AC0AB7CAFF9C3FC0780` — retained identity, not the analyzed implementation |
| About SHA256 | `EE66CD0EAC3810EA44E5AA1C39C69ECA61D61C9518C14F4BE96AC57070207021` |
| Included source | `Source/GSMSN/HarmonyPatches.cs`, project and AssemblyInfo; one logical runtime class |
| Assembly/file version |1.0.0.0; net472; external Assembly-CSharp and0Harmony references |
| Native input |1.7.4998 rev1299 root Assembly-CSharp.dll; SHA256 `FE9B615CF5918955AB750F9D627FB0082FD23C556BDFDEE410F88857C67A081B` |
| Local read-only evidence | `artifacts/Refinement/megamod-spec-20261105/growth-speed/{binary,native}` |

The complete selected decompiled class agrees with included source. The project references
Harmony2.2.2 as non-copy-local; About omits Harmony despite the runtime reference. Source/packages
contains many development Harmony DLLs; none belongs in the owned product. No Defs/Patches,
localization, settings class, custom saved state, bottom tab, gizmo or private cache is introduced.

## Complete source behavior

An internal static `GSMSN.HarmonyPatches` initializer creates Harmony owner
`Jdalt.RimWorld.GSMSN.Main` and installs one unconditional postfix on the public getter
`Verse.Pawn_AgeTracker.GrowthPointsPerDay`. Public callback
`GSMSN_GrowthPointsPerDay(Pawn_AgeTracker, ref float, ref Pawn)` receives the owning private pawn,
reads postprocessed `GlobalLearningFactor` with240-tick stat-cache tolerance and multiplies the
incoming result by it. The ageTracker parameter is unused. There are no faction/age/vat exclusions,
finite guards, initialization guards, migration or original-provider checks. The source modifies
the returned daily rate, never the stored growth-point total or the learning factor stat.

That is the full selected feature; no source behavior is omitted as unexplained custom scope.
The owned draft preserves it and adds semantic admission, one-provider policy, honest dependency
metadata and explicit invalid-rate fallback. No new tuning slider is invented.

## Native beta findings and limits

Read-only decompiled `Pawn_AgeTracker`, `Need_Learning`, `GrowthUtility`, `Gizmo_GrowthTier`,
`ChoiceLetter_GrowthMoment`, `Building_GrowthVat` and `Hediff_VatLearning` cover the selected path.
`CharacterCardUtility`, `StatsReportUtility` and `LearningReadout` were also inspected for possible
display consumers; LearningReadout is the tutorial UI, not child learning. No behavior is inferred
from a class name alone.

| Native contract | Observed code behavior |
| --- | --- |
| GrowthPointsPerDay | Biotech absent→0; growth-vat holder→2 before ordinary suspension; suspended pawn→0; otherwise unsuspended learning need feeds helper, else0 |
| Learning helper | Current need level × age factor(.75 below7, otherwise1) × ChildAgingMultiplier; no GlobalLearningFactor input |
| Actual accumulation | Need_Learning.NeedInterval adds daily rate×.0025 while canGainGrowthPoints is true; ordinary150-tick cadence corresponds to a60000-tick day |
| Learning need itself | Learn caps ordinary satisfaction to remaining capacity; normal decay is separate. Rate postfix changes neither |
| Growth-tier gizmo | Existing tooltip reads GrowthPointsPerDay for ordinary gain display; stored points determine progress; max tier retains native presentation |
| Tiers/birthdays | Thresholds0,30,55,80,100,120,135,150,162; growth birthdays7,10,13. These remain native, not hardcoded owned balance |
| Reward resolution | Ordinary choice resolution resets points and permits later gain; the rate hook does not bypass choices or pause state |
| Historical simulation | TrySimulateGrowthPoints uses the private learning-level helper, bypassing the getter; generated/past initialization is not multiplied |
| Saved state | Native growthPoints/canGainGrowthPoints and ordinary age/choice state; source introduces no saved keys |
| Vat skill training | Hediff_VatLearning awards its own direct skill XP; it is separate from child growth points and is not changed by this source |

These are static facts about the pinned beta, not proof of live1.7 compatibility. In particular,
vat eligibility and native need accumulation must be exercised together before accepting that
workflow; a positive rate query alone cannot prove points were actually earned.

## Actual consumers and selected-source composition

Installed text scan across source/XML/JSON/project files for package, source type/callback and DLL
found only source metadata/code plus RimPy identity data. No executable source-API consumer was
found in that scope; compiled-only/reflection consumers elsewhere remain unproven.

A separate `GrowthPointsPerDay`/learning-path search found **School & Learning**
(`tehdav.schoolandlearning`, Workshop3526830120). Its included
`Source/CompatibilitySystem/Biotech/BiotechIntegration.cs` reflects the native getter and uses the
returned daily rate×.004×a user multiplier for its school growth calculation. That is a concrete
native API reader, not proof of this optional mod's current selected binary or full compatibility.
Preserve the native getter and do not multiply again in its caller. Its curriculum and awards are
not imported into this feature.

**Need Bar Overflow**, S103 (`amch.needbaroverflow`, Workshop2566316158), includes a
`Need_Learning_Learn` patch under `#if g1_4`. That file alone does not establish selected1.7 learning
behavior. S103 must inspect its actual branch; any owned overflow policy feeds the same native rate
without a second GSMSN multiplier. **Toddlers**, S031, learns walking/manipulation from biological
development with its own vat rule; those counters and settings are separate and unchanged.

No actual plant consumer or plant-growth contract exists here. Corrected plant references are a
research-quality correction, not a conflict between the installed mods.

## Static balance anchors

Core `Stats_Pawns_General.xml` gives GlobalLearningFactor base1, minimum0. Core
`Traits_Singular.xml` gives FastLearner and TooSmart +.75 offsets and SlowLearner−.75. Under otherwise
unmodified stats, compare an ordinary native rate of2 points/day:

| Core comparator | Effective factor from these XML inputs | Owned future rate |
| --- | --- | --- |
| No offset |100% |2/day |
| Fast learner alone |175% |3.5/day |
| Slow learner alone |25% |.5/day |
| Too smart alone |175% |3.5/day; its other trait consequences stay native |

These are static normalized comparisons, not finalized Def exports, actual playtest totals or a
promise every child can receive every trait. Genes, active content, learning satisfaction, age,
difficulty and other stat modifiers can change the real values. Fast learners gain a greater chance
of reaching high tiers before native birthdays, slow learners a lower one; this is the source's
intended consequence. No compensating threshold, aging or reward change is silently added.

## Owned contract, deviations and questions

[Proposal](../../../../openspec/changes/specify-megamod-child-growth-learning/proposal.md),
[spec](../../../../openspec/changes/specify-megamod-child-growth-learning/specs/megamod-child-growth-learning/spec.md),
[design](../../../../openspec/changes/specify-megamod-child-growth-learning/design.md),
[tasks](../../../../openspec/changes/specify-megamod-child-growth-learning/tasks.md).

No separate setting exists to apply global/save inheritance to. Individual earned points remain
gameplay state. No new bottom tab or custom UI; native displays remain localized, with any new
availability explanation translated into all seven languages. Core/Harmony required on first
implementation; Biotech required for this feature and optional for the package as a whole.

Deliberate safeguards: exactly one owned multiplier; supported effective semantics and original
provider suppression; valid zero factor retained; invalid factor/product leaves a valid ordinary
rate unchanged, invalid ordinary rate yields zero, bounded diagnostics and no save repair. Source
code/binaries/assets are not redistributed. Safe source swapping or universal provider compatibility
is not claimed from having no custom state.

| ID | Remaining work |
| --- | --- |
| CLG-Q1 | Finalized learning-factor/trait/gene/age/vat comparisons and native birthday consequences; no unrecorded extra balance choices |
| CLG-Q2 | Effective getter/pawn/stat/consumer and cache/accumulation/numerical boundaries; minimal executable admission and supported later repatches |
| CLG-Q3 | Actual optional reader/provider binary behavior, original overlap, S031/S103 composition and exact save/provider round trips |
| CLG-Q4 | Vertical TDD, required metadata/localized status, independent review/package checks and personally observed native learning/vat/choice/save workflows on the reviewed build |

All runtime work remains unchecked. No game/mod execution, build, install, normal save/config edit,
Workshop mutation or publishing occurred during this research.
