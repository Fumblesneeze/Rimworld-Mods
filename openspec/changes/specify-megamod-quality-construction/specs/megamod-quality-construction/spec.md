## ADDED Requirements

### Requirement: Quality targets belong to individual construction projects
Make Rimworld Great Again SHALL let the player enable quality construction and choose a minimum of Awful, Poor, Normal, Good, Excellent, Masterwork or Legendary for an eligible player-owned building, construction blueprint or frame.
An eligible project constructs a building that ordinarily receives quality and can be rebuilt through
ordinary construction. The choice belongs to that project, not to every building of its kind.
Items, terrain, non-quality buildings and another faction's property are unaffected. A target is a
goal, not a promise to obtain that quality. Setting it does not change an existing quality result.

#### Scenario: Target one project
- **WHEN** the player enables quality construction and selects Excellent on one eligible blueprint
- **THEN** that project visibly shows Excellent as its target and keeps it through delivery and frame creation
- **AND** another blueprint retains its own target and enabled state

#### Scenario: Unsupported selection
- **WHEN** a selection includes an item, non-quality building or another faction's building
- **THEN** quality orders do not change those objects or offer an apparent free quality upgrade

#### Scenario: Target already achieved
- **WHEN** quality construction is enabled on a Good building with a target of Normal
- **THEN** the building remains intact, is shown as meeting its target and needs no replacement

### Requirement: Orders and contextual controls expose the same project choices
The mod SHALL provide filled-rectangle Set quality construction and Remove quality construction orders for eligible visible blueprints and frames, plus contextual target and enable controls for individual or mixed selections.
Set exposes the chosen minimum before designation; its initial choice is the current default target.
Each eligible selected project receives the chosen target independently; an existing high-quality
building does not remove lower choices needed by a co-selected blueprint. Fogged or out-of-map cells
are not designated. Repeating the same order does not duplicate it or spend another retry.
Finished-building improvement explains that ordinary deconstruction, resources and rebuilding are
required. Project status exposes the target, replacement count, pending work and any waiting reason.
The inspect surface retains access to cancellation and restart even when finished-building gizmos
are hidden. This feature adds no bottom tab or unrestricted debug quality editor.

#### Scenario: Rectangle order and removal
- **WHEN** the player drags Set quality construction at Good across eligible blueprints and frames
- **THEN** each visible eligible project in the rectangle is marked Good once
- **AND** Remove quality construction clears their quality orders without canceling ordinary construction

#### Scenario: Mixed quality selection
- **WHEN** a Masterwork building and an unfinished eligible blueprint are selected and the player chooses Normal
- **THEN** the blueprint receives Normal and the Masterwork building is treated as already meeting Normal

#### Scenario: Finished improvement is ordinary work
- **WHEN** the player raises the target above a finished building's actual quality
- **THEN** the control explains the destructive replacement work and its current retry limit
- **AND** the building keeps its actual quality while awaiting that work

#### Scenario: Hidden finished gizmos remain recoverable
- **WHEN** finished-building gizmos are hidden and a building has a pending quality replacement
- **THEN** its inspect controls still show the target and permit canceling the quality order

### Requirement: Defaults and preferences use common global and save settings
The mod SHALL place quality-construction configuration in the Construction / Quality construction section of mod settings, using the common Global default, Use global and Current save controls.
Rows occur in this order: automatically enable new projects (true); default minimum (Awful); allowed
Construction levels below the best builder (0, range 0–20); skill that bypasses that relative limit
(20, range 0–20); exclude colony mechs (false); maximum quality replacement cycles (3, values 1–20
or Unlimited); hide finished-building gizmos (false). Unlimited is explicit, not the initial value.
New or missing save entries inherit current globals. Save overrides remain isolated, and checking
Use global retains a dormant override. Only the save editor is disabled while inheriting in a
loaded game; both save editor and Use global are disabled at the main menu. Global controls remain
usable there. Default enablement and target apply only to new projects; eligibility and presentation
changes apply at the next relevant work decision or display. Retry-limit changes apply to the next
replacement decision but do not revoke an already initiated cycle or restart an exhausted project.
No separate map copy of these numeric preferences is introduced.

