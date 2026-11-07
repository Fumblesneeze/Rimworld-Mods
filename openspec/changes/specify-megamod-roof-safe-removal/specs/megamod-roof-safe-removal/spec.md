## ADDED Requirements

### Requirement: Roof preparation covers enabled support-removal work
Make Rimworld Great Again SHALL prepare endangered roofs before enabled workers remove a roof-supporting building or mine a roof-supporting rock on the current map.
The owner is Make Rimworld Great Again, `fumblesneeze.megamod`, `mods/megamod`, targeting 1.7 beta.
Building removal includes deconstruction, uninstalling and clearing a building for construction.
Mining includes ordinary mining and mine-vein work. By default both work families cover colonists
and colony mechs. The Animals option extends this to eligible player animals; Other workers extends
it to eligible non-colonist humanlike workers and non-colony mechs. Foreign animals require both
options. Wild humans follow Other workers, not Animals. These options do not grant a job, work
capability, training, faction permission or access that the worker otherwise lacks.
Non-supporting objects, ordinary attacks, fire and other uncontrolled destruction are outside this
feature. It does not promise safety from every possible roof collapse.

#### Scenario: Remove different kinds of support
- **WHEN** an eligible colonist deconstructs a roof-holding wall, uninstalls a roof-holding building or mines a supporting rock
- **THEN** the enabled removal path checks and prepares endangered roof before the support disappears
- **AND** dismantling an unrelated object that holds no roof adds no roof job

#### Scenario: Independent worker options
- **WHEN** Animals is enabled, Other workers is disabled and a player animal can ordinarily perform the removal job
- **THEN** roof preparation applies to that animal without enabling foreign workers
- **AND** it receives no new construction or mining capability from the option

#### Scenario: Foreign worker and wild human
- **WHEN** Other workers is enabled and Animals is disabled
- **THEN** an otherwise capable wild human or foreign humanlike worker receives protection, but a foreign animal does not

#### Scenario: Uncontrolled destruction
- **WHEN** combat destroys a supporting wall outside a covered removal job
- **THEN** ordinary collapse consequences remain; this feature does not prevent or reverse the damage

### Requirement: Preparation uses current connected support and actual removal plans
The mod SHALL identify roof endangered by the actual removal using the current map's roof connections, complete support footprints and applicable support rules.
The inspected ordinary game uses a 6.9-cell horizontal support distance and cardinal roof
connections. Do not treat diagonal proximity or a building's center alone as proof of support.
A remaining support already scheduled for covered mining, deconstruction, uninstalling or an owned
removal cycle is not promised as permanent support. Connected preparation for concurrent orders
must not let workers rely on each other's departing support. Do not remove roof that remains safe
merely because it is near the target. Roof-removal consequences can extend through a connected
roof; a fixed search boundary must not silently certify an unexamined dangerous continuation.
Recheck before removing roof and immediately before the support disappears, including mining's
final strike. New roofs, changed orders, other workers and changed provider geometry invalidate an
earlier safe result. Where an enabled safeguard cannot establish safety, the action waits with an
explanation; the explicit mountain-risk exception below is the only selected bypass.

#### Scenario: Remaining support preserves a roof
- **WHEN** removing one wall leaves the affected roof safely connected to another unscheduled support
- **THEN** that roof remains and the wall can be removed without unnecessary roof preparation

#### Scenario: Mixed simultaneous removals
- **WHEN** two workers mine one support and deconstruct another supporting the same roof
- **THEN** their preparation considers both removals and does not let either complete on a promise that the other support will remain

#### Scenario: Wide or rotated support
- **WHEN** a multi-cell support keeps a roof safe through an occupied edge away from its center
- **THEN** the complete footprint participates in the decision and rotating it changes the evaluated cells accordingly

#### Scenario: Changed roof at the final strike
- **WHEN** another action creates an endangered roof after mining preparation but before the rock's final strike
- **THEN** automatic mining pauses for fresh preparation before destroying the rock, preserving its remaining physical damage

