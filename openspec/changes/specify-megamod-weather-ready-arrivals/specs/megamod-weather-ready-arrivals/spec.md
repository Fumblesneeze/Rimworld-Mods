# Weather-ready arrivals

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`). Target: 1.7 beta.
Status: specified, not implemented. This prepares incoming groups; it does not continuously manage outfits.

## ADDED Requirements

### Requirement: Prepare ordinary visiting groups at arrival

Trade caravans and ordinary visitor parties SHALL receive weather preparation when generated for
their destination map. Eligible pawns SHALL retain their native group, role and arrival behavior.
Pawns incapable of wearing apparel SHALL be left unchanged. Colonists already on the map, unrelated
pawn generation and existing visitors SHALL not be redressed merely because weather or settings change.

#### Scenario: A caravan arrives on an ice sheet
- **WHEN** a trade caravan is generated for a cold map and legally usable warmer apparel is available
- **THEN** its eligible members arrive wearing improved protection while retaining their normal visit.
- **AND** existing colonists and earlier visitors are not given new apparel by this arrival.

### Requirement: Improve protection against the actual outdoor temperature

Preparation SHALL compare the destination's current outdoor temperature with each pawn's actual
comfortable range. Pawns already comfortable SHALL keep their ordinary apparel unless separate
hazard protection is needed. Cold or heat changes SHALL improve protection for the present condition
without discarding stronger conflicting protection. Once comfortable, the pawn SHALL stop receiving
further temperature-driven replacements. No fixed parka/duster requirement SHALL override better
legal options for that pawn.

#### Scenario: One change is enough
- **WHEN** adding a legal warm garment makes a pawn comfortable at the arrival temperature
- **THEN** preparation stops temperature-driven changes for that pawn instead of repeatedly replacing it.

#### Scenario: The weather is too extreme
- **WHEN** available legal clothing cannot make the pawn comfortable
- **THEN** arrival still completes after bounded preparation, retaining the best accepted protection
  without claiming immunity to the weather or preventing normal temperature consequences.

### Requirement: Clothing respects wearer, faction and replacement restrictions

Ordinary weather candidates SHALL be wearable by the pawn, no more advanced than its known
faction's technology, use permitted materials, satisfy applicable specific apparel/material
requirements, and be enabled in the candidate filter. Utility belts, mechanitor-specific apparel
and clothing not eligible for normal weather generation SHALL remain outside this ordinary pool.
An ordinary temperature candidate SHALL not displace any worn garment excluded by the replacement filter. Required special survival protection may displace protected armor as specified below. Generated
apparel SHALL use normal generated-gear quality/condition rules, not universally best-quality gear.

#### Scenario: Protected armor occupies the required layer
- **WHEN** a warmer candidate would remove armor the replacement filter protects
- **THEN** that candidate is not equipped and preparation considers other legal options.

#### Scenario: Only unsuitable garments remain
- **WHEN** all permitted candidates are unwearable for the pawn or violate material/technology rules
- **THEN** the pawn keeps its existing legal apparel and arrival finishes without an endless retry.

### Requirement: Pollution protection has explicit eligibility

Eligible arrivals SHALL receive special pollution protection when Biotech content is active and
the destination has more than 50% pollution, active toxic fallout, or supported toxic spore weather.
Industrial-or-higher factions SHALL use an available wearable gas mask; lower-tech factions SHALL
use an available legal war veil enabled in Allowed weather clothing when possible. A veil SHALL not be presented as full immunity or
as equivalent to a gas mask. Exactly 50% pollution alone SHALL not trigger this preparation.
Gas masks and vacuum gear SHALL be identified as special provisions outside the ordinary candidate
filter; the lower-tech veil SHALL retain that filter restriction.
Existing adequate hazard protection SHALL not be replaced merely to add another copy.

#### Scenario: Pollution boundary
- **WHEN** pollution is exactly 50% with no toxic condition
- **THEN** pollution alone does not cause special headgear.
- **AND** above 50%, eligible arrivals receive the appropriate available protection.

#### Scenario: Missing optional content
- **WHEN** Biotech or the required protective apparel is unavailable
- **THEN** the unsupported special provision is skipped without preventing ordinary weather preparation.

#### Scenario: Veils excluded by the player
- **WHEN** a lower-tech group arrives in toxic conditions and war veils are disabled in Allowed weather clothing
- **THEN** this feature does not generate war veils for that group.

### Requirement: All humanlike arrivals on vacuum maps receive applicable protection

Every humanlike pawn spawning on a vacuum map SHALL be included in vacuum preparation, regardless
of faction technology, age, visitor/raider role or the ordinary Prepare raiders option. This includes
newly generated pawns and existing NPC/world pawns reused for arrivals. Supported native Odyssey
and supported SOS2 vacuum maps SHALL use their actual environmental and protection rules; merely
loading either provider SHALL NOT classify every map as vacuum. Other admitted providers may supply
the same facts without a fixed garment-name list. Missing or incompatible provider contracts SHALL
withhold only the affected preparation with a diagnostic, not claim unsupported protection.

The arriving pawn SHALL have an actually wearable outfit sufficient for the current vacuum hazard
where suitable gear exists, including age/body-appropriate protection. Already adequate protection
SHALL be retained. Ordinary temperature preparation SHALL preserve required protection. A positive
vacuum-resistance declaration or membership in S130's Spacefarer policy alone SHALL NOT count as a
complete protective outfit. No advanced-technology restriction SHALL exclude an otherwise eligible
humanlike arrival from this survival requirement.

Existing player-owned pawns SHALL use real gear they own or can ordinarily equip; preparation
SHALL NOT spawn free replacements, duplicate inventory, rewrite their apparel-policy choices or
teleport unavailable stock to them. If suitable real gear is unavailable, their arrival retains
ordinary equipment and exposure consequences with an unresolved-protection warning. NPC arrival
provisioning SHALL NOT be skipped merely because the NPC existed earlier as a world pawn.

#### Scenario: Low-technology raider in vacuum
- **WHEN** a humanlike raider spawns on a supported vacuum map while Prepare raiders is off
- **THEN** vacuum preparation still supplies suitable protection where available, without changing the raid's strength budget or orders for the purpose of clothing preparation.

#### Scenario: Protected child in vacuum
- **WHEN** a child arrives with adequate age-appropriate protection and temperature preparation runs
- **THEN** it retains that protection while compatible temperature improvements are considered.

#### Scenario: Reused NPC and returning colonist
- **WHEN** an existing world NPC and an existing player-owned colonist arrive on a vacuum map
- **THEN** the NPC receives applicable arrival protection while the colonist uses real available equipment without free generated gear
- **AND** missing equipment never creates a false claim of full protection.

### Requirement: Special protection remains physically valid

Special hazard equipment SHALL respect the pawn's wearability and the game's apparel coexistence
rules. Required special survival protection SHALL take precedence over the replacement filter,
including protected armor; retain compatible armor and already adequate protection where possible.
When several hazards coexist, prefer a physically valid outfit satisfying all of them. If no such
combination exists, preserve vacuum survival first, then the best compatible other protection, and
report the unresolved hazard. Ordinary temperature changes SHALL not remove protection required for current hazards.
If the required combination cannot be worn together, preparation SHALL keep a valid outfit, finish
without duplicating incompatible garments, and report the unresolved protection rather than claim
the pawn is safe.

#### Scenario: Required survival gear displaces armor
- **WHEN** adequate survival protection requires replacing armor protected by the ordinary replacement filter
- **THEN** the required protective gear may replace that armor, keeping other compatible protection and ordinary valid apparel rules.

#### Scenario: Two hazards require conflicting headgear
- **WHEN** toxic conditions and vacuum require protective equipment that cannot coexist
- **THEN** the pawn is not equipped with an impossible combination and the unresolved protection is reported.

### Requirement: Raider preparation is optional and prospective

Ordinary temperature and pollution preparation SHALL apply to newly generated raiders only when Prepare raiders is enabled,
initially off. Universal vacuum preparation remains independent of this option. Enabling it SHALL not change raid size, faction, combat orders or existing raiders.
Descriptions SHALL explain that better survival apparel can affect combat protection and obtainable
gear; preparation is not purely cosmetic.

#### Scenario: Enable prepared raiders in one save
- **WHEN** the player enables this option for the current save
- **THEN** subsequent raids use preparation while an already present raid is unchanged.
- **AND** another save inheriting the global off default remains unaffected.

### Requirement: Two clear filters use common settings

Settings SHALL distinguish **Allowed weather clothing** from **Clothing that may be replaced**.
The candidate filter SHALL initially allow ordinary apparel while excluding armor, armored headgear
and utility apparel. The replacement filter SHALL initially protect those categories except simple
and advanced helmets and flak vest/pants/jacket, which are replaceable; gas masks SHALL be protected.
Both filters and Prepare raiders SHALL use global defaults and save overrides, searchable in one
arrival-preparation section. Global editing SHALL work from the main menu; both Use global and the
current-save value editor SHALL be disabled there. In a loaded save, Use global SHALL remain usable;
only the current-save value editor SHALL be disabled while inherited. Filter reset SHALL restore
that filter's documented defaults.

#### Scenario: Different filter purposes
- **WHEN** the player allows a coat as a candidate but protects the currently worn conflicting garment
- **THEN** ordinary temperature improvement does not authorize replacing the protected garment; required special survival protection is the documented exception.

#### Scenario: Removed content and settings reload
- **WHEN** a save's filters mention apparel from content no longer present
- **THEN** remaining choices load without errors or silently enabling unrelated clothing.
- **AND** new arrivals use the effective current filters rather than a stale earlier list.

### Requirement: Preparation failures preserve arrivals and ordinary saves

An incompatible preparation integration SHALL remain inactive with a bounded startup warning
identifying the affected behavior and fallback. Unrelated compatible patches SHALL not alone
disable it. With the original Dress for the Weather provider active, megamod SHALL initially defer
this feature to it with an explanation instead of dressing each group twice. Successfully generated
apparel SHALL remain normal game items; disabling the feature SHALL not delete or revert it.

#### Scenario: Feature is removed after an arrival
- **WHEN** a prepared visitor is saved and the feature is subsequently disabled
- **THEN** its ordinary apparel remains intact and later arrivals follow the remaining active rules.
