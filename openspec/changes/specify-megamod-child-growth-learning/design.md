## Context

Owner **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
S088/P2-17, `jdalt.gsmsn`, Workshop2969141187. Complete included source and latest shipped1.5 DLL
agree on a single growth-rate postfix. [Research](../../../mods/megamod/docs/refinement/2026-11-05-child-growth-learning-research.md)
pins source/binary/native evidence and bounded installed consumers. This is documentation only.

## Goals / Non-Goals

**Goals:** preserve the source's one prospective multiplier and native rate contract; keep actual
accumulation, displayed rate, growth vats and supported external consumers consistent; admit the
hook semantically and handle invalid numerical inputs without an invalid owned rate.

**Non-goals:** change learning-need satisfaction, skill XP, toddler learning, age/growth-vat speed,
growth tiers or rewards, generated history, plant growth, shared Thing indexes, source API emulation,
new settings/UI frameworks or accepted save/provider compatibility.

## Decisions

### Adapt one native result, not each consumer

Candidate seam is `Verse.Pawn_AgeTracker.GrowthPointsPerDay` getter. Read the owning pawn and its
effective postprocessed `StatDefOf.GlobalLearningFactor`; return ordinary incoming rate R times
factor L once. Keep supported compatible prior contributions in R and L. Do not patch
`Need_Learning.NeedInterval`, the gizmo, each school feature or our own toddler code to reapply L.
Do not multiply the accumulated `growthPoints` field, native simulation helper or stat itself.

The source's public postfix sits inside an internal class and no executable source-API consumer was
found in the bounded text scan. Native getter identity is the real shared contract: current native
Need_Learning and growth gizmo read it, and installed School & Learning source reflects it for its
school award. Preserve that getter rather than spoof `jdalt.gsmsn` or invent a compatibility facade.
The external school calculation is inspected source evidence, not verified current binary support.

### Keep the native branches and timing

Inspected1.7.4998 rev1299 getter: without Biotech return0; a `Building_GrowthVat` holder returns2;
otherwise suspended pawn returns0; an existing unsuspended Need_Learning returns current learning
level times native age factor (.75 below7, otherwise1) times ChildAgingMultiplier; otherwise0.
The vat branch precedes ordinary suspension. Do not narrow the postfix to free colonists or
ordinary on-map children, or remove the source's vat-rate effect. Eligibility belongs to consumers.

Native Need_Learning applies `rate*.0025` during its normal150-tick need interval when
`canGainGrowthPoints` is true. The source's cached stat query uses `cacheStaleAfterTicks:240`.
Retain at most that query staleness without a second mod-owned cache; credit still follows the
ordinary caller cadence. Repeated UI reads must be pure, not accumulate a scaled result or award
points. No separate per-pawn rate persistence or map index is justified.

Growth birthdays and tier selection are native. The current thresholds are0,30,55,80,100,120,135,
150,162; birthdays7,10,13. These are evidence, not new hardcoded mod policy or fixed-version guards.
Native `TrySimulateGrowthPoints` uses its private learning-level helper and therefore bypasses the
patched getter; retain that source boundary, including generated/history initialization. Existing
saved points and pending choices need no conversion or retrospective application.

S031 toddler walking/manipulation tracks biological progress independently, including its separate
vat correction; do not apply L there. S103 Need Bar Overflow may change a consumed learning level;
an old conditional1.4 learning patch in its installed source is not proof of current1.7 behavior.
Research its selected branch under S103 and compose any owned policy directly. No inferred conflict.
S088 has no plant-growth function: correct the prior naming-based S043/S087 forward references.

### Finite result policy is explicit

For finite R>=0 and finite L>=0 with a representable product, return R*L. R=0 can return immediately
without asking the stat system; L=0 is valid and stops future gain. If L is invalid or the product
overflows, retain valid R. If R itself is negative/non-finite, return0. Emit a bounded per-feature
diagnostic on fallback, not per query/tick or per UI frame. Do not invent a max factor or clamp all
ordinary bonuses to100%; native reward limits remain unchanged.

