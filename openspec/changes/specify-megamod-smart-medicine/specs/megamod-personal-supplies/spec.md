# Personal supplies

Owner **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`; **1.7 beta**.
S062 observable draft only. Common medical configuration/fallback applies.

## ADDED Requirements

### Requirement: Pawn inventory controls expose real individual supply targets
PS01. A controllable free colonist with an inventory SHALL have contextual personal-supply controls
showing item type, current carried personal quantity, desired quantity and any shortfall/pause reason.
Default catalogue SHALL include available medicines, drugs, hemogen and supported portable bedrolls.
An extended catalogue SHALL permit other real haulable item types with search; listing a type does
not grant equipment, ingestion or creation permission. Empty/zero targets request no stock.
Owned target counts SHALL be whole numbers0–9999 consistently in every editor and increment control.
Native groups with tighter limits SHALL retain those limits and show why.

Targets SHALL be individual saved orders, not inherited settings. Existing native 1.7 stock choices
and drug-policy quantities SHALL remain visible contributing orders. Multiple minimum requirements
for the same item SHALL combine as their **maximum**, not their sum: wanting3 by one rule and5 by
another means5, not8. Physical cargo reserved for another job does not satisfy personal supply.
One current item quantity cannot satisfy two physical destinations simultaneously.

#### Scenario: Medicine and drug orders overlap
- **WHEN** native stocking requests3 units and another applicable personal order requests5 of the same item
- **THEN** the effective target is5, both contributors are visible, and two independent stocking jobs do not request8.

#### Scenario: Partial supply and full inventory
- **WHEN** a colonist carries2 of5 requested units but lacks room for another
- **THEN** the UI shows the real2/5 shortage and capacity reason rather than claiming complete stock or hiding the item because some are present.

#### Scenario: Extended catalogue
- **WHEN** the player enables the extended catalogue and requests a real haulable item
- **THEN** it becomes an ordinary physical pickup target, with no spawned item, free capacity or developer-mode requirement.

### Requirement: Editing copying and dropping preserve clear player intent
PS02. Edits SHALL refresh effective targets and next-action decisions immediately, without a stale
cooldown for old values. Copy SHALL capture a snapshot of the selected pawn's owned targets; Paste
SHALL apply that snapshot as one complete replacement of the destination's owned targets, leaving
independent native/drug contributors unchanged and visible. Invalid/unavailable entries SHALL be
reported and retained as unavailable without substituting another type. A failed paste SHALL leave
the previous owned set intact. Clearing one pawn SHALL not invalidate an unrelated copied snapshot.

A deliberate inventory Drop SHALL stop that pawn's owned replenishment target for that item type,
including partial drops, and use the ordinary physical drop action. Independent contributing orders
that still request it SHALL be clearly indicated; this action does not silently rewrite drug policy.
Automatic donation, consumption, interruption or job delivery SHALL not count as a deliberate stop.
Removing an owned target SHALL not immediately destroy, teleport or consume its carried stock.

#### Scenario: Source changes after Copy
- **WHEN** targets are copied, the source pawn's orders change, and the snapshot is pasted elsewhere
- **THEN** the destination receives the copy-time owned values and independent contributors remain unchanged.

#### Scenario: Invalid paste
- **WHEN** a paste cannot be applied safely
- **THEN** the prior destination targets remain intact and the player receives a clear explanation rather than a partially replaced set.

#### Scenario: Manual drop versus medical donation
- **WHEN** a pawn deliberately drops a stocked medicine, or instead donates a dose during valid treatment
- **THEN** only the manual drop stops the owned target; any surviving native/drug request is shown, while medical donation leaves replenishment intent intact.

### Requirement: Scarcity controls replenishment using one current supply total
PS03. Automatic replenishment SHALL initially require colony supply of an item to be at least
**150% of the sum of effective personal targets for spawned free colonists on that map**. The
threshold SHALL be configurable0–500%;0 disables this gate. Count each known permitted physical
unit once across map stock and those colonists' personal inventories, including relevant minified
contents, but excluding other maps, inaccessible contents and quantities committed to other jobs.
It is a total-supply gate, not a promise that50% will remain on shelves after each pickup. Actual
pickup still requires the requesting pawn's current reachability and permissions.

Insufficient supply SHALL pause automatic replenishment, visibly. Return under scarcity starts Off;
when on, owned personal quantities can be returned to ordinary eligible storage only to the extent
they are not required by another active retention rule. Over-target owned excess can return
regardless of scarcity. No destination means it remains carried with a reason. Repeated choices
under unchanged supply SHALL not oscillate pickup/drop. A return does not change the saved target.

#### Scenario: Threshold boundary
- **WHEN** targets total10 and eligible total supply changes from14 to15 at the default150%
- **THEN** replenishment changes from paused to permitted, subject to actual accessible stock and capacity.

#### Scenario: Supply already in inventories
- **WHEN** part of the colony's stock is already carried personally
- **THEN** those real units count once in supply and do not become both ground stock and inventory stock.

#### Scenario: Return is enabled but another policy retains some
- **WHEN** scarcity return is enabled and a pawn's owned target exceeds an independent retained quantity
- **THEN** only unprotected owned quantities can return through ordinary hauling; protected units and saved intent remain.

### Requirement: Stocking is real work within shared carrying capacity
PS04. Replenishment SHALL use reachable permitted reservable supplies and collect only the current
shortfall that fits the pawn's shared inventory-and-hands capacity, including equipment, personal
supplies and S021/S126 cargo. It SHALL not grant a second medical inventory allowance. Another
pawn's protected personal inventory is not an automatic restocking depot. Split, merge, minified,
transfer, consumption and destruction events SHALL update physical quantities before reuse.

Returning excess SHALL use a real reachable accepting storage destination, current storage filters,
priority and enabled refill limits. The effective quantity is checked again at pickup/delivery.
No hidden teleport, un-forbidding, discarded remainder or duplicate count is permitted. A moving
item remains physical through interruption, drafting, death, map departure and save/load.

#### Scenario: Capacity permits only one more unit
- **WHEN** the target shortfall is5 but shared remaining capacity permits1
- **THEN** at most1 unit is collected and the remaining shortage stays visible.

#### Scenario: Destination rejects the return
- **WHEN** a storage filter or refill limit changes before delivery
- **THEN** the pawn chooses another permitted destination or keeps the real item with a visible reason, without silently dropping ownership or exceeding the limit.

#### Scenario: Interrupted while holding a split stack
- **WHEN** stocking or return is interrupted after a real split/transfer
- **THEN** the actual split quantities and their personal/cargo roles survive and cannot be collected or delivered twice.

### Requirement: Optional personal errands yield to available medical work
PS05. Automatic personal restocking and scarcity returns SHALL yield when the same pawn has Doctor
work enabled and a currently executable permitted tending or operation job available. This SHALL
use actual worker/patient/job eligibility, not merely the existence of a sick or unreachable pawn.
It SHALL not reorder already valid explicit commands, enable Doctor work or turn an impossible
operation into permanent suppression of supplies. S024 cleanup and S125 opportunistic hauling SHALL
not insert elective errands ahead of that ready care or hijack reserved medical ingredients.

Disabling owned stocking SHALL stop new owned stock/return errands while preserving saved targets,
existing native policies and real inventory. Already held job items SHALL complete safe recovery
or ordinary delivery; enabling the setting again does not execute work during settings rendering.

#### Scenario: Ready treatment beats topping up
- **WHEN** a doctor could restock personal medicine but has an executable tending job
- **THEN** the elective stock errand yields to care without reserving its supplies or adding a hauling detour.

#### Scenario: Inaccessible patient or impossible operation
- **WHEN** the only medical work cannot currently be performed by this pawn
- **THEN** it does not indefinitely prevent an otherwise eligible personal-stock job.

#### Scenario: Stocking disabled while carrying
- **WHEN** owned stocking is turned off after a pickup
- **THEN** no new owned errands begin, targets remain saved and the real acquired items are recovered without deletion or duplicate pickup.

### Requirement: Dispensed supplies require a real dispensing action
PS06. A requested nutrient-paste supply SHALL be obtainable only through the normal eligible
dispenser workflow, using real ingredients, power, access and carrying capacity. Looking for work,
opening inventory, counting stock or failing a candidate check SHALL produce no meal or resource
change. A dispenser merely being able to dispense SHALL not bypass the scarcity gate by inventing
stock. The conservative initial rule counts only already-existing qualifying meals for that gate;
setting the threshold to0 permits normal physical dispensing without that gate.

Routine meal targets remain S123's separate composition; no extra meal allowance is added here.
Ordinary drug/food ingest controls SHALL invoke the same permitted native action once and consume
actual items, without automatic dosing, treatment-response policy or unrestricted drug use.

#### Scenario: Job search examines a dispenser repeatedly
- **WHEN** repeated stock checks find an available dispenser but no pickup is performed
- **THEN** no paste meal appears and no ingredients or power-dependent production are consumed by the checks.

#### Scenario: Real dispensing and interrupted collection
- **WHEN** a valid stock job dispenses a meal and is interrupted before collection
- **THEN** the real meal and resource cost remain accounted for once and the next job does not pretend it was already carried.

### Requirement: Retention and medicine information use current physical state
PS07. Automatic unloading and caravan-return cleanup SHALL retain only effective personal
quantities, preserving native drug/food/equipment requirements and S021 exact cargo ownership.
Target0 SHALL not protect an entire item type. Medical use can consume a permitted personal dose;
remaining personal stock and shortage update once. Temporary unused medicine SHALL not become
permanent personal stock without an order. Away-world caravan treatment remains native; this
feature does not remotely restock a caravan or count its supplies on a map it has left.

Low-medicine information SHALL include accessible colony inventory medicine as well as ordinary
eligible ground stock without duplication, while distinguishing restricted/unavailable stock from
actually usable supply. Provider/admission uncertainty SHALL not suppress an existing warning on
the strength of an unverified inventory count. Alerts SHALL not reserve, transfer or consume items.

#### Scenario: Five carried with a target of three
- **WHEN**3 units are required personally and2 are unrelated unloadable excess
- **THEN** ordinary cleanup can unload2 while retaining3 rather than protecting all5 or unloading everything.

#### Scenario: Caravan returns then medicine is consumed
- **WHEN** a pawn returns with personal medicine and subsequently uses a dose
- **THEN** valid personal quantities remain through unloading, one dose is consumed, and the displayed shortage changes accordingly.

#### Scenario: Warning sees unreachable or reserved stock
- **WHEN** inventory contains medicine that cannot currently serve the colony's ordinary medical use
- **THEN** it is not reported as freely available or used to hide a low-medicine warning, and the physical items remain untouched.
