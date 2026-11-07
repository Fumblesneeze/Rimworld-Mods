## Owner

Make Rimworld Great Again (`fumblesneeze.megamod`, `mods/megamod`). S031; future 1.7 beta behavior.
All tuning values below are draft source-derived baselines pending native playability verification.

## ADDED Requirements

### Requirement: Childhood has a mobile but dependent toddler stage

With Biotech available, human pawns SHALL become toddlers at biological age 1 and children at age
3. Toddlers SHALL remain dependent young children who can move and sleep voluntarily, including
while held, but cannot perform ordinary work or violence. Their base movement speed factor SHALL
be 0.4 before other health, equipment and learning effects. Generation partway through the stage
SHALL give age-appropriate development; it SHALL NOT turn a toddler into an ordinary worker.

#### Scenario: A baby reaches toddler age
- **WHEN** a human baby's biological age reaches 1 during play
- **THEN** the pawn becomes mobile with toddler needs and restrictions, and the player receives a transition notification
- **AND** existing health, family, ownership and clothing remain intact unless physically incompatible clothing must be safely removed

#### Scenario: The toddler becomes a child
- **WHEN** the pawn reaches biological age 3
- **THEN** toddler-only learning, loneliness and restrictions end and ordinary child behavior and presentation take over
- **AND** completed learning does not leave a permanent capacity penalty or toddler backstory overriding later childhood

### Requirement: Walking and manipulation develop independently

Each skill SHALL advance with biological development over its configured fraction of the toddler
stage, default 80%, capped at completion. At default human ages completion occurs at age 2.6.
Walking SHALL progress from crawling below 50% to wobbly walking, then ordinary walking at 100%.
Manipulation SHALL progress from inept below 30%, to clumsy below 60%, to confident until complete.
Learning capacity offsets SHALL be Moving -0.65/-0.45 for crawling/wobbly and Manipulation
-0.85/-0.55/-0.25 for inept/clumsy/confident. Other ordinary capacity modifiers still apply.
Clumsy toddlers SHALL be able to self-feed; confident toddlers SHALL also be able to dress
themselves. Crawlers SHALL NOT open closed doors. Learned abilities SHALL remain subject to
current physical capacity, accessibility and need restrictions.

#### Scenario: First steps and independent actions
- **WHEN** walking reaches 50% outside a growth vat
- **THEN** the toddler starts visibly wobbling instead of crawling and the player receives a first-steps notification once
- **AND** a still-inept toddler cannot self-feed merely because it can walk

#### Scenario: Developing manipulation
- **WHEN** the player orders eating or wearing apparel before the corresponding learned ability
- **THEN** the contextual action explains why it is unavailable
- **AND** reaching the required stage enables the ordinary eligible action without granting hauling, combat or care of other pawns

### Requirement: Learning uses biological time and survives save changes

Normal biological aging SHALL advance learning once for elapsed biological development, independent
of simulation speed or batching. While growth-vat aging applies, the accelerated biological portion
SHALL contribute at 60% of its ordinary learning rate, without being counted a second time.
Newly generated toddlers SHALL initialize each skill from elapsed toddler-age fraction divided by
that skill's configured learning fraction. Existing owned progress SHALL survive save/load and
setting changes; changing a learning duration affects future progress and SHALL NOT revoke an
already learned ability. No learning SHALL continue beyond toddlerhood.

#### Scenario: Reloading a developing toddler
- **WHEN** a saved toddler resumes with the same age and settings
- **THEN** learned abilities and progress are retained and first-steps or transition notices are not replayed
- **AND** equivalent biological time advances learning equally at different simulation speeds

#### Scenario: Accelerated growth
- **WHEN** an eligible toddler spends biological development time in a growth vat
- **THEN** the vat portion advances learning at the specified 60% rate exactly once
- **AND** growth past toddlerhood removes toddler restrictions even if learning had not completed

### Requirement: Presentation communicates development without corrupting other states

Crawlers and wobbly walkers SHALL have recognizable movement, with wobble diminishing as walking
improves. Bugwatching and crib play SHALL have their associated leaning, lying and wiggling poses.
Carried toddlers SHALL appear held in a lying posture. These poses SHALL end when their activity
ends and SHALL NOT distort portraits, dead/downed bodies or another active provider animation.
Nonhuman bodies SHALL use only supported presentation; lack of a supported gait SHALL NOT disable
unrelated manipulation, feeding or attention behavior.

#### Scenario: Play interrupts movement
- **WHEN** a toddler stops walking to watch bugs, enters a crib, is carried or becomes downed
- **THEN** its visible pose matches the current state without a stale crawl/wobble overlay
- **AND** ending that state restores the appropriate ordinary presentation

#### Scenario: Unsupported body presentation
- **WHEN** a supported toddler race has no suitable humanlike gait
- **THEN** its native appearance remains usable without distorted limbs or missing textures
- **AND** independently supported toddler needs and learned abilities still work
