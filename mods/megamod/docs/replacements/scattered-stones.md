# Scattered Stones

## Identity and provenance

Owner **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`, target **1.7 beta**.
Origin replacement **S112/P2-41**. Installed **Scattered Stones1.7**, **Owlchemist.ScatteredStones**,
[Workshop3528503211](https://steamcommunity.com/sharedfiles/filedetails/?id=3528503211), maintained by
kkirspel, derived from Owlchemist and the1.5 continuation. Inspected2026-11-05. Selected1.7 DLL:
17,920 bytes, assembly1.5.1.0, SHA-256
`1637A7128B820F247B42A6A0BF4A80263B799F0093861333C43D4F71A4C9ED3F`.

[Full research](../refinement/2026-11-05-scattered-stones-research.md) retains historical original
commit `ff68f1ed5827642713c0d200fbaa53d6d5e0c29b`, source/binary differences, identical shared XML,
native1.7.5000 evidence, exact identities and bounded consumers. Original MIT notice says2020 jptrrs;
no license is bundled in the installed continuation. No code, binary or raster art is copied/selected.

## Behavior inventory

**Nothing implemented or accepted.** Entries are specified drafts in the
[observable capability](../../../../openspec/changes/specify-megamod-scattered-stones/specs/megamod-scattered-stones/spec.md).

| ID / behavior | Native action → intended result | Defaults / boundaries | Evidence |
| --- | --- | --- | --- |
| SS01 ground appearance | View/haul supported stone chunks → stable varied appearance |82–128% scale, full-turn rotation,±0.27cells per independent axis; physical properties unchanged | Source graphic/XML plus declared corrections |
| SS02 storage | Haul into/out of building storage → provider layout then restored ground variation | Ground stockpiles still scattered; held/icons unchanged | Source sticky-offset defect; owned layout precedence |
| SS03 initial debris | First admitted map initialization → colored debris at eligible loose chunks/exposed cliffs | Once per map, one patch/cell; exclude stored chunks, hidden/buried/edge/resource cliffs | Source map component, corrected geometry/dedup |
| SS04 yield debris | Actual successful stone yield → debris at actual destination | Dirty mining on; failed/ore-only yield and preexisting chunks do not count | Source postfix versus native Near placement |
| SS05 filth lifecycle | Clean/age debris → ordinary removal or natural-only renewal under supporting rock |240 work,-10 beauty,0 outdoor,-2 cleanliness,max1;60–90days; no tracking/rain thinning; persistence off | Source Def/Rocks plus native Filth/cleaning/expiry |
| SS06 construction | Place rock/build around debris → safe coexistence or local buried-patch removal | Other filth and pawn restrictions retained; no stuck cleaning | Source wipe/construction hooks and geometry correction |
| SS07 configuration | Common settings → current appearance/future deposit or expiry choices | Six inherited global/save rows, seven languages and truthful timing/help | Source six keys plus shared settings contract |
| SS08 compatibility | Unavailable target/provider → scoped native fallback and diagnostic | Source owns overlap; no hidden legacy migration or universal renderer support | Actual metadata/saved contracts and semantic guard design |

## Interactions and composed result

[Permeable Terrain](permeable-terrain.md) owns one-time non-liquid lifetime scaling. Deposits,
mining age resets and covered expiry renewal do not multiply or reroll the assigned lifetime.
[Common Sense](common-sense.md) and ordinary cleaning can act on this real filth; there is no second
cleaning job implementation. Hauling/search/designation/mining/construction keep their own physical
and quantity contracts. All owned composition is direct, without self-patches or a private chunk index.

Current texture/tint replacements are preserved where their renderer is admitted. Building storage
owns its layout. A community Stackable Chunks load-order rule motivates inspection, not a claim
that all stack/storage/texture mods work. Unsupported custom graphics retain their prior behavior.

## UI audit and relocation

| Source surface | Purpose | Owned destination / timing |
| --- | --- | --- |
| Three scale/offset sliders | Appearance configuration | Common Environment and cleaning / Scattered stones appearance rows; current loaded-map refresh |
| Water and Dirty mining checkboxes | Deposit configuration | Same section's debris rows; future deposits only |
| Never despawn checkbox | Lifetime configuration, misleading broad help | Keep debris while covered by rock; future natural expiry only, not cleaning protection |
| Automatic rendering/deposits | Gameplay/presentation | Existing map, mining/hauling/cleaning/construction workflows; no new tab/gizmo/toggle |

Global default / Current save / Use global apply to all six rows with dormant overrides, no main-menu
save mutation and invalid-entry recovery. No pawn-specific setting, source global-file import or
scope exception. Appearance defaults100%, bounds50–200% for sizes and0–300% for offset; water/mining
on, covered persistence off. Native job state and individual debris are saved gameplay state.

## Contracts consumed by other mods and saves

| Actual evidence | Binding | Selected policy |
| --- | --- | --- |
| Workshop database and replacement catalogue | Shared Owlchemist.ScatteredStones package, three source Workshop generations | Original active disables owned overlap; no package spoof |
| Community rule for zhrocks11.stackablechunks | Ordering report that the source can overwrite stacking behavior | Inspect actual current graphics/data before any adapter claim |
| Translation database entries | Source-key/package metadata | Seven owned languages; no claim all old translations bind automatically |
| Source saved debris | Owl_Filth_Rocks, ScatteredStones.Rocks/color, native Filth age/lifetime/thickness | Concrete SS-Q3 migration obligation; not automatically compatible with a new Def/type |
| Source saved map component | ScatteredStones.MapComponent_ScatteredStones/applied | Preserve one-time semantics in owned state; legacy transition requires separate verified migration |
| Public graphic/extension/utility fields | No non-source runtime caller found in bounded installed search | No speculative facade or universal absence claim |

Legacy conversion, if selected, must define supported schema, backup, atomicity, failure restoration,
provider collisions and verified postconditions before use. Owned generation can stop while owned
debris remains cleanable; whole-mod removal from a save is a separate concern, not advertised safe here.

## Implementation and coverage

| Portion | Implemented | Deliberate deviation / possible conflict | Missing |
| --- | --- | --- | --- |
| Chunk variation | Nothing | Independent axes/stable identity; storage owns layout; no permanent centering/cache | Guarded renderer, measured visual brief and live verification |
| Deposits | Nothing | One patch/cell, geometric exposure, consistent water/storage rules and true-yield destinations | Native wiring, first-pass failure recovery and exact provider support |
| Lifetime/cleaning | Nothing | Natural-only renewal instead of broad Destroy veto; explicit false default and real stats | S105 composition and causal cleaning/expiry verification |
| Settings | Nothing | Shared inheritance,1% appearance steps, truthful help and actual redraw | UI/localization/runtime checks |
| Legacy source data | Nothing | No automatic conversion or identity spoof | SS-Q3 practical migration decision and separate contract if selected |
| Source art/infrastructure | Nothing | No copied pixels/assemblies, unguarded PatchAll, broad graphicClass overwrite or private Thing index | Owned art remains wholly unselected; independent native visual gate required |

Required future Harmony and XML Extensions with actual use; no hard source/DLC/optional-provider/
Gateway dependency. XML preserves unrelated fields/lists and checks content/shape before mutation.
Design contains candidate target obligations, not implemented validators or compatibility acceptance.

## Verification and acceptance

Independent review and strict validation outcome live in the [ledger](../refinement/specification-progress.md).
All implementation/asset tasks remain unchecked. **SS-Q1–4** retain effective targets, visual/provider/
balance measurements, legacy saved contracts and native verification. Required final evidence includes
actual mine → yielded chunk/debris, haul → storage/ground presentation, clean → removed patch and
construct → valid remaining geometry on the reviewed build, with exact-process screenshots at distinct
zooms/tints and context-free independent visual review. Static source/XML and package checks cannot
accept those outcomes or establish negligible performance impact.
