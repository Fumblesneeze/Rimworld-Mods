# Remaining-burst target transfer

Owner **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
S081 observable draft only. Policy corrections and compatibility evidence remain in SPT-Q1–4.

## ADDED Requirements

### Requirement: Transfer scheduled burst fire after an automatic pawn target goes down
SPT01. Before a scheduled shot of a supported existing multi-shot ranged burst, the feature SHALL
allow a pawn or turret to transfer from its automatically selected pawn target when that target
is downed or dead. It SHALL choose another eligible hostile threat using the shooter's ordinary
automatic selection preferences from its current position. There is no new nearest-target rule,
extra range, movement order or additional warmup for that transfer. A valid replacement can be a
pawn or another ordinary auto-targetable threat, including a building.

The first scheduled shot also qualifies if the old target went down during warmup. A still-mobile
target, a target cell or a non-pawn target SHALL not trigger this feature. Melee, single-shot
attacks and unrelated abilities SHALL remain unchanged. Unsupported special attack types retain
their own behavior. If no eligible replacement exists, leave ordinary firing behavior intact;
do not force a new target, cancel otherwise native-valid fire or promise to avoid all overkill.

#### Scenario: Automatic target falls mid-burst
- **WHEN** an automatically targeted pawn falls after one shot and another eligible threat is in firing range
- **THEN** a remaining scheduled shot can fire at that threat within the same burst, without restarting warmup or adding shots.

#### Scenario: Target goes down during warmup
- **WHEN** an automatically targeted pawn goes down before the first scheduled shot of a multi-shot burst
- **THEN** that shot can transfer to a currently eligible replacement under the same rules.

#### Scenario: No trigger or no replacement
- **WHEN** the old target is still mobile, is a building/cell, or has gone down with no eligible replacement
- **THEN** this feature leaves the target and native firing result unchanged.

### Requirement: Explicit targets and current firing controls take precedence
SPT02. Pawn transfers SHALL require the shooter's existing **Fire at will** permission to be on;
a pawn without a supported firing controller SHALL not gain automatic transfer. Turrets SHALL
require their ordinary automatic-firing permission, including **Hold fire** being off where that
control exists. These rules apply to each potential transfer, including control edits during a burst.
Disabling transfer permission SHALL not itself redefine how native weapons stop an ongoing burst.

An explicit current pawn or turret target SHALL remain authoritative, including a deliberately
targeted downed pawn. Hunting, finishing, target practice and other target-bound orders SHALL not
silently become general combat attacks. Removing an explicit order only restores transfer when
the remaining native attack is genuinely automatic. No player/non-player faction privilege SHALL
be invented: supported shooters use their own permissions and hostility relationships.

#### Scenario: Fire at will changes mid-burst
- **WHEN** the player disables Fire at will before the next shot and the current pawn target goes down
- **THEN** no automatic transfer occurs; native ongoing-burst stopping behavior remains authoritative.

#### Scenario: Explicitly finish a downed target
- **WHEN** the player explicitly orders a pawn or turret to fire on a downed enemy while another threat is available
- **THEN** this feature does not redirect that order to the other threat.

#### Scenario: Hunter's prey falls
- **WHEN** an ordinary or owned hunting job's chosen prey goes down during a burst
- **THEN** the hunting/finishing/companion workflow retains that prey and this feature does not select an unrelated hostile target.

#### Scenario: Turret Hold fire
- **WHEN** the player enables Hold fire before a potential automatic transfer
- **THEN** no new target is selected by this feature, without creating extra shots or changing the native meaning of Hold fire.

### Requirement: A replacement must be shootable with the actual attack
SPT03. A replacement SHALL satisfy the actual firing weapon's current minimum/maximum range,
line-of-fire and target restrictions from the shooter's real position. It SHALL be on the same
usable map, hostile, an enabled auto-targetable threat, and permitted by current faction/duty and
weapon/turret rules. Respect relevant fog, cover, smoke, roof, incendiary, prisoner, machine,
manning and optional-provider restrictions. The selection SHALL not borrow the range or eligibility
of another equipped weapon or another attack the shooter could theoretically use.

A turret's ordinary permission to bombard other structures SHALL NOT bypass these transfer
requirements. A structure that is not an eligible threat cannot become a replacement merely
because ordinary bombardment can target it. This restriction applies only to transfer; the
turret's normal bombardment selection and explicitly ordered attacks remain unchanged.

Recheck at the scheduled shot. Do not teleport, shoot through otherwise blocking terrain, reveal
hidden targets, target friendlies merely because they are nearer, or override a special weapon's
unsupported targeting rules. Ordinary misses and accidental friendly fire remain possible; this
feature does not guarantee accuracy or a friendly-fire-free shot.

#### Scenario: Nearby target cannot be hit
- **WHEN** one threat is close but blocked/outside the actual weapon's range and another passes its current firing rules
- **THEN** only a genuinely eligible threat can receive the transferred shot.

