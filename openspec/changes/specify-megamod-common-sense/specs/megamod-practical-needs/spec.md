# Practical needs and recreation

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
S024 Common Sense replacement; scheduled potential mood is selected for implementation. Source-derived values and proposed corrections
remain recorded under CS-Q1–4; ordinary costs, policy and actual job execution remain required.

## ADDED Requirements

### Requirement: Outdoor deprivation influences eligible recreation
With outdoor recreation preference enabled, a pawn below 40% Outdoors SHALL prefer a supported enjoyable recreation opportunity at a psychologically outdoor location when one is currently permitted and reachable.
Retain ordinary capacities, boredom, medical-rest restrictions, weather suitability and provider
eligibility. Consider actual supported outdoor locations even when an indoor copy exists. If none
is eligible, use ordinary recreation selection without an endless search or unsafe excursion.
This preference does not override the player's explicitly chosen recreation, which remains under S002.

#### Scenario: Indoor and outdoor copies
- **WHEN** an outdoor-deprived pawn can legally use outdoor and indoor versions of the same activity
- **THEN** the supported outdoor location is preferred without ignoring current access or weather restrictions.

#### Scenario: Unsafe outdoors
- **WHEN** no enjoyable safe outdoor recreation is available
- **THEN** the pawn can choose ordinary indoor recreation rather than repeatedly trying the unsafe outdoor target.

### Requirement: Complete ordinary recreation before repeatedly seeking more
With recreation completion enabled, pawns SHALL seek permitted recreation toward 95% during Recreation time and while awake during Sleep time, then stop preferring another recreation bout until recreation falls below 80%.
Reaching 95% records completion; falling below 80% clears it. That state persists across save/load and
does not depend solely on catching a particular job boundary. Existing urgent sleep, hunger, danger,
medical needs, lord/duty restrictions and early-game recreation restrictions retain precedence.
Anything and Work schedules retain their native recreation rules. A forced or explicitly directed
activity is not cancelled merely because this automatic preference is topped off. Actual activities
still take time and grant their ordinary joy/tolerance, not free need replenishment.

#### Scenario: Awake in a Sleep block
- **WHEN** the pawn is not sufficiently tired to sleep, has not completed recreation and has an eligible activity
- **THEN** it can use that time for recreation while remaining available for urgent needs.

#### Scenario: Completed bout and reload
- **WHEN** recreation reaches 95%, the game is saved/reloaded and recreation is still 85%
- **THEN** the automatic preference remains topped off until the level falls below 80%.

### Requirement: A social drink is consumed once at a useful time
With social-drink economy enabled, ordinary social relaxation with a selected drink SHALL account for that drink's expected recreation gain after tolerance and finish the social phase early enough to reduce avoidable overflow where the normal activity permits.
The proposed target is 99% recreation minus expected drink gain. Retain at least the larger of the
drink's normal ingestion time and 240 ticks of social activity unless normal duration, interruption
or another native ending condition ends it first. This minimum prevents instantaneous consumption;
it does not guarantee zero wasted joy when the pawn is already nearly full. Without a drink, retain
ordinary social timing. Gatherings keep their own ending rules. The drink must actually be obtained
and ingested once; no advance joy, extra dose, duplicate ingestion or forced policy bypass occurs.
Interruption preserves whatever portion remains through the shared inventory rules.

#### Scenario: Drink can fill the remaining recreation need
- **WHEN** normal social gain plus the selected permitted drink would fill recreation
- **THEN** the pawn can move to the normal ingestion step earlier after the minimum social interval, consuming exactly one selected dose.

#### Scenario: Gathering or interruption
- **WHEN** the pawn is attending a gathering or is drafted before ingestion finishes
- **THEN** gathering/interrupt rules retain authority and the drink's effects occur only for actual completed ingestion.

### Requirement: End optional meditation when its needs are satisfied
With meditation economy enabled, nonscheduled meditation SHALL end when recreation is absent, at least 98% or bored of that meditation kind, and psyfocus is not needed or has reached100%.
The ordinary Meditation timetable assignment is exempt. Missing psyfocus capability does not invent
it; missing or unsupported provider semantics leave the ordinary job available. The feature does not
grant psyfocus, change its maximum or interrupt meditation still needed for either purpose.

#### Scenario: Both benefits are satisfied
- **WHEN** nonscheduled meditation has 98% recreation and full psyfocus
- **THEN** the pawn ends that optional meditation and can select other ordinary work.

#### Scenario: Psyfocus still needed
- **WHEN** recreation is full but the pawn still needs psyfocus, or is in assigned Meditation time
- **THEN** this economy rule alone does not stop the meditation.