#### Scenario: Source-compatible initial defaults
- **WHEN** the player places a new eligible blueprint without changing these defaults
- **THEN** quality construction starts enabled at Awful and ordinary finishing uses the best-builder policy
- **AND** obtaining any native quality satisfies that target without a quality replacement

#### Scenario: New defaults preserve existing orders
- **WHEN** the player changes the default target to Excellent and automatic enablement to false
- **THEN** existing projects retain their individual choices and a newly placed project starts disabled

#### Scenario: Inheritance and save switching
- **WHEN** one save inherits a global retry limit of 3 and another overrides it with 5
- **THEN** changing the global limit to 4 affects the inheriting save only
- **AND** switching saves or reloading preserves their separate inheritance and override choices

#### Scenario: Dormant override and main menu
- **WHEN** the player checks then unchecks Use global for a previously overridden row
- **THEN** its previous override returns; a first override instead starts from the current effective global value
- **AND** neither save-specific control can be changed from the main menu

### Requirement: Ordinary finishing uses the best eligible builder on that map
The mod SHALL restrict ordinary finishing of a quality-marked frame to suitable constructors whose Construction skill is at least the best suitable skill on that map minus the allowed difference, with a minimum threshold of zero.
A constructor at or above the absolute bypass skill passes the relative limit. Suitable candidates
are spawned player-colony constructors on that map, alive, not downed, not prisoners and not in a
mental state, capable of construction and with Construction enabled. Include colony mechs with an
overseer and Construction enabled using their ordinary fixed construction skill, unless excluded.
Drafted, sleeping or busy otherwise-suitable constructors still contribute to the best-skill value;
their availability and reachability remain governed by ordinary work. The display explains when
the policy is waiting for a suitable builder. With no positive best skill, the relative threshold
alone imposes no additional restriction. Mech exclusion and native incapability still apply.
Skills, assignments and presence from one map must not determine another map's eligibility.

#### Scenario: Relative and absolute thresholds
- **WHEN** the best suitable builder has skill 15, the allowed difference is 2 and the bypass skill is 20
- **THEN** suitable skill-13 and skill-15 builders pass while a skill-12 builder does not
- **AND** with a bypass skill of 10 the skill-12 builder passes that relative restriction

#### Scenario: Mech exclusion
- **WHEN** a construction mech has a higher fixed skill than every colonist and mech exclusion is enabled
- **THEN** it neither raises the best-builder threshold nor ordinarily finishes quality-marked frames
- **AND** its ordinary work on unrelated projects is unaffected by this option

#### Scenario: Map and availability changes
- **WHEN** a map's best builder departs or becomes downed before another pawn's next construction work decision
- **THEN** that decision uses the remaining suitable builders on that map
- **AND** another map's more skilled constructor does not keep the local frame blocked

#### Scenario: Busy expert remains the expert
- **WHEN** the only best-skilled suitable constructor is drafted or asleep
- **THEN** the relative threshold does not silently fall to let a less-qualified pawn finish
- **AND** the project can wait or the player can change its assignment or issue a forced order

### Requirement: A named builder is contextual map gameplay state
The mod SHALL allow a named colonist assignment for quality construction through a contextual quality-construction control clearly labeled as applying to the current map.
The initial assignment is Automatic. A suitable named colonist takes precedence over relative and
absolute skill thresholds for ordinary quality finishing on that map. Other colonists do not take
over merely because the assigned colonist is busy, sleeping or drafted. If the assigned colonist is
dead, absent from the map, downed, a prisoner, in a mental state or no longer construction-capable
with Construction enabled, the display explains the inactive assignment and falls back to Automatic.
Keep the assignment for a temporarily unavailable colonist so it resumes when suitable again;
remove a permanently missing or dead assignment with an explanation. This assignment persists with
the map and never appears as an inherited mod setting.

#### Scenario: Explicit map assignment
- **WHEN** the player assigns a suitable skill-8 colonist to quality construction on map A
- **THEN** ordinary quality finishing on A waits for that colonist even if a skill-15 colonist is present
- **AND** map B retains its own assignment or Automatic policy

