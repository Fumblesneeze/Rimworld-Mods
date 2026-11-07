## Owner

Make Rimworld Great Again (`fumblesneeze.megamod`, `mods/megamod`). S031; future 1.7 beta behavior.
These are intended supported outcomes, not claims that the referenced providers currently work.

## ADDED Requirements

### Requirement: Optional support is enabled only as a usable whole

An absent optional provider SHALL leave ordinary toddler behavior available. An unsupported or
changed provider SHALL disable only the affected integration, explain its unavailability in
settings and log a bounded startup warning. It SHALL not remove the provider's ordinary care path
while failing to supply its replacement. Admitted integrations SHALL preserve unrelated provider
content and other mods' compatible changes.

#### Scenario: Hygiene support cannot initialize
- **WHEN** an installed hygiene provider is incompatible with the owned bathing workflow
- **THEN** that workflow stays unavailable with a startup explanation
- **AND** the provider's existing caregiver options remain intact and unrelated toddler care still works

### Requirement: Hygiene eligibility is specific to the actual child

With admitted Dubs Bad Hygiene support, infants/toddlers SHALL receive hygiene only where the
provider supports that actual child. Toddlers SHALL not independently wash or receive unsupported
thirst/bladder chores. Owned support SHALL suppress toddler thirst and bladder needs; infant need
eligibility SHALL remain the provider's normal behavior unless separately supported.
Eligible Baby-stage hygiene decay SHALL be multiplied by 0.8 while in an ordinary bed, excluding
sleeping spots. Otherwise, dirty independent play SHALL add 0.2 to the provider's hygiene-decay
factor. One race's missing need SHALL NOT disable another child's hygiene.
On a successful feeding-mess roll, supported child hygiene SHALL fall by **1.2 percentage points ×
activity factor × child's filth rate / max(feeder manipulation, 0.1)**, distinct from the probability
of that roll. Activity factors are 2 for self-feeding and 1 for bottle feeding. A separate adult
feeder with its own provider hygiene need SHALL have a 50% chance of the same loss. This hygiene effect
SHALL remain independent of the floor-filth toggle and clamp needs at zero. Its distinction SHALL
be explained in the setting tooltip.

#### Scenario: Children have different need support
- **WHEN** one child lacks a provider hygiene need while another has one
- **THEN** washing and dirty-activity effects apply only to the supported child
- **AND** inspecting or caring for either child does not change the other's eligibility

#### Scenario: Hygiene loss uses the effect amount rather than event probability
- **WHEN** a bottle-feeding mess roll succeeds with child filth rate 1 and adult manipulation 1
- **THEN** the child's hygiene loses 1.2 percentage points, limited by the amount remaining
- **AND** an adult without a provider hygiene need receives no hygiene effect

### Requirement: Caregivers can wash a dirty child with real water

At hygiene 30% or lower, a capable caregiver SHALL be able to wash an eligible player-owned or
hosted infant/toddler with usable clean water. Automatic selection SHALL honor the caregiver's
childcare assignments, including Never, and urgent-care priorities; it SHALL not interrupt urgent
feeding, rescue or another active care pair. Contextual Wash SHALL explain missing water, caring,
manipulation, access or reservation rather than silently changing forbidden status.
An ordinary wash SHALL last at most 800 ticks and increase hygiene by 0.2 percentage points per
tick of actual washing, stopping at full hygiene. One provider water-bottle unit SHALL be consumed
when actual washing begins; subsequent interruption SHALL not refund already used water or
consume it twice. Failure before washing SHALL consume none. Contamination clearing SHALL require
successful washing, not mere job cancellation. Partial hygiene gain SHALL remain earned.

#### Scenario: A wash is interrupted
- **WHEN** actual washing starts, consumes its water and is interrupted before completion
- **THEN** only earned hygiene improvement remains and no completion-only decontamination or play reward is granted
- **AND** both pawns are released, unused water remains accounted for and another care attempt can proceed

