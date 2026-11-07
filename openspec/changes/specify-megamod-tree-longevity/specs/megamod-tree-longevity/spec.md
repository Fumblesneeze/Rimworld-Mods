# Tree longevity

Owner **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
Origin: S077 Trees Don't Just Die replacement, with explicit compatible descendant coverage.
Draft only; no implementation or native acceptance.

## ADDED Requirements

### Requirement: TL01 Trees remain alive without age-driven decay

Supported trees SHALL no longer take damage or die merely because they have exceeded their normal
lifespan. This SHALL cover wild and cultivated native trees, bonsai when present, and the source's
tree-like woody plants such as saguaro and timbershroom, together with supported modded descendants
of that tree family. Trees already lacking an age limit SHALL retain that behavior. Ordinary food,
drug and flower crops, fibercorn and dead stumps SHALL retain their existing lifetime/decay rules.

#### Scenario: A healthy old tree remains standing
- **WHEN** an otherwise healthy supported oak or pine passes its normal age limit in suitable conditions
- **THEN** age alone does not damage or remove it.

#### Scenario: Bonsai without its former short lifespan
- **WHEN** bonsai content is present and an otherwise healthy cultivated bonsai exceeds its former lifespan
- **THEN** it remains without needing replacement solely because of age.
- **AND** without that content, ordinary supported trees still receive the feature.

#### Scenario: A compatible modded tree uses a nested family
- **WHEN** a supported modded tree belongs to the same tree family through an intermediate variant
- **THEN** it receives the same protection from age-driven decay despite its different variant name.

#### Scenario: A woody crop is not a tree-longevity target
- **WHEN** rice or fibercorn exceeds its ordinary lifespan, or a dead stump deteriorates
- **THEN** this feature does not prevent the normal damage or removal.

### Requirement: TL02 Other threats and forestry remain effective

Trees SHALL retain their normal damage/death responses to fire, attacks, chopping, harmful light or
darkness, pollution, vacuum, terrain and other supported environmental or special mechanics. Removing
age decay SHALL not cancel simultaneous damage from another cause. Growth, fertility, harvest yield,
work, sowing/research availability and special tree interactions SHALL retain their existing rules.
The feature SHALL not issue harvest/cut orders, protect designated trees from workers or alter their
responses to other owned growing/clearing features.

#### Scenario: An old tree is also deprived of light
- **WHEN** a supported old tree is in darkness that normally damages that tree
- **THEN** darkness can still damage and kill it even though age no longer causes damage.

#### Scenario: Harvest an old tree
- **WHEN** the player orders eligible workers to chop a supported old tree
- **THEN** normal work removes it and produces its normal eligible harvest, with normal consequences.

#### Scenario: A light-sensitive woody plant is exposed
- **WHEN** a supported timbershroom is exposed to harmful light
- **THEN** it retains its normal response to that light rather than becoming invulnerable.

### Requirement: TL03 Existing plants and saved age remain intact

Applying this feature SHALL leave existing plants' age, growth, current health and position intact.
It SHALL not heal damage already taken or restore removed trees. Saving and reloading SHALL preserve
that native plant state. When the feature is no longer applied and finite native lifespans return,
trees SHALL be subject to those lifespans using their retained age, without a reset or grace period.
New plants SHALL retain normal growth initialization and their ordinary native age initialization
for plants without a limited lifespan.

#### Scenario: Load an old damaged tree
- **WHEN** the player loads a living tree already older than its former lifespan and missing health
- **THEN** its existing health, growth and age remain, but no further damage occurs solely from age.

#### Scenario: Reload then restore finite lifespans
- **WHEN** a saved tree outlives its former age limit and a later run restores finite lifespan behavior
- **THEN** its retained age can again cause normal age damage; it does not become newly planted.

### Requirement: TL04 Information reflects the effective lifetime

A protected tree's native information card SHALL no longer list a limited lifespan, finite lifespan
duration or the age row normally tied to that limitation. Other growth, harvest and condition
information SHALL remain available. This feature SHALL add no bottom tab, debug control, operational
toggle or settings window. Any new player-facing compatibility explanation SHALL use the seven
baseline languages; technical log-only diagnostics SHALL remain invariant.

#### Scenario: Inspect a protected tree and an ordinary crop
- **WHEN** the player opens each information card
- **THEN** the tree lacks the finite-lifespan information while the ordinary crop retains its normal
  lifespan information and both retain unrelated relevant statistics.

### Requirement: TL05 Forest population follows ordinary growth and spawning rules

Longer-lived trees SHALL remain physical occupants and available resources. Their continued presence
SHALL participate in normal plant population and spacing rules; the feature SHALL not add replacement
saplings, force a population target, speed regrowth or make a fixed promise about forest density.
It SHALL not replace the game's existing plant lists or introduce independent population behavior.

#### Scenario: An old tree still occupies its cell
- **WHEN** native regrowth considers a cell occupied by a protected living tree
- **THEN** the ordinary occupied-cell restriction remains effective and no replacement tree is stacked there.

#### Scenario: The player clears protected trees
- **WHEN** trees are removed through normal work
- **THEN** future plant growth/spawning follows the map's ordinary conditions, with no special immediate
  replacement or guaranteed species from this feature.

### Requirement: TL06 Compatibility failure preserves usable content

If the original Trees Don't Just Die provider is active, megamod's contribution SHALL remain inactive
with a bounded startup warning identifying that authority, without rewriting the original's result.
Otherwise each admitted contribution SHALL change only the intended lifetime behavior and preserve
unrelated plant properties and third-party list additions. Already-unlimited valid targets SHALL
remain valid. Missing optional content SHALL not prevent normal supported trees from working. A
missing, ambiguous or incompatible required target SHALL leave the affected contribution unapplied,
with no partly rewritten plant record, and log a bounded startup warning per skipped enabled patch
identifying the target, failed condition and fallback. Compatible unrelated modifications SHALL be allowed.

#### Scenario: Original provider present
- **WHEN** the original mod is active at startup
- **THEN** it owns tree-lifespan changes, megamod adds no competing contribution and logs that decision.

#### Scenario: Preserve another mod's added tree behavior
- **WHEN** compatible tree content has an additional harvest or growing property
- **THEN** removing the age limit retains that property and any unrelated added list entries.

#### Scenario: Unsupported target shape
- **WHEN** the lifetime contribution cannot be safely applied to its required target
- **THEN** the affected tree retains its prior behavior and a startup warning explains the skipped change.
