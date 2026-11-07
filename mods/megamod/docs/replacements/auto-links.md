# Auto links

## Identity and provenance

- Owner **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`; **1.7 beta**.
- Origin replacement, S074/P2-03. Source **automatic.autolinks**, Workshop **2059389912**, AUTOMATIC.
  Installed metadata supports1.1–1.6; selected1.6 fallback assembly1.0.379.18/file1.0.379.0018.
- [Workshop](https://steamcommunity.com/sharedfiles/filedetails/?id=2059389912) and
  [author repository](https://github.com/AUTOMATIC1111/Autolinks) read before code; installed source,
  full selected class, native description/navigation and actual Info Card Plus consumer paths read.
- MIT copyright2020 AUTOMATIC1111. Retain notice if code is later reused; no upstream code/assembly
  copied. Exact hashes, input paths, evidence limits and later questions are in
  [research](../refinement/2026-11-04-auto-links-research.md).
- **Specification draft only. Nothing implemented or accepted in game.** The fallback selection is
  not a1.7 compatibility claim. Bundled Harmony is unused by the source's selected class.

## Behavior inventory

| ID | Observable behavior | Scope / proposed correction | Evidence / status |
| --- | --- | --- | --- |
| DL01 | Recipe links with outputs, fixed ingredients, research, users and anatomy, both ways | Fixed-only, no special-output inference; include building-owned recipe lists | Full source/native1.7 analysis; draft |
| DL02 | Object/terrain links with fixed costs and research | Terrain and current difficulty costs extend source raw ThingDef-only behavior | Source omission/current native cost semantics; draft |
| DL03 | Egg↔hatchling species and nonhuman corpse→species | No unfertilized guess or added human/reverse-corpse link | Full selected source; draft |
| DL04–05 | Additive ordinary descriptions and native information navigation | Preserve contextual links/history/discovery; stable owned order, no jobs/research/bills | Native description/link paths; draft |
| DL06 | Current relationships and invalidated old links | Content/cost/language/save freshness; no relation to current map stock | Explicit correction to source one-time mutation; draft |
| DL07–08 | Native consumers and guarded fallback | Info Card Plus contract, original sole-provider policy, isolated invalid relations | Actual consumer source/selected methods; draft |

All rows map to [description-link requirements](../../../../openspec/changes/specify-megamod-auto-links/specs/megamod-description-links/spec.md).
Intent and implementation choices are in the [proposal/design/tasks](../../../../openspec/changes/specify-megamod-auto-links/).
Evidence is static inspection, not native gameplay observation.

## Interactions and composed result

Use the shared finalized recipe/content relation data with S010/S012's information and bill browser.
Information navigation preserves their still-valid recipe/material/comparison state; changed choices
or context follow the browser's clearing/reset rules with an explanation. S038 availability exclusions
remain authoritative while existing content remains inspectable; only truly missing content loses
owned endpoints. Descriptive links cannot restore production. S033 physical item search is separate;
a description link does not select a real
map item or claim ingredients are available. No private map Thing cache or internal patch stack.

The source changes Def.descriptionHyperlinks globally. The selected owned design instead enriches the
native description entry with current computed relationships, preserving original/instance links.
This serves both native drawing and the demonstrated Info Card Plus GetHyperlinks consumer while
avoiding permanent Def mutation. Unknown raw-list readers are a documented compatibility limit.

## UI audit and relocation

| Source surface | Purpose | Owned placement |
| --- | --- | --- |
| Ordinary info-card description links | Follow related knowledge | Same contextual native description area and navigation |
| No source settings/tab/debug/toggle | None | No new menu, individual relationship setting or operational control |
| Info Card Plus description pane | Optional provider layout of ordinary information | Same admitted native description entry; provider layout and pause preference stay intact |

There is no new configuration or saved entity order, so no inheritance exception or private settings
system is introduced. If a later option is actually needed, its contract must use the common
global/save UI. Player-facing explanations require all seven languages; technical log-only diagnostics
remain invariant and target labels/icons remain native.

## Contracts consumed by other mods or saves

| Consumer / evidence | Contract | Owned decision / limit |
| --- | --- | --- |
| Native StatsReportUtility and Thing/Pawn | Description entry/GetHyperlinks; instance xenotype and other supplied links | Preserve metadata/context and append owned relationships only |
| Info Card Plus3776660810, kaamalauppias.infocardplus | DrawStatsWorker consumes native cached entries; description.GetHyperlinks even with body text hidden | Same native-entry contract, optional/admitted; no provider package spoof or competing window |
| Source identity in mod-manager/layout-harness lists | automatic.autolinks presence/order | Provenance only; no callable API dependency established |
| Raw Def.descriptionHyperlinks readers outside inspected path | Source-added static lists | Not promised by this design; investigate demonstrated consumers before adding a facade |

Source helpers are private, with no source settings/save component to import. Compiled-only/reflected
consumers are not excluded by the bounded text search. Do not claim a universal drop-in replacement.
No migration is selected; a later importer would need its own backup/atomicity/restoration contract.

## Implementation and coverage

Nothing is implemented. Intentional corrections: buildable terrain, building-side recipe users,
current difficulty costs, invalid-endpoint isolation, no new self/duplicate links, stable appended
ordering and supported refresh. Preserved limits: fixed ingredients only, declared outputs only,
non-pawn recipe users, egg species rather than pawn instances, one-way nonhuman corpses and native
visibility/navigation. No resource, skill, research, stock or balance changes.

Original Auto links presence disables this entire owned feature, including extensions, with an
explanation; its links are left intact. Future dependency **Harmony required** for the selected native
entry adapter; no source library bundled. No XML selected and no feature-specific XML Extensions
dependency. Other providers/DLC are optional. Every actual patch needs its target document and
semantic admission, including effective foreign changes and later repatch retirement. Unknown
required semantics keep normal information available; no patch is accepted merely by signature/hash.

## Verification and acceptance

Independent documentation review and strict validation are recorded in the
[progress ledger](../refinement/specification-progress.md). All implementation tasks remain unchecked.
Native acceptance must follow actual links and Back, preserve Add bill choices, inspect fixed-only
relations and terrain/current costs, exercise hidden/stale/removed targets and long multilingual
lists, and verify separate source/provider runs. Measure actual relation/rendering costs before
performance claims. A loaded Def, successful patch or static prepared screenshot is not acceptance.
