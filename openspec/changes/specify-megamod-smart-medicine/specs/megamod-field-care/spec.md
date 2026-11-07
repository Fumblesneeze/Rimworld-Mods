# Field care

Owner **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`; **1.7 beta**.
S062 observable draft only. Uses the medical configuration and fallback in `megamod-medical-care`.

## ADDED Requirements

### Requirement: Extend treatment location without granting treatment permission
FC01. Field care SHALL extend ordinary tending to living free-colonist patients who are downed,
resting in place or deliberately waiting while drafted, when the doctor and patient meet ordinary
medical permissions, capability, reachability, reservation and safety restrictions. An occupied or
busy drafted patient SHALL not be commandeered. Self-tending still needs ordinary permission; S042
controls its automatic permission defaults. Field care SHALL not turn prisoners, visitors, animals
or hostile pawns into free colonists or remove their existing native treatment options.

Ordinary field care starts Off. No suitable bed allows field care when no usable bed can currently
be reached and used; Always also permits eligible field care despite a bed. A mobile undrafted
patient SHALL still prefer an ordinary usable bed rather than be forced to stay on the ground.
An explicit valid drafted treatment SHALL preserve its native inventory/care scope.

#### Scenario: No bed and eligible resting colonist
- **WHEN** No suitable bed is selected and a resting eligible colonist has no usable bed
- **THEN** a capable permitted doctor can tend them in place through ordinary medical work.

#### Scenario: Drafted patient is busy
- **WHEN** an otherwise eligible drafted patient is performing another action or cannot safely wait
- **THEN** the field-care option does not seize the patient or cancel the action to manufacture eligibility.

#### Scenario: Ordinary bed is available
- **WHEN** a mobile undrafted patient can use a suitable bed even with Always selected
- **THEN** ordinary bed-seeking remains preferred; a player can deliberately hold an eligible patient in place by drafting.

### Requirement: Emergency care compares a real journey with known danger
FC02. Emergency field care SHALL initially be enabled independently of ordinary field mode. For
an eligible downed patient, compare the earliest known current fatal deadline with doctor travel to
the patient plus actual carrying travel from that patient to a suitable bed and a configurable
**2-game-hour allowance**. If the deadline is no later than that sum, tend in place rather than
require the hospital journey first. The allowance is a planning margin, not a wait or bonus treatment.

Use known blood-loss, timed fatal-condition and currently increasing lethal-condition estimates;
unknown future developments SHALL NOT be invented as certain deadlines. With no usable bed/route,
emergency care SHALL allow tending when a known deadline is within doctor arrival plus that allowance.
No-path results SHALL NOT count as zero travel. A doctor who cannot reach the patient still cannot
treat them. This feature does not promise survival or override S058's inter-patient work ordering.

#### Scenario: Hospital journey is too slow
- **WHEN** an eligible downed patient's known deadline is within the real collection-and-carry journey plus allowance
- **THEN** permitted emergency tending can start at the patient without first transporting them to bed.

#### Scenario: No bed and imminent danger
- **WHEN** no usable bed exists, ordinary field mode is Off and the known deadline is within doctor arrival plus allowance
- **THEN** emergency field care remains available instead of being disabled simply because the bed is missing.

#### Scenario: Several fatal estimates and an unknown condition
- **WHEN** the patient has several known deadlines and another condition with no reliable estimate
- **THEN** the earliest known deadline governs and the unknown condition neither hides it nor becomes a fabricated immediate death prediction.

### Requirement: Fetching medicine must leave a useful treatment opportunity
FC03. In field or ordinary tending, every medicine collection plan SHALL be excluded when its
complete estimated itinerary from the doctor's current position through every planned pickup and
holder transfer to the patient, including handling time and one ordinary treatment, cannot finish
before a known fatal deadline. Check the complete itinerary again before each optional additional
pickup; omit that pickup when it would exceed the budget and proceed with already usable supplies.
With less than **2 game hours** remaining, that total must also fit within half the
remaining time. Ground, patient and other-donor inventories SHALL use the same rule; a nearer
permitted supply or permitted unmedicated treatment remains possible. The 2h threshold follows
the emergency allowance setting. Unknown death time leaves ordinary supply choice in force.

Already acquired usable medicine SHALL not be discarded merely because further stock is unsafe
to fetch. Reconsider timing after each actual treatment or relevant interruption. No hypothetical
future treatment effect SHALL be credited before it happens, and a failed prognosis calculation
SHALL not strand the doctor in repeated fetching or grant remote treatment.

#### Scenario: Nearby herbal instead of distant better medicine
- **WHEN** fetching the better medicine leaves no timely treatment opportunity but a permitted nearby supply does
- **THEN** the doctor can use the nearby supply instead of embarking on the fatal detour.

#### Scenario: Donor medicine is also too far away
- **WHEN** an inventory donor is reachable but its fetch-and-treatment time exceeds the emergency limit
- **THEN** inventory ownership does not exempt that medicine from the timing rule.

#### Scenario: No safe medicine fetch
- **WHEN** every eligible medicine fetch is too slow and unmedicated tending is permitted
- **THEN** the doctor can tend without medicine, without a promised survival outcome or invented dose.

#### Scenario: Individually short pickups form a long collection trip
- **WHEN** two supplies each pass a separate direct-trip estimate but visiting both plus transfers and treatment exceeds the emergency budget
- **THEN** the doctor omits the optional second pickup and proceeds with a timely valid plan instead of treating two separate estimates as proof for the combined trip.

### Requirement: Temporary field rest is ordinary ground rest with bounded cleanup
FC04. The feature SHALL provide one identifiable temporary resting place when an eligible patient
needs to rest in place and no usable bed is selected, without destroying or replacing existing
content. It SHALL grant ordinary sleeping-spot consequences: no materials, Comfort0.4, surgery
factor0.7 and ordinary ground-spot rest/tending effects, with no premium-bed bonus. It SHALL not
become a persistent assignable hospital bed or a means to bypass ordinary operation eligibility.
If the cell cannot safely host it, the feature SHALL not create an invalid rest order or retry endlessly.

The player SHALL be able to remove the temporary place with its contextual Cancel action. It SHALL
clean up after its patient stops using it, including interruption/death/departure; a genuinely idle
unreserved place SHALL disappear within **one game hour**. Removal SHALL not erase a real bed,
another patient's place, cargo or unrelated orders. A live patient losing the place retains ordinary
rest/treatment options and actual medical state. Saving/loading SHALL not duplicate places.

#### Scenario: Safe ground rest
- **WHEN** a qualifying patient needs a temporary rest place on a safe empty cell
- **THEN** one marked place supports ordinary ground rest and exposes Cancel without producing a valuable bed.

#### Scenario: Occupied cell or conflicting content
- **WHEN** placing a temporary spot would destroy content or conflict with another active resting place
- **THEN** that placement is declined and no invalid rest job or repeated creation loop is started.

#### Scenario: Interrupt, reload and cleanup
- **WHEN** the patient leaves, dies or changes jobs, including across a save/load
- **THEN** the unused owned place is removed within the cleanup bound and other beds or patients are untouched.

### Requirement: Continue useful care and return to ordinary patient workflows
FC05. After each treatment, the doctor SHALL reconsider the patient's actual remaining need and
field eligibility. Useful permitted tending can continue, including more than one injury on a
waiting drafted patient. When the emergency condition no longer warrants field care and ordinary
field mode does not allow it, field treatment SHALL finish cleanly so ordinary rescue/bed-seeking
can proceed. This does not guarantee an available rescuer or bed. Unused medicine remains physical
and follows shared capacity, personal-retention and ordinary delivery rules.

S024 cleaning SHALL yield to continuing eligible care. Field-rest feeding SHALL be available only
for an actual lying field patient who meets ordinary hunger, food-policy, faction, medical-rest and
feeding restrictions. Merely enabling Always SHALL NOT authorize feeding standing healthy pawns.
S058 still orders eligible feeding patients; it does not create hunger or change meal policy.

#### Scenario: Two injuries while drafted
- **WHEN** a deliberately waiting patient still has two valid treatments
- **THEN** the doctor can continue both without the first completion silently removing all later field eligibility.

#### Scenario: Emergency ends after bleeding stops
- **WHEN** actual treatment removes the emergency reason and ordinary field care is Off
- **THEN** field treatment ends cleanly, unused medicine remains accounted for, and ordinary rescue/rest can become the next valid action.

#### Scenario: Hungry patient versus healthy bystander
- **WHEN** a lying field patient needs feeding while a healthy standing colonist is nearby
- **THEN** ordinary feeding can serve the patient but the field mode does not create a feeding job for the bystander.

### Requirement: Initial field care excludes deferred CE stabilization

FC06. Initial field care SHALL operate without a Combat Extended adapter. CE stabilization jobs,
controls and provider-specific medicine handling SHALL NOT be part of initial implementation or
acceptance. Existing source research remains available for a future scoped change; it does not
establish supported CE gameplay. Owned S080 bleeding-only care remains its separate specified action.

#### Scenario: Ordinary field care without CE
- **WHEN** an eligible patient requires ordinary field treatment in the initial supported configuration
- **THEN** it uses the specified native care and real supplies without adding a CE stabilization action.
