# Spines Matter More Now — replacement coverage

## Identity and provenance

Owner **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
Origin replacement **S099/P2-28**. Source **Spines Matter More Now**, About author Jdalt / Workshop
Jdalt40, `Jdalt.SMMN` / canonical `jdalt.smmn`,
[Workshop 2971629404](https://steamcommunity.com/sharedfiles/filedetails/?id=2971629404).
Advertised support ends at 1.6; selected 1.6 assembly/file is 1.0.0.0. The author reserves reuse rights.
[Research](../refinement/2026-11-05-spine-manipulation-research.md) records complete source/binary,
hashes, native 1.7.4998, comparator definitions and bounded consumer scan. Independently implement
the behavior; no source code/binary/art copying. **Nothing implemented or game-accepted.**

## Behavior inventory

All rows are **specified, not implemented**. Evidence is in the research above and the
[observable contract](../../../../openspec/changes/specify-megamod-spine-manipulation/specs/megamod-spine-manipulation/spec.md)
defines the proposed result.

| ID | Input → observable result | Source scope and owned boundary | Requirement |
| --- | --- | --- | --- |
| SPM-01 | Spinal efficiency changes → body-based Manipulation scales once | Preserve worker-stage factor before direct health offsets/factors/caps; do not multiply final capacity or Moving | Apply spinal efficiency once to body-based manipulation |
| SPM-02 | Inspect different anatomy/replacements → native factor semantics | Healthy/absent neutral, missing existing parts retained, arithmetic mean for multiple spines, above-normal efficiency permitted | Follow actual anatomy and replacement efficiency |
| SPM-03 | Perform work/heal/replace/reload → native capacity and consumers stay current | Existing stat weights/limits/jobs, no separate per-job penalty, new injury or saved multiplier | Let ordinary work and health consume the resulting capacity |
| SPM-04 | Read Health details → current spinal contributor with other contributors | Existing native/compact presentation, no duplicate values or new window | Explain the same result in existing health information |
| SPM-05 | Co-install/change providers → single admitted effect or clear fallback | New semantic guards and original/equivalent suppression; no invented zero for unknown data | Avoid duplicate or unsupported spinal contributions |

## Interactions and composed result

- S045/S046 own Consciousness inputs and limits. Manipulation consumes their current admitted result
  once; this feature adds only its spinal factor at the ordinary body-based stage.
- S007 compact health and later RimHUD display the same native capacity/contributors, with direct
  owned presentation. No self-patches or independent health cache.
- S097 mining selection reads actual MiningYield, and jobs use their ordinary Manipulation-dependent
  stats. No spinal worker filter, extra output factor or additional job penalty is added.
- Native/optional added parts retain actual part efficiency and later direct capacity modifiers.
  RBSE's inspected 1.0-efficiency spines with +0.10/+0.20 direct offsets demonstrate why these are
  separate. No whole organ-pack replacement or universal compatibility is inferred.

## UI audit and state

| Source surface | Actual purpose | Owned destination |
| --- | --- | --- |
| Native Manipulation capacity and impactor list | Explain actual anatomy/health effects | Existing Health details and directly composed compact/HUD consumers |
| Static startup patch | Integrate capacity behavior | Guarded initialization with bounded diagnostic fallback; not a player settings or debug window |

No source settings, bottom tab, operational toggle or custom entity UI exists. No new configuration
is proposed; the common global/save mechanism therefore needs no exemption or empty settings page.
Spinal injuries, implants and their treatment remain native pawn state. Use existing localized
labels and all seven baseline languages for owned messages: English, German, Spanish, French,
ChineseSimplified, Russian and Japanese.

## External contracts and dependencies

| Contract / evidence | Treatment |
| --- | --- |
| Public patch method inside internal SMMN.HarmonyPatches | Bounded scan found no external consumer; no speculative facade or assembly alias |
| Harmony owner Jdalt.RimWorld.SMMN.Main / package jdalt.smmn | Original active → suppress owned overlap; never remove provider patches or impersonate its identity |
| Native manipulation worker/helper/outer modifier chain | Preserve the required stage and admit effective semantics, not just method presence or original IL |
| Spine-tagged custom anatomy and actual implants | Native-compatible data can participate without race-name allowlists; changed executable models need narrow optional adapters |
| Native health and saved injuries | No migration or custom saved factor; retain ordinary health lifecycle and existing save state |

Core and Harmony are required when implemented; source metadata's missing Harmony dependency is not
copied. Optional anatomy/organ providers remain absent-safe. No XML mutation is planned; any later
XML use requires XML Extensions, content/package/shape guards and merge preservation. No upstream
or Gateway assemblies/assets are packaged.

## Missing work and acceptance

[Proposal, design and tasks](../../../../openspec/changes/specify-megamod-spine-manipulation/) retain
all implementation work. SPM-Q1–3 track executable target admission, exact provider/lifecycle
contracts, finalized anatomy/stat comparators and native acceptance. Every SPM row lacks code,
focused RED/GREEN, reviewed build and causal live evidence. Deliberately omit unguarded startup,
duplicate-provider multiplication and any proposed copying of source classes or assets.

Final verification needs native Health inspection and real manipulation-dependent work with actual
spinal effects, then ordinary healing or surgery and observed recovery. Cover missing versus absent
anatomy, later offsets/caps, enhanced parts, existing consciousness, save/reload, seven-language
layouts and separate provider groups. Preserve tested build/mod identities, causal screenshots,
normal-config hashes and cleanup. A logged multiplier or directly assigned expected capacity cannot
satisfy acceptance. Semantic review must preserve ordinary implant acquisition/treatment costs,
Moving, existing thresholds and native work rules while acknowledging intended performance changes.
