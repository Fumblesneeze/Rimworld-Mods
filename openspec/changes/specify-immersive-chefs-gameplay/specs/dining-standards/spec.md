## ADDED Requirements

**Owning mod:** Immersive Chefs (`fumblesneeze.immersivechefs`) at `mods/ImmersiveChefs`.

### Requirement: Eaters acquire service ware

For an eligible dining job in `Strict` or `Prefer` ware mode, the eater or owning Gastronomy server SHALL try to reserve and collect one reachable cutlery unit in addition to the meal while preserving normal reachability, reservation, forbidden-item, and danger rules. Selection SHALL be deterministic: clean cutlery first; if none is eligible, dirty cutlery only when `DirtyWareFallback=Always` or when it is `Urgent only` and the diner is at or below `EmergencyHungerThreshold`; otherwise no cutlery. Failure to obtain a permitted setting SHALL NOT block ingestion and SHALL record the missing-cutlery dining consequence. In `Off` mode the workflow SHALL neither select cutlery nor record a missing-cutlery consequence. Pemmican, packaged/travel survival meals, raw food, drinks, drugs, baby food, and animal feeding SHALL remain hand-eaten exclusions. A non-humanlike animal eating any meal SHALL never acquire plate or cutlery and SHALL receive no Immersive Chefs temperature, culinary-quality, tableware, dining-standard, memory/thought, or custom food-poisoning consequence.

#### Scenario: Animal eats a normal meal

- **WHEN** a non-humanlike animal ingests a covered meal at any temperature or culinary quality
- **THEN** Immersive Chefs applies no ware acquisition, dining memory/thought, or custom poisoning behavior, and any embedded plate is recovered unused without changing sanitation

#### Scenario: Map animal ignores nearby cutlery

- **WHEN** a non-humanlike animal on a regular map chooses and eats a covered plated meal while eligible cutlery is reachable beside it
- **THEN** the animal follows RimWorld's ordinary ingest job without reserving, collecting, carrying, dirtying, or moving the cutlery; the exact embedded plate is recovered clean at the eating location

#### Scenario: Colonist collects cutlery

- **WHEN** a colonist chooses an eligible plated meal and reachable clean cutlery exists
- **THEN** the eating job reserves and collects both without another pawn claiming either item

#### Scenario: Dirty cutlery is allowed only by fallback policy

- **WHEN** no clean setting is eligible, a dirty setting is reachable, and `DirtyWareFallback=Always`
- **THEN** the eater or server reserves that dirty setting and its sanitation contributes to the dining consequence and poisoning calculation

#### Scenario: No permitted cutlery remains non-blocking

- **WHEN** no clean setting exists and dirty settings are forbidden by the current fallback policy
- **THEN** the pawn eats without cutlery and receives the missing-cutlery consequence rather than waiting indefinitely

#### Scenario: Externally spawned unplated meal reaches an ingest driver

- **WHEN** another mod, a trader, a visitor, or a debug action supplies a covered meal without an embedded plate and RimWorld has already admitted its ordinary self-ingest or assisted-feeding job while no permitted plate or cutlery exists
- **THEN** Immersive Chefs leaves the successful native pre-toil reservation successful, attaches a missing-ware dining session when possible, and allows the job to continue without a `TryMakePreToilReservations returned false ... right after StartJob` warning
- **THEN** any stricter ordinary-selection policy MUST reject before `StartJob` and MUST NOT turn a successful driver reservation into a late failure

#### Scenario: Inventory meal reaches the fallback microwave without a restart loop

- **WHEN** `JobGiver_GetFood` starts an ordinary `Ingest` job for a covered cold meal already held in the diner's inventory and an operational Immersive Chefs fallback microwave is available
- **THEN** the dining toils transfer that exact meal directly from inventory into the native carry tracker without first running a spawned-map-target approach toil
- **AND** the same admitted job reaches reheating and ingestion once, without an incompletable toil or same-tick `Ingest` restart loop

#### Scenario: Patient feeding preserves RimWorld's inventory transfer graph

- **WHEN** an already-admitted `FeedPatient` job targets a covered meal held in the feeder's inventory and an operational Immersive Chefs fallback microwave is available
- **THEN** Immersive Chefs preserves RimWorld's native inventory-to-carrier and patient-feeding toil graph without prepending a spawned-map-target approach
- **AND** if the installed feed driver exposes no validated reheat insertion seam, Immersive Chefs skips only optional reheating so the same admitted job can feed the patient without an incompletable toil or same-tick `FeedPatient` restart loop
- **AND** that feeding session does not reserve an unused fallback microwave, so another eligible dining job may reserve and use it normally

