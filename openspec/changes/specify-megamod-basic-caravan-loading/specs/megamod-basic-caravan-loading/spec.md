# Basic caravan loading

Sole owner **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`; **1.7 beta**.
S135 replacement. Observable specification only; no runtime work accepted.

## ADDED Requirements

### Requirement: Assign caravan assistance and unloading to Basic work

BCL01. The feature SHALL assign ordinary assistance gathering selected items for a forming caravan
and ordinary carrier unloading to existing **Basic** work instead of **Haul**. A capable
worker with Basic enabled can therefore help even with Haul disabled. Basic disabled means these
two automatic helper work entries are unavailable, even if Haul is enabled. The feature SHALL not
change the player's numeric priorities or switches, recruit every colonist unconditionally, force
an interruption or alter the caravan participants' separate native gathering duties.

The existing work ordering SHALL remain: in inspected native Basic work, flicking switches comes
before carrier unloading, then caravan-loading assistance, then prisoner release and opening
containers. Higher-priority work types, schedules, needs, duties, explicit orders and ordinary job
interruptions continue to govern. Optional workers still need the resulting Basic-work eligibility
and physical capacity; the change does not grant every mech or animal a new work permission.

#### Scenario: Basic helper with hauling disabled
- **WHEN** a capable non-participant colonist has Basic enabled and Haul disabled, and native caravan assistance has a valid item and carrier
- **THEN** their next eligible Basic decision can assign gathering and physical delivery for that caravan without changing their work assignments.

#### Scenario: Disable Basic
- **WHEN** Basic is disabled and Haul enabled for an ordinary helper
- **THEN** neither reassigned helper work entry is supplied through Haul; a caravan participant's separate native duties are not disabled by this rule.

#### Scenario: Basic ordering and player command
- **WHEN** flicking, unloading and caravan assistance are simultaneously valid, or the worker has a protected explicit order
- **THEN** ordinary Basic ordering prefers flicking, then unloading, then assistance; the change itself does not cancel the explicit order.

### Requirement: Preserve actual caravan and unload eligibility

BCL02. Reassignment SHALL preserve the selected caravan items and amounts, current gathering stage,
usable reachable carrier, reservations, manipulation and native loading job behavior. A helper
without a valid native carrier or item SHALL not be sent into an endless loading loop. The native
helper carrier check is retained, including its narrower eligibility than a participant's own
gathering job; this feature does not promise assistance for every caravan without a pack carrier.

Unloading SHALL still require a different eligible carrier marked for unloading, in the worker's
faction or hosted by it, not a free colonist, not forbidden or burning and reservable/reachable for
the work. It SHALL not empty an arbitrary colonist inventory or create an unload request. Physical
contents, item distinctions, capacity, travel, costs and cargo remain real; moving a work category
does not teleport goods, launch a caravan, change its route or increase its capacity.

#### Scenario: No usable carrier or no remaining cargo
- **WHEN** a caravan cannot supply an eligible carrier, or its gathering request is complete or cancelled
- **THEN** the helper entry supplies no new loading job and the worker can continue other work.

#### Scenario: Unload only requested eligible carriers
- **WHEN** a returned pack animal is marked for unloading and reachable, while another pawn has ordinary personal inventory
- **THEN** Basic work can unload the eligible animal; the other inventory is not emptied merely by this reassignment.

### Requirement: Compose the assignment with shared hauling behavior

BCL03. Enabled S133 carrier-unloading batches SHALL remain eligible under the reassigned **Basic**
work entry; they SHALL not additionally require the worker's ordinary Haul assignment. The work
category grants only permission for that carrier-unload task. S021/S133 capacity, protected supplies,
actual quantities, reservations, recovery and destination rules still apply, with S126 visible cargo
and S132 presentation. If batching is disabled or unsupported, ordinary carrier unloading remains
available under Basic when the assignment itself is admitted.

Caravan helper gathering SHALL retain its native job path; S135 does not introduce a new batch
gathering algorithm. Ordinary storage hauling, pod/shuttle loading and all other work entries SHALL
retain their assignments. No portal loading, portal menu or cargo redirection is added. Opportunistic
hauling SHALL respect S125's separate eligibility and protected parent order; Basic permission for
this task does not grant general Haul permission or route selected caravan goods to unrelated storage.

#### Scenario: Bulk unloading with Haul disabled
- **WHEN** a capable Basic worker with Haul disabled unloads an eligible carrier while S133 batching is active
- **THEN** the batch can run within ordinary physical capacity and recovery rules; it is not rejected solely because Haul is disabled.

#### Scenario: Batch fallback and untouched transport work
- **WHEN** carrier batching is unavailable but these assignments are valid
- **THEN** native unloading still uses Basic, while pod/shuttle loading and ordinary hauling retain their own work permissions.

### Requirement: Keep native controls and save state

BCL04. The Work tab, ordinary caravan formation controls and native carrier-unloading actions SHALL
remain the entry points. No new bottom tab, operational toggle, settings row, work type or patient-like
configuration SHALL be introduced for these fixed assignments. Pawn work choices remain saved
entity state; loading another save SHALL not copy priorities between pawns or saves. No source
settings migration is necessary or promised. Removing the feature restores the remaining active
providers' work assignments on the next game startup without rewriting saved pawn priorities.

#### Scenario: Existing save and fixed assignments
- **WHEN** a player loads a save with established Basic and Haul priorities
- **THEN** those priorities remain, and the two tasks follow Basic without a new settings or main-tab workflow.

### Requirement: Admit both assignments safely and preserve other work

BCL05. Startup SHALL check both documented target assignments as one owned group. Supported existing
Haul assignments can change to Basic; already-equivalent Basic assignments are successful no-ops.
Unrelated fields, work entries and compatible provider additions SHALL remain intact. Missing,
ambiguous or incompatible required definitions, or an unsupported competing assignment, SHALL
withhold both owned edits and leave the remaining game's assignments unchanged. Log one bounded
warning per skipped enabled patch identifying target, failed requirement, unavailable behavior and
fallback. Never remove another provider or repeatedly warn per job.

Original Basic Caravan Loading can coexist when it supplies only these same equivalent assignments;
it SHALL not cause duplicated jobs. A conflicting work-category provider SHALL not be silently
overwritten to win load order. Later incompatible effective changes must not be reported as successful
owned Basic behavior. No universal compatibility with arbitrary work-category mods is claimed.

#### Scenario: Equivalent source or compatible addition
- **WHEN** the source already assigns both tasks to Basic, or another mod adds unrelated work or fields
- **THEN** equivalent assignments stay single and the unrelated additions remain.

#### Scenario: One incompatible assignment
- **WHEN** one required target is missing or assigned to an unsupported custom work category
- **THEN** neither owned assignment is applied, existing provider state remains and startup warnings explain the fallback.