### Requirement: Potential mood adds a restriction to scheduled drugs
With potential-mood checking enabled, scheduled drug use SHALL require both current mood and potential mood to be below the drug policy's mood threshold whenever that threshold is less than 100%.
All other ordinary scheduled-use conditions remain, including timing, policy permission and actual
ability to take the drug. No mood need means no extra mood restriction. The feature does not change
the policy or extend scheduled permission to joy, medical or forced use. S029's future drug-policy
corrections compose in the same decision without competing internal patches.
This applies to actual scheduled selections both on-map and in caravans, including drugs without
an applicable tolerance effect. Comparisons SHALL use unrounded native percentages and be strict:
equality rejects. A threshold of 100% adds no mood restriction. The native potential-mood calculation
and its normal cache/update behavior SHALL remain authoritative; no substitute formula, forced cache
refresh or artificial mood gain is introduced.

An invalid relevant threshold or mood percentage (nonfinite or outside 0–100%), failed native read,
or stale current pawn/policy/row context SHALL make only this extra mood restriction unavailable.
Report a bounded localized explanation at most once per Game for a failed applicable reading; do
not substitute zero or claim the restriction succeeded. Missing mood or an unrestricted threshold
need not evaluate potential mood. Supported S138 tolerance SHALL remain independently effective,
and disabling or losing S138 alone SHALL not disable healthy mood checking. Changes affect the next
native selection, without cancelling, reclassifying or adding a final veto to an executing dose;
dose quantity, interval, stock target and consumption history remain native.

#### Scenario: Mood is already recovering
- **WHEN** current mood is low but potential mood already reaches the policy threshold
- **THEN** this scheduled dose is not taken solely because the displayed current mood is still low.

#### Scenario: Other policy condition rejects use
- **WHEN** both mood values are below the threshold but the interval or another policy rule rejects the drug
- **THEN** the pawn does not take it through this preference.

#### Scenario: Both mood values permit a scheduled dose
- **WHEN** potential-mood checking is enabled, the native mood threshold is below 100%, both current and potential mood are strictly below it, and all other scheduled conditions permit use
- **THEN** the pawn can consume the ordinary physical dose with its native effects and history
- **AND** a 100% threshold bypass alone does not establish this relevant-threshold behavior.

#### Scenario: Either added restriction independently withholds a fresh dose
- **WHEN** native scheduling otherwise permits a dose with empty history and only one of potential mood or tolerance rejects it
- **THEN** that enabled available restriction withholds the dose even if the other feature is disabled or unavailable
- **AND** disabling both removes their added restrictions without granting permission over another native rejection.

#### Scenario: Caravan mood is already recovering
- **WHEN** actual world ticks consider an otherwise permitted caravan dose whose current mood is below the threshold but potential mood reaches it
- **THEN** the dose remains physical and no ingestion effects or history are granted
- **AND** a separate eligible subject below both mood thresholds can consume its ordinary physical dose, preserving the native interval thereafter.

#### Scenario: A local reading fails while the other restriction is healthy
- **WHEN** an applicable mood or tolerance reader throws while the shared scheduled boundary and the other consumer remain valid
- **THEN** only the failed reading becomes unavailable for that selection, with its bounded explanation
- **AND** the healthy consumer can still withhold a native-permitted dose without losing its admitted boundary.

#### Scenario: Tolerance layout is unavailable
- **WHEN** the tolerance editor cannot fit but scheduled potential-mood checking remains supported and enabled
- **THEN** the tolerance feature adds no hidden restriction while potential mood continues to govern subsequent scheduled selections
- **AND** restoring a supported layout can restore retained tolerance choices without resetting dose history or mood.

### Requirement: Patient recreation deliveries respect the recipient
With patient treats enabled, a valid sick-pawn visit SHALL be able to deliver allowed enjoyable food or a recreational drug to the patient through ordinary feeding, or fall back to an ordinary visit when no suitable treat is available.
Validate the patient's current diet, drug joy permission, ideology, ingestion ability and boredom,
and the visitor's actual access, capacity and feeding ability. Selection retains a chemical baseline
weight of 4 and ordinary food/visit weights, each reduced by the recipient's tolerance using the
source-derived fifth-power preference with a 0.001 floor. This does not guarantee a treat every visit.
It is not an alternative route to scheduled or medicinal drugs. Do not wake or feed an ineligible
patient, delay urgent care or take somebody's protected inventory by treating it as free supplies.
One physical selected portion is delivered and consumed through the actual patient's feeding path.

#### Scenario: Allowed patient treat
- **WHEN** an eligible visitor can reach a bored patient's permitted recreational food or drug
- **THEN** it can deliver that actual portion and the patient receives only its normal ingestion effects.