#### Scenario: Hand-eaten food is excluded

- **WHEN** a pawn chooses pemmican or a packaged survival meal
- **THEN** the eating job proceeds without plate or cutlery requirements and without missing-ware thoughts

### Requirement: Missing or dirty ware affects one dining experience

The mod SHALL calculate plate and cutlery cleanliness, material comfort, and craftsmanship for the ingestion event. Missing required ware or using dirty ware SHALL contribute to one combined dining-standard thought and SHALL apply the configured poisoning modifier rather than stacking several equivalent negative thoughts.

#### Scenario: Dirty wooden service gives one consequence thought

- **WHEN** an eligible pawn consumes a meal from a dirty wooden plate with dirty wooden cutlery
- **THEN** the ingestion outcome includes their bounded cleanliness and comfort penalties and at most one unmet-dining-standard thought

### Requirement: Used tableware remains at the dining place

When an eligible humanlike pawn completes map dining at a vanilla-resolved eat surface, the exact used colony plate and cutlery SHALL be returned dirty together to the same selected table cell identified by the native ingest job's `TargetIndex.B`. Immersive Chefs SHALL raise only dining-surface item capacity to two and SHALL retain plate and cutlery as separate physical Things; RimWorld's native multiple-items-per-cell rendering SHALL provide their distinct shelf-like offsets. Dining tables MUST NOT become storage destinations. Both Things SHALL remain physically at that dining place until a pawn claims them for clearing. If the selected table cell cannot retain either exact item, that item MAY use another occupied cell of the same physical table and then the immediate near-diner fallback rather than bypassing RimWorld placement rules. Immersive Chefs MUST NOT infer a different nearby table after ingestion, fabricate a visual substitute, merge away either exact unit, or place used ware on a table when vanilla selected no eat surface. No-table dining and assisted feeding SHALL retain the existing near-diner/patient release behavior. Caravan dining SHALL retain exact inventory return, and personally owned visitor or trader ware SHALL retain personal-inventory return. Gastronomy ownership SHALL still hand the same table setting to its waiter clearing workflow.

#### Scenario: Colonist finishes a meal at a table
- **WHEN** vanilla dining selected a valid table cell in `TargetIndex.B` and an eligible colonist completes ingestion using a colony plate and cutlery
- **THEN** the exact dirty plate and cutlery occupy the selected table cell together as two separately rendered Things, or use another cell of that same table and finally the immediate near-diner fallback only when exact placement is unavailable
- **THEN** they remain there until an ordinary cleaning, hauling, Common Sense, or Gastronomy workflow claims them

#### Scenario: Colonist eats without a table
- **WHEN** vanilla dining leaves `TargetIndex.B` invalid or without an eat surface
- **THEN** the exact dirty plate and cutlery are released near the dining pawn and are not moved onto an unrelated nearby table

#### Scenario: A placement integration temporarily rejects returned cutlery

- **WHEN** RimWorld resolves a valid near-diner candidate for exact returned cutlery but another active placement integration rejects that direct placement
- **THEN** Immersive Chefs retains the exact marked cutlery in its responsible pawn inventory and retries from a freshly resolved candidate without bypassing the placement integration
- **AND** the failed attempt does not enter RimWorld's noisy repeated `Near` placement loop, emit `Failed to place`, lose or duplicate the cutlery, or leave it permanently stranded after an eligible candidate becomes available

#### Scenario: Personal and caravan ownership override map-table retention
- **WHEN** a visiting pawn consumes its personally owned setting or a caravan pawn completes world-holder ingestion
- **THEN** the exact ware follows its personal-inventory or caravan-inventory return contract instead of remaining on a colony table

### Requirement: Guests use colony service before personal fallback

An eligible map guest SHALL search reachable, allowed colony cutlery before cutlery in the guest's own inventory. This behavior SHALL apply to ordinary non-hostile guests and, when `Orion.Hospitality` is active and its adapter validates, pawns recognized by Hospitality as arrived guests. Inventory fallback SHALL preserve the same clean-first and dirty-fallback policy. Colony ware SHALL remain colony property, and cancellation before eating SHALL return it without changing sanitation.

#### Scenario: Hospitality guest uses colony cutlery

- **WHEN** an arrived Hospitality guest starts an eligible meal and clean colony cutlery is reachable
- **THEN** the dining job selects the colony setting before otherwise eligible cutlery in the guest's inventory

#### Scenario: Guest falls back to personal cutlery

