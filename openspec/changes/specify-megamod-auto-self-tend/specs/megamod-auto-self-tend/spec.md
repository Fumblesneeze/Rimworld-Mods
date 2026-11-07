# Automatic self-tend permission

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **RimWorld 1.7 beta**.
S042 / Auto Self-Tend. Observable contract drafted for later implementation; no behavior accepted.

## ADDED Requirements

### Requirement: Automatic permission follows a configurable Medical threshold
The mod SHALL enable ordinary self-tend permission for eligible colonists at or above Medical level
**6** by default, with an integer threshold configurable from **0 to 20**. Below the threshold it
SHALL disable permission by default; an enable-only option SHALL leave below-threshold permission
unchanged. These rules apply on the assessment events defined below, not continuously to manual choices.

#### Scenario: First assessment at the default boundary
- **WHEN** eligible colonists at Medical 5 and 6 receive their first assessment with default policy
- **THEN** permission is off for level 5 and on for level 6, visible in their native Health controls

#### Scenario: Enable-only policy preserves lower-skilled choices
- **WHEN** disable-below is off and two below-threshold colonists with permission respectively on and off are assessed
- **THEN** each retains their existing permission while an at-threshold colonist is enabled

#### Scenario: Threshold extremes and actual skill
- **WHEN** the threshold is 0 or 20 and an eligible colonist's current Medical level meets it
- **THEN** that colonist qualifies, using their current effective skill including aptitude rather than treatment quality

### Requirement: Manual choices survive stable policy and skill classification
The mod SHALL preserve a player's individual permission choice until the pawn first becomes assessed,
crosses the threshold, or is assessed under a different effective policy. Same-side skill changes,
unchanged settings and equivalent inheritance choices SHALL NOT reset the choice. A changed policy
SHALL re-evaluate the pawn even if the pawn remains on the same side of the threshold.

#### Scenario: Player turns off a qualified pawn
- **WHEN** the player turns permission off for an assessed level-8 colonist under threshold 6 and the skill later rises to 9
- **THEN** permission remains off through ordinary updates

#### Scenario: Player enables a lower-skilled pawn
- **WHEN** the player enables an assessed level-4 colonist under threshold 6 with disable-below on
- **THEN** ordinary updates leave it on while skill and effective policy remain on the same side and unchanged

#### Scenario: Genuine crossings reapply policy
- **WHEN** an assessed colonist crosses from level 5 to 6 under default policy, then later drops below 6
- **THEN** permission turns on for the upward crossing and off for the downward crossing

#### Scenario: Policy edit can replace a manual exception
- **WHEN** the player manually disabled a level-10 colonist under threshold 6 and commits threshold 7
- **THEN** permission is enabled again, with settings text explaining that policy changes reapply defaults

#### Scenario: Equivalent policy edits preserve the choice
- **WHEN** the player opens and closes unchanged settings or switches to an override with the same effective values
- **THEN** no manual permission changes merely because the settings surface or inheritance changed

### Requirement: Automation covers native eligible colonists consistently
The mod SHALL assess living native colonists on loaded maps, including contained colonists, and in
player caravans when they have the ordinary self-tend permission control, a usable Medical skill and
ability to do Doctor work. Secure slaves counted as native colonists SHALL follow the same rule.
Babies, including toddlers still in the Baby stage, animals, mechs, subhumans, ordinary prisoners,
non-player guests and pawns unable to do Doctor work SHALL receive no automatic permission writes.

#### Scenario: Multiple maps and a caravan
- **WHEN** eligible colonists are present on two maps and in a player caravan
- **THEN** the same threshold policy applies to each without requiring the player to visit each map

#### Scenario: Held colonist and secure slave
- **WHEN** an otherwise eligible colonist is held in a map container or is a secure slave with the native permission control
- **THEN** they receive the same permission assessment without being released or granted any new medical ability

#### Scenario: Ineligible pawn does not receive hidden permission
- **WHEN** a baby, incapable colonist or other excluded pawn is encountered, including at threshold 0
- **THEN** their permission is not changed and other eligible colonists continue to be assessed

