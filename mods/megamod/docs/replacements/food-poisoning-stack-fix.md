# Food Poisoning Stack Fix (Continued)

## Identity and provenance

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
Origin: **replacement**, S041/P1-32. Source `Mlie.FoodPoisoningStackFix`, Workshop **2843483188**,
About/manifest **1.7.0**, selected assembly **1.7.0.0**, informational revision
`dc261ee3c7fbc29780780467eee44302d69ce4a8`. DLL SHA-256
`F34E9B83F0243F70DAA73AB6C57CFD32AD5D10171DD6A9A6E7270F203261498D`.
Installed MIT license credits Mlie 2020; assembly credits Dyrewulfe 2026. Retain required notices
if code is ever reused; no code, DLL or art is copied into the product.

[Research](../refinement/2026-11-03-food-poisoning-stack-fix-research.md) records public documentation
first, the complete four-type 1.7 DLL, three patches, selected native production/stack/ingestion
paths and concrete repository consumer evidence. Source arithmetic findings are code traces, not
live reproductions; the public PR's intended fix is not taken as proof the installed code is correct.

Owning [proposal](../../../../openspec/changes/specify-megamod-food-taint-accounting/proposal.md),
[design](../../../../openspec/changes/specify-megamod-food-taint-accounting/design.md),
[observable spec](../../../../openspec/changes/specify-megamod-food-taint-accounting/specs/megamod-food-taint-accounting/spec.md)
and [tasks](../../../../openspec/changes/specify-megamod-food-taint-accounting/tasks.md).
**Drafted only; nothing implemented or accepted.**

## Behavior inventory

| ID | Input/action → observable result | Preserved / corrected / added contract | Status |
| --- | --- | --- | --- |
| FP-01 | Stack tainted and clean food → finite mixed population | Source intent; actual quantities/stack limits unchanged | Draft |
| FP-02 | Cook a tainted multi-unit batch → all native-tainted output retained | Correct source count-versus-probability reinterpretation; native chance opportunities unchanged | Draft |
| FP-03 | Full/partial merge or split → moved taint plus exact remainder | Correct recipient overwrite, donor duplication and split double subtraction; fair sampling | Draft |
| FP-04 | Zero/failed/changed transfer → only actual movement counted | Native false return can still mean partial success; no-op cannot overwrite taint | Draft |
| FP-05 | Eat food → exposure from actual eaten portion | Correct remainder/full-destruction callback assumptions, one modified native check | Draft |
| FP-06 | Mixed causes / immunity / other risk → correct attribution and native effects | Consumed causes retained; independent risk not confused with cooking taint | Draft |
| FP-07 | Loss, ingredients or holder transfer → no ghost population | Ordinary physical rules, no invented ingredient transmission | Draft |
| FP-08 | Load known native/owned food → stable assignment or exact owned population | Explicit initial probability assignment, provenance, save backup/atomicity and stale-state handling | Draft |
| FP-09 | Debug toolbar inspection → read-only actual accounting/format | Relocated from source inspect text; hidden from ordinary UI/search | Draft |
| FP-10 | Original/provider changes → one admitted owner and explained fallback | Preserve native probability contracts; no automatic provider migration | Draft |
| FP-11 | Tracked and unsupported food merge → explicit handoff | Both operands/results/remainders retire owned tracking only on actual movement; unknown provenance persists | Draft |

No row is implemented. Original source settings: **none**. Native cooking/illness modifiers remain
the controls; no chance multiplier, new duration, ordinary feature switch or inheritance exception
is introduced. Initial native assignment and per-ingestion/cause rules are explicit proposed
corrections, not asserted source parity.
Finite actual-unit exposure intentionally changes some mixed-stack illness probabilities, including
multi-unit ingestion and modifiers above one; native calculation/illness preservation does not mean
identical odds to the previous averaged-stack model. The design records concrete 65.1% versus 10%
and 10% versus 20% examples, not new configurable constants.

## Interactions and composed result

S021 hauling, S027 deliveries and future S123 meal carrying use the same physical transfer accounting.
No extra inventory, duplicate taint allocation, capacity bonus or internal patches against our own
ports. Shared search data may reflect public stack changes but never expose latent taint as a filter.

