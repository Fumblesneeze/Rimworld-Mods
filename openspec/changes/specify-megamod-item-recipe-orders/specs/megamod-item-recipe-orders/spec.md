# Item-specific recipe orders

Owner **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
Origin replacement, S096/P2-25 Micro Designations. Specification only. Corrections and extensions
below are selected draft behavior; they are not claims about the original mod or completed work.

## ADDED Requirements

### Requirement: Select a recipe for a particular item
The player SHALL be able to order a supported recipe from a loose item's contextual controls without creating or editing a general production bill.
Actions name the operation and appropriate facility, distinguish operations with different results,
and show pending quantity, waiting reasons and cancellation. Commands apply only to the selected
physical item units: the initial quantity is the selected stack's current count and the player can
reduce it before confirming. Each unit is processed once. Compatible selected items can share a
command, but each retains its own authorization. If a keybinding matches several offered actions,
it opens an explicit choice instead of silently executing one. No new bottom tab is added.

#### Scenario: Cut these chunks
- **WHEN** the player selects two stone chunks and orders their applicable stone-cutting recipe
- **THEN** those chunks receive pending orders, ordinary bills remain unchanged, and later chunks are not automatically marked.

#### Scenario: Authorize part of a stack
- **WHEN** a supported equipment stack contains five units and the player confirms a recipe quantity of two
- **THEN** only two units are authorized, the remaining three are not processed by this order, and the displayed quantity reflects that limit.

#### Scenario: Different disposal outcomes
- **WHEN** an item supports native smelting, owned material recycling and owned melee destruction
- **THEN** those choices describe their distinct work and result, and choosing one does not silently execute another.

#### Scenario: Shared shortcut
- **WHEN** the same shortcut matches both smelting and destroying the selected item
- **THEN** the player chooses the intended action before any processing order is changed.

### Requirement: Recipe choices represent a real single-item operation
The mod SHALL offer only supported item-processing recipes whose complete current material requirement can be met by one physical unit of the selected item at an appropriate work facility.
This includes supported butchery, stone cutting, slag processing, equipment smelting or destruction,
cremation and custom single-item operations. A stackable item is not excluded merely because it can
stack. Recipes requiring additional material, multiple units, a living patient or a placed building
are outside this feature. Existing batch and medical bills remain available through their normal UI.
All hard ingredient, item-state and provider restrictions remain effective. An explicit item choice
can change a recipe's normal default ingredient preference only where an ordinary bill could permit
that item. It cannot override a prohibition. Known consequences remain visible and take effect.

#### Scenario: One ingredient category needs three chunks
- **WHEN** a recipe requires three slag chunks even though it lists only one kind of ingredient
- **THEN** it is not offered as a single-item action and its ordinary batch bill is unchanged.

#### Scenario: Explicit human corpse selection
- **WHEN** a supported butchery recipe permits a human corpse but its fresh bill normally selects animal corpses only
- **THEN** explicit selection can authorize that corpse, with the actual butchery consequences explained and preserved
- **AND** an actual ingredient or worker prohibition still prevents the action.

#### Scenario: Corpse changes condition
- **WHEN** a marked corpse becomes rotten and its chosen recipe no longer permits it
- **THEN** work cannot consume it through that recipe, its order shows why, and no fresh corpse is substituted.

#### Scenario: Custom repair needs another ingredient
- **WHEN** a repair recipe needs the selected weapon plus steel
- **THEN** this feature does not offer that recipe as a one-item action or perform a free repair.

### Requirement: Planning and work availability remain distinct
The mod SHALL distinguish a valid item action from whether research, a built facility or an eligible worker currently allows it to run.
By default a supported action remains visible and can be ordered before its research or facility is
available, with the missing conditions stated. Ineligible item conditions and unsupported processing
remain unavailable. Research, facility presence and recipe availability are refreshed when they
change; selecting another object or waiting for a failed job is not required. Pending orders remain
inspectable and cancellable regardless of visibility preferences.