#### Scenario: Temporarily unusable assignment
- **WHEN** the assigned colonist becomes downed
- **THEN** the project reports Automatic fallback and ordinary eligibility uses the other suitable builders
- **AND** the named assignment becomes effective again when that colonist is suitable

### Requirement: Quality policy does not interfere with unrelated work or pure queries
The mod SHALL apply its builder restrictions only to actual finishing of quality-marked frames, preserving native eligibility, reservations, reachability and provider restrictions.
Material delivery, clearing blockers, deconstruction and unmarked construction remain ordinary work.
A player-forced finish order bypasses this feature's relative, absolute, named-builder and mech
restrictions, but never native physical or other applicable construction restrictions. Such an order
still produces an ordinary quality roll and follows the project's quality target afterward.
Opening a menu, inspecting eligibility or another pawn looking for work must not interrupt a working
pawn. If an ordinary active finisher becomes disallowed, its own next construction-work check stops
that finish before further construction progress, preserving the frame; a forced current job is not
stopped by this feature's policy change. No replacement pawn is promised merely because work stops.

#### Scenario: Delivery and blocker work
- **WHEN** a low-skilled pawn delivers resources or clears a blocker for a marked frame
- **THEN** the quality skill restriction does not reject that work
- **AND** its later ordinary finish request is evaluated separately

#### Scenario: Deliberate forced finishing
- **WHEN** the player successfully orders a normally disallowed but natively capable pawn to prioritize finishing
- **THEN** it can perform that construction and receives an ordinary quality result
- **AND** a result below target can still initiate a quality replacement

#### Scenario: Queries do not steal work
- **WHEN** another pawn searches for work or the player opens a context menu for a marked frame
- **THEN** that query alone does not end the current builder's job or release its reservation

#### Scenario: Active policy change
- **WHEN** an ordinary finisher becomes disallowed after the player changes the map assignment
- **THEN** its next construction-work check stops that finish while retaining the frame and completed work
- **AND** an equivalent forced current job continues subject to ordinary game restrictions

### Requirement: Below-target results use costly ordinary replacement cycles
The mod SHALL retain a completed below-target building until an allowed quality replacement can perform ordinary deconstruction followed by construction at the same location.
The replacement retains building kind, material, rotation, quality target and applicable style or
ideology choice. It consumes normal resources, takes normal work, grants normal construction
experience and rolls quality through ordinary construction; it neither guarantees improvement nor
creates refunds beyond native deconstruction returns. A missing resource, capable worker or valid
placement leaves an explicit waiting state rather than creating a finished building. A quality
replacement never substitutes a different building found at the same location.

#### Scenario: Unsuccessful quality roll
- **WHEN** a marked Excellent-target project completes at Good with an available replacement cycle
- **THEN** the Good building is designated for a quality replacement
- **AND** actual deconstruction and rebuilding are required before another quality result exists

#### Scenario: No materials for replacement
- **WHEN** deconstruction completes but there are insufficient materials for its replacement
- **THEN** the matching construction plan remains awaiting ordinary delivery and work
- **AND** no resources or quality result are granted to complete it

#### Scenario: Position becomes obstructed
- **WHEN** the original building is removed but a new obstruction prevents its matching blueprint
- **THEN** the player sees a recoverable blocked replacement intent
- **AND** retrying after clearing the obstruction creates at most one matching blueprint without another quality cycle charge
- **AND** no other object at that cell is destroyed, retargeted or treated as the result

### Requirement: Retry budgets count initiated quality replacements
The mod SHALL count each initiated quality replacement once, when it first orders deconstruction for that cycle, allowing an initial completion followed by at most three such cycles at the default limit.
Failed native construction, job interruption, resource delivery, saving/loading and blueprint-to-frame
progress do not themselves spend or reset a quality cycle. Canceling a cycle after its initiation
does not refund it. Reaching the target ends the run and clears its count; a subsequently raised
target starts a fresh run. Changing an unmet target, disabling/re-enabling its order or raising the
limit does not silently reset an unfinished or exhausted run. An exhausted run keeps its current
building, shows target and spent limit, and notifies the player once for that exhaustion. Restart is
an explicit contextual action explaining renewed work and material costs; it resets an exhausted
run's budget and reevaluates the current target. Unlimited permits further cycles but still requires
real work, materials and cancellation; no immediate reroll loop is allowed.