- **WHEN** no colony cutlery is eligible and the guest carries an eligible setting
- **THEN** that exact carried setting is used and returned dirty to the guest's inventory after eating

### Requirement: Independent children and assisted patients follow diner-aware rules

A child SHALL enter the ordinary plate, cutlery, quality, dining-standard, and active temperature-provider workflow as soon as vanilla permits that pawn to choose and eat food independently. Baby food, milk, and other non-self-feeding toddler workflows SHALL remain excluded. When one pawn feeds an eligible meal to another pawn, including a patient, the feeder SHALL acquire and carry the cutlery, but ware comfort, cleanliness, poisoning, and dining memories SHALL be evaluated for the fed pawn. If no permitted cutlery exists, the missing-cutlery memory SHALL be added to the fed pawn only when that pawn is conscious; the feeder SHALL NOT receive it on the patient's behalf.

On a spawned map, an eligible self-eating or assisted-feeding event completed without cutlery SHALL create one bounded instance of ordinary vanilla dirt at the actual eating location. It SHALL not create dirt for excluded food, aborted jobs, `WareRequirementMode=Off`, or a world-holder ingestion event with no map.

#### Scenario: Self-feeding child eats normally

- **WHEN** vanilla allows a child to start an ordinary eating job for an eligible meal
- **THEN** the child follows the same ware acquisition, dish return, and dining effects as an adult

#### Scenario: Nurse feeds a conscious patient without cutlery

- **WHEN** a nurse completes feeding an eligible meal to a conscious patient and no permitted cutlery exists
- **THEN** the patient receives the missing-cutlery consequence, the nurse does not, and one vanilla dirt event is created at the feeding location

#### Scenario: Nurse feeds an unconscious patient

- **WHEN** a nurse completes feeding an eligible meal to an unconscious patient without cutlery
- **THEN** no missing-cutlery memory is added to either pawn while physical plate recovery and the bounded dirt event still occur

#### Scenario: Nurse receives an unplated externally spawned meal

- **WHEN** an ordinary `FeedPatient` job has already been admitted for a covered meal with no embedded plate while no service ware is available
- **THEN** the feeder's successful native pre-toil reservation remains successful, feeding continues without an engine reservation warning, and the conscious-patient, unconscious-patient, dirt and missing-ware rules remain owned by the fed pawn

### Requirement: Caravan dining conserves reusable service ware

An eligible meal eaten from a caravan SHALL retain the same culinary state, plate, cutlery, dining effects, and active-provider temperature state as its map equivalent. While Thermodynamics - Hot Meals is absent and Immersive Chefs owns temperature, the serving SHALL cool toward the current caravan tile's outdoor temperature while held by the caravan. When Thermodynamics is active, Immersive Chefs SHALL neither cool the serving nor interpret Thermodynamics' state. The diner SHALL use the serving's exact embedded plate or, when an imported serving is unplated, SHALL select and attach one loose caravan plate under the clean-first fallback policy before ingestion. Cutlery SHALL be selected from the caravan inventory under the same policy. If an unplated serving has no permitted loose plate, ingestion SHALL remain non-blocking and record the missing-plate consequence. After ingestion, the exact used plate and any selected cutlery SHALL be returned to caravan inventory without replacement or duplication. Pemmican, packaged survival meals, registered travel foods, raw food, drinks, drugs, baby food, and animal feeding SHALL remain excluded.

The caravan journey SHALL abstract routine washing after each eligible dining event: returned ware becomes clean with wild-water wash provenance. It remains usable, but the next applicable poisoning calculation SHALL include the same wild-water provenance risk as ware washed at a water-terrain fallback on a map.

The same ownership rule SHALL cover a visiting trader or guest caravan temporarily spawned on a colony map. If the diner brought the meal in personal inventory, its exact embedded plate and personally sourced cutlery SHALL return dirty to that pawn's inventory after native ingestion rather than being dropped into colony space. If Gastronomy or another recognized colony service workflow supplied the meal or tableware, that service provenance SHALL win and the colony SHALL retain the returned dirty ware for clearing. Personal-versus-colony ownership MUST be captured before any server or ingestion transfer changes the current holder.

#### Scenario: Caravan pawn eats a plated meal

- **WHEN** a caravan pawn consumes an eligible serving with an embedded plate and selects caravan cutlery
- **THEN** the same plate and cutlery appear in caravan inventory after ingestion, clean with wild-water provenance, and no replacement items are spawned

#### Scenario: Caravan meal becomes cold