#### Scenario: Connected continuation and unknown safety
- **WHEN** clearing a local roof would expose another endangered part of its connected roof or the support decision is unavailable
- **THEN** preparation includes that danger or visibly waits; a search cutoff is not reported as safe completion

### Requirement: Roof preparation is physical work with ordinary worker restrictions
The mod SHALL arrange real roof-removal work before the original removal continues, retaining ordinary movement, work, reservations, permissions and completion effects.
Prefer endangered cells farther from the removed support first where that order is safe. Each
step must remain valid when acted on. A capable original worker performs the roof work; otherwise
eligible roof workers can perform the required preparation while the original removal waits.
Unreachable, forbidden, outside-permitted-area, reserved or otherwise unusable required cells do
not disappear from the safety requirement. If no worker can complete them, show the waiting reason
and retain the target. Do not teleport workers or remove roof merely by scheduling preparation.
The inspected ordinary roof job uses 65 work units, reduced by Construction Speed at 1.7 units per
game tick at speed 1; ordinary admitted modifiers remain applicable. The same ordinary roof work
is the provisional cost of optional mountain-roof preparation. No new resources or roof salvage
are granted. Ordinary removal of unsupported adjoining thin roof as a consequence of real roof
work remains possible; this is not permission to collapse an unapproved mountain roof onto workers.

#### Scenario: Prepare then deconstruct
- **WHEN** a constructor starts removing the only support for a removable roof
- **THEN** the pawn walks to permitted roof-work positions and removes the required roof through real work before deconstructing the support
- **AND** ordinary deconstruction work and material returns occur once

#### Scenario: Miner cannot do construction
- **WHEN** a capable miner cannot perform roof work but a qualified constructor can
- **THEN** the miner's support removal waits for that constructor's actual preparation rather than gaining roof-working capability

#### Scenario: Reservation or inaccessible cell
- **WHEN** a required roof cell is reserved or no permitted worker can reach it
- **THEN** removal waits with a reason and the still-required cell is not treated as cleared
- **AND** resolving the obstruction permits fresh evaluation without a duplicate preparation queue

#### Scenario: Normal roof work cost
- **WHEN** a worker with effective Construction Speed 1 performs an otherwise unmodified roof-removal step
- **THEN** the step requires the ordinary 65 work units at 1.7 units per game tick, with ordinary tick rounding
- **AND** enabling mountain preparation does not instantly erase the roof or create salvage

### Requirement: Mountain policy distinguishes cancellation from deliberate risk
The mod SHALL cancel an automatic covered removal that endangers an unremovable mountain roof when Cancel orders blocked by mountain roof is enabled, initially true.
Notify the player once for that stopped attempt, identifying the worker, target and mountain-roof
reason when the work concerns the colony. Remove only the relevant local removal designation;
mining and mine-vein orders are both recognized. An owned replacement cycle instead retains a
visibly blocked target and must not recreate the canceled attempt automatically. Ordinary blueprint
or frame construction can request blocker removal without a separate removal designation. In that
case retain the construction project, its resources and the exact blocking target, visibly hold
that clearing action across save/load, and do not recreate it or repeat the notification while the
same mountain obstruction remains. Changed roof/support or permission conditions permit a fresh
check and ordinary continuation only if the obstruction is resolved. An explicit contextual Retry
checks the current conditions; forcing accepts only the stated mountain risk. Canceling the parent
releases its hold without deleting the blocker or unrelated orders. S008's accepted mining plans
create ordinary designations; canceling a hazardous cell does not invent a persistent parent plan,
change its other cells or automatically reapply the canceled designation.
No completed work, stored supplies or unrelated orders are erased. An affected foreign worker stops
its own removal attempt without deleting the player's designations.

A successful player-forced removal, or disabling this cancellation option, explicitly permits
proceeding despite mountain roof this feature cannot remove. Both retain preparation for required
removable roof and all ordinary work/access restrictions. Explain this risk in the setting and
applicable order help; no extra confirmation dialog is required. This exception does not grant
mountain-removal permission, bypass research or suppress ordinary collapse consequences.