#### Scenario: No smelter yet
- **WHEN** the player examines an item with a supported smelting action before a suitable smelter exists
- **THEN** the default UI allows a pending order with a missing-facility explanation and does not consume the item
- **AND** completing a suitable facility allows eligible work without replacing the order.

#### Scenario: Research is revoked
- **WHEN** a required research or other recipe availability condition stops being satisfied before completion
- **THEN** the order waits or stops safely before consuming the intact input or awarding a result.

### Requirement: Workers use the ordinary recipe and facility rules
An ordered recipe SHALL require an eligible worker to carry one authorized unit to a usable local facility and perform the real recipe work there.
Respect the recipe's actual work type, work priorities, skills, capacities, research and other
availability conditions, the worker's access and reservations, and the facility's interaction space,
power, fuel and operating rules. At equal work priority, ordinary bill work for that work type is
considered before automatic item-recipe work. Explicit work orders retain ordinary precedence.
Prefer the nearest eligible facility to the item, with stable ties; an unusable nearer facility does
not hide a usable alternative. Refueling can prepare a suitable station through ordinary refueling.
Marking never unforbids, grants reachability, ignores danger limits or increases carrying capacity.
Owned hauling cargo is safely recovered before an ordinary separate hand-carried recipe load.

#### Scenario: Insufficient skill
- **WHEN** a marked recipe requires a skill the available workers lack
- **THEN** no worker starts it by bypassing that requirement, and the order explains the missing capability.

#### Scenario: Refuel or use another station
- **WHEN** the nearest station lacks fuel and cannot currently be refueled but another eligible station works
- **THEN** the worker can use the eligible station, rather than repeatedly picking up and dropping the input
- **AND** if none qualifies the item waits intact with a reason.

#### Scenario: Competing work and reservations
- **WHEN** an eligible worker has ordinary bill work at the same work priority or another worker owns the input or interaction position
- **THEN** normal bill precedence and reservations remain effective and the item order does not create a second processing job for the same unit.

#### Scenario: Held hauling cargo
- **WHEN** a worker already carries owned inventory-hauling cargo and would pick up an ordered corpse
- **THEN** the existing cargo is safely recovered first and no extra carrying allowance is created.

### Requirement: Preserve the chosen recipe's actual outcome
The mod SHALL preserve the chosen recipe's current work amount, speed factors, skills and experience, ingredient consumption, products, repair effects, quality and ordinary consequences.
The feature supplies explicit item selection, not cheaper work, free materials or a different yield.
It preserves normal work and completion notifications needed by supported facility behavior.
Outputs are physical and respect actual stack and placement limits. Viewing an action or its preview
never consumes an item, performs repair, grants experience or draws its eventual random outcome.
A non-consuming operation completes once and clears that authorization; later damage or changed
condition does not make it an automatic repeat order.

#### Scenario: Native slag processing
- **WHEN** one ordinary steel slag chunk is processed using the inspected native recipe with no other balance changes
- **THEN** the worker performs its 400 base work, adjusted by actual speed factors, and produces its 15 steel
- **AND** the custom equipment-recycling percentage is not applied to that recipe.

#### Scenario: Smelt or destroy
- **WHEN** the native combined operation is used on an accepted smeltable item or accepted nonsmeltable item
- **THEN** it uses the recipe's smelting work and actual recovery for the smeltable item, or its destruction work and no smelting recovery otherwise, currently 1600 and 400 base work respectively
- **AND** it is not replaced by the custom repeated-melee action.

#### Scenario: One repair only
- **WHEN** an admitted one-item mending operation repairs the selected item successfully
- **THEN** the item remains with the recipe's repair result and the order ends
- **AND** damage sustained later requires another explicit order.

