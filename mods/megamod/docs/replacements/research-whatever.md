# Research Whatever — replacement coverage

## Identity and status

Owner **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`; target **1.7 beta**.
Replacement **S120/P2-50**, Research Whatever by avilmask/avil, `avilmask.ResearchWhatever`,
[Workshop2552092060](https://steamcommunity.com/sharedfiles/filedetails/?id=2552092060).
Selected1.6 DLL1.0.0.0 matches pinned public1.2.5 binary/source; upstream metadata declares through1.6.
[Research](../refinement/2026-11-06-research-whatever-research.md) records exact identities,
full source inventory, optional consumers, current1.7 differences and static comparators.

**Specified only; nothing implemented or runtime-verified.**
[Change](../../../../openspec/changes/specify-megamod-automatic-research/) ·
[Observable contract](../../../../openspec/changes/specify-megamod-automatic-research/specs/megamod-automatic-research/spec.md).

## Behavior inventory

| ID | Observable contract | Retained behavior / intentional change |
| --- | --- | --- |
| ARW01 | Contextual bench autonomy | On by default, player-only, persistent per bench; off still permits selected manual research |
| ARW02 | Eligible work selects an empty project | Actual bench/pawn restrictions and current player/provider selection win; move selection out of mere unusable candidate probes |
| ARW03 | Cheapest remaining work with policy | Preserve prerequisites and colony tech factor; fix facility-only omission, total-cost tie leak and additive priority penalty |
| ARW04 | Explicit bench exhaustion | No eligible project switches that bench off with one explanation; manual re-enable after new opportunities |
| ARW05 | Anomaly category selection | Primary then immediate overflow, honor existing destinations, current award once; consistent policy/visibility and occupied hybrid-slot safety |
| ARW06 | Reversible study exhaustion | Preserve local journals/finite research/corpse outcome; retain latest captured-entity fix, require usable resume control and real transition notice |
| ARW07 | Completion presentation | Native dialog/positive letter/quiet message/silent, only requested successful presentation; independent letters and completion effects remain |
| ARW08 | Common setting and contextual UI | Native dialog default, global/save/Use-global inheritance, seven languages; configuration leaves bench right-click menu |
| ARW09 | Optional policies and one provider |2 repeatables low priority,11 VGE projects excluded, guarded additive content; source-present original and safe alternate-worker fallback |
| ARW10 | Semantic admission and lifecycle | Minimal targets, native/provider fallback, bounded warnings, owned rollback/revalidation; no persistent research cache or replay |

## Interfaces and co-installation

| Surface | Treatment |
| --- | --- |
| Original package/assembly/Harmony owner | Not impersonated; original active disables this owned overlap without removing or patching it |
| `ResearchWhatever.ResearchWhateverExtansion` bool fields | Public contract inventoried; no actual external consumer found. Known source XML policy translated to owned data; original type binding not promised |
| Bench`active` and GameComponent`NotifyMode` | Owned state follows documented defaults/common persistence; no automatic source-save/settings import or safe-removal guarantee |
| Native research project/progress/knowledge | Retained once; no fake current project during scans, no grants or duplicated completion effects |
| Research Reinvented | Actual separate worker and vanilla-worker suppression inspected; adapter required for owned bench autonomy, otherwise disable that group |
| Optional XML content | Package/content/shape gates, merge-preserving extensions, coherent safe fallback; no rewriting provider cost/prerequisite lists |

Harmony and XML Extensions are required with the planned implementation, never bundled. Anomaly,
Researchable Stat Upgrades and Vanilla Gravship Expanded remain optional. No source mod, other
repository product or Gateway dependency. No code/art/license grant is copied. Current installation
lacks Anomaly data; code-derived study behavior is not a DLC-present compatibility claim.

## Composition, UI and omissions

Bench toggles and subject study/containment choices are entity gameplay state. Completion mode is
configuration in the common Research section, with main-menu save controls disabled. No new bottom
tab, queue, global operational toggle or cheat surface. S071 message placement and S074 links compose
directly through owned presentation; native research/bench/study data avoids another Ctrl+F/hauling
index or source-specific cache. Future research UI providers preserve manual selection precedence.

Intentional differences include correct remaining-work ties/priority, full actual eligibility,
safe commitment, consistent Anomaly policy, visible reversible pauses, successful notification
timing, common inherited settings and additive guarded integration. Source's current1.2.5 captured
entity fix is retained, not claimed as a missing upstream fix. No automatic chains of multiple newly
selected projects in a single study award, hidden auto-resume, or custom research content.

| Scope | Implemented | Remaining |
| --- | --- | --- |
| ARW01–10 | Nothing | All runtime code/XML, target docs, localization, focused TDD and native proof |
| Optional provider support | Nothing | Effective adapters, exact provider-present loaded Defs and native player workflows |
| Balance/performance | Nothing | Static anchors retained; finalized progression, local study value and combined scan cost unverified |
| Migration | Nothing | No source keys/type conversion or blanket removal claim |

## Verification

[Ledger](../refinement/specification-progress.md) tracks ARW-Q1–4, review and strict validation.
Final proof requires actual native bench jobs, changing prerequisites, completed projects, study and
resume, four presentation modes and independent letters, multi-map/save/settings/contextual UI and
admitted provider-present runs on the reviewed isolated build. Startup logs, patched methods,
directly assigned current projects/progress or host ordering tests cannot accept this behavior.
