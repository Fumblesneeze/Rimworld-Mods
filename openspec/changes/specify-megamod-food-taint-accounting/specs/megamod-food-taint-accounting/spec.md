# Finite food-taint accounting

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
Origin S041, Food Poisoning Stack Fix (Continued). Specification only. Rules that correct the source
or settle previously unspecified cases are proposed behavior, not additional user confirmations.

## ADDED Requirements

### Requirement: Compatible food carries a finite quantity of cooking taint
The mod SHALL preserve finite tainted and untainted quantities for supported food using ordinary
cooking-taint behavior. Each physical unit SHALL be tainted or untainted for this purpose; a stack
SHALL never contain more tainted units than food units or a negative quantity. Compatible modded food
SHALL use the same rules without requiring a known mod title. Food without this kind of taint SHALL
retain its ordinary behavior. Taint alone SHALL not change otherwise valid stacking, item identity,
ingredients, nutrition, quality, freshness, stack limits, value or ownership.

#### Scenario: Combine a tainted meal with clean meals
- **WHEN** one tainted meal merges with nine compatible untainted meals
- **THEN** ten meals remain, containing exactly one tainted meal
- **AND** stacking creates neither more food nor additional cooking taint

#### Scenario: A compatible mod adds meals
- **WHEN** a modded meal uses supported ordinary cooking-taint and stack behavior
- **THEN** the same finite-quantity rules apply while its ingredients, nutrition and other properties remain its own

### Requirement: Cooking preserves existing contamination opportunities
The mod SHALL leave native cooking skill, room cleanliness, recipe output quantity and contamination
rolls authoritative. When ordinary production taints an entire produced batch, all units of that
batch SHALL receive that taint and its actual cause; an untainted batch SHALL remain untainted.
The feature SHALL not reinterpret an entirely tainted multi-unit batch as only one tainted meal,
introduce another cooking roll per unit or cure it by moving it. A supported explicit poisoning
action on a whole existing stack SHALL likewise affect its actual units according to that action.

#### Scenario: Produce a tainted multi-meal batch
- **WHEN** ordinary production creates four meals and marks that produced batch tainted
- **THEN** four tainted meals result, retaining the actual cooking cause and ordinary output quantity

#### Scenario: Produce an untainted batch
- **WHEN** ordinary cooking succeeds without marking the produced food tainted
- **THEN** this feature does not add a second chance to taint the batch

### Requirement: Transfers select portions fairly and conserve taint
Splitting or partially transferring a stack SHALL select its moved units fairly without replacement.
Each remaining unit SHALL have the same chance of being selected; previously selected units SHALL
not be selected again. The moved and remaining portions SHALL together retain exactly the original
food and taint, including causes. Existing taint in a receiving stack SHALL be retained and combined
with only the taint actually transferred. Moving a whole stack SHALL move all its taint unchanged.

#### Scenario: Partially fill a storage stack
- **WHEN** two meals from a five-meal stack containing two tainted meals fill an eight-meal clean stack
- **THEN** the resulting stacks contain ten and three meals and still contain two tainted meals in total
- **AND** the recipient receives only the zero, one or two tainted meals actually selected from the donor

#### Scenario: Split an entirely tainted stack
- **WHEN** two meals are split from three tainted meals
- **THEN** the piece contains two tainted meals and the remainder contains one

#### Scenario: Move the entire stack
- **WHEN** a pawn picks up or transfers all meals in a stack
- **THEN** its tainted quantity and causes move intact without another random allocation or duplicate record

#### Scenario: Merge into a stack that is already tainted
- **WHEN** a recipient with existing tainted meals absorbs a selected donor portion
- **THEN** the recipient retains its original taint and gains only the selected portion's taint

### Requirement: Failed or empty transfers do not change food state
An attempt that moves no food SHALL not change either stack's taint, causes or future allocation.
Actual partial transfers SHALL be accounted for even when food remains in the donor. Repeated
notifications SHALL not apply a transfer twice. If an operation fails after moving some food, the
accounting SHALL follow the physical result without restoring food that no longer exists or changing
an uninvolved stack. No error recovery SHALL silently clamp impossible quantities and claim success.

#### Scenario: Haul toward a full stack
- **WHEN** a stacking attempt can transfer zero meals
- **THEN** both stacks retain their prior food, taint and causes unchanged

#### Scenario: Space disappears before placement
- **WHEN** a planned transfer loses space and the actual action moves fewer meals or none
- **THEN** accounting follows only the actual transferred quantity and the remaining food stays intact

#### Scenario: The donor remains after a successful partial transfer
- **WHEN** a valid partial transfer leaves meals in the donor stack
- **THEN** the transfer is counted once and is not treated as a complete failure merely because the donor still exists

