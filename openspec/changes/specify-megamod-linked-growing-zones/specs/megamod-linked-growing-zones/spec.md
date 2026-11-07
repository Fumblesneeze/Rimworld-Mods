# Linked growing zones

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`). Target: 1.7 beta.
Origin: original. Specified only; no implementation or live acceptance.

## ADDED Requirements

### Requirement: Form a persistent group of growing zones

The player SHALL be able to link two or more growing zones on the same map through contextual zone
controls. The first selected eligible zone SHALL supply the crop, Allow sowing and Allow cutting
choices, and the command SHALL identify that source before activation. Only selected zones SHALL
join; selecting a member of an existing group first SHALL add the others to that group. Linking
SHALL leave zone boundaries, names, existing plants and unrelated objects unchanged.

#### Scenario: Link separated fields
- **WHEN** the player selects a rice field first and a potato field second and links them
- **THEN** both share the rice field's crop and sowing/cutting choices without changing existing plants.
- **AND** the link remains after the selection is cleared.

### Requirement: Crop and both growing controls remain synchronized

Changing the crop, Allow sowing or Allow cutting through any linked zone SHALL update all members
as one action. A multi-selection containing several members of the same group SHALL apply a toggle
once, not reverse it repeatedly. A newly created unlinked zone SHALL retain normal game defaults;
linking SHALL not change the defaults of future independent zones.

#### Scenario: Suspend sowing across linked fields
- **WHEN** Allow sowing is switched off through one linked zone
- **THEN** every member displays sowing disabled and growers stop selecting new ordinary sowing work there.
- **AND** changing the shared crop while sowing is disabled leaves sowing disabled everywhere.

#### Scenario: Toggle several selected members
- **WHEN** two zones in the same group are selected and Allow cutting is toggled once
- **THEN** the group's cutting permission changes exactly once and both zones display the same result.

### Requirement: Crop choices respect the participating growers

The crop chooser SHALL offer plants that the participating zones can select under native grower
and research rules. It SHALL preserve skill information, information cards and relevant warnings.
If the proposed source crop cannot be selected for a prospective member, linking SHALL explain the
incompatibility and leave membership and choices unchanged; it SHALL not silently substitute another
crop or link only part of the requested set. The player can select a mutually valid crop and retry.

#### Scenario: Incompatible pollution requirements
- **WHEN** the source crop cannot be selected in another zone because of its pollution conditions
- **THEN** linking is refused with an explanation and both zones retain their prior state.

#### Scenario: No sufficiently skilled grower
- **WHEN** the player selects an otherwise available crop whose required Plants skill nobody can meet
- **THEN** all members receive the crop and the normal lack-of-grower warning remains available.
- **AND** linking does not give an unqualified pawn permission to sow it.

### Requirement: Linking changes orders rather than plant growth or work rules

Each cell SHALL retain its normal fertility, temperature, light, pollution, roof and other growing
conditions. Pawn skill, work availability, reservations, access and protected-plant rules SHALL
remain effective. Changing a crop SHALL take effect through normal future work, not instant plant
replacement. Allow cutting SHALL retain its native distinction: disabling it prevents the growing
work it normally prohibits but does not itself prohibit harvesting the selected mature crop.
Allow sowing SHALL not be repurposed as a general harvest switch.

#### Scenario: Cutting disabled with mature selected crops
- **WHEN** linked rice fields have cutting disabled and their selected rice is mature and harvestable
- **THEN** eligible growers can still harvest that rice under normal rules.
- **AND** disabling cutting does not authorize clearing protected competing plants for sowing.

#### Scenario: One field becomes unsuitable
- **WHEN** a linked outdoor field freezes while another member remains warm enough to grow
- **THEN** the shared choices remain visible but actual work and growth follow each field's conditions.

### Requirement: Move and unlink members without losing their last choices

Moving selected zones into another group SHALL leave unselected former members with their previous
choices. Unlinked zones SHALL retain their last crop and sowing/cutting choices independently.
A group reduced to one member by removal SHALL leave that survivor independent with those choices.
Later edits SHALL not propagate to former members.

#### Scenario: Give one field a different crop
- **WHEN** the player unlinks one field and changes its crop
- **THEN** the remaining linked fields retain their own shared crop and work permissions.

### Requirement: Group controls remain contextual and identifiable

Linked growing zones SHALL show a group name and membership, support naming the group and selecting
its members, and visually identify the participating zones. These controls and crop/work choices
SHALL remain zone-specific gameplay controls. They SHALL not create a new bottom tab or appear in
the global/per-save mod settings interface. Individual zone names and colors SHALL remain intact.

#### Scenario: Inspect a field's group
- **WHEN** the player selects a linked growing zone and chooses Select linked
- **THEN** its available linked zones on that map are selected and their group identity is visible.

### Requirement: Save and zone lifecycle preserve valid decisions

Membership and shared choices SHALL survive save/reload. Expanding or shrinking a member SHALL
preserve its shared choices; deleting a zone SHALL remove only that member. Groups SHALL not cross
map boundaries. If a saved crop's content is missing, the affected group SHALL report that it needs
a new crop choice and suspend new sowing instead of silently sowing a replacement; existing plants
and surviving zones SHALL not be deleted. Once a valid crop is selected, the retained sowing choice
SHALL govern future work again.

#### Scenario: Resize then reload
- **WHEN** the player expands a linked field, saves and reloads
- **THEN** all surviving members retain the group crop and work permissions over their current cells.

#### Scenario: A crop provider is removed
- **WHEN** a linked group's saved crop is no longer available
- **THEN** the group indicates the missing choice and performs no new sowing until a valid crop is chosen.

### Requirement: Failed integration preserves independent growing zones

If synchronization cannot operate safely, it SHALL remain inactive with a bounded startup warning
describing unavailable linking and the fallback. Ordinary independent growing zones SHALL remain
usable. Compatible unrelated patches SHALL not alone disable synchronization. Existing saves or
third-party groups SHALL not be silently converted as part of startup.

#### Scenario: Growing command integration changes
- **WHEN** the required command behavior is incompatible at startup
- **THEN** the warning identifies unavailable linking and normal independent crop selection remains usable.
