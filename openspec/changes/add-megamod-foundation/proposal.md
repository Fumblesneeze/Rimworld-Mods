## Why

Combining many quality-of-life features should remove duplicated infrastructure while delivering the
player behavior we specify from independent source research. Establish the ownership,
evidence, and patch-admission rules before implementing any replacement features.

## Owner

Exactly one owner: **Make Rimworld Great Again**, package ID `fumblesneeze.megamod`, at
`mods/megamod`. Repository tooling and upstream mods are inputs or supporting infrastructure, not
co-owners.

## What Changes

- Create a RimWorld **1.7 beta** product scaffold, as selected by the user, using the pinned ModSdk,
  generated About metadata, a positive-allowlist release profile, and the seven baseline language
  catalogs. The initial assembly contains SDK metadata only; there are no runtime hooks or features.
- Add mod-specific `AGENTS.md` instructions and contributor documentation enforcing analysis →
  observable specification → focused RED/GREEN → implementation → independent review → native
  in-game verification, including original features with no upstream mod.
- Require a documented, executable target spec for every future patch. Admit patches by the minimal
  semantic assumptions they need, tolerate unrelated changes and compatible other patches, and warn
  on startup when an enabled patch cannot be applied safely.
- Keep an honest replacement coverage ledger of researched behavior, intentional deviations and
  omissions. Original-mod API parity, consumer audits, compatibility facades and drop-in/coexistence
  work are excluded; contracts for external integrations actually used and our own save persistence remain.
- Initial gameplay support targets new colonies in single-player without Combat Extended. A loaded
  original provider triggers a startup warning and best-effort disablement of the corresponding
  owned features through the same ordinary path as manually turning them off. This neither changes
  the original nor permanently rewrites saved preferences or guarantees compatibility. Multiplayer,
  CE and importing pre-megamod colonies are deferred.
- Resolve interactions between listed mods into one composed feature specification. Implement those
  interactions directly in owned code, never as a stack of Harmony patches against megamod itself.
- Require XML Extensions presence/shape guards for XML modifications and merge-preserving edits by
  default; whole-list replacement requires an explicit feature intent and compatibility analysis.
- Specify shared map-scoped Thing data and cache ownership for future search and hauling consumers;
  do not introduce a cache or patch framework without a concrete first feature.
- Audit upstream bottom-bar additions and classify every adopted control by megamod's UI rules:
  debug/cheat overrides in the top debug toolbar, configuration in structured global/save settings,
  gameplay control windows in bottom tabs, and simple view/behavior toggles at bottom right.
- Specify a common settings resolution, persistence, and rendering path: editable global defaults,
  per-save overrides, and a shared Use global row; exemptions need a documented reason.

This change delivers the **scaffold and development contracts only**. Allow Tool, Cherry Picker,
Ctrl+F-style search, hauling changes, the patch-admission runtime, and shared caches are future work.
Those names are discovery candidates, not analyzed or implemented compatibility claims.

## Capabilities

### New Capabilities

- `megamod-foundation`: package identity, inert scaffold, localization inventory, and truthful status.
- `megamod-feature-intake`: evidence-based reverse engineering, original feature specifications,
  own persistence, genuine used integrations, original-provider warning/effective-off policy, and a
  replacement/deviation ledger without original API or drop-in compatibility obligations.
- `megamod-patch-target-specs`: documented semantic predicates, startup admission, patch coexistence,
  bounded warning diagnostics, and per-feature failure isolation for future patches.
- `megamod-shared-thing-data`: shared ownership, lifecycle invalidation, consumer-specific freshness,
  native action revalidation, and measured performance for future common Thing data.
- `megamod-ui-placement`: source UI audit, authoritative control-placement rules, structured settings,
  common global-default/per-save inheritance and UI, and disabled save-specific configuration when
  no game is loaded.

### Modified Capabilities

None.

## Impact

- New mod-owned files under `mods/megamod`, this OpenSpec change, and a solution/README registration.
- Required runtime: RimWorld 1.7 beta. No DLC or external mod dependency in the inert scaffold.
  Harmony (`brrainz.harmony`, Workshop `2009463077`) must become required with the first eagerly
  loaded Harmony integration. XML Extensions (`imranfish.xmlextensions`, Workshop `2574315206`)
  becomes required if a future feature uses `XmlExtensions.*` operations.
- Upstream feature providers are read-only behavior research inputs, not runtime dependencies.
  Genuine optional integrations actually used must still declare and validate exact canonical
  package IDs and callable shapes; excluding original-mod compatibility does not waive these checks.
- No dependency, reference, load-order hint, or shipped content from another repository-owned mod
  or the developer Gateway. Gateway remains isolated verification infrastructure only.
- No Workshop publication or acceptance of gameplay is part of the scaffold. Release presentation,
  exact-build release pins, feature tests, and final live acceptance must be completed for a real
  release; profile validity does not claim beta compatibility of future features.
