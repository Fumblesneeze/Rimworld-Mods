# Outdoor breaks

Sole owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
Source S030. Proposed behavior only; native acceptance remains open.

## ADDED Requirements

### Requirement: Low outdoors need can lead to a safe outdoor break
The mod SHALL offer an outdoor break by default to an awake, mobile, undrafted player-controlled
nonslave colonist whose outdoors need is at or below **20%**, who is below the break's **75%**
goal, and whose ordinary schedule permits recreation. The start threshold is configurable
**1–100%** through the common environmental settings. Hunger, configured low-recreation
interruption, medical rest, emergency/player control and active qualifying environmental recovery
take precedence. No animal, prisoner or pawn without an outdoors need is given this break.
This is an ordinary short activity, not a mental break or unrestricted schedule override.

#### Scenario: Qualifying low outdoors need
- **WHEN** a colonist at 20% outdoors need is otherwise eligible during recreation-permitted time
- **THEN** an available safe outdoor activity can be chosen before ordinary work or idle wandering
- **AND** a pawn above the configured start threshold is not sent out by this rule

#### Scenario: High configured threshold and reached goal
- **WHEN** the start threshold is above 75% and the pawn already has at least 75% outdoors need
- **THEN** no new break is created merely to end immediately and be selected again

#### Scenario: Unsafe weather or more urgent behavior
- **WHEN** a proposed break would expose the pawn to unsafe temperatures, toxic surroundings, blood rain, vacuum or another admitted hazard, or urgent/recovery behavior takes precedence
- **THEN** it does not begin or displace the more urgent behavior
- **AND** disabling a hazard's automatic recovery does not itself make exposure safe

### Requirement: Choose an activity that can actually be performed safely
The mod SHALL choose between a physically reachable outdoor walk and skygazing, considering
only feasible activities. When both are feasible it retains the source weighting of **45 chances
out of 99 for a walk**, otherwise skygazing; when only one is feasible it uses that one. Both
require suitable unroofed outdoor cells and ordinary physical access. Walk routes and skygazing
locations must avoid known applicable hazards. These breaks retain the source's allowed-area
exemption, independent of the recovery outside-area setting, but never bypass locked doors or
grant movement abilities. No feasible activity means no break, with the same bounded no-route
retry rule as environmental recovery.

#### Scenario: Skygazing possible without a walking circuit
- **WHEN** there is a safe reachable skygazing cell but no valid outdoor walking circuit
- **THEN** the pawn can skygaze without first requiring a walking route

#### Scenario: One reachable outdoor area
- **WHEN** a safe outdoor activity is outside the pawn's allowed area but physically accessible
- **THEN** the break can use it without editing the allowed area or assignment
- **AND** a locked exit, known hazardous route or missing movement ability still prevents that choice

### Requirement: Breaks have explicit duration, need gains and interruption
The mod SHALL end an outdoor break when outdoors need reaches **75%**, its route/activity ends,
its duration expires or ordinary hunger, schedule, emergency or player-control interruption occurs.
Walking lasts no more than **6,000 game ticks including its travel**; skygazing lasts no more than
**4,000 game ticks after arrival**. Recreation is gained at **half the ordinary equivalent
activity rate**, with ordinary gain clamping and no instant outdoors refill. Full recreation alone
SHALL NOT prevent or end a break while outdoors need remains below its goal; S024's completed
recreation preference does not veto this distinct outdoors-need activity. Contextual reports
describe walking or watching clouds, stars, sunrise, sunset, aurora or eclipse as appropriate.
Changing destination/exposure safety is rechecked within **250 game ticks** and invalidates unsafe
continuation. S002's directed recreation and this automatic break produce one actual activity and
one need gain, without stacking duplicate rewards or commandeering a current player-directed job.

#### Scenario: Outdoors goal reached or duration exhausted
- **WHEN** a skygazing pawn reaches 75% outdoors need, or its 4,000-tick activity limit is reached first
- **THEN** the activity ends and ordinary job selection resumes
- **AND** recreation received reflects actual time spent at half the ordinary equivalent rate, not a completion bonus

#### Scenario: Conditions change during a walk
- **WHEN** a walk's route becomes hazardous or the pawn is drafted or its schedule stops permitting recreation
- **THEN** it ceases the break under the applicable safety or ordinary interruption rule
- **AND** it does not keep following an obsolete route just to finish its planned duration

#### Scenario: Full recreation but low outdoors need
- **WHEN** an otherwise eligible colonist has full recreation and 15% outdoors need, or recreation fills during its break before outdoors need reaches 75%
- **THEN** the outdoor break can begin or continue toward its outdoors goal under the ordinary duration, safety and interruption rules
- **AND** recreation stays clamped at its normal maximum without ending the break solely for that reason or granting an overflow reward

#### Scenario: Directed recreation or missing outdoors need
- **WHEN** a player-directed recreation job is already active, or the pawn has no outdoors need
- **THEN** this feature adds neither a competing break nor a second recreation gain
- **AND** existing directed controls retain their own ordinary eligibility and completion rules
