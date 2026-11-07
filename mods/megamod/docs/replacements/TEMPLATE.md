# <source name or original feature>

Template only; replace placeholders with inspected facts before marking anything analyzed.

## Identity and provenance

- Owner: Make Rimworld Great Again (`fumblesneeze.megamod`, `mods/megamod`).
- Origin: replacement / original.
- Source package/Workshop ID and title; version, inspected date, assembly/source revision and hash.
- Read-only source locations and relevant method/Def/XML paths; license and attribution obligations.
- Observed facts versus inferred behavior and unresolved questions. For original work, source fields
  are explicitly not applicable and the user requirement is the provenance.

## Behavior inventory

| Behavior ID | Native input/action → observable result | Options/defaults, timing/balance, edge cases | Evidence | Owning OpenSpec scenario | Status |
| --- | --- | --- | --- | --- | --- |
| <stable ID> | <specific behavior, not a mod name> | <assumptions to specify> | <inspection/capture> | <link> | <status> |

## Interactions and composed result

Identify selected sources that this source changes or that change it, ordering/options that affect
the result, and any conflict or incompatible defaults. Link each base and modifier to one chosen
composed behavior spec. State which source hooks disappear into direct owned code. No self-patches.

## UI audit and relocation

Inventory every bottom tab and other entry point, including injected/dynamic controls. Split
mixed-purpose windows. Apply [UI placement](../UIPlacement.md) rather than copying the source layout.

| Source entry point / implementation | Actual purpose | Debug / settings / toggle / gameplay window | Megamod destination and rationale | Intentional navigation change / consumer impact |
| --- | --- | --- | --- | --- |
| <tab or control> | <what player does> | <classification> | <assigned native surface> | <deviation> |

Separate mod configuration from pawn/area/other entity-specific gameplay state first. For actual
settings, specify section/order, global default, Use global inheritance, per-save override,
persistence/effect timing, main-menu disabled behavior and save-switch lifecycle. Use the common
load/store/resolve/row-rendering path; document any inheritance exception and its concrete reason.
Identify required native UI acceptance, including disabled override controls while inheriting.

## Contracts consumed by other mods or saves

| Consumer + evidence | Binding / identity / lifecycle contract | Behavior, errors, persistence | Preserve / adapt / unsupported | Verification scenario |
| --- | --- | --- | --- | --- |
| <actual consumer> | <assembly/type/member, Def, package check, event or save key> | <exact semantics> | <choice and reason> | <exact mod list / native workflow> |

Include settings/defaults, saved type/key schema, removal behavior, and migration where applicable.
For migrations specify backup, atomicity, restoration on failure, and postconditions. State why a
compatibility facade suffices or why an upstream package/assembly binding prevents a drop-in claim.

## Implementation and coverage

| Behavior | Implemented portion / code link | Deviation and possible conflict | Missing / deferred / deliberately omitted | Target spec / tests / live evidence |
| --- | --- | --- | --- | --- |
| <ID> | <none until implemented> | <including optimizations> | <explicit remainder> | <links, tested build, observed result> |

Describe original-provider co-installation policy, required/optional dependencies, missing/changed
provider behavior, compatibility boundaries, and shared dataset ownership. XML edits must state
content/package guards and merge policy; intentional list replacement must explain discarded entries.

## Verification and acceptance

Record focused RED/GREEN evidence, exact selected test tier and nonzero counts, reviewed build/package
identity, independent review decisions, and native before/action/after observations in the fresh
isolated game. Include required absent/present/changed-provider and composed-source cases, language
coverage where applicable, performance measurements for optimization claims, and cleanup evidence.
Keep unobserved behavior unverified. Record the final balance/progression/job-reachability assessment.
