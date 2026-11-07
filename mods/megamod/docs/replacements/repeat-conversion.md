# Keep Converting

Owner **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
Origin: replacement. S092/P2-21. **Specified draft; nothing implemented or live-verified.**

## Identity and provenance

Linnun, `linnun.keepconverting`, [Workshop 3461478214](https://steamcommunity.com/sharedfiles/filedetails/?id=3461478214).
Selected 1.7 Release assembly 1.0.0.0 and complete bundled source; Harmony/Ideology required upstream.
[Research](../refinement/2026-11-05-repeat-conversion-research.md) records hashes, source-set identity,
all source behavior, native queue/target/save contracts, optional VME XML and bounded consumers.
No source reuse license was established; no assemblies, code or raster assets are shipped.

## Behavior inventory

| ID | Player action -> observable result | Defaults / boundaries | Owning requirement / state |
| --- | --- | --- | --- |
| KCV-01 | Select guide, assign target -> one visible ongoing assignment | Existing ability, native target eligibility, assigned ideology; can assign during cooldown | Assign an existing conversion ability to one chosen pawn / specified |
| KCV-02 | Readiness returns -> ordinary queued/travelled cast | One pending automatic job, preserve manual work and native costs | Repeat ordinary casts without duplicating or displacing orders / specified |
| KCV-03 | Temporary obstruction -> wait; terminal change -> end | Mental/downed/drafted/range/path/cooldown waits; death/departure/role/faith loss or cryptosleep cancels | Temporary and terminal lifecycle requirements / specified |
| KCV-04 | Edit preferences -> inherited timing/range/waking/ritual behavior | 2500 ticks; range on/1000 cells; waking on; ritual pause off; bounded common controls | Configure timing and preferences through common inherited settings / specified |
| KCV-05 | Enable ritual preference -> preserve matching penalty-free opportunity | Matching Core or admitted VME ritual; no ritual automation or all-prerequisite readiness claim | Reserve conversion opportunities for the matching ritual when requested / specified |
| KCV-06 | Save/load -> same intended target/faith and at most one owned job | No silent source migration, load-time casts or stale cross-game scheduler state | Preserve assignment intent across saves without hidden migration / specified |
| KCV-07 | Missing/changed/original provider -> bounded inactive feature | Whole-product Ideology optional; VME narrow adapter, original overlap suppressed | Keep contextual UI and optional providers correctly bounded / specified |

[Change](../../../../openspec/changes/specify-megamod-repeat-conversion/proposal.md),
[design](../../../../openspec/changes/specify-megamod-repeat-conversion/design.md),
[spec](../../../../openspec/changes/specify-megamod-repeat-conversion/specs/megamod-repeat-conversion/spec.md),
[tasks](../../../../openspec/changes/specify-megamod-repeat-conversion/tasks.md).

## UI and settings audit

| Source surface | Purpose | Owned destination / deviation |
| --- | --- | --- |
| Selected guide assign/cancel gizmo | Per-pawn gameplay assignment | Existing guide commands; target and concise waiting reason, no new tab/global toggle |
| Global translated-description draw patch | Active assignment marker | Owned contextual command rendering; no fragile global matching or borrowed source/VFE texture |
| Standalone settings section | Configuration | Ideology / Repeated conversion in common ordered settings; global/default/save/Use global and main-menu disabled mutations |

Assignment state is not a setting. The source's fresh distance is 1000 but missing-key load default
is 50; owned defaults consistently use 1000. Inactive range retains its value, and malformed numbers
cannot create zero-interval loops. Full/compact commands, scope labels and reasons need seven-language
verification. New raster selection requires the repository asset process when implementation begins.

## Interactions, contracts and deliberate changes

S051 specialist work commands share real ability/job ownership through direct owned code. Existing
conversion power, cooldown groups, manual casting and target restrictions stay native. Optional
VME Exalted Priesthood uses its own role/meme/leader ability/group and corresponding ritual; no
blanket provider or beta compatibility is claimed. Ideology is needed only for this feature.

No external source-API consumer was found in bounded text and DLL-symbol scans. Preserve native
ability/job/role contracts, not an invented Comp/watcher facade. Original source presence suppresses
owned overlap. Source-specific convertTarget/trackedGuides/global keys are not silently migrated;
an importer would require a separately specified backed-up atomic conversion.

Correct duplicate queued casts, unreachable role-loss cleanup, stale targets/registrations,
unsaved intended ideology, incomplete targeting flags and cancellation that leaves owned queued work.
Preserve native command refusals such as Social incapability and ceremony restrictions; ordinary
cast/target readiness alone is insufficient to authorize an automatic effect.
Preserve temporary mental-state retention, but handle terminal reasons first. Use one primary
assignment collection without Human-only XML injection, duplicated map indexes or a static tick flag.
Any later XML use requires XML Extensions with guarded merge-preserving operations.

## Implementation and acceptance

Nothing implemented. No automatic target selection, warden conversion, role grant, ritual execution,
certainty/cooldown rebalance, source removal guarantee or performance claim. KCV-Q1–5 retain exact
runtime admission, lifecycle, settings/localization, S051/provider composition and live evidence.
Core/Harmony required on use; VME/framework optional; no source/Gateway/repository-mod dependency.

Final acceptance must personally observe the native assign -> approach/warmup -> certainty/reaction/
cooldown -> repeat/complete path, plus actual cancellation/temporary/terminal changes, busy/idle
queues, saved pending work and exact provider/settings cases on the reviewed build. Direct certainty
mutation, logs or spawned end states cannot replace causal player workflows. [Progress ledger](../refinement/specification-progress.md).
