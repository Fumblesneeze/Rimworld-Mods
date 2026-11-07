# Item-type weapon classification

Owner **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
S090; proposed observable behavior only.

## ADDED Requirements

### Requirement: Offer a bounded and understandable item catalogue
The feature SHALL list loaded loose-item types with defined melee strikes, including improvised
resource/consumable weapons and ranged weapons with melee strikes. It SHALL exclude worn apparel,
non-item objects and types without those strikes. It SHALL retain the source catalogue's exclusions
for identifiers containing `turret`, `artillery` or `weapon_grenade` without regard to case, and these
named exclusions: `Gun_Needle`, `Gun_NeedleLauncher`, `Gun_ChargeBlasterHeavy`, `Gun_HellsphereCannon`,
`Gun_InfernoCannon`, `Gun_MiniShotgun`, `Gun_MiniFlameblaster`, `Gun_Spiner`, `Gun_Scattergun`,
`Gun_Slugthrower`, `Gun_ThumpCannon`, `Gun_ToxicNeedle`. Each listed row SHALL show an identifiable
item name, available icon, source and a plain-language reason for inclusion, sorted by item name.

#### Scenario: Resource, melee weapon and firearm
- **WHEN** loaded wood, a knife and a firearm each define melee strikes
- **THEN** they are catalogue candidates unless explicitly excluded
- **AND** a firearm without melee strikes is not included merely because it shoots

#### Scenario: Apparel and excluded items
- **WHEN** armor provides melee strikes or an item matches a catalogue exclusion
- **THEN** it is not offered as an editable weapon-classification row
- **AND** this feature does not change its ordinary wear, equip or weapon status

#### Scenario: Similar names and missing content
- **WHEN** two providers supply similarly named candidate items, or a previously configured provider is absent
- **THEN** source/identifier information distinguishes the loaded rows
- **AND** missing saved entries are retained as inactive data rather than applied to a different item

### Requirement: Configure global defaults and save overrides in mod settings
The feature SHALL place its controls in **Mod settings → Equipment → Weapon classification**.
Each eligible item type SHALL have a built-in global default of **Consider weapon: on**, plus the
common **Use global**, **Current save** and **Global default** controls. New saves SHALL inherit
current globals. Per-save editors SHALL be inactive while inheriting; all save mutation controls
SHALL be inactive at the main menu. Changes SHALL affect subsequent supported classification and
equip decisions without restarting; other saves and explicitly overridden rows SHALL stay isolated.

#### Scenario: Inherited default and explicit override
- **WHEN** a global wood setting is off, one save inherits it and another explicitly sets it on
- **THEN** wood is excluded in the inheriting save and permitted under ordinary rules in the overridden save
- **AND** changing the global later does not overwrite that explicit override

#### Scenario: Toggle inheritance
- **WHEN** the player checks Use global and later unchecks it
- **THEN** the save follows the current global while checked, with its editor inactive
- **AND** unchecking restores its dormant override, or seeds its first override from the current effective value

#### Scenario: Main-menu configuration
- **WHEN** mod settings are opened without a current game
- **THEN** globals remain editable and per-save edits, inheritance changes, bulk changes and resets are disabled with a reason
- **AND** no previous or implicit save is mutated

#### Scenario: Reload and switch saves
- **WHEN** settings are saved and the player reloads or switches between differently configured saves
- **THEN** each save restores its own inheritance/overrides and current classification
- **AND** no previous save's result or pending equip decision is reused

#### Scenario: A settings batch cannot be applied
- **WHEN** an individual, bulk, global, inheritance or reset edit fails before its current-game classification and item lists can be updated coherently
- **THEN** the entire edit retains the previous stored values, inheritance flags, effective policy and item-list behavior
- **AND** controls show those previous values with an explicit failure explanation, rather than showing the attempted edit as applied
- **AND** saving or reloading does not resurrect any rejected part of that edit

### Requirement: Search and bulk changes have explicit scope
The settings catalogue SHALL support case-insensitive substring search by item name or identifier,
**Show ranged** initially off and **Show only changed** initially off. Ranged filtering SHALL describe
the item's available attacks independently of whether it is currently excluded. A visible action
scope SHALL distinguish global from current-save operations. Enable/Disable listed SHALL affect
exactly the filtered rows captured when the action is invoked, including scrolled-off matching rows.
Current-save bulk overrides SHALL explicitly show that they turn off Use global for those rows.

Show only changed SHALL compare the chosen scope with its baseline: global versus built-in on,
current-save effective value versus its current global. Reset global defaults SHALL reset all loaded
candidate globals to on, not merely search matches; Reset current save SHALL return all its loaded
candidate rows to Use global while retaining dormant overrides. Labels SHALL distinguish these full
resets from listed-row actions. Search/filter state SHALL not itself change gameplay configuration.

#### Scenario: Find a disabled ranged item
- **WHEN** a firearm with melee strikes is disabled and the player searches for it
- **THEN** it remains hidden until Show ranged is enabled, then remains findable despite being disabled
- **AND** clearing the search does not change any classification value

#### Scenario: Apply to the displayed filter
- **WHEN** the player disables listed global rows under a search and changed filter
- **THEN** exactly the matching set at click time is changed once, even if those edits change filter membership
- **AND** filtered-out rows and save overrides are untouched

