# Perishable storage selection

Owner: **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`.
Target **RimWorld1.7 beta**. S114 replacement specification; implementation remains pending.

## ADDED Requirements

### Requirement: PER01 A contextual bulk action preserves ordinary storage editing
Supported storage configuration SHALL offer one Select perishable / Deselect perishable action
alongside its existing controls. Stockpiles, shelves, linked groups and admitted component-backed
storage use their effective editable storage policy. The action SHALL NOT appear in unrelated
recipe, outfit, food-policy or scenario filters merely because they use similar checkboxes.
It SHALL add no bottom tab, debug button, bottom-right toggle or mod-settings page.

The action's explanation SHALL state that it edits item-type checkboxes in the listed food,
raw-product and corpse categories, including outdoor deterioration; it is not a refrigeration-only
selector. Priority, search, scrolling, preset controls, ranges and special restrictions remain usable.

#### Scenario: Open a storage policy
- **WHEN** the player opens an admitted stockpile or shelf's storage configuration
- **THEN** the perishable action appears there without displacing ordinary storage controls

#### Scenario: Unrelated filter window
- **WHEN** the player opens a bill ingredient, outfit or food-policy filter after using storage
- **THEN** the owned perishable action does not leak into that unrelated window

#### Scenario: Read the scope
- **WHEN** the player inspects the action's explanation
- **THEN** it identifies food/raw-product/corpse scope and includes deterioration as well as rotting, without promising refrigeration requirements

### Requirement: PER02 Qualification preserves the source category and decay meaning
A type SHALL qualify when it belongs to one of the following category families, including any
descendant category, and is defined with ordinary rotting behavior or a positive deterioration rate:
foods, raw meat, raw plant food, plant matter, raw animal products, and corpses including humanlike,
animal, insect and mechanoid corpses. Being in a category alone is insufficient. Belonging to more
than one qualifying family SHALL NOT apply the edit repeatedly.

Classification SHALL use the current loaded type definitions, not the current map's inventory,
temperature, shelter, freshness, damage, rarity or whether a stack is already stored. Modded types
using the same supported categories and decay behavior qualify. Medicine, genes, apparel and other
types outside these families do not qualify solely because they rot or deteriorate. This scope
does not claim support for every custom spoilage system.

#### Scenario: Deteriorating survival food
- **WHEN** packaged survival meals retain their ordinary food category and positive deterioration rate
- **THEN** they qualify even though they do not rot and do not require refrigeration

#### Scenario: Refrigerated or absent food
- **WHEN** a qualifying food is currently frozen, protected on a shelf or has no stack anywhere on the map
- **THEN** its type still qualifies for the storage-policy action

#### Scenario: Category and decay are both needed
- **WHEN** a food type neither rots nor deteriorates, or a deteriorating medicine remains outside the listed families
- **THEN** this action leaves that type's checkbox unchanged

#### Scenario: Modded descendant category
- **WHEN** a modded type in a descendant food or corpse category uses the supported rot or deterioration behavior
- **THEN** it qualifies without a hardcoded item-name list and is edited once even if multiply categorized

### Requirement: PER03 Candidate scope follows intrinsic storage permissions
The action SHALL consider only qualifying types editable for the current storage policy under its
intrinsic parent/provider rules and forced-hidden restrictions. It SHALL NOT treat the policy's
current unchecked types as intrinsically forbidden. A linked group's candidate scope SHALL include
types usable by at least one member while each member retains its own physical restrictions.
Unsupported or unknown contexts SHALL leave ordinary editing available without an unsafe bulk edit.

Text search, collapsed categories and scrolling SHALL NOT narrow this bulk action. Its explanation
SHALL make that whole-policy scope clear. A context with no applicable qualifying types SHALL show
the action disabled with a no-applicable-types explanation, not an active Deselect action.

#### Scenario: Restricted storage
- **WHEN** a container intrinsically accepts only a subset of qualifying foods
- **THEN** the action considers only that subset and never enables meat or other types its intrinsic rules forbid

#### Scenario: Empty editable filter
- **WHEN** all applicable qualifying types are currently unchecked in the editable policy
- **THEN** they remain candidates for Select perishable

#### Scenario: Search and collapsed categories
- **WHEN** the player searches for one food or collapses other categories before using the action
- **THEN** the edit still covers the complete applicable qualifying set, including rows outside the current view

#### Scenario: Nothing applies
- **WHEN** the storage has no applicable qualifying types
- **THEN** the action is disabled with an explanation and changes no filter state

### Requirement: PER04 The action adds or removes qualifying checkboxes without replacing policy
When any applicable qualifying type is unchecked, the action SHALL read Select perishable and
enable all applicable qualifying type checkboxes. When every applicable qualifying type is checked
and the set is nonempty, it SHALL read Deselect perishable and disable those checkboxes. The label
SHALL reflect current checkbox state after manual edits, presets, copy operations or group changes.
The operation is a set-to-allowed/set-to-disallowed edit, not inversion of each individual checkbox.

Unrelated type choices, priority, quality/condition/other ranges, special fresh/rotten and provider
restrictions, refill quantities and other storage policy SHALL remain unchanged. Any inseparable
item variants SHALL follow the same ordinary checkbox behavior without overriding intrinsic
restrictions. An enabled item type does not imply every instance passes the remaining restrictions;
the action SHALL explain this distinction. No permanent perishable mode is created.

#### Scenario: Mixed initial state
- **WHEN** some applicable perishable types are enabled and others are disabled and the player clicks Select perishable
- **THEN** all applicable types become enabled while unrelated type choices remain as before

#### Scenario: Deselect all applicable types
- **WHEN** every applicable perishable type is enabled and the player clicks Deselect perishable
- **THEN** those types become disabled without clearing other item choices

#### Scenario: Retain special restrictions and refill policy
- **WHEN** the player uses Select perishable while rotten items are disallowed and a refill threshold is configured
- **THEN** type checkboxes change but the rotten restriction, threshold, priority and other ranges remain unchanged

#### Scenario: Manual changes remain authoritative
- **WHEN** the player edits a qualifying checkbox after a bulk action or applies a storage preset
- **THEN** the button reflects the resulting current state and does not automatically reapply a remembered perishable selection

### Requirement: PER05 Linked policy and native consequences remain intact
Using the action through any member of an admitted linked group SHALL edit the one shared policy
once. Other members SHALL show the result immediately, and subsequent ordinary storage decisions
SHALL use that policy together with each member's physical restrictions. Existing items SHALL NOT
be moved, destroyed, repaired, refrigerated or made fresh by this action.

Hauling/refill demand and bill-output validation SHALL receive the final policy change. Ordinary
unlinked bills retain native handling. A linked-workbench row whose chosen output storage becomes
unsuitable SHALL retain that unresolved choice and stop new starts until the player resolves it,
following the existing linked-workbench recovery rules rather than silently switching destination.
Saving, loading, copying or unlinking SHALL preserve the resulting native
filter choices under their existing rules. The feature SHALL NOT add separate selection settings
or a save schema; these are storage-specific gameplay choices, not global/save configuration.

#### Scenario: Edit a mixed linked group
- **WHEN** the player uses Select perishable through a shelf linked to a ground stockpile
- **THEN** both show the changed shared policy, but only physically compatible destinations accept each type

#### Scenario: Native hauling and bill feedback
- **WHEN** the player deselects types needed by an ordinary unlinked bill's designated output storage
- **THEN** native output-storage handling reflects the final filter and ordinary hauling/refill decisions use the changed policy

#### Scenario: Linked workbench output becomes unsuitable
- **WHEN** Deselect perishable makes a linked-workbench row's chosen output storage unsuitable for its output
- **THEN** every linked member retains the unresolved destination and stops new starts for that row until it is resolved, without silently switching destination or resetting the shared order

#### Scenario: Existing stacks stay physical
- **WHEN** stored food becomes disallowed by the action
- **THEN** the food remains in place until ordinary gameplay moves or consumes it, with unchanged spoilage and deterioration rules

#### Scenario: Persist the result
- **WHEN** the player saves/reloads, copies the filter or unlinks a storage member after using the action
- **THEN** the resulting item choices follow native/shared policy persistence without reapplying a stored perishable mode

### Requirement: PER06 Failed or stale edits preserve the prior policy
The action SHALL apply only to its intended current storage authority and valid membership. If the
target, group membership or edit permissions change before activation, it SHALL cancel and ask the
player to reopen rather than silently edit another storage. The latest unrelated edits SHALL be
preserved. A successful bulk edit SHALL expose its complete final policy; failure SHALL restore
the prior policy and supported derived effects, verify restoration and report the failed action.
Contexts whose mutation behavior cannot safely satisfy this contract SHALL be unavailable.

#### Scenario: Selection changes before activation
- **WHEN** a pending action belongs to one storage but selection or group membership has changed
- **THEN** no storage is edited and the player receives a stale-context explanation

#### Scenario: Failure partway through application
- **WHEN** a supported bulk-edit operation fails before completion
- **THEN** prior checkbox and unrelated policy state is restored and the action reports failure instead of leaving a partial selection

### Requirement: PER07 Compatibility checks preserve ordinary storage fallback
The feature SHALL activate only when the required storage-context, classification, layout and
mutation semantics are compatible. Compatible foreign patches are allowed. A failed requirement,
unknown shape or installation failure SHALL leave the affected owned action inactive and record
one bounded startup warning per skipped enabled patch, naming the target, reason, missing behavior
and fallback. Ordinary storage editing remains available. Later incompatible changes SHALL disable
only the affected owned integration before unsafe use.

Active `Danoweb.SelectPerishable` SHALL suppress the owned overlap with a startup warning, leaving
the original provider untouched. No hard DLC or optional storage-provider dependency is added.
Owned labels, explanations and controls SHALL remain usable in English, German, Spanish, French,
Simplified Chinese, Russian and Japanese, including supported UI scales and long labels.

#### Scenario: Changed storage integration
- **WHEN** a required integration no longer has the semantics needed for safe bulk edits
- **THEN** a bounded warning explains the skipped action and ordinary checkboxes remain available

#### Scenario: Original provider present
- **WHEN** the original Select Perishable package is active
- **THEN** the owned overlapping action is disabled with an explanation and no provider patches are removed

#### Scenario: Readable contextual controls
- **WHEN** the player uses a supported language or UI scale with long labels
- **THEN** the action, explanation and ordinary storage controls remain readable and independently usable
