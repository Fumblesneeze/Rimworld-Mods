## ADDED Requirements

### Requirement: A workbench group owns one whole bill list
Make Rimworld Great Again SHALL let the player link compatible player-owned production workstations on the same map into one named bill-list group.
Every member displays the same ordered bills and shared editable choices. There are no additional
member-only bills while linked. Different workstation kinds can join when they support the complete
chosen list and its required behavior. Ordinary production, unfinished-product crafting, autonomous
processing and mech production are within scope on compatible stations; pawn medical orders are not
workbench lists. Physical placement, power, fuel, workers and work in progress remain local.
New workbenches start unlinked; proximity, identical names or identical recipes never links them
automatically. Group identity is independent of its name and of any one member.

#### Scenario: Two benches share the entire list
- **WHEN** the player links two compatible workbenches using a list containing meals then kibble
- **THEN** both show meals then kibble with the same bill choices
- **AND** neither retains a hidden independent third bill

#### Scenario: Compatible different bench kinds
- **WHEN** two different workstation kinds support every bill and its required behavior in the chosen list
- **THEN** their different kinds alone do not prevent linking
- **AND** each still needs its own ordinary power, fuel, interaction space and suitable worker

#### Scenario: Identity and map boundary
- **WHEN** two groups receive the same name or a matching new bench is built nearby
- **THEN** they remain separate and the new bench remains unlinked
- **AND** linking a bench from another map is unavailable with a clear explanation

### Requirement: Linking uses contextual controls and an explicit authoritative list
The mod SHALL expose Create group, Join or merge, Rename, Select members, Leave and Dissolve through contextual bill-list or selected-workbench controls, with group name, membership and shared scope visible.
These are saved gameplay choices, not global/save mod settings, and add no bottom tab. Creating,
joining or merging explicitly selects the source bench or group whose whole list will govern the
result. Before replacing a destination list, show the affected benches, source list, replaced bills
and shared finite counts. Never merge by recipe name or silently concatenate two lists. If the
destination already belongs to another group, merge that entire group or cancel; do not silently
detach only the selected member. Canceling the preview changes nothing.

#### Scenario: Join with different existing bills
- **WHEN** a player chooses a meals group as source and an unlinked bench with a kibble bill as destination
- **THEN** the preview explains that the destination's kibble list will be replaced by the meals list
- **AND** confirming adopts that whole list; canceling preserves both original lists and membership

#### Scenario: Merge two groups
- **WHEN** the player chooses group A as source and a member of group B as destination
- **THEN** the preview includes every affected member of B and the discarded destination list
- **AND** confirmation yields one group using A's list and counts, rather than duplicated or combined quotas

#### Scenario: Contextual scope is visible
- **WHEN** the player opens a member's Bills view or details for a linked bill
- **THEN** the view identifies its group and that edits apply to all members
- **AND** Select members reveals those benches without changing their bills

### Requirement: Membership replacement is atomic and preserves existing work
The mod SHALL validate the complete intended group and source list before committing a create, join or merge operation, retaining the prior lists and membership until success is established.
If any destination cannot support a bill, has unsupported consequential configuration or has active
or attached unfinished work that would be displaced by replacing its list, reject the entire
operation with the affected bench and reason. No source or destination is partially changed.
The source's own existing jobs, unfinished items and machine progress remain attached to it, with
their commitments included in the new group's counts. A failed application restores the prior
lists, choices, membership and production accounting. Restoring does not fabricate or duplicate
physical materials, work or products. The player can finish or explicitly cancel destination work
through ordinary controls before trying again.

#### Scenario: One incompatible destination among several
- **WHEN** a multi-bench link includes one bench unable to run a source recipe
- **THEN** the entire link is rejected with that bench and recipe identified
- **AND** every original bill list, count and membership remains unchanged

#### Scenario: Destination has unfinished work
- **WHEN** replacing a destination's list would displace its active job, bound unfinished product or machine process
- **THEN** linking is rejected without canceling, unbinding or duplicating that work
- **AND** the explanation directs the player to finish or cancel it before replacing the list

#### Scenario: Source is already working
- **WHEN** a source bench with an ongoing valid production iteration creates a group with an idle compatible destination
- **THEN** the source's actual work continues with its original materials and worker
- **AND** that iteration counts once toward the shared remaining order

