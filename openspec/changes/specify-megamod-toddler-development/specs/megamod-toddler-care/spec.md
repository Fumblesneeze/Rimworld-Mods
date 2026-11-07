## Owner

Make Rimworld Great Again (`fumblesneeze.megamod`, `mods/megamod`). S031; future 1.7 beta behavior.
S030 owns mandatory thermal avoidance, escape and safe allowed-area return; all activities here
SHALL compose with that behavior. Tuning is provisional until native playability verification.

## ADDED Requirements

### Requirement: Learned self-feeding retains food access rules and adult fallback

Clumsy and more capable toddlers SHALL find and eat eligible food, including baby food, subject
to their diet, food policy, reservation, reachability and physical capacity. Toddler chewing SHALL
take ten times the equivalent ordinary chewing duration. Clumsy eaters SHALL eat at a suitable
nearby safe floor location; more capable eaters SHALL use ordinary eating-location selection.
Food-consumption mood bonuses and penalties SHALL NOT apply to infants or toddlers; poisoning,
nutrition, dietary restrictions and other physical consequences SHALL remain. This deliberately
implements the source manual's mood intent, regardless of its incomplete source suppression.

#### Scenario: A capable toddler eats
- **WHEN** a hungry clumsy toddler can reach permitted baby food
- **THEN** it eats slowly at a safe usable floor spot and receives ordinary nutrition and physical consequences
- **AND** it receives no mood memory solely for that food or eating without a table

#### Scenario: Adult fallback remains available
- **WHEN** automatic feeding of capable toddlers is disabled but a hungry toddler cannot find usable food
- **THEN** ordinary eligible caregiver feeding remains available
- **AND** an already urgently eating toddler is not repeatedly picked up for another feeding attempt

### Requirement: Feeding mess is physical and cleanup follows successful feeding work

Enabled feeding mess SHALL sometimes leave washable food filth at the feeder's position. Its
provisional chance per ordinary simulation tick SHALL be clamped to 0–1 from
0.0003 × activity factor × child's filth rate / max(feeder manipulation, 0.1), with activity factor
2 for self-feeding and 1 for bottle feeding. Equivalent elapsed feeding time SHALL have equivalent
event-count distributions at different simulation speeds and batching, including multiple events
in a longer interval. Filth SHALL cost 5 cleaning work per thickness and
naturally disappear after 35–40 days if not cleaned or washed away earlier.
A capable adult feeder SHALL queue cleanup of actually created mess after feeding, without
superseding emergencies. Disabling floor mess SHALL prevent this filth and its cleanup job;
optional hygiene loss is separately described under toddler integrations.

#### Scenario: Adult cleans actual mess
- **WHEN** an adult finishes feeding and created food filth remains reachable
- **THEN** a cleaning-capable adult can clean that mess through ordinary cleaning work
- **AND** no nonexistent filth target or forced emergency interruption is introduced

#### Scenario: Feeding time is processed in different batches
- **WHEN** equivalent feeding with unchanged participants and rates spans the same simulation time in small or large batches
- **THEN** mess event counts and associated hygiene effects have equivalent distributions
- **AND** a longer batch does not silently collapse multiple possible messes into one

### Requirement: Attention limits what independent play can satisfy

Toddlers SHALL have loneliness visible in their Play explanation, ranging 0–100%. Independent play
SHALL raise Play only to 100% minus loneliness, without overshooting. At 70% loneliness they SHALL
receive a severe loneliness mood effect of -8. The provisional baseline loneliness increase SHALL
be 24 percentage points per day at toddler-stage start, declining linearly to 9.6 at its end,
multiplied by the loneliness setting. This is an explicit normalization of the source cadence.
Adult play SHALL reduce loneliness by 0.06 percentage points per tick of actual interaction.
Loneliness and Play changes SHALL depend on elapsed simulation time, not callback count.

#### Scenario: Toys cannot replace attention
- **WHEN** a toddler with 60% loneliness plays independently
- **THEN** Play can reach at most 40% through that activity and the tooltip explains the attention limit
- **AND** repeated play cannot bypass the cap or permanently spin through pointless replacement jobs

#### Scenario: Adult attention with full Play
- **WHEN** an awake toddler has full Play but loneliness above 1% and an adult is playing with it
- **THEN** the interaction can continue to relieve loneliness
- **AND** sleep, urgent needs or interruption still end the activity normally

### Requirement: Independent activities are accessible and safe

Independent play SHALL offer bugwatching, firegazing, floor drawing, a toybox, baby decorations,
skydreaming and television when the relevant content and location are usable. Activities SHALL
normally last at most 2,000 ticks, with Play gain 0.012 percentage points per tick multiplied by
room play effectiveness, clamped to the attention cap. Full Play at 99%, reaching that cap,
urgent needs, loss of access or safety SHALL end the activity. Selection SHALL not require every
activity to be available and SHALL not endlessly retry an unavailable choice.

| Activity | Observable eligibility and result |
| --- | --- |
| Bugwatching | A safe allowed place, indoors or outdoors; the child leans to inspect the ground. Indoor eligibility intentionally follows the manual rather than the source's outdoor-only check. |
| Firegazing | An accessible working permitted fire within 15.9 cells, watched from a safe adjacent position; losing fuel or power ends viewing. |
| Floor drawing | Clumsy or better manipulation; a safe allowed standable Home-area floor, outside doorways; a real floor drawing appears. This resolves the source's conflicting thresholds in favor of the manual. |
| Toybox | Accessible usable native or supported toybox within 15.9 cells, with visible toy play. |
| Baby decoration | Accessible usable native or supported decoration within 15.9 cells; the child interacts beside it. |
| Skydreaming | Enjoyable safe outdoors under open sky; roofing or unsafe conditions end it. |
| Television | A usable television and a valid viewing position or visible crib/bed, respecting its participant limit and required power; no artificial missing-power failure for a supported unpowered television. |

