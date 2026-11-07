# Bleeding-only emergency tending

Owner **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
S080 observable draft; no implementation or accepted gameplay. Corrections and policy assumptions
are recorded in SB-Q1–4 for later work.

## ADDED Requirements

### Requirement: Order bleeding-only care from the patient context
SB01. A player selecting one capable doctor SHALL have a contextual **Stop bleeding on [patient]
(without medicine)** order for an eligible patient with at least one permitted, currently bleeding,
tendable condition. The order SHALL work both drafted and undrafted and support ordinary queued
orders. It SHALL not create automatic stabilization work, replace ordinary Tend, require a hospital
journey, or spend time collecting medicine. No active bleeding treatment means no actionable order.

#### Scenario: Undrafted field order
- **WHEN** the player selects one undrafted capable doctor and orders bleeding-only care on an eligible downed patient outside a hospital
- **THEN** the doctor goes directly to that patient and begins unmedicated treatment there, without fetching medicine or requiring a bed.

#### Scenario: Drafted doctor and multiple patients
- **WHEN** the player queues this order for two eligible patients using a drafted doctor
- **THEN** each order treats only its patient's permitted bleeding conditions and the next queued order can proceed when the first finishes.

#### Scenario: No qualifying bleeding
- **WHEN** a patient has only bruises, disease, old blood loss or already stopped bleeding
- **THEN** there is no actionable bleeding-only order; ordinary treatment remains available under its own rules.

### Requirement: Preserve medical ability and the source's actual patient scope
SB02. The order SHALL require a living, controllable doctor able to perform medical work, reach
and interact with the patient and obtain the ordinary patient reservation. Ordinary doctors require
Medical skill **at least 1**; a supported mutant whose own rules permit tending uses those rules
instead of this skill floor. Animals and mechanoids SHALL not become doctors through this feature.
Medical work incapacity or inability to manipulate blocks treatment; unrelated inability to perform
unskilled labor SHALL not block an otherwise eligible doctor. Work priority is not rewritten by an order.

Patients SHALL be living, accessible pawns on the same map: downed pawns of any faction (including
wild animals and enemies), or non-hostile colonists, quest lodgers, colony prisoners/slaves, player
animals and colony subhumans entitled to medical care. Standing visitors, standing wild animals
and standing enemies are outside that scope. A doctor can treat themself only while capable and
with current self-tend permission. Aggressive mental states block this action; downing does not
make a currently aggressive patient safe. An upright patient in another mental state cannot be
held for treatment. These restrictions SHALL be checked again during work.

#### Scenario: Enemy or wild animal is downed
- **WHEN** a non-aggressive downed enemy or wild animal is bleeding and the selected doctor can reach and reserve it
- **THEN** the contextual order can treat it without first capturing, taming or assigning it a bed.

#### Scenario: Mobile colony patient versus visitor
- **WHEN** a non-hostile colony prisoner is bleeding while mobile and a standing visitor is also bleeding
- **THEN** the order is available for the prisoner under the other rules, while it does not extend the same mobile eligibility to the visitor.

#### Scenario: Ability and self permission
- **WHEN** a selected pawn lacks manipulation, is incapable of Doctor work, or attempts self-care with self-tending disabled
- **THEN** no treatment begins and the relevant unavailable reason is explained; inability to do unskilled labor alone is not a rejection.

#### Scenario: Hostile patient recovers or becomes aggressive
- **WHEN** an enemy stands up, or a patient enters an aggressive mental state before a treatment completes
- **THEN** the order stops without applying that unfinished treatment or forcing continued cooperation.

### Requirement: Treat only permitted bleeding conditions in blood-loss order
SB03. Each treatment SHALL choose the permitted tendable condition contributing the greatest current
blood loss. Equal contributions SHALL use ordinary tending priority, then severity, then a stable
order among otherwise equal conditions. This includes fresh missing-part wounds and compatible
modded bleeding conditions; membership SHALL not depend solely on being a cut or other ordinary injury.
Non-bleeding conditions SHALL never receive incidental treatment through this order.

