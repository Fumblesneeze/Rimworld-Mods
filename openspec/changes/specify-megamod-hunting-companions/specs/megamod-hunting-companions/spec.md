# Bonded hunting companions

Owner **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`; target **1.7 beta**.
**Origin: original**, S065/P1-60. Specification only; nothing implemented or accepted in game.
All bonded Attack + Haul animals accompanying, one claimant per shot prey, finishing independently
of haulability and hunter-centered wandering are user decisions. Other precise policies below are
proposed defaults retained in HC-Q1–5; they are not additional interview answers.

## ADDED Requirements

### Requirement: Existing bonds and Attack plus Haul training qualify companions

HC01. All currently eligible colony animals bonded to a hunter SHALL be able to accompany that hunter using their existing completed Attack and Haul training.
There is no dog-only restriction, one-pet limit, new Hunting training or requirement for Odyssey's
separate Attack target training. Ordinary prerequisites, current trainability, training decay and
actual learned state remain effective; wanting training or showing partial progress is insufficient.
The feature does not create bonds, train animals, change species, remove injuries or grant capacity.

Proposed permission policy: use the animal's existing assigned master and Follow during fieldwork
choice. That master must be the bonded hunter, both must be player-faction and on the same map,
and fieldwork following must be enabled. Preserve existing master/follow choices and native defaults;
do not silently turn them on or reassign the animal. A trained bonded animal with different/no master
is available for assignment through ordinary animal controls, with the reason explained. These are
entity permissions, not extra training requirements or mod-settings entries for individual animals.

Dead, downed, incapacitated, mentally disturbed, unavailable/transported or explicitly occupied
animals do not acquire this work. Actual movement, allowed-area, access and provider restrictions
remain. An animal can serve only one current hunter and one finishing/retrieval target at a time.
Unbonded trained followers keep their normal following/defense behavior without gaining this feature.

#### Scenario: Several trained bonded animals
- **WHEN** a hunter has three bonded animals with Attack + Haul learned and current following permission
- **THEN** all three can accompany that hunter, without selecting only one favorite or requiring another trainable ability.

#### Scenario: Attack target is not required
- **WHEN** an otherwise eligible animal has ordinary Attack + Haul but lacks Odyssey's Attack target training
- **THEN** it can accompany, finish and retrieve under this contract without buying another expansion or training that separate ability.

#### Scenario: Bond and master differ
- **WHEN** a trained animal is bonded to one hunter but assigned to another master, or fieldwork following is off
- **THEN** it does not abandon that choice automatically and its contextual explanation identifies the existing assignment/follow permission to change.

#### Scenario: Training or physical eligibility changes
- **WHEN** Haul training is lost or the companion becomes downed, mentally disturbed or unavailable
- **THEN** it stops acquiring companion work, releases unsafe pending actions and preserves any actual carried load through ordinary recovery.

### Requirement: Accompany an actual undrafted hunt while preserving ordinary animal needs

HC02. Eligible companions SHALL accompany a hunter performing ordinary undrafted Hunt work or S064 station waiting, departure, pursuit and valid return work.
A station is not required for manual native Hunt work. Taming, mining, miscellaneous fieldwork,
drafted combat and S008 party hunting do not become companion-hunting episodes merely because
native followers are present. Native following/defense outside this feature remains unchanged.

Companions physically approach and remain near the hunter, using S066's shared directional idle
wandering with the hunter as the reference, not another animal. Proposed companion distance is
**3 cells**, separately configurable from station detection and confined animals' walk length.
Purposeful regrouping uses normal movement; no teleport, speed/stat bonus, flight/water capability
or release-to-attack command is granted. The hunter need not wait for every pet to catch up.
Short legal wandering or no movement is valid in cramped cells; purposeful jobs retain their routing.
S066's directional-style toggle does not disable hunting assistance: when that style is off or
unavailable, supported ordinary near-hunter movement can continue with these same permission and
needs rules. An unproven fallback cannot bypass required movement admission.

Critical needs, eating/rest when ordinarily required, medical rest, unsafe temperature, fleeing,
close threats, player orders, caravan/rope duties and other controlling native duties take precedence
over automatic accompanying. No idle promise can trap an animal away from food or sleep. Companion
recruitment/idle movement does not interrupt another real cargo delivery to obtain an empty carrier;
that work completes or recovers first. An animal can rejoin a still-valid hunt afterward.
While a controlling need/duty requires that break, companion following is temporarily suspended,
including competing native follow scheduling. The saved fieldwork-follow choice stays unchanged.
S030's otherwise-applicable environmental recovery can run during this suspension; its exclusion
for other animals actively following a master remains. Resume only after ordinary priorities permit.

#### Scenario: Ordinary hunt without a station
- **WHEN** an eligible hunter starts a normal Hunt job on a manually marked animal
- **THEN** permitted bonded companions approach and accompany the hunt without requiring a hunting spot.

#### Scenario: Wait at a station
- **WHEN** the hunter reaches a configured S064 station and waits
- **THEN** companions remain nearby with bounded directional idle walks, then follow the hunter's actual departure rather than claiming the seat or choosing their own live prey.

#### Scenario: Hunger and an enclosed companion
- **WHEN** one accompanying animal needs food while another cannot leave a one-cell enclosure
- **THEN** the first can take its ordinary eating work and the second waits/yields with a blocked reason, without endless searching, teleportation or immediate job churn.

#### Scenario: Draft or unrelated fieldwork
- **WHEN** the hunter is drafted into S008 party hunting or switches to taming/mining
- **THEN** no new companion finishing is authorized by this feature and the applicable ordinary follow/defense or drafted-hunting rules govern instead.

#### Scenario: Following permission stays on during a needed break
- **WHEN** a hunting companion yields for ordinary needs or eligible environmental recovery while its saved fieldwork-follow choice remains on
- **THEN** native following does not immediately recapture it, the permitted recovery/need action can proceed, and the saved choice remains on for later eligible rejoining.

### Requirement: Respond to the hunter's actually shot quarry becoming downed or dead

HC03. Companions SHALL acquire prey only from a current authorized hunting episode in which their hunter actually hits that episode's quarry with an admitted ranged hunting attack and it becomes downed or dies.
A missed/fully ineffective shot, collateral victim, nearby unrelated injury, merely selected animal
or old corpse does not qualify. An admitted non-projectile weapon can qualify through its actual
ranged hit; guessed attribution from proximity, a weapon name or a battle-log label is insufficient.
The target must remain revealed, on this map, a factionless wild animal and otherwise permitted by
the hunter's current hunt. No human, pet, tame-marked/protected animal or hidden target is added.

Proposed delayed-collapse policy: after a qualifying effective hit, a later downing/death of that
same quarry while the same authorized hunt remains active can trigger assistance, including bleeding
or another injury contributing. This authorizes help with current quarry; it does not award the hunter
exclusive damage/kill credit. Cancellation, retargeting or recovery clears that previous-hit authority.
After recovery, a fresh effective hunter hit is needed before another companion downing claim.
Ordinary retaliation during the hunt does not erase the authorized quarry merely because it becomes
aggressive. Before a new hunt, S064's initial aggressive-animal exclusion still applies.

Only the exact actual prey/corpse is considered. Repeated notices of the same downing/death are one
opportunity, not multiple jobs. Save/load preserves valid pending attribution without inventing hits.
Unknown provider attribution leaves ordinary hunter completion available with an explained limitation.

#### Scenario: Immediate ranged downing
- **WHEN** the hunter's admitted shot wounds its current quarry and it collapses
- **THEN** an eligible companion can claim that exact prey while nearby downed animals remain unrelated.

#### Scenario: Delayed bleeding collapse
- **WHEN** quarry previously hit by its still-active hunter later collapses from bleeding
- **THEN** it can trigger one assistance opportunity without falsely assigning the subsequent killing blow or all damage to the hunter.

#### Scenario: Miss, collateral hit or canceled hunt
- **WHEN** a shot misses, hits an unrelated animal, or a previously hit quarry collapses after its hunt was canceled
- **THEN** none creates a new companion finishing/retrieval order for that victim.

#### Scenario: Prey recovers and collapses again
- **WHEN** claimed prey recovers, then becomes downed again without a fresh qualifying hunter hit
- **THEN** the old companion finishing authority is not revived; the hunter's ordinary valid work can continue.

### Requirement: Transfer one current finishing or retrieval claim

HC04. A qualifying opportunity SHALL select at most one available eligible companion for the prey, with no concurrent owned hunter/companion finishing or carrying action on it.
Proposed claimant order is nearest legal approach to the prey by ordinary route distance, with stable
ties. A living downed prey needs a usable melee attack and safe permitted approach; haul capacity or
a storage destination is not required for that finishing choice. For already-dead prey, select only
a companion able to perform the actual permitted retrieval. Other companions continue accompanying.

Transfer the current hunter's compatible prey/corpse claim only when the companion can actually
start. An unrelated worker's reservation/order is not stolen. If transfer/start fails, release only
the attempted companion claim and let ordinary hunter handling continue. Once transferred, the
hunter stops its new shots/finishing/pickup for that prey; already fired projectiles retain normal
effects. Other colonists and foreign combat systems are not magically made safe to approach.

If the claimant becomes unavailable, another currently eligible companion can take over only after
the old claim is ended and the real target/load is reconciled. Proposed pre-pickup assistance budget
is **2 in-game hours** from the first claim, shared across replacements. On expiry or when no useful
claimant exists, release the handoff to ordinary hunter/hauling work. Do not renew this budget by
reassigning animals, reopening UI, loading a save or retrying the same opportunity. A failed candidate
is not immediately retried in a loop without relevant changed circumstances.

#### Scenario: Three companions reach one prey
- **WHEN** three permitted animals can approach the hunter's newly downed quarry
- **THEN** one receives the current claim and the hunter/others do not start competing finishing or retrieval jobs.

#### Scenario: Ordinary reservation cannot transfer
- **WHEN** the exact prey or corpse is claimed by an unrelated incompatible job
- **THEN** the handoff does not remove that reservation or leave the hunter suppressed without a valid companion.

#### Scenario: Companion is interrupted before pickup
- **WHEN** the claimant loses eligibility and another permitted companion is available
- **THEN** the old claim ends before a revalidated replacement starts, retaining the original assistance deadline and one physical target.

#### Scenario: Assistance cannot make progress
- **WHEN** every permitted approach fails or the shared pre-pickup deadline expires
- **THEN** companion assistance ends for that opportunity with a contextual reason and ordinary eligible work can recover the prey/corpse without an immediate retry cycle.

### Requirement: Reach downed prey and finish through the animal's actual attacks

HC05. The claimed companion SHALL move promptly to still-authorized downed prey using ordinary purposeful movement and use its actual melee attacks until the prey dies, recovers or loses authorization.
There is no numerical movement/damage bonus, instant remote kill, human execution animation,
automatic 180-tick human hunting execution, S008's 60-tick Finish off substitution or free skill/training.
Normal available body tools, reach, attack cooldowns, misses, armor, damage and body-part loss apply.
The resulting corpse/yield can therefore differ from careful human execution; preserve the actual
ordinary result instead of repairing wounds or granting a slaughter-quality yield bonus.

Proposed hazard policy: a living prey whose death is dangerous in melee is not automatically finished
by a companion. The ordinary hunter retains/reclaims its appropriate ranged handling; an actual
corpse can later be retrieved if ordinary safety, access and capacity permit. This is not a claim
that all other prey or incoming gunfire is safe. The animal does not deliberately eat this quarry
as part of its finishing/retrieval job; urgent feeding remains a separate normal action.

Revalidate immediately before every new attack. A recovered mobile animal is not chased or attacked
under the stale downed-prey claim. Ownership/taming/protection, forbidden/allowed-area changes,
station filter/pause cancellation and loss of revealed target information stop new authorized blows.
Ordinary self-defense remains separate. If the prey dies during approach, skip finishing and handle
its actual corpse once; if no usable corpse remains, end without fabricating one.

#### Scenario: Finish without a destination
- **WHEN** a companion can safely reach and attack downed prey but no storage accepts its corpse
- **THEN** it can still finish the prey, and the real corpse remains for ordinary later handling.

#### Scenario: No guarantee of a clean one-hit kill
- **WHEN** the animal's first ordinary melee attack does not kill the downed prey
- **THEN** subsequent attacks use normal cooldowns and current eligibility, preserving actual wounds, body loss and eventual corpse yield.

#### Scenario: Dangerous death or no usable melee attack
- **WHEN** prey is dangerous to kill in melee or the candidate has no usable melee attack
- **THEN** that animal does not receive automatic finishing authority and ordinary permitted hunter handling remains possible.

#### Scenario: Recovery or protection before the next bite
- **WHEN** the prey stands, becomes tame/protected, or is disallowed before another attack
- **THEN** the companion stops new finishing attacks without replaying a queued killing blow or removing the protective choice.

#### Scenario: Prey dies before arrival
- **WHEN** an already-fired shot kills the claimed prey before the companion arrives
- **THEN** the animal performs no extra finishing action and considers only the actual remaining corpse for retrieval.

### Requirement: Retrieve one physical corpse under real carrying and storage rules

HC06. After death, the claimant SHALL physically retrieve the actual corpse only when it has current hauling ability, a legal carrying route, native/composed capacity and an accepting destination.
Recheck corpse identity, reservation, current permission, Manipulation/physical capability and
destination demand before pickup and placement. Normal hunting can unforbid its newly created
corpse once; a later player forbid remains effective. No adjacent items become allowed, no live
prey is carried instead, and no butchering, extra meat/leather or duplicate corpse is generated.

Ordinary single-load carrying and pack-inventory batching are distinct. A trained non-pack animal
can use native single-load hauling when its actual carrying-volume limit permits one whole corpse;
zero pack-inventory capacity alone does not prohibit that path. S021 applies its real combined limits
if a batch route is used, and existing owned cargo must be delivered or safely unloaded before an
ordinary fallback that would otherwise add a second oversized load. No additional inventory or
capacity multiplier is granted. Corpse-batching preferences do not disable ordinary single-load
retrieval, and one corpse cannot be split across several animals.

Automatic corpse storage delivery uses S049's current shared demand, filters, priorities, incoming
claims and physical limits. A satisfied target is not a destination merely because a hunting job
created the corpse. No valid destination or insufficient capacity leaves the actual corpse in place
with no carrying attempt; another ordinary eligible worker can handle it. If the finisher cannot
haul, a currently eligible companion can receive the single retrieval claim within the remaining
assistance budget; otherwise release it. Do not tie finishing eligibility to this later outcome.

#### Scenario: A hauling dog has no pack inventory
- **WHEN** a trained bonded dog can natively carry the corpse as one load but has no pack-inventory capacity
- **THEN** it can retrieve through ordinary single-load hauling without gaining an inventory or being rejected solely by the batch-mass test.

#### Scenario: Finisher cannot carry
- **WHEN** the finisher kills prey whose corpse exceeds its actual carrying limit
- **THEN** no partial/phantom load is created; one other eligible retriever can take over, or the corpse stays available to ordinary work.

#### Scenario: Storage has no remaining demand
- **WHEN** S049's destination limit is satisfied before pickup
- **THEN** the animal does not collect the corpse for that destination or override the limit because it helped hunt it.

#### Scenario: Already carrying a batch
- **WHEN** an animal still holds owned batch cargo and the corpse needs ordinary fallback
- **THEN** the cargo is delivered or safely unloaded first, and the animal never receives a second combined-capacity allowance.

#### Scenario: Player forbids the corpse
- **WHEN** the player forbids the actual corpse after hunting's initial unforbid action
- **THEN** further automatic pickup respects that choice instead of repeatedly unforbidding it.

### Requirement: Complete or cancel assistance without stale return orders

HC07. A successfully transferred assistance job SHALL be able to finish its exact prey/retrieval while the hunter resumes ordinary work, without requiring the hunter to stand idle beside it.
Natural completion of the human hunt for a successful handoff is not itself cancellation. The
companion keeps the originating hunt/station permissions, target identity and remaining deadline;
the hunter starting a later hunt does not replace the animal's current target. After delivery or
released assistance, it reassesses normal needs/orders and rejoins its current still-eligible hunter
only if companion work remains appropriate. Otherwise it returns to ordinary animal behavior.

Explicit hunter interruption/drafting, disabling fieldwork following, master/bond/faction change,
station pause/removal or revoked target permission stops new assistance attacks/pickups. Losing
the hunter to death, downing, mental state, despawn or a different map likewise revokes new work.
For a manual Hunt, canceling its living prey's Hunt order revokes the delegated finishing authority;
ordinary removal of that designation because the prey died does not cancel its valid corpse retrieval.
Training loss, animal interruption and forbidden/inaccessible routes are rechecked, not hidden by
an old claim. These events never cancel unrelated native commands or erase normal bond/death effects.

An animal already holding the corpse SHALL complete lawful delivery or use ordinary safe physical
cargo recovery. Destination change, incapacity, death, faction/map change or interrupted delivery
must leave one actual recoverable item under the game's current holder/placement rules. Do not
respawn a copy at the prey's old location, delete cargo, force a cross-map return or take items from
a new owner. Post-pickup transport follows ordinary recovery rather than dropping/deleting cargo
just because the pre-pickup assistance deadline expires.

#### Scenario: Hunter begins another job after handoff
- **WHEN** the companion has successfully taken the first quarry's claim and the hunter's hunt completes normally
- **THEN** the companion can finish that authorized retrieval while the hunter works, without switching to a newly selected prey or keeping the hunter idle.

#### Scenario: Draft during approach
- **WHEN** the player drafts the hunter while the companion approaches living downed prey
- **THEN** no new companion finishing blow occurs from that canceled hunt and no delayed rejoin overrides the drafted command.

#### Scenario: Cancel the manual prey order after handoff
- **WHEN** the player cancels the original Hunt order while the delegated prey is still alive
- **THEN** the companion stops new finishing attacks even if the hunter has naturally moved on to another job; routine designation cleanup on death instead preserves valid corpse retrieval.

#### Scenario: Destination changes while carrying
- **WHEN** storage becomes full/ineligible after pickup
- **THEN** the actual corpse is rerouted or physically recovered through normal permitted handling, without duplicate cargo or an artificial increase in capacity.

#### Scenario: Companion or hunter dies
- **WHEN** the companion dies carrying its quarry, or the hunter dies while the animal is assisting
- **THEN** normal death/bond consequences remain and the actual cargo follows ordinary physical recovery; no old return or attack authority survives.

### Requirement: Saved assistance reconciles with the real prey and carrier

HC08. Save/load SHALL preserve current companion permission, valid hunting-hit context, claim/phase, exact prey or corpse identity and remaining assistance deadline without repeating completed actions.
Revalidate the hunter, animal, bond/training/follow choices, map, originating station/hunt permission,
actual target/holder and reservations on load. A living prey, loose corpse, carried corpse and
delivered corpse are different states; loading must not kill twice, drop a copy, reset a deadline
or steal a completed delivery back. Recovery clears prior-hit authority as specified even across saves.

Missing identities, unloaded maps, removed content or rejected required integration release stale
intent safely and retain real physical state. Another save never inherits previous-game animals,
claims or configuration. A saved pending attack under a now-disabled feature is not replayed.
Native bond/training/master data is not replaced by a duplicate companion-owned truth.

#### Scenario: Save during approach and transport
- **WHEN** one game is saved during approach and another while a companion carries the corpse
- **THEN** each resumes only its still-valid actual phase, preserving deadlines and one target/load without replaying death or pickup.

#### Scenario: Missing or conflicting target after load
- **WHEN** the saved target is gone or the corpse is already delivered/held by another legitimate owner
- **THEN** stale assistance ends without creating, retrieving or reassigning a replacement item.

#### Scenario: Integration is unavailable on load
- **WHEN** a saved companion job's required behavior can no longer be admitted
- **THEN** its new attacks/pickups remain inactive, claims release safely and actual animals/cargo remain available to ordinary recovery.

### Requirement: Use contextual animal controls and common inherited tuning

HC09. The existing Animals management and selected-animal training surfaces SHALL remain the places to assign masters and choose fieldwork following, with companion eligibility and current activity explained contextually.
Show the bonded hunter, missing training, disabled following, incompatible assignment, blocked route,
current prey/retrieval and unavailable integration only when applicable and player-observable. Avoid
global alerts for every unavailable companion, hidden injury attribution, a new bottom tab, or mod
settings rows for individual animals. Existing non-companion controls continue to work.

Common Hunting / Companions settings SHALL provide feature availability default **on**, companion
distance default **3 cells**, and pre-pickup assistance budget default **2 in-game hours**. The distance
and budget accept positive finite values; invalid/overflowing input retains the previous value with
a reason. Distance changes govern subsequent idle/regroup decisions, not a chase/pickup radius; budget
changes govern new opportunities without refreshing an existing deadline. Turning availability off
revokes new assistance and performs safe cancellation/recovery; turning it on invents no past shot.

Every setting uses Global default, Current save and Use global through the common load/store/render
path. New/missing saves inherit; checking Use global retains a dormant override and disables its
editor. Unchecking restores it or seeds the first override from the current global value. Main-menu
save controls and every save mutation callback remain disabled. All controls/status/explanations
support the seven baseline languages. S066 consumes the same companion distance; its pen/room walk
length is a separate setting and cannot silently replace it.

#### Scenario: Change animal permissions
- **WHEN** the player changes the animal's master or fieldwork-follow choice in its normal controls
- **THEN** eligibility and pending assistance update with an explanation, without changing another animal or a global setting.

#### Scenario: Global default and save override
- **WHEN** one save overrides companion distance while another inherits a changed global default
- **THEN** each uses its own effective distance, dormant values survive Use global, and neither edit changes another save or an existing assistance deadline.

#### Scenario: Main menu and feature disable
- **WHEN** settings are opened with no loaded save, or companion availability is disabled in a loaded save
- **THEN** the main menu cannot mutate save state, while the loaded save stops new assistance safely without deleting carried corpses.

### Requirement: Compose one physical workflow with admitted native and optional behavior

HC10. Companion behavior SHALL compose directly with S064 station authority, S066 wandering, S021 carrying and S049 destination demand, sharing current claims and native/common map data with S033 search.
No separate source-mod caches or patches between our own features are introduced. S008 remains the
owner of drafted party attacks/finishing; this feature grants no automatic drafted retrieval mode.
Ordinary animal self-defense, native training/needs, movement, actual damage and corpse results remain
effective. A companion kill is not falsely rewritten as a human execution or counted twice; preserve
one hunt outcome and the actual participants' damage/death consequences. S117/S118 slaughter changes
apply only to their deliberate slaughter job, not this animal finishing action.

Compatible animal/weapon/training/storage/map modifications SHALL remain usable when the required
behavior is supported. Another patch's presence alone is not incompatibility. Unknown or changed
required behavior leaves the affected path inactive with a contextual reason and one bounded startup
warning per skipped enabled patch naming its target requirement, failure, unavailable behavior and
fallback. Normal native hunting/animal behavior and real held cargo remain. No foreign patches are
removed, no unsupported saved attack is replayed, and no all-provider or source-mod drop-in claim is
made. Actual XML changes preserve unrelated animal behavior and content rather than replacing a tree.

#### Scenario: Native hunter completes when assistance cannot start
- **WHEN** attribution, permissions or the full handoff/recovery path cannot be established
- **THEN** the companion route stays inactive with the required explanation and ordinary permitted hunter completion remains available.

#### Scenario: Compatible species and unsupported weapon integration
- **WHEN** a compatible modded animal has the required bond/training, or a custom weapon's actual hit attribution is unknown
- **THEN** the animal can participate on supported paths, while the unknown weapon path gets explained fallback rather than guessed credit or global disabling of unrelated hunting.

#### Scenario: Slaughter bonuses and drafted finishing remain separate
- **WHEN** a companion kills downed hunted prey while S117/S118 slaughter changes and S008 Finish off also exist
- **THEN** its actual animal attacks and corpse outcome apply without a human slaughter yield bonus, execution timer substitution or duplicate killing/transport.
