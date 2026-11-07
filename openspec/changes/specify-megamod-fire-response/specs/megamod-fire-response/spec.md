## ADDED Requirements

Owner **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
S079 reference: Stop, Drop, And Roll! [BAL]. This is a future draft, not implemented behavior.
Restored leg sampling, ordinary hit weighting, explicit group exclusion and safe fallback are
proposed corrections to the source; defaults and its two independent features are retained.

### Requirement: Fire response and ignition resistance have independent scopes

The mod SHALL offer separate stationary self-extinguishing and apparel ignition-resistance choices
for colonist humanlikes, other humanlikes and animals. Colonists SHALL follow the game's colonist
classification, including secure player slaves where the game classifies them as colonists; other
humanlikes include visitors, prisoners and enemies rather than enemies alone. Animals include wild
and owned animals. Pawns outside these groups SHALL retain ordinary behavior.

Stationary self-extinguishing SHALL default on for colonists and off for other humanlikes and
animals. Ignition resistance SHALL default on for all three groups. Disabling one feature SHALL
NOT disable the other or turn normal fire/damage behavior off.

#### Scenario: Default colony and enemy response
- **WHEN** an able colonist and an able enemy humanlike each catch fire with default settings
- **THEN** the colonist uses the stationary response and the enemy retains ordinary fire-response choices
- **AND** each remains eligible for the independent apparel ignition check on a later new ignition attempt

#### Scenario: Animal behavior is enabled
- **WHEN** stationary response is enabled for animals and an able wild or colony animal catches fire
- **THEN** either can use the stationary response through its normal burning behavior

#### Scenario: Pawn is outside the configurable groups
- **WHEN** a pawn is neither humanlike nor an animal
- **THEN** neither owned feature changes its behavior merely because it did not match a disabled group

### Requirement: An able burning pawn concentrates on extinguishing itself

An enabled, able pawn SHALL begin extinguishing its attached fire in the current cell when its
normal behavior permits self-extinguishing, instead of randomly running or seeking nearby water.
It SHALL use the native150-tick self-extinguishing action, without
an additional10% decision gate, instant extinguishing or an invented rolling animation. The fire
SHALL still be able to harm the pawn until it is extinguished. Successful completion SHALL remove
that attached fire and count one extinguished fire through ordinary records.

#### Scenario: Water is nearby while the pawn burns
- **WHEN** an eligible colonist catches fire near reachable water
- **THEN** its burning response stops to extinguish itself in the current cell rather than running to that water
- **AND** completing the150-tick action removes its attached fire and records one extinguishing

#### Scenario: Pawn is drafted
- **WHEN** an able drafted colonist catches fire and ordinary burning behavior can take control
- **THEN** it uses the same stationary action without needing an undraft command
- **AND** it does not gain permission to keep attacking while ignoring the fire

#### Scenario: Damage occurs during extinguishing
- **WHEN** fire or an external attack damages the pawn during the action
- **THEN** normal damage and incapacitation rules still apply
- **AND** the action provides no temporary invulnerability or healing

### Requirement: Fire response respects ordinary incapacity and lifecycle

Stationary response SHALL NOT make a downed, dead or otherwise unable pawn able to act, bypass
mandatory lying-down behavior or take control of an unrelated job path. If the target fire goes
out through water, weather, another pawn or another valid cause, the response SHALL end safely
without recreating the fire or counting a second extinguishing. Leaving the map, interruption or
loading a save SHALL retain normal job/fire ownership, not restart a free completion timer or
continue acting on a different fire. Disabling stationary response affects subsequent choices;
an already running valid native action can finish normally.

#### Scenario: Pawn cannot perform the action
- **WHEN** a burning pawn is downed or otherwise unable to reach its normal self-extinguishing behavior
- **THEN** this feature does not force it upright or instantly remove the fire
- **AND** ordinary assistance, environment and damage rules remain available

#### Scenario: Someone else extinguishes the fire first
- **WHEN** a helper or the environment removes the attached fire during the action
- **THEN** the pawn leaves the now-unnecessary action safely
- **AND** the feature neither restores the fire nor claims a second completed extinguishing