### Requirement: Eating uses the portion actually consumed
Cooking-taint effects SHALL depend on the actual eaten portion, not on taint left behind in the
source stack. Consumed units and their taint SHALL cease to be available for later transfers or meals.
A canceled eating action that consumes nothing SHALL consume no taint and cause no illness through
this feature. The rule SHALL cover eating from a stack, a separated serving, carried/inventory food
and supported caravan/provider ingestion paths without changing nutrition or the amount eaten.

#### Scenario: Eat the clean portion of a mixed stack
- **WHEN** the portion actually eaten contains no cooking-tainted unit but the remainder does
- **THEN** the remaining taint does not poison that eating action through this feature
- **AND** it remains with the uneaten meals

#### Scenario: Eat the tainted portion
- **WHEN** the portion actually eaten contains the stack's last tainted unit
- **THEN** that exposure can cause ordinary food poisoning and the remaining food has no cooking taint

#### Scenario: Finish the stack
- **WHEN** the eating action consumes the final physical units
- **THEN** the exposure is handled once even though the food object disappears, and no ghost taint remains

### Requirement: Native illness modifiers and independent risks remain authoritative
An eating action containing at least one cooking-tainted unit SHALL make one ordinary cooking-taint
illness check for that action, using the eater's native difficulty, health, gene and immunity modifiers.
Eating several tainted units together SHALL not multiply that chance by the number left in the stack
or create repeated illness notifications for the same exposure. An untainted consumed portion SHALL
have no cooking-taint exposure. Independent dangerous-food, trait, provider sanitation or other
supported risks SHALL retain their own rules; untainted SHALL not be presented as universally safe.

Native illness severity/progression, existing-condition handling and ordinary notification behavior
SHALL remain unchanged. This feature SHALL introduce no new illness duration, chance multiplier or
ordinary setting.

This finite-exposure rule deliberately changes some mixed-stack illness probabilities. An increase
in the native illness multiplier SHALL not make a selected untainted unit cooking-tainted. Eating
more units together can increase the chance of including taint, but still uses one check. Preserving
the native illness calculation and modifiers SHALL not be described as preserving every probability
from the former averaged-stack model.

#### Scenario: Eater has full immunity
- **WHEN** an eater whose native modifiers prevent food poisoning consumes tainted food
- **THEN** this feature does not bypass that immunity, and the food is still consumed normally

#### Scenario: Consume several tainted units at once
- **WHEN** one supported eating action consumes more than one tainted unit
- **THEN** the action uses one native-modified cooking-taint check and retires every consumed unit's taint

#### Scenario: Untainted food has a separate risk
- **WHEN** untainted food is inherently dangerous or a supported provider adds another risk
- **THEN** that separate risk remains possible without inventing cooking-tainted units

### Requirement: Causes follow the tainted portion
Known cooking-taint causes SHALL move with their associated tainted quantities. A merge or clean
transfer SHALL not overwrite them with an unrelated stack's cause. If an eating action's cooking
taint causes illness, the reported cooking cause SHALL come from an actually consumed tainted unit.
For several consumed causes, each consumed tainted unit SHALL have equal chance to supply that
cause. An unknown cause SHALL remain unknown rather than inventing a cook or dirty room. Supported
providers that attribute another actual risk SHALL retain their own established cause rules.

#### Scenario: Two cooking causes share a stack
- **WHEN** meals tainted by a dirty room and meals tainted by cooking skill are merged and later split
- **THEN** each portion retains the causes of its selected tainted meals
- **AND** a later illness does not cite a cause that was only in the uneaten remainder

#### Scenario: Move only clean food from a mixed stack
- **WHEN** a selected portion contains only untainted units
- **THEN** the donor's taint cause does not become a cooking exposure in the recipient

### Requirement: Physical removal retires only the removed food's taint
Ordinary supported destruction, consumption as ingredients, loss or disposal SHALL remove the
corresponding physical units and their taint without transferring it to unrelated food or recreating
destroyed goods. An unlabelled partial removal SHALL use the same fair portion selection as a split.
Changing a food into another product SHALL follow that recipe/provider's ordinary contamination
rules; this feature SHALL not add an unspecified ingredient-to-product transmission rule.

#### Scenario: Part of a mixed stack is destroyed
- **WHEN** an ordinary supported event removes some physical units from a mixed stack
- **THEN** only the selected removed units' taint is retired and the remaining count fits the remaining food

#### Scenario: Use food as an ingredient
- **WHEN** a recipe consumes food units to make another product
- **THEN** those source units no longer retain live taint records, and the resulting product follows its ordinary production rules

