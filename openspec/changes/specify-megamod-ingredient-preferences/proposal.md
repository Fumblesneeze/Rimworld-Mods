## Why

Players need ingredient preferences such as using damaged items, nearly empty stacks, abundant
supplies or random food without repeatedly editing ingredient filters. Best Mix supplies bench and
bill choices; these must compose with output-material preferences, shared storage facts and real
recipe quantities rather than stack several independent sorting patches.

Owner **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`; target **1.7 beta**.
Origin replacement S059/P1-53, Best Mix (Continued), `Mlie.BestMix`, Workshop 2195986094.
**Specification only; no gameplay delivered.**
[Research](../../../mods/megamod/docs/refinement/2026-11-04-best-mix-research.md) records selected
behavior, native 1.7 differences, settings and the proven Nice Bill Tab Expansion consumer.

## What Changes

- Specify contextual bench preferences and independent bill inheritance/override, all 26 ordinary
  source choices plus optional softness; CE electric protection is deferred, with explicit applicability and understandable values.
- Add an unambiguous Default choice preserving ordinary combined selection; retain explicit Nearest,
  current-temperature expiry and Random, without changing recipe permission, work or yield.
- Compose once: material lock, supported output-material preference, explicit ingredient preference,
  remaining enabled spoilage preference and ordinary ties. Mixing and no-mix quantities remain real.
- Consolidate stock-count facts with shared search/storage ownership and keep counting scope separate
  from physical access. Preserve independent bill state, linked defaults and committed iterations.
- Specify the 125-production-bill extension consistently across add, copy/paste and linked lists,
  together with common global/save configuration and safe lower-limit recovery.
- Preserve the two proven external button calls through a narrow facade; selected owned UI calls
  the same behavior directly. Supported optional properties have explicit admission; CE and multiplayer work are deferred.
- **BREAKING relative to source parity:** true inactive/default behavior, current ingredient beauty
  without adding a Stuff factor, explicit unavailable choices, stable ties/copy state, production-only
  capacity and no copied global state/context, dead radius controls or implicit save migration.

## Capabilities

### New Capabilities

- `megamod-ingredient-preferences`: bench/bill choice, observable criteria, stock accounting, composed
  ingredient selection,125-bill capacity, settings, lifecycle and external-control recovery.

### Modified Capabilities

None in the archived baseline. The in-progress S026 material-selection and S024 practical-supplies
drafts receive narrow explicit-preference composition clarifications. S017 already allows a
supported coherent capacity extension; S059 supplies that extension and bill-inheritance semantics.

## Impact

Future runtime changes belong only to megamod's existing bill/state/UI/ingredient paths. Harmony is
required when implemented. Soft Warm Beds and alternate bill windows are optional providers,
validated independently. CE and Multiplayer are deferred; original Best Mix is not a dependency. Other repository mods
and the Dev Gateway never become product dependencies.

No declarative Def mutation or XML operation is selected; owned entity state replaces source
startup component injection. Any later XmlExtensions operation requires XML Extensions
(`imranfish.xmlextensions`, Workshop 2574315206), guarded content/node checks and merge preservation.
MIT and inherited CC BY-NC-SA notices coexist in the source; no source/art/binary reuse is selected.
All implementation, exact provider support, finalized balance and native acceptance remain open.
