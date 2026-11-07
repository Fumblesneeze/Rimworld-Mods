## Context

Owner **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), target **1.7 beta**.
S083 replaces `Argotis.EnemiesUseApparelToo`, Workshop3746069759. Installed support is 1.6, not
1.7; the installed DLL matches the committed DLL in public source revision
`19c1401d7533307639388100040a230340954045`. This establishes artifact identity, not a reproducible
source build or beta compatibility. [Research](../../../mods/megamod/docs/refinement/2026-11-04-enemy-apparel-research.md)
records all inspected source/XML, compiled checks, current native evidence and omissions.
This change is specification only; every implementation and acceptance task remains open.

## Goals / Non-Goals

**Goals:** competent non-player use of genuinely worn combat equipment; one physical action and cost;
legal tactical jumps; additive loadout variety; optional, explicit Ideology talent behavior; common
settings and native combat feedback. Keep source provenance without importing source defects.

**Non-goals:** a generic arbitrary-verb AI, new equipment content, automatically invented commander
roles, emergency explosion jumps, source-save migration, a parallel ability/charge UI or separate
Thing cache. No player combat automation is added. No product dependency on another repository mod.

## Decisions

### 1. Integrate one supported action with native combat

The source prefix selects the first Available, violent, geometrically reachable apparel verb and
returns a one-use `UseVerbOnThing` job, expiry2000. That bypasses stricter lance target validation and
the ordinary ability-choice path. Its duplicate XML ability representations are selected by a
global two-toggle expression rather than a coherent behavior setting. Do not reproduce that switch.

Keep an explicit supported-equipment catalog; bind each action to its exact worn instance and
ordinary cast/cost path. Prefer the existing item verb/effect over copied AbilityDef descriptions,
durations and target checks. Where a native equipment action is not AI-callable, use a narrow owned
adapter with a documented effect/charge commitment boundary. A mere violent flag or Def name is
insufficient for an unknown provider. Manual player controls remain native and do not gain a second
command. Stable item/action ordering follows the current worn-item order and action order; avoid a
new utility-scoring engine before a concrete requirement exists.

The 1.7 `JobGiver_AIFightEnemy.GetAbilityJob(Pawn, Thing)` is static; source prefix requests
`__instance`. More importantly, the caller restricts actor/control eligibility and native ability
selection checks casting-position availability. An eventual bridge must prove those effective
semantics, not just find the old name. Apply explicit non-player/control gates before selection,
including allied defense, never create hostility. Current target validation and one-use lifecycle
must survive skipped originals and compatible provider changes. Keep the ordinary weapon/ability
fallback and do not loop on an impossible first candidate.

Recheck targeting and exact equipment before native commitment. No UI query may synchronize or
spend a second charge meter. Preserve native failure costs: inspected JumpUtility spends a charge
before attempting flyer creation, so failed flyer creation is not automatically a free refund.
Account for interruption, save/load, projectile flight, exhausted-item destruction, manual looting
and multiple equipment sources. Do not replay a cast merely because it reloads or loses admission.

### 2. Tactical jumps reuse native candidates and current item facts

The source inserts a noncolonist jump node after mental-state handling and selects the first
CastJump ability. Its cached range and partly unchecked target/landing paths can go stale or fail.
Use the actual equipped jump source and current stat value, with native landing/LOS restrictions.
No walking-path requirement should prohibit a legal jump over an obstacle. Preserve the specified
injury/threat/search/displacement thresholds as named policy values; they are not health percentages.

Use native attack-target and shooting/retreat candidate finders, bounded by the selected item's
current range. Do not build another map-wide enemy/Thing scan or persist target eligibility. Only
after a valid useful landing exists should a new job replace movement. The owned fallback deliberately
considers retreat when a proposed shooting landing is invalid, not only when native shooting-position
search returns false. Source fallback calls `isThreatend(pawn, 30f, 0)`; current native
`EnemiesAreNearby` returns `count >= maxCount`, so zero admits a fallback without any nearby threat.
The owned rule explicitly requires at least one threat within30. Avoid null-target dereferences and
repeated impossible requests. Source
explosion-escape and turret-position prototypes have no active XML consumer; do not enable them
merely because their classes exist.