### Requirement: Every logical bill edit propagates once without losing distinct rows
The mod SHALL apply addition, deletion, duplication, ordering, renaming, suspension and every supported editable bill choice to the whole group, visible by the next relevant view or work decision even while the game is paused.
Shared choices include recipe/output identity, style and variant, ingredient and special filters,
ingredient radius, worker restriction and skill range, repeat/target quantities and mode,
pause/resume thresholds, product counting scope and filters, and output destination. Applicable
special-production or supported provider choices are included. Two identical recipes are still
distinct bills; changing one does not change the other. An explicit Add, Paste or Duplicate creates
one new logical row, mirrored once at each member. The initial native limit is 15 logical bills;
linking does not multiply or bypass it. A separately specified capacity feature can change that limit.
An edit that cannot be represented consistently is rejected before partial change. A stale open
dialog cannot restore a deleted row or overwrite unrelated newer choices merely by closing.

#### Scenario: All configuration follows the edited row
- **WHEN** the player changes a linked bill's material filter, pawn restriction, output storage and graphic variant
- **THEN** every member shows those same choices for that bill
- **AND** the other bill using the same recipe keeps its separate choices

#### Scenario: Add and reorder while paused
- **WHEN** the player adds one bill through the integrated browser, then moves it above another bill while paused
- **THEN** all members show one added row in the new position on their next display
- **AND** propagation neither repeats Add nor creates one logical order per member

#### Scenario: Limit and stale dialog
- **WHEN** a group with 15 bills receives another Add without a separately specified capacity increase
- **THEN** the addition is rejected consistently without a partial extra row
- **AND** a dialog left open for a subsequently deleted row cannot recreate that row

### Requirement: Recipe browsing and availability respect the complete group
The mod SHALL offer new linked bills only from recipes and bill behaviors supported by every current member, while retaining ordinary research and recipe availability checks.
The Add bill browser states that it is adding to the group. Search, category and applicable-stat
controls use that common recipe catalog; preview and exact material choices follow the browser's
existing contract at the opened bench. Recipe hiding remains a browsing preference, not deletion
of a linked bill. Losing structural recipe or provider support later retains the affected shared
row and its choices with an unavailable explanation, blocks new work for that row throughout the
group and leaves unaffected rows usable. Restore support or remove the incompatible member to
recover; no silent recipe substitution or partial member-only list is allowed.
Temporary lack of power, fuel, ingredients, reachable workers or research does not delete a row or
change its configuration. Ordinary workability is evaluated at the actual member doing the job.

#### Scenario: Common catalog on mixed benches
- **WHEN** linked bench A supports recipes X and Y, but bench B supports only X
- **THEN** new group bills offer X and explain why Y cannot be added to this group
- **AND** unlinking B can make Y available again without changing existing X bills

#### Scenario: Hidden recipe has an existing bill
- **WHEN** a browsing preference hides a recipe at the opened bench
- **THEN** an existing linked bill for that recipe remains in the shared queue and can run normally

#### Scenario: Support disappears after linking
- **WHEN** a provider change makes one existing recipe unsupported by a member
- **THEN** the corresponding row remains visible and unavailable across the group with its choices retained
- **AND** other valid rows and ordinary unlinked work remain available

#### Scenario: One member lacks power
- **WHEN** one linked bench loses power while another remains usable
- **THEN** only the unpowered member stops being available for work requiring power
- **AND** another eligible member can continue the shared order without unlocking any recipe or creating resources

### Requirement: Finite repetitions are one shared remaining order
The mod SHALL treat Do X times as X remaining recipe iterations for the group in total, including started work not yet completed, rather than X repetitions per member.
An iteration becomes committed when its actual work starts, including ingredient collection;
previews and work queries do not consume quota.
Do not start more iterations than the uncommitted remainder. Each successful native iteration counts
once, regardless of its product stack size or number of outputs, and failed or abandoned work does
not count as success. Resuming the same unfinished or machine iteration does not reserve another
iteration. The display distinguishes remaining, committed and available-to-start iterations.
Completing or collecting an already produced result must not start a new iteration or count twice.
Report completion once when the finite production order is satisfied; pending collection remains
visible. Delivery failure or later destruction of a produced item does not replenish the quota.

