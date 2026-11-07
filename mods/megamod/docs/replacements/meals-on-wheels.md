# Meals On Wheels - Continued

## Identity and provenance

Owner **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`; target1.7 beta.
Origin replacement S055/P1-48. `Memegoddess.MealsOnWheels`,
[Workshop3538082807](https://steamcommunity.com/sharedfiles/filedetails/?id=3538082807), Uuugggg /
Meme Goddess continuation. Inspected2026-11-03, selected1.7 assembly/file1.0.0.0 SHA256
`44BE131F7D879C1BA1B4574575D5EFFE334E4D46D333137C78EC821ADF335BD9`.
Source revision [6c39427568029f62562c495d8e65a1a92e0f3f99](https://github.com/MemeGoddess/RimWorld-MealsOnWheels/tree/6c39427568029f62562c495d8e65a1a92e0f3f99),
MIT copyright2025 Alex Tearse-Doyle; retain notices for later reuse. No source code/assets shipped.
[Research](../refinement/2026-11-03-meals-on-wheels-research.md) records all five source files,
installed/repository binary differences, current native food/shuttle paths and actual consumer.

Status: analyzed and observable draft independently reviewed/strictly validated. Nothing implemented.
[Proposal](../../../../openspec/changes/specify-megamod-shared-carried-food/proposal.md),
[design](../../../../openspec/changes/specify-megamod-shared-carried-food/design.md),
[spec](../../../../openspec/changes/specify-megamod-shared-carried-food/specs/megamod-shared-carried-food/spec.md),
[unchecked tasks](../../../../openspec/changes/specify-megamod-shared-carried-food/tasks.md).

## Behavior inventory

All rows are specified, not implemented or runtime verified.

| ID | Action → intended result | Defaults / boundaries | Requirement |
| --- | --- | --- | --- |
| MW01 | Ordinary food search fails → borrow eligible carried food | Native success/animal access preserved; free-colonist self-feeding on same map | MW01 in linked spec |
| MW02 | Consider food → eater/action permission retained | Diet, ingredients, title, freshness, corpse/drug/minimum preference, positive nutrition | MW02 |
| MW03 | Compare donors → meaningful food/distance choice | Fresh non-drug awful-meal+ with<12hours to rot first; all eligible contents, baby-feeding exclusion | MW03 |
| MW04 | Retrieve and eat → actual portion moves and is consumed | Read-only search, normal meal quantity, shared capacity, actual result and food history | MW04 |
| MW05 | No other source → optionally retrieve idle shuttle food | Odyssey passenger shuttle only, interaction access, no loading/ready/departure cargo or plan cancellation | MW05 |
| MW06 | Borrow hauled/kept food → one consistent ownership result | Cargo-first mixed attribution, current-use protection, no internal patches | MW06 |
| MW07 | Move/cancel/load/compete → safe recovery | Current holder/quantity/permissions, no same-tick retry flood or phantom food | MW07 |
| MW08 | Changed/source-coinstalled contract → truthful native fallback | Atomic holder group, bounded warning, quiet missing optional content, no debug spam | MW08 |

## Interactions and composed result

S021 provides one capacity and cargo/kept quantity owner; S024 provides the optional meal preference
score and interruption recovery; S041 carries food-poisoning history through the actual portion;
S033 supplies shared candidate facts where needed. Eligibility remains specific to the eater/action.
S123 meal carrying must preserve current-use food; S133/S135 loading must share transport claims.
S057 preserved-food preference remains to be reconciled during its own sequential analysis.

The source's reported PUAH failure is a research lead, not a claim about every current fork. Our
contract prevents phantom cargo, unsafe held-source results and repeated take/unload loops directly.
General keep protects against unloading, while ordinary food sharing still works unless an actual
provider/current-use restriction applies. Native costs, nutrition, ingestion and spoilage stay intact.

## UI audit and relocation

| Source entry point | Purpose | Megamod destination / deviation |
| --- | --- | --- |
| Automatic food search and native take/ingest jobs | Meet hunger using carried food | Native job/food workflow, actual transfers and normal ingestion |
| Settings class sample checkbox | Unused scaffolding; not wired to Mod | Omitted, no artificial settings page or scope exception |
| Odyssey shuttle search side effect | Makes held food a possible source | Owned real retrieval at native interaction point, not query-time unloading |
| Bottom bar | No source tab | None added; diet and inventory state remain native contextual gameplay controls |
| DEBUG logging/global Harmony.DEBUG | Development diagnostics in repository build | Not copied into normal play; bounded admission warnings only |

No real new mod setting is selected. The documented12hour rule must be a named constant if
implemented. Future configuration follows the common global/save model and protected main-menu
callbacks. Pawn food policies, current-use portions and kept quantities are gameplay state.

## Contracts consumed by other mods or saves

| Consumer / evidence | Binding | Preserve / unsupported boundary |
| --- | --- | --- |
| Installed Immersive Chefs1.6, source and compiled adapter | Canonical Continued package, Meals_On_Wheels assembly, FoodGrabbing.Postfix shape, source owner/priority on FoodUtility | Unchanged binding unsupported; namespace facade insufficient; no repository-mod dependency/spoof or whole-source drop-in claim |
| RimPy and Use This Instead | Package catalog and original→Continued replacement mapping | Discovery metadata only; no callable facade requirement |
| Source Settings.setting | Dead sample key, no live source settings initialization | Omit; no automatic import or foreign-file edit |
| Native inventories/TakeFromOtherInventory | Actual held Thing, transferred count and later normal eating | Preserve real workflow and quantities with safe owned guards; custom shuttle job uses owned persistence |

The successful bounded installed scan found no further executable consumer, not universal absence.
There is no source custom saved job/item identity to impersonate. Any future explicit migration needs
backup, atomicity, failure restoration and verified postconditions. Consumer changes in another
repository mod need their own owner and change; this draft changes none.

## Implementation and coverage

**Nothing implemented.** All MW01–08, semantic target documents, real shuttle job, language coverage,
honest RED/GREEN and final native acceptance remain pending. Deliberate deviations include request-
correct permissions, self-feeding scope, all-stack inspection/ranking, positive nutrition, complete
capacity/claims/recovery and query-side-effect removal. No medicine/water/caregiver/world-caravan or
generic-container expansion. Idle shuttle behavior is conditional on an effective safe contract and
a naturally reachable player workflow; loaded/ready cargo remains protected.

Original `Memegoddess.MealsOnWheels` or `Uuugggg.MealsOnWheels` suppresses the overlapping owned group
with a warning. Changed holder lifecycle disables discovery and transfer together. Harmony required
with implementation; Odyssey/content and external providers optional. No XML planned; actual future
XML use needs XML Extensions dependency, package/content/node guards and merge preservation. Never
ship upstream assemblies or depend on another product mod/Dev Gateway.

## Verification and acceptance

Independent review corrected MW08's technical wording, retaining those obligations in design;
focused re-review found no issues. Strict validation passed77items, zero failed. Eight scoped
documents passed local links/whitespace. The draft has8requirements,39scenarios and24unchecked tasks;
the [ledger](../refinement/specification-progress.md) tracks the current state.
All runtime tasks remain unchecked. MW-Q1–4 retain effective admission, provider/shuttle/consumer
proof, finalized food ranking and future composition, measurement and actual native acceptance.
Final evidence must show a hungry colonist actually approaching, transferring and eating, with
diet refusal, contention/interruption, cargo/taint/capacity, save lifecycle and exact Odyssey-present
shuttle workflows. Search success or transfer alone does not prove a meal was eaten. No game/build/
install or normal user configuration mutation occurred in this documentation slice.
