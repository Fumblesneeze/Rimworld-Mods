# Ordinary query catalog

Owner **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`; **1.7 beta**.
S121 TD Find Lib - Continued. Specification only; no runtime behavior accepted.

## ADDED Requirements

### Requirement: Give every ordinary criterion the same reliable boundaries
The catalog SHALL extend S033's search rules without changing listing, map scope, physical result
identity, include/exclude, deliberate disable or group meanings. Each criterion can recognize a
corpse/minified wrapper or its represented object, but yields one physical result. Inapplicable
candidate kinds simply do not match; an unavailable required observation makes the whole query
unavailable, including its counts and actions. Hidden information remains hidden in suggestions,
categories, nested relationships and results even with god mode enabled. A criterion never orders
work, reserves a target, changes a policy or predicts success merely because it matches.

Numbers use inclusive ranges. Unrestricted lower/upper ends are explicitly labeled, not concealed
behind a finite slider endpoint. Finite ends remain exact; reversed, nonnumeric and infinite input
is rejected while preserving the last valid edit. A naturally indefinite estimate fails a finite
range; a missing/undefined measurement is never fabricated as zero. Ranges are initially unrestricted
unless a requirement below states otherwise. A required exact content/entity choice with no stated
default must be selected before the new clause is enabled. Missing named defaults follow that same
rule, without selecting an arbitrary similarly named object. Imported operands never acquire fresh
defaults in place of their original meaning.

#### Scenario: Distinguish unrestricted from a percentage cap
- **WHEN** a valid capacity is 120% and the player compares an unrestricted range with an explicit 0–100% range
- **THEN** only the unrestricted range matches, and the displayed ranges make that difference clear.

#### Scenario: Evaluate a hidden condition
- **WHEN** a pawn has an undisclosed condition but no disclosed condition in the chosen category
- **THEN** the ordinary category criterion does not reveal the hidden condition in matches or suggestions.

### Requirement: Offer useful choices without changing existing operands
Criteria SHALL be organized into searchable ordinary categories for objects, plants/environment,
pawns, policies, relationships and animals, with expansion categories only when available.
Option labels distinguish otherwise identical names and show required context. The Only available
choices preference restricts candidate-dependent suggestions to disclosed values present across
loaded maps, regardless of list length; it does not change the query's selected map scope or meaning.
Holding Shift reverses that preference for the open chooser and visibly identifies the temporary
mode. At the main menu, choices from available content are shown without consulting a previous game.
The selected value remains visible even when absent from current suggestions. An unavailable saved
choice is retained with an explanation. Categories operate on the same subject as their exact-item
alternatives; selecting a category must not silently change from inspecting a pawn's policy to
inspecting the pawn's own object type.

#### Scenario: Restrict a short choice list
- **WHEN** only two of five available food types are currently disclosed on loaded maps and Only available choices is on
- **THEN** those two are suggested, the already selected absent type remains identifiable, and Shift temporarily exposes all five without rewriting the clause.

### Requirement: Match ordinary objects and measurable properties
The following criteria SHALL be available in addition to S033's name, exact type/stack count,
material and four zone choices. A category includes descendants unless explicitly stated otherwise.

| Criterion | Observable match and initial choice |
| --- | --- |
| General kind | Person, animal/wild person, item, building, plant, natural object or other; use the game's ordinary classification, with wild people in the animal/wild-person choice and natural rock excluded from buildings; choose a kind explicitly |
| Stat | Chosen ordinarily displayed stat in the entered range, initially General labor speed; a stat not meaningful/displayable for this object fails; Market value is a shortcut to the same stat criterion |
| Item category | Object type belongs to the chosen category/descendants; choose explicitly |
| Quality | Object has a quality within the selected interval; all qualities initially; objects without quality fail |
| Special filter | Chosen native item-filter rule, initially Fresh; the rule retains its native meaning |
| Apparel layers / body coverage | Apparel includes every required layer/body-part group and none prohibited; initially On skin / Legs respectively; neither set implies exact equality |
| Forbidden | Spawned forbiddable object is forbidden, allowed or either; Forbidden initially; not reachability for a particular pawn |
| Mineable | Selected resource product, any resource rock, non-resource rock or all mineables; Any resource initially |
| Architect category | Object belongs to the chosen Architect category, initially Production; its packed or planned form can still identify that underlying category |
| Door state | Open, closed, held open or blocked from closing; Open initially; held-open intent is distinct from current open state |
| Health percentage | Pawn's overall health percentage or an object's current/max hit points if it has hit points; unrestricted initially |
| Art | Object currently has active art; no quality or price requirement |
| Content source | Object type originates from the selected active content package, initially Core; materials' providers do not replace the object's own source |

The obsolete mineable criterion is recognized when importing its resource/non-resource/any meaning
but is presented as the current criterion, without a second obsolete menu entry.

#### Scenario: Combine apparel masks
- **WHEN** an apparel criterion requires both On skin and Middle while prohibiting Shell
- **THEN** apparel containing both required layers and no Shell matches even if it also uses an unrelated layer.

#### Scenario: Match a packed object only once
- **WHEN** a minified quality bed satisfies a building/quality query through its represented bed
- **THEN** one packed-bed result is shown; unpacking updates its identity/location without adding a second count.

### Requirement: Describe recoverable materials as potential outputs
Recoverable parts SHALL match objects whose known type or installed parts can potentially produce
a selected material, any recoverable material or a material category. Steel is the initial exact
choice. Potential sources include removable added parts, ordinary non-intricate construction costs,
smelting/butchery products and death leavings for player or hostile objects; stone-chunk butchery
outputs are excluded. Category choices distinguish textiles, raw resources, manufactured resources,
body parts and other recoverables. This is labeled potential output, without claiming a current
recipe, worker, yield, reservation or lawful destruction action exists.

#### Scenario: Find potential steel in unavailable work
- **WHEN** a known object can potentially return steel but no eligible smelting job can currently run
- **THEN** it can match Recoverable steel while the result makes no claim that smelting is available now.

### Requirement: Observe plants and decay in current conditions
The following criteria SHALL inspect current plant/environment state without ordering work.

| Criterion | Match and initial choice |
| --- | --- |
| Crop | Plant type is a crop, whether or not inside a growing zone |
| Harvest product | Selected harvest product, any product or none; Any initially |
| Harvestable | Plant is presently harvestable |
| Growth / growth rate | Current plant growth fraction / current growth rate in the selected range; unrestricted initially |
| Freshness | Fresh, rotting, dessicated, can spoil, refrigerated or frozen; Fresh initially; refrigeration/freezing describe ambient rot conditions even for objects without their own rot process |
| Time to rot | Rottable object's current-temperature estimate, initially 0–10 days; no estimate is not zero |
| Dies leafless | Plant type dies when leafless; not a prediction of temperature death |
| Blighted | Plant currently blighted |
| Temperature | Ambient temperature at held location, initially 10–30°C; values display in the player's temperature units without changing the actual interval |
| Deteriorating | Current final deterioration rate at least 0.001; protection and current conditions matter |

#### Scenario: Compare a refrigerated object and a crop
- **WHEN** a non-rottable object is in freezing conditions and a crop grows outside a growing zone
- **THEN** Frozen can match the object and Crop can match the plant; neither implies a rot timer or zone membership.

### Requirement: Query locations and relationships without escaping visibility
Area SHALL match the held cell in an exact allowed area, Home, build-roof, no-roof, snow/sand-clear
or roofed region, initially Home. Exact areas and zones bind to their actual map/identity under
S033; renamed areas remain the same target and another identically named area is not a substitute.
On screen matches occupied cells overlapping the current camera view and visibly requires Current
map. Faction offers own faction or pawn host faction, with player, mechanoid, insect, hostile,
neutral, allied, any disclosed faction, no disclosed faction and permanently hostile choices;
Player own faction is initial. Hostile/neutral/allied exclude the player faction. Hidden faction
identity is not disclosed by this chooser.

Nearby SHALL match when at least one disclosed object in a square of radius 0–10 cells around the
candidate's held cell satisfies the nested query; radius starts at 0. Center and candidate are
included. At a map edge, only existing cells are considered. It is not a line-of-sight or circular
distance test. Inventory relationship offers Holds a matching known object recursively or Is held
by a matching enclosing holder; Holds is initial. Packed wrappers do not become fictitious inventory
parents. Reserved matches actual object reservations by player-faction pawns, optionally restricted
by a nested reserver query, initially any reserver; it excludes mere cell reservations.

#### Scenario: Search nearby at the edge
- **WHEN** radius 2 is used on a corner cell
- **THEN** only disclosed objects in the clipped square can satisfy the nested query, including the candidate itself if it matches, with no map error or hidden-cell discovery.

#### Scenario: Follow known nested contents
- **WHEN** a known container inside a known carrier contains a matching item
- **THEN** Holds can match the carrier recursively while undisclosed containers remain opaque and physical results remain unduplicated.

### Requirement: Inspect designations without applying them
Designation SHALL offer an exact designation, any or none, initially Any, covering object and held-cell
designations including native smoothing, harvest, extraction and paint-removal cases. Can designate
offers an exact currently available designation action or any eligible action, initially Any. A
None choice means no eligible supported action, correcting the source's always-false branch. Querying
eligibility must not place a designation, create a job, mutate selection or consume resources.

#### Scenario: Find eligible but undesignated work
- **WHEN** an object can be designated for a supported action but has no designation
- **THEN** Can designate that action matches and Designation none matches, without issuing the action.

### Requirement: Inspect pawn condition and ability to act
The following criteria SHALL use the pawn's current disclosed state; absence of a required capability
is a nonmatch, not a fabricated default.

| Criterion | Match and initial choice |
| --- | --- |
| Pawn property | Explicit colonist, free colonist, colony prisoner, colony slave, any prisoner, any slave, colony mechanoid, downed, dead or psylink property; generic custody and colony custody are distinct; no implicit colonist restriction |
| Dies in | Finite estimated time to death from blood loss, initially 0–24 hours; explicitly not all causes of death |
| Skill | Selected or any not-totally-disabled skill, inclusive level and no/minor/major/any/ignored passion; Animals, unrestricted level, Ignore passion initially, consistent with S018 |
| Trait | Exact trait and degree; Pyromaniac initially when available; changing trait shows and initializes its first defined degree |
| Thought | Current non-nullified mood memory or situational mood thought in a selected inclusive From/To stage range; Ate without table initially, selecting a thought initially includes all its selectable stages; stages show their real effects and preserve identity when displayed in effect order; no hidden/social-opinion-only thoughts |
| Need | Selected disclosed need's current percentage; Food 0–50% initially; undisclosed wild-animal needs stay hidden |
| Capacity | Selected or any capacity in range; Manipulation exactly 100% initially |
| Mental state | Exact, any, none or aggressive mental state; None initially |
| Temperature comfort | Cold below safe minimum, cool up to comfortable minimum, comfortable through its bounds, warm above comfortable to safe maximum, hot above safe maximum; Cold initially |
| Incapable | Selected disabled work tag, any disabled tag or none; explicit choice required |
| Area restriction | Current-map assigned allowed area, Home or unrestricted; exact area requires deliberate binding |
| Drafted | Drafted, undrafted but draftable, or draftable; Drafted initially; an undraftable animal is not an undrafted pawn |
| Current action | Exact current job or no job; choose explicitly |
| Inspiration | Exact, any or none; None initially |
| Quest status | Quest lodger or helper; Lodger initially |
| Work enabled | Selected work type active in work settings, initially Firefighting; not a prediction of a reachable job |

#### Scenario: Distinguish bleeding from unrelated danger
- **WHEN** a pawn has no finite blood-loss death estimate but is endangered by another cause
- **THEN** Dies in 0–24 hours does not imply a prediction for that other cause.

#### Scenario: Keep thought stages attached to their effects
- **WHEN** the thought selector displays stages in mood-effect order differing from their source order
- **THEN** selecting an effect matches that stage, and copy/reload retains the same effect rather than its former display position.

#### Scenario: Retain an inclusive thought-stage interval
- **WHEN** the player narrows a thought's From/To choices to three consecutive disclosed effect stages and copies or reloads the query
- **THEN** all three selected stages can match, both endpoints remain included and neighboring unselected stages do not; neither bound becomes a single-stage choice.

#### Scenario: Distinguish colony custody from foreign custody
- **WHEN** a disclosed foreign-held prisoner and a colony prisoner are compared
- **THEN** Any prisoner can match both while Colony prisoner matches only the latter; the same distinction applies to Any slave and Colony slave.

### Requirement: Match disclosed health occurrences and repeated body parts
Health condition SHALL offer any disclosed condition, none, bleeding, tendable now, a specific
condition with an inclusive severity range, and disclosed condition categories. None is initial.
Exact severity matches if **any** disclosed occurrence of that condition is in range, irrespective
of order. A body-part occurrence mode offers Any, All or None of the relevant parts bearing the
selected condition when the game's supported applicability rules identify those parts unambiguously.
It shows the relevant part set before use; a pawn with none of those part types does not pass All
or None vacuously. Unsupported/ambiguous applicability disables that mode with a reason while the
ordinary condition/severity filter remains usable. Unknown applicability is never guessed from the
first surgery in a list. Bleeding and tendable-now use current known conditions and native meanings.

Missing body part SHALL offer any missing part, none missing or a selected part type with Any/All/None
occurrences missing. None missing is initial. Loss of an ancestor counts its descendant as missing;
a species with no such part does not match that specific type in any mode. These filters do not
decide whether a surgery is available; S137 separately owns condition-to-operation suggestions.

#### Scenario: Inspect two different severities
- **WHEN** a pawn has two disclosed infections and only the second is within the chosen severity interval
- **THEN** the exact infection criterion matches regardless of the health list order.

#### Scenario: Inspect repeated limbs
- **WHEN** the relevant set contains two arms and exactly one bears the selected implant
- **THEN** Any matches, All and None fail; a species with no arms does not pass the specific-arm modes.

#### Scenario: Missing ancestor
- **WHEN** a leg is missing and the player searches for a missing foot
- **THEN** the descendant foot counts as missing, without requiring a separate missing-foot entry.

### Requirement: Observe gear relations abilities and schedules
Gear SHALL offer primary weapon, other equipment, worn apparel, inventory, carried object, unused
inventory-and-carried objects, or all worn/equipped gear. The last is initial and excludes inventory
and carried cargo. Has, Any matching and All matching are available; All requires a nonempty set.
Relationship offers a selected social-card-visible direct or implied relation with optional gender
and Has/Any matching/All matching related-pawn modes; initially Parent, Male, Has. All requires at
least one related pawn. Nested matches retain ordinary visibility and stable scope.

Ability SHALL offer exact, any, none or a provider/category group, with Has, On cooldown, Can cast,
Active or remaining charges; initially Any, Has, with a 1-or-more charge range when chosen. Can cast
means current untargeted readiness, not success on an arbitrary target. Active is available only for
supported effects whose activity on the owner is known, with that limitation stated; unsupported
effects are not silently treated as inactive. Schedule offers current assignment (Work initially),
All specified hours match a 24-hour template, or Any specified hour differs. Blank hours are ignored;
an all-blank All template matches a pawn with a schedule, while Any differs fails.

#### Scenario: Gear is not hauling cargo
- **WHEN** a pawn has a weapon and carries steel in inventory
- **THEN** worn/equipped gear can match the weapon but does not match steel unless the inventory/unused scope is selected.

#### Scenario: Avoid empty universal relationships
- **WHEN** a pawn has no related pawns of the selected kind
- **THEN** All related pawns match is false rather than a misleading success.

#### Scenario: Observe a timetable
- **WHEN** only hours 11–17 are set to Sleep in an All template
- **THEN** all seven hours must be Sleep; unspecified hours do not constrain the result and no schedule is changed.

### Requirement: Observe guest and prisoner state distinctly
Guest status SHALL distinguish native guest, prisoner, slave, secure prisoner and secure slave
states, without implying Hospitality-specific membership. Prisoner criteria offer an exact interaction
mode (Maintain only initially), any prisoner, in a prison cell, escaping, mean prison-break interval,
resistance, will and non-recruitable. Prison-cell location can also match an object in that room;
non-recruitable is explicitly a guest recruitment property, not an implicit prisoner filter. Break
interval is the native estimate ignoring sleep, initially 0–120 days when chosen; resistance starts
0–50 and will 0–10. Each mode is labeled for what it actually observes.

#### Scenario: Distinguish a room from custody
- **WHEN** a meal lies in a prison cell and a prisoner is outside it
- **THEN** In a prison cell can match the meal and not the pawn, while Any prisoner can still match the pawn.

### Requirement: Query assigned policies and what those policies permit
Apparel, food and drug policy criteria SHALL match a pawn assigned the exact selected policy,
initially the current game's default when one exists. Cross-save templates require explicit rebinding.
Allowed by apparel policy, Allowed by food policy, Allowed by drug policy and Keep in inventory
inspect **pawns**, not the candidate item's own category. Initial exact items are Parka, Simple meal,
Penoxycyline and Penoxycyline respectively when present. Category alternatives match when at least
one member item satisfies the same pawn-policy test. Food choices have positive nutrition.

Drug permission means joy or scheduled use is enabled, or addiction use is enabled and the pawn is
addicted to/chemically dependent on that drug. Keep in inventory means the drug policy requests at
least one of that drug. Neither means it is currently due, medically advisable, available or actually
carried. Other owned drug-policy, response and medical features retain their action rules.

#### Scenario: Use a food category
- **WHEN** a pawn's policy permits a meal in the selected food category
- **THEN** the pawn matches Allowed by food policy for that category, even though the pawn itself is not a food item.

#### Scenario: Carry request differs from contents
- **WHEN** a pawn is configured to carry a drug but currently has none
- **THEN** Keep in inventory matches, while a query of its actual inventory can correctly find none.

### Requirement: Observe identity age and elapsed records
Age group SHALL match an exact developmental stage, initially Adult. Age offers biological or
chronological years, initially biological up to 50 years with no lower limit, consistent with S018. Gender offers the game's actual gender choices,
initially Male. Time record offers a chosen native time record expressed in whole seconds, hours,
days or years, initially time as colonist/colony animal in 0–60 years. Conversion uses 60 ticks per
second, 2,500 per hour, 60,000 per day and 3,600,000 per year, discarding incomplete selected units.
Copying or changing display language preserves selected record, unit and interval.

#### Scenario: Change record units deliberately
- **WHEN** an elapsed record is less than one whole day
- **THEN** its whole-day value is zero, and a copied query retains days rather than silently changing to seconds.

### Requirement: Observe race products and training without promising yield
Race SHALL offer intelligence, herd/pack/predator/prey properties, wildness, petness, retaliation
chance after failed taming or harm, and trainability. Choosing intelligence starts at Humanlike,
wildness/petness at 25–75%, and trainability at Advanced. Wildness uses its current stat; static race
properties retain their type meaning. These are observations, not changes to animal behavior.

Meat and Leather SHALL offer exact product, any or none plus a rounded current amount range, initially
Any and unrestricted. Eggs, Milk and Wool per year use the species' theoretical annual quantity,
rounded to whole units, and the selected/any/none product; sex restrictions are respected. Eggs mean
unfertilized eggs. These labels distinguish theoretical output from age, food, fertility, fullness,
slaughter skill or worker-dependent yield. Product progress offers laying, hatching, milk fullness,
wool growth, milkable now or shearable now; progress starts unrestricted, while the last two require
an active full producer. Bonded means at least one bond to a colonist. Training matches completed
steps of a selected training, initially Tameness; selecting training starts at one through its
required steps, with a visibly unrestricted upper end if chosen. It does not assign a master.

#### Scenario: Compare annual production with readiness
- **WHEN** a female animal's species produces milk but its udder is empty
- **THEN** its theoretical Milk per year can match while Milkable now does not, without suggesting milk already exists.

#### Scenario: Query a bond without creating a hunting order
- **WHEN** an animal is bonded to a colonist but lacks required hunting training
- **THEN** Bonded still matches and does not grant training or start owned hunting-retrieval behavior.