Current general and individual **No care** choices SHALL exclude their affected conditions; other
care levels permit this explicitly unmedicated order without being changed. Individual care replaces
general advice only for that condition. Self-tend permission remains independent. Re-evaluate the
remaining conditions and permissions before each actual treatment. Once no eligible useful bleeding
treatment remains, finish even if blood loss, infection, non-bleeding injuries or untendable bleeding remain.
The order SHALL not loop indefinitely when a condition cannot be helped by the supported treatment.

#### Scenario: Disease outranks an ordinary wound elsewhere
- **WHEN** the patient has a dangerous disease, two bleeding wounds of different bleed rates and a non-bleeding bruise
- **THEN** the greater bleeding wound is treated first, then the other bleeding wound; the disease and bruise receive no treatment from this order.

#### Scenario: Missing limb is the only bleeding source
- **WHEN** a fresh missing-part wound is the patient's only tendable bleeding condition
- **THEN** the order is available and can stop its bleeding through ordinary unmedicated treatment, without restoring the limb.

#### Scenario: Care changes while the doctor works
- **WHEN** the player excludes the chosen condition from care or revokes self-tend permission before completion
- **THEN** the unfinished treatment is not applied in violation of that change, and no general or individual policy is silently rewritten.

#### Scenario: Untreatable bleeding and no progress
- **WHEN** no permitted condition can receive a useful supported treatment, including a condition that remains eligible but unchanged after one completed attempt
- **THEN** the order ends with an honest unavailable-care explanation instead of repeatedly earning treatment credit or claiming that the patient is stable.

### Requirement: Pay ordinary unmedicated treatment time and consequences
SB04. The action SHALL consume no medicine, including medicine already carried by doctor or patient.
Each treatment SHALL use ordinary unmedicated treatment duration: **600 game ticks divided by the
doctor's current Medical tend speed**, with native whole-tick rounding, rather than a faster emergency
multiplier. Invalid or non-positive speed SHALL prevent treatment with an explanation, not grant an
instant result. Ordinary no-medicine batching applies only within the selected bleeding conditions:
normally one condition, with a supported condition's ordinary same-kind batch rule if applicable.

Treatment quality SHALL retain ordinary doctor, bed and self-treatment effects: the inspected native
baseline uses **0.3** no-medicine potency, **0.7** maximum quality and **0.7** self-treatment factor.
Use supported native rules when these effective stats legitimately change; do not impose a second
quality multiplier. Successful work SHALL retain ordinary treatment notifications, medical experience,
records and applicable patient/doctor relationship or quest effects exactly once. Interrupted or
empty treatment SHALL not award completion credit. Existing blood loss, infection risks, wounds and
other health progression remain; this order SHALL NOT guarantee survival or instantly heal damage.

#### Scenario: Supplies are already in inventory
- **WHEN** the doctor and patient carry medicine and the player selects bleeding-only care
- **THEN** their medicine quantities remain unchanged and real unmedicated work produces ordinary treatment quality.

#### Scenario: Work rate and continued danger
- **WHEN** a doctor with Medical tend speed 100% performs one ordinary single-wound treatment
- **THEN** the treatment requires 600 advancing game ticks, bleeding can continue during that work, and completion stops only the treated bleeding rather than removing accumulated blood loss.

#### Scenario: Cancel before completion
- **WHEN** the player cancels partway through a treatment
- **THEN** the unfinished treatment does not alter the wound or grant completion experience, records or other success effects.

### Requirement: Keep patient control and cleanup local to the order
SB05. The doctor SHALL work at the patient's current reachable position, using the appropriate
bed interaction position when already in bed. This explicit order can ask an eligible mobile colony
patient to wait temporarily, retaining their posture; it SHALL not require drafting the patient or
create a new resting spot. Patient death, departure, lost access or reservation, doctor incapacity,
or a superseding player order SHALL stop unfinished work safely.

Completion/cancellation SHALL release only this action's reservations and temporary waiting state.
It SHALL not cancel a newer unrelated patient order, evict a resting patient from bed, leave them
permanently waiting, or carry them to a new location. Ordinary patient rest/rescue/job choice can
resume after care; an available rescuer or continued bed rest is not promised.