#### Scenario: Two benches finish a shared order
- **WHEN** a group has two remaining iterations and two eligible workers start one each
- **THEN** no third iteration can start at another member
- **AND** the two completed iterations satisfy the order once instead of granting each bench two repetitions

#### Scenario: Batch recipe counts iterations
- **WHEN** a recipe produces four items per iteration and the group is ordered to do it twice
- **THEN** two successful iterations satisfy the order, producing their ordinary outputs
- **AND** linking does not interpret the order as two items or eight additional repetitions

#### Scenario: Query and interruption
- **WHEN** a pawn queries work without starting it, or a started iteration aborts without a resumable product or process
- **THEN** the query commits nothing and the abandoned commitment becomes available again
- **AND** actual consumed materials and lost ordinary progress are not refunded by quota accounting

#### Scenario: Unfinished and already formed results
- **WHEN** an interrupted iteration retains its unfinished item or a machine has produced its result but collection remains
- **THEN** continuation uses that same iteration without another quota charge or duplicated product
- **AND** reaching zero new iterations does not strand the remaining ordinary completion or collection work

### Requirement: Changing repeat choices respects already committed work
The mod SHALL keep shared finite counts nonnegative and reject reducing a finite remainder below its already committed unfinished iterations, explaining how much work must finish or be canceled first.
Increasing the remainder adds only the stated extra iterations once. Switching from Forever or
target-count mode to finite mode requires a count that includes every unfinished iteration already
committed to the row. Switching out of finite mode does not cancel, duplicate or restart those
iterations. Suspension stops new starts and pauses or interrupts active work through its
ordinary mechanisms; it does not complete work or refund ingredients. Resume continues the existing
shared plan and remaining work without resetting its quota. Completed-but-uncollected output is
not an unfinished finite commitment.

#### Scenario: Increase or reduce a finite order
- **WHEN** a row has three iterations remaining, two already committed, and the player changes the total to five
- **THEN** five remain for the whole group, including those two commitments
- **AND** an attempted change to one is rejected without canceling existing work or changing any member's count

#### Scenario: Switch mode with work in progress
- **WHEN** a Forever row with two unfinished iterations is switched to Do X times
- **THEN** its new finite remainder must include at least those two
- **AND** completing them consumes two from that shared remainder without starting them again

#### Scenario: Suspend and resume
- **WHEN** the player suspends a linked row with an unfinished item and an autonomous process
- **THEN** the suspension applies to all members, preserving the physical item/process according to ordinary interruption rules
- **AND** resuming does not duplicate the unfinished work, restore already completed iterations or reset machine time

### Requirement: Target-count mode counts actual stock once and shares hysteresis
The mod SHALL use the linked row's selected map or storage counting scope and applicable product-counting rules once for the whole group, with one target, one pause-when-satisfied choice and one resume threshold.
The production-stock-counting capability refines native counting for its admitted ordinary products,
including unique units, additional types and explicit carrier/away choices. Other counters retain
their supported native behavior; no unknown counter is inferred from its display name.
Do not sum identical stock counts from every member. Preserve equipped/tainted, HP/quality, allowed
material and storage filters. Only recipes whose outputs support meaningful native counting offer
target-count mode. Hysteresis pauses new starts on reaching the target and, when enabled, resumes at
or below the chosen lower threshold; manual Unpause affects the group. Already started iterations
can finish and ordinary in-flight overshoot is possible. Planned or reserved products are not
displayed as existing stock. Forever mode has no finite quota but still uses ordinary physical work.
Population-production extends this shared target with one matching query and transformed target/resume
per logical row, retaining the same physical-work and unique-stock rules. Surplus-production instead
uses one shared ingredient trigger and keeps committed work/collection distinct from new starts.
Those modes' complete choices synchronize with the whole list; they do not create per-bench targets.

#### Scenario: One stock count across several benches
- **WHEN** three linked benches share a target of 20 and the chosen scope contains 12 qualifying products
- **THEN** every member displays 12 out of 20, not 36 out of 20
- **AND** materials being carried for future output do not increase the current product count

