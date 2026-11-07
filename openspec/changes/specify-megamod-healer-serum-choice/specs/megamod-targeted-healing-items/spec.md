# Targeted healing items

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`).
Target **RimWorld 1.7 beta**. S089; proposed observable behavior, not implementation acceptance.

## ADDED Requirements

### Requirement: Choose after ordinary treatment preparation
The feature SHALL open a condition chooser after a supported item's ordinary use or medical
administration work completes, before applying its treatment or consuming it. The chooser SHALL
identify the actual item and recipient, pause ordinary single-player simulation, require an explicit
selection, and offer Confirm and Cancel. It SHALL retain ordinary item access, actor capability,
recipient restrictions and configured preparation work; a patient need not perform the doctor's work.

#### Scenario: Direct serum use
- **WHEN** a capable pawn completes the ordinary use action with an accessible healer serum
- **THEN** the chooser opens for that pawn after the configured use time, with no condition selected
- **AND** the serum has not yet been spent and no condition has been healed

#### Scenario: Administer to a patient
- **WHEN** a doctor completes an allowed healer-serum medical bill for a humanlike or animal patient
- **THEN** the chooser concerns that patient, rather than the doctor
- **AND** being unable to walk or manipulate does not itself require the patient to perform the doctor's action

#### Scenario: Normal restriction still applies
- **WHEN** the required item is inaccessible, the user cannot perform the action, or the recipient is excluded by the supported item's normal restrictions
- **THEN** the normal action is unavailable or fails with its reason, without opening a usable chooser or spending an item

### Requirement: Show eligible conditions with their consequences
The chooser SHALL list currently visible, harmful, item-curable conditions which are not fully
immune. A missing part SHALL be selectable only when neither it nor an ancestor has a directly added
replacement and its parent is present. Entries SHALL identify body part or whole body, condition and
the ordinary severity description, in ordinary health-list priority order, with scrolling as needed.
The displayed consequences SHALL include any same-condition group cure or descendant-part restoration.

#### Scenario: Conditions remain distinguishable
- **WHEN** the recipient has two eligible scars on different body parts
- **THEN** both entries identify their own location and condition, and selecting one clearly identifies the intended target

#### Scenario: Excluded conditions
- **WHEN** a condition is hidden, beneficial, not curable by an item, or already fully immune
- **THEN** it is not offered as a treatment choice
- **AND** a missing child part beneath a missing parent or an added replacement is not separately offered

#### Scenario: No eligible condition
- **WHEN** the patient has no eligible choice when the action is offered or preparation finishes
- **THEN** the action is unavailable or ends with a clear no-eligible-condition explanation
- **AND** no serum is spent and there is no empty confirmation window or automatic worst-condition fallback

#### Scenario: Explain a broader native cure
- **WHEN** the selected condition's ordinary item cure clears all instances together, or a missing limb's restoration also restores its descendants
- **THEN** that broader consequence is explained before confirmation
- **AND** the window does not promise that only the single displayed row will change

### Requirement: Confirm one paid native treatment
Confirmation SHALL recheck the same recipient, selected condition and physical item before beginning
treatment. An admitted treatment SHALL consume exactly one physical item and perform that selected
condition's ordinary item-cure operation once, with its ordinary notification and item-use feedback.
Other eligible conditions SHALL remain unless the selected cure normally affects them. Preparation
time, item availability, market value, cure power and acquisition SHALL remain supplied by the item
and game; this feature SHALL add no crafting, free treatment, partial dose or numerical healing bonus.

#### Scenario: Cure a chosen scar
- **WHEN** the player confirms an eligible scar while another eligible disease is present
- **THEN** one serum is consumed and the selected scar is cured
- **AND** the unrelated disease remains and the appropriate treatment feedback occurs once

#### Scenario: Restore a limb or cure a condition group
- **WHEN** the player confirms an eligible missing limb or a condition that ordinarily cures all instances together
- **THEN** one item pays for that ordinary restoration or group cure
- **AND** descendant restoration and conditions retained during restoration follow the game's normal rules

#### Scenario: Confirmation is delivered twice
- **WHEN** the same pending treatment receives repeated confirmation input
- **THEN** at most one treatment and one item expenditure occur
- **AND** separate physical doses used for other patients remain independent

#### Scenario: Unexpected failure after treatment begins
- **WHEN** an unexpected provider failure interrupts a confirmed treatment after its cure may already have taken effect
- **THEN** the feature reports that the treatment was interrupted and whether its dose was spent
- **AND** it does not automatically repeat the cure or refund an already-applied treatment
- **AND** unrelated queued treatments remain available; a partial result is not reported as success

### Requirement: Cancellation and stale choices preserve unused doses
Cancel, ordinary window dismissal, saving or leaving the current game before confirmation SHALL
cancel the pending choice without treatment or dose expenditure. The unused item SHALL remain
accounted for and available through ordinary storage or placement rules. Rejected or stale choices
SHALL never select a replacement condition automatically. A cancelled medical treatment SHALL not
reopen itself without a new player order. Pending choices SHALL not survive loading another save.

#### Scenario: Cancel or dismiss
- **WHEN** the player cancels or dismisses the chooser before confirming
- **THEN** no cure occurs, the unused dose is retained, and any temporary claim on it is released
- **AND** medical administration does not repeatedly reopen the chooser on its own

#### Scenario: Target or dose becomes invalid
- **WHEN** the selected condition disappears or becomes ineligible, the patient dies or leaves the supported context, or the exact dose becomes unavailable before confirmation
- **THEN** confirmation performs no treatment and spends no available dose
- **AND** it explains the changed situation and requires a fresh selection or new order as appropriate

#### Scenario: Two pending orders claim the same resource
- **WHEN** another order attempts to use the same dose or open another pending treatment for the same patient
- **THEN** it does not create a second actionable claim or duplicate chooser
- **AND** cancellation or completion releases the restriction without an endless retry loop

#### Scenario: Save and reload while choosing
- **WHEN** the player saves, leaves the map/game context, or switches saves with an unconfirmed treatment
- **THEN** the pending choice is cancelled and the unused dose remains represented in its original game
- **AND** loading that save or a different save does not replay a cure, lose the dose or reuse the old choice

### Requirement: Retain contextual medical controls
The feature SHALL use the item's existing use controls and the patient's existing medical-bill
surface, with the chooser as a contextual gameplay window. It SHALL add no bottom tab, settings row,
bottom-right toggle or unrestricted debug healing action. Target choice SHALL be patient-specific
gameplay state, not a global or per-save configuration value. New labels, reasons and consequences
SHALL be understandable in all seven baseline languages and remain operable with long labels.

#### Scenario: Discover and complete treatment
- **WHEN** the player uses the ordinary serum command or patient medical bill and opens the chooser
- **THEN** selection, consequences, confirmation and cancellation are reachable in that context without another management tab

#### Scenario: Long labels and main menu
- **WHEN** the player uses a supported language with long item, part or condition names, or returns to the main menu
- **THEN** the treatment window preserves readable distinctions and accessible controls during play
- **AND** no patient choice or stale treatment is editable from the main menu

### Requirement: Isolate optional healing and automation
Supported Archotech Medicine and Healing Potion providers SHALL receive the same choice workflow
only for the specifically admitted healing item, retaining that item's ordinary cost and effects.
Absent or unsupported providers SHALL not disable independent Core treatment. Automatic external
healing, including More Faction Interaction's shaman, SHALL retain its ordinary target selection,
cost and event outcome; it SHALL neither open this chooser nor require a carried serum. Unnatural
Healing, mutagenic serums and resurrector serums SHALL remain outside this feature. Other owned
medical features SHALL not acquire permission to order or confirm a rare treatment autonomously.

#### Scenario: Supported optional potion
- **WHEN** a supported provider's Healing Potion is used through its admitted normal action
- **THEN** the chooser names that potion and its recipient, and one confirmed treatment spends its ordinary dose
- **AND** other items from that provider remain unchanged

#### Scenario: Absent or changed optional item
- **WHEN** an optional provider is absent, or present with an unsupported healing-item shape
- **THEN** absent content needs no adapter, or the affected adapter remains inactive with a bounded startup explanation
- **AND** independently supported healer-serum use remains available

#### Scenario: Shaman and other medical policies
- **WHEN** a supported shaman event heals its chosen caravan member, or an owned medical scheduling policy updates
- **THEN** the shaman keeps its automatic event treatment without a chooser or inventory-serum charge
- **AND** the scheduling update alone does not order, select or spend a healer serum

### Requirement: Publish only admitted single-provider behavior
The feature SHALL activate only where its documented treatment and content prerequisites hold.
Compatible surrounding changes SHALL remain allowed. Missing, changed or ambiguous prerequisites
SHALL withhold the affected behavior with a bounded startup warning naming the skipped patch, reason,
unavailable behavior and fallback. The original Healer Mech Serum Choice SHALL suppress overlapping
owned behavior whenever loaded, with a bounded startup warning; initial cooperative-original support is excluded. Multiplayer choice SHALL remain inactive
until an admitted synchronized treatment path exists; the original mod's adapter alone SHALL not
count as support. Already-earned health state SHALL require no migration.

#### Scenario: Compatible surrounding addition
- **WHEN** another mod adds unrelated item data or preserves the required treatment semantics
- **THEN** the choice workflow remains eligible without deleting that mod's additions

#### Scenario: Incompatible target or original provider
- **WHEN** a required treatment contract is unavailable or the original chooser is active
- **THEN** the owned overlap is withheld, independent behavior is retained, and the startup explanation identifies what remains in control
- **AND** there is no partial chooser that consumes an item without its admitted treatment path

#### Scenario: Multiplayer without owned synchronization
- **WHEN** the game enters multiplayer without a supported owned treatment adapter
- **THEN** the owned choice workflow is unavailable with a clear explanation, preserving the ordinary provider's use behavior
- **AND** a client cannot cause a local-only cure or dose expenditure through this chooser
