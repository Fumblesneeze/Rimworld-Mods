# Interaction Spot On X

## Identity and provenance

Owner **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), target **1.7 beta**.
Origin replacement **S129**, source Dragonissa's **Interaction Spot On X**,
`dragonissa.InteractionSpotOnX`, [Workshop3827781564](https://steamcommunity.com/sharedfiles/filedetails/?id=3827781564).
Installed1.0.0.0 root assembly SHA256 `8848b5add25c04a34e2d1337fd5da0755e6a82d4945db179ba557249d62fe144`;
public source commit `73f1f6ccc1d83a6f3951c5944033347b8cb75ad9`, no license file found. Source/asset/DLL
redistribution is not selected. [Research](../refinement/2026-11-06-interaction-spot-on-x-research.md)
records all source paths, native1.7 input and source/compiled revision distinction.

## Behavior inventory

| ID | Input → observable result | Default / scope / evidence | Status |
| --- | --- | --- | --- |
| ISX-01 | Plan with only a qualifying chunk at its interaction position → permitted | Chunks on; native chunk classification, including qualifying added content | Specified only |
| ISX-02 | Enable trees then plan over one → permitted | Trees off by default; actual tree classification, not every plant | Specified only |
| ISX-03 | Rotate/use multiple positions, mixed blockers or planned obstruction → current complete eligibility | All positions and unrelated restrictions retained; native geometry | Specified only |
| ISX-04 | Edge/provider/terrain rejection or ignored moved object → preserve correct native rule | Correct broad upstream postfix rather than erase rejection | Specified only |
| ISX-05 | Plan then clear/use → no new obstacle action; ordinary work requirements | Source has no clearing/job changes; S063/S060 retain authority | Specified only |
| ISX-06 | Configure/diagnose → common scoped options and temporary top debug control | Chunks=true, trees=false, diagnostic=false; seven languages | Specified only |
| ISX-07 | Failed admission or original co-installation → warn and retain existing behavior | Semantic guard/owned-only rollback; no source patch removal | Specified only |

[Observable contract](../../../../openspec/changes/specify-megamod-interaction-spot-placement/specs/megamod-interaction-spot-placement/spec.md)
and [proposal/design/tasks](../../../../openspec/changes/specify-megamod-interaction-spot-placement/)
describe future work. No feature or compatibility claim is implemented/accepted.

## Interactions and composed result

Direct S036 Architect, S048 final rotation, S028 replacement/planning/ignored-object semantics,
S060 actual construction access and S063 ordinary footprint plant clearing. No internal patch stack,
additional tree orders, changed costs or passability. Native local lists suffice; no new map cache.
Planning can be possible before actual work; ordinary clearing/reachability still applies.

## UI audit and relocation

| Source surface | Purpose | Owned placement / intentional change |
| --- | --- | --- |
| Global chunks/tree checkboxes | Behavior configuration | Construction → Interaction positions common global/save rows, in that order |
| Persistent debug checkbox in settings | Diagnostic logs | Temporary developer-only top toolbar, initially off; bounded/null-safe |
| Native Architect preview/confirmation | Plan building or destination | Existing surface only; preserve final rotation and native actions |

No new bottom tab or bottom-right toggle. Use global defaults to on for new save entries; per-save
editors are inactive while inheriting, and all save controls/callbacks are disabled at main menu.
Checking inheritance retains the dormant override. Changes affect next preview/final check and
reload/save switching correctly. Existing plans are not deleted. No foreign settings import or
extra entity save data. All owned labels/tooltips require seven languages.

## Contracts consumed by other mods or saves

| Observed contract | Selected treatment | Verification still required |
| --- | --- | --- |
| Native interaction positions, ignored Thing and obstacle categories | Preserve geometry/exclusion; narrow selected obstacle relaxation | Exact effective1.7 caller/hook and native action proof |
| Source public settings, types and Harmony owner | No external consumer found in bounded installed DLL scan; no speculative facade | Any actual future consumer needs binding evidence |
| Source global keys placeOnChunk/placeOnTree/debugMode | Independent owned common settings, no automatic import | Main menu, reload, save isolation and inactivity explanation |
| Added chunk/tree content or placement restrictions | Native content classification plus retained provider constraints | Exact provider content/shape and actual combined workflow |

Original `dragonissa.InteractionSpotOnX` remains authoritative when co-installed; owned overlapping
patches are withheld and explained with a startup warning. It is optional, not a product dependency.
Required future Harmony hooks need minimal executable target admission, compatible-owner tolerance,
atomic owned-only rollback and bounded warnings. No XML mutation or additional XML dependency selected.

## Implementation, deviations and acceptance

**Nothing implemented.** Intentional corrections: preserve bounds, ignored-self and unrelated
rejections; do not overwrite a final report solely from rescanning some Things; safe bounded debug
logs; common scoped preferences; no unconditional PatchAll. Multiple interaction positions remain
all-required, and no arbitrary item/plant exception is added.

All seven groups await focused RED/GREEN, target documents, reviewed build/package, seven-language
UI and personally observed minimized native preview/placement/clear/work evidence. Separate exact
provider/co-installation runs are needed for compatibility claims. Retain build/mod/config/cleanup
provenance; documentation validation cannot accept reachability or final gameplay balance.