### Requirement: Known native food receives a stable one-time assignment
Existing food known to use native probability-only taint SHALL be assigned finite tainted/untainted
units once when admitted to this feature. Each unit SHALL independently use its previous native
taint chance for this initial assignment; it SHALL not be rounded to a fixed average count. Native
zero chance SHALL create no tainted units and certainty SHALL taint every unit. This assignment
SHALL preserve the prior per-unit probability without claiming to recover a historical count the
native game never recorded. After assignment, ordinary transfer rules SHALL conserve that population.

The assignment SHALL be stable across reloading the same pre-assignment save and persisted with
owned state, not rerolled by selecting, hauling or repeatedly loading the food. Values or saved
provenance that could represent an original provider's count SHALL not be silently treated as native
probabilities. Ambiguous, invalid or unsupported source formats SHALL retain an explicit unsupported
migration state and explanation; no guessed conversion or safe source-mod removal is promised.

#### Scenario: Load native certainty
- **WHEN** a known-native save contains five meals with certain cooking taint and no owned assignment
- **THEN** all five become tainted units, not one tainted unit and four clean ones

#### Scenario: Load native fractional chance
- **WHEN** a known-native stack has a fractional cooking-taint chance
- **THEN** each unit receives its one-time assignment at that prior chance and subsequent movement conserves the assigned total
- **AND** reloading the same pre-assignment save does not reroll the assignment

#### Scenario: Source format is ambiguous
- **WHEN** saved food might use the original mod's count format and that format cannot be established safely
- **THEN** automatic normalization is withheld with an explanation rather than guessing whether one means one meal or certainty

### Requirement: Saves preserve owned populations and conversion boundaries
Owned quantities, causes and completed assignments SHALL survive saves, map/inventory/caravan
transfers and reloads without replaying transfers or eating. One save SHALL not inherit another's
food state. Save normalization or a supported format change SHALL leave the input save untouched
until a complete result is verified; before replacing an existing save it SHALL preserve a backup,
commit consistently, and restore the previous complete state if publication fails.

If required integration becomes unavailable, the feature SHALL clearly report that finite-count
enforcement is unavailable and yield to admitted native/provider handling. A valid native probability
fallback SHALL remain a probability rather than a tainted-meal count. Old owned records SHALL not
be reused as current after untracked food operations. Re-enabling requires reconciliation of the
current trustworthy food state; stale records SHALL not resurrect food or taint. Unknown provenance
SHALL remain unsupported rather than being silently normalized as a new native stack.

#### Scenario: Save between two partial transfers
- **WHEN** the game is saved after one partial transfer and loaded before the next
- **THEN** both stacks retain exactly the first transfer's result and the next action cannot replay it

#### Scenario: Save publication fails
- **WHEN** publishing a normalized or converted save fails
- **THEN** the prior complete save/backup remains usable, without a partially updated food population

#### Scenario: Resume after a period of unavailable tracking
- **WHEN** food changed while finite-count handling was unavailable
- **THEN** stale saved counts are not reinstated over that food, and unsupported reconciliation is explained

### Requirement: Providers have one admitted owner for the affected behavior
With original Food Poisoning Stack Fix active, the owned overlapping enhancement SHALL remain
inactive with a bounded startup warning, leaving source code and configuration untouched. Switching
existing food between providers SHALL not be advertised as an automatic migration. Other compatible
patches SHALL not be rejected merely for existing; changed required behavior SHALL withhold the
affected atomic path rather than apply only transfer accounting without correct creation/eating/save handling.

Supported meal providers SHALL receive the actual consumed cooking-taint exposure while retaining
their own additional risk, serving identity and attribution. In particular, an Immersive Chefs
integration SHALL not substitute a stack count for its probability input, overwrite its serving
state or apply its custom risks twice. If that ownership cannot be established, the affected provider
path SHALL remain under provider control with an explanation; unrelated supported food SHALL remain
eligible. Neither repository mod SHALL acquire a runtime dependency on the other.

#### Scenario: Original provider co-installed
- **WHEN** the original source provider is active at startup
- **THEN** the log identifies the withheld owned feature and no second accounting system rewrites its food state

#### Scenario: Supported meal provider combines risks
- **WHEN** a supported provider evaluates an actually consumed serving
- **THEN** its cooking-taint input reflects that consumed portion and its own additional risks are applied once

#### Scenario: A required ingestion seam changed
- **WHEN** the game can still merge food but the required consumed-portion behavior cannot be integrated safely
- **THEN** the affected enhancement remains inactive with a warning and admitted fallback rather than claiming correct stacking alone

