## Context

Owner **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **RimWorld 1.7 beta**.
S106/P2-36 replaces `gabrieel1482.QualityBasedDurability`, Workshop3798015624. About supports1.6;
native1.7 fallback selects1.6 plus root. All six bundled C# files and selected DLL behavior were
read. Assembly/file1.0.0.0, informational1.0.0, MVID5ccac812-5f82-49b6-a67d-fc02d7fda2f8.
[Research](../../../mods/megamod/docs/refinement/2026-11-05-quality-based-durability-research.md)
pins hashes, exact source/defaults, native paths and inspection limits. No implementation yet.

## Goals / Non-Goals

**Goals:** Preserve quality-dependent apparel damage loss and the seven source defaults; honestly
describe broad wear/damage coverage; use common settings, accurate item information and minimal
semantic admission; compose with owned quality/armor/processing features.

**Non-Goals:** Raising MaxHitPoints, repairing on quality changes, rewriting armor protection,
combat-only filtering, a new deterioration scheduler, custom damage rounding, a new item index,
source identity spoofing, copying source assemblies or importing its settings without a contract.

## Decisions

### One native damage contribution

Source patches `Thing.TakeDamage(DamageInfo)` with a prefix. It checks positive amount, `Apparel`
and `TryGetQuality`, then calls `dinfo.SetAmount(amount * multiplier)`. It does not test damage
type, instigator, wearer, faction, spawn state or `ExternalViolenceFor`. It does not patch maximum
health, quality assignment or armor ratings. Retain this once-per-event contribution on admitted
native behavior, adding safe eligibility/finite checks. No broad independent damage simulation.

The inspected1.7 original checks Destroyed/zero, applies matching `def.damageMultipliers` with
`Mathf.RoundToInt`, runs `PreApplyDamage(ref dinfo,out absorbed)`, then calls
`dinfo.Def.Worker.Apply(dinfo,this)` and ordinary notifications. A prefix is acceptable only after
semantic validation of this argument's effective use, not merely method existence. Scaling before
the native item multipliers retains source ordering, including intermediate rounding. Do not move
the multiplier to a final HP subtraction or multiply both the armor and wearer amounts.

Native `ArmorUtility.ApplyArmor` sends `GenMath.RoundRandom(damAmount * 0.25f)` to the apparel
before its separate protection roll. `DamageWorker.Apply` uses `GenMath.RoundRandom` on the
resulting amount, clamps loss to currentHP, subtracts and kills at zero. Preserve those draws,
ordering, absorption and results; do not add a minimum damage floor or saved fractional remainder.
Ordinary rounding may change random consumption when supplied a changed fractional value; no
cross-configuration seed-identical combat outcome is promised.

Zero configured multiplier produces ordinary zero damage and its normal early return, not a
manual HP restore after the worker already acted.100% is an exact no-contribution path. Invalid
nonfinite inputs/products and negative incoming values receive no replacement amount; retain
ordinary handling with a bounded diagnostic for invalid arithmetic. Validate before float
overflow conversion. Unknown quality resolves to neutral1, not an array index exception. Guard
`def.useHitPoints` for eligibility and item display; source lacks that explicit guard, so this
avoids advertising meaningless durability on non-durable modded apparel.

Every separate native event is eligible once; do not use a global last-item/event flag which
would suppress legitimate layered or nested damage. An external dispatcher which re-enters and
rescales the same event needs an explicitly admitted narrow adapter, not a speculative dedup cache.

### Preserve real wear, death and lifecycle paths

`Pawn_ApparelTracker.TakeWearoutDamageForDay` rounds `apparel.wearPerDay`, then sends positive
`DamageDefOf.Deterioration`; `ApparelTrackerTickRare` checks elapsed60000ticks and skips world
pawns. `SteadyEnvironmentEffects.DoDeteriorationDamage` sends1 deterioration damage; corpse
iteration can send it to worn apparel. `Notify_PawnKilled` on external violence chooses15%–40%
of each worn item's currentHP and sends that damage before ordinary apparel death notifications.
All reach the same hook and are intentionally covered. Do not add events where the game supplies none.

Native `CompQuality.SetQuality` changes quality and issues ordinary notifications; reading current
quality at damage/inspection avoids stale per-item values. No new ThingComp, condition rewrite,
saved multiplier or map lifecycle registration is needed. Source saves only globals. Existing
nativeHP/quality survives source removal; historical condition loss is not reconstructed.

### Share configuration and explain the actual quantity

Use seven common descriptors, ordered Awful→Legendary under Equipment / Apparel durability,
defaults1.5/1.25/1/0.85/0.7/0.5/0.25, range0–3 inclusive. Each row gets common global/save/Use
global load/store/validation/rendering. Resolve current save at use, globals when there is no game;
do not capture values in static item dictionaries. Invalid persisted entries use the built-in
quality default individually. Missing save overrides inherit as the common model requires; a
missing persisted global/explicit override value uses the described entry fallback.

Use global retains dormant overrides. Global reset resets only global values; save-section reset
returns that section to inheritance and clears its dormant values. Main-menu mutation callbacks
reject all save writes. Show0% as no loss through this damage path, not universal indestructibility;
show100% as neutral. These are ordinary balance options in mod settings, not new cheat actions or
a gameplay toggle. No extra bottom bar controls. Settings commits affect only later events.

