# Specialist command automation

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), RimWorld1.7 beta.
Origin: replacement, **S051 / P1-45**, with explicit UI and timing deviations. Draft only.

## ADDED Requirements

### Requirement: AC01 Activate only the selected work commands
The feature SHALL consider the following commands for player-controlled humanlike workers who
already possess them, at the start of the listed work. It SHALL neither grant abilities/roles nor
change work permissions, assignments or priorities. Autonomous visitors, prisoners, enemies,
animals and non-player-controlled workers are outside this automation.

| Command | Qualifying work | Built-in configured default |
| --- | --- | --- |
| Farming Command | Sowing, ordinary/designated harvesting and ordinary/designated plant cutting | On |
| Mining Command | Ordinary mining of mineable targets | On |
| Production Command | Bills whose recipe uses Crafting | On |
| Research Command | Ordinary research; admitted Research Reinvented work described below | On |
| Prepare Feast | Cooking-skill bills, including butchering recipes that use Cooking, with the supplying provider available | On |
| Void Suggestion | Anomaly study interactions, including studyable objects as well as captured entities, with the supplying provider available | On |
| Majordomo Work Command | All valid bill recipes, the listed plant work, mining, research and Anomaly study interactions | Off |

Research Reinvented coverage consists of bench analysis of items, in-place analysis, terrain
analysis, research interrogation, learning through comms and its ordinary research job. These
are research triggers only when the corresponding provider integration is admitted. Majordomo's
study trigger does not require the separate Void Specialist mod. Construction, deconstruction,
deep drilling, scanning, hauling, slaughter, combat and unrelated abilities receive no additional
trigger from this feature. Benefit to bystanders does not itself trigger a command.

#### Scenario: Source trigger categories stay distinct
- **WHEN** a worker with Production Command begins a Crafting bill, while another only constructs a building
- **THEN** the first worker is considered for automatic activation and the builder is not

#### Scenario: Cooking uses recipe skill
- **WHEN** a worker with Prepare Feast starts a Cooking-skill butcher bill
- **THEN** the command is considered; slaughtering a live animal alone does not trigger it

#### Scenario: Plant work includes deliberate cutting
- **WHEN** an eligible plant specialist begins sowing, harvesting or designated plant cutting
- **THEN** Farming Command is considered for each qualifying job

#### Scenario: Majordomo covers other bill skills
- **WHEN** Majordomo automation is enabled and its owner begins a valid bill using a skill other than Crafting or Cooking
- **THEN** Work Command is considered without inventing a Production or Prepare Feast trigger

#### Scenario: Research providers preserve their six work categories
- **WHEN** an eligible research specialist begins any of the six admitted Research Reinvented activities
- **THEN** Research Command is considered without bypassing the provider's project, opportunity, prison or comms restrictions

#### Scenario: Majordomo study is independent of Void Specialist
- **WHEN** a Majordomo with automation enabled begins supported Anomaly study without Void Specialist installed
- **THEN** Work Command can activate, while Void Suggestion is unavailable

#### Scenario: Automation does not confer a specialist role
- **WHEN** an ordinary worker or an autonomous visitor starts matching work
- **THEN** no ability is granted and the visitor is not controlled by this feature

### Requirement: AC02 Check at actual work start without a recast loop
The feature SHALL make at most one automatic attempt per supported command per job, immediately
before the first actual work or research interaction, after required travel, ingredient preparation
and reservations. It SHALL use current command ownership, policy, job validity and cast eligibility
at that moment. A failed or skipped opportunity SHALL not wait for cooldown, repeat within the
same job, or force job churn. A later genuinely new qualifying job provides a new opportunity.

#### Scenario: Arrive before casting
- **WHEN** a researcher walks to a distant bench with a ready enabled command
- **THEN** the pawn travels first and attempts the command at the bench before beginning research

#### Scenario: Preparation fails
- **WHEN** a bill job cannot obtain its ingredients or a work target disappears before work begins
- **THEN** this feature spends no command merely because that job was assigned

#### Scenario: Cooldown ends during travel
- **WHEN** a command is unavailable at assignment but ready when the worker reaches valid work
- **THEN** it can activate at that work-start opportunity

#### Scenario: Cooldown ends during work
- **WHEN** a command is unavailable at work start and later becomes ready during the same job
- **THEN** work continues without a new automatic attempt; a later new qualifying job can try

#### Scenario: Repeated steps are not new jobs
- **WHEN** one plant job visits additional plants or one research job repeats an interaction
- **THEN** those steps do not repeat the job's command attempts

