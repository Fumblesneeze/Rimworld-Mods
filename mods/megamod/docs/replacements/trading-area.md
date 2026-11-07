# Original: Dedicated trading area

## Identity and provenance

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
**Origin: original**, S040/P1-31. Source mod/Workshop/version/license: **not applicable**.
The user requested a dedicated fixed area with its own gizmos, drawn through area tools, not a pawn
allowed area. Fixed does not imply immutable dimensions. S039's separately analyzed Trading Spot
supplies the gathering reference; it does not supply this new area contract.

[Native research](../refinement/2026-11-03-trading-area-research.md) identifies inspected beta
assembly/XML hashes, exact types and partial UI inspection boundaries. It separates confirmed intent,
native facts and proposed defaults. No source code, assets or binaries are copied.

Owning [proposal](../../../../openspec/changes/specify-megamod-trading-area/proposal.md),
[design](../../../../openspec/changes/specify-megamod-trading-area/design.md),
[observable spec](../../../../openspec/changes/specify-megamod-trading-area/specs/megamod-trading-area/spec.md)
and [tasks](../../../../openspec/changes/specify-megamod-trading-area/tasks.md).
**Draft only; nothing implemented or accepted.**

## Behavior inventory

| ID | Native action → observable result | Proposed scope/default/edge behavior | Spec requirement |
| --- | --- | --- | --- |
| TA-01 | Open Trading area → current map footprint/context | One area per map, initially empty; no marker/cost/research | Dedicated area; contextual tools |
| TA-02 | Expand/Erase/Clear all → committed cell changes | Discovered in-bounds additions; erase blocked cells; cancelling unfinished input preserves prior footprint | Painting |
| TA-03 | Paint over storage/other areas → independent overlay | No foreign cells/filters/objects/assignments changed; not an allowed-area slot | Painting and area identity |
| TA-04 | Party approaches → physical entry into painted gathering cells | Holes/centroid excluded; retain valid location; native roles/speed/access | Actual painted cells |
| TA-05 | Ordinary idle decision → usable painted destination | Native range retained; paths/followers/needs/delivery/emergencies can leave | Idle-only behavior |
| TA-06 | Narrow/blocked/crowded/disconnected area → valid waiting or native fallback | No invented minimum/capacity, whole-area reservation, unreachable loop or teleport | Safe termination |
| TA-07 | Commit area edit → existing party updates normally | No preview routing, visit reset, gifts or protected-job cancellation | Committed edits |
| TA-08 | Inspect status → honest availability | Empty/designated/per-party unusable/inactive distinguished; no hidden-state leak or spam | Availability view |
| TA-09 | Save/switch maps/world view → isolated footprint and cancelled pending input | Map-specific gameplay state, no global setting or automatic transfer | Persistence/context |

All rows are draft requirements, with runtime code, tests and native acceptance outstanding.

## Interactions and composed result

S039 supplies eligibility, visitor preference, roles, stage ownership, fallback and departure. S040
supplies area cells, editor and ordinary idle membership. Implement them directly as one owned
feature; a patch against our own gathering code is not an integration. If area idle behavior cannot
be safely admitted, do not silently keep point-only routing while claiming the area works.

S027 finite couriers can leave/return under their existing rules. Follow/Escort, combat, needs and
departure retain native precedence. Neither area size nor edit frequency grants cargo, stay time,
prices, movement permissions or exclusive ownership of colony cells. Actual optional Architect
surfaces need package/shape checks; other zones and allowed areas remain independently functional.

## UI audit and relocation

| Entry/action | Classification | Destination and rationale |
| --- | --- | --- |
| Trading area | Map mechanic/entity control | Architect area-tool entry; available even when empty |
| Expand, Erase, Clear all | Entity editing | Selected tool's contextual gizmos with footprint/readout |
| Painted-cell and party availability | Entity information | Same area context; distinguish designation from actual control |
| Include ordinary visitors | Actual preference, owned by S039 | Common mod settings global/save/Use global; no duplicate area checkbox |

No bottom tab, bottom-right global toggle, generic allowed-area picker or hidden marker. Native
Area is not automatically selectable by Selector; design explicitly uses contextual designator
surfaces. Footprint/editing is gameplay state and has no settings inheritance exception to invent.
All UI still needs seven-language native verification. No raster asset has been selected/generated.

## Contracts consumed by other mods or saves

| Consumer | Binding/state | Preservation and limits |
| --- | --- | --- |
| Owned saves | Dedicated map footprint/identity | Round-trip; new/missing state empty; no cross-map/save editor leakage |
| Native/foreign zones and allowed areas | Existing cells, objects, settings and assignments | Independent overlay; never remove/reassign them to host the area |
| S039 routing / S027 delivery | Direct same-owner behavior | Preserve visit/party/receipt state and explicit roles, no internal mod patches |
| Legacy TradingSpot consumers | Old marker Def/package/graphics | Covered by S039; no area facade, package spoof or automatic marker migration |

No external callable consumer has been established for this new feature. Do not invent a public
provider API or source DLL identity. Future opt-in imports need their own backup, atomicity,
restoration and postcondition contract. Safe mod removal or uninspected schema migration is not claimed.

## Implementation and coverage

| Scope | Implemented | Deviations/risks | Missing |
| --- | --- | --- | --- |
| TA-01–09 | Nothing | Original dedicated area; one-area/painting/gizmo defaults are proposals | All runtime behavior/tests/native workflows |
| Data | Nothing | Native map cells/Lords; no per-feature Thing cache | Actual lifecycle/invalidation and measured update cost |
| Integration | Nothing | Atomic S039/S040 route, independent editor where safe | Stable target docs, admission, provider runs |
| Dependencies | No runtime metadata changed | Core required, upstream/DLC optional; Harmony/XML Extensions only when actually used | Exact implementing package checks |

Original PL active: retain/edit cells while S039 overlapping routing is visibly withheld. Unsupported
providers retain their own control; do not turn unrelated patches into universal incompatibility.
Any eventual XML patch uses content/package/shape guards and preserves unrelated category entries.

## Verification and acceptance

TA-Q1–4 track UI defaults, actual idle/native selection, lifecycle/admission and performance/live
evidence. Pure documentation validation does not require a game, but cannot accept any row above.
After focused RED/GREEN and independent review, personally observe real area-tool gestures and
incident-created parties physically enter/idle in the footprint, then edit/block/clear it and observe
deliveries/exits. Include overlap, one cell, map/save switching and actual provider cases. Field
mutation, a changed destination or a screenshot of an empty area is not gameplay proof.

Semantic pass: free area designation creates no physical asset or economy/research change. Travel
distance/exposure can change; native permissions, cargo, costs, visit duration, gifts and emergency
escape remain authoritative. No new numerical radius, work cost or carrying allowance is proposed.
