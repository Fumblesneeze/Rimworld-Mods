## Context

Owner: **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`, target **1.7 beta**.
The [source inventory](../../../mods/megamod/docs/refinement/2026-11-03-blunt-social-fights-research.md)
pins the sole source class, byte-identical public/installed 1.6 DLL, MIT provenance, eleven native
reference types and relevant Core XML. This is specification work; no runtime guard or beta
compatibility is established by decompilation.

## Goals / Non-Goals

**Goals:** reproduce the source's primary-sharp restraint during exact native SocialFighting,
including active Bloodlust exception, AND-only permission, current state and native consequences.

**Non-goals:** nonlethal combat, healing, suppressing every secondary damage effect, banning sharp
attacks during every mental break, new fights/attacks/weights, per-pawn policy or global settings,
source binary/package emulation and blanket support for unknown combat engines.

## Decisions

### 1. Keep one small contextual rule

Express the deterministic decision from already-known facts: prior permission, whether the caster
is a pawn, exact social state, active Bloodlust and primary Sharp classification. A prior false
result stays false. No pawn, different state or active Bloodlust returns the prior result. Otherwise
exclude primary Sharp and preserve other/absent classifications. Avoid broad service interfaces,
new saved state, cached permission or Thing-list scans for a decision on one attack.

The inspected source checks the attacker, not the opponent, faction or race. Its About description
is broader than its code; the Workshop description and code agree on social fights. Native
TraitSet.HasTrait excludes suppressed traits. Preserve native identity-based trait/Def resolution;
do not compare display names or add a custom trait hierarchy. Missing story data means no exception,
not an automatic permission grant or a null exception. Malformed provider data is an integration
failure distinct from a valid null primary-damage classification.

Alternative rejected: rewrite sharp DamageInfo to Blunt after selection. That would preserve the
wrong attack timing/animation, change armor/injury semantics and miss the source's eligibility rule.
Also reject blanket damage suppression, new lethality limits and a whitelist of claw/bite Def names.

### 2. Bridge the existing availability contract without replacing selection

Native `SocialInteractionUtility` checks pawn.verbTracker melee verbs through IsStillUsableBy and
uses adjusted damage times tool chanceFactor for selection. `Verb.IsStillUsableBy` consults virtual
Available, and native casting checks Available again before TryCastShot. An admitted narrow result
postfix is the initial candidate seam; keep the deterministic rule independent of engine types.
Do not replace the complete social job giver or general Pawn_MeleeVerbs selection when a safe
existing permission boundary suffices.

Preserve relative weights of the remaining social candidates, ordinary usability and no-job behavior
for an empty result. Do not import equipment/apparel/hediff/general-combat pools into social fighting
or make a fallback punch. Do not filter pre-fight initiation as if the pawn were already in the state:
source sharp-only participants may enter and then find no permitted attack. Ordinary native recovery
still runs. No busy retry, newly invented timeout, forced recovery or error per tick is needed.

The rule concerns primary DamageDef armor category, including projectile primary damage for an
otherwise supported permission call. It does not add ranged attacks to native social jobs. Additional
tool/surprise effects and independent damage remain native. A valid absent primary DamageDef or
armor category is non-Sharp for this rule, not a reason to disable all such attacks.

### 3. Minimal semantic admission still needs effective callers

Before code, create the stable per-patch target document from the megamod template. Pin required
callable shape for instance bool Verb.Available(), CasterPawn access, current MentalStateDef,
TraitSet.HasTrait and damage lookup. Verify the Core SocialFighting/Bloodlust/Sharp identities and
the state/trait semantics actually consumed. Hashes and exact versions remain evidence identities,
not runtime compatibility predicates.

This is a result filter, not argument rewriting: the original availability body need not match a
whole expected body. However, name/existence alone does not prove that social candidate selection
and execution honor the result. Establish the minimal effective caller/control-flow prerequisites
for the supported native path, including IsStillUsableBy and the execution check. Account for virtual
overrides that bypass the base implementation, skip-original prefixes, postfixes that change the
final answer, and transpilers that remove the consuming check. Allow compatible restrictions and
unrelated edits. Do not use a blanket no-other-owner condition or execute foreign transpilers as a
probe. A later repatch must invalidate/revalidate affected owned admission.

If a single admitted hook cannot cover a specific supported provider, inspect and specify a narrow
package-gated adapter or leave that provider unavailable. Any new hook gets its own target document;
declare atomic groups if more than one is necessary for a claimed path. Roll back only newly added
owned patches on installation failure. Warn once per skipped enabled patch with spec/target/reason/
behavior/fallback, using the foundation protected startup diagnostic path. Do not log every denied
attack or reproduce the source's unconditional success message.

No XML change is needed: the permission depends on current attacker state, and changing a static
DamageDef or race tool would alter ordinary combat. Any later declarative work must use the required
XML Extensions content/package checks and preserve unrelated lists, with dependency metadata.

### 4. No added UI, configuration or serialized state

The original offers no parameter or UI. Keep this a normal admitted behavior without inventing a
settings page, bottom-right toggle, per-pawn exception, debug panel or bottom tab. Common settings
inheritance has nothing to resolve for this slice; this is not a global-only exception to that rule.
Any future configurable choice needs its own observable contract and the common settings mechanism.

Current native pawn state and trait suppression are read at use time. Save/load requires no new
record; removing the feature leaves native injuries and mental-state/partner data intact. Do not
serialize Verb/pawn references or migrate original configuration when none exists. Existing localized
game feedback stays native; any new shared compatibility explanation must support all seven baseline
languages without adding redundant UI.

### 5. Explicit co-installation and consumer limits

When canonical original package `dani.BluntSocialFighting` is active, suppress owned duplication
and warn without unpatching it. Merely finding Workshop files does not activate this policy. The
inspected original advertises only through 1.6; do not assume its DLL has been selected or is valid
under beta merely because its package is enabled.

The installed text-reference scan found only source metadata and RimPy's catalogue. Public
IsAvailablePatch exists, but no callable consumer was demonstrated. Do not create a source-named
assembly/type facade or claim package drop-in compatibility without an actual binding. Unknown
binary/reflection consumers remain SF-Q2. Optional race/attack/gene/combat providers require their
canonical package and consumed shape, absent-safe resolution and separate acceptance runs.

Existing owned social, medical and combat behavior composes directly. No self-patch layer, duplicated
Thing index or runtime dependency on another repository mod is introduced. The source MIT notice
must be retained if substantial code is ever reused; independent implementation is the planned route.

### 6. Verify eligibility and gameplay separately

Unit tests cover the public pure rule's truth table, false preservation, both fighters independently,
active versus suppressed Bloodlust, non-pawn/missing-story cases, absent primary classification and
state transitions. Do not load Harmony/representative Def databases into the Unit tier. Owned patch
ordering/shape/rollback tests belong in the honest Harmony tier with guaranteed cleanup. Real Defs,
active providers and the native job/cast path require isolated game verification.

Native acceptance on the reviewed build needs actual fights and attacks: ordinary native interaction
or the native developer social-fight action can initiate a disposable fixture; pawns then perform
the real social job and strikes. Do not directly call the predicate or apply injury and claim that
proves selection. Retain native animation/battle log/Health before-action-after evidence for sharp
exclusion, surviving blunt damage and Bloodlust asymmetry. Prepare appropriately controlled candidate
sets to make selected attacks observable without replacing the job/damage result with synthetic code.

Also exercise ordinary combat outside the state, no-permitted-attack recovery, active/suppressed
trait changes, mid-fight save/load, and supported custom-primary/secondary damage boundaries. Use
exact original/provider-present groups separately. Preserve player configuration hashes, process
ownership, cleanup, build identity and personally inspected causal captures. Source inspection and
host tests alone cannot accept gameplay or beta/provider compatibility.

## Risks / Trade-offs

- Restraint reduces but does not eliminate serious injuries → document primary-only scope and
  preserve real blunt/Bloodlust/secondary damage, without misleading nonlethal claims.
- A sharp-only pawn can enter a fight but have no legal attack → preserve bounded native no-job and
  recovery; do not solve this by granting attacks or silently changing initiation.
- Custom virtual/provider paths bypass Available → specific semantic adapter or explicit unavailability,
  never blanket compatibility inferred from the base method.
- Later patches can restore a denied attack → effective-chain admission/revalidation and isolated
  provider proof; do not remove another owner's patch to force a passing result.

## Migration Plan

No deployment or migration occurs for this draft. Future implementation adds no owned saved data
or source-setting import. Turning off/uninstalling the product restores ordinary attack availability;
existing native wounds are not healed or removed. Package validation excludes source binaries,
Gateway, test fixtures and other repository mods; eagerly used Harmony remains required.

## Open Questions

SF-Q1–3 in the [progress ledger](../../../mods/megamod/docs/refinement/specification-progress.md)
retain exact effective admission, optional consumers/providers and future implementation/native
acceptance. No questions ask the user to explain existing source behavior.