#### Scenario: Shared pause and manual resume
- **WHEN** a row with target 20 and resume threshold 10 reaches 20 qualifying products
- **THEN** new starts pause for the entire group until stock is at most 10 or the player explicitly unpauses
- **AND** merely opening another member's Bills view does not establish a separate pause state

#### Scenario: Work already underway
- **WHEN** two ordinary iterations are already underway just before another product brings stock to the target
- **THEN** those iterations can finish with normal costs and yields
- **AND** the interface does not promise exact target fulfillment without ordinary concurrent-work overshoot

### Requirement: Shared choices do not copy or teleport physical work
The mod SHALL preserve ordinary worker eligibility, reservations, pathing, ingredient acquisition, costs, recipe work, skill experience, quality rolls, output handling and machine timing at the actual executing station.
The same ingredient radius is measured around each member's own work location. A named worker
restriction refers to that pawn, not one equivalent pawn per bench. Unfinished items retain their
actual creator, materials and work; machine processes retain their actual contents, owner/mechanitor,
bandwidth, corpse or other selected physical subject. No shared-list edit fabricates those things or
transfers progress to every station. Each member can work concurrently when ordinarily capable and
the shared order permits it. Ready local results remain finishable without permitting
new iterations. A provider's restrictions remain in force even for player-forced work.

#### Scenario: Radius and local resources
- **WHEN** linked benches use the same radius but only one has reachable permitted ingredients within its own radius
- **THEN** that bench can obtain work and the other cannot use the first bench as a remote search origin

#### Scenario: Independent unfinished products
- **WHEN** two linked crafting benches have distinct unfinished items made by different pawns
- **THEN** each item keeps its own creator, ingredients and remaining work
- **AND** resuming either completes that physical item once under the appropriate shared bill

#### Scenario: Mech or autonomous production
- **WHEN** compatible linked machines are at different processing stages or have different bound mechanitors or subjects
- **THEN** shared configuration does not copy those stages, pawns, subjects or elapsed time between machines
- **AND** ordinary bandwidth, fuel, power and completion restrictions still govern each process

### Requirement: Editing and deleting preserve native work consequences
The mod SHALL apply renaming and reordering without restarting current work, and apply ingredient, radius and worker choices to new work without retroactively replacing materials or the worker of a started iteration.
An existing unfinished iteration can resume its already committed materials and worker subject to
ordinary physical eligibility; it is identified as existing work if its materials differ from new
choices. Current shared style and output destination apply at the ordinary result-selection or
storage step where those choices can still take effect. A process whose physical subject/output
already exists retains that identity. Deleting a shared row removes it everywhere and terminates
its active bill work through ordinary deletion handling. Preserve/eject existing contents and leave
or unbind unfinished items as the native operation does; do not automatically cancel an unfinished
item for a refund or silently recreate the deleted row elsewhere. A stale action on a deleted row
fails visibly rather than editing the row that later occupies the same list position.

#### Scenario: Reorder active production
- **WHEN** the player moves a running linked bill below another row
- **THEN** its current iterations continue without losing or resetting their ordinary progress
- **AND** subsequent work selection uses the new order at every member

#### Scenario: Change material after work starts
- **WHEN** a steel item is already being crafted and the group is changed to permit only plasteel for future starts
- **THEN** the existing steel iteration keeps its real materials and can finish normally
- **AND** newly started iterations obey the plasteel choice at every member

#### Scenario: Delete an active shared row
- **WHEN** the player deletes a linked row with unfinished items and an active machine process
- **THEN** the row disappears from every member and its active work stops using ordinary deletion handling
- **AND** physical items/contents follow native retention or ejection rules without cloned refunds or an automatic replacement row

### Requirement: Leaving and dissolving allocate remaining finite work explicitly
The mod SHALL let a member leave while retaining a separate copy of the complete current list and its editable choices, keeping its actual work in progress at that member.
The separation preview assigns each unfinished finite commitment to the successor list that owns
the physical work. Unstarted finite repetitions go to the continuing group by default; the player
can explicitly choose the leaving member instead. Show the resulting counts before commit. The
combined finite remainder must equal the previous remainder, rather than being duplicated in both
lists. Target and Forever choices are copied as independent plans after separation, with the preview
explaining that they no longer synchronize. If one member remains, its list becomes independent local
state with the group's remaining work. Leave, Dissolve and last-member conversion retain the ability
to collect already credited machine output even when its new local finite remainder is zero; collection
does not count it again. Dissolve uses the same rule: each member keeps its physical
commitments and one explicitly selected member receives all unstarted finite repetitions, initially
the selected bench. Failed or canceled separation preserves the original group and work.

