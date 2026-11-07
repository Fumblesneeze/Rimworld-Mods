# Bulk transporter loading

Sole owner **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`.
Target **RimWorld 1.7 beta**. S133 replacement specification; no implementation or acceptance yet.
Portal loading is excluded by the user's 2026-11-07 decision. Vehicle Framework loading is deferred.

## ADDED Requirements

### Requirement: BTL01 Native transport requests retain their meaning

The feature SHALL batch ordinary items selected for an admitted pod or shuttle loading request,
using its existing cargo-selection workflow. It MUST preserve selected quantities, distinguished
materials, qualities, condition, ingredients, biocoding and other distinctions retained by that
request. Equivalent portions of the same selected cargo remain interchangeable when the native
request treats them as such; sharing an item type alone is insufficient. Quest cargo described only
by type and amount SHALL retain that broader request without inventing extra restrictions.
Already delivered, currently carried and still outstanding cargo MUST remain distinguishable.
Editing a request MUST retain physical items and reconcile its outstanding amounts.
The transport request SHALL authorize collection of its selected cargo from otherwise valid best
storage into that transport. Ordinary storage-hauling priority restrictions do not reject those
explicitly selected sources or substitute a storage destination; all access, protection, reservation
and capacity restrictions still apply.

#### Scenario: Selected cargo is already in best storage
- **WHEN** the player selects several eligible portions from well-stocked shelves or stockpiles for a pod
- **THEN** a fitting batch collects those selected portions and delivers them to that pod request,
  while unrelated well-stored goods remain untouched

#### Scenario: Different variants of one item
- **WHEN** the player selects five high-quality weapons and leaves lower-quality weapons unselected
- **THEN** loaders collect only eligible selected weapons, leave the others alone and stop after five
  actual deliveries even when several workers participate

#### Scenario: Edit a request during pickup
- **WHEN** the player reduces a request from ten units to six after four were delivered and four picked up
- **THEN** only two of the carried units remain needed, two become recoverable surplus and the display
  shows six requested with four delivered until the next two arrive

#### Scenario: Native request maintenance
- **WHEN** a shuttle refreshes its quest requirements or the player reopens its loading dialog
- **THEN** in-transit cargo remains represented once, actual delivered cargo stays credited and current
  quest requirements govern completion without resetting valid progress or inventing delivery

### Requirement: BTL02 Every batch uses the same real hauling allowance

Loaders SHALL use S021's actual combined mass and carrying-volume limits, worker eligibility,
protected-supply quantities, corpse preference and cargo recovery. Equipment, personal inventory,
temporary cargo and hands count under the same rules. Neither the final hand stack nor carrier
unloading SHALL grant another allowance. New pickups MUST recheck physical capacity, access,
reservations, danger, ownership and the current request. Native boarding and carrying living pawns
SHALL remain their separate native jobs, including their existing restrictions; living pawns MUST
NOT become backpack cargo. Batching SHALL NOT change work priorities, training or manipulation.

#### Scenario: Almost full loader
- **WHEN** a pawn with equipment and personal supplies has room for only part of the selected cargo
- **THEN** the batch takes only that part, keeps supplies protected and cannot add an over-limit final
  hand stack; an oversized candidate does not prevent another eligible fitting portion being used

#### Scenario: Supported and unsupported workers
- **WHEN** a capable worker receives loading work, or a worker cannot use owned batch hauling
- **THEN** the capable worker follows current work and access rules, while the other retains eligible
  native loading actions without a misleading bulk order or a repeated empty waiting job

#### Scenario: Passengers are not stacked cargo
- **WHEN** a request includes walking colonists, an animal or a downed pawn
- **THEN** existing boarding or individual carrying workflows handle them, and item batching neither
  puts them in an inventory nor bypasses their native eligibility

### Requirement: BTL03 Workers coordinate without manufacturing cargo

Several eligible workers SHALL help one request without collecting more than its outstanding
eligible quantities. Claims for planned work MUST NOT count as delivery or prevent another worker
from taking released work. Current and queued jobs, native loaders and owned loaders SHALL share
one accurate view of committed quantities. Requests on other maps or unrelated transports MUST
NOT affect each other. A changed or unavailable source SHALL release its unfulfilled commitment.
Blocked nearby items MUST NOT permanently hide reachable requested items farther away.
Each trip SHALL collect from a finite planned set and then deliver; newly arriving cargo waits for
a later trip rather than extending the current collection indefinitely.

#### Scenario: Competing workers and queued orders
- **WHEN** two workers and a queued loading order compete for the last three units
- **THEN** their combined new pickups cannot exceed three, cancellation releases only the canceled
  commitment and no worker is credited with cargo it has not delivered

#### Scenario: Stale candidate or changed access
- **WHEN** a planned item is destroyed, moved, forbidden or reserved by another job before pickup
- **THEN** the loader rechecks it, skips the invalid pickup and releases that portion without stealing
  the reservation or looping indefinitely; other reachable eligible cargo can still be loaded

#### Scenario: Independent requests
- **WHEN** two maps or two unrelated transport groups have similar cargo requests
- **THEN** progress, reservations and cancellation in one do not remove work or cargo from the other

### Requirement: BTL04 Delivery preserves quantities and destination limits

Only the quantity actually accepted by the intended destination SHALL reduce the request. A stack
merge, partial acceptance or interrupted transfer MUST NOT duplicate, delete or double-credit items.
Loaders SHALL observe each transport's current remaining capacity and acceptance rules, including
other workers' commitments. Group distribution SHALL preserve the player's total selection without
rounding extra units into existence. It SHALL use fitting group members rather than knowingly
overfill one. If a valid distribution cannot be established, it MUST explain the blocked cargo and
preserve existing cargo and the request for revision; it MUST NOT claim that an unproved arrangement
is physically impossible. No optimal packing or shortest-route guarantee is required.
Delivery SHALL include S021's three simulation ticks of handling per unloaded portion plus the
destination's native handling time; source-specific cosmetic delays SHALL NOT add a second charge.

#### Scenario: Uneven distribution
- **WHEN** five identical units are assigned across four transports, or transports have unequal capacity
- **THEN** exactly five units remain selected and fitting destinations are used without excess
  allocation or knowingly exceeding any transport's capacity

#### Scenario: Partial or refused transfer
- **WHEN** a destination accepts two of five carried units, accepts none, or merges them into a stack
- **THEN** only the accepted amount is credited once and every remaining unit stays physically
  recoverable; a full or changed destination cannot silently consume the remainder

#### Scenario: Capacity changes while walking
- **WHEN** another delivery fills the planned transport before this loader arrives
- **THEN** the loader uses another reachable eligible member of the same requested group or retains
  the cargo for recovery, and never treats group-wide free space as permission to overfill a member

### Requirement: BTL05 Contextual orders have a bounded destination and lifetime

An eligible pawn's contextual transport menu SHALL offer a single batch and an until-complete
loading order, with current unavailable reasons. Both SHALL target the chosen request; a single
batch ends after that trip, while until-complete repeats useful batches for that request until
completion, cancellation, interruption or inability to make progress. It MUST NOT wander into
another transport group. The menu action MUST recheck its target before interrupting the current
job or moving cargo. Explicit loading SHALL first use eligible uncommitted cargo already carried
for that request; unrelated cargo SHALL be handled through normal owned recovery. Ordinary hauling
opportunities SHALL use S125's bounded parent-job rules and MUST NOT redirect an explicit order.

#### Scenario: Until complete means this request
- **WHEN** a player orders a pawn to keep loading one pod group until complete
- **THEN** it continues useful batches for that group, stops when its order ends and does not start
  loading another nearby group merely because work exists there

#### Scenario: Menu becomes stale
- **WHEN** loading is canceled or the destination disappears after the menu opens but before clicking
- **THEN** the click reports that the order is no longer available and leaves the pawn's current job,
  personal supplies and physical cargo intact

#### Scenario: Already carrying something useful
- **WHEN** a pawn holds three eligible uncommitted cargo units for the chosen request and unrelated cargo
- **THEN** those three can be delivered before another pickup, personal supplies are not reclassified
  and unrelated cargo is retained or recovered without dumping the entire inventory

### Requirement: BTL06 Carrier unloading respects the carrier and its cargo

Batch unloading SHALL start only where the current native carrier-unloading rules allow it,
including the carrier's unload intent, faction/access, reservation and eligible item portions.
Protected personal supplies SHALL stay protected under the native and owned supply rules.
An unloading session SHALL exclusively coordinate extraction from that carrier while allowing its
ordinary movement unless the native job itself requires otherwise. It MUST NOT compete with the
carrier's active cargo delivery. Extracted portions SHALL fit the worker's shared allowance and
continue into ordinary eligible storage delivery or S021 recovery. Unsupported specialized carriers
SHALL retain their native unloading path. No new command to strip arbitrary pawns is introduced.
Extraction SHALL retain the native ten simulation ticks per extracted portion; later storage
delivery uses S021 handling.

#### Scenario: Returning pack animal
- **WHEN** an eligible pack animal is set to unload and a worker can carry several of its cargo portions
- **THEN** the worker extracts fitting unloadable portions, visibly carries them and delivers them to
  valid storage while another worker cannot extract the same portions simultaneously

#### Scenario: Protection and changing intent
- **WHEN** a carrier contains protected supplies, starts delivering its own cargo or stops unloading
- **THEN** protected portions remain and further incompatible extraction stops; goods already extracted
  remain accounted for and recoverable

#### Scenario: No storage or failed placement
- **WHEN** an extracted portion cannot be stored or safely placed
- **THEN** it remains physically held and tracked for bounded recovery, rather than being erased from
  accounting, dropped into an invalid place or retried continuously

### Requirement: BTL07 Interruption and save changes preserve real cargo

The feature SHALL release obsolete future commitments after drafting, replacement orders, canceled
queues, map changes, death, destroyed targets or disabled batching, and retain physical cargo through
S021 recovery. Save/load SHALL preserve cargo classification and the current request, then reconcile
work with actual items and jobs. Saving MUST NOT deliberately interrupt every loader. Stale claims
MUST NOT hold boarding, quest completion or other workers indefinitely. S021's unchanged-problem
recovery retry interval of 600 simulation ticks applies; an actual relevant change or explicit safe
retry can act sooner. Unloading must continue safely when new batching is disabled.

#### Scenario: Interrupt at the handoff
- **WHEN** a pawn is drafted after moving cargo into its hands but before depositing it
- **THEN** the cargo remains physical and classified, undelivered quantities stay outstanding and the
  freed work can be taken without duplicating or forgetting the interrupted cargo

#### Scenario: Save and reload mid-load
- **WHEN** the player saves during collection or delivery and later reloads
- **THEN** the save action does not replace loading jobs with waits, and reload restores accurate
  request, cargo and commitment state without phantom delivered amounts or abandoned reservations

#### Scenario: Boarding and changed requirements
- **WHEN** selected passengers can board after useful loading finishes, or all remaining work is blocked
- **THEN** real cargo and current native quest/boarding rules determine the outcome; stale owned
  claims do not prevent the native action, and a blocked request has an accurate explanation

### Requirement: BTL08 Controls and presentation use common conventions

The feature SHALL add no bottom main tab. Cargo selection, batch orders and request cancellation
SHALL use existing contextual transport/carrier controls. Current-play batch loading and carrier
unloading switches SHALL use a compact bottom-right hauling control. Configuration SHALL live in
ordered mod settings under Hauling / Transport loading, with global defaults, Current save and Use
global through the common settings behavior. Initial defaults SHALL enable pod/shuttle batching and
carrier batching and enable automatic opening of transport Contents and eligible carrier Gear.
Current switches start from those effective defaults and persist as current-save operational state;
changing a default affects saves without an explicit current switch choice. Reset to default clears
that choice. Changing either inspection preference affects the next selection.
Auto-opening SHALL happen only for a newly selected single compatible subject, use its actual tab,
and never repeatedly override a player's chosen tab. S126 cargo and S132 Gear SHALL show the same
real items, remaining capacity and cargo/supply distinction. Diagnostics belong in the top debug
toolbar, developer mode only. There SHALL be no capacity-bypass option. All added UI requires the
seven baseline languages, readable scaling and unchanged keyboard/native selection access.

#### Scenario: Inheritance and main menu
- **WHEN** settings are opened in a loaded game with Use global checked, or at the main menu
- **THEN** only the save-value editor is disabled by inheritance; the loaded-game Use global checkbox
  stays editable, while the main menu disables every save control and callback and permits global edits

#### Scenario: Select and inspect
- **WHEN** the player selects one compatible transporter or eligible carrier with auto-opening enabled
- **THEN** its existing Contents or actual Gear tab opens once; switching tabs afterward is respected,
  and multiselection or selection changes cannot open a stale subject's tab

#### Scenario: Operational switches and direct composition
- **WHEN** the player switches off new batching while a pawn carries transport cargo
- **THEN** no new batch begins, current cargo is safely delivered or recovered and the shared visible
  cargo and Gear views remain accurate without duplicated inventory, settings or map counts

### Requirement: BTL09 Compatibility failure retains safe native behavior

Every integration SHALL activate only when its documented target conditions hold. Benign changes
and compatible patches SHALL remain admissible. A failed or uncertain enabled integration SHALL
leave its dependent behavior inactive, preserve other providers and log a bounded startup warning
identifying the unavailable behavior and fallback. The source Bulk Load For Transporters active
alongside this mod SHALL withhold the owned overlap with an explanation. No upstream package
identity, assembly, saved job or settings migration is promised. Unsupported transports retain
their existing native/provider controls. Portal loading SHALL be excluded entirely: no owned portal
batch job, menu, automatic loading, cargo redirection or patch. Vehicle Framework loading is not
part of this initial capability. Ordinary game portal behavior is outside this feature's authority.

#### Scenario: Changed target or foreign provider
- **WHEN** a required loading integration has changed incompatibly or the original loading provider is active
- **THEN** the affected owned loading behavior stays unavailable with a startup explanation, native or
  provider loading remains accessible and other providers are not removed or partly overwritten

#### Scenario: No portal or unproved vehicle extension
- **WHEN** a player uses a portal or an unsupported vehicle while owned transport batching is enabled
- **THEN** this feature adds no bulk command or automatic cargo delivery there and leaves the existing
  game/provider workflow in control