#### Scenario: Default cancellation
- **WHEN** automatic mining would endanger mountain roof while mountain removal is unavailable and cancellation is enabled
- **THEN** the local mining attempt is canceled with a bounded explanation, the support remains and unrelated mining orders remain

#### Scenario: Uninstall and owned cycle cancellation
- **WHEN** automatic uninstalling or an owned quality-removal cycle is blocked by unremovable mountain roof
- **THEN** the relevant uninstall order is canceled or the owned cycle is held visibly blocked, respectively
- **AND** neither silently repeats; the quality cycle retains its spent budget and the building remains

#### Scenario: Explicitly accept mountain risk
- **WHEN** the player successfully forces the removal, or disables mountain cancellation before an automatic attempt
- **THEN** required removable roof is prepared first and the support can then be removed despite the remaining mountain roof
- **AND** the mountain roof is not magically removed and ordinary collapse consequences remain

#### Scenario: Construction clears a blocker without a removal order
- **WHEN** a blueprint or frame requests mining or deconstruction of a mountain-supporting blocker without a separate removal designation
- **THEN** the parent project and resources remain visibly blocked, including after reload, without repeating that clearing attempt or notification while the obstruction is unchanged
- **AND** a resolved obstruction permits fresh ordinary evaluation, Retry rechecks it, and canceling the parent releases only its own hold without destroying the blocker

#### Scenario: Enabled foreign worker
- **WHEN** an enabled foreign worker reaches a mountain-blocked removal with cancellation enabled
- **THEN** that attempt stops without changing player designations or repeatedly issuing the same notification while conditions are unchanged

### Requirement: Optional mountain removal has explicit permission and research
The mod SHALL permit mountain-roof preparation only when Remove mountain roof during preparation is enabled and its selected research requirement is satisfied.
The option starts false consistently for new settings and missing entries. The research choice
starts None, meaning no research requirement after the player enables the option; a selected project
must be completed in the current save. Selecting None is explicit and distinct from a saved project
that is missing or unavailable. Retain a missing selection visibly, block its mountain permission
and explain it until the project returns or the player changes the choice. Do not substitute another
project or silently remove the requirement. Installing a roof provider does not enable the option
or replace the choice. Automatic matching of upstream research names is not selected in this draft.
Research completion and changed effective settings affect the next relevant decision, including
pending preparation. Provider restrictions still apply where that provider governs the roof.
This feature adds no manual mountain-roof drawing tool; any such external tool remains independent.

#### Scenario: Explicit research-free mountain work
- **WHEN** the player enables mountain preparation with None selected and ordinary roof work is permitted
- **THEN** covered removal can prepare the mountain roof through real roof work before removing its support
- **AND** the option adds no separate mountain-roof designation tool

#### Scenario: Unfinished and finished research
- **WHEN** a project is selected but unfinished
- **THEN** mountain preparation remains unavailable and the cancellation/risk policy applies
- **AND** finishing that project permits the next eligible preparation without rewriting the global setting

#### Scenario: Missing project is not None
- **WHEN** a saved selected project is absent after a content change
- **THEN** the choice remains visibly unavailable and does not unlock mountain preparation
- **AND** selecting None or restoring and completing the original project can resolve that restriction

#### Scenario: Provider installation and setting change
- **WHEN** a roof provider is installed or the player disables mountain preparation during a pending job
- **THEN** installation does not turn the option on, and further mountain work uses the current explicit permission before changing the roof

