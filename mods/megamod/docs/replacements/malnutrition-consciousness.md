# Consciously Malnourished

## Identity and provenance

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
Origin replacement, **S045 / P1-37**, `ProfileName.MalCon`, Workshop **2935404464**, About author
PsiGoatAryn / Workshop creator ProfileName. Inspected 2026-11-03; source declares 1.4–1.6, no versioned
assembly or settings. Complete source is four operations in `Patches/MalCon.xml`, SHA-256
`27DBBFB600E07ECAADC5BAEA567FBA6630E316727581FA8441C0E2F87C2F11E5`.
No included license established; no source XML, art or assemblies copied into the product.

[Research](../refinement/2026-11-03-malnutrition-consciousness-research.md) records primary-document
limits, complete XML inventory, exact native beta capacity/health behavior and concrete providers.
[Proposal](../../../../openspec/changes/specify-megamod-malnutrition-consciousness/proposal.md),
[design](../../../../openspec/changes/specify-megamod-malnutrition-consciousness/design.md),
[observable spec](../../../../openspec/changes/specify-megamod-malnutrition-consciousness/specs/megamod-malnutrition-consciousness/spec.md)
and [tasks](../../../../openspec/changes/specify-megamod-malnutrition-consciousness/tasks.md) own the draft.
**Nothing implemented or accepted in game.**

## Behavior inventory

| ID | Native input/action → result | Selected rule / deviation | Status |
| --- | --- | --- | --- |
| ML-01 | Ordinary early malnutrition → consciousness ceiling | Trivial95%, minor90%, moderate80%, severe70%; no additional flat deduction | Drafted |
| ML-02 | Already impaired or boosted pawn → current capacity respects ceiling | Never raises low capacity; boosted capacity is still capped; stronger other ceilings survive | Drafted |
| ML-03 | Starvation/feeding → ordinary progression/recovery | Extreme10% cap and lethal100% severity unchanged; no healing or immunity | Drafted |
| ML-04 | Existing save/Health inspection → same native condition with actual effect | No new state/UI/settings; native severity and identity persist | Drafted |
| ML-05 | Unsupported condition/provider → unchanged prior behavior | Semantic Consciousness selection, atomic four-stage availability and startup warnings | Drafted |

All rows are specified in the linked observable contract, derived from complete source and native
research. The source changes every pawn using the shared condition, including non-colonists/animals.
It supplies no new hunger need, content Def, public API, job or player command.

## Interactions and composed result

S046 Consciousness Patch is analyzed and specifies one physiological result followed by these final
malnutrition ceilings in native capacity evaluation. No self-patch stack is used; combined runtime
verification remains open. S007 Compact Hediffs reads the actual native condition/capacity.
No separate Thing/health index or per-port cache is needed.

Installed examples distinguish conflicts from compatible edits: Malnutrition Reworked changes stage
thresholds/effects and is unsupported without an explicit adapter; Better GC's unrelated mothball
addition should survive; a historical Simply Expansive stage-list replacement illustrates ambiguous
ordinal targets. These are bounded XML observations, not beta compatibility certifications. Source
claims of Combat Extended compatibility remain unverified pending exact active-mod proof.

## UI audit and settings

The source has no menu, bottom tab, toggle, debug action or setting. The owned feature adds none;
ordinary Health/capacity details show the effect in the game's languages. Condition and severity
are pawn gameplay state. Fixed ceilings are selected behavior, **not** a global-only settings exception.
Any future configurable variant must specify common settings scope and safe shared-Def lifecycle first.

## External and save contracts

| Contract / evidence | Preserve or unsupported | Verification boundary |
| --- | --- | --- |
| Native Malnutrition Def identity/severity, scenario/achievement/medical references | Preserve; no new condition or severity conversion | Loaded Defs plus actual starvation/feeding and save/load |
| Native Consciousness/capacity/death behavior | Preserve all but the four intended offset-to-ceiling changes | Real health UI, low/boosted examples and ordinary extreme/lethal progression |
| Source package/assembly/settings | No assembly or saved settings; no executable package consumer found in bounded scans | No package spoof, facade or source migration claim |
| Other capacity/stage/condition fields | Preserve unrelated entries and stronger scalar caps; reject ambiguous stage models | Exact provider-present/absent and changed-shape cases |

No file migration or new saved type. XML application retains prior state and restores only owned
changes on failure; no hardcoded baseline restoration. Feature removal can restore flat deductions
and alter survival, so safe mid-save removal/provider swapping is not claimed. Later conversions
need their own backup, atomic publication, restoration and verified postconditions.

## Implementation and coverage

| Scope | Implemented | Intentional deviation / risk | Missing |
| --- | --- | --- | --- |
| ML-01–05 | Nothing | Source values retained; stronger caps preserved and unrelated offsets protected | All code/XML/tests/native acceptance |
| Original selectors | Nothing | Reject ordinal-only, all-capacities matching and partial application | Minimal per-patch target checks and atomic final XML proof |
| Provider composition | Nothing | Original source suppresses overlap; no blanket ban on other edits | S046 combined runtime proof and exact framework/health providers |
| Settings/state/performance | Nothing | No speculative controls, migrations or cache | Native load/recovery/UI and bounded load-cost evidence |

XML Extensions (`imranfish.xmlextensions`, 2574315206) is required when implementing this guarded XML;
its Harmony dependency must be present. The installed framework advertises only through 1.6, leaving
beta selection/support open. DLC and providers are optional. Active `ProfileName.MalCon` leaves the
owned overlap unavailable with bounded warnings; its own operations remain untouched. No owned
gameplay Harmony patch is currently planned and no framework/source/Gateway DLL is bundled.

Use XML Extensions to check actual Def/node shapes and package IDs for optional content. Merge only
the relevant offset/cap fields; do not author replacement stages or capacity lists. All four stages
form a coherent group. Every skipped enabled patch needs the documented startup warning and fallback.

## Verification and acceptance

S046's [contribution draft](physiological-consciousness.md) resolves ordering: its physiological
benefit precedes these final ceilings; no internal patch is needed. ML-Q1–3 retain beta framework/
atomic XML, S046/provider verification and focused/native acceptance.
Static arithmetic comparators are not finalized balance proof. Final reviewed-build evidence must
show ordinary starvation and actual eating/patient feeding, resulting consciousness/health, recovery,
load and exact optional-provider scenarios in fresh minimized isolated games. Personally inspect
causal screenshots and native health outcomes; loaded Defs and logs alone do not close gameplay tasks.