S082 quality changes invalidate/recompute jump range through the same current stat observation;
S081 burst retargeting owns ordinary supported burst transfers, not a duplicate apparel cast.
S051 specialist work autocast remains separate from NPC combat talents. Compose owned policies
directly, never Harmony-patch one owned feature from another.

### 3. Deployment follows the real projectile and faction lifecycle

Hunter/turret packs use their actual supported projectile and actor behavior. Do not substitute an
instant spawn or copy a stale actor lifetime. Deadlife's source ranged wrapper is an intentional
extension rather than proof that a native manual pack already behaves that way. Its helper marks
corpses/pawns at the intended destination during ability application, before the real projectile
release. That can claim actors even if the capsule misses or fails; the owned contract binds any
marking to the actual release/affected content.

Anomaly data is absent in this installation. Validate the current pack, projectile, gas/faction
marking, charge and resurrection contracts in a genuine DLC-present environment before admitting
that branch. Do not guess that a missing search result is a renamed Def, or infer behavior from
assembly types alone. Until that work exists, fail locally with the specified fallback for enabled
present-but-unsupported content; optional absent DLC is quiet. Source deadlife values remain explicit
provisional baselines. Keep every other native item's current warmup, scatter, duration and cost.

### 4. Add candidates without mutating whole generation pools

Source XML adds armor/utility tags, missing-only allow chances and specific torso alternatives. The
native required-apparel path tests each alternative chance per candidate, then selects among eligible
candidates; 50% is not the probability of a final raid wearing that armor. It subtracts the selected
price without enforcing a simple remaining-budget cap in that path. Preserve native semantics rather
than inventing a hard budget limit or advertised equipment share.

Owned per-save configuration cannot safely be represented by globally rewriting Def lists on every
save switch. Use guarded declarative registrations for the supported catalog and a small native
generation adapter that composes candidates for that generation under the current effective policy.
Do not temporarily mutate shared lists, add a second torso requirement, replace required provider
apparel or reroll a finished pawn. Where declarative tags alone would cause disabled-save leakage,
keep owned markers inert until the adapter admits them; test both inherited and overridden saves.

Preserve native/provider candidate entries, explicit allow chances, body/layer/material/style rules,
actual charges/quality/loot and the specified cross-DLC gates. S004 weather/hazard readiness governs
the final loadout, and S078 staged generation publishes a group only once after all owned preparation.
Candidate enrichment, equipment and talent rolls must not repeat on save/load or continuation.

Native specific-apparel selection precedes ordinary `apparelRequired`; overlapping ordinary required
items are skipped. This is how source alternatives replace cataphract armor, champion plate armor
and guild elite marine armor. Explicitly admit only these specified native torso substitutions, keeping
native fallback when an alternative is not selected and preserving other required pieces. Additional
provider locks/custom requirements and hazard protection still win. Inspect those minimal relationships
rather than requiring the entire PawnKindDef to match a hash. Odyssey source patches target abstract
`TradersGuildEliteBase` and `TradersGuildSlasherBase`; current `Salvager_Elite` and `Salvager_Scrapper`
inherit them and are explicitly included in owned scope, not silently narrowed to named guild pawns.

### 5. Optional talents are not new implant content

Source Ideology patches enable native BerserkTrance/CombatCommand AI and add a 35% tribal implant
pool path with fallback budget100–200. Three hidden implant Things, brain surgery recipes and grant
hediffs exist, but commander/marksman recipients have no corresponding generation rule. Do not
silently activate these advertised-but-unwired grants. Actual possessed supported commands may run
under ordinary native AI eligibility; the owned tribal grant is a separate 35% opportunity preserving
unrelated implant probabilities and money. This is an intentional observable deviation, not a claim
that source final implant shares are exactly35%.

Use a minimal pawn-owned grant/cooldown record or native ability ownership mechanism once current
save/downing behavior is established. No physical implant, Plasteel recipe or generic talent framework.
Remove only the owned cast grant on defeat. Source sets Recruitable=false and does not restore it;
the draft explicitly retains this exclusion after defeat, without periodically overwriting explicit
external recruitment overrides. Show a localized reason. Existing genuine role abilities and unrelated
implant/recruitment restrictions remain owned by their providers. Current trance/aura effects settle
under native termination rules; disabling new automation does not delete committed effects.

### 6. Common settings, UI and contracts

