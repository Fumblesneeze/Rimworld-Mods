## ADDED Requirements

### Requirement: Drug policies follow the current content catalog
Make Rimworld Great Again SHALL maintain one usable row for each currently available drug type in every supported drug policy.
Owner: Make Rimworld Great Again (`fumblesneeze.megamod`, `mods/megamod`), target RimWorld 1.7 beta.
Available means enabled game content classified as a drug item, not a quantity currently owned by
the colony. Reconciliation occurs after content choices are final and before the affected policy is
used following load, creation, copying or an admitted content change. It removes missing, disabled
and no-longer-drug entries; it does not delete physical items, illnesses or addiction records.

#### Scenario: Add content to an existing save
- **WHEN** the player loads a supported save after adding a drug type
- **THEN** each existing policy contains that drug once with the new-row defaults
- **AND** rows for other available drugs retain their applicable choices.

#### Scenario: Remove or disable content
- **WHEN** a drug has been removed from the active content or disabled so that it is no longer a usable drug
- **THEN** its old policy row is removed even if an old reference or physical item remains
- **AND** maintenance does not delete the physical item or change a pawn's health to achieve the cleanup.

#### Scenario: No colony stock
- **WHEN** a valid drug is out of stock, unresearched, forbidden or unreachable
- **THEN** its policy row and choices remain available
- **AND** normal availability rules still prevent an impossible drug-taking or refill job.

#### Scenario: Empty policy data or no drugs
- **WHEN** a supported policy has no saved rows, only invalid rows, or no enabled drugs exist
- **THEN** maintenance builds the applicable current rows or leaves a valid empty policy
- **AND** the policy remains selectable with the native empty-state display instead of being deleted.

### Requirement: Existing choices and assignments survive maintenance
The feature SHALL retain each surviving policy's identity, name, assignments, default status and applicable row choices.
It preserves scheduled permission, frequency, mood/recreation thresholds and inventory quantity.
It only clears addiction permission when a drug is not addictive and pleasure permission when the
drug cannot provide pleasure; these inapplicable permissions do not automatically return if the
drug later gains that property. For conflicting duplicate rows, the first previously effective row
wins as a whole: permissions are not combined and quantities are not added.

#### Scenario: Edited policy remains edited
- **WHEN** the player loads a renamed policy with custom drug permissions, thresholds and inventory amounts
- **THEN** valid choices remain attached to the same drugs and assigned pawns
- **AND** the default policy and ordering of policies are unchanged.

#### Scenario: Drug properties change
- **WHEN** a retained drug ceases to be addictive or ceases to provide pleasure
- **THEN** only the now-inapplicable addiction or pleasure permission is cleared
- **AND** scheduled choices and inventory targets remain unchanged
- **AND** a later return of the property does not itself enable the cleared permission.

#### Scenario: Conflicting duplicates
- **WHEN** a supported policy contains multiple rows for the same drug with different choices
- **THEN** only its first previously effective row remains, retaining its complete choices
- **AND** repair reports the discarded duplicate without combining permissions or increasing inventory amounts.

### Requirement: New rows use explicit native defaults
The feature SHALL initialize a newly introduced drug in an existing policy with the inspected 1.7 defaults below, without deriving permission from the policy's name.
The same defaults apply to a newly created blank policy. Existing rows are never re-defaulted merely
because their values happen to match these defaults. These are gameplay policy values, not new mod
settings. A fully removed row that later returns is a new row; no hidden historical choice is restored.

| Choice | Initial value |
| --- | --- |
| Satisfy addiction | On only if the drug is addictive |
| Use for recreation | Off |
| Scheduled use | Off |
| Schedule frequency, while inactive | 1 day |
| Mood/recreation thresholds, while inactive | 100% / 100% |
| Take to inventory | 0 |

#### Scenario: New drug in a restrictive or permissive named policy
- **WHEN** a new drug is added to an existing policy named No drugs, Social drugs, Unrestricted or any custom name
- **THEN** it receives the same listed new-row defaults
- **AND** the name does not grant recreational use, a schedule or a carried supply.

#### Scenario: Rename or language change
- **WHEN** the player renames a policy to a built-in policy name or changes language
- **THEN** existing permissions remain unchanged
- **AND** the next introduced drug still receives the listed new-row defaults.

#### Scenario: Return after saved removal
- **WHEN** the player saves after a drug row was removed and later reloads with that drug enabled again
- **THEN** the returned row uses new-row defaults rather than an undocumented old schedule or inventory target.