#### Scenario: Save and reload during the action
- **WHEN** a valid ongoing self-extinguishing action is saved and reloaded
- **THEN** its remaining work and target retain ordinary saved-job behavior
- **AND** loading alone does not extinguish a different fire or grant an extra record

### Requirement: Ignition resistance samples eligible present body coverage

For an eligible new ignition attempt on a configured pawn, the mod SHALL sample one present outer
body part using ordinary flame-hit weighting, restricted to eligible parts. Hands, feet and their
subparts SHALL be exempt from this sample; legs SHALL remain eligible. Internal and missing parts
SHALL NOT be chosen. Face and head parts remain distinct, so an open-faced helmet need not protect
the exposed face. This explicitly corrects the source's additional leg exclusion and local-part
weighting. The feature SHALL use actual compatible body/apparel coverage, not human-only labels.

If a supported sample cannot be established, the attempt SHALL retain ordinary ignition behavior,
without an exception or invented immunity. A pawn with no apparel covering the sampled part SHALL
receive no added ignition resistance from apparel worn elsewhere or from natural heat armor.

#### Scenario: Legs are exposed
- **WHEN** the pawn's legs are present and uncovered while other parts are protected
- **THEN** an eligible ignition sample can select a leg and receive no protection from clothing that does not cover it

#### Scenario: Head protection leaves the face exposed
- **WHEN** the sampled face part is not covered by the worn helmet
- **THEN** that helmet does not block ignition through protection it supplies only to another part

#### Scenario: Custom body has no supported eligible sample
- **WHEN** a custom body has no valid present eligible outer part or its coverage cannot be resolved safely
- **THEN** the feature leaves ordinary ignition behavior responsible for that attempt
- **AND** it neither crashes nor treats the pawn as immune

### Requirement: Each covering garment can prevent ignition independently

The mod SHALL check each distinct covering worn garment once, from outermost to innermost, using
its current effective heat-armor rating. The chance that ignition passes a garment SHALL be
`clamp(1 - strength × (heatArmor - threshold), 0, 1)`, using fractions for heat armor and threshold.
Any garment blocking the attempt SHALL prevent the new attached fire; if every garment passes,
ordinary attachment rules SHALL decide the result. Checks are independent, not added together.

Strength SHALL default to4 and permit finite values0–10. Threshold SHALL default to25% and permit
finite values0–200%. At defaults, a garment with25% heat armor offers no added resistance,26% lets
96% of attempts through,49% lets4% through, and50% or more blocks the attempt at that covered part.
Strength0 SHALL provide no added resistance. A nonfinite or otherwise unusable effective armor
rating SHALL cause safe ordinary fallback for that attempt, not arbitrary immunity.

#### Scenario: One protective layer
- **WHEN** the sampled part has one covering garment with50% effective heat armor under defaults
- **THEN** the new ignition attempt is blocked at that part
- **AND** this alone does not make uncovered parts or direct burn damage immune

#### Scenario: Two partially protective layers
- **WHEN** two distinct covering garments each have a50% chance to let ignition pass
- **THEN** the combined chance to pass both is25%
- **AND** a garment covering multiple groups on the same part is not counted multiple times

#### Scenario: Effective clothing protection changes
- **WHEN** quality, material, equipped clothing or a supported modifier changes the garment's effective heat armor
- **THEN** subsequent eligible ignition attempts use that current effective value
- **AND** the feature does not use a stale cached value or apply direct-damage armor reduction a second time

### Requirement: Ignition prevention does not override other fire rules

The additional check SHALL apply only to an otherwise eligible new attached fire. It SHALL NOT
force ignition on nonflammable targets, repeat checks on an already attached fire, alter unrelated
non-pawn targets or change fire size/instigator when an attempt proceeds. A blocked new attachment
SHALL NOT interrupt the pawn's job or increment its times-on-fire record as though ignition occurred.
Direct weapon/burn damage, existing-fire damage, ground fire, spread, water/rain/vacuum extinguishing
and native immunity SHALL remain governed by their existing rules. The check itself SHALL NOT wear
down apparel; ordinary attacks can still do so.