### Requirement: Temporary roof orders preserve player intent and map ownership
The mod SHALL keep preparation's temporary Remove roof and suppressed Build roof orders tied to their actual map and outstanding removal work.
Existing player roof orders remain player-owned. Overlapping preparations share required cells
without duplicate work or premature restoration. Keep temporary preparation active until its last
related removal completes, is canceled or is deliberately abandoned. Restore only the unchanged
player intent that preparation temporarily displaced. A later player edit wins, including erasing
an apparently unchanged temporary Remove roof mark; it must not be silently redrawn by that same
attempt. If the edit prevents safe completion, wait with a reason until the player changes the
conditions or explicitly retries. Restoring Build roof is an order, never an instant rebuilt roof.
Do not rebuild between two related support removals. Work already performed remains physical.

#### Scenario: Restore the displaced build order
- **WHEN** preparation temporarily suppresses an existing Build roof area and the last related removal finishes
- **THEN** its unchanged original area choice returns, while the removed roof stays absent until ordinary building work constructs a roof

#### Scenario: Player edits during preparation
- **WHEN** the player erases a temporary Remove roof mark or changes the roof-area choice
- **THEN** that edit survives cleanup and the same attempt does not recreate the erased order
- **AND** support removal waits if the edit leaves preparation unsafe

#### Scenario: Shared cells and distinct maps
- **WHEN** two removals require a cell on map A and another uses the same coordinates on map B
- **THEN** completing one removal on A neither clears the other one's required area nor changes map B

#### Scenario: Existing remove area and cancellation
- **WHEN** the player cancels preparation that overlapped a pre-existing Remove roof area
- **THEN** the original Remove roof choice remains, only owned temporary intent is released, and removed roof is not recreated

### Requirement: Continuation preserves work and composes owned features directly
The mod SHALL retain one original removal intent through preparation, interruption and save/load, preserving its target, relevant choices, earned work and physical results.
Drafting, target cancellation/destruction, death, map departure or loss of capability releases the
worker and its reservations without forcing it back into an obsolete order. Surviving authorized
work can be reconsidered through ordinary work selection; no duplicate queued job, material return,
mining yield, experience, removal completion or quality attempt is awarded by waiting or retrying.
Unrelated player-queued orders retain their choices and order. Reload restores only unresolved
authorized preparation on its original map, with current safety and access rechecks.

S008 mining tools retain their cell/material selection and ordinary designation behavior. S013 quality cycles
retain their budgets, costs and builder rules; a roof wait does not spend another cycle. S028 material
replacement, upgrades and install-over retain their existing physical transaction and contents.
If an owned replacement keeps continuous valid roof support, no roof preparation is required solely
because the building identity changes. If support is actually lost, its removal stage uses this
same roof decision before the old building or contents are committed away. S060's construction
enclosure check remains a separate applicable condition on covered construction completion.

#### Scenario: Save during roof work
- **WHEN** the player saves and reloads during preparation
- **THEN** completed roof removal remains physical, unresolved intent resumes at most once after rechecking, and no original removal or unrelated queued order is duplicated

#### Scenario: Drafting or changed target
- **WHEN** a preparing pawn is drafted or the target order is canceled
- **THEN** its owned continuation stops and reservations are released without teleporting it or undoing physical work
- **AND** another eligible worker can only resume surviving authorized work through ordinary selection

#### Scenario: Final-boundary wait retains work
- **WHEN** a newly discovered roof risk blocks an otherwise finished removal
- **THEN** earned removal progress remains and waiting grants no further work experience, yield or repeated completion effects

#### Scenario: Quality replacement and planned mining
- **WHEN** preparation delays an owned quality cycle or a selected planned mining cell
- **THEN** the quality budget is not replenished or charged again and the mining plan does not change its selected material or silently recreate a canceled hazardous attempt

#### Scenario: Continuous and lost support during replacement
- **WHEN** an owned replacement continuously preserves valid roof support
- **THEN** it retains the safe roof and its ordinary supplies, contents and quality transaction
- **AND** a replacement that actually loses support prepares endangered roof before committing that loss, including install-over

