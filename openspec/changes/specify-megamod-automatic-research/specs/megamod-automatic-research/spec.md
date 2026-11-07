# Automatic research

Owner **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`; target **1.7 beta**.
Origin replacement **S120 Research Whatever**. This is a behavioral draft, not implemented gameplay.
Proposed corrections and defaults are distinguished from source facts in research/design.

## ADDED Requirements

### Requirement: Research-bench autonomy is a contextual persistent choice

Each supported player-owned research bench SHALL expose an initially enabled automatic-research
toggle on its contextual controls. It SHALL govern that bench's ability to start an unselected
ordinary project, persist with the bench, and not prevent work on an already selected project.
Non-player benches SHALL NOT choose player research. This entity choice SHALL NOT be a mod setting.

#### Scenario: Newly available player bench
- **WHEN** the player selects a new supported research bench
- **THEN** its automatic-research toggle is visible and on, with a description of selection and exhaustion

#### Scenario: Off does not cancel deliberate research
- **WHEN** the player turns a bench's toggle off while a project is selected
- **THEN** the project and progress remain and eligible researchers can continue it at that bench
- **AND** that bench does not automatically select the next project when the selection becomes empty

#### Scenario: Ownership and persistence
- **WHEN** a bench is saved, reloaded, moved or temporarily changes ownership
- **THEN** its stored choice follows that bench, and only player ownership permits automatic selection
- **AND** another bench or save does not inherit this entity's choice

### Requirement: Automatic selection occurs through eligible research work

With no ordinary project selected, an enabled bench SHALL allow an otherwise eligible researcher
to start a research job by choosing an available project for that actual bench. Existing research
choices SHALL take precedence, including choices made by another admitted provider. Candidate
inspection, an unusable bench or an ineligible pawn SHALL NOT change the colony's selected project.
The native shared project, work priorities, access, reservations and ideology restrictions SHALL remain.
Successful automatic starts SHALL retain the ordinary research-start sound and tutorial cues.

#### Scenario: Idle researcher begins useful work
- **WHEN** no project is selected and a researcher can start ordinary work at an enabled suitable bench
- **THEN** an eligible project is selected and the pawn starts the normal research workflow
- **AND** one quiet notification names the researcher and project and identifies the bench

#### Scenario: A temporary inability to work
- **WHEN** a bench is unpowered, inaccessible or reserved, or the pawn cannot perform the research job
- **THEN** merely considering that bench does not select a project or switch its toggle off

#### Scenario: Existing or intervening player choice
- **WHEN** a project is already selected or a player/provider selects one before automatic work commits
- **THEN** the automatic choice does not replace it, erase progress or alter an external queue

#### Scenario: Multiple benches and maps
- **WHEN** researchers on different maps become eligible while the shared project is empty
- **THEN** the first admitted choice fills the normal shared project slot
- **AND** subsequent researchers use that choice where their own benches permit it, without duplicate starts

### Requirement: Ordinary choices preserve progression and minimize remaining work

Automatic ordinary research SHALL choose only unfinished bench research currently available under
the game's and admitted providers' prerequisites, visibility/difficulty and actual bench requirements.
Required facilities SHALL apply even when a project does not demand a particular bench type.
Excluded projects SHALL not be automatically selected. Eligible ordinary-priority projects SHALL
precede low-priority projects regardless of their costs. Within that priority class, choose the
smallest current remaining work as displayed for the colony, accounting for existing progress and
technology penalties. Only genuinely equal remaining-work choices in that class SHALL tie randomly.
Unsupported or invalid work estimates SHALL not become automatic choices.

#### Scenario: Progress matters more than original price
- **WHEN** one eligible project has total work of 1,000 and progress of 900, and another has total work of 300 and no progress
- **THEN** the first project is chosen because 100 work remains rather than 300

#### Scenario: Equal total costs are not an equal tie
- **WHEN** two same-priority projects each cost 1,000 but have 900 and 0 progress
- **THEN** only the project with 100 remaining work qualifies for the cheapest choice

#### Scenario: Low priority cannot win through a tie or huge cost
- **WHEN** an ordinary-priority project and a low-priority project are both eligible
- **THEN** the ordinary-priority project wins even if its total or remaining work is much larger
- **AND** an excluded project is never used to break that tie

#### Scenario: Real prerequisites and facilities
- **WHEN** research needs a missing prerequisite, techprint, analysis, inspection, special pawn, powered bench or active facility
- **THEN** automatic selection does not bypass that requirement, including a facility-only requirement

#### Scenario: True tie
- **WHEN** several admitted projects share both the best priority and the same smallest remaining work
- **THEN** one is randomly selected from only that tied group

### Requirement: Exhausted benches stop with a recoverable explanation

If an otherwise usable enabled bench has no eligible automatic ordinary project, it SHALL switch
its own toggle off and issue one neutral notification identifying that bench and the current lack
of eligible research. This SHALL NOT mean all future research is permanently exhausted. The player
SHALL be able to re-enable it after prerequisites or content change. Exhaustion SHALL not cancel
another bench's autonomy, an active project or manual research choices.

#### Scenario: No eligible project at this bench
- **WHEN** an eligible work attempt finds no automatic project suitable for that bench
- **THEN** that bench turns automatic selection off and explains why once, without repeated idle notifications

#### Scenario: New opportunity after exhaustion
- **WHEN** prerequisites change and the player re-enables that bench
- **THEN** its next eligible work attempt considers current research and can start a newly available project

### Requirement: Positive Anomaly study selects an appropriate unchosen project

Automatic Anomaly selection SHALL respect existing projects when an ordinary study action produces
positive knowledge with Anomaly active. This includes the knowledge category and its immediate overflow
category. If both are empty, it SHALL choose an available project in the primary category first,
or the immediate overflow category if no primary project is eligible. Within the selected category,
the same exclusion, priority and remaining-work rules SHALL apply. It SHALL preserve native knowledge
awards, overflow, local study progress, discoveries, cooldowns and work restrictions, without replay.
Selection SHALL NOT overwrite any occupied project slot that the candidate would also affect.
Unsupported category relationships SHALL leave automatic selection inactive for that action.
Projects mixing ordinary research work and Anomaly knowledge without supported progress and
completion rules SHALL be excluded from both automatic selection paths, even with no existing selection.
Successful automatic study choices SHALL produce one quiet researcher/project notification with a
usable subject target when available, and retain ordinary research-start sound and tutorial cues.

#### Scenario: Primary category before cheaper overflow
- **WHEN** positive study has no selected destination and both categories contain eligible projects
- **THEN** it starts the cheapest eligible primary-category project even if overflow offers cheaper research
- **AND** it reports that actual choice once without changing or duplicating this study action's rewards

#### Scenario: Existing overflow selection
- **WHEN** the primary category is empty but an overflow project is already selected
- **THEN** study continues through the native route without replacing it with a new primary project

#### Scenario: Overflow and knowledge preservation
- **WHEN** no eligible primary project exists but an eligible immediate-overflow project does
- **THEN** that project can be selected and this same study action awards its normal knowledge once
- **AND** completing it still follows native excess-knowledge routing rather than inventing an automatic chain

#### Scenario: Nonpositive or unrelated study
- **WHEN** Anomaly is absent or a study action produces no positive Anomaly knowledge
- **THEN** this feature neither selects an Anomaly project nor pauses study because of research exhaustion

#### Scenario: Candidate affects another occupied selection
- **WHEN** an unusual project would replace an occupied ordinary or other category selection
- **THEN** it is not selected automatically, and the existing selection remains intact

#### Scenario: Unsupported mixed-cost project with empty selections
- **WHEN** both selections are empty and a project demands ordinary work and Anomaly knowledge
  without a supported combined progress and completion rule
- **THEN** neither automatic bench work nor automatic study selects that project
- **AND** supported alternative projects remain eligible rather than attempting an undefined completion

### Requirement: Exhausted study pauses only after local value is complete and resuming is possible

Automatic exhaustion SHALL pause future study only after a positive study action has no eligible
primary or immediate-overflow project or existing relevant selection, and the subject's finite study and journal discoveries are
complete and any special research-dependent outcome is ready. An unnatural corpse SHALL retain
study until research permits its destruction. Uncertain extra outcomes SHALL prevent automatic
pausing until their provider is understood. The current study action SHALL still finish normally.
Pausing SHALL use a visible, usable contextual control with a functioning resume path: captured
entities use Maintain only, and ordinary objects use their Study toggle. A subject without an
admitted reversible control SHALL remain unchanged. Newly available research SHALL not silently
override a player's paused choice.

#### Scenario: Local discovery remains
- **WHEN** no category project is eligible but a journal entry, finite study or special corpse outcome is unfinished
- **THEN** study remains enabled so the ordinary workflow can finish that local benefit

#### Scenario: Captured entity exhaustion and resume
- **WHEN** a captured entity has no remaining admitted research or local discovery benefit
- **THEN** it switches from Study to Maintain only and reports that transition once
- **AND** choosing Study later actually restores study without a hidden disabled switch

#### Scenario: Ordinary object's study toggle
- **WHEN** an exhausted subject has an available ordinary Study toggle
- **THEN** future study is turned off through that control and one explanation identifies the subject
- **AND** the player can re-enable it when new research becomes available

#### Scenario: Unknown or irreversible study control
- **WHEN** a subject has an unsupported extra study outcome or no usable resume control
- **THEN** exhaustion does not silently disable that subject or change its unrelated prisoner/containment policies

#### Scenario: Current action and later availability
- **WHEN** exhaustion is detected during a study action
- **THEN** that action's knowledge, local discoveries and normal consequences still occur once
- **AND** later project availability alone does not switch a paused subject back on

### Requirement: Completion presentation changes only the intended notification

Research completion presentation SHALL offer **Native dialog** (built-in default), **Letter**,
**Quiet message**, and **Silent**. For completions that normally request the research dialog, the
selected mode SHALL retain that dialog, replace it with one positive letter containing the project's
description, replace it with one quiet completion message, or suppress that presentation respectively.
This SHALL apply to manual and automatic projects, ordinary and Anomaly research. It SHALL not create
a notification for a completion whose caller requests none, report failed completion as success,
suppress independent story/discovery letters, or change progress, unlocks and completion effects.

#### Scenario: Four presentations
- **WHEN** a research project completes with a normal completion dialog request
- **THEN** Native dialog retains the native presentation, Letter gives one positive completion letter,
  Quiet message gives one quiet completion message, and Silent gives none of those presentations

#### Scenario: Independent discoveries and starts
- **WHEN** Silent is selected and research also produces an independent story or unlock letter
- **THEN** that independent letter remains, as do separate automatic-start and exhaustion notices

#### Scenario: Suppressed or unsuccessful completion
- **WHEN** a caller requests no completion dialog or the completion does not succeed
- **THEN** this feature does not invent a replacement success notice

### Requirement: Completion preferences use common inherited settings and clear UI

Completion presentation SHALL use the common Global default, Current save and Use global row in
an ordered Research settings section. Missing values SHALL use Native dialog; new/missing save
entries SHALL inherit the current global default. Changes SHALL affect the next applicable
completion. Inheriting SHALL disable the save editor, retain a dormant override, and resume that
override when inheritance is turned off; a first override SHALL start from the effective global
value. Main-menu save editing and mutation SHALL be disabled. All owned labels, explanations and
notices SHALL support English, German, Spanish, French, Simplified Chinese, Russian and Japanese.
The feature SHALL add no bottom tab or global operational toggle and SHALL not hide settings on a bench menu.

#### Scenario: Inheritance and independent saves
- **WHEN** a global mode changes while one save inherits and another overrides it
- **THEN** only the inherited save's next completion follows the new default; neither save edits the other

#### Scenario: Override round trip
- **WHEN** the player enables Use global and later disables it
- **THEN** the retained save override returns, or the first override is seeded from the current global mode
- **AND** reloading or changing saves preserves the correct scopes

#### Scenario: Main menu and localization
- **WHEN** settings are opened without an active game in any supported language
- **THEN** global presentation remains editable and save controls and their callbacks are disabled with an explanation

### Requirement: Optional policies preserve external content and select one implementation

Admitted repeatable Stacking and Productivity research from Researchable Stat Upgrades SHALL be
low priority for automatic selection. The source's eleven named Vanilla Gravship Expanded projects
SHALL remain excluded from ordinary automatic bench selection while their provider is active.
These policies SHALL not alter manual availability, costs, prerequisites, other extensions or
unrelated projects. Missing providers SHALL require no content changes. Original Research Whatever
present SHALL select the original implementation and disable this owned overlap without removing
the provider. Other research providers SHALL retain their intentional job restrictions and queues.
Unproven alternate job integration SHALL disable affected owned automatic bench selection rather
than force vanilla jobs back on. Settings or save migration SHALL not be implied.

#### Scenario: Optional repeatable research
- **WHEN** the admitted repeatable provider and projects are present
- **THEN** eligible non-low-priority research is selected before those repeatable projects
- **AND** without the provider no similarly named unrelated project is reclassified

#### Scenario: Gravtech remains with its progression system
- **WHEN** the admitted Vanilla Gravship Expanded policy is active
- **THEN** its listed excluded projects are not selected by automatic bench work
- **AND** their native/provider availability, costs and manual control remain unchanged

#### Scenario: Other content additions
- **WHEN** another provider adds facilities, policy data or research projects
- **THEN** those entries remain present, and repeating owned initialization does not duplicate additions

#### Scenario: Original or alternate worker provider
- **WHEN** the original mod is active, or an alternate research worker cannot be safely integrated
- **THEN** the corresponding owned overlap is inactive and the source/provider's existing workflow remains
- **AND** unrelated megamod features remain available without a false compatibility claim

### Requirement: Unsupported targets fail locally and preserve research state

Each enabled patch SHALL be admitted only when its documented minimal behavior requirements hold.
Compatible third-party changes SHALL not be rejected merely for existing. Unknown or failed targets
SHALL leave only the affected patch or declared coherent group inactive, preserve existing research,
progress and entity choices, and issue one bounded startup warning per skipped enabled patch stating
the target, reason, lost behavior and fallback. Supported later changes SHALL be revalidated without
duplicate hooks, notifications or partial actions. This feature SHALL not create a separate persistent
research or study cache, replay missed research, or convert old source settings/save data silently.

#### Scenario: Harmless change and compatible patches
- **WHEN** other patches leave the needed research or presentation behavior intact
- **THEN** the relevant owned integration remains eligible for admission

#### Scenario: Missing or changed semantics
- **WHEN** a target no longer provides the required eligibility, knowledge award or presentation behavior
- **THEN** the affected integration remains inactive with a bounded explanation and its documented native/provider fallback

#### Scenario: Repeated initialization and later change
- **WHEN** initialization repeats or supported later patch changes invalidate a previously admitted group
- **THEN** no duplicate choices or rewards occur, only owned partial installation is removed,
  and already selected projects and earned progress remain intact