#### Scenario: Doctor work priority is zero
- **WHEN** a colonist capable of Doctor work meets the threshold but Doctor work is currently unassigned
- **THEN** permission can be enabled, their work priorities remain unchanged and treatment is not guaranteed

### Requirement: Assessments have bounded and predictable timing
The mod SHALL assess eligible pawns before play begins after new-game initialization or loading,
within **600 advancing game ticks** after recruitment, a relevant skill change or return to scope,
and immediately upon committing an effective policy change for the active game. The ordinary bound
is **10 seconds at normal simulation speed**, not elapsed real time while paused. Merely rendering
settings SHALL NOT apply uncommitted changes.

#### Scenario: Recruitment and skill growth
- **WHEN** an eligible pawn joins or crosses the threshold while the game advances
- **THEN** the appropriate permission is visible within 600 game ticks

#### Scenario: Paused policy edit
- **WHEN** the player commits a changed effective policy while paused
- **THEN** current eligible colonists reflect the policy before returning to play without advancing the simulation

#### Scenario: No gameplay advancement
- **WHEN** the player leaves a paused game alone without committing policy changes
- **THEN** wall-clock time alone does not generate recurring permission changes

### Requirement: Map transfers and temporary absence preserve assessment meaning
The mod SHALL retain each pawn's prior assessment across ordinary map/caravan movement and temporary
absence. Returning under unchanged skill classification and policy SHALL preserve manual choices;
returning after an effective policy change SHALL apply the policy current for that pawn. Pawns in
world travel outside loaded maps and player caravans SHALL be assessed on return rather than being
granted remote treatment or having unrelated world state modified.

#### Scenario: Transfer with a manual exception
- **WHEN** an assessed colonist with a manual permission choice moves from a map into a caravan under unchanged policy and classification
- **THEN** movement alone does not reset their choice

#### Scenario: Policy changes while the pawn is absent
- **WHEN** a known pawn leaves covered populations, policy changes and is applied to other colonists, then the pawn returns
- **THEN** the returning pawn is assessed using the new policy within the ordinary bound

#### Scenario: Temporarily ineligible pawn returns
- **WHEN** an assessed pawn becomes ineligible and later eligible again
- **THEN** no permission is written while ineligible, and the returning assessment compares current policy and skill classification with that pawn's last successful assessment

### Requirement: Permission does not bypass ordinary medical gameplay
The mod SHALL change only self-tend permission. Native medical eligibility, patient condition,
Doctor work assignments, medicine restrictions and consumption, reservations, job selection, work,
quality and consequences SHALL remain in force. Existing medical features SHALL compose through
these ordinary rules without duplicated treatment or an invented priority override.

#### Scenario: Allowed self-treatment is performed normally
- **WHEN** an injured eligible colonist is enabled and their native work and medical conditions allow self-treatment
- **THEN** ordinary treatment can proceed with normal work, supplies and observable health results

#### Scenario: Permission is not instant healing
- **WHEN** permission turns on for a pawn unable to perform treatment in their present condition
- **THEN** no injury is directly healed, no medicine is created and no work restriction is removed

#### Scenario: Medical care and priorities remain the player's choices
- **WHEN** the player changes the threshold for a colonist with a restricted medicine policy and existing work priorities
- **THEN** only self-tend permission can change; care policy and priorities remain intact

### Requirement: Policy uses common global defaults and per-save overrides
The mod SHALL place minimum Medical skill and disable-below policy in **Mod settings → Health →
Automatic self-tending**, in that order, using common Global default, Use global and Current save
controls. New or missing save options SHALL inherit live global defaults. Each option SHALL support
an independent save override, retain a dormant override while inheriting, and use the current global
value when first creating an override. These are policy settings; individual pawn permissions are not.

