# Structural damage

Owner: **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`. Target **1.7 beta**.
Specification draft only; no runtime behavior accepted.

## ADDED Requirements

### Requirement: Ordinary attacks have configurable structural effectiveness
The feature SHALL apply one additional damage multiplier to supported ordinary attacks against eligible buildings, retaining other damage modifiers and normal hit-point rounding.
Initially eligible are walls, doors and mineable barriers, including natural rock, ore, collapsed rock
and smoothed rock. A building's normal movement obstruction determines whether its passable or
impassable value applies; an opened door does not change category just because it is open.
The default **Leave other buildings unchanged** option is enabled. Disabling it extends these ordinary
attack multipliers to all buildings with hit points. Items, pawns and plants are never added to scope.

| Ordinary attack family | Passable multiplier | Impassable multiplier |
| --- | --- | --- |
| Cutting and piercing, including weapon pokes, scratches and bites | 0.40 | 0.10 |
| Blunt impact, including ordinary crush attacks | 0.50 | 0.30 |
| Bullets and arrows | 0.20 | 0.20 |
| Ordinary explosive blasts | 0.40 | 0.25 |

Each value ranges from **0 through 3**, displayed to two decimal places. These are multipliers of
otherwise applicable damage, not absolute damage or replacements for independent resistances.
Zero prevents that family's hit-point damage on affected buildings; one leaves its damage unchanged.
The option exempting other buildings skips this feature's ordinary multiplier entirely, even at zero.
Existing immunity remains immunity. Fire, decay, mining, dedicated breaching and unrecognized damage
do not become an ordinary family solely because they damage a building.

#### Scenario: Wall versus door
- **WHEN** an ordinary blunt attack would cause 20 structural damage before final hit-point rounding
  to an impassable wall or a passable door under otherwise identical conditions
- **THEN** default owned multipliers make those amounts 6 and 10 respectively, before ordinary rounding
- **AND** the attack's other modifiers, hit chance and timing remain applicable.

#### Scenario: Default exemptions and explicit broader scope
- **WHEN** an ordinary bullet hits a turret, furniture, a structural wall or a loose item
- **THEN** the default option changes only the wall's damage
- **AND** disabling Leave other buildings unchanged also applies the projectile multiplier to the
  turret and furniture, while the loose item's damage remains unchanged.

#### Scenario: Zero and independent modifiers
- **WHEN** the player sets an affected family to zero and attacks an eligible barrier and an exempt building
- **THEN** the barrier loses no hit points from that attack and the exempt building retains its otherwise applicable damage
- **AND** no invalid damage, healing or removal of another immunity results.

#### Scenario: Explosive and unrecognized attacks
- **WHEN** an ordinary explosive blast would deal 400 damage to an impassable wall before final rounding
- **THEN** its default multiplier yields 100 before rounding and remaining-hit-point limits
- **AND** fire and an unsupported custom attack retain their existing behavior instead of inheriting an assumed explosion rule.

### Requirement: Crushing heads preserve other attacks and dedicated breaching
The feature SHALL give supported blunt weapon heads a crushing attack that deals 1.5 times their otherwise applicable structural damage against impassable buildings and 2 times against passable buildings.
This enhancement applies to all buildings with hit points, including buildings exempt from ordinary
reductions. It replaces the qualifying head strike's ordinary blunt reduction; the two never multiply together.
Weapons must have an identified blunt head, as the native mace does; other handles, capacities,
attacks, quality, material, speed, reach, availability, recipe and research remain unchanged.
Crushing retains ordinary blunt-specific resistances and responses, including against pawns and items.
Additional attack effects do not gain the head's structural bonus merely by accompanying that strike.
Dedicated breach-axe and thump-cannon
attacks retain their normal damage and do not acquire ordinary blunt or explosion reductions.
The existing social-fight restrictions and item-destruction behavior remain authoritative.

#### Scenario: Head and handle remain distinct
- **WHEN** a supported mace's head hits a wall and its handle subsequently pokes that wall
- **THEN** the head uses the crushing enhancement and the handle uses the ordinary piercing reduction
- **AND** a native choice of another attack is neither replaced nor forced to use the head.

#### Scenario: Crushing is not an extra pawn or item damage bonus
- **WHEN** a supported head hits a pawn in ordinary combat or an item during an owned destruction order
- **THEN** it retains the ordinary blunt injury or item damage result, without the structural enhancement
- **AND** the respective eligibility and completion rules remain unchanged.

#### Scenario: Equipping a mace does not add a social-fight attack
- **WHEN** a pawn equipped with a supported mace enters a native social fight
- **THEN** it still selects only otherwise eligible native social-fight attacks under the owned sharp-attack restriction
- **AND** the equipped head does not become available merely because this feature supports it in ordinary combat.

#### Scenario: Breaching equipment and existing resistance
- **WHEN** a breach axe or thump cannon attacks a wall or door with ordinary-family values set to zero
- **THEN** its dedicated breaching damage is unchanged by those settings
- **AND** independent target resistance still applies.

#### Scenario: A target specifically resists blunt damage
- **WHEN** a supported head's strike would deal 40 structural damage before an existing blunt-specific
  resistance of one quarter against an impassable building
- **THEN** the resistance still reduces the amount to 10 and the crushing enhancement makes it 15
  before ordinary rounding, with no ordinary blunt reduction applied on top
- **AND** additional independent attack effects retain their own damage handling.

### Requirement: Mining changes are limited to constructed walls
The feature SHALL halve otherwise applicable mining damage against constructed nonmineable walls while preserving ordinary mining of rock, ore, collapsed rock and smoothed rock.
This fixed contribution does not change mining speed, yield, research, deconstruction work or refunds,
designations, reachability, roof-removal ordering or safe-removal eligibility. It is independent of the
ordinary-family exemption option. Other mining modifiers remain applicable exactly once.

#### Scenario: Mine a constructed wall
- **WHEN** a native mining job capable of damaging a constructed nonmineable wall lands a hit that
  would otherwise remove 40 hit points under the current conditions
- **THEN** the owned contribution reduces that hit to 20 before ordinary rounding
- **AND** it does not authorize a previously unavailable mining job or replace deconstruction.

#### Scenario: Mine ore and smoothed rock normally
- **WHEN** a colonist mines ore, natural rock, collapsed rock or smoothed rock under otherwise identical conditions
- **THEN** damage, final mineable removal, yield and job timing remain unchanged by the constructed-wall rule
- **AND** safe roof handling still governs whether and when a removal job starts.

### Requirement: Optional rock resistance has explicit coverage
The feature SHALL offer an initially disabled option making the supported natural-rock family immune to ordinary native bullets and arrows.
Coverage includes ore, collapsed rock and smoothed rock descended from that family, even when no
longer described as natural rock. Built walls, loose stone chunks, explosions, mining and laser or
other custom projectiles are outside this option unless separately documented support is added.
The setting changes subsequent matching damage, not current hit points or projectile firing.

#### Scenario: Enable rock projectile immunity
- **WHEN** the player enables the option and a native bullet or arrow strikes rock, ore or a smoothed rock barrier
- **THEN** the struck barrier loses no hit points from that projectile
- **AND** a built wall still uses its ordinary projectile multiplier, while native mining remains possible.

#### Scenario: Disable immunity without restoring health
- **WHEN** the player disables the option after a rock barrier has been damaged by another attack
- **THEN** later bullets and arrows use the ordinary structural policy
- **AND** existing damage and mining progress are not reset.

### Requirement: Dig-enabled escape attempts use native attack alternatives
The feature SHALL prevent mining in the specific native exit behavior that already chose a dig-enabled route and found a blocking building.
For a mineable blocker, the pawn uses the native melee-or-wait alternative. A nonmineable blocker
retains an eligible native building-destroying ranged attack; otherwise it uses the native attack or
wait behavior. Existing reservation failure, nearby movement, waiting and job interruption remain.
Ordinary exit selection, portals, flight, other mining jobs and other AI duties are unchanged.
This does not guarantee escape from an invulnerable or otherwise unbreakable barrier, nor introduce
an immediate retry loop or force an unavailable attack.
An exit attack is ordinary combat destruction, outside roof-safe mining. Even when safe removal covers
the same pawn's mining work, this attack does not prepare roof before destroying its support; ordinary
collapse consequences remain. Other covered mining/removal work retains its roof safeguards.

#### Scenario: Mineable blocker during an eligible exit
- **WHEN** the native dig-enabled exit behavior reaches a rock blocker and can normally choose an attack
- **THEN** the pawn attacks rather than performing a mining job for that exit decision
- **AND** an unrelated colonist mining order remains available under its own rules.

#### Scenario: Native ranged breacher and contention
- **WHEN** that exit behavior encounters a nonmineable blocker with an eligible building-destroying ranged attack
- **THEN** that native ranged choice remains usable
- **AND** ranged-job failure or interruption follows its existing native handling.

#### Scenario: A melee escape attempt cannot reserve the blocker
- **WHEN** the native melee alternative cannot reserve the selected blocker
- **THEN** its ordinary nearby wait/move behavior occurs
- **AND** no owned unbounded search or immediate retry is introduced.

#### Scenario: Escape attack destroys a roof support
- **WHEN** a foreign pawn whose mining work would be covered by enabled Other workers roof safety
  instead attacks a supporting rock through this exit behavior
- **THEN** the attack does not initiate mining's roof preparation or cancellation
- **AND** loss of required support has its ordinary roof-collapse consequences, while separate covered mining work remains protected.

### Requirement: Structural settings use common global and save controls
The feature SHALL place four passable/impassable setting pairs and the two options in the common mod-settings Combat / Structural damage section, with global defaults and per-save inheritance.
Each pair is one validated value. Each row shows Use global, Current save and Global default.
New/missing save entries inherit the current global value. Use global retains a dormant override;
unchecking restores it, or seeds the first override from the current effective global value.
Checking it disables the save editor. In the main menu, save editors, Use global and all save mutation
callbacks are disabled with an explanation; global controls remain usable. Invalid or nonfinite pairs
are rejected together; malformed persisted pairs use the built-in pair with a bounded diagnostic.
Valid changes apply to subsequent damage events, without restart, healing, resetting jobs or editing
another save. Save switching selects the new effective values. There is no new tab, debug control or
bottom-right toggle. Labels, scope, defaults and coverage explanations use all seven baseline languages.

#### Scenario: Inheritance and dormant values
- **WHEN** a save inherits a family pair and the player changes its global default
- **THEN** subsequent affected attacks use the new pair and an independently overridden save is unchanged
- **AND** temporarily checking Use global preserves the current save's dormant override for restoration.

#### Scenario: Main menu and invalid input
- **WHEN** settings are opened without an active save, or a pair includes an invalid number
- **THEN** no save-scoped state can be edited, and the invalid pair cannot partly replace the last valid pair
- **AND** global controls remain usable with clear scope and range explanations.

#### Scenario: Reload and save switching
- **WHEN** the player reloads or switches between saves with different overrides
- **THEN** subsequent damage uses that save's effective values
- **AND** existing hit points, other saves and unrelated settings are unchanged.

### Requirement: Supported optional content has bounded documented behavior
The feature SHALL retain intended support for the source's eight optional content groups only where the installed content and behavior satisfy their documented prerequisites.
The documented special attacks from Alpha Animals, Vanilla Genetics Expanded, Oddities Weapons,
Integrated Implants and Alpha Mythology use the cutting/piercing pair; Oddities Core bullets use the
projectile pair. Supported JecsTools laser attacks use fixed additional 0.4 passable/0.2 impassable
multipliers. Supported VFE Vikings special attacks use the configured impassable projectile multiplier
only, leaving their passable damage unchanged. These contributions obey the ordinary building-scope
option; they do not change their injuries, status effects, firing or damage against nonbuildings.
This is coverage of documented attacks, not every attack from those packages. Missing providers are
normal. Unsupported present content retains its own behavior with a bounded startup explanation.

#### Scenario: Supported special attack
- **WHEN** a supported optional special attack hits an eligible wall and then a pawn
- **THEN** the wall receives its documented structural multiplier
- **AND** the pawn's ordinary injury and secondary effects remain unchanged.

#### Scenario: Different optional coverage
- **WHEN** a supported Vikings special attack hits a passable building or the provider is absent
- **THEN** no owned optional multiplier is applied to that passable hit and absence creates no missing-content error
- **AND** changed unsupported content is not silently treated as a known attack based on its display name.

### Requirement: Admission failure and original-provider authority remain coherent
The feature SHALL activate only contributions whose documented target prerequisites hold, allowing unrelated changes and compatible other mods.
An unsafe contribution remains inactive with one bounded startup warning per skipped enabled patch,
identifying the feature, target, failed prerequisite, unavailable behavior and fallback. Failed groups
leave their targets unchanged and do not alter usable attacks or remove foreign behavior.
The original Walls are solid, when active, is the sole provider of this overlapping feature: all owned
S072 contributions remain inactive, saved preferences remain, and settings explain source authority.
No foreign mod is disabled or unpatched. Independently safe contributions can remain usable when a
different contribution is unavailable. A list addition must preserve unrelated and inherited content.

#### Scenario: Changed weapon or damage path
- **WHEN** weapon-head recognition or a structural-damage prerequisite no longer holds at startup
- **THEN** the affected contribution stays inactive with the bounded explanation
- **AND** weapons retain their original usable attacks.

#### Scenario: Benign modification and repeated initialization
- **WHEN** unrelated code or compatible list additions are present and initialization is repeated
- **THEN** eligible behavior remains available exactly once
- **AND** other damage modifiers and attacks are retained without duplicate owned effects.

#### Scenario: Original provider takes authority
- **WHEN** the original Walls are solid is active alongside the megamod
- **THEN** it alone supplies this feature and owned preferences are preserved without double reductions,
  crushing enhancements, mining changes or exit changes
- **AND** removing it on a later startup permits freshly admitted owned contributions to use those preferences.
