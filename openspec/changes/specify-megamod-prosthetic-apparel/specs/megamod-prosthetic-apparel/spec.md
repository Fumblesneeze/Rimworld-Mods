# Apparel over suitable prosthetic replacements

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
S025 replacement specification only. PA-Q1–4 retain proposed corrections and exact native/provider
verification. This capability changes clothing eligibility, not the pawn's anatomy.

## ADDED Requirements

### Requirement: Suitable installed replacements count for ordinary clothing
When ordinary clothing availability fails only because its covered body parts are represented as missing, the mod SHALL allow an enabled, supported installed replacement to supply the corresponding clothing coverage.
This applies wherever ordinary apparel eligibility is checked, including putting clothing on and
retaining it after a successful supported replacement operation. The pawn must actually have the
replacement installed at the corresponding attachment, and it must provide that missing coverage.
A loose prosthetic in inventory, an absent limb, a removed replacement or an incidental implant
without replacement coverage does not qualify. Unknown provider behavior grants no exception.
Existing native eligibility is preserved. This rule applies to ordinary apparel-capable pawns without
granting clothing capability to animals or other bodies that cannot normally wear that apparel.

#### Scenario: Gloves over replaced arms
- **WHEN** a pawn with enabled supported bionic arms tries to wear otherwise permitted gloves whose natural hand coverage is missing
- **THEN** the pawn can put on the real gloves through the ordinary clothing workflow.

#### Scenario: Successful replacement while wearing boots
- **WHEN** a successful supported leg-replacement operation leaves enabled replacements providing the boots' coverage
- **THEN** the existing boots remain wearable and are not removed solely because the natural feet became represented as missing.

#### Scenario: Loose replacement or incidental implant
- **WHEN** a pawn lacks the relevant foot and only carries a bionic leg, or has an implant that does not replace the missing foot's coverage
- **THEN** this feature does not make foot-only clothing wearable.

### Requirement: Coverage follows the actual branch and native garment rule
The mod SHALL consider every covered group of the garment and accept any qualifying natural or enabled replacement coverage, preserving the native any-covered-part rule rather than requiring all groups or both paired limbs.
A replacement can supply only its own actual attachment and replaced descendants. A left replacement
cannot stand in for a right-only garment; a hand replacement cannot supply an unrelated shoulder.
Descendant groups such as finger jewelry are included only when that replacement really covers them.
The result does not depend on group order, language or whether the body part has a custom label.
Supported alternative bodies use their actual corresponding groups and attachment relationships;
matching an English name alone does not establish anatomy or compatibility.

#### Scenario: Left-only jewelry and right replacement
- **WHEN** only a right replacement is available and no natural member of the jewelry's left-only group remains
- **THEN** the left-only jewelry remains unavailable, while qualifying right-only jewelry can be worn.

#### Scenario: Later group supplies coverage
- **WHEN** clothing covers hands and feet, only enabled foot replacement coverage is available, and hands appear first in its coverage list
- **THEN** it passes the body-availability rule just as it would with feet listed first.

#### Scenario: One remaining covered member
- **WHEN** native clothing eligibility already accepts a single natural hand or a supported garment has one qualifying replaced branch
- **THEN** this feature does not add a requirement to possess the other hand as well.

#### Scenario: Equivalent body without custom labels
- **WHEN** an admitted alternative body supplies the correct replacement subtree and garment group but lacks human-readable side labels
- **THEN** that missing label alone does not prevent otherwise valid coverage.

### Requirement: Clothing permission does not repair health or bypass other restrictions
The exception SHALL alter only the clothing body-availability decision and retain all other applicable native and supported-provider clothing rules.
Layer conflicts, race/body restrictions, biocoding, age/gender restrictions where applicable, policy,
ownership, reachability, reservation and handling remain in force for the chosen action. A provider's
independent denial is not reinterpreted as missing anatomy. There are no new body parts, restored
digits, changed health/capacities, altered surgery success, free prosthetics or extra armor modifiers.
Ordinary garment protection, costs and equip time remain native; the feature does not extend an
item's protection onto a new body region. Health displays continue reporting the real body state.

#### Scenario: Another garment occupies the layer
- **WHEN** replacement coverage qualifies but an existing garment conflicts
- **THEN** the ordinary clothing replacement/conflict workflow still applies rather than stacking incompatible garments.

#### Scenario: Biocoding or provider denial
- **WHEN** an item is biocoded to another pawn or a supported provider independently disallows it
- **THEN** approved prosthetic coverage does not override that denial.

#### Scenario: Health after wearing jewelry
- **WHEN** a pawn wears newly permitted jewelry over a replaced hand
- **THEN** health, actual missing natural parts, manipulation and surgery state remain unchanged by wearing it.

### Requirement: Replacement preferences have explicit defaults and a complete catalog
The mod SHALL expose separate arm, hand, leg and foot coverage preferences for discoverable replacement candidates, showing the replacement name, source and description, with these initial source-derived choices.