The source adds a `StatDrawEntry` through an iterator postfix on `Thing.SpecialDisplayStats`, in
`StatCategoryDefOf.Apparel`, priority5000, using the item's multiplier as a percent. Preserve all
upstream entries and use one owned entry for an actual eligible item. This is not a new StatDef,
maximum-health factor or armor stat. Explain scope and ordinary downstream handling. Ensure native
info-card refresh/reopen sees current item/quality/settings and group status; any required refresh
hook needs its own target spec. Do not simulate damage to compute a tooltip. Seven languages are
required; source has English, Spanish and SpanishLatin only.

### Minimal executable admission and coherent fallback

Before implementation, create and index per-patch target documents from the megamod template.
Planned stable IDs, pending exact runtime binding:

| Planned patch | Required semantics | Failure unit |
| --- | --- | --- |
| `megamod.apparel-durability.damage` | Callable Thing/DamageInfo shape, real apparel/quality/HP eligibility and writable amount; effective original amount flows through ordinary multipliers/PreApplyDamage into exactly the intended damage worker and results; compatible prefix/worker/skip-original contracts | Damage plus active-rate display group |
| `megamod.apparel-durability.info` | Correct instance's effective SpecialDisplayStats enumeration reaches the displayed item info; existing entries preserved; one owned row; current quality/settings and admission are visible | Same group |
| Any necessary info refresh hook | Exact observed refresh callback and cache ownership only; no blanket map/stat cache invalidation | Same group if required for truthful display |

Validate original and effective semantics at startup. Compatible unrelated patches are allowed;
do not gate on hash, exact whole-method layout or no other Harmony owners. Relevant skip-original
prefixes, damage replacements, custom workers and effective transpilers must be understood. Do not
invoke foreign patch code speculatively. Any owned transpiler checks its actual incoming stream
each run and returns it unchanged on failure. Missing/ambiguous semantics and validator/install
exceptions fail the group closed. Roll back only this group's owned patches. Per skipped enabled
patch, warn once with package/patch/spec IDs, target, failed predicate, lost behavior and fallback.
Repeated initialization is idempotent. Supported later repatching revalidates and withdraws the
owned group if necessary; unobserved dynamic rewrites remain an explicit support boundary.

Original package detection is case-insensitive canonical-ID policy. When it is active, suppress
both owned hooks and explain provider ownership without mutating its settings or patches. No
claim is made that its original1.6 hooks themselves work correctly on1.7. Unknown custom paths
remain unclaimed; no silent heuristic interception of arbitrary damage/HP setters.

### Direct composition and economical data use

- S075 UnLimitedArmor changes allowed armor-rating range; this feature only changes item loss.
  Preserve native/provider protection calculations and avoid multiplying the pawn's injury again.
- S082 quality reworking owns its outcome/repair consequences. Read its resulting quality for
  later events without healing a second time or retaining the previous rate.
- S054 repair knowledge and S096 Micro Designations retain authorization/work/cost/consumption;
  recipe removal is not damage. S023 owns [on-site melee destruction](../specify-megamod-melee-item-destruction/specs/megamod-melee-item-destruction/spec.md),
  which receives this loss contribution once per actual hit. Retain its ten-completed-ineffective-
  cycle suspension, persistent low-water/count state and explicit Retry or genuine susceptibility/
  attack-change recovery.0% does not authorize an endless unchanged work loop, force damage or
  deletion. Do not add a second no-progress counter to this feature.
- S104 No Burn Metal changes ignition/flammability contribution, not this damage multiplier;
  actual fire damage reaching eligible apparel still uses quality. Neither feature forces fire.
- S009 comparison and later inventory/quality display consume current item data through their own
  supported surfaces. Do not invent an exact abstract-item lifetime, duplicate damage logic or
  widen those capabilities from this draft.

Native item/quality and common settings suffice. This feature has no periodic scan or owned Thing
index. Reuse shared settings/inspection invalidation if actually needed; do not add a second Ctrl+F
cache or broad event bus. Keep hot-path checks bounded and avoid allocation/log formatting when
inactive/neutral; measure before making performance claims.

## Risks / Trade-offs

- **Source wording understates scope:** broad daily wear and deterioration scaling is retained
  explicitly, with no combat-only option invented. Legendary25% can roughly quadruple life under
  simple equal damage exposure; real survival includes armor, rounding and other effects.
- **RNG and damage-provider differences:** retain native downstream behavior; inspect actual
  selected provider paths and verify causal native workflows before claiming compatibility.
- **0% or extreme custom tiers:** preserve source configuration and explain exact immunity scope;
  final playability compares ordinary, low and high quality, no unsupported balance claim.
- **Source is1.6 and unlicensed in inspected material:** independently implement behavior; retain
  provenance, no code/art copying, no assumption of source binary compatibility.
- **Info caching and partial activation:** inspect effective current-item route and keep active
  contribution/display coherent. All metadata/admission/runtime details stay pending until tested.

## Migration Plan

No code, dependencies or saved data change in this draft. Implement one reviewed behavior at a
time with focused TDD and native acceptance. Native itemHP/quality remain authoritative. Removal
leaves current condition alone and removes future owned damage scaling/display. Original globals
are not read or rewritten. Any later importer requires a separate backup/atomicity/restoration/
postcondition contract. Product packages never include upstream assemblies or the Dev Gateway.

## Open Questions

- **QBD-Q1:** Exact effective1.7 damage/worker/info/refresh contracts and supported later-repatch detection.
- **QBD-Q2:** Any actual third-party API/reflection consumers, custom damage providers and public license/source.
- **QBD-Q3:** Finalized apparel/material comparators, extreme multiplier playability and seven-language layouts.
- **QBD-Q4:** Focused runtime TDD, independent review, package checks and causal native acceptance evidence.

These are recorded research/acceptance work, not questions for the user to explain existing behavior.
