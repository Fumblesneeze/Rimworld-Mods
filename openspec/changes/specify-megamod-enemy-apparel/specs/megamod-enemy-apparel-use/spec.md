# Enemy apparel use

Owner **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
S083 specification only; no runtime behavior accepted.

## ADDED Requirements

### Requirement: Automatic combatants use equipment they actually wear
Eligible non-player humanlike pawns SHALL consider supported worn combat equipment while already
engaged in ordinary combat. This includes hostile combatants and allies defending themselves against
their own enemies; it does not create hostility or an attack against a neutral pawn. Downed, dead,
unspawned, incapable or player-controlled pawns do not receive new automatic equipment actions.
Player-forced orders, mental-state restrictions and the current combat controller remain authoritative.
The feature grants no automatic action to colonists, player slaves or player-controlled subhumans.

The initial catalog is psychic shock/insanity lances; Royalty grenadier/phoenix/locust armor and jump
packs; Odyssey hunter packs; Anomaly turret packs, shard lances and deadlife packs. Missing DLC or
equipment means no action is offered for it. Other mods' equipment is considered only after its
actual use, costs and targeting contract is supported; mere possession of an offensive-looking item
does not authorize arbitrary custom effects.

An eligible worn offensive action is considered before ordinary attacks. Where several qualify,
use a stable equipment/action order, selecting the first currently valid action; an invalid one does
not block later valid candidates or ordinary combat. One automatic use request performs at most
one use, and a request that cannot progress ends within **2000 game ticks**, returning to normal
combat decisions. It does not repeatedly reset its timeout to freeze the pawn.

#### Scenario: Raider uses grenadier armor
- **WHEN** an active humanlike raider wears charged grenadier armor and has a valid target
- **THEN** it can visibly aim and launch a grenade before returning to ordinary combat, spending the real armor's charge

#### Scenario: Player control and invalid first item
- **WHEN** a colonist wears the same equipment, or a non-player combatant's first candidate cannot legally affect its target
- **THEN** the colonist receives no new automatic action, while the non-player combatant can consider another valid action or continue ordinary combat

### Requirement: Target validation preserves the actual equipment contract
Automatic use SHALL obey the worn item's current range, minimum range, line of sight, target kinds,
psychic sensitivity/immunity, boss restrictions, faction permissions, safe casting position, melee
limitations, friendly-fire rules and any supported provider-specific restrictions. Checking whether
an attack can geometrically reach a target is not sufficient when the item forbids affecting it.
Recheck before commitment and follow the ordinary action's interruption rules during warmup.
No target means ordinary combat continues; no invisible or downed pawn becomes an eligible target
merely to expend a charge. Terrain-targeted packs use a valid target cell, not a forbidden pawn target.

Effects, warmup, aim error, cooldown, damage, faction attribution, goodwill consequences and consumable
behavior remain those of the supported equipment, except for the explicitly described deadlife
launch below. A converted action does not remove native aiming error or change a lance's status
duration. The same physical use never occurs through two alternative representations.

#### Scenario: Psychic protection
- **WHEN** a lance user encounters a psychically deaf, immune or otherwise forbidden boss target
- **THEN** it skips that invalid lance use without spending a charge and can continue ordinary combat

#### Scenario: Grenade accuracy and friendlies
- **WHEN** an enemy launches a supported grenade near other combatants
- **THEN** the actual grenade's current warmup, scatter, effect and applicable friendly-fire restrictions remain, with no perfect-accuracy duplicate cast

#### Scenario: Changed target during warmup
- **WHEN** the target leaves range, becomes invalid or the equipment is removed before use commits
- **THEN** the invalid action is interrupted through its normal rules without applying an effect from a no-longer-worn item

### Requirement: Charges and state belong to the physical item
Each committed use SHALL consume the ordinary charge cost from the exact worn source item once.
Preserve its remaining charges, ordinary reloading requirements and destruction-on-empty behavior.
No separate ability meter grants extra uses or refreshes charges by being selected, dropped, worn,
transferred, saved or reloaded. If multiple items provide the same action, one selected item pays;
the others are not drained. An empty or unusable item cannot start a cast.

Before the item's normal commitment point, rejection consumes nothing. After that point, preserve
the native item's failure and consumption rules without adding an automatic refund or second debit.
Saved warmup, projectiles, flight and deployed objects continue once, not as a new free use.
Defeated pawns drop only their actual remaining equipment under normal loot rules. Manual player
use of acquired equipment retains its existing contextual controls and normal resource behavior.

#### Scenario: Two jump sources and reload
- **WHEN** a pawn wears two supported jump sources and uses one before a save/reload
- **THEN** only that source pays once, its remaining charge count survives, and the other source is unchanged

#### Scenario: One-use pack
- **WHEN** a hunter or turret pack commits its ordinary single use
- **THEN** one ordinary deployment occurs and the used pack is consumed as specified by that item, without a second ability copy or free replacement in loot

### Requirement: Tactical jumps use current range and legal destinations
With tactical jumps enabled, a capable non-player humanlike SHALL consider a jump using its worn,
charged jump pack or locust armor during ordinary combat. Every destination must be in bounds, visible,
walkable for that pawn, clear of an impassable or closed-door landing and within the current item's
effective jump range with the required line of sight. It need not be reachable by walking around
the obstacle. Honor actual jump restrictions, valid landing occupancy and the exact item's current
quality-dependent range; do not keep an old range after equipment or quality changes.

