## Why

Short messages can overlap the colonist bar, hiding both information and controls. Specify an owned
replacement for Better Message Placement (Continued) that keeps native message actions while using
the visible bar and current UI dimensions to position them.

## Owner

Exactly one owner: **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`.
Target **RimWorld 1.7 beta**. S071 is a replacement reference, not a co-owner.

## What Changes

- Specify automatic placement below visible colonist-bar content, with native placement when no bar
  is drawn. Account for current rows, labels, weapons, group frames and the owned mood-bar display,
  including its hidden/reduced portraits and adjustable weapon presentation.
- Retain horizontal/vertical percentage adjustments, each initially zero and limited to −100% through
  +100%, in common global/save settings. Apply them once relative to automatic placement using the
  current logical viewport. Manual adjustment may intentionally overlap the bar.
- Keep the native stack together and within the viewport when it fits; if it cannot fit, retain its
  native order/spacing and prioritize the newest message. Document clipping and possible bar overlap
  rather than promise that unlimited text fits on every screen.
- Preserve text, posting, sound, archive policy, expiry, ordering, hover, target/quest actions and
  dismissal. Letters, alerts, logs and combat text are outside this change.
- Replace source-generated test messages with an inert, labeled settings preview; make reset and
  inheritance obey their explicitly selected scope. No new bottom tab or operational toggle.
- Correct source double-origin addition, estimated row counts, physical-resolution offsets and
  partial patch failure. Use semantic admission, bounded fallback and best-effort original-provider handling.

Manual placement, common scoped controls and the bounded inert preview are partially implemented,
with focused host evidence and reviewed English native reset/enable/disable/preview workflows.
Revision20 adds an implemented-unverified current native portrait/label geometry path, with explicit
manual-only fallback for current unproved adornments. Full automatic geometry, complete native
interaction and preview coverage, rendered locale checks and final acceptance remain unfinished.
No whole replacement is verified; layout choices and remaining evidence obligations stay visible
in refinement.

## Capabilities

### New Capabilities

- `megamod-message-placement`: visible-bar clearance, scoped manual positioning, bounded overflow,
  unchanged native message interactions, inert preview and compatibility fallback.

### Modified Capabilities

None. S001 composition consumes its effective visible presentation; it does not change that
feature's observable contract.

## Impact

- Source: **Mlie.BetterMessagePlacement**, Workshop **2259516339**; exact 1.7 binary, MIT attribution,
  documentation, native evidence and limits are in the
  [research](../../../mods/megamod/docs/refinement/2026-11-04-better-message-placement-research.md).
- Future implementation touches native short-message placement and common Interface / Messages
  settings. No new gameplay entities, Def values, resource balance, raster art or private Thing cache.
- Harmony is **required when the integration is implemented**; the original mod and optional
  colonist-bar providers are **optional**. No DLC requirement. No selected XML change or new XML
  Extensions dependency; any later XML contribution must separately follow the guarded XML rules.
- An active original triggers a bounded startup warning and best-effort ordinary placement-off
  behavior, preserving stored preferences and the original. Original API/consumer/facade and
  coexistence work are excluded. Unrecognized
  bar layouts do not imply a whole-megamod incompatibility; automatic clearance may be unavailable
  while admitted manual/native positioning remains usable.
- No upstream assembly, source helper dependency, other repository mod or Dev Gateway dependency.
  No source API identity spoofing, automatic settings import, save migration or broad compatibility claim.
