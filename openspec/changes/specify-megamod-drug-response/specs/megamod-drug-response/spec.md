# Automatic pain and condition responses

Owner **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`, **RimWorld 1.7 beta**.
S124. Observable draft only. Source-like defaults and intentional corrections below are proposed
choices pending DR-Q3–7, not confirmed playtesting or delivered functionality.

## ADDED Requirements

### Requirement: Response configuration has visible scope and conservative defaults
DR01. The feature SHALL provide ordered **Medical > Drug responses** settings sections: General,
Pain relief, Condition rules, and Notifications. Configuration SHALL use the common Global default,
Current save and Use global controls for every setting below. Each condition SHALL have its own
Use global choice for its complete rule (both response slots, methods and intervals). Changing one
condition locally SHALL not freeze another condition's global updates. New/missing condition entries
inherit their current global rule, or the empty built-in rule if no global rule exists.

| Choice | Initial value and allowed values |
| --- | --- |
| Pain responses / condition responses | Off / off |
| Include prisoners for pain / conditions | Off / off |
| Include non-medical pain drugs | Off |
| Show only researched choices | On |
| Respond after full immunity | On |
| Always use administration bills for intense/extreme pain | Off |
| Pain method | Self-use when capable; alternative Administration bill |
| Pain minimum interval | 24hours; whole hours6–48 |
| Four pain-band drugs | None |
| Condition-rule collection | Empty |
| New condition first response | None, Administration bill,24hours; method also Self-use when capable; interval whole hours6–120 |
| Optional second response | None, Administration bill only,24hours; interval whole hours6–120 |
| Pain / condition notifications | On / on |

The per-save control SHALL be inactive while inheriting; both it and Use global SHALL be inactive
at the main menu. Global editing SHALL remain available. All callbacks, resets and collection edits
SHALL obey scope. Changing a global value SHALL affect saves inheriting that particular setting or condition, not
overrides of that condition;
restoring Use global SHALL retain a dormant override. New effective values apply at the next response
evaluation and before an unconsumed automatic dose, never undoing an already consumed drug.

#### Scenario: Initially inert configuration
- **WHEN** a new save inherits untouched defaults
- **THEN** the feature creates no automatic dose or bill, and the settings show both families off and no selected drugs.

#### Scenario: One condition overrides and another inherits
- **WHEN** a save overrides flu but inherits wound infection, and both global rules change
- **THEN** local flu stays unchanged while wound infection follows the new global rule.

#### Scenario: Global additions and removals
- **WHEN** a global condition rule is added or removed
- **THEN** saves inheriting that condition gain it or resolve to an empty rule, while a local override remains unchanged.

#### Scenario: Disable one inherited condition in this save
- **WHEN** the player chooses Disable in this save for an inherited condition
- **THEN** that condition receives an explicit empty override and remains off through later global changes
- **AND** other conditions keep their own inheritance; choosing Use global again restores current inheritance.

#### Scenario: Dormant condition override
- **WHEN** a condition override is switched to Use global and later switched back
- **THEN** its dormant two-slot override is restored, or a first override is seeded from that condition's effective global rule.

#### Scenario: Clear current rules and later global addition
- **WHEN** a player confirms Clear all in the current-save scope
- **THEN** currently configured conditions receive explicit empty local overrides without changing globals or another scope
- **AND** a later newly configured global condition still inherits unless that condition already has an explicit local override.

#### Scenario: Switch inheritance during treatment planning
- **WHEN** a save resumes global settings that remove a pending response
- **THEN** the unconsumed owned response is revalidated and withdrawn if no valid reason remains; consumed effects and player-authored operations remain unchanged.

### Requirement: Saved rules are searchable and edits are unambiguous
DR02. Settings SHALL show all configured conditions in a searchable, scrollable list, with condition,
first and second drugs, methods, intervals and active/unavailable status visible without selecting
each condition in turn. Search SHALL match displayed condition and drug names. Add/Edit SHALL expose
the selected condition and both responses together. Valid field edits SHALL take effect immediately
with visible saved state; invalid edits retain the preceding valid value and explain the error.
There SHALL be no hidden extra Set action. Removing one response SHALL preserve the other; removing
all rules SHALL require an explicit clear-all confirmation naming the edited scope and explaining
that current-save clearing disables currently configured conditions individually, while future new
global conditions can still inherit. Global deletion leaves independent save overrides intact. Search filtering
SHALL never change response priority or delete hidden rules. The UI SHALL remain usable at supported
window sizes and in all seven baseline languages.

#### Scenario: Edit the second response
- **WHEN** the player changes the second drug or interval, closes settings and reopens them
- **THEN** that change remains visible on the same condition row, while the first response and its method remain unchanged.

#### Scenario: Search and clear safely
- **WHEN** a search shows one of several configured conditions and the player removes its second response
- **THEN** only that response is removed; hidden rows, the first response and the other configuration scope remain intact.

### Requirement: Condition and drug choices describe actual available content
DR03. The baseline condition catalog SHALL include asthma, burns, blood loss, carcinoma, fibrous
mechanites, flu, food poisoning, gut worms, malaria, muscle parasites, plague, sensory mechanites,
sleeping sickness, wound infection and anesthetic. It SHALL additionally include actual drug
addictions supplied by admitted content, excluding the source's special mental-treatment chemical.
Admitted Common Ailments and Diseases Overhauled catalogs SHALL expose their individually supported
conditions with provider labels. Unknown or absent condition types SHALL NOT be guessed from names.
Added body parts are not condition-drug response targets.

Pain choices SHALL be purposeful pain-relief drugs with an actual advertised effect reducing pain
by at least20% multiplicatively or20percentage points additively. Small incidental relief alone is
insufficient. Medical-only is the default; non-medical choices require the stated setting. Condition
choices SHALL be ingestible medical-category drugs. Selecting a condition-drug pair expresses the
player's desired response; it SHALL NOT assert a cure or manufacture a treatment effect. Drug info
and the selected route's actual availability SHALL be accessible.

Research filtering SHALL affect new choices only, using complete current production prerequisites.
It SHALL NOT suppress an existing valid response using an acquired drug. With no save loaded,
research state SHALL be shown as unavailable and all content-qualified global choices remain
selectable. Missing/disabled selections SHALL remain visible as inactive saved choices, never be
silently substituted or reassigned to an identically named item. Restored content SHALL be
revalidated before its saved rule can run.

#### Scenario: Bought drug and later research
- **WHEN** an existing configured drug is owned but its production is unresearched
- **THEN** research filtering does not forbid its use; new-choice filtering reflects research, and showing all choices does not grant production or stock.

#### Scenario: Removed optional drug
- **WHEN** the selected optional drug or condition is absent after loading
- **THEN** that response remains labeled unavailable, other valid rows work, and no different drug is chosen automatically.

#### Scenario: Drug does not cure the selected condition
- **WHEN** the player selects a valid medical drug whose ordinary effect does not cure the selected illness
- **THEN** the rule is presented as that chosen medication response and any dose has only its ordinary effects, not an invented cure.

### Requirement: Pain and condition triggers have precise boundaries
DR04. Pain responses SHALL use the current total pain: none at or below1%, minor above1% and below15%,
serious15% to below40%, intense40% to below80%, extreme80% or greater. Only the current band's selected
drug applies; None SHALL NOT borrow a lower-band drug. A condition rule SHALL require at least one
currently visible matching condition; several matching wounds SHALL NOT multiply the dose. Each
condition supports up to two distinct configured responses. Identical-drug slots SHALL be rejected
with an explanation, rather than create a second dose. The second response remains optional.

If Respond after full immunity is off, only fully immune instances stop justifying that condition
response; another non-immune matching instance can still justify it. Anesthetic response SHALL wait
while its severity is80% or greater. These checks SHALL NOT remove conditions, alter immunity or
make operations happen simply because a condition is selected in Health.

#### Scenario: Pain crosses a boundary
- **WHEN** pain changes from39% to40%, or from79% to80%
- **THEN** the intense or extreme band respectively becomes applicable, subject to the shared dose interval; no additional dose is justified solely by the band change.

#### Scenario: Multiple infections
- **WHEN** two visible infections match one configured response
- **THEN** the pawn receives at most one pending dose for that response, and a fully immune infection does not hide a second non-immune one when immunity suppression is enabled.

### Requirement: Patient eligibility and care choices bound automation
DR05. S124 SHALL operate on living organic humanlike colonists, including ordinary colony slaves,
and separately enabled colony prisoners, on a map or in their player caravan. It SHALL NOT enroll
animals, mechanical pawns, hostile/foreign visitors or unsupported physiologies merely because they
have a similar condition. A supported route must be able to affect that patient with the selected
drug. Mental state, burning, active safety escape and current player-forced work SHALL defer owned
response actions. S124 SHALL NOT remove orders, undraft a pawn, wake it to self-medicate, or end
ongoing ingestion, rescue, tending, feeding or surgery to start a response. Ordinary discretionary
work can yield at an interruptible opportunity; queued player orders remain intact.

General No care SHALL prevent S124 automatic administration. S062 individual No care SHALL exclude
that condition as a condition-response reason; other independently permitted conditions or pain can
remain reasons. Actual operation medicine SHALL follow the existing operation care rules, including
S062's explicit Surgery uses best choice; S124 SHALL NOT apply a tending ceiling in its place or
treat every non-medicine drug as a tending-medicine tier. Doctor capacity,
work permission, medical-rest needs and independent medical/surgery restrictions remain effective.

#### Scenario: Drafted or ordered patient
- **WHEN** a drafted pawn or pawn with forced current/queued work develops a configured condition
- **THEN** S124 does not interrupt, clear or replace those orders; the response can be reconsidered after that restriction ends.

#### Scenario: Individual care refusal
- **WHEN** the only matching infection is set to No care
- **THEN** that infection creates no automatic drug response; inspecting its rule does not change care, and a separately permitted pain response remains independently evaluated.

#### Scenario: Non-enrolled animal
- **WHEN** a colony animal has the same condition as a configured colonist
- **THEN** S124 does not self-dose it or create a condition-response bill; any separately specified S134 animal behavior retains its own scope.

### Requirement: Native drug policy remains independent and explicit
DR06. A policy-bearing pawn SHALL have a valid current drug row before an automatic response can use
that drug. S032's maintenance supplies catalog repair without rewriting existing permissions or
creating a new policy. A positive-frequency scheduled drug SHALL suppress S124 supplemental use even
when the schedule is currently waiting for mood, recreation or time. Configured non-addiction pain
and condition responses are separate treatment permission: they SHALL NOT enable pleasure use,
change a schedule or raise inventory targets. An addiction-triggered response SHALL additionally
require the policy's addiction permission. A pawn with no drug-policy tracker can receive an otherwise
permitted medical administration; a broken/missing policy on a policy-bearing pawn grants no permission.

Native patient/drug restrictions, including supported physiology and the initiating action's
applicable ideological refusal, SHALL remain. S086's Medical go-juice classification SHALL govern
category-dependent choices without imposing a manual-inventory-only veto on a route that does not
use it. S124 SHALL reject non-medical pain medication for teetotalers and for non-player-faction
patients. It SHALL NOT claim that a configured dose avoids ordinary addiction/dependency/overdose
risks. Manual operations remain manual.

#### Scenario: Schedule owns the selected drug
- **WHEN** pain calls for a drug already scheduled every two days, but its mood threshold is not met
- **THEN** S124 creates no supplemental dose and does not change the schedule to make it eligible.

#### Scenario: Withdrawal chosen by the player
- **WHEN** the current drug policy disallows addiction use of the proposed addiction-response drug
- **THEN** the automatic addiction response remains blocked rather than defeating the withdrawal choice.

#### Scenario: Plain treatment is not recreational permission
- **WHEN** the player enables an otherwise permitted non-addiction medical response while pleasure and scheduled use are off
- **THEN** that response can operate without enabling either policy permission or adding carried inventory stock.

### Requirement: Response methods use real stock and ordinary action paths
DR07. A new response SHALL plan one ordinary full-dose equivalent of the configured drug through
normal ingestion, preferring eligible own inventory then reachable permitted map stock for self-use.
Physical item count SHALL NOT count a partial item as a full dose. Supported partial quantities and
effects SHALL use S085's complete shared contract; an administration recipe unable to account for
fractions excludes remainders. No automatic new order is created from less than its required usable
quantity. S124 SHALL NOT invent fractional effects or combine items through an unsupported path.
It SHALL recheck
stock, reservations, forbidden/access state, quantity ownership and actual carrying capacity before
pickup and use. It SHALL NOT fetch extra stock merely because a policy has a carried quantity;
ordinary shared stock management owns refilling.

Pain SHALL use a medical bill when configured, for enabled prisoners, when incapable of self-use,
or when the intense/extreme bill override applies. Medical rest with overall health below75% also
requires a pain bill. Enabled on-map prisoners SHALL use administration bills for both condition
slots, including a first slot configured for self-use. For other patients, a first condition response
configured for self-use SHALL wait while incapable, not silently change methods; configured bill
responses and second responses use administration.
Sleeping capable pawns do not self-medicate; an otherwise valid bill can await ordinary treatment
without being evidence of a completed dose. Genuine inability to self-ingest includes age/capacity,
not just walking ability.

Bills SHALL use an actually available administration of the selected drug for this patient, one
ordinary dose, with ordinary ingredients, work, effects and medical eligibility. Supply suitability
SHALL be evaluated for an eligible doctor using ordinary operation ingredients plus the surgeon's
eligible inventory when that operation path supports it and the doctor-inventory switch permits it.
S124 SHALL NOT import S062 tending's patient/other-colonist/animal donor extension into operations,
or require the patient's ability to walk to a shelf. A missing recipe, doctor or accessible supply produces a visible
waiting reason and no invalid new bill. Unreachable preferred stock SHALL NOT hide reachable stock
of the same drug. No substitute drug, extra medicine or remote consumption is invented.

#### Scenario: Downed patient and accessible doctor supply
- **WHEN** a pain response requires administration and an eligible doctor can bring the selected drug to the patient
- **THEN** the patient need not be able to walk to the drug, and one ordinary medical bill can be queued and completed with real stock.

#### Scenario: Self-use condition while incapacitated
- **WHEN** a non-prisoner's condition first response is self-use and the pawn cannot self-ingest
- **THEN** that slot waits visibly; a separately configured second administration response can proceed if its own requirements hold.

#### Scenario: Prisoner condition response uses a doctor
- **WHEN** condition responses include prisoners and a prisoner's first slot is configured for self-use
- **THEN** the map response uses an eligible administration bill, even if the prisoner could walk to the drug; it neither self-doses nor waits merely for self-ingestion capability.

#### Scenario: Only an unrelated donor carries the drug
- **WHEN** the only drug is in another colonist's or animal's inventory outside ordinary operation supply access
- **THEN** S124 waits instead of promising an administration bill based on that tending-only donor; moving the drug into an eligible operation supply makes it available normally.

#### Scenario: Reserved or vanished supply
- **WHEN** the chosen stack becomes forbidden, reserved elsewhere or disappears before use
- **THEN** no item is duplicated or consumed remotely, no completed dose is recorded, and the response waits or selects another eligible stack of the same drug.

### Requirement: Actual consumption starts waiting periods
DR08. A configured response SHALL be eligible for its first dose without an artificial game-start
delay. Its minimum interval starts only after successful actual consumption and expires at the
inclusive stated time. Failed pickup, failed bill creation, cancellation before any consumption or
a merely queued operation SHALL NOT count as a dose. Successful use of the same drug through another owned response, a native
schedule or a manual action SHALL count toward that drug's configured S124 waiting periods for the
patient. All pain bands additionally share one pain-response interval after an actual owned pain
dose, so switching bands or drugs does not immediately repeat pain medication.

For a fully admitted S085 partial-consumption path, every newly committed positive amount SHALL
retain its actual effects/exposure and update the last-consumed time once. Cancellation after such
progress SHALL show a partial dose and start the interval; Resume automation SHALL NOT erase that
history or waive the interval for a new dose. Continuing the same still-active consumption action
can finish only its original remaining dose budget, without creating a second order or repeating
committed effects. A canceled action does not automatically restart that budget before the interval.
Cancellation/reload with no new consumption SHALL NOT refresh the timestamp. If S085's complete
risk/history/quantity path is unresolved or loses admission, it remains unavailable for partial
handling; ordinary intact-dose paths remain available, while existing remainders retain their state
and cannot be treated as full stock.

An existing active drug effect greater than75% of the selected drug's positive ordinary effect
increment SHALL defer another S124 dose where that comparison is supported. This is not75% of an
arbitrary maximum. A supported drug capable of overdose SHALL also respect the native automatic
overdose deferral: current overdose above50% and less than half an in-game hour since an
overdose-capable drug. Ordinary side effects, tolerance, addiction and overdose remain possible;
unknown effects SHALL NOT be described as proven safe. The interval and current deferral reason
SHALL be inspectable in the patient's response status.

#### Scenario: Bill waits for a day
- **WHEN** an administration bill was queued yesterday but has not been performed
- **THEN** it remains a pending dose, not a consumed dose or permission to queue another one.

#### Scenario: Manual dose and exact expiry
- **WHEN** a patient manually consumes the configured drug and its response interval is24hours
- **THEN** no S124 repeat is allowed before24hours, and it becomes eligible at24hours if all other conditions still hold.

#### Scenario: Interrupted fetching
- **WHEN** a drug-fetch job fails before ingestion
- **THEN** the dose interval does not start, supplies remain conserved, and retry does not repeatedly interrupt protected work or flood messages.

#### Scenario: Cancel after a partial drug exposure
- **WHEN** an admitted partial path commits half a drug dose before player cancellation and the player then resumes automation
- **THEN** Health shows that half-dose exposure, its effects/history remain, and no new automatic dose is allowed before the configured interval; reloading without further consumption changes neither quantity nor time.

#### Scenario: Partial stock cannot fill a whole-dose order by count
- **WHEN** only half a dose remains and no admitted path can collect the full usable quantity required by a new response
- **THEN** that response waits; the half-dose is preserved rather than consumed or reported as a whole dose.

### Requirement: Medical producers share one pending-dose decision
DR09. S124 and owned S134 automation SHALL share pending-dose decisions for each patient. The same
drug SHALL have at most one automatic pending administration or self-use; chemical-bearing drugs
SHALL additionally have at most one automatic pending dose per chemical. Different illness/pain
reasons for that dose SHALL be visible without multiplying it. Existing native/player/foreign bills,
including suspended bills, and known current ingestion SHALL block equivalent automatic duplication;
they SHALL NOT be resumed, reordered, deleted or adopted as owned orders. Distinct compatible drugs
can have separate pending doses, within the ordinary operations limit of15; physical ingestion
and scarce supplies cannot be promised simultaneously to multiple consumers.

S134's incapacitated unmet-addiction/dependency response SHALL take precedence over a competing
new S124 response for that chemical. It SHALL require its own current unmet need, patient eligibility,
doctor/surgery permission and addiction permission. A positive-frequency schedule alone SHALL NOT
block that S134 assistance when the incapacitated patient cannot satisfy the need. After a dose,
actual need and current effects SHALL be reevaluated before another producer acts. S124's waiting
periods do not redefine S134's biological need thresholds or grant S134 permission. Remaining S124
ties SHALL prefer pain, then condition responses in stable catalog order and slot order; UI search
or language changes SHALL NOT change that priority.

#### Scenario: Both producers find the same need
- **WHEN** an unconscious dependent patient qualifies for S134 and a configured S124 response would administer the same chemical
- **THEN** one eligible S134 dose is selected, the S124 reason does not add another bill, and later use depends on updated need and S124 history.

#### Scenario: Suspended manual bill
- **WHEN** a player has suspended an equivalent administer-drug operation
- **THEN** it stays suspended and prevents automatic duplicate ordering; neither owned producer bypasses it by selecting another drug for the same chemical.

#### Scenario: Two different condition drugs
- **WHEN** two configured responses use distinct chemicals, have available supply and both remain eligible
- **THEN** each can have its ordinary pending dose without crossing the bill limit, and every consumption rechecks current effects and reservations before the next.

### Requirement: Player cancellation and changing reasons remain effective
DR10. Pending owned doses SHALL retain their reasons and provenance. Before consumption, disappeared
conditions, withdrawn permission, changed selected drugs or failed compatibility admission SHALL
remove only that invalid reason; an unchanged shared valid reason can keep the dose. An unstarted
owned action with no reason SHALL be withdrawn. Already consumed effects and independent player
operations SHALL remain. If the player edits an owned bill, it becomes player-controlled; automation
SHALL preserve it and treat it as an existing operation.

Deleting or suspending an automatic-origin dose, including one edited/reordered into player control,
or canceling its self-use by a player order, SHALL
hold automatic responses for that patient's affected drug/chemical until explicit Resume in Health.
The hold SHALL cover both owned producers, survive reload and be visibly named. It SHALL NOT prevent
native schedules, manual orders or unrelated chemicals; current policy remains independently editable.
An automatic failure creates no player hold. Resuming an automation-held suspended bill SHALL require
the player to resume that bill too, or remove it; Resume automation alone cannot bypass it.

#### Scenario: Player deletes an automatic operation
- **WHEN** the player deletes a queued automatic drug operation
- **THEN** it is not recreated on the next scan, Health shows the corresponding response hold, and unrelated care continues.

#### Scenario: Shared reason disappears
- **WHEN** pain resolves but an unchanged permitted condition still justifies the same owned drug dose
- **THEN** the dose keeps only the condition reason; if no reason remains it is withdrawn before consumption.

#### Scenario: Player takes control of a bill
- **WHEN** the player changes an automatic bill's settings or order
- **THEN** it is retained as a player operation, later automation does not erase it on condition recovery, and equivalent automatic doses remain blocked.

#### Scenario: Reorder then delete
- **WHEN** the player reorders an automatic-origin bill and later deletes it while the triggering condition persists
- **THEN** its retained origin creates the same visible response hold as direct deletion; transferring control does not permit immediate automatic recreation.

### Requirement: Caravan responses conserve supplies and permissions
DR11. Eligible player-caravan colonists and separately enabled prisoners SHALL use only actual
carried available drug quantities through the ordinary caravan consumption path. Both response
slots can operate on a caravan without inventing a medical bed or remote map bill; the UI SHALL
explain that the map administration mode becomes caravan administration. The same trigger, policy,
history, holds, physiology and shared pending-dose checks SHALL apply. S124 SHALL defer both families
during caravan night rest and mental state. A supported ordinary caravan route must be able to
administer to the patient; otherwise the rule waits, especially for incapacitated patients. A carrier
can supply a dose but is not thereby enrolled as a patient. Map/caravan transfers SHALL not reset
history or replay an unconsumed map operation as a free dose.

#### Scenario: Pack animal carries medication
- **WHEN** an eligible colonist needs a configured response and a caravan animal carries the permitted drug
- **THEN** one actual unit is consumed from that inventory with ordinary effects, and the animal receives no S124 treatment.

#### Scenario: Night rest and map transfer
- **WHEN** the caravan rests at night or enters a map with a recently treated patient
- **THEN** night rest causes no S124 dose, and entering the map retains the consumed-dose interval and player holds.

### Requirement: Health explains automation without changing bill selection
DR12. The selected patient's Health view SHALL provide a contextual Drug responses status/control
showing relevant reasons, selected drugs, pending versus consumed doses, next eligible times,
blocking reasons and held responses with Resume. It SHALL link to the appropriate configuration
section and ordinary drug-policy/operations controls. Patient holds and actual operations are
save-local gameplay state, not global settings. Notifications SHALL distinguish an order queued
from a drug consumed, follow the configured family switches and combine simultaneous reasons for
one dose; unchanged blocking reasons SHALL NOT spam messages.

No Drug Response bottom tab SHALL be added. S137's condition-click recipe filter SHALL remain a
separate discovery interaction: showing an amputate-leg, remove-bionic-part or remove-quills recipe
SHALL NOT add a response rule, authorize a dose, clear a hold or schedule that operation. S007/S062
condition menus and the existing medical bill view SHALL remain usable together.

#### Scenario: Explain a blocked response
- **WHEN** the player inspects a patient's configured response with unavailable stock
- **THEN** Health identifies the selected drug and waiting reason without reporting successful treatment or repeatedly sending the same message.

#### Scenario: Click an infected foot
- **WHEN** S137 filters recipes after the player clicks a foot infection
- **THEN** a relevant ancestor amputation can appear under S137's rules, but S124 creates no amputation, response rule or drug order from that click.

### Requirement: Save and provider changes cannot duplicate treatment
DR13. Owned configuration, dose history, holds and still-valid pending-dose identity SHALL survive
save/load and map transfer. Loading SHALL reconcile real jobs/bills and actual inventory without
replaying consumed doses, resetting intervals or merging unrelated patients. Missing/changed optional
content SHALL deactivate only its affected responses with an explanation. Source settings/world
components SHALL NOT be automatically imported. Native existing bills remain ordinary operations.

Disabling an owned response family SHALL prevent new orders and withdraw its unconsumed owned-only
orders, retaining real supply, history, holds and other producers' valid reasons. Re-enabling SHALL
revalidate, not replay old queued work. Removal of the mod from a save is not a compatibility claim
of this draft; no external save rewriting or migration is included.

#### Scenario: Reload with a pending operation
- **WHEN** the player reloads with one owned queued dose and a saved hold for another chemical
- **THEN** there remains one pending dose, the hold remains effective, and no completed treatment is inferred from the queue.

#### Scenario: Disable pain while a condition still applies
- **WHEN** pain responses are disabled but the same pending dose still has a valid enabled condition reason
- **THEN** only the pain reason is removed; the condition dose can remain without being duplicated or losing its physical supply.

### Requirement: Incompatible integration leaves ordinary care available
DR14. Each enabled response integration SHALL be admitted only when its required game/provider
behavior is available. An incompatible or unknown target SHALL leave the affected integration or
declared dependent feature group inactive with one bounded startup warning identifying the failed
patch, reason, lost behavior and fallback. Compatible unrelated patches SHALL remain allowed.
An absent optional drug/illness provider SHALL contribute no choices or edits. A present changed
provider SHALL leave its affected rules unavailable without partially modifying content.

Active original Drug Response or Priority Treatment SHALL trigger a bounded startup warning and
the ordinary per-feature settings disable path for the owned overlapping ordering scope, with the
same effective off behavior as a user disabling it. This is best effort, not a cooperative
compatibility guarantee or source-specific scheduling handoff. No original API/binary/save parity
checks or facade are required; no external bill, setting or patch is changed. Ordinary
manual drug use, native policies and medical operations remain available. Required content and
provider checks SHALL preserve unrelated content additions and existing extensions.

#### Scenario: Compatible foreign observer
- **WHEN** another mod observes the same action without removing the semantics this feature needs
- **THEN** its presence alone does not disable the response or require an exact game-method match.

#### Scenario: Changed ingestion semantics
- **WHEN** the supported consumed-dose observation can no longer distinguish real consumption from an attempted order
- **THEN** dependent owned scheduling stays inactive with a startup warning instead of risking repeated dosing, while ordinary care remains available.

#### Scenario: Original installed alongside replacement
- **WHEN** original Drug Response is active with the megamod
- **THEN** startup reports the overlap and requests the ordinary settings disable path for the owned feature, best effort
- **AND** its ordinary off/withdrawal semantics apply without deleting source settings, source bills or foreign patches or promising their compatibility.