#### Scenario: Three cycles after initial completion
- **WHEN** the initial result and three subsequent quality replacements all finish below target at the default limit
- **THEN** the fourth completed building is retained with an exhausted status and one notification
- **AND** no fourth replacement is automatically ordered

#### Scenario: Native failure within a cycle
- **WHEN** ordinary construction fails during the second initiated quality replacement
- **THEN** ordinary failure consequences still occur and the recovered project remains on cycle two
- **AND** repeated native failures do not replenish the quality budget or count as fresh quality replacements

#### Scenario: Explicit restart and unchanged exhaustion
- **WHEN** a player raises an exhausted project's configured limit or toggles its quality order off and on
- **THEN** the existing building remains until the player explicitly restarts the exhausted run
- **AND** Restart begins a new budget using the current limit and target

#### Scenario: Raising a previously achieved target
- **WHEN** a Good building previously met Good and the player raises its target to Excellent
- **THEN** it starts a fresh ordinary replacement run and a completed deconstruction creates its matching plan
- **AND** the old achieved status does not prevent that reconstruction

### Requirement: Cancellation distinguishes quality work from ordinary removal
The mod SHALL own only the replacement work it initiated and cancel that work when the player removes quality construction, cancels its pending deconstruction or lowers its target to an already achieved quality.
Before the building is destroyed, cancellation removes only that quality removal order and prevents
its active removal job from destroying the building. After destruction, removing quality construction
leaves an existing replacement blueprint/frame as ordinary construction; a blocked unplaced
replacement intent is canceled visibly. The player can cancel ordinary construction separately.
A pre-existing manual deconstruction order is never adopted as a quality cycle. Canceling a quality
removal clears its pending intent so a later manual removal does not unexpectedly rebuild it.
Other objects, player orders and reservations are unaffected. Cancellation does not undo already
completed native destruction or consumed materials.

#### Scenario: Cancel before removal completes
- **WHEN** the player disables quality construction while its building is being deconstructed for quality
- **THEN** that quality removal stops before destroying the still-existing building
- **AND** the building remains at its actual quality with no pending automatic reconstruction

#### Scenario: Native cancel followed by manual removal
- **WHEN** the player cancels the pending quality deconstruction using ordinary cancellation, then later manually orders deconstruction
- **THEN** the later order removes the building normally without this feature recreating it

#### Scenario: Existing manual order
- **WHEN** the player enables quality construction on a building already manually designated for deconstruction
- **THEN** the manual removal is not turned into a quality replacement or charged as a quality cycle
- **AND** the quality controls explain that manual removal takes precedence

#### Scenario: Lower goal or cancel after destruction
- **WHEN** the player lowers a still-existing Good building's target from Excellent to Good
- **THEN** its owned pending quality deconstruction is canceled and the target is achieved
- **AND** disabling a project whose building is already gone leaves any existing blueprint as ordinary construction instead of restoring the destroyed building

### Requirement: Replacement preserves supported configuration and survives project lifecycle
The mod SHALL preserve the project's target, enabled state, retry progress, exhaustion and owned pending intent across save/load and native blueprint/frame transitions, without changing its forbidden state or adding duplicate orders.
Before automatically removing a configured building, it must establish that its supported native
configuration can survive the replacement: applicable storage filters/priority/group membership,
bill definitions and order, and owner or assignment choices. Retain those choices on the replacement
when still valid; report an unavailable choice rather than silently substituting another. If a
building's consequential custom configuration cannot be preserved, retain it and explain why
automatic quality replacement is unavailable. Do not erase contents or occupants through a shortcut;
native removal restrictions and handling still apply. Moving/minifying/reinstalling the same building
retains its quality intent and achieved/exhausted state without rerolling quality or spending a cycle;
pending quality removal waits for a supported placed state and never attacks the old cell.
An ordinary native automatic-rebuild plan inherits quality enablement and target only if that native
plan is actually created, starting a new quality run. This is distinct from recovery within an owned
quality cycle, which retains its budget. This feature does not enable native auto-rebuild globally.