### Requirement: Preset construction retains valid native choices
The feature SHALL allow supported policy presets to be created using their valid current choices while ignoring entries for unavailable drugs before those entries can prevent construction.
Fresh preset creation is distinct from adding content to an already existing policy. The active
preset's valid explicit choices and broad pleasure permission apply during its creation; afterward
only inapplicable addiction/pleasure permissions are cleared. Do not rewrite a preset's unrelated
entries or reinterpret its name. This is specific unavailable-drug recovery, not arbitrary save or
content-error recovery.

#### Scenario: Fresh ordinary presets
- **WHEN** the player starts a Core game with the ordinary drugs enabled
- **THEN** Social drugs permits Beer and Smokeleaf recreation, and Unrestricted permits pleasure drugs
- **AND** One drink per day retains its daily Beer schedule and inventory quantity of 1, with Smokeleaf recreation
- **AND** No drugs retains native addiction-only permission for addictive drugs rather than gaining recreational or scheduled use.

#### Scenario: Preset refers to removed content
- **WHEN** a supported preset contains a now-unavailable drug alongside valid entries
- **THEN** the policy is created without that unavailable row or its missing-drug error
- **AND** the valid entries retain the preset's intended values.

#### Scenario: Customized or external valid preset
- **WHEN** supported active content supplies different valid explicit preset choices
- **THEN** a fresh policy uses those choices rather than hard-coded replacements for familiar preset names
- **AND** loading an existing policy does not reapply the preset over the player's edits.

### Requirement: Alphabetical navigation does not change drug priority
The feature SHALL show drug rows in alphabetical order using the displayed language while leaving native drug-taking and inventory-refill priority unchanged.
Rows with equal displayed names keep a stable distinguishable ordering for the current content and
language. Each control and tooltip remains attached to the intended drug. Alphabetizing does not
reorder policies, choose a new default or reset a pawn's dose history. Sorting is navigation, not
a new drug-choice policy.

#### Scenario: Find and edit a drug
- **WHEN** the player opens a drug policy containing drugs whose native order is not alphabetical
- **THEN** the window displays them alphabetically
- **AND** editing a row changes that drug's choices, including when another drug has the same displayed name.

#### Scenario: Two eligible scheduled drugs
- **WHEN** two available scheduled drugs are simultaneously eligible and the alphabetically first is not native-first
- **THEN** opening, editing an unrelated row or closing the alphabetized window does not change which drug the native priority selects
- **AND** ordinary frequency, overdose, access and other permission checks remain effective.

#### Scenario: Two carried-supply deficits
- **WHEN** a pawn could refill two permitted drugs and the display order differs from native priority
- **THEN** the pawn's first refill remains the native-priority choice
- **AND** viewing the policy does not increase either inventory target or bypass carrying constraints.

#### Scenario: Newly enabled drug has earlier native priority
- **WHEN** a supported content change enables a drug during play that native initialization would prioritize before an existing drug and the player configures both for use or carrying
- **THEN** the next choice between equally eligible available drugs follows that complete native ordering, including the new drug
- **AND** saving and reloading before that job, with the same eligibility and dose history, does not change the choice merely because the new row was previously appended.

#### Scenario: Display changes during editing
- **WHEN** language or admitted content changes require the displayed rows to refresh
- **THEN** an in-progress edit cannot be applied to a different drug that moved into the same screen position
- **AND** a removed selected drug loses its editor without transferring that edit to another row.

### Requirement: Native policy controls remain contextual gameplay controls
The feature SHALL retain native policy management and pawn assignment entry points without adding a bottom tab, debug command, operational toggle or mod-settings policy editor.
Rename, copy, default selection and deletion retain their native meaning. Drug policies and assigned
pawns are save-local gameplay state, not global-default/per-save mod configuration. The native
default-policy choice must not be confused with a global mod default.

#### Scenario: Copy and assign a policy
- **WHEN** the player copies a valid policy and assigns the copy to another pawn through native controls
- **THEN** the copy retains the source's valid choices independently and displays the same current drug catalog
- **AND** later edits to the copy do not change the source or another save.

#### Scenario: Default and delete controls
- **WHEN** the player changes the default policy or attempts to delete an assigned policy
- **THEN** ordinary default assignment and in-use deletion restrictions remain effective
- **AND** alphabetical drug display does not choose the default on the player's behalf.

#### Scenario: Main menu and save switching
- **WHEN** the player returns to the main menu or opens another save
- **THEN** maintenance does not edit a previous save's policies through a mod-settings control
- **AND** the loaded game's own policies and assignments determine the available choices.

