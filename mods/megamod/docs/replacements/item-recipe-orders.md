# Micro Designations — replacement coverage

## Identity and provenance

Owner **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
Origin replacement, S096/P2-25. Source **Micro Designations**, avilmask/avil,
`avilmask.MicroDesignations` / canonical `avilmask.microdesignations`, Workshop1877972690.
Selected1.6 assembly1.0.0.0; advertises1.0–1.6 only. Public source commit
ff1bdd4f83d1c9e98741f80ba581100de37f4f13 includes a byte-identical selected DLL.
[Research](../refinement/2026-11-05-item-recipe-orders-research.md) records hashes, source/binary/XML
analysis, native1.7 comparisons, historical consumer and unknown reuse license.
**Nothing implemented or accepted; no package/binary drop-in claim.**

## Behavior inventory

All rows are **specified, not implemented** in the
[observable contract](../../../../openspec/changes/specify-megamod-item-recipe-orders/specs/megamod-item-recipe-orders/spec.md).

| ID | Player action → result | Selected correction / boundary | Requirement |
| --- | --- | --- | --- |
| IRO-01 | Select physical items and choose a recipe → one-time pending quantity | Contextual choices, reduce stack quantity, disambiguate operations/keys; no future-item capture | Select a recipe for a particular item |
| IRO-02 | Examine applicable recipes → actual one-unit choices | Full material/hard-filter checks; stack support; no multi-input/batch/medical shortcuts | Recipe choices represent a real single-item operation |
| IRO-03 | Order before a bench/research → visible waiting reason | Retain source native-fallback planning, improve presentation and refresh actual conditions rather than wait for failed job | Planning and work availability remain distinct |
| IRO-04 | Eligible pawn starts → real hauling and work at usable facility | Ordinary skills, priority, access, capacity, nearest eligible bench and refueling | Workers use the ordinary recipe and facility rules |
| IRO-05 | Finish → native recipe products or repair | Actual costs/XP/consequences; no recycled-yield substitution or automatic repeat mending | Preserve the chosen recipe's actual outcome |
| IRO-06 | Change choice or move/split/merge material → exact authorization persists | Direct S022/S023 choice and S021 portions; preserve unrelated orders, explicit map-return resume | One processing choice follows only authorized quantities |
| IRO-07 | Interrupt or unsupported job fails → intact input and understandable recovery | Native unfinished work, persistent suspension, no unchanged pickup/drop loop | Interrupted or failed work has a bounded recovery path |
| IRO-08 | Complete with blocked output → recover only remaining products | No duplicate effect, reroll or recreated input; recovery outlives bench/input | Completion and delivery cannot duplicate a result |
| IRO-09 | Set visibility defaults/save overrides → next contextual refresh | Three false defaults, common inheritance, pending status never hidden, seven locales | Visibility preferences use common settings |
| IRO-10 | Use owned features/providers → coherent isolated behavior | No linked/copied hidden bill, repair-knowledge bypass or duplicate original provider | Compose owned features and isolate unsupported providers |

## UI audit and configuration

| Source surface | Purpose | Owned destination / deliberate change |
| --- | --- | --- |
| Reverse item designation gizmos | Order a specific recipe | Existing selected-item controls, truthful facility/result/quantity and state |
| Cell/drag methods with no found Orders registration | Internal designator support | No exposed source Architect tool claimed or copied solely from these methods |
| Mod settings, three global checkboxes | Hide contextual choices | Production → Item recipe orders; Global default / Current save / Use global |
| Work Tab workgiver lookup | Inherit ordinary work priority | Narrow optional integration, no per-recipe work column |
| Failure and remaining output | Source has no explicit recovery UI | Contextual pending/recovery controls including absent original input or bench |

No new bottom tab or bottom-right boolean control is needed. Item choice, quantity and progress are
gameplay state. All three hide preferences initially false; common dynamic inheritance, dormant
overrides, disabled inheriting editors and main-menu save callback guards apply. Changes refresh
new actions, never hide existing Cancel/Retry. The first label names colony prerequisites, preserving
the source's research/ideology/faction check rather than suggesting research alone. Seven locales:
English, German, Spanish, French,
ChineseSimplified, Russian and Japanese. Asset selection is deferred to the raster/live visual gate.

## Composition and external contracts

| Consumer / evidence | Binding and behavior | Policy / verification still required |
| --- | --- | --- |
| Source XML | RecipeJobDef jobName/workerClass, DefExtention.Ignore, DesignatorHotKey.hotKey; recipeName+Designation | Metadata/reference intent retained; no speculative binary identity facade |
| Simple Recycling2079862064 historical1.3/1.4 adapter | Package gate plus NeolithicRecipeJobDef/SimpleRecipeJobDef types absent from selected source1.6 | Dormant in its1.5 selection; unsupported historical binding, no package spoof to activate it |
| Source save/settings | Detached billSaved and dynamic source Defs; hide_unresearched/hide_empty/hide_inactive | No automatic import or alias; own state uses admission-aware recovery/common settings |
| Work Tab / BetterLoading source probes | Workgiver priority redirect / alternate static-population timing | Canonical provider and semantic proof required; no source type-name-only detection copied |
| Custom mending/recycling worker | RecipeJobDef chooses provider job; callbacks need valid context | Only actual admitted one-item protocols, unchanged work/results, no blanket provider promise |
| Original avilmask.microdesignations active | Competing recipe orders | Withhold S096 new/resumed work, suspend saved inputs, retain owed output; explicit Resume after removal |

S022 recycling and S023 melee destruction remain distinct actions with one shared processing choice.
S054 repair knowledge applies directly to admitted mending. S014/S017 linking/management must never
copy these item orders as general bills. S052/S053 facility activity and S021/S033 physical facts are
direct composition, not an internal patch stack. Unknown conflicting external orders on an actual
target block affected owned processing until supported arbitration; no broad removal of providers.

Core and external Harmony are required on implementation; custom recipe providers and work-priority
integrations are optional. No DLC or foreign assemblies required. Owned Defs remain additive; actual
XML patches need XML Extensions/package/content/shape guards and the required dependency at first
use. No whole-list replacement, independent Thing cache or product dependency on the dev Gateway.

## Implementation, omissions and acceptance

The [change](../../../../openspec/changes/specify-megamod-item-recipe-orders/) contains proposal,
design, capability and unchecked tasks. All IRO rows lack runtime implementation, executable patch
targets, focused RED/GREEN and live evidence. Deliberately omit global null-bill/save overrides,
runtime mutation of every foreign item definition, unsorted per-recipe workgiver creation, removal
of unrelated designations, stale-selection caches and unchanged failed-job loops. Multi-input and
batch recipes keep their existing bill route; source migration and unverified historical APIs remain
unsupported. This is not a promise of arbitrary modded recipe support.

MDR-Q1–5 retain exact native/provider contracts, finalized recipe balance, shared lifecycle and
acceptance. The [progress ledger](../refinement/specification-progress.md) records document review
and validation separately. Final acceptance needs actual selected-item input → physical pawn
haul/work → native products/repair, plus interruption, exact quantities, partial output recovery,
provider groups, common settings/locales and cleanup on the independently reviewed build. Static
source/XML comparison does not prove game behavior or unchanged effective economy.
