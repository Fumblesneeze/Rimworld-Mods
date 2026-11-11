# Priority treatment

Owner **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`; target **1.7 beta**.
S134 replacement, observable draft only. All runtime implementation and acceptance remain pending.

## ADDED Requirements

### Requirement: Interrupt ordinary work only for a ready priority doctor

PT01. Enabled emergency response SHALL consider only spawned, player-controlled colonists with
Doctor work enabled at priority **1**, manipulation sufficient for the chosen work, and an
interruptible current activity. Drafted, downed, mentally disturbed, burning or duty-bound doctors
SHALL not be recruited. Explicit current or queued player orders, prioritized work and deliberate
rest-until-healed SHALL be preserved. Finishing oxygen escape or another mandatory activity SHALL
take precedence. A doctor carrying a pawn or already rescuing, tending another patient, feeding as medical care,
carrying a patient for surgery or performing a medical operation SHALL continue that care.
An ordinary visit to a sick pawn is not itself protected medical work.
Automatic self-tending is not protected from an otherwise eligible response; explicit player-ordered
self-tending remains protected by the player-order rule.

At rest **10% or lower**, or while starving, doctors SHALL not be interrupted by this feature.
Ordinary hunger alone does not block response. Sleeping doctors respond only when Wake doctors is
on; doctors already eating finish their meal when Let doctors finish meals is on. Turning that
option off allows interruption of ordinary eating, never of a deliberate protected player order.
Doctors currently resting for medical reasons or themselves needing urgent bleeding treatment
SHALL respond only for rescue or urgent tending, not other tending. Urgent here uses the game's
current urgent-care rule: inspected beta predicts blood-loss death in less than **18 game hours**.

#### Scenario: Priority and explicit work remain authoritative
- **WHEN** two otherwise ready doctors have Doctor priorities 1 and 2, and a third priority-1 doctor has a queued player order
- **THEN** only the first can be interrupted for this emergency; work assignments and player orders stay unchanged.

#### Scenario: Sleep, meals and exhaustion
- **WHEN** a ready doctor sleeps or eats while a patient needs care
- **THEN** the respective options determine interruption, but starvation or rest at 10% always blocks it.

#### Scenario: Continue current care
- **WHEN** a doctor is tending a patient, carrying one to bed or performing an operation as another emergency appears
- **THEN** the feature does not abandon that current care to chase the new patient.

#### Scenario: Injured doctor has a restricted response
- **WHEN** a doctor resting for medical reasons can rescue a casualty, tend an urgently bleeding patient or tend a minor injury
- **THEN** rescue and urgent tending can qualify; the minor injury does not interrupt their medical rest.

#### Scenario: Automatic and deliberate self-tending differ
- **WHEN** an otherwise ready doctor is automatically tending a minor personal wound while a permitted rescue is available
- **THEN** that self-tending can yield to rescue; a player-ordered self-tending job would remain protected instead.

### Requirement: Compose rescue and tending with existing care rules

PT02. A ready doctor's next-patient decision SHALL compare eligible rescue and tending candidates
by the shared S058 medical urgency rule before work-family preference. Imminent life-threatening
need takes precedence over routine rescue or routine tending. Within equal urgency, rescue precedes
tending; within that family use S058's remaining class/distance ties where enabled, otherwise
ordinary distance/ties. For the selected casualty, S062 decides necessary field care before transport.
It does not redirect the doctor to a less urgent casualty. Already active care remains protected by PT01.

Preserve an admitted external provider's complete ordering only where it satisfies this combined
contract. If that cannot be established, withhold the affected owned emergency selection and report
the fallback rather than quietly promising urgency-first while executing rescue-first.
The restricted doctor in PT01 considers only urgent tending candidates. Other doctors consider
eligible tendable injuries and fresh missing-part wounds. Include sickness additionally admits
routine currently tendable diseases; **a currently treatable life-threatening disease remains
eligible even when Include sickness is off**. An unrelated untreatable danger does not authorize an
otherwise invalid action or create an operation. Native permissions and availability still govern.

Rescue SHALL preserve native faction, threat, baby-care, bed, reachability, permission and reservation
requirements. A valid shared bed needs a real available sleeping place; the rescuing doctor being
in that bed does not create an extra place. Tending SHALL preserve current patient care, condition
exceptions, posture or S062 field-care permission, supplies, real work and treatment effects.
NoCare conditions do not themselves trigger tending; general NoCare does not become treatment
permission. The feature SHALL not broaden automatic rescue to outsiders, create beds or medicine,
grant self-tend permission, or turn every injury into an operation.

#### Scenario: Rescue before routine tending
- **WHEN** a ready doctor has a permitted rescue and a minor wound to tend, with no applicable field-care exception
- **THEN** rescue is chosen; the doctor physically carries the patient to a valid bed.

#### Scenario: Field emergency and target ranking compose
- **WHEN** an eligible casualty needs the enabled S062 emergency field-care path before transport
- **THEN** that care remains possible, and shared urgency selects the patient first; remaining S058 family ties and S062 field care apply once.

#### Scenario: Competing rescue and urgent tending patients
- **WHEN** one patient needs routine rescue and another is imminently threatened by blood loss and can receive urgent field tending
- **THEN** the imminently threatened tending patient goes first if the rescue casualty is not equally urgent; an urgent rescue can still receive S062 field care before transport.

#### Scenario: External ordering is retained
- **WHEN** an admitted external ordering provider prefers a farther eligible patient while owned S058 ordering is inactive
- **THEN** the supported provider ordering is preserved; if it cannot be honored, this emergency selection is withheld rather than choosing the nearer patient.

#### Scenario: Sickness option changes admission
- **WHEN** a patient has only a routine, non-life-threatening currently tendable infection and Include sickness is off
- **THEN** this feature does not interrupt a doctor for that patient; ordinary doctor work still can tend them.

#### Scenario: Foreign casualty or full bed
- **WHEN** automatic rescue rejects a foreign casualty, or every valid bed place is unavailable
- **THEN** no automatic rescue bypass occurs; permitted explicit rescue and other eligible work remain available.

### Requirement: Operation eligibility uses actual capability and ordinary risks

PT03. A doctor with an injury or scar SHALL remain eligible for automatic operation scheduling and
execution when current work capability, skill, access, patient permission, supplies and ordinary
medical rules permit it. No added injury/scar veto or forced-order exemption setting SHALL exist.
Actual loss of capacity, ordinary surgery risks and PT01's protection of a doctor's own urgent
needs remain. Drug administration and quill removal follow the same actual-operation eligibility.

#### Scenario: Scar alone does not forbid treatment
- **WHEN** a scarred doctor can ordinarily perform an available dependency-drug operation
- **THEN** the scar alone does not block automatic scheduling or execution.

#### Scenario: Incapable or risky doctor
- **WHEN** an injury prevents the required manipulation, or changes ordinary surgery risk
- **THEN** ordinary capability restrictions or changed risks apply without an invented immunity or blanket injury ban.

#### Scenario: Deliberate operation
- **WHEN** the player orders an operation
- **THEN** ordinary skill, supplies and patient restrictions remain, without a separate injury-veto bypass.

### Requirement: Assist incapacitated patients with unmet dependencies

PT04. Enabled dependency assistance SHALL schedule at most one ordinary matching drug operation
per unmet chemical dependency for a living, spawned player-faction or player-hosted patient in a
usable bed who cannot wake or is deathresting. Ordinary sleep alone SHALL not qualify. The patient
must permit medical care, not be marked for slaughter, have a currently available administration
recipe and room below the ordinary **15 medical bills** limit. The considering doctor must be
eligible for the operation, including PT03 and current access/reservations; this scheduling belongs
to ordinary enabled Doctor work and SHALL not require emergency priority 1 or interrupt a job.

An addiction qualifies when its chemical need is no longer satisfied; inspected native threshold is
**10% or lower**, with an associated addiction. A genetic dependency qualifies at the game's current
due-for-satisfaction point; inspected beta reaches this at **4.5 days** since the dose, before
deficiency at 5 days. These native biological rules SHALL remain distinct from S124 response
cooldowns. Optional genetic content absent or unsupported supplies no genetic trigger.

With a drug policy, the drug must explicitly be allowed for addiction, including genetic dependency;
missing policy rows grant no permission. A pawn with no drug-policy facility can qualify without
one. A reachable, reservable, unforbidden supply and currently usable matching recipe are required.
Among eligible supplies use the nearest to the considering doctor, then a stable ordinary tie.
The drug and quantity remain the ordinary recipe's; no new dose, medicine, effect or overdose
immunity is created. Existing S062 operation ingredient and doctor-inventory rules remain.

#### Scenario: Unconscious dependent colonist
- **WHEN** a patient cannot wake, has an unmet psychite dependency, allows the drug for addiction and has reachable supply and a suitable doctor
- **THEN** one normal administration operation is queued and completed through ordinary medical work with real consumption.

#### Scenario: Asleep, forbidden or not due
- **WHEN** the patient only sleeps normally, the dependency is satisfied, or the available drug is forbidden by policy
- **THEN** no dependency-assistance operation is added.

#### Scenario: Lower-priority doctor can schedule assistance
- **WHEN** a priority-2 doctor naturally reaches eligible medical work for an incapacitated dependent patient
- **THEN** this assistance can schedule an operation without promoting that doctor's priority or interrupting other work.

### Requirement: Share drug ordering and respect deliberate cancellation

PT05. S134 SHALL use the same pending-dose and actual-consumption authority as S124 DR09–10 and
DR13. One pending dose per patient/drug and per chemical SHALL cover both producers. Existing
manual, foreign and suspended matching bills and current ingestion block duplicates; they SHALL
not be adopted, reordered, resumed or deleted. S134's currently eligible incapacitated dependency
reason precedes a competing new S124 reason for the same chemical. A positive scheduled interval
does not exclude S134 assistance, while it can still exclude the general S124 response.

Recheck current need, patient, doctor, policy and supply before starting. A recovered patient or
removed reason withdraws only that producer's unstarted owned reason; another valid reason keeps
the shared order. Actual exposure and remaining need, not merely bill creation, govern subsequent
doses. Editing or reordering an owned bill hands it to the player while retaining automatic origin.
Deleting/suspending an automatic-origin order or cancelling its automatic ingestion sets the shared
patient/drug-or-chemical hold until explicit Resume in Health, including after reload. A failed
path or scarce supply is not a player hold. Resume does not unsuspend an existing bill.

#### Scenario: Both features request the same chemical
- **WHEN** Drug Response and Priority Treatment qualify for the same incapacitated patient's chemical
- **THEN** one coordinated dose is requested with visible reasons, not two operations or two ingestions.

#### Scenario: Suspended and cancelled treatment
- **WHEN** a matching bill is suspended, or the player deletes an automatic-origin drug bill
- **THEN** suspension blocks duplicates; deletion also preserves the shared hold until explicit Resume, even after loading.

#### Scenario: Recovery removes one reason
- **WHEN** a patient wakes before an owned dose starts but another S124 reason still qualifies
- **THEN** only the incapacitated-dependency reason is withdrawn and the remaining valid order stays, with policy checked again.

### Requirement: Automate only the supported quill operation

PT06. Enabled quill removal with supported Odyssey content SHALL let ordinary eligible Doctor
work schedule one available native quill-removal operation for a living player-faction or
player-hosted patient in a usable bed with visible porcupine quills, awake or asleep. Care,
slaughter, doctor/operation eligibility and the 15-bill limit still apply. Any existing matching
operation, including a suspended one, blocks another. The operation SHALL use its current ordinary
work and effects: inspected native recipe has **600 base work**, no ingredients or anesthesia and
removes all visible matching quills on completion. No harvested quill items or unrelated surgery
SHALL be added. Missing or changed content disables only unsupported quill automation.

Deletion or suspension of an owned automatic-origin quill bill SHALL hold further automatic quill
orders for that patient until explicit Resume quill removal in Health; that choice survives reload.
Edits hand ownership to the player, with origin retained. Lost quills, care permission or feature
disablement withdraw only unstarted owned orders; manual/foreign bills remain. Repeated checks or
failed work SHALL not immediately recreate a deliberately cancelled order. Clicking a condition
under S137 only filters available recipes and SHALL not schedule or authorize an operation.

#### Scenario: Remove several quills through one operation
- **WHEN** an eligible patient in bed has several visible quills and no matching bill
- **THEN** one normal operation is added and the doctor's actual completed work removes the visible quills.

#### Scenario: Player stops automatic removal
- **WHEN** the player deletes the automatic-origin quill operation and reloads
- **THEN** it stays absent until they explicitly resume quill removal; unrelated medical work is unaffected.

#### Scenario: No Odyssey or condition click only
- **WHEN** quill content is absent, or the player merely clicks a condition to filter recipes
- **THEN** that action creates no quill operation and does not authorize surgery.

### Requirement: Use common configuration and contextual patient controls

PT07. Configuration SHALL appear under ordered **Health / Priority treatment** settings. Initial
priority-treatment automation defaults on; Include sickness defaults off; Wake doctors, Let doctors
finish meals, dependency assistance and quill removal default on. All six defaults SHALL use the common global/save model, named and ordered
as listed. Unsupported optional content SHALL show an explanation without losing the saved choice.

Rows SHALL show Global default, Use global and Current save. New/missing save entries inherit;
inherited values follow later global changes. In a loaded game Use global remains editable while
the inactive save-value editor retains its dormant override. At the main menu all save controls
and callbacks are disabled; globals remain editable. Changes affect future decisions and do not
reset patient orders, holds or completed work. Configured initial automation initializes the
save's operational switch once; the small bottom-right switch controls current automation and is
saved with that game. Switching it off stops new S134 actions and withdraws only unstarted owned
reasons, without abandoning a physical care action or altering S124's independent valid reasons.

Patient reason/status, ordinary medical bills, restriction explanations and Resume actions SHALL
live in Health, composed with Compact Hediffs and the owned bill view. They are patient state, not
inherited settings. No separate bottom main tab or cheat healing control SHALL be introduced.
New labels, scope explanations and unavailable reasons SHALL be translated in all seven languages.

#### Scenario: Inheritance and the main menu
- **WHEN** the player changes a global value and opens an inherited save, then returns to the main menu
- **THEN** the save uses the new global value; its Use global checkbox was usable while loaded, but all save callbacks are disabled at the main menu.

#### Scenario: Disable without abandoning a patient
- **WHEN** the player switches off current automation while a doctor carries a patient and an unstarted owned dependency reason exists
- **THEN** physical care can finish safely and the unstarted S134-only reason is withdrawn without deleting a manual bill or another producer's valid order.

### Requirement: Keep admission, ownership and lifecycle observable

PT08. Each enabled integration SHALL be checked against its documented minimal behavioral
requirements before use. Compatible foreign modifications and unrelated edits SHALL remain allowed.
If a requirement is false or unknown, the affected family SHALL remain inactive with one bounded
startup warning per skipped enabled patch stating its identity, failed requirement, unavailable
behavior and fallback. Independent supported families continue; no foreign patch or list member
is removed. Declarative scheduling additions SHALL preserve existing entries without duplication.
If original Priority Treatment is active, a bounded startup warning SHALL identify the overlap and
request the ordinary per-feature settings disable path, with the same effective off behavior as a
user disabling it. This is best effort, not a cooperative-compatibility guarantee.
Unsupported external drug-order automation SHALL not be presented as coordinated S124/S134 care.

Saved state SHALL preserve owned order identity, actual dosing history and patient holds through
load and map changes without saving stale eligibility or scheduling on load alone. No source
settings/save migration, original API/binary/save compatibility checks, facade or consumer parity is
required. Existing native medical bills remain ordinary bills. Original-mod save import is outside
scope; the feature SHALL not silently rewrite foreign serialized state.

#### Scenario: Compatible modification and unsupported shape
- **WHEN** one compatible mod changes unrelated behavior but another removes a required medical decision
- **THEN** supported integrations remain active; the affected family is withheld and the startup log explains the fallback.

#### Scenario: Original provider is active
- **WHEN** startup detects active original Priority Treatment
- **THEN** it warns and requests the ordinary per-feature settings disable path, best effort, without promising coordinated source/owned scheduling.

#### Scenario: Owned save reload
- **WHEN** an owned game is reloaded
- **THEN** holds and pending identity are restored without granting treatment or duplicating a bill.
