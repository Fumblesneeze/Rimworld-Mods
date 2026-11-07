# Partial ingestion

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
S085 FoodTracker replacement; observable draft only. FTR-Q1/Q2 must be resolved before enabling
affected drug or categorical-effect paths; FTR-Q3 also gates partial batch timing. These open
decisions do not waive the requirements below.

## ADDED Requirements

### Requirement: Admit a complete supported consumption path
The feature SHALL preserve partial consumption for ordinary movable food items, meals and supported
ingestible drinks and drugs, including compatible content-added items. The initial drug analysis
covers beer, ambrosia, psychite tea, smokeleaf, go-juice and flake. A name alone does not establish
support: all of an item's effects, restrictions, condition and actual consumption path must be
supported together. Unsupported items retain ordinary consumption without invented partial effects.
Living plants, corpses, non-ingestion medicine use and instant special-use objects are outside this
initial scope. Their ordinary behavior remains available. Drug category changes, such as the selected
medical go-juice behavior, do not themselves make a supported dose ineligible.

#### Scenario: Ordinary meal and compatible added meal
- **WHEN** a pawn begins eating either a normal meal or an added meal with supported behavior
- **THEN** either can retain a remainder without losing its dietary identity or granting new permission to eat it.

#### Scenario: Unhandled drug outcome
- **WHEN** a drug has an effect that cannot safely be apportioned or preserved
- **THEN** that item's whole consumption path remains ordinary, its unsupported partial behavior is explained once, and no fraction of its effects is applied separately.

### Requirement: Credit only actual consumption and retain the balance
Consumption SHALL account for the actual amount eaten during the eating/drinking/using stage, with
no credit for walking, fetching or waiting. Interruption commits only previously uncredited work
and leaves the complementary usable amount. Completing or cancelling again cannot credit it twice.
Consumption is bounded by the available amount and the actual intended batch, not an unrelated
stack's full contents. The initial owned behavior retains every positive usable remainder; it has
no 4.5%-of-item or 0.01-nutrition discard threshold. Ordinary need limits still apply, and genuinely
consumed excess cannot reappear as food merely because the eater was full.

#### Scenario: Half a meal before drafting
- **WHEN** a pawn eats half of a full meal and the player drafts it
- **THEN** the pawn receives that half's applicable nutrition once and the remaining half stays available; resuming does not repeat the first half.

#### Scenario: Cancellation before eating
- **WHEN** an ingestion order is cancelled during fetching or before any eating progress
- **THEN** the item and the pawn's ingestion effects remain unchanged.

#### Scenario: Batch boundary and small remainder
- **WHEN** a pawn consumes 2.25 item-equivalents from a three-item batch, or leaves 2% of one item
- **THEN** the first case retains 0.75 of an item and the second retains 2%, without a bonus whole item, hidden disposal or unconsumed nutrition credit.

### Requirement: Remaining eating work follows actual remaining amount
Whole-item eating duration SHALL retain the applicable normal duration and pawn modifiers. A
single-item remainder's eating work scales by the remaining portion of that same item; interrupted
work is not restarted as a full meal. Native multi-item batches use one eating duration, so their
resumed work cannot be inferred from remaining item count alone. Partial batch handling stays
inactive until FTR-Q3 specifies resumed and recombined batch timing, including transfer to a different
eater; ordinary batch consumption remains available. Pausing or changing simulation speed does not
add consumption. No source
40-tick minimum or beer120/go-juice100/ambrosia100 timing override is introduced. A positive remainder
still requires at least one actual simulation tick to consume; this rounding cannot create nutrition
or repeat effects. Feeding another pawn uses the patient's consumption, not the feeder's nutrition.

#### Scenario: Resume a quarter meal
- **WHEN** a pawn resumes a quarter of a meal under unchanged eating conditions
- **THEN** its remaining eating work is one quarter of the normal whole-meal work, rounded up to at least one tick, and only that quarter is consumed.

#### Scenario: Pause or feed a patient
- **WHEN** the player pauses during eating, or interrupts a supported patient-feeding action
- **THEN** paused time consumes nothing; feeding credits only the patient and preserves the remaining item without crediting the carer.

#### Scenario: Batch timing is not yet specified
- **WHEN** a multi-item eating batch lacks the required resumed-work contract
- **THEN** that path retains ordinary whole-batch handling instead of choosing a per-item duration or creating partially tracked progress with unspecified timing.

### Requirement: Dose effects follow the consumed original quantity
Supported measurable effects SHALL be proportional to the consumed original-item quantity, using
the item's applicable full-portion effects and the recipient's ordinary modifiers. This includes
nutrition, recreation, chemical satisfaction, rest, psyfocus, high/tolerance severity and existing
addiction-severity changes where present, preserving positive or negative direction and native caps.
A half of a half dose is one quarter of a full dose. Resuming, immediate completion, administration
or caravan consumption cannot grant a full dose for a remainder. A feature must not implement only
benefits while dropping risks, dependencies, notifications or other native consequences.

#### Scenario: Repeat interruption of a drug remainder
- **WHEN** a pawn consumes half a dose, then half of the remainder, then finishes it under unchanged conditions
- **THEN** measurable quantity-based effects are credited as 50%, 25% and 25% of the applicable full dose, with ordinary caps/decay and no duplicated full-dose completion.

#### Scenario: Harmful and recipient-dependent effects
- **WHEN** a supported drug reduces rest or changes addiction severity, or its recipient has body-size or genetic modifiers
- **THEN** the same direction and modifiers apply to the actual consumed quantity; interruption does not reverse a penalty into a benefit.

