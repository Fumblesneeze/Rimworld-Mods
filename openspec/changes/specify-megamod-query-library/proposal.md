## Why

Search and population-sensitive production need the same expressive, reusable queries. Specify TD
Find Lib's catalog and library independently so copying a query, changing maps or losing optional
content cannot silently change what it means or expose information the player has not discovered.

## Owner

Exactly one owner: **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`.
Target **RimWorld 1.7 beta**. Origin replacement, **S121/P2-51**, TD Find Lib - Continued,
`Memegoddess.TDFindLib`, Workshop3529443295, Uuugggg / continuation Meme Goddess.

## What Changes

- Supply ordinary item, building, environment, pawn, policy, animal and expansion query criteria
  with explicit meanings, defaults and examples. Build on S033's existing query composition,
  visibility, physical identity, map scope and unavailable-result contracts.
- Provide a global reusable library with independent working copies, deliberate save/cancel,
  group organization, complete copy/export/import and explicit rebinding of save-specific choices.
- Use the same owned queries in S033 search and S018 production. Preserve narrow, demonstrated
  exchange contracts through the real optional library; do not pretend to replace its assembly.
- Correct lost operands, first-occurrence health checks, unsafe category shortcuts, stale label
  bindings, lock bypasses and partial imports. Keep unsupported definitions intact for repair.
- Separate developer inspection from ordinary queries and counts. Admit bounded non-mutating
  inspection only through the top debug toolbar, with no automatic execution of imported members.
- Offer source-derived examples as explicit, editable imports; no automatic alerts, production
  orders or recreation of examples the player deleted.

**Specification only.** [Research](../../../mods/megamod/docs/refinement/2026-11-06-td-find-lib-research.md),
[catalog](../../../mods/megamod/docs/refinement/2026-11-06-td-find-lib-catalogue.md) and
[sample audit](../../../mods/megamod/docs/refinement/2026-11-06-td-find-lib-samples.md) retain exact
inputs and differences. Nothing is implemented, migrated or verified in a running game here.

## Capabilities

### New Capabilities

- `megamod-query-catalogue`: ordinary criteria and defaults, consistent option suggestions and
  visible handling of unavailable information.
- `megamod-expansion-queries`: Royalty, Ideology, Biotech, Odyssey and narrow optional-provider
  observations, including unavailable-content and persistent-choice behavior.
- `megamod-query-library`: reusable templates, transactional editing, examples, persistence,
  complete exchange, shared settings and consumer boundaries.
- `megamod-debug-queries`: separately entered developer inspection with bounded, non-mutating
  traversal and no ordinary-consumer leakage.

### Modified Capabilities

None. This fills the explicitly reserved S121 catalog/library scope in S033 and S018; it preserves
their workspace, refresh, bill-map, query-copy and failure semantics.

## Impact

- Reuse the owned shared Thing data and native observations. No standalone runtime library mod,
  per-source cache, self-patching stack, new bottom tab or background query scheduler.
- RimWorld is required. DLC and TD Find Lib, LTO Colony Groups, CAI5000 and Float Sub-Menus are
  optional; no source dependency on TD's Bug Fixes is inherited. RealFOW's dormant always-true
  predicate is not a supported integration. Installed CAI/Float providers declare only through1.6.
- No new Harmony or XML patch is justified by the library itself. Any necessary integration hook
  follows foundation target admission; Harmony becomes required if used, and XML Extensions
  (`imranfish.xmlextensions`, Workshop2574315206) is required with any XML Extensions operation.
- No upstream assembly/package impersonation, automatic source-save migration, copied assemblies,
  other repository-product dependency or Dev Gateway dependency. MIT attribution is retained.