The source has no finite guards. The proposed correction prevents an invalid owned **rate**; it
does not promise to repair externally corrupted counters or every provider's own direct writes.
GLF's Core StatDef minimum is0, but supported providers still need effective validation. CLG-Q2
must cover representable rate and accumulation boundaries in actual consumers; reject unsupported
numerical semantics rather than silently broaden the acceptance claim.

### Native UI and feature availability

The existing `Gizmo_GrowthTier` tooltip reads this rate only when it ordinarily shows progress and
gain; maximum-tier presentation and pawn visibility remain native. No new UI entry, settings row,
feature-specific toggle or global/save setting is needed. Pawn growth points are gameplay state.
Reuse native localized labels/rounding. Any new feature-unavailable explanation uses the common
status presentation and all seven languages, not an unrelated bottom tab.

Biotech is required for the feature but optional for the megamod. Absence is an expected inactive
content scope, not a patch failure or excuse to make the whole package unusable. Core and external
Harmony are required when eagerly using Harmony. The source About declares Biotech but omits
Harmony despite its binary reference; do not copy that metadata gap. No XML operation is proposed;
if one later becomes necessary, add XML Extensions and its guarded content/package/shape contract
with first use. Ship no source/private Harmony/Gateway assembly or other repository mod dependency.

### Per-patch target contract and coexistence

Before implementation, register a stable target document and executable predicate for the actual
hook. This changes a result, so a matching getter name alone is insufficient: prove owning-pawn
access, units of points per simulated day, Biotech/vat/suspension branches, the consumed stat shape,
and that native consumers use the rate once for their ordinary credits/displays. Prove Global
Learning Factor is not already incorporated by the effective pipeline before adding it again.

Allow benign edits and compatible rate/stat providers. Inspect relevant effective code and
skip-original interactions; no assembly-hash equality or blanket rejection of other Harmony owners.
Never replay foreign transpilers speculatively. Any actual transpiler must validate its incoming
stream each invocation, returning it unchanged on failure. Install idempotently; if admission or
installation fails, remove only newly installed owned patches in the affected group. Revalidate
supported later repatches. Each skipped enabled patch gets a bounded startup warning with package,
patch/spec ID, target, failed predicate, lost behavior and fallback; no ticking warning flood.

Active `jdalt.gsmsn` suppresses the owned overlap whenever that original provider is loaded. Other providers
which already perform the same scaling need exact recognized composition, not guessed factors or
inverse division. Unsupported/ambiguous cases withhold the owned patch, leaving foreign owners
untouched. An optional C# integration is package-ID/Def-resolved, absent-safe and shape-guarded.

## Risks / Trade-offs

- High/low learning factors alter the chance of reaching a good birthday tier → source-derived
  stat factors are explicit; compare current finalized traits/genes, learning conditions and vat
  paths before balance acceptance, without adding unsupported reward rebalance.
- A rate getter can be queried more often by UI or other mods → pure result calculation, native
  bounded stat cache and no repeated multiplication/awards or per-query logging.
- Another rate provider may already scale learning → semantic admission and known-provider policy;
  preserve compatible consumers, disable only the owned overlap when it cannot be proved.
- Native historical simulation bypasses the getter → deliberate prospective-only contract, not
  an accidental promise to recompute generated children or compensate existing saves.

## Migration Plan

No source settings or custom save data exist to import. Preserve native growthPoints and choice
state; valid save/load uses ordinary native serialization. No source package spoof or migration
guarantee follows from this small state footprint. Exact source-present/source-absent round trips
remain acceptance work; a future actual migration needs backup, atomicity and restoration rules.

## Open Questions

CLG-Q1: finalized balance across traits/genes, learning and vats. CLG-Q2: exact effective target,
cache/accumulation/numeric and consumer semantics. CLG-Q3: actual optional consumers, source coexistence
and S031/S103 boundaries. CLG-Q4: implementation, localized status, focused tests/review/package and
reviewed-build native player workflows. No question asks the user to explain existing mod behavior.