#### Scenario: Explicit save bulk override
- **WHEN** the player invokes the current-save Enable or Disable listed override action
- **THEN** the shown matching rows receive that explicit save value and Use global becomes unchecked for them
- **AND** the action's label/count makes that inheritance change clear before activation

#### Scenario: Reset scope is distinct
- **WHEN** the player resets globals or the current save while a search is active
- **THEN** the clearly labeled full-scope reset applies to all loaded candidate rows in that scope
- **AND** globals become on, or save rows inherit, without deleting absent-provider entries or changing another scope

### Requirement: Exclusion governs new weapon use without removing items
An eligible type with effective Consider weapon off SHALL not count as a weapon, melee weapon or
ranged weapon in supported classification consumers and SHALL not be newly equipped through supported
actions. Ordinary Equip SHALL be absent for that type; other relevant denial surfaces SHALL explain
the classification setting. On SHALL restore ordinary classification and eligibility, preserving
independent restrictions. The feature SHALL not add attacks, equipment slots or capabilities, alter
attack values, destroy/drop already held items, or disable ordinary hauling, crafting, consumption,
building or fuel use. This type policy SHALL apply to all supported consumers, not a pawn-specific rule.

#### Scenario: Disable an improvised weapon
- **WHEN** the player disables wood as a weapon and then opens an ordinary equip menu for it
- **THEN** the Equip action is absent and supported weapon searches no longer return it as a weapon
- **AND** the same wood remains available for normal hauling, construction and fuel use

#### Scenario: Re-enable with ordinary restrictions
- **WHEN** a type is enabled again but a pawn cannot reach or manipulate it, is forbidden by ordinary combat rules, or fails an independent provider restriction
- **THEN** the ordinary restriction still prevents equipping it
- **AND** enabling classification does not turn a non-equippable object into usable equipment

#### Scenario: Previously queued equip or held item
- **WHEN** a type is disabled after an equip job is queued but before its item transfer, or while an item of that type is already held
- **THEN** the queued action does not newly equip it or drop the previous equipment
- **AND** an already held item is neither forcibly removed nor destroyed; the setting is not a disarm or attack-cancellation command

#### Scenario: Refresh existing map items
- **WHEN** the player changes classification for items already on a map, then searches, hauls, moves or removes those items
- **THEN** supported weapon views and actions reflect the new policy without stale entries, duplicates or lost ordinary items
- **AND** enabling the type again makes existing eligible items discoverable without respawning them

### Requirement: Integrate supported equipment providers directly
Supported Simple Sidearms pickup and inventory-switch actions SHALL respect the same effective type
exclusion before transferring or dropping equipment. Its separate tool/weapon preferences and
ordinary restrictions SHALL remain meaningful for permitted types. Absent Simple Sidearms SHALL
need no adapter. Unsupported provider paths SHALL be explicitly reported as unsupported without
claiming that menu filtering proves direct-switch coverage. Owned equipment/search/hauling features
SHALL consume the same current policy rather than maintain independent copies.

#### Scenario: Sidearm menu and inventory switch
- **WHEN** an admitted Simple Sidearms integration encounters a disabled type through pickup or a direct inventory switch
- **THEN** it does not equip the disabled item or disturb the current weapon as a side effect of refusing
- **AND** permitted types retain its ordinary capacity, tool preference and use restrictions

#### Scenario: Retain a remembered carried sidearm
- **WHEN** a carried item is protected from ordinary inventory unloading by an admitted Simple Sidearms preference and its type becomes disabled
- **THEN** classification alone does not remove that existing unload protection or force the item out of inventory
- **AND** explicit drop or forget actions still work, without granting permission to equip the disabled item

#### Scenario: Provider absent or changed
- **WHEN** Simple Sidearms is absent or its required direct-switch semantics are unsupported
- **THEN** no adapter is needed, or that adapter stays inactive with a bounded explanation of the missing coverage
- **AND** independent native classification/equip behavior remains available without a broad compatibility claim

### Requirement: Admit coherent policy and preserve configuration intent
The feature SHALL apply only when its documented classification, action and data-refresh prerequisites
hold. Compatible surrounding changes SHALL remain allowed. Missing or ambiguous prerequisites SHALL
withhold the affected behavior and produce bounded startup warnings naming skipped patches, reasons,
unavailable behavior and fallback. Original Is This A Weapon? SHALL suppress overlapping owned
behavior whenever that original provider is loaded. Inactive support SHALL be distinguished from the player's
stored values, which SHALL be retained. No original settings SHALL be imported automatically.

#### Scenario: Safe surrounding change or original provider
- **WHEN** a compatible mod preserves the needed semantics, or the original classifier is active
- **THEN** the compatible case remains eligible, or the owned overlap yields with an explanation
- **AND** there is one effective classifier, with no erased configuration or foreign-mod removal

#### Scenario: Required refresh or action contract fails
- **WHEN** a required native classification/equip/refresh contract cannot be admitted
- **THEN** the affected group stays inactive with ordinary behavior and a clear startup explanation
- **AND** settings show that stored values are not currently applied instead of promising a partially working exclusion

#### Scenario: Readable settings without extra navigation
- **WHEN** the player opens this section in any of the seven baseline languages
- **THEN** search, row identity, reasons, scope, inheritance and bulk/reset actions remain readable and operable
- **AND** the feature adds no bottom tab, debug override, bottom-right toggle or per-pawn control
