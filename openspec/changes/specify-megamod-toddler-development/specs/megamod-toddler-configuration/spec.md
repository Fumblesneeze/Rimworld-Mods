## Owner

Make Rimworld Great Again (`fumblesneeze.megamod`, `mods/megamod`). S031; future 1.7 beta behavior.

## ADDED Requirements

### Requirement: Configuration uses common global defaults and save overrides

Actual toddler configuration SHALL use the common ordered Childcare settings section with a
Global default, Current save and Use global control per row. New/missing overrides SHALL inherit
the current global value; dormant overrides SHALL be retained. Main-menu save controls and their
actions SHALL be disabled. No row below is exempt from this model. Values SHALL survive reload,
save switching and reset of only the selected scope.

| Setting | Built-in default | Valid values |
| --- | --- | --- |
| Respect bedtime | On | Boolean |
| Put floor-sleeping children to bed | On | Boolean |
| Feed capable toddlers | On | Boolean |
| Feeding floor mess | On | Boolean; tooltip distinguishes provider hygiene loss |
| Allow toddler drafting | Off | Boolean |
| Toddler baby talk | Off | Boolean |
| Infant / toddler Play decay multiplier | 5 / 3 | Each 0–50 |
| Loneliness multiplier | 1 | 0–10 |
| No-expectations mood | +20 | 0–100 |
| Infant base comfort minimum / maximum | 20 / 30 °C | Minimum -30–25; maximum 26–50 |
| Toddler base comfort minimum / maximum | 18 / 28 °C | Same ranges |
| Walking / manipulation learning fraction | 0.8 / 0.8 | Each 0.01–1 of toddler span |
| Apparel availability | Baby tribalwear | Five modes described in apparel capability |
| Automatic move feedback | Danger and unknown | None; danger; unknown; danger and unknown; all |

Settings SHALL apply to subsequent decisions in the active save. Needs/mood/comfort changes SHALL
take effect without resetting current needs or health; clothing availability SHALL affect future
acquisition without deleting items; learning duration SHALL affect future progress without
relearning/regression. Disabling drafting SHALL retain an already drafted child's Undraft action
but prevent new drafting. Turning baby talk on SHALL display toddler thoughts in infant-style
babble; off SHALL use ordinary readable thought wording. This changes presentation only, not thought
effects, mood values or social interaction behavior. Base comfort values SHALL
compose with actual race, clothing and health modifiers, not replace final protection.

#### Scenario: Global and save values differ
- **WHEN** one save inherits toddler Play decay and another overrides it
- **THEN** changing the global value affects only the inheriting save's effective behavior
- **AND** the other save retains its override, with no leakage through shared clothing or comfort data

#### Scenario: Main-menu editing
- **WHEN** toddler settings are opened without a game
- **THEN** global defaults remain editable while Current save and Use global controls cannot mutate any save

#### Scenario: Switching baby-talk display
- **WHEN** the player turns toddler baby talk on and then off while viewing a toddler's thoughts
- **THEN** the text switches between infant-style babble and ordinary readable wording
- **AND** the underlying thoughts, mood effects and social behavior remain unchanged

### Requirement: Entity choices remain on ordinary gameplay controls

Per-child area, schedule, clothing policy, feeding/care assignment and adoption SHALL remain
entity gameplay state on existing appropriate Assign, Schedule, Gear or contextual controls.
Babies/toddlers SHALL appear where those choices are meaningful. No additional bottom tab SHALL
be introduced. Transition/first-steps notices, Play explanations, inability reasons and care
reports SHALL use clear localized text. Automatic move feedback SHALL classify actual medical/
temperature danger and unknown reasons according to the selected mode; even All SHALL omit routine
putting-down of an otherwise safe held child. Explicit player commands SHALL report their own
result rather than duplicate automatic announcements.

#### Scenario: Configuring one child
- **WHEN** the player changes a child's allowed area or clothing policy
- **THEN** it affects that child's gameplay state through the relevant existing control
- **AND** it does not create a global default or another child's override

### Requirement: Development overrides are debug actions

The mod SHALL expose baby spawning, development reset and detailed baby-safety diagnostic controls
only through the top developer controls when developer mode is enabled.
Map-dependent actions SHALL require a map and Biotech and explain unavailability. They SHALL
never become an ordinary bottom tab, an unrestricted gameplay menu or hidden background mutation.

#### Scenario: Developer mode is off
- **WHEN** the player views ordinary child controls with developer mode disabled
- **THEN** developmental reset/spawn overrides are absent and normal care remains available

### Requirement: Unavailable or competing behavior is explicit and isolated

Without Biotech, toddler features SHALL be unavailable with an explanation while unrelated megamod
features remain usable. When the original Toddlers provider or a recognized equivalent is active,
the overlapping owned toddler feature SHALL stay inactive and warn instead of applying a second
development/care system. An enabled patch whose required behavior is unavailable or changed SHALL
be skipped with a bounded startup warning naming the unavailable behavior and fallback. Compatible
unrelated modifications SHALL not alone disable a patch. A failed dependent group SHALL not leave
mobile children without required care, partially altered provider content or a silently disabled
native alternative.

#### Scenario: Two toddler providers are selected
- **WHEN** the player loads the original provider alongside megamod
- **THEN** overlapping owned toddler behavior remains inactive with a clear startup warning
- **AND** megamod does not remove the other mod, duplicate its stages or claim combined compatibility

#### Scenario: A changed target is detected
- **WHEN** an enabled toddler integration no longer satisfies its required game behavior
- **THEN** the affected patch/group is not applied and startup explains the missing behavior and retained fallback
- **AND** compatible unrelated game changes and other patches do not cause rejection on their own

### Requirement: Save adoption and removal are not implicit migration

Owned toddler development, attention, paired activities and arrival state SHALL survive ordinary
save/load without duplicate stages, lost pawns, permanent waiting or replayed rewards. Adding the
feature to an otherwise supported save SHALL initialize eligible native toddlers once from their
current biological age without changing family, health, possessions or completed owned progress.
Loading a save with foreign toddler jobs/development SHALL not silently claim conversion or safe
source removal. Such migration SHALL require its own specified backup, atomic conversion,
restoration-on-failure and verified outcomes before being offered. Disabling or removing a feature
with owned saved content SHALL explain its limitations rather than promise safe removal.

#### Scenario: New owned development in an existing native save
- **WHEN** a supported save contains an age-two human with no owned or foreign development history
- **THEN** initialization provides age-appropriate development once while preserving its existing identity and health
- **AND** later reloads retain earned state instead of repeating initialization

#### Scenario: A foreign toddler save is selected
- **WHEN** the save still contains original-provider development or active jobs
- **THEN** megamod does not silently reinterpret them as owned state or tell the player it is safe to remove the provider
- **AND** any future conversion must satisfy the separate migration contract before it can run
