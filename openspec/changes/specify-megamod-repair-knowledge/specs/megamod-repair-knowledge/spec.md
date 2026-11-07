# Megamod repair knowledge

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`).
Target: **RimWorld 1.7 beta**. Origin: S054, I Cant Fix That.
This is an observable draft; no behavior is implemented or accepted in game.

## ADDED Requirements

### Requirement: RK01 Colony knowledge limits existing repair work

The mod SHALL restrict otherwise available repair work by the colony's knowledge of the target.
This SHALL cover ordinary building repair, breakdown repair and admitted maintenance, equipment
mending and automatic repair. It SHALL apply to colonists, colony prisoners, slaves and colony
mechs only where their normal work rules allow that action, and to admitted player-owned automatic
repairers. It MUST NOT create a repair action or change work priority, skill, speed, cost, success
chance, reachability, reservations, ownership or other ordinary eligibility.

#### Scenario: Unknown building technology
- **WHEN** an otherwise eligible colony worker seeks to repair a damaged building whose required research is unfinished
- **THEN** that repair is refused and other eligible work remains available.

#### Scenario: Knowledge becomes available
- **WHEN** the required research completes and the worker next considers the same eligible repair
- **THEN** knowledge no longer prevents the normal repair job.

#### Scenario: Native refusal still wins
- **WHEN** a knowledgeable worker cannot reach or reserve the target, lacks necessary work ability or faces another ordinary refusal
- **THEN** repair remains unavailable for that ordinary reason.

#### Scenario: Colony workers and foreign visitors
- **WHEN** an admitted provider permits a colony prisoner, slave or mech to repair, or a foreign visitor repairs for another faction
- **THEN** colony workers use the colony's knowledge while the foreign visitor's work is not restricted by this feature.

#### Scenario: Repair does not govern other uses
- **WHEN** the colony equips, uses, buys, moves, constructs, recycles, smelts or destroys an item
- **THEN** this feature adds no restriction to that action; each action's own rules still apply.

### Requirement: RK02 Production knowledge follows alternative research routes

For an item with identifiable manufacturing routes, the colony SHALL know how to repair it when
at least one route's research requirements are fulfilled. All research requirements within that
route SHALL be required together. A recipe's explicitly required research SHALL determine its
knowledge requirement; only when it has no explicit research SHALL the construction research of
any legitimate workbench that offers it supply the fallback. An unrestricted such workbench SHALL
provide an unrestricted research route. The workbench need not currently exist.

All identifiable manufactured outputs SHALL count, including recipes making several item types.
Repair, recycling or refurbishment of an existing item SHALL NOT itself teach how to manufacture
that item. An item without an identifiable manufacturing route SHALL acquire no invented research
requirement; building requirements and the optional technology ceiling still apply. Available
ingredients, pawn skills and non-research restrictions on manufacturing SHALL NOT change knowledge.

#### Scenario: Alternative recipe
- **WHEN** one manufacturing recipe's research is complete and another recipe for the same item remains locked
- **THEN** the completed route satisfies production knowledge regardless of recipe ordering.

#### Scenario: All requirements of one recipe
- **WHEN** a manufacturing route requires two research projects and only one is complete
- **THEN** that route does not yet satisfy production knowledge.

#### Scenario: Bench research fallback
- **WHEN** a recipe has no explicit research and only workbenches requiring unfinished research can offer it
- **THEN** the colony cannot repair its manufactured output through that route until one bench's research is complete.

#### Scenario: Alternative workbench including content-added offerings
- **WHEN** a recipe without explicit research is offered by several legitimate workbenches and one has no research requirement or has completed research
- **THEN** that route is known, including an offering added by the workbench's own recipe list.

#### Scenario: Explicit recipe knowledge does not add a bench gate
- **WHEN** the recipe's explicit research is complete but a possible workbench has additional unfinished construction research
- **THEN** that recipe still satisfies the repair knowledge requirement.

#### Scenario: Multiple manufactured outputs
- **WHEN** a researched manufacturing recipe produces several different items
- **THEN** every identifiable manufactured output gains that research route.

#### Scenario: No manufacturing recipe
- **WHEN** a recovered item has no identifiable manufacturing route
- **THEN** the production-research part does not refuse repair solely because the item is uncraftable.

#### Scenario: A repair bill cannot unlock itself
- **WHEN** an available mending or refurbishment recipe returns an existing damaged item
- **THEN** that recipe does not bypass unfinished manufacturing research for the item.

#### Scenario: Knowledge is distinct from production capacity
- **WHEN** the colony knows the research but lacks a built workbench, ingredients, a sufficiently skilled crafter or a non-research manufacturing permission
- **THEN** those absences do not erase repair knowledge; the actual repair retains its own normal requirements.

### Requirement: RK03 Buildings retain applicable knowledge and scenario restrictions

Building repairs SHALL additionally require the building's construction research and minimum
construction technology. Advancing beyond a building's maximum construction technology SHALL NOT
remove the ability to repair it. Existing scenario or storyteller prohibitions on building traps,
mortars or turrets SHALL also prevent their repair, with an explanation distinct from research.

#### Scenario: Direct building research
- **WHEN** a building requires unfinished construction research even though another production route is known
- **THEN** its repair remains blocked by that unfinished research.

#### Scenario: Below minimum construction technology
- **WHEN** the colony is below a building's minimum construction technology
- **THEN** that building cannot be repaired until the minimum is met.

#### Scenario: Primitive furniture in an advanced colony
- **WHEN** an industrial colony otherwise qualifies to repair a hoopstone ring or Game of Ur board
- **THEN** having advanced beyond that furniture's maximum construction technology does not prevent repair.

#### Scenario: Scenario prohibits the equipment
- **WHEN** the active difficulty prohibits construction of the target trap, mortar or turret
- **THEN** repair is refused for that restriction even if its research and technology requirements are satisfied.

### Requirement: RK04 Optional technology ceiling uses consistent inherited settings

The common mod settings SHALL place repair configuration under **Work → Repair knowledge**.
**Limit repairs by colony technology** SHALL default to Off. **Technology levels above colony**
SHALL default to 1 and accept whole values from 0 through 7, remaining inactive while the ceiling
is Off. With the ceiling On, an item SHALL require a technology level no higher than the colony's
current level plus that allowance, capped at Archotech. The ceiling SHALL be additional to research
and applicable building restrictions; turning it Off SHALL NOT bypass them. Items with no assigned
technology level SHALL not be rejected by this ceiling.

Each row SHALL use the common Global default, Current save and Use global controls, persistence and
dormant-override rules. New or missing owned entries SHALL consistently use the stated defaults and
inherit global values. Changes SHALL affect the next relevant work decision, including ongoing
work checks. Save controls and their mutation actions SHALL be inactive at the main menu. No
upstream settings SHALL be imported or modified automatically.

#### Scenario: Consistent first-use defaults
- **WHEN** the feature first loads, or an owned settings entry is missing after loading a save
- **THEN** its built-in policy is ceiling Off with allowance 1, using current global values where inherited.

#### Scenario: Ceiling boundary
- **WHEN** an industrial colony enables allowance 1 and otherwise eligible repairs target spacer and ultra equipment
- **THEN** spacer equipment passes the ceiling and ultra equipment does not.

#### Scenario: Research remains required
- **WHEN** the ceiling is disabled or admits a target whose required research is unfinished
- **THEN** the research refusal still applies.

#### Scenario: Full range and unassigned technology
- **WHEN** allowance 7 exceeds Archotech or the target has no assigned technology
- **THEN** the displayed ceiling stops at Archotech and unassigned technology alone does not prevent repair.

#### Scenario: Global and save isolation
- **WHEN** a player changes global defaults, uses a save override, restores Use global or switches saves
- **THEN** inherited saves follow current globals, overrides remain isolated and dormant values are retained without changing other saves.

#### Scenario: Main menu
- **WHEN** settings are opened without an active save
- **THEN** global controls work, save controls and Use global are visibly inactive, and no previous or implicit save is edited.

### Requirement: RK05 Refusal explains the actual missing condition

Knowledge refusals SHALL be shown through the relevant native repair command or work feedback,
identifying missing research, an unmet technology threshold or a scenario restriction as applicable.
An already unavailable native command SHALL retain its earlier reason. Candidate filtering SHALL
continue to other eligible targets rather than repeatedly selecting the same refused item. The
feature SHALL add no bottom tab or independent management window.

#### Scenario: Drafted repair refusal
- **WHEN** a drafted worker has an otherwise usable repair command but lacks the required knowledge
- **THEN** the command is unavailable and explains the missing condition without starting a job.

#### Scenario: Existing disabled reason
- **WHEN** the native repair command is already disabled because no path exists
- **THEN** the command retains that explanation instead of replacing it with a knowledge message.

#### Scenario: Another repair candidate exists
- **WHEN** a worker or automatic repairer encounters one unknown target and another eligible target
- **THEN** the unknown target does not prevent consideration of the eligible one or produce repeated job-start failures.

#### Scenario: Current settings explanation
- **WHEN** the technology ceiling is enabled with an active colony
- **THEN** settings show the current colony technology and effective maximum repair technology alongside the scoped controls.

### Requirement: RK06 Repair eligibility remains safe throughout work

Knowledge SHALL be checked when repair is offered, when work starts and before further repair
results or resource consumption. A change that removes eligibility SHALL stop further restricted
work without consuming additional ingredients, destroying the target or donor, duplicating items
or applying a free completion. Already legitimate repair progress and costs SHALL remain.
Interrupted work SHALL release or safely retain carried/reserved items under its ordinary recovery
rules. Saving, loading, map switching and changes to available content SHALL not preserve stale
permission. Newly learned knowledge SHALL be usable at the next normal work decision.

#### Scenario: Research or settings change before start
- **WHEN** a previously chosen job is no longer eligible when the worker reaches its target
- **THEN** it stops before repair results or repair costs are applied.

#### Scenario: Ceiling tightened during continuous repair
- **WHEN** a player tightens the ceiling while a worker or admitted automatic repairer is restoring hit points
- **THEN** no further restricted repair is applied after the next work check, and previously restored hit points remain.

#### Scenario: Breakdown component protected
- **WHEN** breakdown repair loses eligibility before its component-consuming completion
- **THEN** the component is not consumed, the breakdown is not cleared and the items remain recoverable.

#### Scenario: Mending ingredients protected
- **WHEN** a mending job loses eligibility before returning the repaired item
- **THEN** no target, donor or additional material is consumed by that refused completion and no duplicate or fully repaired output appears.

#### Scenario: Load and map transition
- **WHEN** a save is loaded or work is considered on another loaded map
- **THEN** eligibility uses that save's current colony knowledge and settings, with no permission carried over from another game or earlier content set.

### Requirement: RK07 Optional repair systems share the target rule

Admitted repair systems SHALL apply the same knowledge rule to the equipment being repaired.
This includes maintenance, repair benches, repairable gear, weapon donors, mending and automatic
repair. Ordinary material
ingredients SHALL NOT need their own repair knowledge merely because they are consumed in a repair.
Provider costs, donor consumption, work, range, power and success rules SHALL remain intact. The
restriction SHALL not convert maintenance into new repair content or apply to unrelated recycling,
medical care, psychic repair or other unadmitted actions.

#### Scenario: Maintenance of unknown equipment
- **WHEN** an admitted mechanical maintenance provider offers work on equipment whose knowledge is missing
- **THEN** that work is refused under the same rule as repair, without changing other maintenance behavior.

#### Scenario: Material does not become another repair target
- **WHEN** a known item can be mended using steel, cloth, components or another ordinary ingredient
- **THEN** this feature tests the repaired equipment rather than rejecting its material because that material has unrelated manufacturing research.

#### Scenario: Donor-based weapon repair
- **WHEN** an admitted repair action needs a second weapon as a donor
- **THEN** the repaired weapon must be known and normal donor selection and consumption apply only to an eligible completed repair.

#### Scenario: Autonomous repairer
- **WHEN** an admitted player-owned automatic repairer finds an eligible known target
- **THEN** it repairs under its own range, power and rate rules, and stops further repair when knowledge eligibility is revoked.

#### Scenario: Direct composition with owned work
- **WHEN** a future megamod repair workflow uses the same equipment
- **THEN** it presents one consistent knowledge decision while owned recycling, destruction and medical work retain their distinct rules.

### Requirement: RK08 Existing developer override remains distinct

The native God mode control SHALL bypass only the additional restrictions of this feature, using
the existing top developer interface. Other native or provider refusals SHALL remain. No ordinary
settings, bottom-tab or contextual cheat button SHALL be added for this bypass.

#### Scenario: God mode enabled
- **WHEN** native God mode is enabled for an otherwise permitted repair of unknown equipment
- **THEN** this feature does not refuse it for research, technology or its added building restrictions.

#### Scenario: God mode does not grant native capability
- **WHEN** God mode is enabled but the native or provider action is still unavailable
- **THEN** this feature does not create that action or override its refusal.

#### Scenario: God mode disabled again
- **WHEN** God mode is turned off before further repair work
- **THEN** the next work check again enforces the colony's current knowledge policy.

### Requirement: RK09 Incompatible restrictions fall back coherently

The ordinary building feature SHALL work without optional repair mods. Missing optional providers
SHALL be quiet. An enabled patch whose required behavior cannot be confirmed SHALL remain inactive
and produce a bounded startup warning identifying the patch, target requirement, failure, affected
behavior and fallback. An affected repair path SHALL fall back as a whole to its native/provider
behavior, with no partial selection/completion restriction. Benign unrelated changes and compatible
third-party patches SHALL be allowed. Later detected loss of compatibility SHALL suspend the
affected owned restriction and report its fallback without damaging work or items.

When I Cant Fix That is active, the overlapping owned restriction SHALL be inactive with an
explanation, leaving the original provider in control. Original files, settings, mod selection and
patches SHALL not be changed. Advertised upstream integrations SHALL not be presented as verified
megamod compatibility before their actual workflows pass acceptance.

#### Scenario: Core-only repair policy
- **WHEN** the game runs with the megamod's required dependencies and no optional repair providers
- **THEN** ordinary building and breakdown knowledge checks work without missing-provider warnings.

#### Scenario: Changed optional repair contract
- **WHEN** an installed optional repair provider no longer supports safe knowledge checks through its whole repair lifecycle
- **THEN** its owned restriction stays inactive, its original repair behavior remains and the startup warning states that fallback.

#### Scenario: Benign other patch
- **WHEN** another mod changes unrelated behavior while the required repair semantics remain true
- **THEN** that change alone does not disable the owned restriction.

#### Scenario: Original source co-installed
- **WHEN** I Cant Fix That is active alongside the megamod
- **THEN** the megamod does not add a second repair-knowledge policy and reports that the source controls the overlap.

#### Scenario: Compatibility changes after startup
- **WHEN** a later change invalidates an admitted repair path
- **THEN** its whole owned restriction is suspended safely, existing items remain intact and the reported fallback matches the behavior actually available.
