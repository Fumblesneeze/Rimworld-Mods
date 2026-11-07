# Linked stockpiles and shelves

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`). Target: 1.7 beta.
Origin: original. Specification only; no implementation or live acceptance.

## ADDED Requirements

### Requirement: Link stockpiles and shelves through their contextual controls

The player SHALL be able to link stockpile zones, shelves, or a mixture of both on the same map
using the native-style Link settings command. Only selected eligible storage members SHALL join;
other selected objects SHALL not acquire storage behavior. The command SHALL explain when fewer
than two eligible members are selected or they already share the same group. This feature SHALL
not add a bottom tab or move storage-specific controls into mod settings.

#### Scenario: Create a mixed warehouse group
- **WHEN** the player selects one ground stockpile and two shelves and chooses Link settings
- **THEN** all three become one linked group accessible through their contextual storage controls.

#### Scenario: Selection does not imply membership
- **WHEN** a selected set also contains a non-storage building
- **THEN** linking affects only eligible storage members and leaves that building unchanged.

### Requirement: The first selected eligible member supplies the initial policy

Linking SHALL use the first selected eligible member's effective storage policy. If it already
belongs to a group, that group SHALL be the destination. The command SHALL identify the member
whose policy will be used before activation. Linking SHALL not merge differing policies by union
or silently choose a different member based on its type.

#### Scenario: Priority and filter differ before linking
- **WHEN** a Critical food stockpile is selected first, a Low weapon shelf second, and they are linked
- **THEN** both use Critical priority and the food choices from the stockpile.
- **AND** selecting the shelf first instead uses its Low priority and weapon choices.

### Requirement: Linked members share the complete editable storage policy

Every linked member SHALL show and edit the same item/category choices, special filter choices,
quality and condition ranges, other available native storage filter restrictions, and priority.
An edit through any member or the selected group SHALL apply to all members immediately for
subsequent storage decisions. Existing physical items SHALL remain subject to normal hauling;
editing a filter SHALL not teleport or destroy them.

#### Scenario: Edit a shelf in a mixed group
- **WHEN** the player raises the minimum allowed quality and changes priority through a linked shelf
- **THEN** the linked ground stockpile shows the same choices and subsequent hauling uses them.

### Requirement: Shared policy does not remove physical restrictions

A member SHALL accept an item only when both the shared policy and that member's intrinsic rules
permit it. Group controls SHALL allow choices usable by at least one member and distinguish
restrictions that prevent particular members from accepting them. Shelf stack capacity, protection
from deterioration and other physical properties SHALL remain specific to the shelf; linking SHALL
not grant these properties to ground cells. Unsupported storage types SHALL not join accidentally.

#### Scenario: Chunks belong on the ground
- **WHEN** a group contains a shelf and a stockpile and the player enables chunks in its policy
- **THEN** eligible chunks can be hauled to the stockpile but not onto the shelf.
- **AND** linking does not increase ground stack capacity or prevent outdoor deterioration there.

### Requirement: Moving a selected member does not move its whole former group

When selected members join a different group, only those selected members SHALL move. Remaining
members SHALL keep their former policy. A former group reduced to one member by this action SHALL
leave that member independent with its last effective policy, matching native shelf unlinking.

#### Scenario: Move one shelf from another warehouse
- **WHEN** a member of group A is selected first and one shelf from group B is selected second and linked
- **THEN** the selected shelf joins A and uses A's policy while unselected members of B keep B's policy.

### Requirement: Unlinking preserves the last policy without continued synchronization

Unlinking SHALL give each departing member an independent copy of the shared policy at that time.
Later edits to either side SHALL not affect the other. If removing a member dissolves a group with
one survivor, that survivor SHALL also retain its last policy. Repeating unlink on an independent
member SHALL not reset its choices.

#### Scenario: Separate two linked stockpiles
- **WHEN** the player unlinks one of two stockpiles
- **THEN** both keep the former shared filter and priority as independent policies.
- **AND** changing one afterward leaves the other unchanged.

### Requirement: Copy and paste operate on the effective policy

Copying from a linked member SHALL copy its shared policy. Pasting into any linked member SHALL
update the whole receiving group. Copy/paste SHALL not change group membership or copy physical
storage properties. Pasting into an independent member SHALL affect only that member.

#### Scenario: Paste into one member
- **WHEN** the player copies an independent stockpile's policy and pastes it into a linked shelf
- **THEN** every member of the receiving group uses that policy, subject to its own physical limits.
- **AND** the source stockpile remains independent.

### Requirement: Groups can be recognized and selected as one

Linked storage SHALL expose its group name and member count, support renaming the group, highlight
its connected members, and offer Select linked. These actions SHALL include zone cells as well as
shelves. Renaming the group SHALL not erase individual zone or storage names needed after unlinking.

#### Scenario: Find all linked storage
- **WHEN** the player selects one mixed-group member and activates Select linked
- **THEN** all available members of that group on the map are selected and can be identified visually.

### Requirement: Membership survives ordinary storage lifecycle changes

Groups and policies SHALL persist through save/reload. Resizing a zone SHALL change its usable cells
without changing its group policy. Deleting a member SHALL remove only that member and preserve
survivors' policies. A shelf moved to another map SHALL become independent with its effective policy;
groups SHALL not synchronize across maps. Bill destination and item-count choices SHALL continue to
refer to valid surviving storage, following native invalid-destination handling when a group ceases
to exist for ordinary unlinked bills. Bills governed by whole-list workbench linking instead SHALL
use that feature's explicit unresolved-scope recovery, retaining the missing choice and stopping new
work until the player resolves it. Linking SHALL not duplicate physical items or their contribution
to a selected group total.

#### Scenario: Resize and reload
- **WHEN** a linked zone is expanded and the game is saved and loaded
- **THEN** its current cells and surviving members retain their shared policy and group identity.

#### Scenario: Remove a destination member
- **WHEN** a shelf is destroyed while its group still contains several valid members
- **THEN** the remaining group continues to work and a bill using that group still has a valid destination.

#### Scenario: Selected storage group disappears from linked workbench bills
- **WHEN** a storage group ceases to exist and a whole-list workbench bill used it for counting or output
- **THEN** the linked row retains the unresolved choice and blocks new starts until the player resolves it
- **AND** ordinary unlinked bills still follow their native invalid-destination handling

### Requirement: Incompatible integration leaves ordinary storage usable

If linked-zone support cannot operate safely, it SHALL be inactive with a bounded startup warning
describing the unavailable behavior and fallback. Compatible unrelated patches SHALL not by
themselves prevent linking. Ordinary independent stockpiles and native shelf controls SHALL remain
usable. No existing save SHALL be silently rewritten to convert third-party storage groups.

#### Scenario: Changed storage integration
- **WHEN** required storage behavior is unavailable at startup
- **THEN** the warning explains that mixed linking is unavailable and ordinary storage remains usable.
