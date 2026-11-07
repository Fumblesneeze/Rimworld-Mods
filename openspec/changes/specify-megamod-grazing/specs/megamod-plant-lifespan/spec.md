# Plant lifespan policy

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **RimWorld 1.7 beta**.
S043 / Graze up. Specification only; no implementation or accepted gameplay.

## ADDED Requirements

### Requirement: Finite plant lifespans can be scaled without changing growth
The mod SHALL provide a **100%** default lifespan multiplier with a **10–1000%** range for supported
plants with a finite native lifespan. It SHALL affect existing as well as newly grown plants when
their ordinary age check next runs, while leaving growth speed, research, sowing/harvest work and
unrelated environmental damage unchanged. Plants without a finite native lifespan SHALL remain so.

#### Scenario: Scale a finite lifespan
- **WHEN** a supported plant normally lives 20 days and the multiplier is 200% with no soft floor
- **THEN** its age-related lifespan becomes 40 days without changing its growth rate or age

#### Scenario: Immortal plant
- **WHEN** a plant has no finite native lifespan
- **THEN** this policy does not make it die from age, even with a nonzero floor

### Requirement: The lifespan floor is a soft adjustment with a precise boundary
The mod SHALL provide a soft-floor value of **0 days** by default, configurable from **0 to 60 days**.
For a plant whose unscaled incoming lifespan is greater than **1 day**, if its scaled lifespan falls
below the floor, its effective lifespan SHALL move halfway from the scaled value toward the floor.
A lifespan of 1 day or less SHALL receive scaling only. The floor SHALL NOT be described as a
guaranteed minimum lifespan or protection from other death causes.

#### Scenario: Halfway adjustment
- **WHEN** a plant has an unscaled 20-day lifespan, a 50% multiplier and a 60-day soft floor
- **THEN** its effective lifespan is 35 days, not 10 or 60

#### Scenario: One-day boundary
- **WHEN** a plant has an unscaled 1-day lifespan, a 50% multiplier and a 60-day floor
- **THEN** its lifespan is 0.5 days because the floor adjustment does not apply

#### Scenario: Already above the floor
- **WHEN** a supported plant's scaled lifespan equals or exceeds the floor
- **THEN** no floor adjustment is added

### Requirement: Policy changes preserve plant age and previous damage
The mod SHALL apply committed lifespan settings without resetting age, healing plants or instantly
destroying them. A plant now older than its effective lifespan SHALL undergo ordinary age-related
damage on subsequent native updates. Increasing lifespan SHALL not reverse earlier damage. Save
and load SHALL retain native age, growth and health rather than restarting the lifetime.

#### Scenario: Shorten lifespan below existing age
- **WHEN** the player shortens a plant's effective lifespan below its current age
- **THEN** ordinary age damage becomes applicable on the normal update, without direct destruction at settings commit

#### Scenario: Extend lifespan after age damage
- **WHEN** the player lengthens the lifespan of an already damaged plant
- **THEN** its existing damage and age remain, even if it is no longer old enough for further age damage

#### Scenario: Save round trip
- **WHEN** the player saves and reloads a plant under a nondefault lifespan policy
- **THEN** the same saved age/growth/health and effective scoped policy are used, without replaying damage

### Requirement: Lifespan policy uses common settings and independent compatibility admission
The mod SHALL place lifespan multiplier and soft floor, in that order, under **Mod settings →
Animals and plants → Plant lifespan**, using common Global default, Use global and Current save
controls. New/missing save values SHALL inherit globals; dormant overrides SHALL be retained while
inheriting. Main-menu save controls and callbacks SHALL be inactive. Original Graze up or unsupported
lifespan semantics SHALL suppress only the affected owned behavior with bounded startup diagnostics;
failure of unrelated grazing alone SHALL NOT disable supported lifespan policy.

#### Scenario: Inheritance and save isolation
- **WHEN** the global multiplier changes and one save inherits while another overrides it
- **THEN** each resolves its own effective value, dormant overrides survive Use global toggling, and switching saves leaves plant ages intact

#### Scenario: Main-menu edit and reset
- **WHEN** lifespan settings are opened at the main menu
- **THEN** global edits/reset are available, save controls/reset are inactive, and no implicit or previous save is modified

#### Scenario: Scoped reset and invalid values
- **WHEN** a current-save lifespan reset is requested or an invalid number is entered
- **THEN** reset restores this save's lifespan inheritance and clears only its corresponding overrides, while invalid edits are rejected and invalid stored values are safely bounded/defaulted with a diagnostic

#### Scenario: Independent failure isolation
- **WHEN** the grazing group is unavailable but native lifespan semantics remain supported and the original Graze up provider is absent
- **THEN** lifespan policy can remain active; if its own target is unsupported, its skipped patch is explained once at startup and native/provider lifespan remains in control
