# Contextual housekeeping

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
S024, Common Sense replacement. Specification only; defaults and proposed corrections are tracked
in CS-Q1–4. Preferences use the sibling configuration capability.

## ADDED Requirements

### Requirement: Clean briefly around an ordinary activity
Eligible player-controlled colonists SHALL perform a bounded cleaning opportunity near an ordinary non-bill building activity or recreation destination before beginning that activity when Clean before work is enabled.
Cleaning requires active Cleaning work, actual ability, safe access, allowed/home area and current
cleaning permissions. Burning, mental-state, downed or urgent-care needs prevent optional cleaning.
Player-forced orders and an existing queued sequence are not silently delayed. Construction frame
work opts out; prisoner recruitment/conversion/enslavement and ceremony participation can opt in
where their ordinary native availability permits. A provider's explicit no-opportunity choice is
honored. Completed food delivery can trigger the same bounded cleanup when no queued order follows.
Bill work uses only the separately controlled pass after ingredient delivery, not an additional
before-collection cleaning pass. The same worker eligibility, explicit-order and urgent-need
safeguards apply to every owned optional cleaning route.

The proposed detour rule skips pre-work cleaning if the actual walk to the building costs more than
500 ordinary movement ticks and direct travel to the next required ingredient is less than 70% of
travel via that building. Missing reliable route information skips this optional detour. It does
not change speed, unlock doors or authorize dangerous shortcuts.

#### Scenario: Researcher starts work in a dirty room
- **WHEN** an eligible researcher starts an ordinary research job near accessible filth
- **THEN** the pawn physically cleans within the opportunity budget and then resumes research.

#### Scenario: Player order or urgent need
- **WHEN** a player forces a task, a queued sequence already exists, or the worker needs urgent tending
- **THEN** optional pre-work cleaning does not insert a delay ahead of that task or care.

#### Scenario: Forbidden cleaning area
- **WHEN** the activity is allowed but its surrounding filth is outside permitted cleaning space
- **THEN** the pawn proceeds with the activity without repeatedly trying to clean forbidden filth.

#### Scenario: Both cleaning preferences enabled for a bill
- **WHEN** a cook starts an ordinary bill with Clean before work and Clean between bill stages enabled
- **THEN** ingredients are collected before the single between-stage cleaning pass
- **AND** the before-work preference does not insert another cleaning pass for that bill.

### Requirement: Cleaning has one finite physical-work budget
Each cleaning opportunity SHALL select at most its configured number of distinct eligible filth objects, then finish or resume its parent activity without replenishing the same opportunity indefinitely.
Defaults are 5 objects before work, 5 between bill stages/before eating, and 0 meaning all currently
eligible objects after tending. Counts refer to filth objects, not thickness layers. Cleaning each
object still requires its normal work, current cleaning speed and terrain effects. In an ordinary
room, consider that room and adjacent doorways; for a huge room or one larger than 160 cells, consider
only the nearest 200 radial cells around the activity within the same room/doorways. The limit applies
across all cells together. New dirt does not continually extend the selected pass. Invalid or newly
reserved filth is skipped; no dirty object is removed for free.

#### Scenario: Several filth objects share the last cell
- **WHEN** a five-object opportunity has selected four objects and reaches a cell with several more
- **THEN** it selects at most one more and resumes the parent activity after that finite pass.

#### Scenario: New dirt during an unlimited pass
- **WHEN** an unlimited post-tending pass is underway and more dirt appears
- **THEN** it completes the finite selection already made instead of restarting forever.

### Requirement: Clean between ingredient delivery and productive work
When Clean between bill stages is enabled, an eligible worker SHALL clean within the between-stage budget after delivering required ingredients and before beginning the recipe's ordinary work.
The bill, exact ingredients, material choice, unfinished work and facility ownership remain the same.
No second bill starts and no ingredients are consumed merely to clean. If cleaning becomes unavailable,
continue the still-valid bill; if the bill itself becomes unavailable, end safely with physical
ingredients retained through ordinary recovery. Autonomous worktables retain their actual container
and production lifecycle. Optional cleaning never delays an urgent medical procedure.

#### Scenario: Dirty stove with delivered ingredients
- **WHEN** a cook has delivered the selected ingredients and the stove allows incidental cleaning
- **THEN** the cook cleans the bounded eligible dirt and cooks the same bill with the same material quantities.

#### Scenario: Bill removed during cleaning
- **WHEN** the player deletes the bill while the worker cleans
- **THEN** that recipe is not started or consumed afterward and its physical ingredients remain recoverable.

### Requirement: Eating cleanup is separately optional
When Clean before eating is enabled, an eligible undrafted pawn SHALL perform one between-stage cleaning pass around its actual eating or social-relaxation place before ordinary consumption or social activity.
The setting defaults off. Food taken from inventory or a dispenser stays physical and reserved where
appropriate; cleanup never makes a second meal or dispenses again. Inability to clean does not prevent
eating. Urgent hunger, danger, drafting, treatment and explicit player commands take precedence.
No unsupported dining provider is replaced with a generic eating job.

#### Scenario: Cleanup before a dispenser meal
- **WHEN** a pawn obtains one dispenser meal and eating cleanup is enabled
- **THEN** it cleans within the budget and eats that same meal, without dispensing another.

#### Scenario: Urgent hunger during cleanup
- **WHEN** the pawn needs to eat urgently before its optional pass finishes
- **THEN** cleanup yields to eating and does not restart ahead of the same meal.

### Requirement: Patient care precedes cleanup
After successful tending, eligible workers SHALL clean the patient's surroundings within the post-tending budget when enabled, unless continuing care or existing orders take precedence.
Another patient needing tending in the same room, or one expected to die from blood loss in less than
six hours, takes precedence over the cleanup. All other urgent native care remains authoritative.
Checking for follow-up care does not start, reserve, consume or lose another job. The optional visit
or food-delivery cleanup cannot be substituted for treatment.

#### Scenario: Another bleeding patient
- **WHEN** a doctor finishes tending and another accessible patient has less than six hours before predicted blood-loss death
- **THEN** the doctor remains available for treatment instead of beginning optional cleanup.

#### Scenario: Successful nonurgent care
- **WHEN** tending finishes with no priority follow-up and eligible dirt remains
- **THEN** the doctor performs one physical cleaning pass under the configured budget.

### Requirement: Buildings retain their own cleaning choice
Each supported bill-work building SHALL retain a contextual Allow incidental cleaning choice, initially enabled, independently of whether its control is currently shown.
This is saved building state. Disabling it prevents the associated owned pre-work/between-stage
cleaning at that building; global cleaning preferences still determine whether the opportunity exists.
Hiding the optional control does not reset a saved opt-out. It does not stop an ordinary explicit
Clean order or change other buildings. No new bottom tab is added.

#### Scenario: Hide an opted-out building's control
- **WHEN** incidental cleaning is disabled on one workbench and the player later hides cleaning controls
- **THEN** that workbench stays opted out through reload while other workbenches retain their own choices.

### Requirement: Postpone weather-replenished clearing
When postponing precipitation clearing is enabled, pawns SHALL leave snow/sand clearing orders pending while snow, rain or airborne sand is currently falling on that map and reconsider them after it stops.
Ordinary filth cleaning and unrelated work remain available. The feature does not remove precipitation,
clear surfaces for free or erase the player's designated clearing areas.

#### Scenario: Snow resumes during a clearing shift
- **WHEN** new snow is falling as the worker considers another snow/sand clearing job
- **THEN** the worker takes other work and the pending area can be cleared normally after precipitation ends.