| Attachment section | Initially enabled candidate identities |
| --- | --- |
| Arms | SimpleProstheticArm, BionicArm, AdvancedBionicArm, ArchotechArm |
| Hands | None |
| Legs | SimpleProstheticLeg, BionicLeg, AdvancedBionicLeg, ArchotechLeg, MuscleStimulatorLegs |
| Feet | None; leg defaults are not copied into foot choices |

An enabled preference still requires actual replacement coverage: a muscle stimulator supplied as
an ordinary nonreplacement implant does not restore a lost foot for clothing. New unlisted candidates
default off except the explicitly admitted modular defaults below. An unavailable or unsupported
candidate is identified rather than silently granted coverage. An explicit empty selection means
no added exception and does not disable normal natural-part clothing eligibility.

Show one row per distinct replacement and attachment section, even when several procedures install
it. Include supported conversion procedures that replace an existing prosthetic without prescribing
a new attachment. Search is case-insensitive substring matching on replacement name, stable identifier
and originating mod name; clearing search restores the whole catalog without altering any choice.
Search and viewing do not initialize, overwrite or discard preferences as a side effect.

#### Scenario: Distinct hand and foot choices
- **WHEN** the player first opens the configuration
- **THEN** hand and foot exceptions are initially off, and a leg preference does not also authorize a distinct foot replacement.

#### Scenario: Duplicate installation procedures
- **WHEN** two procedures install the same replacement at the same attachment kind
- **THEN** the catalog contains one shared decision for that replacement and kind.

#### Scenario: Search preserves choices
- **WHEN** the player searches by a provider name, changes one visible choice and clears search
- **THEN** that choice changes and hidden rows retain their previous values.

### Requirement: Preferences use shared inheritance and retain absent content
Each replacement-and-attachment preference SHALL use the common Global default, Current save and Use global controls, with new or missing save entries inheriting the current global value.
The save editor is inactive while inheriting. Checking Use global retains a dormant override;
unchecking restores it or seeds a first override from the current effective value. At the main menu,
both the save editor and Use global are disabled in presentation and mutation callbacks. Explicit
save overrides never change another save or its global default.

Remember choices for unavailable content without activating them or resetting them during search,
save or settings writes. If the same content returns, restore its prior choice; a same-label item
from a different source does not inherit it. Apply missing-entry defaults without replacing explicit
false values. Original-mod settings are not silently imported. Settings belong in ordered mod
settings sections, not a new bottom tab or individual pawn setting. All text uses the seven baseline
languages; stable identifiers can help distinguish same-name candidates without replacing readable names.

#### Scenario: Two saves and a changed default
- **WHEN** one save disables bionic-arm coverage explicitly, another inherits it, and the global choice changes
- **THEN** only the inheriting save follows the new default; switching saves restores the correct effective choices.

#### Scenario: Missing provider returns
- **WHEN** a configured provider is absent for a save/settings write and the same content is later restored
- **THEN** the previous choices remain available rather than being lost or reset by the temporary absence.

#### Scenario: Main-menu settings
- **WHEN** the player opens these settings without an active game
- **THEN** global defaults remain editable while no save-specific callback can change an implicit or previous save.

### Requirement: Eligibility responds to real changes without manufacturing apparel
Changes to effective preferences, installed replacements, body coverage or selected pawn SHALL be reflected at the next relevant ordinary eligibility check without requiring a restart or settings-window visit.
Successful replacement and supported conversion use the resulting installed coverage; failed
operations or lost replacements do not grant the intended future coverage. A pending wear action
rechecks the actual pawn and garment before taking the target from the ground or its apparel source.
If neither native nor admitted replacement coverage now qualifies, stop that wear action before further clothing removal or
target transfer and leave the target in its existing valid location/holder. If an intervening change
occurs after a transfer has already happened, preserve that same physical garment through ordinary
safe return/placement or visible held recovery, without duplication or deletion. A rejected wear
does not add a forced-wear entry or a repeated equip attempt. Earlier completed ordinary drops are
not undone or duplicated by cancellation. Save/load reconstructs availability from current
body state and saved preferences, not a saved permanent permission flag.

Changing a preference alone does not forcibly strip, delete, spawn or re-equip clothing. Already worn
items remain subject to ordinary subsequent game/provider checks; disabling an exception does not
guarantee future retention if native anatomy cannot support them. Ordinary explicit removal and
S024 unloading remain authoritative. Any owned adapter refreshing a preview is read-only and cannot
remove physical apparel or duplicate an equip job.

#### Scenario: Replacement removed before wear completes
- **WHEN** the supporting replacement is lost before a queued wear action executes
- **THEN** the action cannot use an old positive result to equip otherwise unsupported clothing
- **AND** the target garment remains physical in its existing valid location or safe recovery, with no new forced-wear entry.

