## ADDED Requirements

**Owning mod:** Immersive Chefs (`fumblesneeze.immersivechefs`) at `mods/ImmersiveChefs`.

### Requirement: Eaters acquire service ware

For an eligible dining job in `Strict` or `Prefer` ware mode, the eater or owning Gastronomy server SHALL try to reserve and collect one reachable silverware unit in addition to the meal while preserving normal reachability, reservation, forbidden-item, and danger rules. Selection SHALL be deterministic: clean silverware first; if none is eligible, dirty silverware only when `DirtyWareFallback=Always` or when it is `Urgent only` and the diner is at or below `EmergencyHungerThreshold`; otherwise no silverware. Failure to obtain a permitted setting SHALL NOT block ingestion and SHALL record the missing-silverware dining consequence. In `Off` mode the workflow SHALL neither select silverware nor record a missing-silverware consequence. Pemmican, packaged/travel survival meals, raw food, drinks, drugs, baby food, and animal feeding SHALL remain hand-eaten exclusions.

#### Scenario: Colonist collects silverware

- **WHEN** a colonist chooses an eligible plated meal and reachable clean silverware exists
- **THEN** the eating job reserves and collects both without another pawn claiming either item

#### Scenario: Dirty silverware is allowed only by fallback policy

- **WHEN** no clean setting is eligible, a dirty setting is reachable, and `DirtyWareFallback=Always`
- **THEN** the eater or server reserves that dirty setting and its sanitation contributes to the dining consequence and poisoning calculation

#### Scenario: No permitted silverware remains non-blocking

- **WHEN** no clean setting exists and dirty settings are forbidden by the current fallback policy
- **THEN** the pawn eats without silverware and receives the missing-silverware consequence rather than waiting indefinitely

#### Scenario: Hand-eaten food is excluded

- **WHEN** a pawn chooses pemmican or a packaged survival meal
- **THEN** the eating job proceeds without plate or silverware requirements and without missing-ware thoughts

### Requirement: Missing or dirty ware affects one dining experience

The mod SHALL calculate plate and silverware cleanliness, material comfort, and craftsmanship for the ingestion event. Missing required ware or using dirty ware SHALL contribute to one combined dining-standard thought and SHALL apply the configured poisoning modifier rather than stacking several equivalent negative thoughts.

#### Scenario: Dirty wooden service gives one consequence thought

- **WHEN** an eligible pawn consumes a meal from a dirty wooden plate with dirty wooden silverware
- **THEN** the ingestion outcome includes their bounded cleanliness and comfort penalties and at most one unmet-dining-standard thought

### Requirement: Guests use colony service before personal fallback

An eligible map guest SHALL search reachable, allowed colony silverware before silverware in the guest's own inventory. This behavior SHALL apply to ordinary non-hostile guests and, when `Orion.Hospitality` is active and its adapter validates, pawns recognized by Hospitality as arrived guests. Inventory fallback SHALL preserve the same clean-first and dirty-fallback policy. Colony ware SHALL remain colony property, and cancellation before eating SHALL return it without changing sanitation.

#### Scenario: Hospitality guest uses colony silverware

- **WHEN** an arrived Hospitality guest starts an eligible meal and clean colony silverware is reachable
- **THEN** the dining job selects the colony setting before otherwise eligible silverware in the guest's inventory

#### Scenario: Guest falls back to personal silverware

- **WHEN** no colony silverware is eligible and the guest carries an eligible setting
- **THEN** that exact carried setting is used and returned dirty to the guest's inventory after eating

### Requirement: Independent children and assisted patients follow diner-aware rules

A child SHALL enter the ordinary plate, silverware, temperature, quality, and dining-standard workflow as soon as vanilla permits that pawn to choose and eat food independently. Baby food, milk, and other non-self-feeding toddler workflows SHALL remain excluded. When one pawn feeds an eligible meal to another pawn, including a patient, the feeder SHALL acquire and carry the silverware, but ware comfort, cleanliness, poisoning, and dining memories SHALL be evaluated for the fed pawn. If no permitted silverware exists, the missing-silverware memory SHALL be added to the fed pawn only when that pawn is conscious; the feeder SHALL NOT receive it on the patient's behalf.

On a spawned map, an eligible self-eating or assisted-feeding event completed without silverware SHALL create one bounded instance of ordinary vanilla dirt at the actual eating location. It SHALL not create dirt for excluded food, aborted jobs, `WareRequirementMode=Off`, or a world-holder ingestion event with no map.

#### Scenario: Self-feeding child eats normally

