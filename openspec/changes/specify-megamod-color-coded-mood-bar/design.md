# Colonist-bar information design

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`). Target: 1.7 beta.

## Context

P1-01 was researched documentation first, then against the installed 1.7 DLL because the public
description and published source archive span older versions. The
[source report](../../../mods/megamod/docs/replacements/color-coded-mood-bar.md) records identity,
coverage, evidence, source discrepancies and unresolved contracts. No code or upstream asset is
being shipped. These artifacts specify future work; their existence is not runtime acceptance.

## Goals / Non-Goals

Goals: reproduce the selected observable information, preserve native bar workflows, use common
settings, and admit only compatible patches. Make omissions and corrections explicit.

Non-goals: changing mood/health, adding management tabs, importing the source's cache or SettingHelper,
copying a broad drawing replacement without guards, claiming optional compatibility from author prose,
or migrating existing source settings silently.

## Decisions

1. **Evidence priority.** User directions override source behavior. For unmodified source features,
   installed 1.7 code resolves older documentation, but defects are documented deviations rather than
   intentional behavior. Decompilation is static evidence, not proof of operation in a loaded game.
2. **Presentation composition.** Prefer narrow extension points retaining native drawing and input.
   The source replaces `ColonistBarColonistDrawer.DrawColonist` through a prefix returning false and
   duplicates native icons/labels. Do not select that architecture merely for parity. If a replacement
   remains necessary, enumerate all native responsibilities and guard the required effective semantics.
3. **Settings.** Reuse the foundation's common definitions, persistence, resolver and UI. Source
   `AltLabel` describes an alternative implementation; keep the visible name/age behavior without
   requiring this technical toggle. `ForcePortrait` becomes a clearly described refresh-on-draft option.
   Settings changes invalidate affected presentation immediately; no separate Apply button is needed.
4. **Defaults.** Fresh `MoodPatchSettings`/`ExposeData` values determine defaults, not the inconsistent
   reset handler. Mood RGBA tuples: extreme `(1,0,0,.44)`, major `(1,.5,.31,.44)`, minor
   `(1,.96,.016,.44)`, neutral `(.87,.96,.79,.44)`, content `(0,1,1,.44)`, happy `(.1,.75,.2,.44)`.
   Preserve meaningful display bounds; do not reproduce the source's cast of weapon scale to integer.
5. **State freshness.** Derive transient flags from current pawn state rather than retaining one-way
   true flags. A disabled or filtered overlay must not leak. Cache only measured presentation work
   with explicit pawn/map/game lifecycle invalidation. This is not a separate Thing search index;
   do not duplicate shared Thing indexing or invent a framework without a second real consumer.
6. **Provider policy.** The initial co-install policy is explicit suppression of our CM feature when
   `CrashM.ColorCodedMoodBar.11` is active, with a bounded warning. Do not remove or unpatch that mod.
   Optional alternate bars require canonical package identity, shape checks and isolated adapters.
   An unsupported optional adapter cannot take down the native-bar implementation elsewhere.
7. **No implicit migration.** New global/save settings start at the specified defaults. Reading or
   migrating old source settings requires a separate backup/atomicity/restoration contract. Never
   write the original mod's settings or spoof its package identity.

## Patch target preparation

Actual stable-ID target documents are required before implementation, when exact hooks are chosen.
This is a hook investigation list, not completed target admission:

| Surface | Required semantic investigation |
| --- | --- |
| Native bar draw | Available portrait bounds/pawn, original execution and selection/input preservation; effective foreign drawing changes, not a whole-method hash |
| Bar entry refresh | Callback shape and lifecycle needed to release derived presentation data; existence-only if only observing completion |
| Draft refresh | Current portrait invalidation path and draft state, without interfering with drafting |
| Optional bar adapter | Canonical active package plus each consumed member, draw ownership, fallback and ordering |

Transpilers, if needed, must validate each incoming instruction stream and return it unchanged on
failure. Every enabled skipped patch gets the foundation warning; required atomic groups roll back
only newly owned patches. XML is unsuitable for dynamic bar behavior; no Def changes are proposed.

## External contracts

The source probes `TacticalGroups`/legacy alternate-bar types, `RR_PawnBadge`, `JobInBar` and
`DisabledPawns` by reflection. It invokes external badge/job draw postfixes when using Colony Groups,
and consumes `Disabled.IsDisabled`. These are outgoing integrations, not proof that those mods consume
CM APIs. The incoming-consumer audit remains open. Do not expose the source's public implementation
types as a compatibility facade until a real consumer is identified. Resolve canonical provider IDs
and selected version shapes before claiming support.

## Risks / Trade-offs

- Extensive native-bar replacement can discard 1.7 behavior or other overlays; prefer narrower work.
- Source “troubled” flags, weapon/glow placement and reset behavior are inconsistent. Corrected
  behavior is visible and must be listed in the replacement report.
- Color-only information has accessibility limits. Icons, shape/size and tooltips should carry meaning;
  exact accessible presentation needs visual review, not a claim based on these files.
- Timing is a visible preference, not a promised benchmark result. Measure update and draw cost before
  calling the replacement faster. The source's timings/performance claims are not ours.
- Public source archive license does not establish provenance for every later asset or helper.

## Migration Plan

No runtime migration in this documentation change. Later introduce settings through the common
system, guard the display integration, and validate native bar behavior before optional-provider
matrices. Retain the source-disabled/native fallback and preserve saves on feature removal.

## Open Questions

Record these for later; do not interrupt the sequential research interview:

- **CM-Q1 (research):** exact 1.7 caravan/world-view semantics for current-map/current-screen filtering;
  extend the observable scenarios after tracing native behavior. Do not silently exclude travelers.
- **CM-Q2 (research):** canonical optional-provider identities, current source contracts and real
  incoming consumers. The upstream author's compatibility statement is not sufficient.
- **CM-Q3 (design/visual):** precise collision-free positions, tooltips and accessible alternatives
  across bar scales and multiple relations; verify source visuals and native 1.7 before final layout.
- **CM-Q4 (provenance):** confirm later-version asset/helper licensing if any reuse is proposed.
  Current plan is independent implementation and owned assets, so source reuse is not required.

## Verification plan

Focused honest host tests cover threshold precedence/boundaries, worst-disease selection, current
state transitions, filtering, fractional scales, settings inheritance and reset consistency. Patch
tests cover semantic guard failures and benign foreign patches. Final reviewed-build native workflows
must cover selecting/drafting/reordering, schedule editing, actual tending/recovery/pregnancy and
temperature changes, save/settings transitions, plus exact supported optional-provider runs. Direct
state setup is only a fixture; inspect causal before/action/after screenshots. No runtime task is
complete without the repository's final live acceptance and playability review.