#### Scenario: Preference changes while approaching ground clothing
- **WHEN** a forced wear job is approaching a garment on the ground and its sole qualifying replacement preference is disabled
- **THEN** the invalid job stops before despawning that garment or removing further worn clothing
- **AND** the ground item remains available without a new forced-wear entry or automatic retry loop.

#### Scenario: Coverage lost while using an apparel source
- **WHEN** a pawn loses the sole qualifying coverage while waiting to take clothing from a supported apparel source
- **THEN** the invalid wear job leaves the same garment in that source before transfer and does not mark it forced
- **AND** if a supported synchronous transition already moved it before invalidity was detected, the same item remains in safe physical recovery rather than disappearing or being recreated.

#### Scenario: Preference changed before opening settings again
- **WHEN** a loaded save changes the effective arm preference and later checks a pawn's gloves
- **THEN** eligibility uses that value without a restart, while the setting change itself performs no physical clothing transfer.

#### Scenario: Reload with different installed state
- **WHEN** a pawn and preferences are saved and loaded
- **THEN** availability reflects the actual surviving replacements and current effective preference, not permission remembered from a removed limb.

### Requirement: Admitted modular replacements preserve equivalent coverage
With the supported Integrated Implants and EBSG combination active and the alternate Medical System Expansion 2 anatomy route absent, the mod SHALL offer modular bionic and archotech arms/legs as equivalent approved candidates, initially enabled in their corresponding arm/leg sections.
Only the four inspected modular arm/leg variants receive these defaults; arbitrary extra modules do
not become new limbs. Their actual resulting attachment and replacement coverage still govern
clothing. Explicit user choices override the defaults and are never reenforced by reopening settings.
Registration and availability do not depend on first visiting settings. Missing or changed content
disables only the affected integration and preserves preferences. No surgery, module capacity or
provider-specific health rule is changed. A different anatomy system needs its own admitted mapping.

#### Scenario: Convert an approved bionic arm
- **WHEN** an ordinary successful supported conversion produces the admitted modular bionic arm before the player has opened settings
- **THEN** its enabled equivalent coverage can support the same clothing without inventing natural fingers.

#### Scenario: Player disables a modular choice
- **WHEN** the player explicitly disables one modular replacement and reopens settings or reloads
- **THEN** discovery does not reset that decision to the initial enabled default.

#### Scenario: Alternate anatomy route or missing module
- **WHEN** the selected anatomy/provider combination no longer supports the assumed modular replacement
- **THEN** no unverified exception is granted, while independent supported replacements remain usable.

### Requirement: Supported outfit previews reflect actual effective eligibility
With the admitted Choose Your Outfit integration active, its selected-pawn clothing view SHALL represent replacement-supplied clothing coverage consistently with current owned preferences and ordinary actual apparel eligibility.
Refresh on selected-pawn, effective-preference or health changes; do not flatten all four attachment
choices into permission for every descendant of every enabled implant. A visual clothing region
does not claim that the pawn's natural anatomy has regrown. The view remains a policy/preview tool:
previewing does not equip items, change health or create resources. Provider layer/filter rules stay
authoritative. Without admitted display support, leave the provider's ordinary view intact and
identify the preview limitation; do not claim complete drop-in compatibility merely because wearing
through the native path works.

#### Scenario: Selected pawn changes
- **WHEN** the player switches from a pawn with an enabled left replacement to one lacking the corresponding coverage
- **THEN** the supported preview updates for the second pawn rather than retaining the first pawn's regions.

#### Scenario: Save-specific preview choice
- **WHEN** a save override changes a replacement's effective permission while the supported view is open
- **THEN** its next refresh reflects that save's choice without altering the global preference or physical clothing.

### Requirement: Co-installation and admission have explicit fallbacks
When the original provider is active, owned overlapping eligibility and dependent preview extensions SHALL remain inactive with a clear explanation, without uninstalling the provider, writing its settings or claiming its identity.
Owned preferences remain editable for later use and persist even while execution is unavailable.
When a required game/provider assumption is missing or incompatible, skip the affected enabled patch
and log one bounded startup warning identifying its target, failed prerequisite, unavailable behavior
and fallback. Compatible unrelated patches alone are not a reason to skip. A lost body-availability
integration leaves ordinary eligibility authoritative; an unsupported optional preview cannot disable
independent native wearing support. No source save or settings conversion is performed implicitly.

#### Scenario: Original provider also active
- **WHEN** a save loads with the original mod active
- **THEN** only the selected original eligibility behavior runs and owned preferences remain preserved without a competing exception.

#### Scenario: Unknown independent apparel veto
- **WHEN** the actual apparel check includes an unsupported independent denial whose meaning cannot safely be preserved
- **THEN** the affected owned exception is withheld with a startup reason instead of forcing the denial to true.
