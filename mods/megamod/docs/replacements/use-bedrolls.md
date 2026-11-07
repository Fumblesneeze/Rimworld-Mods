# Use Bedrolls - Continued

## Identity and provenance

- Owner **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`; **1.7 beta**.
- Origin **replacement**, S073/P2-02. Source `Memegoddess.UseBedrolls`, Workshop **3534169351**,
  title Use Bedrolls - Continued; Uuugggg / Meme Goddess. Selected1.7 assembly/file1.0.0.0.
- [Workshop documentation](https://steamcommunity.com/workshop/filedetails/?id=3534169351) first,
  then [maintainer repository](https://github.com/MemeGoddess/RimWorld-UseBedrolls), installed source,
  full selected binary, XML and current native1.7.4998 behavior. Read-only inspection2026-11-04.
- MIT copyright2025 Alex Tearse-Doyle; retain notice for any future reuse. No source assembly/code
  copied into the product. Hashes, exact inputs, static evidence limits and PBR-Q1–5 are in
  [research](../refinement/2026-11-04-use-bedrolls-research.md).
- **Specification draft only. Nothing implemented, migrated or verified in game.**

## Behavior inventory

| ID | Observable behavior | Defaults / edges | Owning capability | Status |
| --- | --- | --- | --- | --- |
| PB01 | Ordinary rest can use a physically deployed bed | Away-home on; home off; distance rule off, threshold100,0–300; medical/duty vetoes remain | [Portable beds](../../../../openspec/changes/specify-megamod-portable-beds/specs/megamod-portable-beds/spec.md) | Draft |
| PB02–03 | Own, ground then spare shared beds; nearby sheltered deployment | Protected stock/manifest/capacity; no side effects during search or destructive clearing | Same | Draft; intentional corrections |
| PB04 | Recover same bed after rest, preserving work/occupants | Ordinary uninstall; occupied/blocked/full waits; already-packed identity and load | Same | Draft; source failure cases corrected |
| PB05 | Arrival redistribution and personal supply accounting | Each recipient compatible; native unload modes; deployed commitment distinct from carried/free stock | Same | Draft |
| PB06–07 | Contextual controls, common settings and bounded compatibility fallback | No new tab/toggle; source authority and recoverable saved state | Same | Draft |
| HB01–03 | Remember/reclaim/cancel home intent | S115 composition: release on exit false; optional per-allowance memory; occupied-slot reclaim false | [Home memory](../../../../openspec/changes/specify-megamod-portable-beds/specs/megamod-home-bed-memory/spec.md) | Draft; source bookkeeping/default corrected |
| BT01–02 | Produce native single/double bedrolls at tailoring benches |40/85 materials; proposed600/1100 work; Crafting quality; preserve compatible recipes/categories | [Tailoring](../../../../openspec/changes/specify-megamod-portable-beds/specs/megamod-bedroll-tailoring/spec.md) | Draft; work deliberately differs |

Evidence for all rows is documentation plus read-only source/native analysis, not runtime observation.
See [proposal/design/tasks](../../../../openspec/changes/specify-megamod-portable-beds/).

## Interactions and composed result

S021/S126 supply one physical capacity/identity account; S062 owns personal targets, retained stock
and medical/field-rest intent. A deployed personal bed covers its corresponding shortfall without
being counted as carried or free supply. No competing restocking job should acquire a duplicate.
S061 roof-safe uninstalling remains applicable. S060 excludes installation of already-built furniture;
no frame-enclosure protection is promised here. S070 consumes native timetables.
[S115 bed ownership](sensible-bed-ownership.md) retains home claims by default, with optional released
memory per allowance, scoped manual supersession and temporary rest without a second ordinary claim.
Its seat fallback follows eligible deployment. S135 caravan loading remains queued. These are direct owned calls, not a stack
of patches against our own implementation. Candidate data comes from native listers/shared map index,
with current pawn-specific permission rechecks and no independent bed cache.

## UI audit and relocation

| Source surface | Purpose | Owned destination / change |
| --- | --- | --- |
| Mod settings | Five global defaults | Pawns / Rest and portable beds; common global/save rows, plus feature enable; distance visible but inactive while off |
| Home-bed portrait/cancel gizmo | Remembered travel claim | Contextual bed/pawn pending-owner list; cancel only selected memory, preserve unrelated current claim |
| Temporary-bed portrait/cancel gizmo | Stop automatic pickup | Contextual custodian/shared sleeper state; release automation without destroying/unassigning bed |
| Drafted-pawn pickup gizmo | Real pickup order | Same contextual action with capacity/occupancy/access reasons and normal movement/work |
| Owned waiting-job status | Retry blocked portable work | Custodian and surviving bed/installation show ordinary Retry, distinct from forced pickup and its roof-risk exception |
| Tailoring Add bill | Ordinary production | Existing integrated bill browser, native bill filters and controls |

No source bottom tab/debug surface was found; none is added. No bottom-right toggle is selected.
Entity choices stay entity state. All actual settings use Global default / Use global / Current save,
dynamic inheritance, retained dormant overrides, disabled main-menu save controls/callbacks and no
cross-save leakage. Changes affect next decisions, not rendering-time work or interrupted valid sleep.
Seven-language UI and actual player workflows remain unchecked implementation tasks.

## Contracts consumed by other mods or saves

| Consumer / evidence | Binding | Decision / remaining proof |
| --- | --- | --- |
| Smart Medicine3526251680 StockUpGUI | Presence of Memegoddess.UseBedrolls exposes native Bedroll | S062 exposes owned supported beds directly; no spoofed ID. Original external menu can still omit the choice |
| Replacement catalog3396308787 / mod-manager db1847679158 | Old Uuugggg.UseBedrolls and TheLordDrake.UseBedrolls identities and translation metadata | Source-lineage authority detection; not proof of callable API or analyzed old versions |
| Native recipes/products | Bedroll, BedrollDouble; generated Make_ recipe identities where admitted | Preserve native products and compatible recipe users; no duplicate/conflicting definition overwrite |
| Source saves/settings | homeBeds, placedBeds, source component/job types and five global keys | No automatic import or source type facade; backup and finish/cancel source work before removal |

The bounded text search found no further callable consumer, but cannot exclude compiled-only/reflected
bindings. Do not claim general drop-in compatibility. Any later migration must specify backup, atomic
conversion, failure restoration and verified postconditions. Disabling a feature is not safe mod removal.

## Implementation and coverage

Nothing is implemented. Deliberate departures from source are transaction-safe selection/transfer,
shared mass limits, current recipient compatibility, persistent recovery progress, narrow expected
message suppression, no global pawn construction override, no destructive install footprint, protected
personal stock, coherent rest episodes, multi-owner memory and validated occupied-slot reclaim.
Unassigned ordinary beds are explicitly configured for a valid colony-slave sleeper after installation,
preserving compatible sharing and subsequent player choices.
Tailoring work increases from350/600 to proposed600/1100 while native products and material costs remain.

Original source lineage gets sole authority over overlapping new work/recipes when active; owned
pending jobs remain stopped and physically recoverable until a permitted cleanup. Required future
dependencies are Harmony and XML Extensions; original providers/content/DLC are optional. Each
per-product XML contribution is guarded/atomic/idempotent, preserving unrelated lists and recipes.
Every actual patch still needs a target document, executable semantic admission, focused tests and
reviewed-build live evidence. Candidate seams in design are not accepted patch targets.

Deliberately outside this draft: visitors/animal rest automation, new caravan sleep mechanics, new
beds/art, automatic source settings/save migration, multiplayer guarantee, blanket optional content
support, safe mid-save removal or completed compatibility claims.

## Verification and acceptance

Documentation review/validation is tracked in the [ledger](../refinement/specification-progress.md).
All implementation tasks remain unchecked. Final acceptance needs actual resting/deploying/packing,
caravan entry, home reclaim/cancel and bill production on the reviewed build, including interrupted
work/load, double-bed occupants, capacity loss, manifest protection, source present/absent and changed
required semantics. Settings/languages, finalized material/work/value comparators and shared-index
costs need their own evidence. Tests/startup/logs/Def inspection alone do not prove these workflows.
