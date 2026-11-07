## Why

Repeatedly configuring production bills is cumbersome, and a target that overlooks equivalent goods
or goods carried away can order unnecessary production. Better Workbench Management provides useful
controls, but its individual-bill links and older counting assumptions need to be reconciled with
native 1.7 and the confirmed whole-list workbench design.

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`). Target: **1.7 beta**.
Origin: replacement of **Better Workbench Management**, `falconne.BWM`, Workshop **935982361**.
This change specifies behavior for later implementation; **no gameplay is delivered or accepted**.

## What Changes

- Copy one bill or a complete production list, append independent copies atomically, and paste
  applicable choices into an existing bill while retaining its recipe and physical work.
- Add contextual navigation, drag ordering, clear delivery and resume-level controls, and a
  bench-wide worker policy that governs existing and subsequently added bills.
- Count selected additional product types and explicitly included traveling stock without counting
  a physical item twice. Use one coherent count for the display and the production decision.
- Compose with whole-list links, mixed storage and the integrated Add bill browser directly.
  **BREAKING relative to source:** no individual-bill Paste Link/Break Link or local suspension
  exception; linking affects the entire plan. Clipboard content is a copy-time snapshot, and a
  multi-bill paste either succeeds completely or changes nothing.
- Preserve native renaming and native 1.7 nonhuman carry counting; common settings replace the
  source's global-only preferences. No new bottom tab or debug command is needed.
- Record source gaps, concrete external consumers and deferred provider/save migration work rather
  than claiming that the source package, assemblies or serialized types are drop-in compatible.

## Capabilities

### New Capabilities

- `megamod-workbench-bill-management`: copying, pasting, list/detail conveniences, configuration
  scope, whole-list composition and safe lifecycle behavior.
- `megamod-workbench-worker-policy`: contextual bench/group worker restrictions and inheritance by
  newly added bills, without bypassing ordinary worker eligibility.
- `megamod-production-stock-counting`: additional products, scope/carrier/away choices, unique item
  totals, shared freshness and recovery when counting support is unavailable.

### Modified Capabilities

None in the archived baseline. The unimplemented S014 draft is clarified alongside this change:
its shared plan consumes these worker/count choices, without a second linkage mechanism.

## Impact

Future runtime dependency: **required Harmony**. The chosen design stores away associations in
owned save state and does not inject a pawn comp or require XML for this feature. If implementation
instead needs any XmlExtensions operation, update this proposal and declare **XML Extensions
(`imranfish.xmlextensions`, Workshop 2574315206) required** before adding guarded, merge-preserving XML.

Optional provider work: **No Max Bills: Redux** (`justharry.nomaxbillsredux`) has a verified reflected
limit contract, but no tested beta adapter yet. Outfitter, Project RimFactory, Colony Groups and
Deep Storage source integrations require canonical identities and exact supported shapes before
acceptance; they are not required to play the native feature. Selected Everybody Gets One is S018,
whose modifier behavior must join the owned plan directly in its own change. No product dependency
on another repository mod or the Dev Gateway is introduced.

The source-active overlap is withheld with an explanation. Saved owned extended policies are never
silently reinterpreted if support fails. No source configuration/link migration is promised.
See [research](../../../mods/megamod/docs/refinement/2026-11-02-better-workbench-management-research.md),
[coverage](../../../mods/megamod/docs/replacements/better-workbench-management.md) and BM-Q1–4 for
evidence, proposed corrections and outstanding work.