#### Scenario: Leave with committed work
- **WHEN** a group has five finite iterations left, the leaving bench owns one unfinished iteration and other members own one
- **THEN** the default preview assigns one to the leaving bench and four to the continuing group
- **AND** choosing the leaving bench for unstarted repetitions instead assigns four to it and one to the continuing group

#### Scenario: Separate target or Forever plans
- **WHEN** a member leaves with a target-count or Forever row
- **THEN** it keeps the complete row and current choices as an independent plan with an explicit preview of that consequence
- **AND** subsequent edits to either list do not synchronize

#### Scenario: Dissolve or retain the last member
- **WHEN** the player dissolves a group
- **THEN** every member retains its complete list and physical work, with finite commitments and unstarted counts allocated as previewed
- **AND** an automatically remaining single member instead retains all remaining counts without prompting or multiplying them

#### Scenario: Collect credited output after separation
- **WHEN** a machine already holds a credited product, its remaining count is zero, and it leaves or its group dissolves or becomes a single member
- **THEN** that same product remains collectable through the ordinary local completion workflow
- **AND** collection neither starts another iteration nor credits or announces the same product twice

### Requirement: Movement destruction and reconstruction preserve meaningful membership
The mod SHALL retain membership and choices when the same workbench is temporarily minified or otherwise unavailable on its map, without allowing it to start work while unplaced.
Group edits still reach that member's saved list. Reinstalling it on the same map resumes ordinary
eligibility without resetting shared counts. Destruction removes the actual member and reconciles
its work once: a surviving resumable item remains one commitment of its logical row, while an
aborted iteration with no resumable work releases its commitment without refunding materials.
Completed output never replenishes quota. Another valid member can resume surviving work only through
ordinary physical and creator restrictions. Supported quality or material reconstruction keeps its
group membership through the identified replacement, never by matching any new object at the cell.
A fresh unrelated bench is unlinked. Cross-map transfer separates transferred members before they
can work on the new map: work physically transported with them follows them, unstarted finite
repetitions stay with the remaining original group, and transferred lists are visibly suspended for
review of map-local choices. A separate unfinished item left behind stays with the original group
and counts only there while that group remains. If the entire group transfers together, keep its
internal relationship and remainder but still require review/resume on the new map. Any unfinished
item left behind in that case remains one visibly unavailable commitment of the transferred plan
until it is physically recovered, explicitly canceled or becomes non-resumable through ordinary
game events; it is not teleported or silently
replaced by new work. Removing a map cannot leave a hidden running order.

#### Scenario: Minify and reinstall on the same map
- **WHEN** a linked bench is minified, its group is edited, and that same bench is reinstalled on the same map
- **THEN** it shows the current shared list and unchanged group accounting
- **AND** minification did not duplicate or complete any production

#### Scenario: Destroy one member
- **WHEN** a member is destroyed while another member survives
- **THEN** the remaining group keeps its choices and unfinished quota
- **AND** real surviving unfinished work remains counted once, while lost non-resumable work does not count as a successful product

#### Scenario: Supported reconstruction versus unrelated placement
- **WHEN** supported quality reconstruction replaces an identified group member
- **THEN** the matching replacement retains membership and shared choices
- **AND** placing an unrelated bench in the same cell does not inherit them

#### Scenario: Transfer to another map
- **WHEN** one member moves to a different map while others stay
- **THEN** its local list is separated and suspended for review, with its own unfinished commitments but no duplicated unstarted quota
- **AND** no stale storage or pawn reference is silently replaced to make it run there

#### Scenario: Bench moves but unfinished item stays
- **WHEN** one member transfers to another map without its separate unfinished item and the original group still has members
- **THEN** the item and its one commitment remain with the original group for ordinary eligible continuation
- **AND** the transferred list does not acquire a copy of either its progress or its finite commitment