#### Scenario: The room has only one usable activity
- **WHEN** a toddler needs play and only one listed activity is feasible
- **THEN** it can perform that activity and gain Play up to the attention limit
- **AND** changes in power, roof, access, temperature or urgent needs cause an appropriate stop or safe response

#### Scenario: Clumsy indoor play
- **WHEN** a clumsy toddler has a safe allowed Home-area floor indoors
- **THEN** floor drawing and bugwatching can be selected without requiring confident manipulation or an outdoor trip

### Requirement: Adult play belongs to the actual adult-child pair

Eligible caregivers SHALL consider ordinary adult play when Play is at most 40%, loneliness is
at least 80%, or both loneliness is at least 40% and Play at most 80%, independently of low mood.
Distressed-child care SHALL additionally consider a child with (Play below 30% or loneliness above
50%) and (mood below 40% or crying). Childcare assignments, access and actual care eligibility remain
required. Crying-related priority SHALL
depend on the relevant distress, not the mere presence of unrelated caregiver thoughts. Failure
to offer one play activity SHALL allow other feasible activities to be considered.
Medical-rest children SHALL receive suitable bedside play at the bed's current position.
The child SHALL wait only for its actual current caregiver, and interruption or a changed target
SHALL release both participants without granting unearned play, attention or completion benefits.

#### Scenario: Bedside play after moving a child
- **WHEN** a caregiver takes a child to a suitable nearby bed before playing
- **THEN** the caregiver approaches the child's actual bedside and the interaction occurs there
- **AND** the child is not left waiting while the adult goes to the old location or another child

#### Scenario: A happy toddler still needs attention
- **WHEN** an otherwise eligible toddler has full Play and high mood but loneliness reaches 80%
- **THEN** ordinary adult childcare can initiate play to relieve loneliness without waiting for crying or low mood
- **AND** a zero Play-decay setting does not make adult attention unreachable

### Requirement: Crib exit follows capability and has a finite safe outcome

Crawlers and children requiring medical rest SHALL NOT leave a crib independently. Sleeping children
SHALL stay in the crib unless they can actually self-feed and food is below their ordinary
want-to-eat threshold; hunger alone SHALL NOT wake an inept walker for an unhelpful exit. Ordinary
needed rest SHALL take precedence over a purely recreational exit. Otherwise eligible awake walkers
SHALL use a reachable safe adjacent exit, normally after 2,000 ticks of wiggling; urgent hunger or
Play below 30% SHALL skip that delay. Awake crib play SHALL offer suitable television or open-sky
play, including for a downed child where physically possible. A capable adult SHALL have a contextual
Let out action for a crawler, with an unavailable reason when no valid placement exists.
No adjacent destination SHALL cause a bounded no-action result, never an endless search, forced
drop into a blocked cell or teleportation. Capability and destination changes SHALL be rechecked.

#### Scenario: A crib has no usable exit
- **WHEN** a walker or ordered caregiver attempts to leave a crib in a blocked single-cell space
- **THEN** the child remains safely supported and the player can see why no exit is possible
- **AND** clearing a safe adjacent cell permits a later ordinary attempt

#### Scenario: Sleep and rest are not repeatedly interrupted
- **WHEN** a sleeping child has no qualifying self-feeding need, or a tired awake child needs rest more than recreational exit
- **THEN** it stays to sleep/rest instead of repeatedly leaving and being returned to bed
- **AND** low Play alone does not override sleep or medically required rest

#### Scenario: Hunger exit requires a useful learned ability
- **WHEN** a sleeping hungry walker cannot yet self-feed
- **THEN** hunger does not initiate independent crib exit and ordinary adult feeding remains available
- **AND** a physically capable self-feeder below its ordinary want-to-eat threshold can wake and leave through a valid safe exit, subject to the medical-rest restriction

### Requirement: Caregiver placement protects the child and respects meaningful orders

Automatic care SHALL honor caregiver childcare permissions and current adult-child reservations.
It SHALL not compete with urgent eating, active carrying or explicit player control. Safe medical
rest SHALL take precedence over ordinary bedtime. With their settings enabled, sleeping on the
floor, scheduled bedtime and a held child's Rest below 30% SHALL justify safe bed placement.
An awake, unscheduled, safe toddler SHALL NOT be repeatedly returned to bed without another reason.
Unsafe medical beds SHALL NOT be preferred solely because injury has not yet become serious.
Care SHALL select a usable safe medical bed, ordinary bed or floor refuge as appropriate; if none
exists it SHALL retain safe support and report the failure instead of assuming the carrier's cell
is a valid drop. A nearer caravan gathering point SHALL NOT override child safety.

#### Scenario: Automatic bedtime is unsafe
- **WHEN** the preferred bed or assigned area is dangerously hot or cold
- **THEN** automatic care uses an available safe alternative and S030 prevents an immediate unsafe return
- **AND** the child's bed ownership and area assignment are not silently rewritten

#### Scenario: Explicit placement and failure feedback
- **WHEN** the player opens a child's safety or crib command
- **THEN** unavailable actions explain missing access, capacity or destination and messages describe the actual intended destination
- **AND** an explicit physically possible unsafe crib order requires a visible temperature warning; it does not change automatic safety policy

#### Scenario: Placement fails after pickup
- **WHEN** the reserved destination becomes unusable while the child is carried
- **THEN** the caregiver re-evaluates safe placement, retains the child if necessary and releases obsolete reservations
- **AND** no successful-placement message or duplicate child appears before an actual successful transfer
