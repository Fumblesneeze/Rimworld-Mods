# Linked growing buildings

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`). Target: **1.7 beta**.
Origin: S076 Linkable Hydroponics replacement with explicit membership/eligibility corrections.
Draft only; no runtime implementation or acceptance.

## ADDED Requirements

### Requirement: LB01 Link selected growing buildings with an identified source

The player SHALL be able to link at least two placed, player-controlled, supported growing buildings
on one map using contextual controls. The command SHALL identify the first selected eligible building
and its crop as the source before activation. Linking SHALL immediately apply that crop to the
resulting group. If that first member already belongs to a group, its existing group SHALL receive
the selected incoming members. Only selected incoming members SHALL move from other groups;
unselected members of those groups SHALL retain their previous choices. Existing plants, power
switches and unrelated selections SHALL remain unchanged by linking.

#### Scenario: Link basins with different crops
- **WHEN** the player selects a rice basin first, a potato basin second, and activates Link
- **THEN** the displayed rice source determines the crop of both linked basins immediately.
- **AND** their existing plants remain until normal work changes them.

#### Scenario: Transfer a selected member
- **WHEN** a member of group A is selected first and one member of group B is linked to it
- **THEN** group A receives that selected member and applies A's crop to it.
- **AND** unselected members of B keep their previous crop and do not join A.

#### Scenario: Reject an unavailable source
- **WHEN** the source has no valid crop choice or a requested member is no longer eligible
- **THEN** linking explains the problem and leaves all membership and crop choices unchanged.

### Requirement: LB02 Crop choices cover all affected members

The crop chooser SHALL consider every member of each selected building group, including unselected
members, together with any independently selected growers affected by the same crop command.
It SHALL offer only crops selectable for all those growers under their current native and supported
provider rules. Linking and crop changes SHALL recheck the requested result when activated and apply
as one action or leave the prior choices and memberships unchanged. Different grower types SHALL
be allowed when they share a valid source crop; matching building labels alone SHALL not grant access.
Native crop ordering, skill information, information cards and applicable warnings SHALL remain usable.

#### Scenario: An unselected member limits a crop
- **WHEN** one selected member permits a crop that another linked member cannot select
- **THEN** the group chooser does not offer that crop.

#### Scenario: A crop becomes unavailable while the menu is open
- **WHEN** the player chooses a formerly valid crop after eligibility or membership has changed
- **THEN** the operation rechecks the current affected growers and either applies to all or reports
  why it cannot, without a partly changed group.

#### Scenario: Compatible optional growers
- **WHEN** supported planter boxes and hydroponics can all select the identified source crop
- **THEN** they can form one building group while retaining their individual growing conditions.

#### Scenario: Insufficient pawn skill
- **WHEN** a selectable crop requires more skill than any available grower has
- **THEN** the crop can still be chosen with the normal warning and skill information.
- **AND** linking does not enable an unqualified pawn to sow it.

### Requirement: LB03 Keep group edits coherent with ordinary growing orders

A crop edit through any linked building SHALL update its whole group once, including when several
members are selected. Multiple selected groups SHALL receive the chosen crop without merging their
membership. A command that also selects growing zones SHALL honor each group's own crop rules and
leave zone sowing/cutting permissions unchanged. Building groups SHALL contain only buildings;
zone groups SHALL retain their separate crop/sowing/cutting behavior. Linking SHALL not introduce
building sowing/cutting switches or silently attach a building to an overlapping zone.

#### Scenario: Change two selected members of one group
- **WHEN** the player makes one crop choice with two linked basins selected
- **THEN** all members receive that choice once, with no repeated warnings or toggled-back result.

#### Scenario: Edit crops across independent group kinds
- **WHEN** a supported crop command includes a linked field and a linked basin
- **THEN** the mutually valid crop applies to the affected field and basin groups, which stay separate.
- **AND** field sowing/cutting choices retain their previous values.

#### Scenario: Try to create a mixed membership
- **WHEN** the player attempts to link a field and a basin as one group
- **THEN** the linking controls explain that these are separate group kinds and change neither group.

### Requirement: LB04 Linking preserves physical growing and work rules

Linking SHALL change future crop orders without changing plant age, fertility, yield, growth speed,
power consumption, light, temperature, pollution, research, building cost or pawn work requirements.
Normal reservations, access, skill, protected-plant rules and native effects of losing power or
removing a building SHALL remain effective. A changed crop SHALL not instantly replace plants or
invent harvests. Existing jobs SHALL follow normal job validation rather than being globally cancelled.
Changing environmental conditions SHALL not silently split the group or substitute its crop.

#### Scenario: One basin loses power
- **WHEN** one linked basin loses power while another remains powered
- **THEN** both retain the shared crop choice while each experiences its normal power-dependent work
  and plant effects.

#### Scenario: Change crops during work
- **WHEN** a grower is already working when the group crop changes
- **THEN** existing plants and that job follow normal game validation and later work uses the new order.

### Requirement: LB05 Unlink and disband without resetting crops

Unlink selected SHALL remove only selected building members. Disband group SHALL identify the
affected group and member count before activation and remove all its links, including unselected
members. Both actions SHALL retain each building's last crop. A group reduced to one valid member
SHALL dissolve, leaving that survivor independent. Later edits SHALL not affect former members.

#### Scenario: Give one basin another crop
- **WHEN** the player unlinks one basin and changes its crop
- **THEN** the other group members retain their shared crop and no later edit reaches the unlinked basin.

#### Scenario: Disband the selected group
- **WHEN** the player activates the identified Disband group command
- **THEN** all members of that group become independent with their last crop retained.

### Requirement: LB06 Identify groups through contextual controls

Linked buildings SHALL show an editable group name, current membership count and shared crop or
recovery state. Select linked SHALL add available members of the chosen group to the current
selection on that map without removing unrelated selections. Selecting a member SHALL identify
other members visually without altering building appearance or normal selection behavior; text
SHALL also identify membership without relying solely on color. Crop/group controls SHALL stay on
the selected growers, create no bottom tab, and remain entity gameplay state rather than settings.
All new player-facing labels, tooltips and explanations SHALL be available in the seven baseline
languages. Technical log identifiers SHALL remain invariant.

#### Scenario: Find a group's buildings
- **WHEN** the player selects one linked basin and activates Select linked
- **THEN** available members are added to the selection with their shared identity visible.
- **AND** unrelated selected objects remain selected.

#### Scenario: Inspect in another language
- **WHEN** a supported baseline language is selected
- **THEN** group actions, membership and recovery explanations use that language and remain accessible
  from the native contextual surface.

### Requirement: LB07 Save and lifecycle retain only valid membership

Group identity, membership and crop choices SHALL survive save/reload on their own map. Destroying,
despawning, uninstalling, transferring or losing player control of a building SHALL remove it from its former group before
later group edits can reach it; a newly spawned or reinstalled building SHALL begin independent.
Loading a saved placed member SHALL retain its valid link. Stale saved references SHALL not attach
replacement buildings, other maps or another game's objects. If surviving saved members disagree
after independent changes, the feature SHALL preserve their individual choices, report that relinking
is needed and withhold propagation until the player explicitly relinks them.

#### Scenario: Reload a valid group
- **WHEN** the player saves and reloads a named group of placed basins
- **THEN** its surviving valid members retain the group and crop, without a fresh linking action.

#### Scenario: Reinstall a former member
- **WHEN** a supported building is uninstalled and reinstalled, even before the next game tick
- **THEN** it is independent and former group edits no longer reach it.
- **AND** normal native consequences for its plants still apply.

#### Scenario: A saved group contains conflicting independent choices
- **WHEN** saved members have different crops after a period of independent use
- **THEN** loading preserves those choices, displays the need to relink and does not choose a winner.

### Requirement: LB08 Missing crops remain recoverable

When a saved group's chosen crop content is missing, affected buildings SHALL identify the missing
choice and accept a new mutually valid crop through their contextual controls. They SHALL not sow
a substitute or start new sowing until that choice is resolved. This pause SHALL not delete surviving
plants or suppress otherwise normal harvesting. Recovery SHALL not crash the crop chooser, use an
unrelated building's crop or change other groups. Unlinking SHALL retain the unresolved choice and
recovery control on affected buildings until the player chooses a valid crop.

#### Scenario: Remove a crop provider
- **WHEN** a saved linked crop is absent on load
- **THEN** the group visibly needs a crop choice, starts no new sowing and preserves surviving plants.
- **AND** choosing an available crop restores ordinary future sowing subject to each building's rules.

#### Scenario: Inspect a building whose crop is missing
- **WHEN** the player selects and inspects an affected native basin or supported optional planter
  before opening the crop chooser
- **THEN** the inspect pane and contextual controls remain usable and explain the missing choice,
  without repeated errors or a substituted crop.

#### Scenario: Unlink while a crop is missing
- **WHEN** the player unlinks a building whose saved crop is missing
- **THEN** it remains able to select a valid crop without silently acquiring a default or losing recovery.

### Requirement: LB09 One provider owns building links and unsafe integration degrades visibly

If the original Linkable Hydroponics provider is active, megamod building linking SHALL remain
inactive with a bounded startup warning naming that authority; it SHALL not import, rewrite or
operate the original provider's groups. Owned zone linking SHALL not broadcast through source-owned
building groups. Unsupported optional growers SHALL retain their provider's independent controls.
When required integration cannot be verified, the affected owned linking SHALL stay inactive with
a bounded startup warning per skipped enabled patch identifying the unavailable behavior and fallback.
Compatible unrelated modifications SHALL not alone disable linking. Valid independent crop choices
SHALL remain usable, with no silent overwrite of saved groups or another provider's behavior.

#### Scenario: Original provider present
- **WHEN** the original linking mod is active at startup
- **THEN** megamod supplies no competing building links and logs the authority decision.
- **AND** it does not change the original groups or treat their unselected members as owned zones.

#### Scenario: Required crop integration has changed
- **WHEN** the current game's crop workflow no longer supports safe group changes
- **THEN** affected owned linking remains inactive with a startup warning and usable independent crop
  controls instead of partly synchronized groups.

#### Scenario: An unrelated modification is present
- **WHEN** another mod adds a compatible unrelated gizmo without breaking the required crop behavior
- **THEN** its presence alone does not prevent owned linking or remove that gizmo.