#### Scenario: All benches transfer while an unfinished item stays
- **WHEN** the whole group transfers but an unfinished item remains on the old map
- **THEN** the moved plan shows that one commitment as unavailable and preserves its physical item and remaining work
- **AND** it cannot spend that commitment on another iteration unless the original is explicitly canceled or becomes non-resumable through ordinary game events; physically recovering it permits continuation once

### Requirement: Missing references and compatibility failure are visible and recoverable
The mod SHALL retain unavailable named workers, storage/count scopes, recipes, styles and provider choices as explicit recovery information, without silently broadening a restriction or substituting another choice.
An affected row cannot start new work until the player selects a valid replacement or deliberately
chooses the ordinary fallback. A temporary unavailable worker or station simply waits under its
existing policy; it does not silently become Any worker. Already started work follows its ordinary
physical and cancellation rules, with blocked output placed safely through native behavior if its
destination vanished. Failures of required group-support prerequisites produce a bounded startup
warning and suspend affected saved groups before independent member copies could start duplicate
work or unsafe active work could commit. Retain group choices, counts and physical work for recovery;
do not silently dissolve the group. A restoration requires explicit Resume after consistency is
established. Benign foreign changes and absent optional mods do not disqualify supported native groups.

#### Scenario: Storage or assigned pawn disappears
- **WHEN** a linked bill loses its selected storage scope or a permanently unavailable named worker
- **THEN** all members show the same unresolved choice and stop new starts for that row
- **AND** the player can choose a replacement or explicit native fallback without other fields changing

#### Scenario: Compose with disappearing linked storage
- **WHEN** the linked storage group chosen for a shared bill's count or output ceases to exist
- **THEN** this unresolved-choice rule takes precedence for the linked workbench row, preserving the lost scope and blocking new starts
- **AND** the storage feature's native fallback continues to apply to ordinary unlinked bills

#### Scenario: Loaded group without valid support
- **WHEN** a save containing linked remaining counts and active work loads but required group support is rejected
- **THEN** the affected group is suspended before member copies can act as independent full orders or unsafe work commits
- **AND** its saved plan and real physical work remain recoverable, with an explanation and no duplicated quota

#### Scenario: Restored support and explicit recovery
- **WHEN** compatible support returns and the saved group's counts and physical commitments reconcile successfully
- **THEN** the group stays suspended until the player explicitly resumes it
- **AND** Resume neither restarts completed iterations nor resets surviving progress or remaining counts

### Requirement: Save and composition preserve one authoritative production plan
The mod SHALL save group identity, members, logical row identities, ordering, choices, shared counts/pause state and outstanding physical-work associations, restoring them consistently before work resumes.
Reloading or switching saves must not reset a quota, multiply rows, replay completion or connect
unrelated equal-looking bills. A rejected edit or membership operation restores its prior valid
state. The integrated Add bill browser adds one logical row with its chosen material; selected bill
modifiers add their owned choices to this shared plan directly. Linked storage remains the selected
count/output entity. A bench-wide worker policy from workbench management is also shared across the
whole plan; source policy, replacement preview and successor copies follow these same membership
rules, never member-local overrides. Additional-product and away choices synchronize per logical row.
Supported reconstruction retains the same membership. Combining these
features does not create duplicate bills or production. No automatic import of another mod's link
data or compatibility with its package-specific bindings is implied.
Native prices, ingredient quantities, output quantities, work, research and quality probabilities
are unchanged by linking itself.

#### Scenario: Reload identical recipes with different choices
- **WHEN** a group containing two rows of the same recipe but different materials and counts is saved and loaded
- **THEN** the two rows retain their identities, order, choices and separate shared accounting
- **AND** loading does not combine them or restart previously completed production

#### Scenario: Browser and storage composition
- **WHEN** a player adds one exact-material bill through the integrated browser and selects a linked storage group for counting/output
- **THEN** every member receives that one logical bill and the same explicit material/storage choices
- **AND** no member silently chooses a different material, counts the storage repeatedly or bypasses normal costs

#### Scenario: Save isolation
- **WHEN** the player switches to another save with similarly named benches and groups
- **THEN** only that save's own memberships, bill choices and physical work are restored
- **AND** neither save acquires the other's counts, references or production progress