#### Scenario: Policy changes before work
- **WHEN** the player switches the command off or the pawn loses its ability during travel
- **THEN** the pending work-start opportunity does not cast the stale command

### Requirement: AC03 Preserve ordinary casting and job continuity
The feature SHALL cast on the worker through the ordinary ability behavior, honoring current
eligibility, warmup, charges, cooldown groups, effects, recipients, durations and costs. It SHALL
not remove restrictions, directly grant the resulting effect, extend an aura, or alter ability
balance. Manual casting SHALL remain available under ordinary rules, including when automation
is off. Automated attempts SHALL not overlap an existing cast or consume one ability twice.
When both a specific work command and Work Command qualify, the specific command SHALL be
considered first; each subsequent command is rechecked after the preceding cast finishes.

#### Scenario: Native effect and cooldown remain visible
- **WHEN** an eligible worker automatically completes a command
- **THEN** the ordinary cast/effect and cooldown appear, the worker proceeds with the same valid work, and nearby recipients receive only the normal ability effects

#### Scenario: Costs remain real
- **WHEN** a supported external command has a drawback or charge cost
- **THEN** automatic activation pays that same cost; enabling it does not waive the drawback

#### Scenario: Shared cooldown prevents a second command
- **WHEN** two commands qualify but the first activation makes the second unavailable
- **THEN** only the first casts and the worker continues without waiting for the second

#### Scenario: Cast refusal does not block work
- **WHEN** the native ability refuses activation at a valid work-start opportunity
- **THEN** work continues without an automation retry loop or a fabricated success/effect

#### Scenario: Player interruption wins
- **WHEN** the player drafts or redirects the worker during automatic warmup
- **THEN** normal interruption rules apply, no cancelled order is restored over the player's new command, and no second effect is manufactured

#### Scenario: Manual activation remains native
- **WHEN** the player left-clicks a supported ability while automatic use is off
- **THEN** the ordinary cast interaction, eligibility and cooldown behavior remain available

### Requirement: AC04 Place shared live controls at bottom right
The feature SHALL offer one compact bottom-right control for current-game specialist automation,
opening a small list of the available command types. Each entry SHALL show its effective On/Off
state and whether it follows its configured value; selecting On or Off creates a current-game
operational choice, and **Use configured value** clears that choice. New/missing choices follow
configuration. The control SHALL visibly distinguish all-off, some-on and all-on states across
available commands. It SHALL operate across the current game's maps and loaded-game world view,
not merely the currently selected pawn. No bottom tab or debug-only entry is added.

The source's right-click-on-ability toggle SHALL not be retained. Ability controls can show a
noninteractive state indicator/tooltip explaining automation scope, but their normal input and
other providers' input SHALL be preserved. Text, states and explanations SHALL use all seven
baseline languages. Unsupported commands SHALL not be offered as functional switches; retained
unavailable choices remain identifiable in configuration without being applied.

#### Scenario: Shared toggle scope is explicit
- **WHEN** the player selects Mining Command Off in the bottom-right list
- **THEN** upcoming mining jobs across that save stop automatic activation, with no global-default change and no hidden pawn-only setting

#### Scenario: Use configured value restores following
- **WHEN** the player clears a live override and later changes the effective configured value
- **THEN** the live state follows that value without requiring a new game

#### Scenario: Cooldown does not lock the switch
- **WHEN** every current owner of a command is on cooldown
- **THEN** its available live switch remains usable for future work opportunities

#### Scenario: Right-click does not change automation
- **WHEN** the player right-clicks a pawn ability, the map or an unrelated command
- **THEN** this feature does not change automation or intercept the ordinary input

#### Scenario: State is readable without a new tab
- **WHEN** some available command types are on and others off
- **THEN** the bottom-right control shows a mixed state and its compact list explains the individual values and scope

### Requirement: AC05 Configure defaults with common inheritance
The feature SHALL expose the seven configured defaults in a stable **Work → Specialist commands**
settings section, ordered Farming, Mining, Production, Research, Prepare Feast, Void Suggestion,
Work Command. Each SHALL use common **Global default**, **Use global**, and **Current save**
controls, with the AC01 built-in defaults. New/missing save values SHALL inherit current globals;
Use global SHALL retain dormant overrides. At the main menu, all save editing and callbacks SHALL
be inactive with an explanation. Effective changes SHALL affect the next not-yet-started cast
opportunity for commands following configuration; they SHALL not erase explicit live choices,
interrupt a cast already started, remove effects or alter cooldowns.

#### Scenario: Defaults protect the costly optional command
- **WHEN** a new game uses untouched configuration with all supported providers available
- **THEN** the first six command types follow On and Work Command follows Off