Use total injury severity divided by lethal damage threshold as the source-derived injury measure.
Below **0.50** counts as healthy. A healthy melee-weapon user seeks a legal landing near its current
eligible enemy. Otherwise a pawn at or above **0.50** with at least **2** nearby threats within
**2 cells** seeks a legal retreat. A ranged user can instead seek a legal firing position, preferring
cover when weapon range exceeds **5 cells**. If no valid shooting jump exists, a pawn at or above
**0.25** injury with at least **one nearby threat within 30 cells** can seek retreat. Requiring an
actual nearby threat is an intentional correction to the source's zero-threat fallback check.
These are provisional tactical thresholds, not estimates of remaining HP.

Approach and shooting-position jumps require a displacement of at least **8 cells**; retreats can
be shorter when that is the valid escape. Jump-target search is bounded to **8 plus jump range**,
plus **66% of the current ranged weapon's range limited to 2–20 cells** for ranged users. Select only
ordinary valid, visible, non-downed threats; no target or legal useful landing leaves ordinary combat
unchanged. A rejected candidate does not stop the pawn's current movement or repeatedly request the
same impossible jump. Once flight starts, normal jump movement, timing, landing and charge rules apply.
There is no separately added emergency explosion-jump behavior in this draft.

#### Scenario: Melee approach over an obstacle
- **WHEN** a healthy melee combatant can jump at least 8 cells to a legal landing near its enemy
- **THEN** it performs one charged jump and resumes combat there, even when the walking route is longer

#### Scenario: Current quality changes range
- **WHEN** a jump item's quality or other supported range modifier changes before the next jump decision
- **THEN** candidate selection and the actual jump use the new range, rejecting a now-unreachable landing without charge loss before commitment

#### Scenario: Retreat or no landing
- **WHEN** an injured threatened combatant has a legal escape landing, or every possible landing is blocked
- **THEN** it can retreat in the first case and continues normal combat in the second, without an endless jump loop or a jump into an invalid cell

#### Scenario: Fallback without a nearby threat
- **WHEN** a combatant at or above 0.25 injury has no valid shooting jump and no threat within 30 cells
- **THEN** this fallback does not initiate a retreat jump or spend a charge merely because the pawn is injured

#### Scenario: Fallback with a nearby threat
- **WHEN** the same injured combatant has at least one threat within 30 cells and a legal useful escape landing
- **THEN** the fallback can initiate one normal charged retreat jump

### Requirement: Deployments keep their own effects and faction ownership
Hunter and turret packs SHALL create their normal projectile and deployed actor at a valid ordinary
deployment location. Their lifetime, targeting, movement, explosion and ownership remain native to
the item/provider. This does not add the source's unconnected experimental turret-position planner.

With Anomaly and a supported deadlife pack, non-player combatants SHALL be able to launch its
deadlife capsule at a valid location associated with nearby corpses/combat: baseline **23.9-cell
range**, **1.5-second warmup**, **0.1-cell forced miss radius**, one normal pack charge. Use the
ordinary deadlife projectile/dust behavior, with the source's baseline **21 cells of gas quantity**
kept consistent with the actual projectile. This is an explicit added ranged use for enemies, not
a silent change to the player's manual pack action. Faction marking follows the actual successful
release and affected corpses/pawns; do not permanently claim actors at the intended destination when
the capsule misses, is interrupted or never releases dust. Resurrection and gas effects obey the
current Anomaly rules, not immediate direct revival or guaranteed success. This branch remains
unavailable until its actual installed content/behavior is verified.

#### Scenario: Hunter drone launch
- **WHEN** a valid enemy hunter-pack use completes
- **THEN** one actual capsule deploys the ordinary faction-owned hunter drone, which follows its normal behavior and lifetime

#### Scenario: Deadlife release does not preclaim a failed target
- **WHEN** a deadlife launch fails before release or its actual landing differs from its intended cell
- **THEN** no pre-release target is falsely marked as belonging to the caster; only actual affected content follows the supported release's faction rules

### Requirement: Unsupported integration is isolated and explained
Compatible unrelated mod changes SHALL remain supported. An enabled patch whose required behavior
cannot be established leaves only its affected action or necessary group unavailable and logs a
bounded startup warning naming the skipped patch, failed condition, missing player behavior and
fallback. Missing optional DLC is normal. Do not erase other mods' behavior or partially apply an
action without its charge accounting. When original EUAT is active it owns new overlapping behavior;
suppress owned duplicates and retain safe resolution of existing owned casts, effects and saves.
No original source settings/jobs/abilities are automatically migrated.

The feature SHALL add no bottom tab or separate duplicate ability menu. Use existing combat effects,
item/pawn inspection, charge displays and manual commands. Actual configuration follows the common
settings requirement in loadout variation; any added visible text has all seven baseline languages.
Automatic quality changes, burst retargeting and specialist autocast do not multiply these actions.

#### Scenario: Changed custom charge behavior
- **WHEN** a provider changes the required charge/effect behavior beyond the supported contract
- **THEN** that automatic action stays unavailable with a startup explanation, while ordinary unaffected combat and other compatible apparel still work

#### Scenario: Original source activated
- **WHEN** EUAT is loaded alongside the megamod
- **THEN** it owns new overlapping use, generation and talent behavior without duplicate owned charges/effects, and existing owned state is recovered or completed safely
