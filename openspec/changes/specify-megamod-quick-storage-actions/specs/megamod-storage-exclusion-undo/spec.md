# Contextual storage exclusion and undo

Owner **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
Origin replacement S107, Quick Stockpile Creation. Specification only; no implementation accepted.

## ADDED Requirements

### Requirement: Exclude types or categories from the actual storage policy
The mod SHALL offer contextual exclusion for selected visible haulable items currently accepted by supported player-editable storage, with direct exact-type exclusion and explicit category alternatives.

The action identifies the target storage or linked group and affected type/category before changing
it. Packed objects use the contained storable type and outer physical location. The command changes
each affected effective policy once; repeated stacks or linked members do not repeat the edit.
Mixed eligible selections can affect multiple clearly identified policies in one batch. Unstored
or unsupported selected items are identified as omitted, never assigned an imagined destination.

#### Scenario: Same type in two storages
- **WHEN** accepted steel stacks in two independent supported storages are selected and excluded
- **THEN** steel is disabled in both identified policies once
- **AND** other independent storage policies remain unchanged

#### Scenario: Linked storage shares one edit
- **WHEN** an item in a shelf linked to a ground stockpile is excluded
- **THEN** the effective linked policy changes once and all group members show that choice
- **AND** each member's physical restrictions remain unchanged

#### Scenario: Category action across a selection
- **WHEN** selected accepted items share Apparel and the player explicitly chooses category exclusion
- **THEN** Apparel's applicable descendant types are excluded from every identified affected policy
- **AND** the choice does not silently affect only whichever item happened to be selected first

#### Scenario: Packed and unstored items
- **WHEN** the selection includes a packed object in supported storage and a loose item outside storage
- **THEN** the stored object's contained type is excluded from its actual policy while the unstored item is omitted with an explanation

### Requirement: Change only item allowances and keep physical contents intact
The mod SHALL remove only the requested item-type allowances, preserving other item choices, special-state filters, quality/condition/risk ranges, priority, names, group membership and refill controls.

Category exclusion removes its descendant item types, not unrelated special-filter options.
Items are never deleted, moved, teleported, damaged or unpacked by editing a policy. Normal hauling
can subsequently react under its existing capacity, access, reservation and destination rules.
Later presets, manual edits and copy/paste retain their own contracts rather than creating a
permanent named exclusion which continuously reapplies itself.

#### Scenario: Exclude apparel while retaining other policy
- **WHEN** a storage with a quality minimum, condition range, taint restriction and refill limits excludes Apparel
- **THEN** apparel types are removed while those restrictions and all non-apparel choices remain unchanged

#### Scenario: Items wait for ordinary hauling
- **WHEN** an accepted stack becomes disallowed by the command
- **THEN** it remains physically intact in place until ordinary gameplay moves it
- **AND** lack of a valid destination does not delete it or force an impossible haul

### Requirement: Validate and apply the intended batch atomically
The mod SHALL revalidate selection, physical source storage, policy identity and group membership before exclusion, then apply the complete intended batch or leave all its policies unchanged.

An open stale menu never follows an item to an unexpected destination or edits a newly composed
group. Each affected policy's latest unrelated choices are preserved. Validation/application
failure restores the complete pre-command policy state for that batch and reports failure; it
does not leave some requested storages changed. No item change is part of this transaction.

#### Scenario: Item moves or group changes
- **WHEN** an item changes storage or a target group changes membership after the menu is opened
- **THEN** the old action changes no policy and requests a fresh selection/menu

#### Scenario: One policy cannot be committed
- **WHEN** one destination in a prepared multi-storage batch cannot safely accept the edit
- **THEN** none of the batch remains changed and the player receives a failure explanation

#### Scenario: Unrelated newer edit
- **WHEN** priority or another unrelated policy field changes before an otherwise valid exclusion commits
- **THEN** that latest choice survives and only the requested type allowances are removed

### Requirement: Undo the actual exclusion rather than broadly reallow a category
The mod SHALL retain the three most recent successful exclusion commands for the current loaded-game session and offer precise undo through affected storage's existing configuration, including after the sample item has been hauled away.

One batch counts as one command, even when it touches multiple types or policies. Undo identifies
the whole batch and restores only allowances that command actually removed; previously excluded
types remain excluded. Later unrelated edits survive. Undo is refused without changes if an affected
allowance was subsequently edited, any target/group identity changed, or the system cannot prove
that restoration still belongs to that command. Successful undo consumes that record. No-op and
failed commands do not evict history. History is transient, not saved or shared between games.

#### Scenario: Prior exclusion survives category undo
- **WHEN** hats were already disallowed, a new command excludes Apparel, and that command is undone
- **THEN** only previously allowed apparel types are restored and hats remain disallowed
- **AND** no unrelated special filter or range changes

#### Scenario: Unrelated and overlapping later edits
- **WHEN** priority changes after exclusion and the player undoes it
- **THEN** the new priority survives while the command's removed allowances are restored
- **WHEN** an affected allowance was manually changed afterward, even back to the same apparent value
- **THEN** the stale undo is refused with an explanation rather than overriding the newer intent

#### Scenario: Sample item has left
- **WHEN** the excluded stack is hauled away but the target policy and undo remain valid
- **THEN** the affected storage's configuration still offers that undo without needing the original stack selected

#### Scenario: Three command limit and reload
- **WHEN** four successful exclusion commands have been made in this loaded-game session
- **THEN** only the newest three are offered for undo, each clearly identifying its affected scopes
- **WHEN** the game is unloaded or a save is loaded
- **THEN** no prior transient undo is applied or reused, while the stored filters retain their actual saved choices

### Requirement: Keep exclusion compatibility and presentation coherent
The mod SHALL enable exclusion and undo only for admitted effective storage/edit/observation contracts, preserving ordinary controls when unsupported.

Original-provider presence suppresses the overlapping owned actions without changing that provider.
Every skipped enabled patch logs a bounded startup warning identifying the failed condition,
unavailable behavior and fallback. Compatible other owners alone do not disqualify a context.
Partial activation or supported later invalidation removes only owned affected actions and invalid
history, without undoing a previously committed filter edit automatically. All action/scope/undo/
failure text uses the seven baseline languages. These are contextual gameplay controls, not per-item
mod settings, a new bottom tab or a map-wide item cache.

#### Scenario: Unknown storage adapter
- **WHEN** the item's storage has an unsupported policy or edit-history contract
- **THEN** the owned exclusion/undo is unavailable with a reason and ordinary provider controls remain usable

#### Scenario: Feature disabled after an edit
- **WHEN** the owned actions become unavailable after a successful exclusion
- **THEN** the actual filter remains as the player last set it, ordinary manual editing works, and no stale undo later modifies it

#### Scenario: Localized shared controls
- **WHEN** the player excludes and undoes through a supported shelf, zone or linked group in a baseline language
- **THEN** the affected policy names, content choices and unavailable reasons are clear on the existing contextual surfaces
- **AND** no second preset menu, automatic filter reapplication or physical stock mutation is introduced
