# Smarter Construction

## Identity and provenance

Owner **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`; target **1.7 beta**.
Origin replacement S060/P1-54, `dhultgren.smarterconstruction`, author dhultgren/Hultis,
[Workshop 2202185773](https://steamcommunity.com/sharedfiles/filedetails/?id=2202185773).
Inspected 2026-11-04; selected installed assembly/file 1.7.0.0, SHA256
`8005E094B69EE22E6D642898CC4B8F4EEC3EDBB0829BBDCB0093367702BCB9D4`.
Public commit `c9953f1cc122cc7c0227466224e411d7805754bd`; no explicit LICENSE/COPYING file found.
No source code, tests, assemblies or artwork copied into the product.

Status: complete source/native/consumer research, observable draft independently reviewed and
strictly validated. **Nothing implemented or verified in game.**
[Research](../refinement/2026-11-04-smarter-construction-research.md),
[proposal](../../../../openspec/changes/specify-megamod-construction-access/proposal.md),
[design](../../../../openspec/changes/specify-megamod-construction-access/design.md),
[spec](../../../../openspec/changes/specify-megamod-construction-access/specs/megamod-construction-access/spec.md),
[unchecked tasks](../../../../openspec/changes/specify-megamod-construction-access/tasks.md).

## Behavior coverage

All rows are specified, not implemented.

| ID | Selected behavior | Boundary |
| --- | --- | --- |
| CT01 | Defer automatic closing around player pawns and unfinished work | Actual preparation still works; loose goods and foreign pawns are not protected by this rule |
| CT02 | Full rotated footprint and small side-connected spaces | Fewer than 100 cells; no universal escape or large-enclosure claim |
| CT03 | Physically move builder to a permitted outside work position | Real reach/permissions, no teleport or ignore-forbidden bypass |
| CT04 | Prefer nearby constrained blocking frames | Inclusive 15 cells, inside S058 categories, native eligibility and separate delivery retained |
| CT05 | Forced construction can intentionally enclose | Applies only to that action and preserves all ordinary construction requirements |
| CT06 | Preserve physical work across waiting, movement and reload | One frame/result, no duplicate resources/continuation or retry XP/failure farming |
| CT07 | Recover a genuinely stalled construction journey | 300 game ticks with checks within 50; legitimate work/wait/transit and forced actions excluded |
| CT08 | Direct quality/replacement and optional geometry composition | No minified-installation coverage; provider-specific admission and ordinary fallback |
| CT09 | Current map/worker facts, contextual reasons and safe startup fallback | No new tab/settings UI, source present owns overlap, no package or assembly impersonation |

## Differences and conflicts

Source caches omit map or worker identity in consequential places. The owned behavior rechecks the
correct current context and shares native/map facts. Source safe-position choice uses HashSet-first
and ignoreForbidden; owned relocation requires actual reach and permission. Full-footprint movement,
preparatory jobs, one continuation and genuine-stall recovery are explicit corrections. Source
readonly configuration fields are not copied as a settings framework; policy values are documented
and will be named constants, with possible controls retained for refinement.

S058 structure/floor/spike-trap categories remain above the local neighbor order; S013 worker/quality
budgets and S028 replacement materials/state stay authoritative. These are direct owned decisions,
not patches between internal ports. No delivery reorder is inferred from the author's comment.

## External contracts and omissions

Actual MultiFloors, Vehicle Map Framework and As above, So below II integrations patch source detector,
finder, priority/walkability or pawn-cache internals. MultiFloors also conditionally loads a strongly
bound source-assembly adapter; VMF package-gates its hooks and targets anonymous finder methods.
Those bindings are **not preserved by a facade**. Supporting the resulting movement behavior instead
requires narrow verified owned provider adapters. Their exact identities, calls and hashes are in
research. The inspected MultiFloors/VMF samples are 1.6 contracts, not 1.7 acceptance.

No protection promise for every occupant, large areas, minified installation, unsupported custom
completion, arbitrary portals or multiplayer. No source save/settings import or general safe-removal
claim. Co-installed source disables owned overlap without removal or foreign patch/config changes.

## Remaining implementation and verification

CT-Q1–4 in the [ledger](../refinement/specification-progress.md) retain policy scope/controls, effective
native completion/recovery, optional consumers and composition/balance/acceptance work. Per-patch
target documents, RED/GREEN, independent implementation review, package checks and personally observed
native build/move/wait/force/reload/provider workflows remain required. Public speed or compatibility
claims and pure geometry tests cannot replace those workflows. No game or build was run for this draft.

The draft has **9 requirements, 36 scenarios and 25 unchecked tasks**. Independent source and
composition reviews found no behavioral issues; one stale S058 scenario count was corrected.
Final typed validation passed **82 items, zero failed**; twelve scoped Markdown documents passed
local-link and whitespace checks. These checks do not establish runtime compatibility.