#### Scenario: Global and save values stay separate
- **WHEN** one save overrides Farming Off and another inherits global Farming On
- **THEN** they resolve separately, and restoring Use global retains the first save's dormant override

#### Scenario: Live choice survives a default edit
- **WHEN** a current-game operational choice is Off and its configured default changes to On
- **THEN** it stays Off until Use configured value is selected; the UI shows that distinction

#### Scenario: Main menu cannot edit a save
- **WHEN** settings are opened with no loaded game
- **THEN** global defaults are editable, save controls and their callbacks are disabled, and no previous save or live state changes

### Requirement: AC06 Persist choices without replaying casts
The feature SHALL persist current-game operational choices separately from configuration and
ordinary ability cooldown/effect state. Save switching SHALL not leak any choice or pending
attempt. A resumed job SHALL retain whether its work-start opportunities have already been
handled; loading alone SHALL not cast or duplicate an interrupted/successful activation.
Unavailable provider choices SHALL remain dormant for later return without granting replacement
abilities. No automatic import of upstream global settings
or source-specific jobs SHALL be claimed.

#### Scenario: Reload during work
- **WHEN** a player saves and reloads after a job's work-start opportunity
- **THEN** the resumed work does not cast again merely because it was reconstructed, and native cooldown/effect state remains authoritative

#### Scenario: Reload before work
- **WHEN** a still-travelling job is saved and loaded
- **THEN** it retains only its unconsumed opportunity and rechecks current conditions when actual work begins

#### Scenario: Provider disappears and returns
- **WHEN** a provider is unavailable in one load and later restored
- **THEN** its saved switch choice remains dormant while unavailable and becomes usable only after admission, without a retroactive cast

### Requirement: AC07 Optional content and failures remain isolated
The feature SHALL require Ideology for specialist automation, while the rest of megamod remains
usable without it. Optional integrations SHALL require the actual active supplying package,
content and supported behavior, not a matching label or a discovered type alone. Absent optional
content SHALL be normally unavailable; incompatible enabled integrations SHALL be skipped with
bounded startup warnings identifying the affected behavior and manual/native fallback.
Research Reinvented's six activities SHALL be supported together: if any cannot safely support
automation, all six SHALL leave automation inactive, with ordinary research/provider play intact.

#### Scenario: Ideology or an optional provider is absent
- **WHEN** the game lacks Ideology, or only lacks Cooking Specialist
- **THEN** respectively the feature or only Prepare Feast is unavailable without broken jobs or invented content

#### Scenario: Same name from another provider
- **WHEN** unrelated content uses a familiar command name without an admitted provider contract
- **THEN** it is not automatically cast solely because the name matches

#### Scenario: Research integration is incomplete
- **WHEN** one of the six Research Reinvented activities cannot safely support automation
- **THEN** automation is unavailable for all six, a bounded warning explains this, and vanilla command automation remains independent

### Requirement: AC08 Admit compatible behavior and avoid duplicate ownership
The feature SHALL allow compatible changes by other mods. At startup, an enabled integration that
cannot safely support the intended work or casting behavior SHALL be unavailable with bounded log
warnings explaining the affected behavior, reason and fallback. Independently supported behavior
SHALL remain available. Later supported compatibility changes SHALL not leave automation acting
on obsolete assumptions. Ordinary work and manual casting SHALL remain the fallback, and controls
SHALL not claim an unavailable integration is functioning.

When original Auto-Cast Specialist Commands is active, this owned feature SHALL be inactive as a
whole, with one clear explanation and retained dormant choices, leaving the original's behavior untouched. This
feature SHALL not claim repairs to the original provider. Owned bill/work/UI features SHALL
compose directly with this behavior, preserving one automatic opportunity and existing player
orders without duplicating automatic casts.

#### Scenario: Compatible third-party edit
- **WHEN** another mod changes unrelated job code while the required work-start and cast behavior remain valid
- **THEN** the owned integration can remain active

#### Scenario: Required work meaning is lost
- **WHEN** a changed job no longer has an identifiable safe work-start point
- **THEN** that integration remains inactive with a bounded startup warning, while ordinary work continues

#### Scenario: Original mod is also active
- **WHEN** both the original source and megamod are enabled
- **THEN** megamod suppresses its specialist automation and controls, preserves dormant choices, and leaves the original's behavior untouched

#### Scenario: Owned bill features cooperate
- **WHEN** an owned bill-selection or linked-workbench feature leads to a qualifying job
- **THEN** it retains the same bill, recipe and ingredient choices and receives only this feature's single work-start opportunity
