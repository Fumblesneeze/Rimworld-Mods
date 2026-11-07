# Workbench bill management

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`). Target: 1.7 beta.
Origin: replacement. Specification only; no implementation or live acceptance.

## ADDED Requirements

### Requirement: Copy captures the selected production choices
The mod SHALL offer Copy for one production bill and Copy all for a workbench's ordered production list, capturing their editable choices at that moment.
Later source edits or deletion do not alter that snapshot. Copies include recipe, native name/style,
ingredient choices, radius, worker choices, repeat/count/delivery choices and supported owned modifiers.
They exclude physical ingredients, unfinished work, assigned machine subjects, reservations, progress,
completion credits and link membership. Copy all preserves distinct rows of the same recipe. An empty
or unsupported list cannot silently copy a partial list or retain an older clipboard as if copied anew.
The clipboard is temporary within the current game and clears when another save is loaded or the game
is left; it is not an export/import format.

#### Scenario: Edit source after copying
- **WHEN** the player copies a bill for ten steel swords and then changes or deletes the source
- **THEN** the pending copy still describes the original ten-sword order and its steel choice
- **AND** it contains no physical work or link to the former source.

#### Scenario: Copy distinct repeated recipes
- **WHEN** Copy all captures two identical recipes with different materials and quantities
- **THEN** both appear in their original order with their separate choices.

#### Scenario: Empty list and save switch
- **WHEN** the player tries Copy all on an empty or unsupported list
- **THEN** the action explains why it cannot copy and does not present stale clipboard contents as a new copy
- **AND** loading another save clears any previous clipboard.

### Requirement: Paste appends a complete independent order or changes nothing
The mod SHALL append copied bills in order only after checking the entire destination operation, including current recipe availability, supported choices and capacity.
Use the ordinary fifteen-row limit unless an explicitly supported limit extension governs all add and
paste paths. Pasting never unlocks research or supplies materials/workers. It creates independent new
orders, including the captured remaining finite count as additional work; it does not move the source
quota or duplicate a physical commitment. The preview identifies destination/group, new rows, quantities
and any destination worker policy that will override copied worker choices. Missing map-local references
require explicit replacement or cancellation. Failure or cancellation restores the complete prior list,
choices and work. Do not add only the first rows that fit.

#### Scenario: Insufficient capacity
- **WHEN** thirteen existing rows receive a four-row clipboard without a supported limit extension
- **THEN** paste explains the capacity problem and adds none of the rows.

#### Scenario: Different station or lost reference
- **WHEN** a copied recipe is unavailable at a destination member, or its selected storage cannot be resolved there
- **THEN** paste identifies the incompatible choice before changing the list
- **AND** no member receives a partial order.

#### Scenario: Copy a partly started order
- **WHEN** a source has five remaining iterations including two already started and its copied order is pasted
- **THEN** the preview identifies five additional independent iterations at the destination
- **AND** the source keeps its original five and both physical started iterations; none is cloned or transferred.

### Requirement: Paste choices preserves the destination recipe and existing work
The mod SHALL offer Paste choices when exactly one other bill is copied, applying compatible editable choices to the destination without changing its recipe, identity or physical work.
The preview identifies transferred, retained and overridden choices. Same-recipe copies preserve all
applicable configuration. Between different recipes, ingredient filters transfer only when their fixed
allowed ingredient sets and special restrictions are equivalent; otherwise retain destination ingredients.
Transfer repeat/count choices only where supported; preserve destination output style/name and any
inapplicable quality, taint, material or equipped-count choice. Transfer supported shared modifiers only
when meaningful for the destination. Bench worker policy has precedence. A finite count below already
committed work, an unavailable reference or unsupported active-work transition rejects the operation
until explicitly resolved. Apply accepted choices together, using the ordinary timing rules for ongoing work.

#### Scenario: Related recipes with different ingredients
- **WHEN** a meal bill's choices are pasted into an apparel bill
- **THEN** the apparel recipe, name/style and incompatible ingredient choices remain unchanged
- **AND** the preview shows which other choices can transfer without claiming that everything was copied.

#### Scenario: Existing physical work
- **WHEN** Paste choices changes future ingredients on a bill with an unfinished product
- **THEN** the unfinished product keeps its actual materials, creator and progress
- **AND** accepted changes affect subsequent work at the ordinary bill boundaries.

#### Scenario: Committed finite work exceeds copied remainder
- **WHEN** a linked row with three committed iterations receives a copied finite remainder of two
- **THEN** the entire paste is rejected with that reason and no member's choices change.

### Requirement: All operations use the whole linked production plan
The mod SHALL apply append, paste choices, rename, reorder, suspend, delivery and counting edits through a linked member to its one shared whole-list plan.
Copying a member captures that plan once. There is no independent per-row Paste Link, Break Link or
local suspension exception. Linking, leaving and dissolving use the whole-list feature's preview,
quota allocation, membership and recovery rules. Its common recipe catalog and physical eligibility
remain authoritative. Repeated recipes keep distinct row identities. Opening or hovering UI alone
does not copy choices between physical benches or change production state.

#### Scenario: Paste into one linked member
- **WHEN** the player appends two copied rows through one of three linked benches
- **THEN** the group gains two logical rows, not six independent orders
- **AND** all members show the same choices and the confirmed additional shared finite quantities.

#### Scenario: Suspend and change repeat mode in the overview
- **WHEN** the player uses the overview to suspend a linked row or select a supported repeat mode
- **THEN** the entire row changes subject to its existing committed-work rules
- **AND** the player need not unlink a row or open Details merely because it is linked.

### Requirement: Overview controls retain ordinary bill actions
The mod SHALL provide optional drag reordering, a delivery shortcut and the resume threshold alongside a target-count bill's current/target display when pause-when-satisfied is enabled.
Reordering changes priority without restarting work. Turning drag off retains native ordering controls.
The delivery shortcut switches between Drop on floor and Best storage, visibly identifying a selected
specific destination before a deliberate switch replaces it; merely drawing the shortcut changes nothing.
Native details remain available for choosing a specific destination. Retain native rename, delete,
suspend, information and editing actions; dragging a row must not consume a button click as a reorder
or suppress unrelated interface buttons. Counts and resume values must remain readable at supported UI scales.

#### Scenario: View and reorder a paused target
- **WHEN** a target of twenty with resume threshold ten is shown and its row is dragged below another
- **THEN** current stock, target and resume threshold remain visible and only order changes
- **AND** any real work retains its progress.

#### Scenario: Delivery shortcut replaces a specific target
- **WHEN** a bill currently sends products to a named stockpile and the player deliberately chooses Drop on floor
- **THEN** its delivery choice changes visibly to Drop on floor
- **AND** opening or hovering the shortcut alone does not discard the named destination.

#### Scenario: Disable drag and use ordinary buttons
- **WHEN** drag ordering is disabled or the player clicks Delete instead of a row's drag region
- **THEN** native ordering remains available and the clicked action executes once without an unintended reorder.

### Requirement: Detail navigation follows the bill's actual owner
The mod SHALL provide previous/next navigation among supported production bills in their owning list, without wrapping at either end or relying on an unrelated current selection.
Changes are retained before moving to the next bill. If a row or owner disappears, close or refresh
with an explanation rather than editing an unrelated row. Navigation uses current logical order and
skips rows that do not support production details. A linked list is traversed once.

#### Scenario: Selection changes while details are open
- **WHEN** the player selects another bench before using Next in an open bill's details
- **THEN** navigation stays with the original bill's list and does not alter the newly selected bench.

#### Scenario: End of list or deleted bill
- **WHEN** details are at the final supported row, or that row is removed while open
- **THEN** Next is unavailable at the end and a removed row cannot be used to edit another bill accidentally.

### Requirement: Convenience preferences share global and save controls
The mod SHALL expose Auto-open bills and storage tabs, Drag to reorder, and New bills drop on floor with built-in defaults on, on and off respectively.
Auto-open applies to single selected native workbenches, stockpiles and storage buildings that expose
the relevant tab. Keep an already open tab if that same tab is supported by the new selection; otherwise
open the appropriate Bills or Storage tab. Multi-selection and unsupported provider surfaces are unchanged.
Auto-open and drag changes apply to the next relevant action. Drop-on-floor defaults affect newly created
ordinary production bills only; pasted explicit delivery choices and existing bills are unchanged.

All actual preferences, including the counting capability's rows, use common loading, saving, resolution
and rendering in an ordered Production settings section. Each row shows Global default, Current save and
Use global. New saves inherit current globals; dormant overrides survive toggling inheritance. Inherited
save editors are inactive. At the main menu both save controls and their mutation actions are disabled.
Save changes never write another save or the global default. Entity choices stay in contextual bill/bench
controls. Translate all added controls, scope labels and explanations in the seven baseline languages.

#### Scenario: Preserve a deliberate inspect tab
- **WHEN** auto-open is enabled and the new single selection supports the currently open inspect tab
- **THEN** that tab remains open; otherwise the appropriate supported Bills or Storage tab opens.

#### Scenario: Change default delivery
- **WHEN** New bills drop on floor is enabled and the player creates a bill, then pastes a snapshot explicitly set to Best storage
- **THEN** the newly created bill drops products, the pasted bill retains Best storage and older bills are unchanged.

#### Scenario: Inheritance and main menu
- **WHEN** a save inherits Drag to reorder and its global default changes
- **THEN** the save uses that current global value while another save's explicit override remains unchanged
- **AND** returning to the main menu disables save editing and Use global without erasing dormant overrides.

### Requirement: Unsupported management support remains explicit and isolated
The mod SHALL withhold each unsafe management operation with a bounded startup warning and an explanation of the available fallback, while retaining ordinary unaffected bill controls.
Benign foreign edits alone do not disable support. With the original Better Workbench Management active,
withhold these overlapping conveniences rather than applying two competing implementations. Its controls
are not governed by our settings. Saved owned linked plans retain their own guarded recovery; no foreign
link/settings migration or source package impersonation occurs automatically. No new bottom tab is added.

#### Scenario: Rejected copy integration
- **WHEN** copy/paste integration cannot safely operate but native bill controls remain valid
- **THEN** startup reports the unavailable convenience and native unrelated editing remains usable
- **AND** the mod does not partially paste or erase saved plans.

#### Scenario: Original source remains active
- **WHEN** the original source and megamod are active together
- **THEN** the warning distinguishes source-owned controls from withheld megamod conveniences
- **AND** changing our retained preferences does not pretend to configure the source.