### Requirement: Interruption cannot evade or multiply categorical consequences
Positive consumption SHALL retain applicable food provenance, poisoning, ideology, drug policy,
chemical dependency, history and notification consequences. A zero-consumption attempt causes none.
Splitting, repeated cancellation and save/load cannot create extra opportunities to reroll or erase
the same consumed exposure, nor repeatedly refresh a once-per-consumption consequence without new
consumption. A full dose retains its normal risk baseline. The precise partial-dose addiction,
overdose, poisoning and categorical-event schedules remain FTR-Q1/Q2 decisions; affected paths MUST
stay inactive until those schedules and their observable boundary scenarios are specified together.

#### Scenario: Cancel and reload without consuming more
- **WHEN** the same partially consumed item is cancelled or reloaded repeatedly without further eating
- **THEN** no additional thoughts, dependency resets, addiction/overdose rolls or poisoning events occur and prior exposure is not erased.

#### Scenario: Risk contract is incomplete
- **WHEN** a drug's benefits can be scaled but its required risk/consequence behavior is unresolved
- **THEN** partial handling is not enabled for that drug; ordinary ingestion remains available rather than a risk-free fractional substitute.

### Requirement: Preserve the actual item and its permissions
A remainder SHALL retain the original food identity, ingredients, dietary tags, poison/contamination,
rot progress, damage, quality and other supported meaningful condition. Creation cannot freshen,
clean or improve it. Storage, food and drug policies and relevant orders continue to apply; neither
existing nor newly created policies are broadened. Splitting/merging cannot erase unsafe provenance
or turn a forbidden diet into an allowed one. Discovery or hauling never grants ingestion permission.

#### Scenario: Spoiled, damaged or poisoned meal
- **WHEN** eating a damaged or contaminated meal is interrupted
- **THEN** its remainder retains the applicable damage, age and unsafe condition, and cannot be sold or eaten as a fresh clean item.

#### Scenario: Restrictive filters
- **WHEN** a meat-containing leftover is offered to a vegetarian-only food policy or woody-only storage
- **THEN** it remains disallowed and neither filter changes; allowing another leftover elsewhere has no effect on those restrictions.

### Requirement: Keep interruption and failure outcomes consistent
Drafting, job replacement, downing, death and supported feeding interruption SHALL leave consumed
amounts, effects and remaining items consistent. Normal item destruction or theft remains effective;
the feature cannot resurrect it from an old eating snapshot. A failed placement or transfer cannot
duplicate items, grant unpaid effects or silently delete a remainder. An available lawful holder or
drop location retains it; if none exists, the attempted transfer leaves its prior holder/state intact
with a bounded explanation. Completion and interruption racing at the end count consumption once.

#### Scenario: Full hands or blocked drop
- **WHEN** interruption occurs and an intended drop cannot place the remainder
- **THEN** the remainder stays in an available lawful holder, or the transfer leaves the previous holder intact, without a second physical copy or extra consumption.

#### Scenario: The food is destroyed during the job
- **WHEN** an external event destroys the actual food while ingestion is underway
- **THEN** only already consumed progress can be credited and no usable remainder is recreated from the destroyed item.

#### Scenario: Final tick and cancellation
- **WHEN** cancellation coincides with normal completion
- **THEN** the pawn receives only the amount actually consumed and no duplicate leftover appears.

### Requirement: Preserve owned progress across save and game changes
Owned remainders and committed consumption SHALL survive save/load, storage/inventory travel and
map/world transitions. Saving during eating preserves enough progress to resume without resetting
the item or repeating effects. Another game cannot inherit live consumption, selections or transfers
from the previous game. Invalid or unsupported saved partial state is reported and preserved for
recovery, not repaired by inventing food, silently shrinking a stack or treating it as full.
Automatic import of FoodTracker saves/settings and removal-safe migration are not provided here.

#### Scenario: Reload mid-meal and after interruption
- **WHEN** the player reloads a save made during eating or with a stored remainder
- **THEN** the same amount remains, previously consumed effects are not credited again, and finishing consumes only the balance.

#### Scenario: Switch saves or remove required content
- **WHEN** another save is opened, or a partial item's required content is unavailable
- **THEN** no prior game's progress leaks into the new game; unsupported state receives an actionable explanation without silent quantity conversion or automatic rewriting of the original save.

### Requirement: Use one implementation and isolate unsupported changes
Only one implementation SHALL own overlapping partial ingestion. With original FoodTracker active,
the owned overlap remains inactive with an explanation and without changing the original's settings.
Unsupported game/provider changes leave affected partial behavior inactive with bounded startup
warnings identifying the missing behavior and fallback. Compatible unrelated changes remain allowed.
Existing owned leftovers cannot be reinterpreted as full items when handling becomes unavailable;
the player receives a recovery explanation before affected consumption or transfer can proceed.
This feature adds no bottom tab, general settings window or routine action toggle. Developer
diagnostics, if provided, are transient top debug controls and disabled when developer mode is off.

#### Scenario: Original provider or unsupported game update
- **WHEN** the original owns overlap or required support is no longer valid
- **THEN** there is no double consumption or partial patch behavior; the player sees the affected capability and fallback, while unrelated ordinary play remains usable.

#### Scenario: Existing partial stock loses support
- **WHEN** a saved owned half-meal is encountered with its required handling unavailable
- **THEN** it is not consumed, priced or transferred as a whole meal; its state is retained with a recovery explanation.
