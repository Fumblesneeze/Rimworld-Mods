# Mining Priority (Continued) — replacement coverage

## Identity and provenance

Owner **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
Origin replacement, S097/P2-26. Mining Priority (Continued), Uuugggg original/Halicade continuer,
`Uuugggg.MiningPriority` / canonical `uuugggg.miningpriority`, Workshop3610712721.
Selected1.7 assembly/file1.0.0.0; About supports1.6/1.7. Public commit
a1429a00feadaad8123950883b594a546bbc75c7 includes a byte-identical DLL.
[Research](../refinement/2026-11-05-mining-priority-research.md) records hashes, documentation,
complete selected source/binary analysis, native1.7.4998 comparisons, consumer scan and unknown reuse
license. **Nothing implemented or accepted; no source binary/package drop-in claim.**

## Behavior inventory

All rows are **specified, not implemented** in the
[observable contract](../../../../openspec/changes/specify-megamod-mining-priorities/specs/megamod-mining-priorities/spec.md).

| ID | Player workflow → result | Preserved or deliberately changed | Requirement |
| --- | --- | --- | --- |
| MNP-01 | Assign/designate work → eligible automatic target selected | Ordinary work priorities, current jobs, permissions and mining plans remain | Improve automatic selection without changing mining permission |
| MNP-02 | Several surface targets → rarer resources, then progress and distance | Occurrence rather than price; exact comparison precedence, explicit unknown-rarity category; retain absolute HP-damage measure | Rank surface deposits by rarity before progress |
| MNP-03 | Several drills → actual next-resource rarity, then near exhaustion | Full supported footprint, finite before fallback, independent drill toggle; native extraction unchanged | Rank deep drills by their next output and remaining finite deposit |
| MNP-04 | Assign miners → relative actual-yield qualification |100% default, inclusive0–100% fraction, no yield cap; route/target-suitable pool and ordinary rock exemption | Reserve yield-sensitive work for sufficiently productive workers |
| MNP-05 | Configure Ignore busy → retain expert or allow ready substitute | Default off; idle/seeking workers included, valid empty population has no numeric threshold and is recomputed when ready; no caller-dependent standard or preemption | Define busy-worker fallback without caller-dependent results |
| MNP-06 | Change facts or issue explicit native work → current explanation/ordinary override | Fresh paused/save/map/work state; honor initial explicit work and renewals of the same active native order without extending it | Keep choices and explanations current |
| MNP-07 | Set global/save policy → next selection follows effective choices | Eight common preferences, seven languages; omit technical cache-TTL setting | Configure policy through the common settings interface |
| MNP-08 | Provider/target changes → supported contribution or clear fallback | Coherent family activation, whole-provider-order deferral, original coexistence | Preserve native behavior when an integration is unsupported |

## UI audit and configuration

| Source surface | Purpose | Owned destination / change |
| --- | --- | --- |
| Global mod settings | Target/worker-selection policy | Work → Mining and drilling; common Global default / Current save / Use global |
| Not-the-best-miner JobFailReason | Explain added worker restriction | Contextual automatic-threshold information plus explicit-override explanation; native failures keep precedence |
| Ordinary right-click work | Player chooses work explicitly | Preserve actual1.7 initial forced-order bypass and correct renewal gating while the same native order is active; do not copy the stale disabled-right-click FAQ |
| Cache refresh ticks | Technical stale-cache interval | Omitted; freshness and shared invalidation are implementation obligations |

No bottom tab, new work column, standalone window, operational toggle or debug action is added.
Six source policy booleans retain true defaults; Ignore busy retains false; fraction retains100%,
now in whole-percent steps. New/missing saves inherit dynamically, dormant overrides survive,
inherited value controls are inactive and all main-menu save callbacks are guarded. Policy affects
new selection, not cancellation of a valid current job. Explain rarity rather than price and actual
yield rather than a skill8 cutoff. Complete English, German, Spanish, French, ChineseSimplified,
Russian and Japanese; source includes only four of those languages.

## Contracts and composed behavior

| Surface / evidence | Selected policy and remaining verification |
| --- | --- |
| Public Mining_Priority helpers/Settings/Mod.settings and TD.Utilities slider | Bounded installed symbol scan found no external consumer; no speculative facade or assembly alias |
| Nine global source keys, including priorityGoodEnough and refreshCacheTime | No automatic import; eight owned common preferences, no saved best-miner/cache state |
| Original/continued shared package uuugggg.miningpriority | Disable owned S097 overlap with bounded reason; never unpatch/remove provider or change its jobs |
| Optional custom workgiver, stat or drill geometry | Canonical package/shape and semantic admission; no type-name-based universal support |
| Unsupported non-neutral ordering | Preserve provider's entire selection order/ties; independent admitted yield gate may remain |

Compose directly with [S008 mining plans](allow-tool.md), S058 automatic-work infrastructure,
S019 shared worker facts, S052/S053 power/activity and S021/S033 map data. S019's bill skill
restriction has different membership/override rules; do not silently apply it to miners or change
it while sharing facts. No own-mod patch stack or separate per-source Thing cache.

Core and external Harmony are required when implemented; custom providers/DLC are optional. No
new Def/XML patch is currently needed. Any later XML mutation requires XML Extensions dependency,
package/content/shape guards and additive preservation. No foreign/Gateway assemblies in product.

## Implementation, omissions and acceptance

[Proposal, design and tasks](../../../../openspec/changes/specify-megamod-mining-priorities/) retain
the future implementation work. Every MNP row lacks code, executable target specs, focused RED/GREEN
and final native evidence. Deliberately omit weighted-score crossover, one shared stale benchmark,
caller-only busy exceptions, wrong drilling toggle, nine-cell exhaustion, copying provider scores,
old skill8 wording and implicit source settings import. No new yield, speed, work or resource balance.

MNP-Q1–4 retain exact effective seams/suitability, provider/consumer/license, finalized comparisons,
shared invalidation and acceptance. [Progress](../refinement/specification-progress.md) distinguishes
document review from gameplay proof. Final acceptance needs native assignment/designation/forced
actions → observed worker selection and real mining/drilling → actual damage/products, with current
thresholds, common settings/locales and separate provider groups. Preserve reviewed-build identity,
causal views, config hashes and cleanup; performance and balance claims require measured live work.