### Requirement: Configuration uses the common inherited settings interface
The mod SHALL expose configuration under Construction and mining / Roof preparation in mod settings through the common global-default and per-save controls.
Rows in order: building-removal preparation (true), mining preparation (true), Cancel orders blocked
by mountain roof (true), Other workers (false), Animals (false), Remove mountain roof during
preparation (false), Required mountain-removal research (None). Research offers None and a searchable,
alphabetically labeled list of available projects, with the current selection visible even when
filtered or unavailable. Clearly explain worker scope, cancellation/forced risk, research and cost.
Changes apply at the next relevant decision; disabling a work family releases its owned temporary
work without automatically executing a previously suspended hazardous continuation.

Every row has Global default, Use global and Current save. New/missing save entries inherit the
current global value. Inheriting disables the save editor; the main menu disables both save editor
and Use global, including callbacks, while global editing remains available. A dormant override is
retained when inheritance is enabled and restored on return; the first override starts from the
effective global value. Global changes affect inheriting saves only. Missing project choices remain
stored through save switching. Roof areas, targets and pending jobs are gameplay state, not settings.
Contextual inspection explains waiting/recovery without a new bottom tab, debug override or separate
operational toggle. Existing native orders remain the player action surface.

#### Scenario: Default and inherited values
- **WHEN** a new save uses the built-in defaults
- **THEN** colonist/mech building removal and mining are prepared, mountain-blocked automatic attempts are canceled, and mountain removal is off
- **AND** changing a global row affects that save until it chooses an override

#### Scenario: Override and main-menu controls
- **WHEN** one save overrides a row and another inherits it
- **THEN** global editing affects only the inheriting save, dormant overrides survive switching/reload, and the main menu cannot mutate either save-specific control

#### Scenario: Research search and unavailable choice
- **WHEN** the player filters research choices or a saved project's provider is absent
- **THEN** the current choice stays identifiable, None remains an explicit choice, and missing content is not displayed as a satisfied requirement

#### Scenario: Disable a work family while it waits
- **WHEN** the player disables building-removal preparation while an owned continuation is suspended
- **THEN** its temporary intent is released with player edits preserved and the old hazardous continuation remains stopped until an explicit new order or resume

### Requirement: Compatibility failure leaves a clear bounded fallback
The mod SHALL enable each integration only when its documented required behavior remains available, allowing unrelated or compatible changes by other mods.
An unsupported required game behavior skips the affected enabled patch group and logs a bounded
startup warning naming the skipped patch, target requirement, reason, unavailable behavior and
fallback. Ordinary new native work remains available without a roof-safety promise from this feature;
saved owned suspended removals stay stopped and recoverable rather than silently executing unsafe
continuations. Activating either original provider disables overlapping new owned roof automation
with an explanation and does not alter foreign patches or settings.

Supported external level/roof/worker content must preserve its real permissions and geometry.
Adjacent coordinates on different logical levels do not share roofs, support or temporary orders.
If required optional support is unavailable, do not run an unqualified flat-map substitute on those
affected maps. The examined source-specific level bridge is not a promised drop-in interface;
owned support requires its own admission and player verification. No automatic upstream settings
or pending-job migration, general multiplayer guarantee or safe mid-save removal claim is made.

#### Scenario: Compatible foreign patch
- **WHEN** another mod changes unrelated mining or construction behavior without removing required roof/removal semantics
- **THEN** this feature remains eligible and preserves the applicable ordinary changes

#### Scenario: Unsupported required behavior
- **WHEN** startup cannot establish a required removal or roof-work behavior
- **THEN** the affected enabled patch group is skipped with a bounded warning and ordinary new native work remains available
- **AND** previously suspended owned removals remain visibly stopped and recoverable

#### Scenario: Source co-installation
- **WHEN** either original Smarter Deconstruction provider is active
- **THEN** overlapping new owned automation stays off with an explanation, without changing that provider or silently replaying saved owned work through it

#### Scenario: Different logical levels
- **WHEN** an admitted level provider places roofed regions on distinct logical levels within one map
- **THEN** preparation uses only the target's actual level and never clears another level's roof or orders
- **AND** without admitted support the affected owned feature is unavailable with a bounded explanation