### Requirement: Content and scheduled-use features compose directly
The feature SHALL reconcile against completed content choices and preserve all additional applicable drug-use restrictions.
Owned content filtering performs the same reconciliation without separate patches between owned
features. External filtering is optional and must be admitted for its actual supported behavior.
Maintenance neither grants use of disabled content nor treats shared physical-stock search results
as the drug catalog. The Common Sense potential-mood rule remains an additional restriction on
otherwise allowed scheduled use.

#### Scenario: Owned content filtering
- **WHEN** an owned content choice takes effect and disables a drug
- **THEN** the completed content change and policy reconciliation agree that it is unavailable
- **AND** maintenance does not recreate it as an enabled policy row on the next load.

#### Scenario: Optional external filtering
- **WHEN** a supported external filter disables a drug while keeping its old item data
- **THEN** the drug loses its policy row without requiring deletion of the old item data
- **AND** the same policy-maintenance behavior works when that external filter is absent.

#### Scenario: Additional scheduled-use restriction
- **WHEN** a surviving row allows scheduled use but the enabled potential-mood condition rejects taking the drug now
- **THEN** maintenance does not make the pawn take the drug or erase that additional restriction
- **AND** newly defaulted rows with scheduled use off cannot gain a job from that condition alone.

### Requirement: Repair is repeatable and persists through ordinary saves
The feature SHALL make each supported repair once per affected state, preserve valid edits across ordinary save/load, and avoid partial owned repairs on failure.
Prepare a complete result before committing an owned policy repair; restore that policy's prior
state if its commit fails and report the unavailable repair. Do not write external save files or
autosave on the player's behalf. Retain original on-disk data until the player saves normally.
Repair reports accurately summarize added, removed, duplicate and normalized rows across policies;
unchanged policies produce no repeated normal log chatter.

#### Scenario: Repeated successful reconciliation
- **WHEN** a repaired policy is checked again without a content or policy change
- **THEN** no extra rows, permissions, inventory quantities or repair notifications are produced.

#### Scenario: Save and reopen repaired policies
- **WHEN** the player saves normally after a successful repair and reloads that save
- **THEN** the repaired choices, assignments and default remain in effect without duplicate repair
- **AND** valid dose history is preserved rather than making a scheduled dose due early.

#### Scenario: Abandon an unsaved repair
- **WHEN** the player exits without saving after a content-removal repair
- **THEN** the existing save file has not been rewritten by this feature
- **AND** a later load evaluates that original saved state against the then-active content.

#### Scenario: Repair failure
- **WHEN** an owned repair cannot commit its complete supported result
- **THEN** the affected policy retains its pre-repair choices rather than a partially changed result
- **AND** a bounded warning identifies the failed repair without claiming that unrelated content corruption was fixed.

### Requirement: Compatibility checks isolate unsupported behavior
The feature SHALL check each required patch target before enabling its behavior and warn at startup when an enabled patch cannot be safely applied.
Warnings identify the patch, failed condition, affected behavior and fallback. Compatible foreign
changes and unrelated method edits are allowed. With the original Drug Policy Fix active, the owned
overlapping maintenance and presentation are withheld with an explanation; no foreign patch is
removed and no source identity is impersonated. If the early maintenance group is unavailable,
native/provider behavior remains and missing-content recovery is not claimed. Presentation failure
alone leaves native row order while admitted maintenance remains available.

#### Scenario: Compatible foreign changes
- **WHEN** another mod changes unrelated behavior while the required policy and row semantics remain supported
- **THEN** the owned feature can operate rather than rejecting the target solely because another patch exists.

#### Scenario: Unsupported maintenance or display
- **WHEN** required maintenance semantics are missing or ambiguous
- **THEN** the affected maintenance group is inactive and startup reports the unavailable cleanup/preset recovery
- **AND** if only presentation is unsupported, maintenance remains available with native row ordering and a display warning.

#### Scenario: Available drug has incomplete policy data
- **WHEN** a currently available drug lacks data required by the game's drug policies
- **THEN** owned policy maintenance is unavailable and leaves all policy choices unchanged, rather than treating that drug as removed and deleting its rows
- **AND** a bounded explanation identifies the affected drug and unsupported content condition without claiming to repair that content
- **AND** a drug already excluded by applied content choices does not prevent maintenance of the remaining supported drugs.

#### Scenario: Original provider active
- **WHEN** the original Drug Policy Fix is active alongside the megamod
- **THEN** owned overlap is withheld and its reason is reported
- **AND** the source is not unpatched and its own sorting/repair behavior is not advertised as owned corrected behavior.