Four behavior settings replace the source implementation switches: offensive use, tactical jumps,
expanded loadouts (On) and Ideology talents (Off). Source field initialization says Ideology On but
Scribe missing-key default says Off; choose explicit Off. All use the shared definition/load/store/
effective-value/rendering path, dynamic global inheritance and dormant per-save overrides. New
decision/generation timing is specified; no restart or global Def mutation is required for a save
override. Main-menu guards cover resets/callbacks, not just a grey control. Numeric baselines remain
named policy values until configurable numeric rows are justified and specified.

No bottom tab or new right-side live toggle. Configuration belongs in Combat / Non-player equipment;
actual equipment charges, cast effects and talent/recruitment state remain native contextual displays.
Source English strings and empty LanguageData are not localization coverage; seven languages are due.

Bounded installed source/text/binary searches found no incoming consumer beyond EUAT itself. Its
public ability/charge helpers and advertised XML extensibility are recorded, but do not justify
spoofing `Argotis.EnemiesUseApparelToo` or preserving every class. A proven consumer can later receive
a narrow adapter. No automatic source settings/job/ability migration or safe mid-save assembly
removal is claimed. Original EUAT owns new overlap if active; suppress owned duplicate decisions,
generation and talent grants while safely resolving existing owned state.

### 7. Every actual bridge has executable semantic admission

Before implementation, create/index stable patch-target documents linked to these observable
scenarios, validators, tests and later native evidence. Candidate groups are offensive action selection
with its cost/lifecycle bridge; tactical-jump selection; each generation extension; each optional
talent registration; and only necessary projectile/faction bridges. Separate absent-DLC branches.

Existence/callable shape can suffice for a pure observation. Any argument-changing prefix must prove
the consumed argument's operation/control-flow context and cardinality; a signature alone cannot
prove safe action selection or charge ownership. Check effective code and skip-original interactions.
Every transpiler validates its actual incoming stream on every invocation and returns it unchanged
on failure; do not speculatively execute foreign transpilers. Allow benign changes and compatible
foreign owners, not whole-method hashes/exact versions or an unpatched-method requirement. Handle
supported later repatching by revalidation/invalidation. False/unknown/exception/install failure
leaves the action/necessary group inactive, rolling back only newly installed owned patches.

XML Extensions guards must check active package ID for external content and actual Def/node shape
before additive, duplicate-free merges; inspect the installed operation syntax first. Do not copy
source duplicate scalar/container additions, replace whole tags/requirements/abilities lists or
globally set AI flags as a substitute for per-save policy. Add the required XML Extensions dependency
with first use, Harmony with its actual use, and no source DLL/assets or Gateway in the product.

One bounded startup warning per skipped enabled patch includes package/patch/spec/target, failed
predicate, unavailable behavior and fallback. Disabled settings and absent optional packages are
not failed candidates. No tick/job log spam. Source-present suppression is explained once.

## Risks / Trade-offs

- AI use increases effective damage and reduces recoverable charges. Native costs alone do not
  prove raid balance; generation alternatives may also increase valuable loot.
- Source jump thresholds are heuristic, not measured optimal tactics. Bound retries and preserve
  normal combat when no valid landing exists; no speculative expensive planner.
- Source/native versions differ and Anomaly data is missing. Compatibility remains pending, even
  for source-advertised gear. Research identifies the exact current compiled native inputs.
- The 35% direct grant and persistent recruitment exclusion are explicit draft choices needing
  playability review; no commander grant is invented to fill a source wiring gap.
- No source license was found in the bounded inspected distribution/repository scan. Independently
  implement behavior; do not treat source visibility as a reuse grant.

## Migration Plan

No migration is delivered. Implement small vertical behavior slices with honest Unit/Harmony/Defs
tiers, then fresh isolated real game workflows on reviewed builds. Additive owned save fields default
to inheritance and absent grants; existing source state is not guessed/imported. A future source
migration needs a separate backed-up, atomic, failure-restorable contract and verified postconditions.

## Open Questions

EUAT-Q1–6 in the refinement ledger retain tactical/balance, native lifecycle, DLC/provider generation,
optional talent recipients/recruitment, consumers/migration and UI/acceptance work. Do not interrupt
the sequential specification pass or claim these research gaps are completed implementation.