- **GIVEN** Thermodynamics - Hot Meals is absent and Immersive Chefs owns temperature
- **WHEN** an eligible meal remains in a caravan on a cold world tile long enough to cross a thermal boundary
- **THEN** its inspect state and eventual ingestion use the colder current serving temperature

#### Scenario: Caravan plates an imported serving at dining time

- **WHEN** an eligible unplated serving and an eligible loose plate exist in caravan inventory
- **THEN** that exact plate is attached for the dining event and returned after ingestion without requiring a map work surface

#### Scenario: Caravan pawn eats travel food

- **WHEN** a caravan pawn consumes pemmican or a registered packaged travel meal
- **THEN** no plate or cutlery is selected, returned, washed, or reported missing

#### Scenario: An arrived trader eats the meal it brought
- **WHEN** a trader-caravan pawn on the colony map consumes an eligible plated meal that originated in its personal inventory without colony restaurant service
- **THEN** the exact embedded plate returns dirty to that pawn's inventory after the meal disappears and is not dropped for colony ownership

#### Scenario: Personal visitor ware survives interrupted dining persistence
- **WHEN** the game is saved and loaded while an arrived trader or guest is carrying its personally owned plated meal and cutlery through an active ingestion job
- **THEN** loaded-game recovery interrupts the transient job without dropping the personal cutlery, preserves the meal's exact personal-plate provenance for the replacement native ingestion job, consumes any temporary recovery marker without duplicating either item, and leaves every unrelated kitchenware item in that visitor's inventory untouched
- **THEN** any positively marked colony ware whose first physical return fails remains marked and its exact pawn-and-Thing identity is retried by a bounded-per-tick pending-recovery path that does not depend on the interrupted job remaining active or disturb newly delivered active-session ware

#### Scenario: An arrived guest receives colony restaurant service
- **WHEN** Gastronomy supplies an eligible colony meal and colony place setting to a visiting pawn
- **THEN** the returned dirty plate and cutlery remain colony property for the server/clearing workflow rather than being inserted into the guest's inventory

### Requirement: Colony expectations scale gradually

When `ColonyDiningStandards` is enabled, the mod SHALL derive minimum service comfort, material category, meal complexity, and culinary-quality score from the pawn's current expectation level using the table below. Colony standards SHALL be disabled independently and SHALL not apply wealth expectations to prisoners.

For dining standards, the combined service comfort SHALL be `clamp((plate Dining Comfort + cutlery Dining Comfort) / 2, 0, 1)`. The material tiers SHALL be ordered as follows, and both the plate and cutlery SHALL meet the selected tier unless a row explicitly says otherwise:

- `Basic`: any eligible clean service-ware material.
- `Durable`: vanilla steel, registered plastic or ceramic where that item supports it, registered stainless steel, silver, or gold; wood and adobe do not qualify.
- `Refined`: registered ceramic where that item supports it, registered stainless steel, silver, or gold. For each plate or cutlery item that has neither a compatible ceramic nor stainless path registered, Good-or-better vanilla steel SHALL be the satisfiable base-game substitute for that item.
- `Silver`: vanilla silver or gold.
- `Gold`: vanilla gold.

Immersive Chefs SHALL add no ceramic material of its own. A ceramic route SHALL qualify only while a compatible ceramic material or fixed recipe is registered. Material compatibility extensions MAY add equivalents but MUST NOT lower the built-in rows silently.

| Current vanilla expectation | Minimum material | Minimum combined service comfort | Minimum meal complexity | Minimum culinary score |
| --- | --- | ---: | --- | ---: |
| Extremely Low | Basic | `0.00` | Simple | `0` (Awful) |
| Very Low | Basic | `0.10` | Simple | `20` (Poor) |
| Low | Durable | `0.20` | Simple | `35` (Normal) |
| Moderate | Durable | `0.30` | Simple | `35` (Normal) |
| High | Refined | `0.45` | Advanced | `50` (Good) |
| Sky-high or higher | Refined | `0.60` | Advanced | `65` (Excellent) |

For a modded expectation Def, the evaluator SHALL use its declared expectation order and select the greatest row whose vanilla expectation order is not above it, unless a compatibility extension explicitly supplies a row. An order below Extremely Low SHALL clamp to the Extremely Low row and an order above Sky-high SHALL clamp to the Sky-high row. This keeps an identical Def order deterministic across colony wealth changes and game languages.

#### Scenario: Expectations rise with colony wealth

- **WHEN** a free colonist moves from Low to High expectations
- **THEN** the minimum rises from Durable service, comfort `0.20`, Simple complexity, and score `35` to Refined service, comfort `0.45`, Advanced complexity, and score `50`
- **THEN** it remains below the highest Royalty requirement