#### Scenario: Incendiary hit is prevented from igniting
- **WHEN** an incendiary attack deals direct damage but its additional ignition attempt is blocked
- **THEN** the direct damage remains subject to ordinary armor/damage rules
- **AND** no attached fire, ignition-only job interruption or times-on-fire increment is produced

#### Scenario: Pawn is already burning or cannot ignite
- **WHEN** attachment is attempted on an already-burning or nonflammable pawn
- **THEN** the feature performs no new apparel ignition contest and does not extinguish an existing fire
- **AND** the ordinary no-new-attachment result remains intact

#### Scenario: Pawn burns again after extinguishing
- **WHEN** an extinguished pawn remains exposed to a later valid ignition source
- **THEN** that later attempt can ignite it according to its current protection and settings
- **AND** completing self-extinguishing has not granted lasting immunity

### Requirement: Configuration uses the common global and save controls

All eight configuration values SHALL use the common ordered mod-settings section for Fire response,
with separate Stationary response and Ignition resistance groups. Each row SHALL show Global default,
Current save and Use global. New/missing save entries SHALL inherit the current global default;
inherited per-save editors and all main-menu save controls SHALL be disabled, including their
mutation actions. Returning to inheritance SHALL retain a dormant override, and later overriding
SHALL restore it or initialize it from the current effective value.

Changes SHALL apply at the next response choice or eligible ignition attempt; they SHALL NOT reroll
an attempt in progress, remove existing fire or reset an ongoing action. Resets SHALL identify and
affect only the selected scope. Nonfinite/corrupt numeric values SHALL recover to their built-in
default, and finite out-of-range values SHALL be clamped to the specified range. No source settings
are imported automatically. New labels, scope explanations, tooltips and errors SHALL support all
seven baseline languages. No bottom tab, pawn-specific setting or operational toggle is added.

#### Scenario: One save changes animal response
- **WHEN** the player clears Use global and enables stationary animal response for the current save
- **THEN** only that save's subsequent choices change
- **AND** the global default and other saves remain unchanged after saving, loading and switching games

#### Scenario: Settings are opened at the main menu
- **WHEN** no game is loaded and the player opens Fire response settings
- **THEN** global defaults remain editable while per-save controls, Use global and save resets are visibly and functionally disabled

#### Scenario: Invalid saved numeric configuration
- **WHEN** stored strength is nonfinite or stored threshold is a finite value above200%
- **THEN** strength recovers to4 and threshold clamps to200%
- **AND** settings remain usable without changing another scope

### Requirement: Each feature retains safe provider authority

The mod SHALL admit stationary response and ignition resistance only when each feature's required
game/provider conditions hold. The water-choice and self-extinguishing behavior SHALL activate
together; a partial response change SHALL NOT disable ordinary escape without supplying the required
action. Compatible changes and foreign patches SHALL be allowed. Unsupported anatomy/content or
changed conditions SHALL retain the applicable loaded provider rather than broad immunity or a
broken action.

With `balistafreak.StopDropAndRoll` active, the source SHALL own both behaviors and our contributions
SHALL remain inactive. No double resistance, source unpatching or package impersonation is allowed.
A skipped enabled patch SHALL log one bounded startup warning naming its identity, target condition,
unavailable behavior and fallback. A later invalidation SHALL stop the affected owned behavior
safely and report once; other features and foreign providers remain intact.

#### Scenario: Source mod is also active
- **WHEN** Stop, Drop, And Roll! [BAL] is active alongside megamod
- **THEN** only the source supplies these behaviors
- **AND** megamod reports its inactive contribution without importing or overwriting the source's settings

#### Scenario: Only one response prerequisite survives
- **WHEN** water-choice suppression is supported but the required self-extinguishing path is incompatible
- **THEN** the owned stationary-response group stays inactive as a whole
- **AND** the loaded fire-response provider retains authority with the required startup diagnostic

#### Scenario: Compatible apparel or combat modification is present
- **WHEN** an optional provider preserves the actual required coverage, heat-rating and attachment/action conditions
- **THEN** its compatible content is not excluded merely for being modded
- **AND** any separately incompatible feature falls back without disabling unrelated supported behavior
