# Smarter work priorities

Owner **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`; target **1.7 beta**.
Origin replacement S058/P1-52, SmarterDoctors (Continued). Observable draft only; nothing implemented.
The user selected imminent danger first when choosing a new patient. Remaining native integration
and playability verification are pending.

## ADDED Requirements

### Requirement: Improve target choice within existing automatic work

SW01. When a worker naturally chooses its next automatic task, the feature SHALL order eligible
targets within that kind of work. It SHALL preserve enabled work, work-type priorities, schedules,
explicit player orders, existing queued jobs, ordinary interruptions and access/skill/permission
rules. A better-ranked target SHALL NOT itself interrupt a running job or authorize forbidden work.
After all specified priorities tie, prefer the nearer target by ordinary horizontal map distance;
if distance also ties, retain ordinary game selection. Eligibility SHALL be checked for the current
worker and action, including changes since a previous choice.

#### Scenario: Assigned work still governs
- **WHEN** a pawn has doctoring disabled or another work type ahead of it
- **THEN** medical target ordering does not enable doctoring or move it ahead of the assigned work.

#### Scenario: Explicit choice and current job survive
- **WHEN** the player orders a permitted lower-ranked target or a worker is already doing a valid job
- **THEN** ranking does not replace that order or job merely because a preferred target exists.

#### Scenario: Preferred target is unavailable
- **WHEN** the preferred patient, animal, plant or frame is forbidden, inaccessible or unavailable to this worker
- **THEN** it is not made eligible and the worker can select another ordinary eligible target.

#### Scenario: Equal priority uses distance
- **WHEN** two eligible targets tie on every applicable priority but are different distances away
- **THEN** the nearer target is selected, without changing either target's future eligibility.

### Requirement: Shared medical urgency precedes routine patient classes

SW02. New automatic medical patient choices SHALL put currently life-threatening needs ahead of
routine care. A patient qualifies when the game reports a current life-threatening condition or a
reliable finite predicted death within the ordinary urgent-care window (inspected baseline:
less than18 game hours). The candidate action must be an actually permitted response to that need.
An unrelated condition, nonfatal disease, missing estimate or a small immunity gap alone does not
prove imminent death. Preserve native/provider restrictions and never manufacture a death forecast.

Within the urgent group, patients with comparable reliable finite death predictions rank by the
shorter remaining time. Urgent patients without such a prediction follow those with a prediction;
remaining ties use the ordinary family rules below. This explicit estimate policy does not claim
perfect survival prediction. Recheck current facts when choosing work; do not interrupt protected
ongoing care merely because another patient becomes more urgent.

Within equal urgency, tending retains these ties: visible, not-fully-immune immunity-building
disease first (smallest immunity-minus-severity gap across that patient's qualifying diseases),
then bleeding (shorter reliable predicted death), then remaining tending, then distance/native ties.
A nonfatal disease therefore cannot outrank an imminently bleeding patient. Full immunity and hidden
conditions remove that disease contribution; ordinary care/medicine and actual tended effects stay.
S134 uses the same urgency once across rescue and tending, then rescue before tending for equal
urgency. S058 alone still orders within its selected work family rather than dispatching new work.

#### Scenario: Imminent bleeding before routine disease
- **WHEN** one eligible patient has a nonfatal immunity-building disease and another is imminently dying from blood loss
- **THEN** the bleeding patient ranks first.

#### Scenario: Comparable fatal predictions
- **WHEN** two eligible urgent patients have reliable comparable predicted death in100 and200 ticks
- **THEN** the100-tick patient ranks first, without losing the difference to score rounding.

#### Scenario: Routine disease comparison
- **WHEN** two nonurgent disease patients have worst immunity-minus-severity gaps of-0.20 and+0.10
- **THEN** the-0.20 patient ranks first; its gap does not by itself assert a fatal deadline.

#### Scenario: Urgent condition without a reliable clock
- **WHEN** the game identifies a life-threatening condition but supplies no comparable finite death prediction
- **THEN** that patient remains ahead of routine care, after urgent patients with comparable finite deadlines, without an invented forecast.

#### Scenario: No current care permission
- **WHEN** a threatened patient cannot receive the candidate action under current permissions or actual eligibility
- **THEN** urgency does not authorize it, create medicine or remove another valid patient from consideration.

### Requirement: Feed starving patients in malnutrition order

SW03. The feature SHALL rank eligible patients with an empty food need and malnutrition by greater
malnutrition severity first within the same automatic feeding work. Patients without both conditions
SHALL receive no additional feeding priority. Equal severity uses distance. Ordinary species, age,
care, food-policy, ingestion, supply and reservation rules SHALL remain; the feature SHALL NOT rank
food types, force overfeeding or create a food need for a pawn without one.

#### Scenario: Greater malnutrition is fed first
- **WHEN** two eligible patients have empty food needs and malnutrition severity 0.30 and 0.10
- **THEN** the 0.30 patient is selected first even when farther away.

#### Scenario: Residual malnutrition does not qualify by itself
- **WHEN** a patient's food need is above zero but malnutrition remains
- **THEN** that patient receives no starvation-priority bonus under this rule.

#### Scenario: No suitable meal or feeding eligibility
- **WHEN** a starving patient cannot be fed through the ordinary permitted feeding action
- **THEN** ranking does not bypass the restriction and another eligible patient can be considered.

### Requirement: Preserve tameness and concentrate requested training

SW04. For each eligible animal, the game SHALL still choose its next requested and currently
trainable lesson with existing prerequisites. Compare animals by that lesson in this order:
**tameness, hauling, rescue, attack, guard, comfort, other training**. Missing training types SHALL
add no requirement or new lesson. Within tameness, fewer retained training steps SHALL rank first;
within another category, more completed steps toward the current lesson SHALL rank first. Category
order SHALL remain decisive even with unusually large modded step counts. Equal steps use distance.

The feature SHALL NOT mark unwanted training as wanted, finish training instantly, change skill/food
costs, bypass training intervals or guarantee every animal will receive equal attention. Unknown
trainable lessons remain usable in the final category. A worker's ordinary preparation, including
collecting food before training, remains possible and does not promise an immediate lesson.

#### Scenario: Lost tameness takes precedence
- **WHEN** eligible animals need tameness maintenance and another needs hauling training
- **THEN** tameness comes first, with the animal retaining fewer tameness steps first.

#### Scenario: Finish some hauling training
- **WHEN** two eligible animals next need hauling with six and two completed steps
- **THEN** the six-step animal ranks first without completing the lesson for free.

#### Scenario: Prerequisites remain native
- **WHEN** hauling is requested but the animal's next available lesson is guard
- **THEN** it is compared as needing guard and is not treated as already eligible for a hauling lesson.

#### Scenario: Unknown training cannot overflow categories
- **WHEN** an optional training type has hundreds of completed steps and another animal needs tameness
- **THEN** tameness still ranks first and the unknown lesson remains available in the final category.

#### Scenario: Training request changes
- **WHEN** an animal has no remaining requested trainable lesson or is currently ineligible
- **THEN** it is not given a training job merely because it ranked highly earlier.

### Requirement: Prefer fertile growing locations with a hay tie-break

SW05. Within automatic sowing and, separately, grower harvesting, eligible growing locations SHALL
rank by their **current effective fertility**, greatest first. For equal fertility, non-haygrass
SHALL precede haygrass, then distance decides. Sowing uses the requested crop; harvesting uses the
plant being harvested. This SHALL apply to native growing zones and eligible growing buildings,
including their real fertility and pollution effects. Crop, season, maturity, cutting permission,
worker ability and supply requirements remain unchanged.

The rule chooses the location around which a new harvesting job is formed. The game's ordinary
local harvest batch and its internal order SHALL remain; the feature does not promise to sort every
plant in that batch or interrupt a batch when another field becomes preferable.

#### Scenario: Fertility outranks the hay distinction
- **WHEN** eligible haygrass has fertility 1.40 and another crop has fertility 0.70
- **THEN** the haygrass location ranks first for the applicable sowing or harvesting choice.

#### Scenario: Equal fertility prefers other crops
- **WHEN** haygrass and another eligible crop have equal fertility
- **THEN** the other crop's location ranks first even when farther away.

#### Scenario: Growing building and changed terrain
- **WHEN** eligible building-grown and zone-grown crops compete, or pollution changes fertility
- **THEN** the next choice uses their actual current fertility without a zone-only exception.

#### Scenario: Mixed local harvest batch
- **WHEN** a preferred harvesting location produces an ordinary local batch containing nearby plants
- **THEN** the worker can finish that native batch in its ordinary order without repeatedly switching fields.

### Requirement: Cut haygrass after other eligible plants

SW06. Within automatic plant-cutting work, eligible non-haygrass plants SHALL rank ahead of haygrass,
then distance SHALL decide. This SHALL NOT add cutting designations, cancel protected plants,
convert cutting to harvesting or reprioritize other work types. Only actual haygrass receives this
classification; similarly named or related optional plants do not automatically inherit it.

#### Scenario: Cut other designated plants first
- **WHEN** a worker can cut a nearby designated haygrass plant and a farther designated non-hay plant
- **THEN** the non-hay plant ranks first.

#### Scenario: Hay is the remaining valid cutting task
- **WHEN** only haygrass is eligible for cutting
- **THEN** it can still be cut normally.

#### Scenario: No cutting permission
- **WHEN** a plant is protected or has no applicable permission for this cutting action
- **THEN** its class does not create permission to cut it.

### Requirement: Finish structures before floors and spike traps

SW07. Within automatic work on construction frames ready to be finished, ordinary structures SHALL
rank first, terrain construction such as floors second, and the ordinary spike trap last. Equal
categories use enabled Smarter Construction local constrained-frame preference before distance;
without that preference they use distance directly. That local rule SHALL NOT cross these categories
or override current enclosure/worker eligibility. This SHALL NOT reorder material delivery, repairs, deconstruction, mining
or other work types, change construction cost/time/quality or waive skills, access or blocking work.
Other traps SHALL NOT be assigned the spike-trap category solely because they are traps.

#### Scenario: Build the usable structure first
- **WHEN** a bed frame, floor frame and ordinary spike-trap frame are all eligible and supplied
- **THEN** the bed frame ranks ahead of the floor, which ranks ahead of the spike trap.

#### Scenario: High-ranked blueprint lacks materials
- **WHEN** a structure is only an unsupplied blueprint while a floor frame can be finished
- **THEN** this ranking does not make the blueprint finishable or prevent ordinary floor work.

#### Scenario: Player prioritizes a trap
- **WHEN** the player explicitly orders an eligible spike trap to be finished
- **THEN** the automatic category order does not replace that command.

#### Scenario: Construction access refines a category
- **WHEN** two eligible structure frames share this category and the enabled construction-access rule prefers a nearby constrained blocking frame
- **THEN** that local preference precedes distance within the category, while a floor or spike trap cannot jump ahead merely through its neighbor score.

### Requirement: Rescue eligible colonists before animals

SW08. Among targets already eligible for the same automatic rescue work, shared SW02 urgency SHALL rank
first. Within equal urgency, colonists rank first, animals and wild humans second, and other eligible
pawns last; ties use distance. The game
continues to determine colonist status, rescue eligibility and suitable beds. Native automatic and
explicit rescue permissions, threat checks, reservations, childcare, caravan-taking, slaughter and
other protected activities SHALL remain intact.

The feature SHALL NOT automatically broaden rescue to other factions or turn a rejected rescue
into an allowed one. The source's undocumented eligibility override is deliberately omitted; a
player can still use the game's permitted explicit rescue workflow.

#### Scenario: Colonist before animal
- **WHEN** a colonist and animal have equal urgency and are both eligible for the worker's automatic rescue
- **THEN** the colonist ranks first even when farther away.

#### Scenario: Ineligible colonist does not block valid rescue
- **WHEN** a colonist has no suitable accessible bed or is otherwise ineligible while an animal is eligible
- **THEN** ranking does not authorize the colonist's rescue or block the valid animal task.

#### Scenario: Outside-faction casualty
- **WHEN** a downed outsider is excluded from automatic rescue by the game
- **THEN** this feature does not automatically rescue them; a permitted explicit rescue remains available.

#### Scenario: A later rule rejects rescue
- **WHEN** the current game or a supported integration rejects rescue for a protected activity
- **THEN** ranking does not reverse that rejection.

### Requirement: Compose work decisions once and respect other ordering

SW09. These rules SHALL contribute once when their automatic work actually chooses eligible targets.
They SHALL NOT schedule drug responses, operations, self-tending, patient visits, emergency actions
or housekeeping merely to create a target. Existing care-before-cleanup behavior remains; food and
medicine selection, physical quantities and animal training requests remain owned by their ordinary
mechanics. Later owned features affecting these choices SHALL specify one combined decision before
implementation rather than applying repeated reprioritization.

If another active provider supplies its own non-neutral target ordering for a work selection and no
explicitly supported combination exists, S058 SHALL leave that entire selection's ordering to the
provider, including its ties, rather than blend incomparable priorities or reset them. Unrelated or
compatible changes SHALL NOT disable the feature merely because they affect the same game method.

#### Scenario: Continuing patient care
- **WHEN** existing care rules call for further tending before cleanup
- **THEN** housekeeping remains postponed and S058 orders only the actual eligible tending choice once.

#### Scenario: Treatment orders remain separate
- **WHEN** a drug response, explicit operation or self-care action is pending
- **THEN** target ranking does not create duplicate treatment, consume extra medicine or silently replace that action.

#### Scenario: Provider owns this ordering
- **WHEN** an unsupported external rule supplies non-neutral priorities for this automatic selection
- **THEN** the provider's complete ordering remains, without mixing some S058 scores into it.

#### Scenario: Compatible unrelated change
- **WHEN** another mod changes an unrelated aspect of the same work path while required behavior remains valid
- **THEN** the supported target ordering can remain active.

### Requirement: Keep ordinary controls and recover safely when unavailable

SW10. The feature SHALL use existing work, health, training, growing and construction controls,
without a new settings window, bottom tab, debug control or view toggle. Entity-specific choices
remain entity state. No extra saved priority state or source settings migration is selected. After
loading, changing maps or changing relevant conditions, the next ordinary choice SHALL use current
facts; neither loading nor enabling the feature cancels valid jobs or performs work immediately.

At startup, each enabled integration SHALL be checked for its documented requirements. If a work
family's required ordering cannot be supported safely, that entire family SHALL retain the game's
remaining behavior while independent supported families continue. Log a bounded warning identifying
the failed integration, unmet requirement, unavailable behavior and fallback. Unsupported current
candidate data SHALL fall back for that selection without discarding patients or inventing priorities.
If original SmarterDoctors or its continued provider is active, all owned S058 overlap SHALL remain
inactive with an explanatory startup warning; do not remove or reconfigure the external mod.

#### Scenario: No new menu is required
- **WHEN** the player manages work, training, patients or crops
- **THEN** their existing contextual controls remain the entry points and no source bottom tab is added.

#### Scenario: Load and revisit a changed map
- **WHEN** a save loads or a worker returns to a map where health, fertility or training changed
- **THEN** future selections use current facts without restoring obsolete rankings or granting completed work.

#### Scenario: One integration is unavailable
- **WHEN** medical ordering fails its compatibility check but construction ordering is supported
- **THEN** tending retains the remaining game's behavior, construction ordering can work, and startup logs explain the medical fallback.

#### Scenario: Unreadable current candidate information
- **WHEN** a selected work comparison cannot safely interpret a candidate's required information
- **THEN** that selection retains ordinary ordering instead of hiding the candidate, throwing repeatedly or comparing invented values.

#### Scenario: Source provider is active
- **WHEN** either original or continued SmarterDoctors is active alongside megamod
- **THEN** owned S058 rules are inactive, the external provider is untouched and startup logs identify the overlap and fallback.