#### Scenario: Prisoner receives service without wealth demand

- **WHEN** a prisoner eats a plated meal with available cutlery
- **THEN** ware effects apply but no colony-wealth dining expectation is evaluated

### Requirement: Royalty titles impose satisfiable standards

When Royalty and `RoyaltyDiningStandards` are enabled, the mod SHALL apply the following title rows. The effective requirement SHALL be the component-wise stricter of the pawn's colony-expectation row and title row, so acquiring a title never lowers an existing expectation. Freeholder and any lower title SHALL add no title-specific minimum. Gendered title equivalents SHALL use the same row.

| Vanilla title | Minimum material | Minimum combined service comfort | Minimum meal complexity | Minimum culinary score |
| --- | --- | ---: | --- | ---: |
| Yeoman or Acolyte | Refined | `0.40` | Advanced | `50` (Good) |
| Knight/Dame or Praetor | Silver | `0.55` | Advanced | `65` (Excellent) |
| Baron/Baroness | Silver | `0.70` | Elaborate | `65` (Excellent) |
| Count/Countess or higher | Gold | `0.80` | Elaborate | `80` (Masterwork) |

For a modded title, the evaluator SHALL use its declared title seniority and select the greatest listed vanilla row whose seniority is not above it, unless a compatibility extension explicitly supplies a row. A title below Yeoman adds no title-specific row and a title above Count clamps to the Count-or-higher row. Every title row MUST remain craftable when optional material mods are absent: Refined SHALL then accept its Good-or-better vanilla-steel substitute, Silver SHALL use base-game silver or gold, and Gold SHALL use base-game gold. Ceramic and stainless remain preferred Refined examples only when compatible material paths are actually registered.

#### Scenario: Lower title accepts stainless service

- **WHEN** a Yeoman eats an Advanced score-`50` meal using clean stainless plate and cutlery with combined service comfort at least `0.40`
- **THEN** the noble satisfies the title's service-material requirement

#### Scenario: Lower title has no optional material mod

- **WHEN** no compatible ceramic or stainless path is registered and a Yeoman uses Good-quality clean vanilla-steel service meeting the other row thresholds
- **THEN** the explicit base-game substitute satisfies the Refined material requirement without creating a ceramic Def

#### Scenario: Highest title rejects lesser service

- **WHEN** a Count or Countess eats from clean stainless service instead of gold service
- **THEN** the combined dining thought reports the unmet Gold material standard once even when the meal meets the Elaborate, comfort-`0.80`, and score-`80` thresholds

### Requirement: Dining standards settings are bounded and live

The mod SHALL provide live toggles for colony and Royalty dining standards, both enabled by default. Disabling either standards system SHALL stop new thoughts from that system without removing unrelated ware cleanliness or poisoning effects. `QualityMoodScale` is owned by the meal-state capability and scales only the culinary-quality mood effect; it SHALL NOT create a duplicate dining setting or scale the combined unmet-standard thought.

#### Scenario: Player disables colony standards

- **WHEN** `ColonyDiningStandards` is turned off during play
- **THEN** subsequent non-title meals do not evaluate colony expectation thresholds while service cleanliness continues to apply

### Requirement: Gastronomy delegates service ware handling

When the supported Gastronomy integration is active, waiters and servers SHALL bring reserved cutlery with meals and prioritize clearing dirty plates and cutlery after dining. Waiter delivery SHALL preserve the delivered physical Thing as a distinct non-merging inventory item. If the diner already opened a session using personal-inventory cutlery as a fallback, the waiter-delivered colony setting SHALL replace that session's active cutlery while the personal setting remains owned by the diner, separate, and clean. While Thermodynamics - Hot Meals is absent and Immersive Chefs owns temperature, waiters SHALL also reheat qualifying cold meals before delivery. With Thermodynamics active, Immersive Chefs SHALL add no service-time heating behavior. Eaters SHALL retain the normal fallback when no eligible server owns the order.

#### Scenario: Waiter serves a complete setting

- **WHEN** a Gastronomy waiter owns an eligible dining order with reachable clean cutlery
- **THEN** the waiter delivers the meal and cutlery and later exposes the dirty service ware to the clearing workflow

#### Scenario: Waiter supersedes a guest's personal fallback

- **WHEN** an arrived Hospitality guest has already selected personal cutlery and the owning Gastronomy waiter delivers a reachable colony setting
- **THEN** the dining session uses the distinct colony cutlery without merging into, taking, or dirtying the guest's personal setting
