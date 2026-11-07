# Everybody Gets One - Continued replacement coverage

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`). Target: **1.7 beta**.
Queue **S018**, intake **P1-14**, origin **replacement**. **Analyzed; specification drafted.
Nothing implemented or accepted in game.**

Source **Everybody Gets One - Continued**, Uuugggg / Meme Goddess, package
**Memegoddess.EverybodyGetsOne**, Workshop **3530806680**.
[Author description](https://steamcommunity.com/sharedfiles/filedetails/?id=3530806680),
[change notes](https://steamcommunity.com/sharedfiles/filedetails/changelog/3530806680),
[public source](https://github.com/MemeGoddess/RimWorld-EverybodyGetsOne).
Installed 1.7 assembly/file **1.0.0.0**, declared support **1.1–1.7**; MIT source provenance.
Installed decompilation governs where the pinned public binary differs. No upstream code/assemblies
are packaged by this documentation work.

[Exact inputs and research](../refinement/2026-11-02-everybody-gets-one-research.md) support
[proposal/design/tasks](../../../../openspec/changes/specify-megamod-adaptive-production/),
[population production](../../../../openspec/changes/specify-megamod-adaptive-production/specs/megamod-population-production/spec.md)
and [surplus production](../../../../openspec/changes/specify-megamod-adaptive-production/specs/megamod-surplus-production/spec.md).

## Behavior coverage and intentional changes

| Behavior | Inspected source | Proposed replacement / status |
| --- | --- | --- |
| Population plus offset / quantity per person | Query result plus targetCount or multiplied by it; same transform for resume | **Specified:** explicit operands/effective totals, zero target floor, coherent hysteresis and checked finite arithmetic |
| Mode selection/defaults | CanCountProducts required; existing numeric fields retained, native fresh 10/5 | **Specified:** retained valid operands with units/preview; negative-to-nonnegative switch requires explicit replacement |
| Overview/details | One-unit custom step; overview permits negative offset, Details depends on fragile field rewrites and native zero minimum | **Specified correction:** same signed rules in both views; actual finite derived totals, no misleading Infinite label |
| Default people | Spawned IsColonist excluding slaves/lodgers, no age or prisoner-status filter | **Specified:** babies/children/downed and arrested player colonists can count; foreign prisoners, animals/mechs/caravans do not count by default |
| Editable population | TD Find Lib ordinary listing/filter tree and map controls; result is sum of stackCount | **Specified:** real editable selection, non-pawn physical units, preview, inspected predicate families and clear matching unit |
| Wrapped things and range limits | Conditions test outer or corpse/minified inner before inversion; source range end stops can be unbounded | **Specified:** one represented unit, working Dead/pawn properties on corpses, explicit open bounds and provisional condition defaults |
| Full query catalogue / saved-library organization | Shared dependency exposes many more predicates and library workflows | Separately researched/reviewed [S121 draft](td-find-lib.md); runtime implementation/codecs remain pending and concrete unsupported imports fail as a whole |
| Scope / missing filters / failures | Current-map predicates can follow camera; unresolved clauses disabled; rebuild may return partial results | **Specified corrections:** stable declared scope and unavailable whole-query result; no silent broadening or zero fallback |
| Editor cancel | Source revert imports listing/children but retains name/map | **Specified correction:** detached complete draft, Apply all or discard all |
| Query freshness | Once per game tick except query dirty flag | **Specified:** next relevant evaluation including paused changes, shared candidate/index path |
| Include equipped | Query-matched pawns across maps, but only if all three trackers exist | **Specified composition:** S017 unique stock with explicit query carriers; inspect each existing holding independently |
| BWM support | Main DLL targets existing DrawFilters for person modes; older MirrorBills bridge copies searches | **Direct composition:** S014 whole list and S017 stock/copy rules; no internal compatibility patch stack |
| Clipboard / save | Bill-reference QuerySearch dictionary, inactive clone, paste retargeted to destination map; old map-comp migration | **Specified owned snapshots:** relative Bill map retargets, explicit map IDs preserved/previewed; no automatic foreign save migration |
| Surplus eligibility | Exactly one ingredient entry; ResourceCounter sum, native fixed-ingredient bypass | **Specified narrowing:** all currently eligible alternatives resource-countable; preserve fixed ingredients regardless of hidden dormant filters, intersect editable alternative restrictions |
| Surplus meaning | Strict stock > threshold; full batches can consume below it | **Specified:** physical resource units and trigger-not-reserve explanation; unchanged recipe work/cost/yield |
| Physical continuation | Bound UFT special case; no complete autonomous collection guarantee | **Specified correction:** committed work and ready output remain collectable through supported native paths |
| Legacy generated-method patch | Static constructor heuristic present, invocation unproven; native 1.7 branch absent | **Not ported blindly:** verify actual modern start/continuation semantics instead |
| Source settings | Unused sample checkbox, actual settings hookup commented out | **Omitted as inactive source code:** no invented preference |

These refinements are explicit proposals, not statements that the user separately confirmed every
numeric/UX choice or that source code inspection proved a live bug. No full EGO replacement claim
is made while library breadth, implementation and final workflows remain open.

## UI, settings and balance

Use the existing bill overview/details and a contextual matching editor. Mode, target operand,
resume operand, query, map choice and threshold are **bill gameplay state**, shared across linked
logical rows; they are not mod settings. No new bottom tab or bottom-right toggle. Debug raw-object
inspection is outside this ordinary editor. Shared-library configuration, if introduced under S121,
must use the common global/save settings framework and main-menu guards.

Built-in query is Bill map, spawned colonists excluding slaves/lodgers; mode switch retains valid
existing operands. Source fresh 10/5 and base step one are explicit. Effective targets floor at zero;
effective resume is clamped below a positive target. All strings/help/errors need seven-language
implementation. Costs, yields, batch size, research and skill gates are unchanged. Counting selected
people or remote equipment can change when replacement work occurs; surplus does not protect a
minimum remaining reserve. Native work/collection and actual batch outcomes need final observation.

## External contracts and dependencies

| Contract | Decision / limit |
| --- | --- |
| TD_PersonCount, TD_XPerPerson, TD_WithSurplusIng saved repeat Defs | Recorded, **not automatically adopted**; own identities avoid claiming recovery of source query payloads that were not migrated |
| World billPersonCounters and older PersonCountMapComp | Source save lifecycle recorded; owned persistence only, no binary/schema migration promise |
| TD Find Lib ISearchReceiver / QuerySearch.CloneArgs / SearchTransfer | Real optional exchange contract; isolated canonical-package/shape adapter with whole-query translation and editor lifetime, no shipped library DLL |
| TD.EGO transfer Source identity | Recorded, not impersonated; distinctly owned receiver avoids silently representing source EGO |
| BWM DrawFilters / MirrorBills | Actual modifier relationship composed directly with S014/S017; current source co-install interoperability not established |
| Community ordering, Use This Instead and saved mod lists | Metadata consumers only; no callable facade or package spoof justified |
| Other uninstalled/dynamic consumers | Bounded literal scan does not prove absence; no universal drop-in claim |

Required future runtime dependency: **Harmony**. Source EGO and TD Find Lib are not required for owned
production/query behavior. **Memegoddess.TDFindLib** is optional exchange; its own dependencies remain
its responsibility. Source EGO continued/original active withholds our overlap, with recoverable owned
saved state and startup warning. Three owned additive repeat Defs need no XML mutation; if later XML
patches are selected, XML Extensions becomes required with per-patch existence/content checks and
merge-preserving operations. No product reference to or shipped Dev Gateway.

## Implementation and verification

**Nothing implemented.** EG-T1–8 in the design are candidate patch groups, not executable validators.
EG-Q1 retains numeric/UX validation; EG-Q2 shared query/library breadth under S121; EG-Q3 exact beta
counter/holder/provider and lifecycle proof; EG-Q4 guard/test/language/review/native acceptance work.
All implementation tasks remain unchecked. Document validation and independent review are recorded
in the [sequential ledger](../refinement/specification-progress.md).

Future acceptance requires actual query editing/cancel, population transitions, ordinary production
and consumption, shared linked bills, copying/reload/recovery, and real surplus/UFT/autonomous
collection. Optional exchange needs an exact provider-present run. Observe native actions and causal
results on the reviewed build; startup logs, query diagnostics or prepared end states are insufficient.