### Requirement: Bathing combines safe care and earned play

A toddler not requiring medical rest SHALL be eligible for an admitted usable bath or wash bucket
when Play is below 70%, or otherwise on a 20% selection chance. Selection SHALL prefer shorter
child-to-fixture travel with a 30-cell penalty for a bucket. Once selected, checking the same
attempt SHALL not reroll a different activity. Unavailable bathing SHALL leave ordinary washing
available. A caregiver SHALL reserve, carry and place the child safely, fill a bath with real
provider water for at most 1,000 ticks, then wash/play for at most 1,000 ticks or until both hygiene
and Play exceed 99%. Buckets SHALL use their own valid filled/usable state, not a bath-only rule.

During actual bathing, hygiene SHALL rise 0.1 percentage points per tick, Play 0.025 percentage
points per tick times room effectiveness, adult social recreation 0.018 percentage points per
tick, and loneliness SHALL fall at the ordinary adult-play rate. Values SHALL clamp to their
valid bounds and reflect elapsed time. Successful play can grant ordinary interaction benefits;
travel, filling or a failed placement SHALL not. Consumed provider water SHALL be accounted for
once; unused contents SHALL follow the provider's ordinary drain/bucket-use rules, not disappear
solely because this mod cancelled a job. Cleanup SHALL release the fixture and both participants,
remove temporary washing state and place or retain the child safely on every exit and reload.

#### Scenario: Filling fails before bath play
- **WHEN** water or fixture access fails after the child reaches the bath but before play starts
- **THEN** no play/attention/recreation reward is granted
- **AND** the caregiver recovers the child, temporary washing state ends, fixture ownership is released and real water usage remains correct

#### Scenario: A completed bath
- **WHEN** a caregiver fills and uses a valid bath successfully
- **THEN** the child visibly gains hygiene, play and attention, the adult gains the stated recreation, and provider contamination/drain consequences occur normally
- **AND** the child finishes at a safe valid location rather than a silently failed drop

### Requirement: Alien development respects each supported race

An admitted alien race SHALL retain its own infant/child ages and appearance. An existing supported
toddler stage SHALL be reused once. For a supported immobile-infant to mobile-child interval of at
least two years with no toddler stage, the draft default toddler start SHALL be one third of that
interval after infancy starts, rounded to the nearest whole year, with a minimum offset of one year
and strictly before childhood. Unsupported or ambiguous stages SHALL remain unchanged with an
explanation. Learning SHALL scale to the actual resulting toddler interval. A race without suitable
humanlike walking anatomy SHALL retain native gait rather than forced human animation.

#### Scenario: Late-starting alien infancy
- **WHEN** a supported race has infancy at age 20 and childhood at 24 without a toddler stage
- **THEN** its default toddler stage starts at age 21, retains childhood at 24 and learns over that actual interval
- **AND** initialization or reload cannot insert a duplicate stage or change another race's infant behavior

#### Scenario: Existing race appearance
- **WHEN** a supported race already defines toddler graphics or multiple conditional body addons
- **THEN** its appearance and unrelated conditions remain intact
- **AND** unsupported appearance does not leave partially changed life stages, invisible bodies or an infinite traversal

### Requirement: Clothing-provider organization preserves unrelated categories and progression

When Progression: Attire support is admitted, owned baby garments SHALL appear in its appropriate
child-apparel/headgear organization without discarding unrelated categories. A supported tribalwear
research requirement supplied by its companion content SHALL remain meaningful rather than be
erased by a global clothing template. The original Toddlers package SHALL not be required merely
to activate this owned equivalent. Unsupported provider content SHALL receive an explicit limited-
support explanation; keeping similar item names alone SHALL not be called drop-in compatibility.

#### Scenario: Attire is present without Toddlers
- **WHEN** the admitted clothing integration is enabled with its required content
- **THEN** the owned garments appear in the intended provider categories and obey the admitted research path
- **AND** other category membership remains, no source package is impersonated and missing companion content leaves base progression intact