### Requirement: Mixed ownership does not guess an unsupported population
Unassigned known-native food SHALL receive its stable initial assignment before an actual transfer
with tracked food so both participate in ordinary finite accounting.
If one operand instead has ambiguous taint format or is controlled by an unsupported provider,
otherwise valid stacking SHALL remain available through that operation's admitted native/provider
handling. When food actually moves, both participating stacks SHALL relinquish owned finite-count
tracking, including any donor remainder. Their surviving/results' unsupported provenance SHALL be
retained through later saves, splits and merges until explicit reconciliation establishes a supported
state. Old owned counts SHALL not be silently reattached to either result.

Before the unowned mutation begins, a tracked operand SHALL expose its valid pre-transfer native
probability, never its integer tainted count. The unknown operand SHALL not be guessed, converted or
rewritten by this feature. The resulting food SHALL retain the native/provider operation's actual
outcome; no finite-count conservation claim SHALL be made across that unsupported boundary. The
affected-route explanation SHALL identify the loss of tracking without exposing latent quantities
in ordinary play. If no food moves, the attempt SHALL not itself relinquish otherwise valid owned
tracking or change its population. Unsupported provenance SHALL not be cleared merely because a
later probability value falls between zero and certainty.

#### Scenario: Tracked food meets unassigned known-native food
- **WHEN** a valid merge includes probability-only food whose native format is established
- **THEN** that food receives its stable assignment and both portions follow finite conservation

#### Scenario: Unknown donor partly fills a tracked recipient
- **WHEN** some food moves from an ambiguous donor into a tracked recipient
- **THEN** ordinary physical transfer remains possible, the recipient and donor remainder remain
  explicitly unsupported for finite tracking, and the old recipient count is not reused

#### Scenario: Tracked donor partly fills an unsupported recipient
- **WHEN** only part of a tracked donor moves into an unsupported recipient
- **THEN** the recipient and surviving donor both relinquish finite tracking under the admitted
  native/provider operation, without changing how many units physically moved

#### Scenario: Full merge in either direction
- **WHEN** a tracked and unsupported stack fully merge, regardless of which is the recipient
- **THEN** the resulting stack remains unsupported and a destroyed donor leaves no reusable owned record

#### Scenario: Boundary transfer moves nothing
- **WHEN** a tracked and unsupported stack attempt to merge but no food moves
- **THEN** that attempt alone does not retire the tracked stack's population or reinterpret the unknown stack

### Requirement: Hidden taint is visible only through explicit developer diagnosis
Ordinary inspect panes, owned search/sort/filter controls and gameplay gizmos SHALL not reveal
latent taint, exact counts or hidden causes. Existing publicly known food-type risks and actual
illness notifications SHALL remain available. A read-only **Food taint** action in the top debug
toolbar SHALL inspect selected supported food only when developer mode is on. It SHALL show the
physical quantity, tracked tainted quantity/causes and tracking/format availability without claiming
that zero cooking taint excludes all other risks. It SHALL not change food, allocations or random
outcomes. Turning developer mode off SHALL hide/close the diagnostic and reject its stale callbacks.

No new bottom tab, ordinary setting, global/save preference, bottom-right toggle or source-style
debug text in the normal inspect pane SHALL be added. Area/pawn/food state is not a setting.

#### Scenario: Select a tainted stack in ordinary play
- **WHEN** the player selects or searches supported food with developer mode off
- **THEN** this feature does not identify tainted units or expose their hidden cause

#### Scenario: Inspect through the top debug toolbar
- **WHEN** developer mode is on and the player invokes Food taint on selected food
- **THEN** a read-only diagnostic shows its actual tracking state and quantities without changing the next transfer or illness roll

#### Scenario: Disable developer mode with the diagnostic open
- **WHEN** developer mode is turned off
- **THEN** the hidden-state diagnostic closes and cannot be reopened through a stale control

### Requirement: Owned hauling and delivery preserve the same food state
S021 batch hauling, S027 courtesy delivery and future owned meal-carrying paths SHALL use the same
physical portion accounting. An extra pickup, detour, inventory holder, partial placement or return
SHALL not create another tainted population or grant more food/carrying capacity. Current stack and
holder state SHALL remain authoritative; shared search data SHALL not expose latent taint or cache
another independently maintained food inventory.

#### Scenario: Batch-haul and partially unload meals
- **WHEN** an owned hauler collects mixed food and unloads it into several valid stores
- **THEN** the physical portions conserve their original taint and all normal capacity/storage rules remain

#### Scenario: A visitor delivers purchased meals
- **WHEN** an S027 courier delivers a received food portion and leaves an undelivered remainder
- **THEN** taint follows those actual portions without changing receipt quantities, payment or the finite delivery entitlement
