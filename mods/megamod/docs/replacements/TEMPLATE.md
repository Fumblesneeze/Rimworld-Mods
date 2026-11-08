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

| Source entry point / implementation | Actual purpose | Debug / settings / toggle / gameplay window | Megamod destination and rationale | Intentional navigation change / player impact |
| --- | --- | --- | --- | --- |
| <tab or control> | <what player does> | <classification> | <assigned native surface> | <deviation> |

Separate mod configuration from pawn/area/other entity-specific gameplay state first. For actual
settings, specify section/order, global default, Use global inheritance, per-save override,
persistence/effect timing, main-menu disabled behavior and save-switch lifecycle. Use the common
load/store/resolve/row-rendering path; document any inheritance exception and its concrete reason.
Identify required native UI acceptance, including disabled override controls while inheriting.

## Own persistence and external integrations actually used

| Owned state or used integration + evidence | Save schema or required integration contract | Behavior, errors, persistence | Scope and limitations | Verification scenario |
| --- | --- | --- | --- | --- |
| <our saved state or actual integration> | <our save key or required package/Def/member/lifecycle> | <exact semantics> | <supported scope> | <exact mod list / native workflow> |

Specify our settings/defaults, owned save schema, reload behavior and any required migration of our
own state. For such migrations specify backup, atomicity, restoration on failure and postconditions.
Genuine integrations we use remain package/Def-resolved, absent-safe and shape-guarded. Original-mod
API/consumer audits, compatibility facades, saved-state imports and drop-in/coexistence work are not
part of replacement research. Independently research observable source behavior; do not copy code.

## Implementation and coverage

| Behavior | Implemented portion / code link | Deviation and possible conflict | Missing / deferred / deliberately omitted | Target spec / tests / live evidence |
| --- | --- | --- | --- | --- |
| <ID> | <none until implemented> | <including optimizations> | <explicit remainder> | <links, tested build, observed result> |

Identify the original's canonical package ID and corresponding owned feature settings. If it is
active, emit a bounded startup warning and attempt best-effort suppression through the same ordinary
effective-disable path as manually switching those features off. Record that path's timing and
limitations; do not promise complete coexistence safety or rollback of all effects. Keep stored
preferences intact and do not modify the original. Installed-but-inactive sources do not suppress
features; unrelated features and their shared services remain available. No original-provider
compatibility matrix or API parity work is required.

Describe required/optional dependencies and missing/changed-provider behavior for genuine external
integrations actually used, plus shared dataset ownership. Game-target semantic admission remains
required. XML edits must state content/package guards and merge policy; intentional list replacement
must explain discarded entries.

## Verification and acceptance

Record focused RED/GREEN evidence, exact selected test tier and nonzero counts, reviewed build/package
identity, independent review decisions, and native before/action/after observations in the fresh
isolated game. Include required absent/present/changed-provider cases for integrations actually used,
focused original-provider warning/effective-off cases, composed-source behavior cases, language
coverage where applicable, performance measurements for optimization claims, and cleanup evidence.
Keep unobserved behavior unverified. Record the final balance/progression/job-reachability assessment.
