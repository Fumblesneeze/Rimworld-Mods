# Useful hauling detours

Owner **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`, **1.7 beta**.
S125 While You're Up replacement. Specification only; numerical defaults and deliberate corrections
are proposed for later verification. S021 owns cargo/capacity; S024 shares the bill decision.

## ADDED Requirements

### Requirement: Opportunities preserve the pawn's actual work permissions
The mod SHALL offer a storage detour only before an ordinary automatic job that permits one, for
a spawned player-controlled humanlike capable of the required hauling and original work.
Keep the ordinary age, work, access, allowed-area, danger and reservation rules. The parent job
must have a valid reachable destination at least three cells away. Drafting, explicit or queued
player orders, required job cleanup, medical emergencies, caravan preparation and transporter
loading do not acquire these detours. General and supply detours are both skipped while the pawn's
total bleeding rate is greater than 0.001. This does not stop its ordinary required work or care.
Animals and mechs retain S021's separately specified ordinary hauling; S125 grants no new work.
Goods already held, protected for personal use, or owned by another bill, cart or loading request
are not loose spare items. A current bill's own selected ingredients can be considered for its
authorized supply detour without releasing its claim to unrelated workers.

#### Scenario: Ordinary trip with useful loose goods
- **WHEN** an eligible pawn automatically travels to permitted work and a qualifying storage haul lies along the trip
- **THEN** it can physically collect and deliver those goods before returning to the still-valid work.

#### Scenario: Protected work and bleeding
- **WHEN** the pawn is following a forced or queued order, preparing a caravan, loading a transporter, handling an emergency, or bleeding above the threshold
- **THEN** neither a general nor a supply detour is inserted by this feature.

#### Scenario: No new abilities
- **WHEN** a guest, prisoner, animal, mech or humanlike unable to haul has a nearby item
- **THEN** S125 does not grant it an opportunistic hauling job or override its ordinary work restrictions.

### Requirement: General opportunities have a finite search and a useful destination
The mod SHALL consider a finite current set of eligible loose items and accept only real storage
deliveries within the selected search and travel limits. General opportunities require storage of
strictly higher priority than the item's current valid storage. No usable storage means no detour.
For a candidate, prefer the highest-priority qualifying destination; at equal priority prefer one
near the midpoint between the item and the parent destination, with stable ties. A failed candidate
does not turn into an unrestricted ordinary batch. Candidate discovery uses the same current item
facts as S021 and item search; each action still checks this pawn's permissions and physical items.
Newly appearing goods wait for a later trip. Search ends even with no candidate, impossible limits,
a closed route, an empty map or repeated identical job checks.

#### Scenario: Goods already stored at the best priority
- **WHEN** goods are already in valid best-priority storage and the pawn merely passes nearby
- **THEN** a general opportunity does not move them between equal-priority stores.

#### Scenario: Candidate changes during planning
- **WHEN** another pawn collects a candidate or fills its proposed destination
- **THEN** current permissions and capacity are rechecked and no duplicate pickup or imaginary free space is used.

#### Scenario: Nothing qualifies
- **WHEN** every current item fails the configured limits, including a zero distance limit
- **THEN** the search ends and ordinary scheduling continues without a repeating same-attempt search loop.

### Requirement: Search choices explain which opportunities they can miss
The mod SHALL offer **Nearby**, **Balanced** and **Thorough** search, initially **Balanced**.
Nearby keeps the configured start-to-item and storage-to-work distance and distance-ratio limits
as hard limits and uses the configured nearby-area search budgets. Balanced and Thorough first
prefer items within those limits, then allow farther candidates that satisfy the whole-trip rules.
Balanced checks the actual route for its first otherwise qualifying candidate and ends that search
if the route fails. Thorough can continue through the remaining finite candidates after that
failure. Neither mode promises the shortest trip or guarantees finding every possible opportunity.
All accepted routes must be reachable and safe under the pawn's applicable native permissions;
Nearby's approximate search never grants access through a forbidden or impassable route.

#### Scenario: Wall blocks the first promising candidate
- **WHEN** the first otherwise qualifying item requires an unacceptable actual detour around a wall
- **THEN** Balanced leaves the original work undelayed, while Thorough can consider another candidate within the same finite search.

#### Scenario: Useful item along a longer trip
- **WHEN** an item exceeds the initial proximity preferences but collecting and delivering it satisfies the whole-trip limits
- **THEN** Balanced or Thorough can select it, while Nearby rejects it if a hard proximity limit fails.

### Requirement: One whole trip consumes one travel budget
The mod SHALL limit the complete planned detour, including all pickups, all physical delivery
visits and return to the pending destination. Initially total travel is at most **170%** of the
original direct trip. Travel before the first delivery journey and after that journey combined
is at most **100%** of the original trip: count every pickup leg, movement between deliveries and
the final return, excluding only the leg from the last pickup to the first delivery. Equality is
allowed. For Balanced and Thorough, both straight-line screening and actual route cost must pass;
Nearby uses straight-line limits plus its bounded local search and ordinary route eligibility.
Settings describe these measures; a door, terrain, level or map boundary cannot be treated as a
zero-cost shortcut. Unsupported route measurement skips the affected detour. A zero-length or
unreachable original trip has no detour budget.
S021's finite additional-pickup radius still restricts a batch. Its nearest valid physical
delivery order determines the route being checked. Every extra pickup must fit the remaining
whole-trip budget; it never receives a fresh allowance. Required handling time is not removed.

#### Scenario: Exactly at the limits
- **WHEN** an original trip costs 100 travel units and the detour costs 170 with 100 counted outside its last-pickup-to-first-delivery leg
- **THEN** the trip meets these travel limits, subject to all other permissions and capacity checks.

#### Scenario: Extra pickup makes the whole batch too long
- **WHEN** each pickup looks reasonable alone but collecting and delivering both exceeds the original trip's limit
- **THEN** the second pickup is excluded and the first accepted trip keeps its original budget.

#### Scenario: Linked shelves are far apart
- **WHEN** two linked shelves share settings but require separate physical visits
- **THEN** both visits count in the trip and shared settings do not erase travel or create capacity.

### Requirement: Supply relocation helps the selected construction or bill
With the respective supply option enabled, the mod SHALL allow one useful storage haul before
delivering a construction resource or collecting a bill ingredient. It uses the actual material
and permitted portions selected for that work. The destination must be strictly closer to the
work site by a supported actual route than the resource's current location, and the legal haul
must include at least one extra unit beyond the immediate selected delivery portion. Already-held
ingredients are not unloaded merely to create an opportunity. If no extra unit fits, continue the
ordinary supply job. A room boundary alone neither authorizes nor prohibits the detour.
Use the same total and additional-leg limits, with the original supply trip measured from the
pawn through the selected resource to the work site. All planned supply cargo goes to the selected
physical storage destination; linked stores elsewhere are separate destinations. Higher-priority
supply trips may include ordinary spare goods accepted there, within S021's finite search and
this trip's limits. Material locks, ingredient radius, shared bill iteration, quantities, quotas,
construction costs and unfinished-work association remain unchanged. S024's storage-detour option
and S125's bill-supply option are the same effective preference and decision, not two trips.

#### Scenario: Extra construction supplies fit
- **WHEN** a builder needs five units immediately and can legally relocate ten selected resource units to storage closer to that work
- **THEN** it can make one bounded storage trip before resuming construction supply, without consuming or completing the construction through that relocation.

#### Scenario: No useful surplus
- **WHEN** capacity or destination space permits only the immediate selected ingredient quantity
- **THEN** the pawn uses ordinary ingredient delivery instead of inserting a storage trip for no surplus.

#### Scenario: Bill material and radius remain authoritative
- **WHEN** a started linked bill selected steel within its committed ingredient radius and a nearby alternative material or out-of-radius stack would be easier to haul
- **THEN** the supply detour retains that iteration's actual permitted choices and does not substitute those alternatives.

#### Scenario: Both source behaviors notice the same bill
- **WHEN** the owned Common Sense and While You're Up behaviors identify a supply opportunity
- **THEN** they make one shared decision under the single bill-supply preference and schedule at most one detour for that attempt.

### Requirement: Equal-priority relocation is a narrow work-bound exception
With equal-priority supply relocation enabled, initially on, the mod SHALL permit selected supplies
to move to a different physical store of the same priority when all supply-benefit rules pass.
It never permits lower-priority storage or a move within the same physical store. Only materials
actually eligible for the pending construction or bill join an equal-priority relocation; matching
an item type alone does not override quality, material, ingredient or reservation restrictions.
A destination building must be allowed by both general and supply building filters for this
exception; stockpile destinations follow the supply stockpile permission. Stored goods outside
this explicit work-bound request remain ineligible for equal-priority ordinary batching. Visible
and effective storage priorities do not change to manufacture eligibility. Once that attempt
has had its detour, it cannot start another relocation of the same supplies before the parent work.

#### Scenario: Same-priority workshop shelf
- **WHEN** selected supplies have a distinct equal-priority shelf nearer the pending work, both applicable filters allow it, and surplus and travel limits pass
- **THEN** they can be relocated without changing either store's displayed or actual priority.

#### Scenario: Unrelated goods beside the supplies
- **WHEN** an equal-priority supply trip passes clothing that the pending job cannot use
- **THEN** that clothing is left in place even if the destination accepts it.

#### Scenario: Disable the exception
- **WHEN** equal-priority relocation is disabled
- **THEN** a new supply trip requires strictly better storage, and ordinary ingredient delivery remains available.

### Requirement: Destination preferences do not grant storage capabilities
The mod SHALL provide separate general-opportunity and supply destination preferences, each with
a stockpile switch and a searchable storage-building list grouped by source mod. Both stockpile
switches and automatic building selection initially are on. Automatic selection includes ordinary
Core storage buildings and supported optional storage; general opportunities initially exclude
optional stores with additional handling delays, while supply relocation can use them when that
handling and capacity are supported. Unknown or unsupported storage is visibly unavailable.
Manual selection can choose supported entries, including supported slow storage, but cannot waive
its normal handling, capacity, filters, reservations, S049 refill rules or linked physical limits.
Automatic mode shows its effective choices read-only. Switching to manual first seeds those choices;
switching back retains the dormant manual choices. A later return to manual restores them. New
building types follow automatic policy in automatic mode and start unselected in an existing manual
list. Removed types remain unavailable saved choices, never substitutes for a similarly named type.

#### Scenario: Automatic selection versus manual selection
- **WHEN** the player switches an automatic general list to manual and excludes one supported shelf type
- **THEN** only that chosen scope changes, and returning through automatic to manual restores the exclusion.

#### Scenario: New and missing storage content
- **WHEN** installed content changes between sessions
- **THEN** automatic lists reflect currently supported content, existing manual lists do not silently opt into new types, and missing choices remain recognizable and inactive.

#### Scenario: Physical storage restriction
- **WHEN** an enabled building cannot accept meat or its refill ceiling has been reached
- **THEN** the preference does not authorize a meat delivery or further automatic refill.

### Requirement: Detours use the shared physical cargo rules
The mod SHALL use S021's one real mass and volume allowance, exact personal/cargo/urgent quantities,
destination commitments, physical pickup and grouped delivery. It adds no capacity multiplier,
separate hand allowance, cargo store or independent item cache. General and supply opportunities
do not gain urgent priority or widen a cancelled/partial urgent order. If inventory batching is
unavailable, a supported ordinary single-load detour can run only with its own normal limits and
after existing owned cargo is safely handled. It still obeys every S125 purpose, surplus, travel
and destination restriction. If those cannot be preserved, skip the detour and continue the parent.

#### Scenario: Personal medicine shares a cargo type
- **WHEN** two personal medicine units and five hauled units share a physical stack during a detour
- **THEN** delivery can take only the five cargo units; a stale remembered item cannot select the personal units instead.

#### Scenario: Batching unavailable
- **WHEN** the supported single-load route can perform the same useful detour and no stranded owned cargo blocks it
- **THEN** the pawn may use that route without gaining extra capacity or silently relaxing the detour limits.

### Requirement: One pending work attempt receives at most one detour
The mod SHALL preserve the identity of the pending work through opportunity selection, collection,
delivery and return. After an accepted detour, continue that still-valid work or let ordinary
scheduling reconsider it if it is no longer valid. Resuming or reselecting the same attempt,
unloading its cargo, saving/loading, or repeated checks cannot insert a second detour before that
work gets its turn. An unchanged failed search also is not immediately retried for the same attempt.
Meaningful changes to items, destinations, access or the work can allow a new search, but do not
reset an already consumed detour allowance. Completion/cancellation of that work ends the attempt.
Required cleanup, explicit orders and emergencies retain priority over recovery and resumption.

#### Scenario: Work resumes after unloading
- **WHEN** a pawn completes its opportunity and the original bill remains valid
- **THEN** it proceeds to that bill without another opportunity inserted by either S125 or S024.

#### Scenario: Save halfway through a detour
- **WHEN** a save is loaded while the pawn holds detour cargo
- **THEN** the surviving cargo and parent-attempt state are reconciled, destinations are rechecked, and the same work does not receive a fresh detour allowance.

### Requirement: Changed destinations do not cause uncontrolled recovery trips
The mod SHALL stop adding cargo when its purpose, capacity, destination or permission becomes invalid.
A replacement delivery must pass current storage rules and the remaining original whole-trip
budget, including travel already performed. A different cell in the same store is not exempt.
If no valid constrained delivery remains, S021's bounded safe placement/retention and 600-simulation-
tick unchanged-failure retry rule govern the actual cargo. Recovery is not successful storage
delivery and cannot become another unrestricted pickup trip. A cancelled parent is never recreated
solely to use its old route. Player drafting and valid orders take effect without cargo loss.

#### Scenario: First destination fills during travel
- **WHEN** an alternative store would exceed the trip's remaining budget
- **THEN** no new goods are collected for it and existing goods use safe cargo recovery rather than an unrestricted replacement haul.

#### Scenario: Same store but far-away free cell
- **WHEN** the only remaining space in a large stockpile makes the actual route exceed the remaining budget
- **THEN** sharing the stockpile identity does not waive that limit.

#### Scenario: Draft after pickup
- **WHEN** the player drafts the loaded pawn
- **THEN** it obeys the order and uses the selected S021 cargo-retention/drop policy without losing or substituting physical quantities.

### Requirement: Configuration uses clear common scope and effect timing
The mod SHALL place configuration under **Hauling > Opportunities**, ordered as initial enable default,
search/travel, construction and bill supplies, destination lists, and advanced limits. Every actual
configuration row uses the common Global default, Current save and Use global controls except the
initial enable default. That row is global-only through the common renderer, labeled as the initial
state for games without an operational choice. A per-save seed would be meaningless because the
game's bottom-right operational choice is persisted independently; this is a documented scope
exception, not a second per-save configuration value. Each
destination list, including automatic/manual state and dormant choices, inherits as one collection.
Defaults are: enabled on; Balanced; construction supplies on; bill supplies on; equal-priority on;
both stockpile switches and automatic lists on; start-to-item preference30 cells and50% of the
original trip; store-to-work preference50 cells and60%; total170%; additional legs100%; Nearby
search budgets25 for each end. Advanced controls start collapsed, as UI state only.
Distances accept0–999 cells in tenths; ratios accept0–1000% in whole percentages; the two Nearby
budgets accept integers1–1000. Zero preferences remain valid finite restrictions, never unlimited.
Invalid/nonfinite edits do not replace a valid value. Defaults and ranges are proposed source-derived
choices except the explicit bounded budget range. Existing source settings are not silently imported.
Valid changes other than the initial enable default apply at the next relevant planning/pickup/delivery check; existing cargo remains
recoverable. New saves inherit live global values. Main-menu save controls and mutation actions are
disabled; globals remain editable. Use global retains/restores dormant overrides as in the common model.

#### Scenario: Scoped filter edit
- **WHEN** a save overrides its destination collection and the global list changes
- **THEN** that save retains its override, while inheriting saves use the new global choices.

#### Scenario: Main menu and invalid value
- **WHEN** settings are opened without a save or an invalid number is entered
- **THEN** save mutation is disabled at the main menu and an invalid edit never silently creates unlimited search or travel.

#### Scenario: Tighten limits with cargo held
- **WHEN** the effective travel limit is reduced during a trip
- **THEN** subsequent decisions use the new limit, further invalid pickups stop, and real cargo remains recoverable.

### Requirement: Operational controls and explanations use the appropriate surfaces
The mod SHALL provide a small bottom-right **Hauling opportunities** on/off control, initially
seeded from the current global initial enable default for a new game or an existing save that has
no operational value, and then saved as current-play state. Later changes to that initial default
do not overwrite an existing game's saved operational choice.
Changing it affects only that game, not global/save configuration defaults. Off prevents new S125
general and supply detours, including the composed S024 bill-storage detour; required ingredient
collection and ordinary hauling continue. Existing cargo completes a still-permitted delivery or
recovers safely. Native vanilla opportunities are not globally disabled by this owned control.
Pawn job reports identify a detour as on the way to work or moving supplies closer to work and
name the current destination where available. Stale/deleted targets produce a safe recovery status.
All controls, reasons and reports use the seven baseline languages. No new bottom tab is added.

#### Scenario: Turn off during play
- **WHEN** the player disables Hauling opportunities while a pawn has already collected cargo
- **THEN** no new S125 detour or extra pickup starts, existing cargo is safely delivered/recovered, and the toggle survives save/load without changing global preferences.

#### Scenario: Inspect the worker
- **WHEN** a pawn is moving extra supplies before a bill
- **THEN** its report explains that purpose rather than claiming the bill is already being performed or completed.

#### Scenario: First use in an existing save
- **WHEN** an existing save without a Hauling opportunities value is loaded while the global initial default is off
- **THEN** its operational control starts off and retains its own saved choice on later loads, even if the global initial default changes
- **AND** no ineffective per-save initial-default override is offered.

### Requirement: Route diagnostics stay in developer controls
The mod SHALL expose optional route visualization only through the top developer controls, off
initially and hidden/inactive when developer mode is off. Show the original general trip in red,
accepted general detour in green, original supply trip in magenta, supply detour in cyan, and rejected
delivery alternatives in yellow, with a textual legend and reason so color is not the sole cue.
The overlay is temporary diagnostic state, resets on game/session change and does not change job
selection, storage permissions or saved configuration. It is neither a mod-settings preference nor
a bottom-bar gameplay tab/control.

#### Scenario: Disable developer mode
- **WHEN** route visualization is on and developer mode is turned off
- **THEN** the overlay and its control disappear and hauling behavior remains unchanged.

### Requirement: Unsupported integrations leave ordinary work available
The mod SHALL suppress only affected owned detours when the necessary game/provider behavior
cannot be established and explain the unavailable behavior and fallback in bounded startup logs.
Active original While You're Up providers suppress overlapping owned scheduling; unsupported
external Common Sense bill detours suppress owned bill detours; unsupported external batching
suppresses only dependent inventory-detour routes. No foreign setting or patch is edited, no
package identity is impersonated and unrelated compatible patches are allowed. Supported native
single-load routes remain eligible only if they retain all relevant rules. When an affected route
is unavailable, the underlying work remains available and saved cargo is not abandoned. Missing
optional storage withholds that storage's candidates rather than all unrelated opportunities.

#### Scenario: Original opportunity provider installed
- **WHEN** an original While You're Up implementation is active
- **THEN** the owned overlapping scheduling stays inactive with an explanation, without removing the provider or changing its settings.

#### Scenario: Unsupported floor or map route
- **WHEN** a vehicle interior or level transition cannot supply a reliable permitted route and destination
- **THEN** that detour is skipped and the native work path remains available; matching coordinates do not prove proximity.

#### Scenario: Benign patch and changed required behavior
- **WHEN** another patch leaves the required job behavior intact
- **THEN** its mere presence does not disable the feature
- **AND** if required behavior instead is missing or ambiguous, only the affected detour is withheld with a startup reason and ordinary-work fallback.