#### Scenario: Special turret restriction
- **WHEN** a candidate is forbidden by the turret's prisoner, roof, machine, incendiary or manning rules
- **THEN** transfer does not bypass that restriction even if a generic combat search would return it.

#### Scenario: Multiple attacks on one shooter
- **WHEN** the shooter has several attacks with different ranges or target restrictions
- **THEN** the currently firing attack determines eligibility; unsupported selection leaves its ordinary behavior intact.

#### Scenario: Hostile turret also has ordinary bombardment targets
- **WHEN** a hostile overhead-firing turret's automatic pawn target goes down and a colony structure is in range but fails transfer's threat requirements
- **THEN** this feature does not transfer to that structure; outside transfer, the turret can still choose ordinary bombardment targets under its unchanged native rules.

### Requirement: Keep shot accounting and outcomes native
SPT04. Transfer SHALL only redirect shots already due in the existing burst. Preserve remaining
shot count, scheduled intervals, final cooldown, ammunition/fuel/charge costs, native accuracy and
damage, interruption, failure and completion effects. Already fired projectiles SHALL retain their
original trajectories and targets. Actual target-facing and attack feedback SHALL correspond to the
shot taken; no false hit or damage credit is produced for the old or replacement target.

Saving/loading a supported ongoing burst SHALL preserve actual remaining work and recheck current
target/permission before the next due shot. A save, repeated initialization or a failed transfer
SHALL not replay a shot, refund a consumed shot, multiply end-of-burst effects or fabricate a target.

#### Scenario: Count a transferred burst
- **WHEN** a weapon with three scheduled shots transfers after the first shot
- **THEN** at most the two remaining scheduled shots can be redirected; their ordinary spacing/costs and final cooldown remain, and the first projectile is not redirected in flight.

#### Scenario: Firing becomes unavailable
- **WHEN** ammunition, fuel, stance, access or another native firing prerequisite fails before a remaining shot
- **THEN** transfer does not bypass the failure or grant a free shot, and native cleanup/completion behavior occurs once.

#### Scenario: Save with a burst in progress
- **WHEN** a valid burst is saved and loaded after one completed shot
- **THEN** only remaining native shots can fire, using current eligible targets and permissions with no replayed shot or cost.

### Requirement: Use existing controls without duplicating combat state
SPT05. The feature SHALL use the existing pawn Fire at will, turret Hold fire and explicit-target
controls as entity gameplay state. It SHALL add no bottom tab, global/save setting, separate
behavior toggle, debug command or target-selection window. Native target feedback SHALL remain
usable in all baseline languages. Any added player-facing explanation SHALL be localized; log-only
diagnostics and technical identifiers remain invariant.

Owned hunting/finishing features SHALL compose directly with explicit-target precedence. Other
owned combat features retain their damage, armor and ignition rules; transfer changes only the
target for a supported shot and does not reapply those effects. Candidate data SHALL remain current
without introducing a separate per-source map cache.

#### Scenario: Existing control is sufficient
- **WHEN** the player selects a pawn or turret to change its firing permission or explicit target
- **THEN** the ordinary contextual controls govern transfer without another menu or configuration layer.

#### Scenario: Other owned combat effects
- **WHEN** a transferred shot hits under owned armor or fire-response rules
- **THEN** that ordinary shot produces those effects once; target transfer does not add a second damage or ignition event.

### Requirement: Admit only compatible shooting paths and preserve provider ownership
SPT06. An active Spray Transfer source provider SHALL own its overlapping behavior and suppress
the owned feature with a bounded startup explanation. Other compatible patches SHALL be allowed.
Every actual owned patch SHALL require a documented target spec checked on startup; failed,
ambiguous or unknown required semantics SHALL leave the affected shooting path unmodified and
emit one bounded warning per skipped enabled patch with target, failed requirement, lost behavior
and fallback. No foreign patches SHALL be removed and no source identity impersonated.

Admission SHALL establish that the selected target actually drives the intended shot and its
necessary target-dependent effects, not just that a method name exists. Unsupported special
weapons/providers SHALL keep their original behavior rather than receive a partial transfer.
Repeated initialization or later supported patch changes SHALL not duplicate target selection or
leave a previously invalidated transfer active. Unrelated supported shooters remain usable.

#### Scenario: Original source is active
- **WHEN** the game starts with Spray Transfer active
- **THEN** only the external provider performs this overlap and the startup explanation identifies why owned transfer is inactive.

#### Scenario: Changed shot path or compatible modification
- **WHEN** another mod preserves the required shot semantics, or instead removes the required use of the selected target
- **THEN** the compatible path is admitted; the incompatible path keeps native/provider shooting with the skipped patch warning and no partial target change.

#### Scenario: Reinitialization and later repatch
- **WHEN** initialization repeats or a supported later patch rebuild changes the shooting path
- **THEN** each due shot has at most one owned transfer decision and invalidated paths stop transferring before further use.
