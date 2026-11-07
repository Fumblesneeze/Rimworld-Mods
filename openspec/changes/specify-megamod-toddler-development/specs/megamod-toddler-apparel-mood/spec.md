## Owner

Make Rimworld Great Again (`fumblesneeze.megamod`, `mods/megamod`). S031; future 1.7 beta behavior.
Values are provisional source baselines; material, quality and ordinary health effects still apply.

## ADDED Requirements

### Requirement: Functional baby clothing has ordinary acquisition and protection

Infants and toddlers SHALL have craftable onesies, tuques, shadecones and tribalwear, subject to
the selected availability policy. Each recipe SHALL make one garment using ordinary materials,
work and quality. Tribalwear SHALL be available without research at a crafting spot; the other
garments SHALL require Complex Clothing and an ordinary hand or electric tailoring bench.
These explicit bench choices are the draft owned contract, pending native progression checks.
The following declared values SHALL apply before material/quality effects:

| Garment | Material units | Work | HP | Mass | Equip seconds | Armor factor | Cold/heat insulation factors |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Onesie | 20 | 2000 | 50 | 0.18 | 1.5 | 0.2 | 0.6 / 0.6 |
| Tuque | 10 | 1200 | 50 | 0.03 | 0.8 | 0.2 | 0.5 / 0 |
| Shadecone | 10 | 1800 | 50 | 0.07 | 0.8 | 0.2 | 0.1 / 0.5 |
| Tribalwear | 15 | 1400 | 60 | 0.5 | 1.5 | 0.2 | 0.5 / 0.5 |

The tuque SHALL use fabric; the other three SHALL accept fabric or leathery materials, including
compatible modded materials in those categories. The explicit fabric-only tuque is an owned
choice resolving uncertain source inheritance. Onesies SHALL cover torso, neck, shoulders, arms,
legs and feet; tribalwear torso and legs; hats the upper head in the ordinary overhead layer.
Hats alone SHALL NOT count as covering nudity. Garments SHALL have usable ground and worn visuals.

#### Scenario: Early protection is attainable
- **WHEN** an eligible colony with suitable material orders baby tribalwear before Complex Clothing
- **THEN** a crafter uses 15 material units and 1400 work to make one wearable garment at a crafting spot
- **AND** onesie, tuque and shadecone recipes become available through their specified research and tailoring path

### Requirement: Clothing modes change availability without deleting possessions

The common setting SHALL offer these modes, with **Baby tribalwear** as default:

| Mode | Owned availability |
| --- | --- |
| No baby apparel | No new owned baby-garment crafting, trading or generation; no added special/child baby eligibility |
| No tribal babywear | Hide owned tribalwear; retain the other garments with Complex Clothing; enable supported special protective items |
| All tribal babywear | All four owned garments use the tribalwear no-research crafting-spot route; enable supported special protective items |
| Baby tribalwear | Preserve the ordinary routes above; enable supported special protective items |
| Child apparel | Hide new owned babywear; allow suitable child apparel and supported special items on babies only where wearability and presentation are supported |

Supported special items SHALL include gas masks, shield belts, psychic foil helmets and cloth
masks, plus children's vacuum suits/helmets when their content is available and admitted. Added
wearability SHALL NOT waive material body-part, layer, equipment, research or environmental
protection rules. Other providers' original baby eligibility, recipes and progression SHALL remain
intact unless a separately specified adapter changes them. Existing garments SHALL never be deleted
or forcibly stripped by a mode change. Unsupported child-item presentation SHALL explain the
unavailable extension rather than display missing art. Changes SHALL take effect in the current
save's future availability decisions without affecting another save.

#### Scenario: Changing modes with clothed children
- **WHEN** the player switches from Baby tribalwear to No baby apparel
- **THEN** new owned babywear acquisition is hidden but existing items and worn protection remain
- **AND** a provider's independently supported infant outfit retains its original availability

### Requirement: Dressing is a real caregiver or learned self-care action

Infants and less-developed toddlers SHALL be dressable by a capable caregiver through contextual
controls. Confident toddlers SHALL also dress themselves. Automatic caregiver optimization SHALL
respect childcare assignments, the child's clothing policy, reachable usable garments, body/layer
compatibility and existing forced/locked protection. A score improvement of at least 0.05 SHALL
be required for an ordinary upgrade using the ordinary apparel comparison. Removing a policy-
violating garment SHALL remove only that eligible garment, not strip all unrelated clothing.
Automatic dressing SHALL NOT interrupt urgent feeding, emergency safety or another active care pair.
The adult and child SHALL remain paired only while the real dressing action continues.

#### Scenario: One garment violates policy
- **WHEN** a caregiver automatically replaces an eligible disallowed hat
- **THEN** the intended hat is removed while unrelated clothes and forced/locked items remain
- **AND** the replacement requires physical access and actual dressing time