#### Scenario: Treat in an occupied bed
- **WHEN** the player orders this care on an eligible patient already resting in bed
- **THEN** the doctor treats from the usable interaction position and cleanup does not itself evict the patient or cancel their underlying rest requirement.

#### Scenario: Player replaces the temporary wait
- **WHEN** a patient receives a new valid order while the doctor is treating them
- **THEN** the emergency action respects loss of cooperation and its cleanup does not cancel the newer order merely because it is also a waiting action.

#### Scenario: Target disappears or another doctor owns care
- **WHEN** the patient leaves the map or the required reservation cannot be obtained or retained
- **THEN** no remote treatment or second simultaneous treatment occurs, and owned temporary state is released.

### Requirement: Save actual work without replaying treatment
SB06. Valid owned queued/in-progress orders SHALL survive save/load with their patient identity,
remaining work and eligibility rechecked. Already completed wound treatment and completion effects
SHALL not replay. Invalid targets or unsupported saved state SHALL cancel only the affected order
with a bounded diagnostic and leave existing health and other orders intact. This change SHALL NOT
automatically import source jobs/settings or promise safe removal with source jobs still saved.

#### Scenario: Save midway through field care
- **WHEN** the player saves during a treatment and reloads an otherwise valid game
- **THEN** remaining work continues at the correct reachable position, without free completion, duplicated credit or loss of the previous completed treatments.

#### Scenario: Queued patient is no longer eligible
- **WHEN** a queued patient dies, disappears or no longer has a qualifying condition before that order starts
- **THEN** that order ends safely and does not block later valid queued work indefinitely.

### Requirement: Compose one medical action without duplicating settings or UI
SB07. The order SHALL appear in the native patient context, with progress/job reports and relevant
rejection explanations in all seven baseline languages. It SHALL add no bottom tab, debug action,
corner toggle, configuration screen or new adjustable medical policy. Orders and current treatment
are entity gameplay state. Existing Health views SHALL show actual treatment results consistently.

S042 supplies self-tend permission; S062 supplies individual care and ordinary field treatment;
S058 chooses automatic patients without interrupting this valid explicit order. S024 cleaning and
other optional errands SHALL not delay its emergency treatment. This order's bleeding-only scope
and blood-loss ranking take precedence over ordinary S062 batch ranking only for this explicit action.
Combat Extended stabilization is deferred outside initial support. Drug Response/Priority Treatment
SHALL NOT duplicate the same treatment while this action owns the patient. Automatic triage and drug administration policy are not redefined here.

#### Scenario: Several owned medical features are enabled
- **WHEN** the player issues bleeding-only care with individual care, automatic patient ranking and field care available
- **THEN** one doctor performs this explicit permitted bleeding-only action without fetching supplies, cleaning first, treating unrelated conditions or starting duplicate emergency care.

### Requirement: Unsupported integration leaves ordinary care usable
SB08. An active **Stabilize Bleeding** source provider SHALL retain ownership of its overlapping
order; the owned order remains inactive with a bounded startup explanation. No foreign provider
SHALL be disabled, unpatched or impersonated. Missing/changed required medical or menu contracts
SHALL disable only the dependent owned action with an explanation and preserve ordinary care.
Compatible foreign changes SHALL remain allowed. Every actual enabled patch that cannot meet its
documented target requirements SHALL be skipped with one bounded startup warning identifying the
patch, failed requirement, lost behavior and fallback. Unknown effective behavior SHALL not silently
turn this command into ordinary all-condition treatment.

#### Scenario: Original source is active
- **WHEN** the game starts with Stabilize Bleeding active
- **THEN** there is no second owned bleeding-only command and startup logs explain that the original provider owns the overlap.

#### Scenario: Compatible extension or missing treatment contract
- **WHEN** an unrelated compatible medical extension is present, or the required treatment selection/effect contract is unsupported
- **THEN** the compatible case remains usable; the unsupported case leaves the owned action inactive with bounded diagnostics and ordinary care intact.