#### Scenario: Patient forbids the available drug
- **WHEN** the visitor could personally take a drug but the patient's joy policy forbids it
- **THEN** that drug cannot be selected for this visit and ordinary visiting remains possible.

### Requirement: Snacks and inventory use follow the actual purpose
With proper snack quantities enabled, a recreation ingestion job SHALL take no more than the ordinary quantity the pawn will actually ingest, while preserving the original smaller requested amount.
With inventory ingestibles enabled, permitted ingestible drugs need not provide nutrition to be used
from inventory, but the originating scheduled, recreational, medical or explicit-order rules still
govern permission and quantity. Neither option grants access to forbidden items or bypasses overdose,
recipient, ideology or provider checks that apply to that action. Unsupported choices fall back to
the ordinary native decision; no null job becomes an invented consumption job.

#### Scenario: Large stack of sweets
- **WHEN** recreation selects a stack larger than the pawn's ordinary ingestion amount
- **THEN** the job takes only the needed amount, leaving the rest physical and available.

#### Scenario: Nonnutritious drug in inventory
- **WHEN** a permitted drug-use job selects a currently ingestible nonfood drug in the pawn's inventory
- **THEN** it can use that item without requiring nutritional value, while an unpermitted purpose still fails.

### Requirement: Optional sleeping mood movement is bounded
With sleeping mood movement enabled, an on-map sleeping pawn whose mood would ordinarily be frozen SHALL move toward potential mood only when that also moves it toward 50%, stopping at whichever boundary it reaches first.
The option defaults off. Each ordinary mood update moves at most 1% of the normal per-hour rise or
fall amount in the applicable direction. It does not move against potential mood, continue beyond 50%,
affect awake mood rates or update unconscious, suspended/cryptosleep or off-map pawns through this
feature. Waking resumes ordinary mood behavior. Save/load does not add a catch-up mood award.
Mood below 50% never falls through this option; mood above 50% never rises through it. Mood already
at 50% or equal to potential mood does not move.

#### Scenario: Low mood with positive potential
- **WHEN** an eligible sleeping pawn has 30% mood and 70% potential mood
- **THEN** mood slowly rises toward 50% and stops there unless ordinary awake behavior later changes it.

#### Scenario: Low potential on either side of the midpoint
- **WHEN** an eligible sleeping pawn has 70% mood and 40% potential mood
- **THEN** this feature slowly lowers it only to 50%
- **AND** a pawn already below 50% is not lowered further by this option.

#### Scenario: Potential mood points away from the midpoint
- **WHEN** a sleeping pawn has 30% mood with 20% potential, or 70% mood with 80% potential
- **THEN** this option leaves mood unchanged rather than moving it farther from 50%.

### Requirement: Optional hygiene scheduling retains provider eligibility
With recreation completion enabled and a supported hygiene provider active, bathroom, drinking-water and washing opportunities SHALL fit around ordinary Recreation/Sleep schedules while urgent hygiene needs retain priority.
Below30% bladder/thirst/hygiene is urgent. During Recreation or awake Sleep time, maintenance is
considered below 80% bladder/thirst and 55% hygiene. Toilet/water rank ahead of washing at the same
urgency; urgent native survival and medical requirements still apply. Below-30% hygiene needs can
precede ordinary recreation. Nonurgent maintenance ranks below an eligible recreation bout that has
not yet reached completion; it becomes eligible ahead of further leisure when recreation is topped
off or no eligible recreation is available. This is selection priority, not forced cancellation of
an active player order or an unsupported provider job. Outside those maintenance
windows, preserve ordinary provider behavior instead of suppressing its work. Respect provider
patient-feeding status, fixture assignment, permissions, network/resources and actual jobs. No
provider or no applicable need means no replacement hygiene need or free refill.

#### Scenario: Recreation not yet complete
- **WHEN** a supported self-caring pawn has an eligible incomplete recreation bout and a nonurgent maintenance need during Recreation time or awake Sleep time
- **THEN** ordinary recreation ranks ahead of that maintenance opportunity.

#### Scenario: Hygiene after recreation is topped off
- **WHEN** recreation is topped off and a supported self-caring pawn has a nonurgent maintenance need in that window
- **THEN** an eligible ordinary provider activity can precede further leisure without bypassing fixture costs or access.

#### Scenario: Urgent hygiene before recreation
- **WHEN** an applicable hygiene need falls below 30% while the pawn considers ordinary recreation
- **THEN** the eligible urgent provider activity ranks ahead of recreation, subject to higher-priority survival and medical care.

#### Scenario: Provider unavailable or patient cannot self-care
- **WHEN** the provider is absent, incompatible or rejects self-care for that pawn
- **THEN** the integration does not invent a bathroom job or override its patient-care rules.