#### Scenario: Independent dressing
- **WHEN** a confident toddler wears eligible clothing
- **THEN** it performs the ordinary wear action and leaves displaced clothing where dropped
- **AND** it does not acquire an ordinary hauling job or remain stuck waiting for a former caregiver

### Requirement: Clothing removal episodes preserve safety and item integrity

Clumsy and confident toddlers SHALL sometimes try removing clothing, with source-derived mean
intervals of 6 and 20 days respectively while those learning stages remain active. These are
random rates, not scheduled guarantees. The episode SHALL require manipulation and removable
clothing, SHALL not run in a caravan, and SHALL end for medical rest or no eligible clothing.
Between items it SHALL have a 50% chance to end; otherwise it SHALL choose a removable worn item.
Forced/locked items SHALL be excluded. Removal SHALL take the item's equip time and produce the
actual dropped item only after success. It SHALL recheck current protection and S030 safety before
removal; it SHALL NOT intentionally strip the child into unsafe temperatures. The correction is
reliable post-removal protection evaluation and forced-item filtering, not a new temperature margin:
S030's actual safe range, including the base comfort range extended by 10°C, remains authoritative.

#### Scenario: Protective clothing becomes necessary
- **WHEN** a removal episode is underway and current temperature makes the chosen garment necessary
- **THEN** that removal stops without losing the item or disabling ordinary rescue
- **AND** saving/reloading the episode cannot duplicate the garment or forget its safety checks

### Requirement: Toddler mood is visible without hiding unrelated mental states

Infants and toddlers SHALL use a no-expectations mood bonus, default +20, instead of ordinary
expectations. Toddlers SHALL not receive ordinary random adult mental breaks or work/meditation
alerts for abilities they do not have. Ordinary eligibility for other health and mood consequences
SHALL remain. Crying, giggling and the owned clothing-removal episode SHALL not alone prevent
player control, carrying or ordinary caravan/shuttle participation. These three episodes SHALL
also preserve ordinary allowed-area obedience and SHALL NOT alone end an otherwise valid gathering,
block an otherwise eligible ritual role or trigger a group mental-break departure. All other age,
role, access and activity restrictions SHALL remain; unrelated mental states SHALL retain their own
restrictions. Enabled drafting SHALL be retained through these three episodes
without granting violence. Crying/giggling mean-day curves SHALL interpolate these source values:

| Mood fraction | Crying mean days | Giggling mean days |
| --- | --- | --- |
| 0 | 0.001 | 8 |
| 0.25 | 0.02 | 4 |
| 0.3 | 0.1 | Between the adjacent listed giggling points |
| 0.5 | 1 | 1 |
| 0.75 | 4 | 0.1 |
| 1 | 8 | 0.05 |

These curves SHALL replace the corresponding whole curves intentionally, not append contradictory
points; all other fit eligibility and duration remain ordinary behavior.
Go frenzy inspiration SHALL use a minimum biological age of 1 while retaining all other ordinary
inspiration prerequisites; this does not guarantee that every toddler can receive it.

#### Scenario: A crying passenger
- **WHEN** an otherwise eligible toddler cries during caravan preparation
- **THEN** crying alone does not strand it or prevent the assigned adult from carrying it
- **AND** an unrelated disabling condition still receives its normal eligibility checks

#### Scenario: Harmless crying during ordinary group activity
- **WHEN** an otherwise eligible toddler cries while attending a gathering or being considered for a ritual role
- **THEN** crying alone does not cancel the gathering, disqualify that role or trigger the group's mental-break departure
- **AND** genuine age, physical, role or unrelated mental-state restrictions still apply

#### Scenario: Allowed areas still matter during a tantrum
- **WHEN** a crying, giggling or clothing-removing toddler autonomously selects a destination
- **THEN** ordinary allowed-area restrictions remain in force, with only the separately specified S030 safety exception
- **AND** the episode does not itself make forbidden areas available

#### Scenario: Young inspiration eligibility
- **WHEN** a pawn at biological age 1 or later otherwise meets every ordinary Go frenzy prerequisite
- **THEN** the inspiration is not excluded solely by a higher minimum-age requirement
- **AND** an age below 1 or another unmet prerequisite still prevents eligibility

#### Scenario: Mood-related episodes
- **WHEN** otherwise eligible toddlers experience low versus high mood over equivalent simulation time
- **THEN** their crying/giggling tendencies follow the stated curves without guaranteeing an event at a fixed deadline
- **AND** their visible no-expectations effect uses the current effective setting

### Requirement: Swaddling reflects the child's actual clothing and life stage

Toddlers SHALL not display an infant swaddle. Infants who are no longer psychologically nude due
to clothing SHALL display that clothing without the swaddle. A hat alone SHALL not imply a fully
clothed infant. Layering, body shape, held posture and supported provider graphics SHALL remain valid.

#### Scenario: Dressing an infant
- **WHEN** an infant gains a body garment and later wears only a hat
- **THEN** the body garment suppresses the swaddle while the hat alone follows ordinary nude-infant wrapping
- **AND** actual apparel protection remains independent of this presentation choice
