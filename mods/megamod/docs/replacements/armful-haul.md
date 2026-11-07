# dd.Armful Haul — selected visible cargo

## Identity and provenance

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
Origin: replacement subset, **S126**. Source **dd.Armful Haul**, `dd.armfulhaul`, Workshop
[3808100512](https://steamcommunity.com/sharedfiles/filedetails/?id=3808100512), author metadata
**dd**, installed version **0.4.2**. [Research](../refinement/2026-11-03-armful-haul-research.md)
records documentation-first inspection, hashes, all 17 source files and selected installed 1.7
bodies. No license file was found; no source code, assembly or art is selected for copying.

Status: **analyzed; selected-subset specification drafted**. Nothing implemented or accepted.
The user requires visible hauled stacks regardless of the hauling implementation selected under
S021 PH-Q5. Broader Armful behavior is reference material, not blanket feature selection.

## Behavior inventory

All selected scenarios live in [visible hauled cargo](../../../../openspec/changes/specify-megamod-visible-haul-cargo/specs/megamod-visible-haul-cargo/spec.md).

| ID | Input/action → observable result | Defaults and boundaries | Status |
| --- | --- | --- | --- |
| AH-01 | Actually collect and move cargo → each held stack contributes to the visible pile | Enabled by default; no planned/personal phantom cargo or duplicate primary; no capacity increase | Specified |
| AH-02 | Select hauler → current summary and individual stack details | Proposed one scrollable contextual list; separate same-looking stacks; mixed quantities labeled | Specified |
| AH-03 | Deliver/transfer/interrupt/reload → presentation follows surviving cargo | Failed drop retains held status; saved drawings never recreate items | Specified |
| AH-04 | Toggle view or change shared setting → same save preference changes | Bottom-right toggle; global/save inheritance; main-menu save controls disabled; information remains when pictures off | Specified |
| AH-05 | Required presentation unavailable → ordinary view and truthful available information | Bounded startup warning, visible reason; one overlapping renderer; no cargo mutation | Specified |
| AH-R1 | Storage/construction/bill/loading batching | Source native wrappers, per-stack limits and recovery are comparison inputs to S021/S125/S133/S135 | Analyzed, broader adoption pending |
| AH-R2 | Explicit prison-food return/sweep | No automatic clearing; not selected by the required visual subset | Analyzed, not adopted |
| AH-R3 | Logical Floors, DSU/ports and Misc. Robots adapters | Exact package/consumer/shape and native acceptance still needed | Analyzed, no compatibility promise |

## Interactions and composed result

S021 owns actual held quantities, personal supplies, aggregate capacity, work and recovery. S126
reads those facts directly; no patches against our own hauling feature and no second Thing cache.
S008 urgency and future S125/S133/S135 workflows retain their own contracts. Future S127 RimHUD and
S132 inventory presentation must compose the same facts without duplicate UI; their analyses remain
queued. Source PUAH incompatibility is documented but not reproduced for every fork; see research.

Intentional differences: no full allowance per extra stack; no global-only source settings; no
translated-string replacement dependency; no unrestricted 0.1–10 spacing slider initially; one
scrollable cargo control instead of one extra gizmo per stack. These choices preserve visible and
individually inspectable goods while respecting real capacity and usable contextual controls.

## UI audit and relocation

| Source entry point | Purpose | Owned destination / change |
| --- | --- | --- |
| Native carried render | See stacks during movement | Native pawn view, adaptive compact pile |
| Extra per-stack selection gizmos | Inspect actual carried goods | Native first-item command retained; one contextual Hauled cargo list provides access to every additional stack |
| Pawn inspect string | List current hand stacks | Additive summary with full contextual details; preserve foreign text |
| DrawAllStacks checkbox | Toggle pictures | Bottom-right operational control and one common default/override row |
| Pile spread slider | Configure visual spacing | Not selected initially; AH-Q2 tracks evidence-based need |
| Hand-stack/group/corpse settings | Change source hauling | Outside S126; broader comparison remains PH-Q5 |
| Debug log setting | Diagnose work | Any owned equivalent belongs in the top developer controls |
| Prison-food right-click entries | Issue ordinary explicit orders | Analyzed only; not added by S126 |

No source bottom tab was found. Actual cargo is entity gameplay state, never an inherited setting.
The one display preference uses Hauling → Cargo display, Global default / Use global / Current
save, default on, immediate application, dormant override retention and main-menu mutation guards.
Bottom-right use writes the same save override; no hidden duplicate toggle state.

## Contracts consumed by other mods or saves

No inbound binary consumer has been proven. Installed source/XML search found only source-owned
Armful identity references; it cannot establish absence of DLL consumers. Research records outbound
Logical Floors/DSU/robot contracts and source `ddArmfulTrip`/global settings. We neither emulate
the source assembly/package nor import its jobs/settings. Any future actual consumer preservation
or migration requires a separately evidenced adapter/contract, including recovery on failure.

Known active source Armful wins overlapping presentation: withhold owned drawing/inspection with
an explanation, without altering source patches. S021 governs overlapping collection and retained
owned cargo recovery separately. Other compatible patches are allowed; unknown provider support
requires package/shape admission and native proof, not name-only heuristics. Harmony is a required
future dependency; no source mod or other repository product is required. XML Extensions becomes
required if later guarded declarative mutations use it; none is selected here.

## Implementation and acceptance

Implementation: **none**, including no semantic guard code, tests or game-facing hook. Target-spec
candidate seams and test tiers are in the [design](../../../../openspec/changes/specify-megamod-visible-haul-cargo/design.md);
all [tasks](../../../../openspec/changes/specify-megamod-visible-haul-cargo/tasks.md) remain unchecked.
Independent document review found no issues; strict S126 validation and typed repository OpenSpec
validation passed. The draft contains seven requirements and thirty observable scenarios.
AH-Q1–4 retain integration, native layout/provider and final acceptance work. Source hashes and
static analysis are evidence provenance, not compatibility predicates or gameplay proof.

Future acceptance must personally observe actual native pickup/movement/delivery and lifecycle
changes on the reviewed build, inspect individual cargo, exercise settings from main menu/save,
and include supported/incompatible providers. Live visual review at different zooms and a final
balance/playability pass are mandatory. This presentation grants no resources, hauling permission,
capacity or faster work. No runtime verification or performance improvement is claimed by prose.
