# Better Message Placement (Continued)

Owner **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`; **1.7 beta**.
Origin replacement, S071. **Observable draft independently reviewed and validated. Nothing
implemented or verified.**

Source **Mlie.BetterMessagePlacement**, Workshop **2259516339**, maintainer Mlie / author Dyrewulfe.
Inspected **2026-11-04**: selected 1.7 DLL, manifest1.7.0, assembly1.7.0.0; MIT credits D. R. Gunter
(2019). [Research and exact hashes](../refinement/2026-11-04-better-message-placement-research.md)
separate static findings from unperformed native verification. No source binary/code/art was copied
into the product or executed.

Owning [proposal](../../../../openspec/changes/specify-megamod-message-placement/proposal.md),
[design](../../../../openspec/changes/specify-megamod-message-placement/design.md),
[observable spec](../../../../openspec/changes/specify-megamod-message-placement/specs/megamod-message-placement/spec.md)
and [tasks](../../../../openspec/changes/specify-megamod-message-placement/tasks.md).

## Behavior inventory and coverage

All rows are **specified only**; no implementation, target document, test or live acceptance exists.
Requirement names in the linked spec define the complete behavior and scenarios.

| ID / requirement | Input → observable result | Source deviation / remaining limit |
| --- | --- | --- |
| BMP-01 Automatic placement follows the visible colonist bar | Native short message → below current visible content, ordinarily four UI units clear | Actual rows/labels/weapons/frames/outlines; hidden bar reserves nothing; no doubled native origin |
| BMP-02 Manual offsets are relative to automatic placement and use the visible UI | Scoped 0/0, ±100% pair → one relative adjustment on next display | Logical viewport replaces physical preference dimensions; intentional overlap allowed; atomic validation |
| BMP-03 Viewport fitting keeps native stack order and defines overflow | Off-screen preferred position → whole-stack fit, or newest-priority overflow | Explicit correction to unbounded source offsets; fitting can overlap bar; no new wrapping/spacing/lifetime policy |
| BMP-04 Relocated messages retain native behavior and interactions | Actual message → same content/archive/sound/lifetime and displayed hitbox/action | Native short messages only; not letters, alerts, logs or combat text |
| BMP-05 Position configuration uses common scopes and existing settings navigation | Common global/save pair, inheritance/reset/reload → correct effective position | Source is global-only; no source import, bottom tab, toggle or per-entity setting |
| BMP-06 Preview shows effective positioning without posting a message | Settings edit → labeled scaled viewport sample | No historical test message, sound or target; independent coordinate detection replaces sum comparison |
| BMP-07 Compatibility selects one provider and fails only unsupported contributions | Admission/provider selection → supported contribution or explained native/manual fallback | Source original remains sole authority when active; no partial placement or whole-product LTO ban |

## Interactions and composed result

Use current native bar layout and S001's effective visible presentation through owned calls, including
hidden/reduced entries, labels/health strips, indicators/outlines and adjustable weapon bounds. Hidden
native portrait rectangles are not added back; separately visible group frames still count. Consume
the prepared visible presentation without duplicating filter decisions or changing S001 update timing.
Messages draw before the native bar, so previous-frame-only geometry is insufficient. No self-patches,
duplicated colonist census, private map/Thing scan or new shared-index consumer is justified. Keep
native message rectangles accurate for bar overlap/fading and target actions. Combined costs are
unmeasured; no performance improvement is claimed.

The placement policy does not manage arbitrary HUD windows. Queued RimHUD/RPG inventory work is
not grounds for speculative global collision avoidance. Unknown/changed bar geometry can disable
automatic clearance while admitted offsets still use the native anchor. Native position-path failure
disables owned placement entirely, retaining preferences and ordinary messages.

## UI and settings audit

| Source surface | Purpose | Owned destination / deviation |
| --- | --- | --- |
| Two global offset sliders and reset | Position configuration | One common Interface / Messages compound row, scoped pair and explicit reset scope |
| SilentInput test message after slider sum changes | Placement feedback | Inert labeled effective-scope viewport preview inside settings; no archive pollution |
| Ordinary native message background/text | Read, hover, navigate and dismiss | Same native message surface and interactions at its relocated position |
| VersionFromManifest footer | Source version presentation | No helper dependency; ordinary megamod version presentation |

Defaults **0%/0%**, finite range **−100%..+100%**, four-unit default bar clearance. Global/save/Use
global/dormant override/main-menu callback rules apply to the pair. Save reset requires an enabled
override and preserves inheritance state; global reset changes only the default. Preview names its
effective scope; global edits under an override do not move that save's preview. Valid settings apply
next display without resetting message timers. Seven-language UI remains to implement/verify.

## Contracts and providers

| Evidence / binding | Selected treatment | Unverified boundary |
| --- | --- | --- |
| Native Messages/Message posting, archive, targets and rectangles | Preserve native interfaces/behavior and relocate once | Exact effective integration and Unity geometry/input require native verification |
| Source settings/helper types and xOffsetPercent/yOffsetPercent config | No facade or config import selected | Bounded installed XML/CS/JSON search found no actual runtime API consumer; not an all-binary audit |
| Active Mlie.BetterMessagePlacement | Original sole provider, owned placement dormant and preferences retained | Source double-origin/row/offset behavior remains under its authority; owned preview unavailable |
| Source metadata marks DerekBickley.LTOColonyGroupsFinal incompatible | Do not copy a whole-megamod flag; exact geometry adapter needed | Actual provider implementation not found/analyzed; no automatic-clearance support claim |
| Other compatible bar/message providers | Minimal consumed semantic checks, narrow adapter only for proved contract | No rejection merely because a foreign Harmony owner exists; no universal provider support |

No native message/save payload conversion. Automatic migration/import, source assembly identity,
arbitrary third-party bindings and safe removal of source-specific data are not promised. Any later
migration needs backup, atomicity, restoration and postcondition verification. No original-mod
configuration writes or foreign unpatching.

Harmony becomes required at implementation. Original/bar providers and DLC are optional; no selected
XML change or new XML Extensions dependency. No source binary/helper, other repository mod or Gateway
dependency. Every actual patch needs its semantic target document, actual effective/incoming validation,
unchanged-stream failure, owned-only rollback and bounded warning. Static source hashes identify
evidence; they are not runtime admission gates.

## Verification and remaining work

BMP-Q1–4 retain exact admission/geometry, provisional fitting/preview choices, real providers/consumers
and focused tests/languages/cost/native acceptance. Final evidence must produce a message through an
ordinary native player workflow and inspect its displayed placement plus hover/click/dismiss result,
across required view/layout/settings/provider cases on the reviewed build. Preview, direct injected
message/state, clean logs and installed patches alone cannot accept that behavior.

Semantic pass: message generation, significance, sound, archive, expiry, quest/game outcomes and
progression remain native. The feature changes readability/navigation only. Very large text or small
screens can still clip/overlap; no silent message deletion or inflated lifetime solves that constraint.
No gameplay task is complete at this documentation stage.

Independent source/native review corrected clearance wording for messages without visible backgrounds.
Composition review corrected S001 bounds to cover hidden/reduced entries and adjustable weapons;
focused re-review found no remaining P1/P2. Typed strict validation passed **93 items, zero failures**.
All **24 implementation tasks remain unchecked**; this is document validation, not runtime evidence.