- **WHEN** vanilla allows a child to start an ordinary eating job for an eligible meal
- **THEN** the child follows the same ware acquisition, dish return, and dining effects as an adult

#### Scenario: Nurse feeds a conscious patient without silverware

- **WHEN** a nurse completes feeding an eligible meal to a conscious patient and no permitted silverware exists
- **THEN** the patient receives the missing-silverware consequence, the nurse does not, and one vanilla dirt event is created at the feeding location

#### Scenario: Nurse feeds an unconscious patient

- **WHEN** a nurse completes feeding an eligible meal to an unconscious patient without silverware
- **THEN** no missing-silverware memory is added to either pawn while physical plate recovery and the bounded dirt event still occur

### Requirement: Caravan dining conserves reusable service ware

An eligible meal eaten from a caravan SHALL retain the same temperature, culinary state, plate, silverware, and dining effects as its map equivalent. The serving SHALL cool toward the current caravan tile's outdoor temperature while held by the caravan. The diner SHALL use the serving's exact embedded plate or, when an imported serving is unplated, SHALL select and attach one loose caravan plate under the clean-first fallback policy before ingestion. Silverware SHALL be selected from the caravan inventory under the same policy. If an unplated serving has no permitted loose plate, ingestion SHALL remain non-blocking and record the missing-plate consequence. After ingestion, the exact used plate and any selected silverware SHALL be returned to caravan inventory without replacement or duplication. Pemmican, packaged survival meals, registered travel foods, raw food, drinks, drugs, baby food, and animal feeding SHALL remain excluded.

The caravan journey SHALL abstract routine washing after each eligible dining event: returned ware becomes clean with wild-water wash provenance. It remains usable, but the next applicable poisoning calculation SHALL include the same wild-water provenance risk as ware washed at a water-terrain fallback on a map.

#### Scenario: Caravan pawn eats a plated meal

- **WHEN** a caravan pawn consumes an eligible serving with an embedded plate and selects caravan silverware
- **THEN** the same plate and silverware appear in caravan inventory after ingestion, clean with wild-water provenance, and no replacement items are spawned

#### Scenario: Caravan meal becomes cold

- **WHEN** an eligible meal remains in a caravan on a cold world tile long enough to cross a thermal boundary
- **THEN** its inspect state and eventual ingestion use the colder current serving temperature

#### Scenario: Caravan plates an imported serving at dining time

- **WHEN** an eligible unplated serving and an eligible loose plate exist in caravan inventory
- **THEN** that exact plate is attached for the dining event and returned after ingestion without requiring a map work surface

#### Scenario: Caravan pawn eats travel food

- **WHEN** a caravan pawn consumes pemmican or a registered packaged travel meal
- **THEN** no plate or silverware is selected, returned, washed, or reported missing

### Requirement: Colony expectations scale gradually

When `ColonyDiningStandards` is enabled, the mod SHALL derive minimum service comfort, material category, meal complexity, and culinary-quality score from the pawn's current expectation level using the table below. Colony standards SHALL be disabled independently and SHALL not apply wealth expectations to prisoners.

For dining standards, the combined service comfort SHALL be `clamp((plate Dining Comfort + silverware Dining Comfort) / 2, 0, 1)`. The material tiers SHALL be ordered as follows, and both the plate and silverware SHALL meet the selected tier unless a row explicitly says otherwise:

- `Basic`: any eligible clean service-ware material.
- `Durable`: vanilla steel, registered plastic or ceramic where that item supports it, registered stainless steel, silver, or gold; wood and adobe do not qualify.
- `Refined`: registered ceramic where that item supports it, registered stainless steel, silver, or gold. For each plate or silverware item that has neither a compatible ceramic nor stainless path registered, Good-or-better vanilla steel SHALL be the satisfiable base-game substitute for that item.
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

- **WHEN** a prisoner eats a plated meal with available silverware
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

- **WHEN** a Yeoman eats an Advanced score-`50` meal using clean stainless plate and silverware with combined service comfort at least `0.40`
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

When the supported Gastronomy integration is active, waiters and servers SHALL bring reserved silverware with meals, reheat qualifying cold meals before delivery, and prioritize clearing dirty plates and silverware after dining. Eaters SHALL retain the normal fallback when no eligible server owns the order.

#### Scenario: Waiter serves a complete setting

- **WHEN** a Gastronomy waiter owns an eligible dining order with reachable clean silverware
- **THEN** the waiter delivers the meal and silverware and later exposes the dirty service ware to the clearing workflow