#### Scenario: Saved pending and exhausted projects
- **WHEN** a save containing a forbidden marked frame, pending quality removal and exhausted building is loaded
- **THEN** each retains its own state, the frame stays forbidden and no duplicate replacement is ordered
- **AND** an exhausted notification is not replayed merely because the save was loaded

#### Scenario: Configured building replacement
- **WHEN** an eligible supported configured building completes a quality replacement
- **THEN** its valid storage choices, linked group, bills and assignments survive where applicable
- **AND** unrelated buildings' configuration and bill order are unchanged

#### Scenario: Unsupported custom configuration
- **WHEN** a custom quality-bearing building has consequential configuration whose replacement preservation is unsupported
- **THEN** it remains intact with an explanation instead of being automatically destroyed with lost configuration
- **AND** ordinary player construction and removal remain available

#### Scenario: Move the same building
- **WHEN** the player minifies and reinstalls the same targeted building elsewhere
- **THEN** its actual quality and target/run state remain unchanged and no quality action affects its former cell

#### Scenario: Ordinary automatic rebuilding
- **WHEN** native automatic rebuilding actually creates a new plan after unrelated destruction of a targeted building
- **THEN** the plan inherits enablement and target with a fresh quality budget
- **AND** with native automatic rebuilding unavailable or off this feature does not create that plan itself

### Requirement: Compatibility failures leave ordinary construction available
The mod SHALL activate this behavior only where its documented game and optional-provider prerequisites hold, allowing compatible foreign changes and isolating unsupported behavior with a bounded startup warning.
When the original QualityBuilder provider is active, disable megamod's overlapping quality feature
with an explanation, without removing the provider or altering its settings. Optional-provider
absence must not disable ordinary supported quality construction. A changed or unsupported custom
construction path keeps its own restrictions and is not claimed to support automatic quality retries.
Selected Replace Stuff behavior must compose directly with quality intent when specified, without
duplicate replacement jobs or an additional quality roll beyond the single material-replacement
completion specified by S028. Material replacement retains the existing run's remaining budget and
exhaustion; any subsequent quality cycle pays normal costs and consumes an available attempt.
No automatic source
save/settings conversion or compatibility with package-specific retextures is promised.
When loading a save with an owned quality removal pending or in progress, unavailable automation
must suspend that removal before it can destroy the building, including when the original provider
now disables our overlap. Retain the intact building, target and spent-cycle state, identify the
suspended project and explain the cause. This affects only our owned removal; ordinary manual orders
and the original provider's orders remain untouched. Restoring compatibility does not silently resume
destruction. A contextual Resume action continues a suspended initiated cycle without charging it
twice or resetting its budget; an exhausted run still requires Restart.

#### Scenario: Original provider co-installed
- **WHEN** the game starts with QualityBuilder and megamod both enabled
- **THEN** megamod reports that its overlapping feature is inactive and leaves source-provider behavior alone

#### Scenario: Unsupported path and compatible unrelated changes
- **WHEN** a required replacement prerequisite is absent or cannot be established
- **THEN** affected quality automation stays inactive with a startup warning describing the unavailable behavior and ordinary fallback
- **AND** unrelated supported features remain available; a benign foreign patch alone is not grounds for rejection

#### Scenario: Optional provider absent
- **WHEN** VEF or external Replace Stuff is not installed
- **THEN** supported native quality construction remains available without a missing-provider failure

#### Scenario: Saved quality removal with unavailable automation
- **WHEN** a save loads with our pending or active quality deconstruction and startup rejects its retry support or disables it because QualityBuilder is active
- **THEN** that owned removal is suspended before destruction and the intact building, target and spent cycles are preserved with a visible reason
- **AND** unrelated manual and source-provider removal orders remain unchanged

#### Scenario: Resume after restoring support
- **WHEN** support becomes available again for a previously suspended initiated cycle
- **THEN** the building remains intact until the player uses Resume
- **AND** Resume continues that cycle without another cycle charge, while exhausted runs remain subject to explicit Restart
