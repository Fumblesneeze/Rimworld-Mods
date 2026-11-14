## Why

The original 137 researched feature drafts, plus the 2026-11-08 S138/S139 drug-policy additions,
need one implementation plan that consolidates shared data and
behavior while allowing several narrowly scoped agents to work at once. Coordinating contracts,
integration and native verification centrally avoids duplicated caches, conflicting edits and
premature acceptance of individually tested features that fail when combined.

## Owner

Exactly one owner: **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`,
RimWorld **1.7 beta**. Referenced source mods remain research inputs, not additional product owners.
This change delivers design, roadmap and governing task/process documents only; no gameplay code,
tests, runtime dependency changes, deployment or live acceptance is delivered now.

## What Changes

- Plan concrete shared components, their first consumers, authority, lifetime, freshness and failure
  behavior; reuse native data and existing owned helpers before adding materialization.
- Map every S001–S152 feature to an implementation chunk and dependency-aware integration part.
  Revision 23 added S140–S152; revision 24 expands S141 to all-mod option search without a whitelist.
  The other twelve require full per-source research and observable drafts before implementation.
  Revision 25 resumes the existing S024/S138 batch after the user's explicit activation.
  Preserve individual OpenSpec behavior contracts and task traceability instead of merging them away.
- Authorize parallel implementation of multiple source-derived features within megamod. Implementers
  perform focused test-first work, independent review and executable verification-scenario authoring.
  The orchestrator runs the integrated part's scenarios after all its chunks are ready and personally
  observes the native results before accepting behavior.
- Give the orchestrator authority to revise the plan as evidence changes: resequence/split/reassign
  chunks, revise shared designs and coordinate changed dependencies. Preserve user decisions and
  acceptance gates; record the evidence, affected contracts, superseded assignments and new plan.
- Keep implementers narrow. They inspect referenced mods and existing shared code to find game paths
  and edge cases faster, but do not copy source/decompiled code or substitute upstream behavior for
  the owned specification/design. Unforeseen cross-chunk changes return to the orchestrator.
- Add the explicitly user-authorized megamod-only process exception to root and local instructions,
  update development entry points, and distinguish implementation handoff from live acceptance.
- Exclude original-mod drop-in/API compatibility work under the latest user decision. Retain startup
  warnings and best-effort ordinary feature-off settings, without coexistence promises or persistent
  preference rewrites. Game-target checks and genuine third-party integrations remain required.

## Capabilities

### New Capabilities

- `megamod-staged-delivery`: observable coordination records, bounded handoffs, revised plans and
  integrated acceptance status. This is a mod-owned delivery-process contract, not a new game mechanic.

### Modified Capabilities

No archived gameplay capabilities change. The existing 137 feature drafts and the five foundation
capabilities retain their gameplay intent; original-mod compatibility obligations are superseded by
the latest scope decision. S138/S139 add separate policy features. The design coordinates implementation;
behavioral amendments, if later needed, belong in their original owning changes.

## Impact

Canonical artifacts live here: `design.md` (shared architecture and delivery protocol), `roadmap.md`
(parts/chunks/feature coverage), `tasks.md` (governing execution gates). Mod-local instructions and
documentation link here. Root `AGENTS.md` receives only the scoped exception, not a different
acceptance standard for other mods.

Future code remains in the megamod product, host-safe genuinely shared contracts under `shared/`
when needed, and honest test projects under `tests/`. No runtime library mod or dependency between
repository-owned products is introduced. Harmony becomes required at its first actual use; XML
Extensions (`imranfish.xmlextensions`, Workshop `2574315206`) becomes required at its first guarded XML
use. Other mods/DLC are optional, individually admitted inputs unless an existing feature requires
their content. Multiplayer, CE, original-save import and portal containers remain excluded/deferred
as already specified. The Dev Gateway stays developer-only and out of product packages.

Existing typed build/test/package/E2E operations remain the automation surface. A missing recurring
Gateway/tooling capability needs a separate tooling-owned change and operation, linked as a
dependency; this mod-owned plan does not silently take ownership of repository tooling.
