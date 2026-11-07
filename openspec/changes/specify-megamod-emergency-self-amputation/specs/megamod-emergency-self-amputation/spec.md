# Emergency self-amputation

Owner: **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`. Target **1.7 beta**.
Origin replacement S056/P1-50, Cut It Off. Observable draft only; nothing implemented or verified.
Choices correcting or extending source behavior are proposals recorded in research, not additional
confirmed user preferences. Values below are selected draft defaults for later balance review.

## ADDED Requirements

### Requirement: EA01 Emergency eligibility follows the current pawn and map
The feature SHALL offer emergency self-amputation to a spawned player-controlled humanlike colonist
who is alive, not downed, able to be awake, move and manipulate, capable of medical and caring work,
has Doctor work and self-tending enabled, permits medical care, and has an eligible infected part.
No other spawned player-controlled humanlike on this map may be able to provide the required
medical help now. Check actual work/caring ability, assigned Doctor work, required skill, medical
permission, reachable patient and required accessible supplies. A sleeping helper counts if ordinary
medical work can wake them; an unreachable, incapable or Doctor-disabled pawn does not count merely
because they exist. A child or slave counts only when genuinely eligible to help. Held/other-map
pawns, visitors, prisoners and animals do not count as an available colony medical helper.
Recheck help before confirmation and before the irreversible outcome. Never schedule self-amputation
automatically or rewrite another pawn's assignments to make them count.

#### Scenario: Unavailable other colonists
- **WHEN** an otherwise eligible infected colonist has only downed, Doctor-disabled or unreachable potential helpers
- **THEN** their mere presence does not block the emergency action.

#### Scenario: Reachable sleeping doctor
- **WHEN** another assigned capable doctor can be awakened through ordinary work and can reach and treat the patient
- **THEN** ordinary medical help is available and the emergency self-amputation action is withheld.

#### Scenario: Child is not a doctor by mere presence
- **WHEN** the only other local colonist is a child who cannot perform the required care
- **THEN** that child does not block the action; a genuinely capable and available child or slave would.

#### Scenario: Help becomes available during work
- **WHEN** an actual eligible helper arrives before the irreversible step
- **THEN** the attempt safely cancels with intact surviving supplies and no self-inflicted outcome.

#### Scenario: Actor cannot care for themself
- **WHEN** self-tend or Doctor work is off, medical work is unavailable, care is prohibited, or the pawn loses consciousness, movement or manipulation
- **THEN** a new attempt cannot start and its unavailability reason is visible when inspecting the action
- **AND** the feature does not change those policies to qualify the pawn

#### Scenario: Ordinary selection does not start surgery
- **WHEN** an eligible pawn is selected, including a player-controlled slave
- **THEN** the command is offered without starting work or altering health, medicine, bills or queued orders

### Requirement: EA02 Only a chosen infected expendable part is removed
The feature SHALL allow selection only of a present natural outside limb or limb segment/digit,
eye, ear or nose directly affected by a wound infection whose removal is not intrinsically fatal
for that anatomy. Core/vital/internal parts, implanted replacements and healthy parts are excluded.
The default is the most severe eligible infection, with ties in the stable displayed body order;
the player can choose another eligible infected part. Losing remaining mobility/manipulation and
bleeding after removal remain possible. An ineligible worse infection does not hide eligible ones.

#### Scenario: Worse infection cannot be amputated
- **WHEN** a core-part infection is more severe than an infected removable leg
- **THEN** the leg remains an offered choice and the core part is excluded

#### Scenario: Player chooses a different infected part
- **WHEN** two eligible infected parts exist and the player selects the less severely infected one
- **THEN** the action identifies and later acts on that exact part without switching to the default

#### Scenario: Healthy or vital part is requested
- **WHEN** a healthy part, internal organ, vital part or implanted replacement is inspected
- **THEN** this feature offers no removal action for it

#### Scenario: Anatomy does not establish a safe category
- **WHEN** a custom body part has a similar name but its anatomy does not establish the allowed removal behavior
- **THEN** it is not offered merely because its name resembles an ear, nose or limb

### Requirement: EA03 Contextual confirmation exposes the irreversible choice
The feature SHALL use the selected pawn's contextual commands, with a confirmation showing the
specific part, infection, medicine unit or no-medicine choice, chance, work duration, lasting part
loss and possible bleeding, incapacity or death. Confirming interrupts current work and clears
queued orders, as stated in the confirmation; cancelling changes neither. No bottom tab, mod-settings
health editor or debug-only override is introduced. Ordinary health and order controls remain usable.

#### Scenario: Review and cancel
- **WHEN** the player opens the command then cancels its confirmation
- **THEN** no job, queued order, medicine, body part or memory changes

#### Scenario: Confirm the emergency
- **WHEN** the player confirms the displayed choice while its conditions still hold
- **THEN** the pawn stops current work, clears queued orders and begins that one attempt
- **AND** the selected part and progress remain inspectable during work

#### Scenario: Choice changes while confirmation is open
- **WHEN** the selected part, medicine availability or displayed chance changes before confirmation
- **THEN** the action requires an updated confirmation or reports why it is unavailable rather than silently accepting different terms

#### Scenario: No alternate control surface
- **WHEN** the player looks for this action in ordinary play
- **THEN** it uses the eligible pawn's contextual command and adds no bottom tab, global health editor or view-toggle button

### Requirement: EA04 Medicine choice obeys policy and preserves real quantities
The feature SHALL choose one allowed medicine unit from the pawn's inventory first, otherwise the
nearest reachable, accessible, unclaimed map medicine; equal proximity choices prefer higher potency,
then a stable item order. It respects the pawn's care category and forbidden/access rules. The selected
medicine supplies the chance and cost regardless of where it is carried. No-medicine care or no
available permitted supply permits a specifically confirmed unmedicated attempt; No Care forbids
the action. A lost selected medicine never silently becomes a different or unmedicated attempt.

#### Scenario: Inventory and map medicine agree
- **WHEN** otherwise identical pawns use one allowed medicine unit of the same potency from inventory or from the ground
- **THEN** both see the same chance and each completed attempt consumes exactly one unit from its chosen supply
- **AND** unrelated medicine and the rest of a stack remain intact

#### Scenario: Care category limits the choice
- **WHEN** herbal care is selected and only industrial medicine is present, or no-medicine care is selected with medicine nearby
- **THEN** no disallowed medicine is selected or consumed and the confirmation identifies an unmedicated attempt

#### Scenario: No Care differs from no medicine
- **WHEN** the pawn permits no medical care
- **THEN** neither a medicated nor an unmedicated attempt starts

#### Scenario: Access and competition matter
- **WHEN** a closer medicine is forbidden, unreachable or committed to another action and a farther permitted unit is available
- **THEN** the farther unit is selected without stealing the closer claim or teleporting a unit

#### Scenario: Chosen medicine disappears
- **WHEN** the selected unit is lost or becomes prohibited before the attempt resolves
- **THEN** the attempt cancels safely without consuming a substitute or injuring the pawn
- **AND** a new explicit order is needed to try with different medicine or none

### Requirement: EA05 Work and chance are visible and bounded
Each attempt SHALL require1.92 game hours of work after collection. Without medicine, success SHALL
interpolate linearly from **50% at Medical0** to **90% at Medical20** (70% at10), clamping skill to0–20.
These more forgiving selected draft defaults replace13%/19.5%/26%; actual playability remains unverified.
With medicine of positive finite potency, success SHALL be the greater of:
- the unmedicated chance plus **10 percentage points times potency**; and
- the source-reference medicine result, **65% times potency times (1 +0.05 times Medical skill)**.
Limit final chance to0–100%. Thus medicine never worsens a no-medicine attempt or the inspected source
medicine chance, and a useful medicine gives a positive benefit until the cap. Invalid potency is
not a usable medicine choice. Chance is captured at work start after confirmed terms are checked.

No bed, anesthetic, new tool or research is required, and no extra skill experience is awarded.
Do not add another normal surgery-success roll or bed/room/work-speed bonus. Even a100% success
roll retains actual removal, bleeding, lost capacity and possible physiological death consequences.

#### Scenario: No medicine at low neutral and mastered skill
- **WHEN** Medical0,10 or20 confirms an unmedicated attempt with default tuning
- **THEN** the displayed success chances are50%,70% or90%, with no medicine consumed.

#### Scenario: Medicine is never a penalty
- **WHEN** a Medical10 pawn uses herbal potency0.6, industrial1 or glitterworld1.6 with default tuning
- **THEN** success is76%,97.5% or100% respectively, and exactly one selected medicine unit is consumed.

#### Scenario: Beginner medicine and bounded expert chance
- **WHEN** Medical0 uses herbal potency0.6 or a Medical20 pawn uses industrial potency1
- **THEN** chances are56% and100%, with no negative or greater-than100% probability.

#### Scenario: Time and external progress change
- **WHEN** a confirmed attempt starts, then skill changes during its work or game speed changes
- **THEN** its chance stays the shown work-start value and work still takes 1.92 game hours
- **AND** no extra medical experience is awarded for starting or completing the attempt

#### Scenario: Surgery facilities do not multiply the formula
- **WHEN** the same pawn and medicine attempt this action near or in different beds or rooms
- **THEN** this emergency chance and work duration do not gain bed, room or surgery-speed bonuses

### Requirement: EA06 Completed outcomes match actual health
A completed attempt SHALL spend its agreed medicine once and resolve one outcome. A successful
roll removes the selected part and its ordinary attached descendants, with the game's normal health,
bleeding and capacity consequences and no harvested products or automatic aftercare. A failed roll
inflicts one ordinary random-part cut injury of 15–29 damage before normal damage mitigation; it can
injure another part, incidentally sever a part or kill. The feature does not cure other infections,
protect against death, repeat damage or reroll when an outcome cannot be confirmed.

#### Scenario: Successful removal
- **WHEN** a completed attempt succeeds and native removal leaves the pawn alive
- **THEN** the chosen part is visibly missing, dependent parts and infection follow normal removal consequences, and no harvested item appears
- **AND** remaining injuries, infections and bleeding still need ordinary care

#### Scenario: Failed attempt causes injury
- **WHEN** a completed attempt fails its chance
- **THEN** one ordinary 15–29 damage cut is resolved on a normally selected body part and the result is reported as a failed attempt
- **AND** survival or intactness of the target is not guaranteed

#### Scenario: Last working limb is lost
- **WHEN** a successful attempt removes the pawn's last useful moving or manipulating limb
- **THEN** resulting incapacity or downing occurs normally and is not undone to preserve initial eligibility

#### Scenario: Death or unconfirmed removal
- **WHEN** an attempted outcome kills the pawn, or the intended successful removal cannot be established
- **THEN** feedback describes death or the unconfirmed result and never claims survival after successful removal
- **AND** no automatic second damage application, refund, restored limb or duplicate attempt occurs

### Requirement: EA07 Memories and tales describe the living outcome once
The feature SHALL grant a living successful amputee a nonstacking +30 mood memory lasting 3 days
and one survival tale, removing the opposite failure memory. A living pawn after a failed roll
receives a nonstacking -18 mood memory lasting 0.5 day and loses the success memory. Repeating the
same living outcome refreshes its duration, not its magnitude or stack count. Dead pawns, cancelled
attempts and unconfirmed outcomes receive no new survival/failure memory or survival tale. A pawn
without a mood-memory system can still have the physical outcome without a fabricated mood effect.

#### Scenario: Living success is recorded
- **WHEN** the selected infected part is successfully removed and the pawn is alive
- **THEN** its success message, +30 three-day memory and one survival tale agree with the missing part
- **AND** an earlier failure memory is removed

#### Scenario: Living failure replaces success memory
- **WHEN** a later completed attempt fails and the pawn lives
- **THEN** it receives the -18 half-day failure memory, loses the success memory and receives no new survival tale

#### Scenario: Repeated outcome or no mood system
- **WHEN** another living success or failure occurs, or the pawn has no mood-memory system
- **THEN** an existing matching memory is refreshed without stacking, or no mood effect is added when the system is absent
- **AND** a physical result never depends on successfully creating a memory

### Requirement: EA08 Cancellation and saves preserve one explicit attempt
The feature SHALL recheck clinical eligibility, the chosen infection/part and medicine permission
before work and before irreversible resolution. Becoming downed, losing necessary capacities,
leaving the map, losing self-tend/Doctor/care permission, another capable colonist arriving, or the
infection resolving cancels an unfinished attempt. Manual cancellation is allowed. Before resolution,
cancellation causes no injury, medicine consumption or outcome memory; it releases claims and keeps
collected medicine physically accounted for. Reordering starts full work again. Loading a valid
saved active attempt resumes its same part, medicine choice, work progress and fixed chance; invalid
saved work cancels safely. Saving and loading never duplicates resolution within that saved attempt.

#### Scenario: Infection resolves or help arrives
- **WHEN** another capable colonist arrives or the chosen infection disappears while work is underway
- **THEN** the unfinished attempt ends without removing a part, spending medicine or awarding an outcome

#### Scenario: Manual interruption preserves medicine
- **WHEN** the player cancels before completion after the pawn has collected a medicine unit
- **THEN** the unit remains physically present and correctly attributed, other claims are untouched and a later order requires full work again

#### Scenario: Save during work
- **WHEN** a valid halfway-complete attempt is saved and reloaded
- **THEN** it resumes halfway on the same infected part and medicine terms with the same chance
- **AND** completion consumes at most one unit and grants only one outcome for that attempt

#### Scenario: Saved job loses its conditions
- **WHEN** a saved attempt cannot safely resume because its part, chosen medicine, care permission or supported feature behavior is unavailable
- **THEN** it cancels with a useful explanation, preserves actual health and items, and does not silently choose another part

### Requirement: EA09 Shared medical behavior and unavailable features remain coherent
The feature SHALL use current self-tend and medical policy without taking over their controls,
and share medicine quantities and claims with hauling and other care. If it cannot safely operate,
no owned emergency action starts and ordinary care remains available under its existing rules.
A bounded startup warning identifies an enabled unavailable action, reason and fallback. With the
original Cut It Off active, our overlap is inactive with a warning while the original remains
untouched. No source identity impersonation or automatic foreign-save import is performed. EA10
uses the common settings menu rather than adding a separate tuning window.

#### Scenario: Automatic self-tend changes its flag
- **WHEN** automatic self-tend or the player changes the pawn's self-tend policy
- **THEN** eligibility reflects the actual flag without resetting it to force eligibility

#### Scenario: Medicine is also haul cargo
- **WHEN** a chosen medicine unit belongs to a shared haul load
- **THEN** taking or consuming it updates the same physical quantity and claim once, leaving no phantom delivery or duplicated unit

#### Scenario: Original or unavailable feature
- **WHEN** the source mod is active or this enabled feature cannot safely support the current game
- **THEN** the owned command does not duplicate or partially perform surgery and one useful startup warning explains the reason and ordinary-care fallback
- **AND** foreign commands, health state and saves are not rewritten

### Requirement: EA10 Chance tuning uses common scoped settings
The feature SHALL expose Unmedicated success at Medical0 (default50%), Unmedicated success at
Medical20 (default90%), and Minimum medicine benefit per potency (default10 percentage points)
in Medical / Emergency self-amputation settings through the common Global default / Current save /
Use global path. The two endpoints form one paired curve setting with one inheritance choice; the
medicine benefit has its own row. Endpoints accept0–100% with the upper-skill endpoint at least the
lower; medicine benefit accepts1–100 percentage points. Validate each complete curve on edit/load;
malformed persisted curves fall back as a complete pair to50/90 with a bounded diagnostic. Invalid
edits leave the previous valid configuration. A save cannot inherit only one endpoint.
New saves inherit, inherited editors are inactive, dormant overrides survive, and main-menu save
controls/callbacks are disabled. Changes govern future work-start chances, not active attempts.

#### Scenario: Inherited setting changes during an attempt
- **WHEN** the global curve changes while one save inherits the curve and another overrides the complete pair
- **THEN** future attempts use each save's effective curve, but an already started attempt retains its confirmed chance.

#### Scenario: Invalid curve or main-menu edit
- **WHEN** the player enters a mastered endpoint below the novice endpoint or tries a save edit from the main menu
- **THEN** the mutation is rejected without changing active chances or another save.


#### Scenario: Global edit cannot invert a saved curve
- **WHEN** global endpoints change from50/90 to50/60 while a save overrides the complete curve with80/90
- **THEN** that save keeps80/90, an inheriting save uses50/60, and no mixed80/60 curve is produced.

#### Scenario: Zero medicine benefit is rejected
- **WHEN** the player enters0 for the minimum medicine benefit
- **THEN** the prior valid positive value is retained, preserving a positive benefit for usable medicine below the cap.
