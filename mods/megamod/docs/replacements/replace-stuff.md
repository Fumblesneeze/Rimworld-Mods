# Replace Stuff — Continued

## Identity and provenance

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), target **1.7 beta**.
Origin: replacement **S028 / P1-22**. Source `Memegoddess.ReplaceStuff`, Workshop **3526354009**,
Uuugggg / continued by Meme Goddess. Inspected 2026-11-02. Documentation first:
[Steam](https://steamcommunity.com/sharedfiles/filedetails/?id=3526354009),
[changelog](https://steamcommunity.com/sharedfiles/filedetails/changelog/3526354009),
[source](https://github.com/MemeGoddess/RimWorld-ReplaceStuff).

[Research](../refinement/2026-11-02-replace-stuff-research.md) pins source/native/consumer evidence,
all 52 source files, 28 current Def files and inherited patches. Installed Release DLL SHA-256
`18DC66FD3721C5BB879304EA919E8BA9B43E0060F0DF404E0FF6A7DF2A1221A3`, informational
`1.7+11e33b712e62cc1820b5196ee5581db522bc69a3`; public checkout has matching normalized source
but a different committed Debug DLL. No binary parity/reproducibility claim. MIT-style license
copyright [2025] [Alex Tearse-Doyle]; retain notices for substantial licensed reuse. Nothing from
the source is copied into the product package. The [117-list catalog](../refinement/2026-11-02-replace-stuff-catalog.md)
is an exact evidence inventory, including disabled/broken groups, not a support promise.

## Observable coverage

All rows: **analyzed and specified with retained gaps; not implemented**.

| Scope | Player action → proposed result | Important limits |
| --- | --- | --- |
| Material replacement | Choose material/drag or rebuild same definition over itself → intact building changes material after work | Stuff-only new cost/old returns; fixed components retained; full HP and builder quality; actual configuration survives |
| Retarget/failure/cancel | Change, fail or cancel order → actual resources recover, old building remains | Only earned removal work carries to new material; no duplicate return, free reroll or same-cell identity guess |
| Upgrade/install | Build/install supported different object over one old building → old stays until valid completion | Full new costs or actual installed item; ordinary deconstruct returns; multi-old footprint rejected |
| State and use | Upgrade/rebuild → declared bills/storage/temperature/crop/bed choices transfer | Different transfer scope for upgrade versus rebuild; owner/contents/provider loss refuses unsafe upgrade; no free fuel/charge |
| Automatic rebuilding | Native destruction-created rebuild → supported remembered choices restored | Native home/research/destruction eligibility; exact order survives failure/load, cancellation abandons memory |
| Rock/fog planning | Plan over rock/unexplored cells → ordinary prerequisites, no hidden-terrain reveal | Mining capability/yield, suitable smoothing, correct-map/full-footprint reveal checks |
| Support terrain | Place unsupported building → legal researched bridge plans/cost preview | Whole cost, existing support reuse, safe dependent cancellation; no arbitrary soil/substructure conversion |
| Construction access | Valid corner work/passer-by handling → physical construction without accidental blockers | No wall traversal, trapped/deleted pawn, recursive validator or blanket unrelated-work suppression |
| Wall devices | Build ordinary/wide cooler/vent → retained wall and native thermal function | Same counterpart cost/output, correct ports, native power/research/blocked-side limits |

Owned artifacts: [proposal](../../../../openspec/changes/specify-megamod-building-replacement/proposal.md),
[design](../../../../openspec/changes/specify-megamod-building-replacement/design.md),
[tasks](../../../../openspec/changes/specify-megamod-building-replacement/tasks.md),
[replacement spec](../../../../openspec/changes/specify-megamod-building-replacement/specs/megamod-building-replacement/spec.md),
[planning spec](../../../../openspec/changes/specify-megamod-building-replacement/specs/megamod-construction-planning/spec.md),
[device spec](../../../../openspec/changes/specify-megamod-building-replacement/specs/megamod-wall-temperature-devices/spec.md).

## UI, settings and composition

| Source surface | Owned placement / state |
| --- | --- |
| Structure Replace Stuff, build/install tools, contextual inspection | Same gameplay surfaces; no new bottom tab |
| Bottom-right over-rock/fog toggle | Same ordinary operational control, saved per game; default configured through common settings |
| Hide ordinary/wall coolers/vents, bridge reorder | Structured common settings, global defaults plus Use global/save override; hide flags false, over-rock default true; main-menu save controls disabled |
| Temperature, ownership, storage/bills, construction orders | Entity state in native contextual UI, not settings |
| God-mode instant change | Native top-debug access only; no ordinary-play cheat control |
| All-dropdown ordering and broad unpacking | Not retained; only owned affected offers change, third-party organization remains |

S005 explicit storage identity and capacity, S013 finite quality budget, S014/S017/S018/S019/S026
whole-list bill/production policies and S021/S024 shared physical facts compose directly. No patches
between owned features. S049 entity limits and later construction sources must reconcile during
their own research turns; they are not presumed implemented. Seven-language UI remains future work.

## Consumed contracts and limits

Actual consumers include NANAME's reflected bridge-list read/extension with generated blended terrain,
Poured Concrete's material-change callback,
VGE's predicate/bridge exclusions, embedded Material Sub-Menu and Basic Dropdowns' source sanity
method, historical Architect cost methods, Wall Light/Research Reinvented ordering, and QualityBuilder.
Research gives exact package gates, versions and member shapes. Package/display-name gates do not
activate against megamod merely because names are retained. Source `MaterialsNeeded` mismatch and
historical 1.4/1.6 evidence are explicit; no blanket 1.7 or drop-in claim. Optional adapters are
package/Def/shape gated. No fake source package or Harmony owner and no foreign unpatching.

Original active: source owns new overlapping offers/orders; retain owned recovery and unique device
identities. Original source frames, generated Def names, destroyed-building records and settings are
not automatically imported. Legacy loading, safe mod removal and arbitrary state callbacks are not
promised. Harmony becomes required with use; XML Extensions becomes required with guarded XML.
DLC/catalog providers remain optional. Upstream incompatibility declarations are evidence, not a
blanket ban on unrelated mods modifying the same native methods.

## Deviations and outstanding work

**Implemented: nothing.** Proposed corrections include native removal minimum20, unambiguous old
targets, exact-order recovery, safe occupied beds/contents, deduplicated preview, whole bridge costs,
correct-map fog rechecks, safe bounded corner drops and order-scoped construction relaxation.
Wide-device same-cost/output and freestanding placement are retained source choices, recorded for
balance review. No automatic arbitrary fuel/charge/provider-state preservation. Disabled historical
catalog entries, questionable SoS2/RimEffect groups and missing transfer callbacks require separate
admission evidence; they are not silently carried forward. RS-Q1–5 retain the outstanding work.

Review clarifies existing S013/S014 composition: ordinary destruction-created rebuilding gets a fresh
finite quality run, while material change/retry/reload retains the current budget; material completion
rolls once. Structural recipe loss blocks new work group-wide. Installing an already-configured item
keeps its actual HP/quality/state and refuses conflicting groups. Fogged support needs/costs remain
unknown until reveal; they cannot leak terrain through a bridge preview.

## Verification

Progress/document review and strict validation are recorded in the
[sequential ledger](../refinement/specification-progress.md). Every implementation task remains
unchecked. Acceptance needs actual designation/build/install/cancel/rebuild, jobs, material returns,
UI/state persistence and thermal workflows on the reviewed build, including exact optional-provider
combinations. Asset acceptance is separate and mandatory. No runtime, save migration, compatibility,
balance or performance acceptance follows from this document.