Native/modded food using supported ordinary paths participates generically. Immersive Chefs is a
concrete probability/serving consumer: a supported optional adapter must correlate the actual eaten
portion with its serving and apply custom risks/attribution once. A matching field name alone is
insufficient. If unsupported, covered food remains under provider control with a visible diagnostic;
unrelated supported food stays eligible. Neither repository mod gains a runtime dependency on the other.
For actual cross-boundary transfers without a joint route, both participating stacks relinquish
owned tracking, including any remainder; native/provider physical results and unsupported provenance
are retained. A zero transfer does not itself retire tracking. Known-native unassigned food instead
receives its stable assignment before ordinary finite transfer. This degradation is explicit and
cannot be mistaken for conserved exact counts across unknown formats.

## UI audit and relocation

| Source surface | Purpose | Owned placement / change |
| --- | --- | --- |
| Developer-mode extra inspect text on food | Hidden-state debugging | Explicit read-only Food taint action in top debug toolbar |
| No source ordinary menu/settings/bottom tab | None | None introduced |
| Actual native illness notification | Player-observable health event | Preserve native notification, with cause from consumed exposure |

The developer diagnostic shows physical/tainted counts, causes and tracking availability, not a
universal safe-food verdict. Developer mode off closes it and rejects stale callbacks. Ordinary
inspect/search/gizmos never disclose latent contamination. Seven-language UI and native input still
need acceptance; no new raster art has been selected.

## Contracts consumed by other mods or saves

| Contract / actual evidence | Source behavior | Owned decision and boundary |
| --- | --- | --- |
| Native PoisonPercent/poisonPct probability and cause | Reused as integer count without changing native consumers | Preserve probability semantics; owned counts/cause groups stored separately |
| Native save keys `poisonPct`, `cause` | No source discriminator; one can mean count or certainty | No guessed source conversion; explicit native/owned provenance and normalized saves |
| Source four patch classes / owner ID | Bounded text and ignore-disabled DLL scans found no external caller binding | No speculative facade or package/assembly spoof; dynamic/uninstalled consumers unproven |
| Immersive Chefs UnifiedFoodPoisoningPatch | Reads/writes probability temporarily and restores it; serving-based custom risk | Exact optional serving/risk/finalizer ownership or affected-path fallback |
| Immersive Chefs CulinaryState / DiningIngestionOutcomePatch | Transfers/consumes serving records, snapshots ingestion data | Correlate actual portion before compatibility claim; no direct edits to the other mod |

Original provider active suppresses owned overlapping accounting with bounded warnings. Installing or
removing providers around existing food is not an automatic migration. A future source converter
needs its own preview, backup, atomicity, failure restoration and verified postconditions; invalid
source counts cannot reveal which historical units were actually tainted. Runtime compatibility and
safe removal remain unproved.

## Implementation and coverage

| Scope | Implemented | Deliberate deviations / conflict risks | Missing |
| --- | --- | --- | --- |
| FP-01–11 | Nothing | Correct finite accounting, native probability kept, actual consumed context and explicit unsupported-boundary handoff | All code/tests/target specs/native evidence |
| Persistence | Nothing | Versioned owned state, known-native assignment, explicit suspended/ambiguous formats | Exact provenance/serialization/save recovery proof |
| Providers | Nothing | One admitted route, original suppression, no uniform compatibility promise | Exact absent/present/changed-provider runs |
| Performance | Nothing | Aggregate populations and shared physical operations | Measured large-stack/allocation/combined-hauling cost |
| Dependencies | No metadata changed | Core required; Harmony required when used; upstream/DLC/providers optional | Implementing package checks; XML Extensions only on actual XML use |

No current XML patch. Any later Def integration must use guarded content/package/shape checks and
merge-preserving operations. No source or Gateway assemblies/references belong in the product.

## Verification and acceptance

FP-Q1–4 retain concrete runtime/format/provider gaps. Focused tests must prove bounds, conservation,
fair allocation, actual transfer/consumed quantities, no-op behavior, immutable source-save recovery
and real finalizer ownership in honest environments. They do not accept gameplay.

Final acceptance requires personally observing actual cooking, native stack/haul/eating workflows,
save/load and the real developer readout on the reviewed build, including multi-unit pemmican and
focused provider cases. A field showing a number, source math trace or debug-created poisoned end
state alone is insufficient. Retain causal actions, physical outcomes/screenshots and exact build/
mod/configuration/cleanup evidence.

Semantic pass: conserve food and cooking-taint quantities while preserving recipe output, room/cook
rolls, native illness modifiers, independent risks and ordinary transfer reachability. Batch-cooking
taint covers the batch, not one unit. No economy, nutrition, stack-size or carrying bonus is introduced.
