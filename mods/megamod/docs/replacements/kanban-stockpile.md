# KanbanStockpile Continued replacement

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
Origin: replacement, **S049 / P1-43**, plus confirmed custom storage refill rules.
Status: **source/native analysis and observable draft independently reviewed and strictly validated**.
Nothing implemented or accepted in game.

## Provenance and owning contract

**KanbanStockpile Continued**, endag, `endag.kanbanstockpile`,
[Workshop3767756434](https://steamcommunity.com/sharedfiles/filedetails/?id=3767756434), supports1.6,
Manifest1.2.1, selected DLL1.0.9716.36123. Published
[source](https://github.com/Endag/RimWorld-KanbanStockpile) has MIT attribution to ubergarm.
The [research record](../refinement/2026-11-03-kanban-stockpile-research.md) pins source commit and
hashes, reads every behavior type, distinguishes stale multiplayer/debug claims, records native1.7
integration problems and bounds the consumer scan. No source binaries/code/art copied into product.

[Proposal](../../../../openspec/changes/specify-megamod-storage-refill/proposal.md),
[design](../../../../openspec/changes/specify-megamod-storage-refill/design.md),
[spec](../../../../openspec/changes/specify-megamod-storage-refill/specs/megamod-storage-refill/spec.md),
[tasks](../../../../openspec/changes/specify-megamod-storage-refill/tasks.md).

## Behavior coverage

| Original feature | Owned observable result | Difference / limitation | Status |
| --- | --- | --- | --- |
| Stack refill percentage | Per-type X/Y cycle using group totals; strictly below X starts, actual Y stops; incoming stock separate | Replaces cell-local percentage with required group hysteresis. No percentage mode or automatic source import | KS-01–03 drafted |
| Similar-stack-count limit | Off or1..8 actual stacks of each type across the group | Retained but adapted; accepted variants share type, oversized stacks count once; no eviction | KS-05 drafted |
| Maximum stack-size control | One storage/group slider, Unlimited initially, caps further automatic stack filling | No physical stack-size change, split or per-item slider; native maxima always apply | KS-04 drafted |
| Reserved-stack checks | Unique real delivery quantities and positions coordinated across haulers | Correctness mandatory, no performance opt-out; full source stack and duplicate reservation counts rejected | KS-06 drafted |
| Material similarity setting | Accepted variants of a type share quantity/count targets; native merging unchanged | No global source toggle; entity filters determine relevant stock | KS-02/05 drafted |
| PUAH fallback setting | Direct shared-demand input to owned batch/opportunity hauling | No self-patch or universal CapacityAt=0; newer selected hauling references govern owned capacity | KS-06/07 drafted |
| Save and copy/paste | Complete S005 policy copy, separate cycle state, stable identity, rebuilt claims | No owner-string keys/stack-trace copy heuristic; no copied in-flight claims | KS-08 drafted |
| Optional storage support | Shape-admitted native/provider contexts; supported items use actual restrictions | Source compatibility names alone insufficient; unknown contexts visibly unavailable | KS-09 drafted |
| Multiplayer | None selected | Continuation explicitly drops it; old README claims not adopted | Deliberately unclaimed |

Every implemented portion is **none**. Target documents, code, honest-tier tests, performance data
and native evidence remain future tasks, not implied by the draft table.

## UI audit and configuration

The source adds two sliders and one checkbox/numeric control in the existing storage tab, plus a
mod-settings page with three booleans; it adds no bottom tab. The owned version uses one structured
section on the native contextual storage surface: per-type search and quantities, incoming/actual
stock, status, one stack-fill slider and an optional stack-count limit. Linked members show the
same policy. Impossible item types are not offered; changed/missing rows retain recoverable data.
These are storage gameplay state, not global/save mod settings. Source booleans become explicit
mandatory policy/direct composition rather than empty replacement settings rows. Any later actual
configuration follows the foundation's common global/save mechanism. Seven-language workflow and
layout still require native acceptance.

## Direct interactions and contracts

- S005 owns linkage and full policy lifecycle; this extends it without a second grouping system.
  Future S050 filter-only presets must preserve refill fields they do not own. Copy/paste of complete storage
  policy includes refill choices and initializes destination cycle from its own stock.
- S021 owns capacity, batch quantities and lossless recovery, S125 opportunities compose directly,
  S126 displays actual cargo. S008 urgent hauling respects automatic refill; transport/caravan loading
  and consumer ingredient jobs remain separate.
- S033/Ctrl+F and existing hauling share any justified derived membership data; native listers first,
  current policy/eligibility rechecked at use, no per-port cache or unmeasured optimization claim.
- Production output, explicit directed deliveries, S027 trade/gift drops and safe recovery can exceed
  refill limits, preserving physical capacity and stock validity. Refill does not add an eviction loop.
- Core and Harmony are required when implemented. Optional providers need actual package/shape checks.
  No XML patch or XML Extensions dependency is currently proposed. Original `endag.kanbanstockpile`
  suppresses owned integration, without foreign unpatching or promised source repairs.

No inbound binary consumer was found in the bounded installed scan. Source public helpers/struct,
Harmony owner, package, three save fields and three global keys are recorded in research; no assembly
facade or package spoofing is selected. The two policy models differ, so save/settings migration is
not automatic. A future explicit importer needs its own backup, atomicity and recovery contract.

## Verification and remaining work

KS-Q1–4 track effective targets, native/provider data and slots, UI/lifecycle and actual acceptance.
The semantic pass preserves resource mass, physical stack maxima, worker carrying allowance,
ordinary labor and priorities. It deliberately allows a low configured limit to prevent reaching Y,
with visible explanation rather than invented capacity. Source behavior and static hazards were
inspected; no source bug was live-reproduced and no compatibility/performance claim is accepted.
Independent review resolved one surviving-group cycle ambiguity; focused re-review found no issues.
The draft has **9 requirements, 39 scenarios and 24 unchecked implementation tasks**. Strict
validation and scoped link/whitespace checks passed; this establishes no runtime acceptance.