### Requirement: One processing choice follows only authorized quantities
Owned recipe orders SHALL use the same exclusive per-item processing choice and quantity rules as owned recycling and melee destruction.
Changing the choice before an irreversible effect stops the old work safely and discards its partial
work; unrelated designations remain. Each authorized portion retains active or suspended state
through splitting, merging, carrying and saving. Known consumption retires its own authorization.
When another action removes only a count from mixed marked/unmarked material without attribution,
debit marked quantity first. If differing processing choices or suspension states make attribution
ambiguous, pause the affected orders for explicit resolution rather than guess. Worn, equipped,
installed or personally kept items are not newly targeted; taking an ordered item for such use
cancels its order. Ordinary hauling preserves it. Leaving the map suspends work, clears stale
station claims and requires explicit Resume after return. Missing items never authorize substitutes.

#### Scenario: Replace only the processing choice
- **WHEN** the player changes a surviving item's owned recycling order to its supported native smelting recipe
- **THEN** the old processing work stops and only the new applicable choice remains
- **AND** unrelated hauling or other orders are retained until consumption makes them moot.

#### Scenario: Merged active and suspended units
- **WHEN** two active units merge with one suspended unit and three unmarked units
- **THEN** only the two active units can be processed with safe attribution, or work waits visibly
- **AND** neither splitting, reload nor resuming a different portion activates the suspended or unmarked quantities.

#### Scenario: Unspecified consumption
- **WHEN** another job removes three units from two marked and three unmarked units without identifying the portion
- **THEN** no recipe authorization remains on the two physical survivors.

#### Scenario: Keep or depart with the item
- **WHEN** a pawn equips an ordered weapon, or ordinary hauling carries an ordered item out of the map
- **THEN** equipping cancels that weapon's order, whereas map departure suspends local processing
- **AND** dropping the weapon does not reauthorize it and returning cargo needs explicit Resume.

### Requirement: Interrupted or failed work has a bounded recovery path
An interrupted recipe SHALL preserve the surviving authorized input and follow that recipe's normal unfinished-work rules without transferring work to a substitute or awarding an unfinished result.
Native recipes that lose uncommitted work on interruption still do so; recipes with supported
unfinished-work persistence retain it. Saving and loading the same active supported job retains its
normal progress, while cancelled jobs do not acquire invented saved work. Temporary access, power
or fuel loss can wait for that condition to recover. The first unsupported execution failure, or
failure without an identified recoverable condition, suspends with an explanation and Retry, so work scanning, pawn changes or reload
cannot endlessly replay the same failed pickup/work cycle. Explicit Retry or a relevant capability
change permits a fresh bounded attempt; it does not undo previous completed effects.

#### Scenario: Draft during ordinary processing
- **WHEN** the player drafts a worker before a recipe's irreversible completion
- **THEN** ordinary interruption occurs, the surviving input remains physical and no completion reward is granted
- **AND** a later attempt retains or restarts work according to that recipe's normal unfinished-work behavior.

#### Scenario: Unsupported custom callback
- **WHEN** an admitted custom job fails because its required execution context is no longer supported
- **THEN** the order is suspended, intact input is not consumed, and the reason and Retry remain available
- **AND** another work scan or reload does not silently repeat the failed cycle.

### Requirement: Completion and delivery cannot duplicate a result
An item-recipe order SHALL complete each authorized unit at most once, retaining identifiable recovery state when a supported completion or output placement is interrupted.
Before an irreversible effect, cancellation preserves the intact input and awards no result. After
an effect has committed, cancellation cannot recreate consumed material, undo a completed repair,
reroll output or repeat completion rewards. Resolved products still awaiting placement remain
recoverable and are not generated again; retry places only the undelivered remainder. Recovery
status and retry stay accessible through contextual item-order recovery controls after input or
facility removal. A route whose completion effects cannot be safely tracked remains unavailable
before work starts. Ordinary bills and other mods' jobs are not globally altered to achieve this.

#### Scenario: Partial product placement
- **WHEN** a completed recipe consumed its input and only one of three resolved product stacks can be placed
- **THEN** the other two remain owed and visible for recovery
- **AND** retry or reload places only the remainder without restoring input or rerunning product creation and completion effects.

#### Scenario: Facility removed after completion
- **WHEN** the bench disappears while products still await placement
- **THEN** contextual recovery remains available and safely places the owed products when possible without another input or another performance of the recipe.

