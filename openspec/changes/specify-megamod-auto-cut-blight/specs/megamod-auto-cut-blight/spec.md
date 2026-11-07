# Automatic blight cutting

Owner: **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`; **RimWorld 1.7 beta**.
Origin: S069 Auto-Cut Blight replacement. Draft observable behavior; nothing implemented or accepted.

## ADDED Requirements

### Requirement: New infection receives an ordinary cut order
The feature SHALL, while enabled and available, place one ordinary cut order on a newly blighted, revealed plant that the player can ordinarily designate for cutting without additional protection confirmation.
The plant must still exist on a player-controllable map. This applies across that map, including
outside growing zones and hydroponics; it does not require a home-area or linked-zone membership.
Automatic marking preserves existing equivalent and unrelated orders and does not un-forbid plants.
An existing tree-extraction order takes precedence: this feature does not add a competing cut or
select its preferred cutting work for that tree while extraction remains ordered. Existing competing
orders are not erased, and extraction retains normal eligibility rather than being guaranteed to succeed.

#### Scenario: New eligible infection inside or outside a growing zone
- **WHEN** a revealed eligible plant becomes blighted while automatic blight cutting is enabled
- **THEN** it receives an ordinary visible cut order whether or not it belongs to a growing zone
- **AND** nearby healthy plants receive no new order from this automation

#### Scenario: Hidden or protected plant
- **WHEN** a plant becomes blighted in fog, without player cutting authority, or requiring a protection confirmation
- **THEN** this automation does not place an order or bypass that restriction

#### Scenario: Existing orders survive automatic marking
- **WHEN** a newly infected eligible plant already has a cut order or another designation, without a tree-extraction order
- **THEN** the existing cut order is reused or one missing cut order is added
- **AND** unrelated orders are not silently removed or duplicated

#### Scenario: Infection while extraction is already ordered
- **WHEN** a tree awaiting extraction becomes blighted, whether extraction was chosen manually or through advance clearing
- **THEN** this feature does not add a competing cut or give a coexisting cut order its blight priority
- **AND** the extraction order and its normal work eligibility remain intact
- **AND** cancelling extraction alone does not trigger an automatic cut of the already infected tree; the player can explicitly order cutting

#### Scenario: Infection did not actually occur
- **WHEN** an attempted infection leaves the plant uninfected, already infected, or no longer present
- **THEN** it creates no new automatic cut order

### Requirement: Blighted cutting participates in native work priorities
The feature SHALL prefer currently blighted plants with cut orders over ordinary plant cutting within Plant Cutting, using the game's emergency-work eligibility and existing pawn priorities.
Disabled Plant Cutting stays disabled. Emergency eligibility follows its priority relative to other
enabled ordinary work, not a fixed requirement to use priority 1. Other work-type ordering, needs,
safety responses and job interruption remain under normal game control.

#### Scenario: Blighted work is preferred within enabled plant cutting
- **WHEN** a pawn chooses eligible Plant Cutting work and both blighted and ordinary cut targets are available
- **THEN** the blighted cut target is preferred through this feature's priority
- **AND** the pawn travels to it and performs normal cutting work rather than instantly destroying it

#### Scenario: Emergency eligibility respects relative work priorities
- **WHEN** Plant Cutting is priority 2 and the pawn's strongest enabled ordinary work is priority 2
- **THEN** blighted cutting is eligible for the game's emergency-work selection
- **AND** when another ordinary work type is priority 1 instead, blighted cutting follows normal work selection rather than gaining that emergency eligibility

#### Scenario: Disabled or ineligible worker
- **WHEN** Plant Cutting is disabled or a pawn cannot ordinarily perform the cutting because of access, willingness, protection, burning, reservation or required capability
- **THEN** this feature does not bypass that restriction to assign the cut
- **AND** enabling automation does not rewrite pawn work priorities or force every pawn to abandon its current job

### Requirement: Manual and automatic orders share actual cutting behavior
The feature SHALL apply its blight priority to eligible ordinary cut orders regardless of whether they were placed automatically, manually, by the order tools or by a compatible provider, except while the plant has an extraction order.
Normal movement, cutting effort, speed, collection, experience and job cleanup remain in force.
The feature adds no healthy-plant buffer, yield bonus, experience bonus or increased infection/spread.
Existing native continuation to adjacent blighted plants remains available.

#### Scenario: Manual response to an existing outbreak
- **WHEN** the player uses the existing blight-cutting command on an eligible outbreak
- **THEN** its ordinary cut orders qualify for the same blight priority while this feature is enabled
- **AND** the player uses the existing command surface without a second blight-order type

#### Scenario: Harvest and cut orders coexist
- **WHEN** an eligible blighted plant has both a harvest order and a cut order
- **THEN** this feature's preferred work cuts the plant rather than choosing harvesting instead
- **AND** merely selecting that work does not clear unrelated orders in advance

#### Scenario: Native adjacent blight continuation
- **WHEN** completing a normal cut causes the game to continue cutting adjacent blighted plants
- **THEN** that native continuation remains available with its normal eligibility and target cleanup
- **AND** this feature adds no second continuation or healthy-crop clearing step

### Requirement: Event scope and cancellation remain explicit
The feature SHALL respond to new infections rather than automatically scanning old outbreaks when a save loads, the option is enabled or a cell is revealed.
Cancelling an order removes it normally. Repeated notification of the same infection cannot recreate
it through this feature. A later actual reinfection, explicit manual action or native adjacent-cut
continuation can create a new order. Recovery or disabling does not silently erase existing orders.

#### Scenario: Old outbreak on enable or reload
- **WHEN** an already infected plant without a cut order is loaded, revealed or present when automation is enabled
- **THEN** this feature leaves it without an automatic order until a genuinely new infection occurs
- **AND** the existing manual blight-cutting command remains usable

#### Scenario: Cancel without a new trigger
- **WHEN** the player cancels a plant's cut order and no new infection, manual order or native adjacent-cut continuation occurs
- **THEN** loading the save or repeating notification of that same infection does not restore its order

#### Scenario: Recovery leaves the explicit order
- **WHEN** a cut-designated plant ceases to be blighted
- **THEN** it no longer qualifies for this feature's blight priority at the next work choice
- **AND** its ordinary cut order remains until normally completed, removed or cancelled, while already running work follows normal game behavior

#### Scenario: Disable during an outbreak
- **WHEN** the player disables automatic blight cutting
- **THEN** new owned automatic marking and blight-priority choices stop
- **AND** existing ordinary cut orders, native running work and manual cutting remain available
- **AND** the control explains that this does not stop other providers or the game's own cutting continuation

### Requirement: One common preference controls the behavior
The feature SHALL expose Automatic blight cutting as one initially enabled preference in Plants / Blight settings and one small bottom-right operational toggle.
The toggle changes the current save's same preference, not another independently persisted option.
Settings show Global default, Current save and Use global; missing save choices inherit the live
global value. Per-plant orders are gameplay state and are not presented as mod settings.

#### Scenario: Toggle writes a save override
- **WHEN** the player changes the bottom-right toggle during a loaded game
- **THEN** it writes an explicit current-save override and the settings row shows the same effective choice
- **AND** other saves and the global default are unchanged

#### Scenario: Global inheritance and dormant override
- **WHEN** Use global is checked
- **THEN** the save follows current global changes and its own editor is inactive while its prior override is retained
- **AND** unchecking restores that override or seeds the first override from the current effective global value

#### Scenario: Main menu and save switching
- **WHEN** settings are opened without an active save
- **THEN** Current save, Use global and their mutation actions are inactive with an explanation, while Global default remains editable
- **AND** loading or switching saves selects that save's own preference without changing its ordinary plant orders

#### Scenario: Native placement and readable controls
- **WHEN** this feature is available in a loaded game, including world view
- **THEN** its effective state and scope are readable through the small operational control and common settings
- **AND** no additional bottom tab, work column or debug control is created
- **AND** all new labels, tooltips and unavailable explanations are translated in the seven baseline languages

### Requirement: Availability and provider authority are truthful
The feature SHALL apply each enabled contribution only when its documented compatibility conditions hold, allowing compatible changes by other mods.
Automatic marking and preferred work may be available independently. Unavailable contributions
leave ordinary game/manual behavior intact, retain requested preferences and visibly explain what
is missing. Each skipped enabled patch produces a bounded startup warning identifying the failed
condition, unavailable behavior and fallback; no repeated work-time warning is produced.

#### Scenario: Only one contribution is available
- **WHEN** automatic marking is compatible but preferred work is unavailable, or conversely
- **THEN** the admitted contribution remains usable and the control/settings identify which part is unavailable
- **AND** ordinary cut work or manual marking supplies the stated fallback respectively
- **AND** if neither is available, the control shows unavailable rather than claiming working automation

#### Scenario: Compatible producer already marked the plant
- **WHEN** another compatible provider has placed the equivalent ordinary cut order
- **THEN** the order is reused without rejecting compatibility solely because another provider exists
- **AND** disabling this feature does not change that provider's settings or promise to stop its orders

#### Scenario: Original Auto-Cut Blight is active
- **WHEN** the selected original Auto-Cut Blight provider is active alongside the megamod
- **THEN** it alone supplies the overlapping feature and the owned controls explain that authority
- **AND** owned preferences remain dormant without removing the original's work or changing its settings

#### Scenario: Legacy source save is not silently converted
- **WHEN** a save contains source-specific blight orders or jobs
- **THEN** this feature does not silently relabel, erase or claim to migrate them
- **AND** documentation identifies that migration and safe removal require separate verification
