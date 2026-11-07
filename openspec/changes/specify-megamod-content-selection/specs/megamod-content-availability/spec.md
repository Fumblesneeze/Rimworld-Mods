# Content availability

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`).
Target **RimWorld 1.7 beta**. These effects use the applied policy from `megamod-content-selection`.
Specification draft only. The rules concern new choices and actions unless explicitly stated.

## ADDED Requirements

### Requirement: Exclude future physical content without corrupting existing goods

Excluded items, buildings, plants and pawn races SHALL not be chosen by supported ordinary random
stock, rewards, equipment, map resources, wild growth, animal population, migration or spawner
selection. Excluded buildings SHALL not be offered for new construction, installation or claiming;
excluded plants SHALL not be offered for new sowing. Excluded material SHALL not be chosen for new
generated objects or new production. Existing goods SHALL retain ordinary use, storage, movement,
trade value and nutrition. Existing animals SHALL retain their identity and ordinary needs.
Explicit unsupported third-party generators SHALL be identified as outside verified coverage;
the mod SHALL not promise interception of every arbitrary direct spawn.

#### Scenario: Generation excludes a selected item
- **WHEN** supported ordinary stock or reward generation selects from candidates including an
  excluded item and otherwise eligible items
- **THEN** it omits the excluded candidate while preserving the ordinary rules for the others
- **AND** it does not replace the exclusion by an extreme count of another item

#### Scenario: Existing stock remains physical stock
- **WHEN** an excluded meal, material or animal already exists in the loaded colony
- **THEN** ordinary inspection and applicable storage, hauling, eating or animal-care actions still
  recognize that entity and its unchanged properties

#### Scenario: Excluded planting and construction
- **WHEN** the player browses sowing, building, installation or claiming choices
- **THEN** excluded choices cannot create a new order, without removing already existing objects

### Requirement: Preserve costs and make unavailable production explicit

An excluded recipe or a recipe producing an excluded ordinary output SHALL not be offered for new
bills or executed by existing bills. A recipe SHALL also be unavailable if any required ingredient
group has no allowed ingredient left. A fixed excluded construction cost SHALL make new work
unavailable; it SHALL never be silently removed from the cost. Alternatives that remain allowed
SHALL retain their ordinary quantities, work and skill requirements. Existing affected bills and
unfinished construction orders SHALL remain stored and visibly blocked with a reason; completed
products and work SHALL not be destroyed or refunded. Re-allowing the required content SHALL
permit ordinary resumption at the next application. An operation already in progress SHALL not
produce a newly excluded output after loading the new policy; its ordinary recoverable work and
ingredients SHALL be retained without duplication. Butchering follows the explicit incidental-output
rule below; its allowed products are not ordinary fixed recipe outputs for this blocking rule.

#### Scenario: Only one required ingredient group becomes empty
- **WHEN** exclusion removes all choices for one of two required ingredient groups
- **THEN** the recipe is unavailable and an existing bill explains the missing allowed ingredient
- **AND** the remaining group alone cannot produce the output for a reduced cost

#### Scenario: Alternatives remain
- **WHEN** an ingredient group allows wood or steel and only wood is excluded
- **THEN** an otherwise legal bill can use steel at its ordinary amount and work cost
- **AND** existing wood remains physical stock without satisfying this new production requirement

#### Scenario: Disable a product or fixed construction material
- **WHEN** a saved policy excludes a bill's product or a blueprint's required fixed material
- **THEN** the existing order remains visible but blocked and no free product or building appears

### Requirement: Fixed starting supplies and incidental products respect exclusions

The mod SHALL omit excluded fixed starting supplies when beginning a new game, identify the omitted
supplies during setup and leave the saved scenario definition unchanged. It SHALL not replace them
with other goods. Butchering and ordinary destruction leavings SHALL omit excluded products while
retaining the ordinary quantities of allowed products. Their underlying yield statistics, work
amounts and existing stock SHALL remain unchanged; lost excluded products SHALL not be compensated
with extra materials or value. A butchering operation with no allowed product SHALL be unavailable
with a reason rather than consume a corpse for nothing. Destruction SHALL still destroy the target
under ordinary rules even when no allowed leavings remain. These are explicit acquisition effects,
not a claim to intercept every foreign scripted spawn.

#### Scenario: Excluded starting supplies
- **WHEN** the selected scenario specifies an excluded starting item
- **THEN** setup explains its omission and the new colony receives none of that item or replacement
  goods, without rewriting the saved scenario or affecting an existing colony's supplies

#### Scenario: Some or all butchering products excluded
- **WHEN** ordinary butchering would yield allowed meat and excluded leather
- **THEN** it yields the ordinary meat quantity and no leather, at ordinary work cost
- **AND** if all products are excluded instead, the bill remains blocked and the corpse is retained

#### Scenario: Excluded destruction leavings
- **WHEN** ordinary destruction produces both allowed and excluded leavings
- **THEN** the target is destroyed, only the allowed leavings appear at ordinary quantities, and
  the omitted result does not increase other yields

### Requirement: Research exclusion preserves surviving prerequisites

An excluded research project SHALL be absent from selectable research. Its own research gate
SHALL be bypassed for otherwise available content, while every surviving prerequisite above it
and every independent prerequisite SHALL remain required. Chains of excluded projects SHALL be
handled consistently without duplicate requirements. Previously completed research SHALL remain
recorded for restoration. A cyclic or unresolved prerequisite chain SHALL leave the affected
dependent choice unavailable with an explanation instead of granting an accidental free unlock.
The content-settings page SHALL explain that excluding research can intentionally unlock its
otherwise available content earlier. Other research costs and work SHALL remain unchanged.

#### Scenario: Bypass the middle of a research chain
- **WHEN** research B requires A, C requires B and another project D, and only B is excluded
- **THEN** C still requires A and D but no work on B
- **AND** otherwise available content gated only by B still requires B's surviving ancestor A

#### Scenario: Invalid chain and restored progress
- **WHEN** an affected chain is cyclic or a previously completed excluded project is re-allowed
- **THEN** the invalid chain is reported without free unlocking, or the restored project's prior
  completed progress is retained respectively

### Requirement: Terrain and Architect exclusions control new orders

Excluded terrain SHALL not be offered as a new floor/buildable-terrain order, including through
floor sampling tools. Excluding terrain SHALL not replace existing map ground or change its costs.
Natural terrain with no build command SHALL be described as having no construction choice to hide;
terrain exclusion alone SHALL not promise removal from world or map generation. Excluded Architect
categories SHALL hide their commands from ordinary Architect browsing and search, while excluded
designation tools SHALL not start new designations. Existing completed structures and issued
designations SHALL remain. Re-allowing a category or tool SHALL not bypass its other restrictions.

#### Scenario: Sampling an excluded floor
- **WHEN** the player samples existing excluded floor through an available floor-sampling tool
- **THEN** no new placement order starts and the unavailable floor choice is explained
- **AND** the existing floor and material cost remain unchanged

#### Scenario: Hide a category and a designation tool
- **WHEN** the player searches Architect or attempts to activate an excluded tool
- **THEN** it cannot expose a hidden category's commands or start the excluded designation
- **AND** already-issued work is not cancelled merely because its tool was hidden

### Requirement: World and pawn generation use allowed choices

Excluded configurable factions SHALL be absent from new-world faction selection and automatic
starting-faction creation. Existing factions and settlements SHALL remain; exclusion alone SHALL
not erase their diplomacy or prohibit every action by an existing faction. Excluded pawn kinds
and races SHALL not be chosen by supported new-pawn, raid-group, animal or spawner selection.
Excluded biomes SHALL not be selected for newly generated world tiles; existing tiles and maps
SHALL remain. Excluded map-generation steps and scatter entries SHALL not run on new maps through
their supported generation paths. A required choice with no valid alternative SHALL stop that
generation with a clear reason rather than loop, create a malformed pawn or silently use excluded
content. Valid alternatives SHALL follow their ordinary selection rules.

#### Scenario: New world versus existing world
- **WHEN** a policy excludes a configurable faction and a biome
- **THEN** new-world selection omits them while loading an existing world preserves its factions,
  settlements and terrain

#### Scenario: Required pawn kind has no alternative
- **WHEN** a generation request requires an excluded kind and has no eligible ordinary alternative
- **THEN** that request ends with an explanatory failure instead of substituting a colonist,
  retrying forever or generating the excluded pawn

#### Scenario: Exclude a map step and scatter entry
- **WHEN** a new map is generated through the supported step and scatter selection paths
- **THEN** the excluded step contributes no generated feature and the excluded scatter entry is
  not placed, while other steps and existing maps remain unaffected

### Requirement: Pawn appearance and background exclusions affect new selection

Excluded traits, backstories, optional body types, hair, beards and tattoos SHALL not be selected
for new pawns or offered by supported ordinary selection menus. Existing pawns SHALL retain their
current attributes. A removed body-type preference SHALL use the ordinary remaining body-type
selection, with basic male/female body types kept available; it SHALL not rewrite existing bodies.
Excluded weapon traits SHALL not be selected for newly generated applicable weapons; existing
weapon traits SHALL remain. Excluded naming-rule groups SHALL not supply new culture names or
titles; ordinary remaining naming behavior SHALL apply without renaming existing entities.

#### Scenario: Generate a pawn after appearance exclusions
- **WHEN** a new pawn would randomly receive an excluded trait, backstory or appearance option
- **THEN** selection uses eligible alternatives or reports that a required choice cannot be made
- **AND** existing pawns with that option keep it

#### Scenario: Existing names and weapon traits
- **WHEN** a naming group and weapon trait are excluded before new generation
- **THEN** newly generated results omit them, while already named entities and existing weapons
  retain their names and traits

### Requirement: Events and world selection omit excluded choices

The mod SHALL omit excluded incidents, independently selectable quests, trader kinds, gatherings,
mental breaks, inspirations and weather from their supported ordinary random selection paths. New
excluded quests SHALL not be added or advertised as available. Existing quests, current weather,
ongoing gatherings, active mental breaks and active inspirations SHALL continue under ordinary
rules. Excluded raid strategies, arrival modes and raid-age restrictions SHALL not be selected;
remaining choices SHALL keep ordinary eligibility and weighting. Excluding all valid candidates
SHALL result in a bounded no-event outcome or a clear failure of an explicit request, not a loop
or fallback to forbidden content.

#### Scenario: Suppress a quest offer and retain an active quest
- **WHEN** an excluded quest would be offered while another instance is already active
- **THEN** the new quest and its availability letter are absent and the active instance remains

#### Scenario: Select an event with fewer eligible options
- **WHEN** ordinary selection considers an excluded incident, trader, gathering, mental break,
  inspiration, weather or raid option
- **THEN** that option is not selected and remaining candidates keep their ordinary rules
- **AND** excluding every candidate does not create unbounded retries

### Requirement: Health and thought exclusions do not erase existing state

A newly applied excluded health condition SHALL be refused by the supported condition-addition
path. Existing conditions SHALL retain their severity and ordinary progression; exclusion SHALL
not heal a pawn by deleting the current condition. Excluded thoughts SHALL not create new memories
or contribute new situational mood effects. Existing memories SHALL retain ordinary expiry rather
than being silently removed. The page SHALL describe these effects as gameplay-changing choices,
including their possible survival and mood consequences.

#### Scenario: Exclude a condition already present on a pawn
- **WHEN** the loaded policy excludes that condition and a new application is attempted
- **THEN** the new application is refused while the pre-existing condition remains and progresses
  normally

#### Scenario: Exclude a thought
- **WHEN** an ordinary event would create an excluded memory or a situation would contribute its mood
- **THEN** that new effect does not occur, while previously stored memories keep ordinary expiry

#### Scenario: Existing excluded memories compose with timed fading
- **WHEN** the applied policy excludes new acquisition of a thought whose existing memory is covered by S068
- **THEN** that existing memory retains its current fading and expiry policy without deletion or age reset
- **AND** fading previews retain the type and distinguish its blocked acquisition from unsupported fading;
  pending content choices do not change the current applied result.

#### Scenario: Excluded counselling award is not advertised as granted
- **WHEN** the applied thought policy refuses a captured counselling memory or fallback boost
- **THEN** no excluded or substitute mood benefit is created and the native action's result reports no such award
- **AND** if truthful reporting cannot be admitted, the affected counselling action is refused with an
  explanation before cost/cooldown/effects; if safe refusal also cannot be admitted, the coupled
  exclusion group is unavailable at application and its requested choice is retained, not called applied.

### Requirement: Apparel and drug policies respect content selection

Excluded apparel SHALL not be equipped through the supported wear action or selected as new
required/generated apparel. Apparel already worn SHALL remain worn with its ordinary components
and effects. An excluded drug SHALL be absent from the current usable drug-policy catalogue under
S029, including existing policies, even if physical stock remains. Restoring that drug SHALL use
S029's fresh-row behavior rather than resurrecting discarded policy instructions. Existing drug
stock SHALL retain ordinary properties; this filter SHALL not independently rewrite all explicit
ingestion or medical-order rules.

Drug-item exclusion SHALL depend on available owned drug-policy maintenance. If original Drug
Policy Fix is active, or the required owned maintenance cannot run successfully, drug-item
exclusions SHALL be withheld with a visible reason and bounded warning. Requested individual and
preset choices SHALL remain stored, but those drug items SHALL not be shown as successfully
excluded. Other supported content families SHALL continue working. Application SHALL not leave
new drug exclusions active with contradictory policy rows, or partly publish an owned set of
policy changes when that application fails.

#### Scenario: Try to wear a previously acquired excluded garment
- **WHEN** the player orders a pawn to wear an excluded garment from storage
- **THEN** the supported wear action refuses it with an explanation
- **AND** a pawn already wearing another instance keeps its gear and effects

#### Scenario: Excluded drug still exists in storage
- **WHEN** content policy is applied with that drug excluded
- **THEN** current drug policies reconcile once to omit it, even though hauling and stock search
  still see its physical stacks

#### Scenario: Original Drug Policy Fix or unavailable maintenance
- **WHEN** original Drug Policy Fix is active or required owned policy maintenance is unavailable
- **THEN** requested drug-item exclusions are visibly withheld while other supported exclusions apply
- **AND** no claim is made that the original understands the owned content choices

#### Scenario: Drug-policy application fails
- **WHEN** policy cleanup fails while applying requested drug exclusions
- **THEN** the affected drug-exclusion application stays inactive, prior policy rows are retained,
  and the page explains the failure without presenting a partially applied policy as complete

### Requirement: Ideology choices and styles respect exclusions

With Ideology active, excluded memes, precepts and ritual patterns SHALL be absent from new
ideology generation and ordinary creation/reform selection. Excluded styles SHALL not be offered
or newly chosen for ordinary style selection and new production. Required apparel/building choices
within otherwise available precepts SHALL omit excluded candidates; if a mandatory requirement
has no valid candidate, that dependent choice SHALL be unavailable with a reason. Existing
ideologies, completed rituals, assigned roles and styled objects SHALL remain unchanged until an
ordinary player action changes them. Exclusion SHALL not rewrite a save's already baked styles.

#### Scenario: Create or reform an ideology
- **WHEN** the player opens ordinary ideology selection under exclusions
- **THEN** excluded choices and unusable dependent requirements are unavailable with appropriate
  reasons, and legal remaining choices retain ordinary constraints

#### Scenario: Existing styled objects
- **WHEN** an excluded style is already used by buildings or stored items
- **THEN** those objects retain their appearance while new ordinary style choices omit that style

### Requirement: Genes and abilities remain coherent

With the relevant content active, excluded genes and xenotypes SHALL not be selected in new
ordinary generation or ordinary gene/xenotype selection. Existing pawn genes SHALL remain. A
surviving generated xenotype SHALL omit excluded genes; if it cannot be generated validly, it SHALL
be unavailable with a reason. Excluded base-game abilities SHALL not be newly granted through
supported title or precept grants; already learned abilities SHALL remain usable under ordinary
rules. Without the corresponding DLC, choices SHALL remain unavailable rather than treating an
installed but inactive DLC as active.

#### Scenario: Generate a surviving xenotype with an excluded gene
- **WHEN** a new pawn receives that xenotype through supported generation
- **THEN** it omits the excluded gene if the result is valid, otherwise that choice is rejected
- **AND** an existing pawn's genes are not removed

#### Scenario: New versus existing ability grant
- **WHEN** a title or precept would newly grant an excluded ability
- **THEN** it does not grant it, while a previously learned instance remains available

### Requirement: Optional powers and psycasts use the same policy

When supported Ancients content is active, excluded powers SHALL be absent from new power
selection. With supported Vanilla Psycasts Expanded content active, excluded paths SHALL be absent
from new path selection and excluded psycasts SHALL not be offered or newly granted. Surviving
psycasts whose prerequisite was excluded SHALL retain its surviving ancestors and all independent
requirements; invalid or cyclic chains SHALL remain unavailable with a reason. Existing learned
powers and psycasts SHALL remain. Absent or incompatible providers SHALL not break base-game
content selection and SHALL be identified as unavailable integrations.

#### Scenario: Exclude a psycast in a prerequisite chain
- **WHEN** a player browses an otherwise allowed later psycast
- **THEN** the excluded intermediate psycast is not offered, its surviving ancestral and independent
  requirements remain, and existing learned psycasts are not erased

#### Scenario: Optional provider is absent or changed
- **WHEN** powers or psycasts cannot be controlled by a supported integration
- **THEN** their saved choices are retained as unavailable, while supported base-game exclusions work

### Requirement: Interface exclusions hide choices without deleting mechanics

Excluded work types SHALL hide their corresponding ordinary work-priority columns but SHALL not
disable their jobs or erase pawn priorities. Excluded main-menu-bar tabs SHALL be hidden without
deleting their mechanic. Excluded storytellers and scenarios SHALL be absent from new selection;
the current storyteller and existing game scenario effects SHALL remain. Excluded special item
filters SHALL be omitted from supported storage/recipe filter menus without broadening existing
stored acceptance rules. Existing rules that depend on a hidden filter SHALL remain respected and
explainable. Excluded random social interactions SHALL not be chosen randomly; unrelated explicit
social actions SHALL retain their normal behavior. Developer spawn selectors SHALL omit excluded
physical content; no new cheat access SHALL appear in ordinary settings or bottom tabs.

#### Scenario: Hide a work column
- **WHEN** the player opens work priorities after excluding a work type
- **THEN** that column is absent, while existing priorities and eligible jobs continue to operate

#### Scenario: Hidden filter and existing storage policy
- **WHEN** a special item filter is hidden but an existing storage policy depends on it
- **THEN** the hidden choice does not silently expand what the storage accepts
- **AND** the player can inspect why an item is still disallowed

#### Scenario: Hidden selectors and random interactions
- **WHEN** the player opens ordinary tab/storyteller/scenario choices or the game selects a random
  social interaction
- **THEN** excluded choices are absent without stopping an existing storyteller, deleting a tab's
  underlying mechanic, or rewriting unrelated explicit social actions

### Requirement: Owned consumers share availability without losing physical stock

The mod SHALL give its bill browser, Architect, drug-policy reconciliation, storage/search and
hauling features one consistent applied content policy. Browsing or sorting SHALL not reintroduce an
excluded production choice. Physical inventory searches and hauling SHALL still include existing
excluded things when otherwise eligible. Content exclusion SHALL not bypass reservations, capacity,
permissions, research or job eligibility. After policy application, dependent owned views SHALL
refresh consistently before presenting new actions.

#### Scenario: Search and production disagree for a legitimate reason
- **WHEN** excluded material exists in storage and the player searches stock and browses recipes
- **THEN** stock search can find the real material while production refuses to select it as a new
  ingredient, with the exclusion explained
- **AND** hauling still observes ordinary capacity and destination restrictions

#### Scenario: A linked production plan becomes blocked
- **WHEN** policy application blocks a bill shared by linked workbenches
- **THEN** every linked member reports the same unavailable production choice and retains the
  shared plan, recoverable work and existing quota state
- **AND** blocking or later resuming does not count a finished product, issue duplicate work credit,
  or silently reset the group's quota
