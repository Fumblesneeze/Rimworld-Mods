# Original: Cooking-dependent slaughter recovery

## Identity and intent

- Owner **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`; target1.7 beta.
- Origin **original**, S118/P2-46/P2-47. No upstream source mod or binary contract is invented.
- User direction: Cooking contributes to slaughter effectiveness, default neutral level10; skill
  changes yield as well as duration, earned through slaughter while native base yields stay intact.
- [Refinement](../refinement/2026-11-06-slaughter-yield-refinement.md) separates confirmed intent
  from proposed formula/bounds/product/scope choices and records current native evidence.
- The earlier global doubled-base/skill16 suggestion is superseded. No source implementation is
  copied; [S117](skilled-slaughtering.md) supplies the independently specified timing contribution.

## Behavior inventory

All rows are **specified, not implemented or verified** in the
[owned contract](../../../../openspec/changes/specify-megamod-slaughter-yield/specs/megamod-slaughter-yield/spec.md).

| ID | Observable behavior | Boundary / decision |
| --- | --- | --- |
| SY01 | Cooking contributes to coordinated slaughter speed | Proposed75/100/125% at0/10/20; one S117 snapshot, no duplicated factors |
| SY02 | Deliberate slaughter earns a factor on its actual corpse | Designated/auto origin; no mental/hunting/companion/euthanasia/unrelated-death/legacy inference |
| SY03 | Animals/Melee/Cooking determine bounded recovery | Proposed normalized skills,50% influence,50–200% recovery; work speed/capacities excluded from this extra factor |
| SY04 | Later real meat/leather quantities reflect preparation | Before ordinary rounding; native carcass/butcher effects retained; no extra trophies/organs or whole-recipe multiplier |
| SY05 | Physical recipe production and pure inspection | One normal consumption/iteration; actual counts/stack/carry rules; previews do not generate or consume |
| SY06 | Stable earned record through save/transfer and worker/settings changes | Corpse-owned, not pawn/Def/map-owned; resurrection/destruction do not carry it forward |
| SY07 | Six common inheritable tuning rows | Future-attempt timing, dormant overrides, validated inputs and main-menu disabled save actions |
| SY08 | Contextual current/earned information | Native pawn/corpse surfaces, input-dependent recipe previews and seven languages |
| SY09 | Coherent target admission and optional fallback | Requires owned S117; source-selected/unknown pipelines do not receive guessed bonuses; valid dormant records retained |

## Direct composition and UI audit

S117 calls the shared Cooking/recovery policy and carries pending provenance directly. No patches
against owned helpers and no duplicate job ledger. This addition does not convert every native cut
or animal death into deliberate preparation. S008/S065 finishing/hunting remain outside yield scope.

The common settings section owns six global/save rows; per-corpse preparation and active-attempt
snapshot are gameplay facts, not entity settings. Native pawn information shows current predicted
proficiency; corpse information shows its earned factor and later butcher/carcass dependencies.
No new tab, operational toggle or unrestricted override. S010 stays honest about variable recipe
outputs; never enumerate native product generation to preview a body.

Native recipe completion handles physical outputs and consumes the corpse. S014/S017 counting,
S021 hauling and S033 shared Thing facts receive normal production events; there is no extra corpse
index, speculative resource credit, standalone bonus batch or unlimited hauling container.

## Compatibility and lifecycle contracts

| Surface | Planned contract / limitations |
| --- | --- |
| S117 SlaughteringSpeed | Cooking composes directly before final minimum; other valid speed contributors remain timing effects, not automatic extra yield modifiers |
| Native meat/leather amounts | Base values/stat modifiers remain; factor enters the actual two output quantities before native rounding |
| Generic/discrete byproducts | Native counts and body-part effects unchanged; no invented extra horns, organs or trophies |
| Corpse record | Versioned exact-body state, current record stays earned through transfer/load; worker death/config changes do not recalculate |
| Source/other providers | Original selected by S117 disables S118 overlap; nonstandard processors/corpse stacks need concrete guarded adapters |
| Old saves/removal | Missing records neutral; no automatic legacy inference, retroactive reward, source import or unsupported safe-removal promise |

Harmony and S117's guarded XML Extensions use are required with actual implementation. No upstream,
DLC, other repository product or Gateway dependency. No upstream facade/license claim or art copied.

## Implementation, deviations and open decisions

| Scope | Implemented here | Missing / proposed |
| --- | --- | --- |
| SY01–09 | Nothing | All code, targets, common settings/UI/localization, persistence, TDD and native proof |
| Balance | Nothing | Concrete tunable defaults and static rat/cow/thrumbo expectations; proposed, not finalized game measurements |
| Product/origin scope | Nothing | Meat/leather and deliberate nonmental slaughter selected as draft; SY-Q1 retains review |
| Other processors/products | Nothing | Explicit optional research/adapters where warranted; no universal compatibility or whole-megamod acceptance |

## Verification

[Ledger](../refinement/specification-progress.md) tracks SY-Q1–5 and document review/validation.
Future proof requires native deliberate slaughter with different skilled handlers, inspection of
the resulting real corpses, later butcher bills with separate butchers and observed physical counts.
Include excluded kills, injuries/partial consumption, rare discrete products, snapshot changes,
save/reload/caravan/container transfer, worker departure, resurrection, defaults/overrides/main-menu
scope, optional/fallback and seven-language cases. Use a fresh minimized isolated reviewed build,
personally inspect causal before/action/after views and retain build/mod order, actions/screenshots,
config hashes and cleanup. No direct marker assignment or generated end-state screenshot accepts it.

## Reconciled user decision — 2026-11-07

The approximately60% additional recovery at mastered skills is accepted. The existing normalized
curve remains160.5406% at all20 and100% at all10, with unchanged animal base yields and ordinary
butcher efficiency applied separately. Remaining tuning and live playability are unverified.