#### Scenario: Save with pending and completed work
- **WHEN** a save contains an intact pending input and a different consumed input with owed outputs
- **THEN** loading restores the first order only against its surviving units and the second only as output recovery
- **AND** no input, result, repair or reward is duplicated.

### Requirement: Visibility preferences use common settings
The mod SHALL expose Hide recipes with unmet colony requirements, Hide recipes with no built facility, and Hide recipes whose facilities are all unresearched, in that order, each initially off.
Colony requirements include the recipe's research, ideology and faction prerequisites; worker skill,
ingredient access and facility presence are separate work conditions and do not trigger that first filter.
These preferences affect new contextual action visibility only and never grant work eligibility,
cancel pending orders or hide their status/recovery controls. They use common Global default,
Current save and Use global controls. New or missing save values inherit dynamically; using global
retains a dormant override and disables its per-save editor. Clearing Use global restores the
override or seeds it from the current effective value. Without a loaded save all save controls and
their mutation callbacks are disabled. Changes apply on the next contextual refresh. Item quantities,
choices, suspension and progress are gameplay state, not settings. All controls, scope labels,
status and reasons are localized in the seven baseline languages.
For the facility-research visibility preference, an existing compatible facility counts as
available even if its construction research is not learned; actual work restrictions still apply.

#### Scenario: Hide unavailable actions without losing orders
- **WHEN** the player enables Hide recipes with no built facility after ordering an item for a missing station
- **THEN** new matching action choices are hidden but that order's status and Cancel remain accessible.

#### Scenario: Research alone does not meet every colony requirement
- **WHEN** the recipe's research is complete but an ideology or faction prerequisite remains unmet
- **THEN** Hide recipes with unmet colony requirements hides that new action when enabled
- **AND** with the preference off it remains a planned choice with the unmet condition explained, without permitting work.

#### Scenario: Save inheritance and main menu
- **WHEN** a save uses global values and the global visibility preference changes
- **THEN** the save follows the new value while an explicit override in another save remains unchanged
- **AND** the main menu allows only global edits, with no mutation of an implicit or previously loaded save.

### Requirement: Compose owned features and isolate unsupported providers
The mod SHALL compose item recipe orders directly with owned processing choices, repair knowledge, hauling and facility behavior while preserving ordinary workbench bills and independently supported features.
Item orders do not appear in, link, copy or synchronize through a workbench's general bill list.
An admitted mending action obeys the owned repair-knowledge restriction. Working facilities receive
their normal activity and power behavior. Compatible changes alone do not disable the feature;
missing or changed required capabilities disable only the affected integration and log a bounded
startup warning explaining unavailable behavior and fallback. Ordinary bills remain usable.
With original Micro Designations active, owned new recipe orders are withheld and unfinished owned
orders are visibly suspended; removing it requires explicit Resume after safe activation. Owed
outputs remain recoverable once. Conflicting external orders on the same item require a supported
ownership integration or visible suspension of affected owned processing. No provider is removed,
foreign order/settings imported or package identity impersonated.

#### Scenario: Linked workbenches
- **WHEN** the player orders a particular corpse to be butchered at a linked workbench
- **THEN** no ordinary bill is inserted or copied to linked benches and no other corpse is authorized.

#### Scenario: Repair knowledge and active facility
- **WHEN** an admitted custom mending operation lacks required owned repair knowledge
- **THEN** it waits without granting repair through this alternate entry point
- **AND** when legitimate work runs, normal work activity and power behavior remain effective.

#### Scenario: Original provider coexistence
- **WHEN** a save with pending owned recipe work is opened with original Micro Designations active
- **THEN** owned new and resumed recipe work is withheld, intact inputs remain suspended and already owed outputs can be recovered once
- **AND** later removing the provider does not silently restart destructive work.

#### Scenario: Changed provider with ordinary bills
- **WHEN** an external recipe's required job behavior no longer supports safe item orders
- **THEN** its owned item action is unavailable with a reason and startup warning
- **AND** its ordinary bill remains under that provider's control and other supported item actions remain usable.