#### Scenario: Global edit and overridden save
- **WHEN** a global threshold changes while the loaded save inherits it, then another save with an explicit threshold is loaded
- **THEN** the first save uses the new global threshold and the second retains its override without changing either save's other settings

#### Scenario: Use global preserves a dormant override
- **WHEN** the player enables Use global on an overridden option and later disables it
- **THEN** the saved override returns, while its editor was inactive during inheritance

#### Scenario: Main menu has no current-save target
- **WHEN** the settings are opened from the main menu
- **THEN** global controls work, all Current save and Use global controls and callbacks are inactive with an explanation, and no previous save is modified

#### Scenario: Invalid threshold input
- **WHEN** a threshold entry is invalid or outside 0–20
- **THEN** the active policy never uses an out-of-range or fractional level; invalid editing is rejected and invalid stored values are bounded with a diagnostic, while a missing value uses 6

### Requirement: Individual permissions retain their native contextual interface
The mod SHALL retain the ordinary Health-tab permission control and native explanations. It SHALL
explain policy events, enable-only behavior and first-assessment effects in mod settings, in all
seven baseline languages. It SHALL introduce no bottom tab, new pawn-settings row, debug command or
bottom-right behavior toggle for this feature.

#### Scenario: Contextual edit remains reachable
- **WHEN** the player selects an eligible pawn and opens Health, including with owned compact health presentation
- **THEN** they can change native self-tend permission and stable assessments preserve that change

#### Scenario: Policy description accurately limits manual preservation
- **WHEN** the player reads the automatic self-tending section
- **THEN** it clearly states that threshold crossings, effective policy edits and first assessment can change individual permissions

### Requirement: Save lifecycle preserves native permission and valid history
The mod SHALL save its valid assessment history with the current game while retaining ordinary native
permission persistence. Loading unchanged valid history SHALL preserve manual choices. First use or
a missing pawn record SHALL apply first-assessment rules; invalid or unrecognized existing history
SHALL be reported and affected automation suspended rather than guessed. No upstream settings or
component migration SHALL occur automatically.

#### Scenario: Save and reload a manual exception
- **WHEN** the player saves and reloads a valid assessed game with unchanged effective policy and skill classification
- **THEN** the pawn's manual permission choice remains unchanged

#### Scenario: Globals changed between sessions
- **WHEN** a saved game inherits a global policy that changed since its last successful assessments
- **THEN** loading applies the new effective policy to eligible pawns before play

#### Scenario: First installation into an existing game
- **WHEN** owned assessment history does not yet exist
- **THEN** eligible colonists receive first assessment, which can replace existing native permission choices as described in settings, without importing or modifying original-provider configuration

#### Scenario: Invalid history and provider suspension
- **WHEN** history is unrecognized or automation is unavailable because another provider owns the behavior
- **THEN** affected native permissions are left unchanged, the reason is reported and valid dormant history is retained for later reassessment

### Requirement: Compatibility failure leaves native medical controls usable
The mod SHALL enable automation only when its consumed native contracts are supported. An active
original Auto Self-Tend provider SHALL suppress owned overlapping automation with a bounded startup
warning and an explanatory settings status. Compatible external changes SHALL NOT be rejected merely
because another mod modified a method. Unsupported behavior SHALL leave native permissions untouched;
every enabled patch skipped by any later implementation SHALL identify its failed target requirement
and lost behavior in startup logs without recurring spam.

#### Scenario: Original provider is active
- **WHEN** the game starts with the original Auto Self-Tend provider active
- **THEN** owned automatic permission writes remain inactive, startup logs and settings explain ownership, and the source provider and native controls remain untouched

#### Scenario: Required native contract is unavailable
- **WHEN** a required permission or assessment contract cannot be safely used
- **THEN** this automation remains inactive with one bounded diagnostic explaining the reason and native-manual fallback, without disabling unrelated megamod features

#### Scenario: Compatible external permission edit
- **WHEN** another supported feature changes an assessed pawn's native permission while classification and effective policy remain stable
- **THEN** ordinary assessments leave that change in place, just as for a manual edit
