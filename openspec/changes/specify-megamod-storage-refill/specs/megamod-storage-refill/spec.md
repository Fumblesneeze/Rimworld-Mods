# Controlled storage refilling

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
Origin: replacement, S049 KanbanStockpile Continued, with confirmed custom refill behavior.
Specification only; no gameplay implementation or acceptance.

## ADDED Requirements

### Requirement: KS-01 Refill controls belong to the selected storage

The mod SHALL expose refill controls through the contextual storage configuration of supported
stockpiles, shelves and their linked groups. The player SHALL edit quantities per individual item
type, never a shared category quantity. Each row SHALL identify its type, qualifying stored quantity,
incoming automatic quantity, start-below quantity X, fill-to quantity Y and current refill status.
Categories and search SHALL help find rows without turning categories into targets. Choices SHALL
respect storage's intrinsic item restrictions. These entity controls SHALL NOT become mod settings,
a new bottom tab or many per-item gizmos. Labels, explanations and validation SHALL be translated
in all seven baseline languages.

#### Scenario: A wood-only storage is selected
- **WHEN** the player opens its refill controls
- **THEN** eligible woody item types can be configured and meat cannot be selected as a usable target.

#### Scenario: Linked storage is inspected from either member
- **WHEN** the player opens a linked shelf and then a linked stockpile
- **THEN** both display the same group quantities and policy, clearly identified as affecting the group.

#### Scenario: Search does not create a category quota
- **WHEN** the player searches for medicine or expands its category
- **THEN** each eligible medicine type has its own row and target, rather than sharing one medicine total.

### Requirement: KS-02 Item totals describe real stock across the whole group

The mod SHALL count qualifying stored units of an item type across all members of its current
linked group, or only the independent storage when unlinked. A physical item SHALL contribute once.
Items SHALL qualify when their type and characteristics pass the current shared filter and their
member's intrinsic storage restrictions. Material, quality and condition variants of the same type
SHALL share the type total when they qualify; their ordinary stack-merging rules SHALL remain distinct.
Types SHALL not be combined merely because they share a category. Counts SHALL include qualifying
stored items even when temporarily forbidden, reserved for another job or unreachable by a particular
hauler. Items outside the group, equipped or carried by pawns SHALL NOT appear as stored stock.
Known incoming automatic deliveries SHALL be shown separately. Actual removal, consumption, splitting,
merging, arrival and membership changes SHALL be reflected before subsequent refill decisions.

#### Scenario: Two shelves already satisfy a target
- **WHEN** linked shelves hold 8 and 12 qualifying meals of the same type and Y is 20
- **THEN** the group shows 20 stored and does not request another 20 for each shelf.

#### Scenario: Different materials do not create hidden subtargets
- **WHEN** the filter allows steel and wood variants of one non-stackable weapon type
- **THEN** both contribute to that weapon's quantity while remaining separate physical items.

#### Scenario: Stored and carried are distinguished
- **WHEN** a pawn removes 5 of 20 units and another pawn is bringing 3 units
- **THEN** the group shows 15 stored and 3 incoming, without counting either carried quantity as stored.

#### Scenario: A filter stops accepting part of the stock
- **WHEN** its quality restriction excludes some items already in the storage
- **THEN** those items no longer satisfy the refill target, remain physically present and follow ordinary storage handling.

### Requirement: KS-03 Refill cycles start below X and finish at Y

New storage SHALL have no enabled per-type refill targets, preserving normal automatic storage
behavior unless another stack control is enabled. Enabling a new type rule SHALL initially offer
X=1 unit and Y equal to that type's current physical stack maximum, with the player free to change
both. Valid enabled rules SHALL use positive whole quantities with X no greater than Y; invalid
edits SHALL explain the problem and leave the last valid policy active.

A waiting rule SHALL start a refill cycle only when its qualifying stored quantity is strictly
below X. The cycle SHALL remain open through intermediate quantities until actual stored stock
reaches Y. Incoming deliveries SHALL reduce further demand but SHALL NOT close the cycle before
arrival. A cancelled or failed delivery SHALL release its contribution so missing stock can be
supplied again. A waiting quantity equal to X SHALL remain waiting. At X=Y, any shortage below
that quantity SHALL open a cycle. Disabling a rule SHALL remove its quantity restriction without
deleting items. On enabling or editing X/Y, the cycle SHALL be reevaluated: open below X, waiting
otherwise; editing unrelated storage presentation SHALL NOT reset it.

#### Scenario: Strict lower boundary
- **WHEN** a waiting rule has X=10, Y=30 and exactly 10 stored units
- **THEN** it remains waiting; consumption to 9 opens a refill cycle toward 30.

#### Scenario: Refilling continues above the lower threshold
- **WHEN** an open cycle with X=10, Y=30 receives stock from 9 to 18
- **THEN** it continues seeking the remaining 12 units rather than waiting again at 18.

#### Scenario: An incoming load fails
- **WHEN** an open cycle needs 12 units and a promised load of 12 is cancelled
- **THEN** the group becomes eligible to request those 12 again without needing consumption below X.

#### Scenario: A rule is configured while stock is between its limits
- **WHEN** the player enables X=10 and Y=30 with 18 units already stored
- **THEN** it starts waiting and begins its first cycle only after stock falls below 10.

#### Scenario: An invalid target is entered
- **WHEN** the player enters a negative quantity, fraction, unrepresentable quantity or X greater than Y
- **THEN** the invalid edit is identified and does not replace the last valid rule.

### Requirement: KS-04 One stack-fill slider limits further automatic hauling

Every supported independent storage or linked group SHALL have one standard stack-fill slider
applying to all item types and stacks, initially **Unlimited**. A finite choice SHALL be a positive
whole unit count; its effective limit for any stack SHALL never exceed that item's actual physical
stack maximum. The slider SHALL offer finite values through the largest current physical maximum
among accepted types, plus Unlimited; a previously saved higher choice SHALL remain displayed and
retained when available types change, with its effective restriction explained. With no accepted
types, only Unlimited SHALL be newly selectable and any saved finite choice SHALL remain retained.
This is a single storage policy, not a per-type stack-size table.

Automatic deliveries SHALL fill a compatible existing stack only up to the lower of this choice
and its actual maximum. Empty usable stack positions SHALL observe the same limit. The setting
SHALL NOT resize, split, delete or enlarge existing stacks, change physical stack limits or force
removal of an existing larger stack. Unlimited SHALL preserve actual physical limits.

#### Scenario: One choice covers different stack sizes
- **WHEN** the slider is 20 and two accepted item types have physical maxima 10 and 75
- **THEN** automatic hauling fills their stacks to at most 10 and 20 respectively.

#### Scenario: Existing stock is larger than the new choice
- **WHEN** a 50-unit stack exists and the player changes the slider to 20
- **THEN** the 50 units stay together and receive no further automatic refill until there is room below the effective limit.

#### Scenario: Unlimited does not increase capacity
- **WHEN** a stack is physically full and the slider is Unlimited
- **THEN** a hauler cannot add more to that stack.

#### Scenario: The filter changes the slider's finite range
- **WHEN** a saved choice of 50 remains but accepted types now have maxima no greater than 10
- **THEN** 50 remains the saved choice, its effective maximum of 10 is explained, and existing stock is unchanged.

### Requirement: KS-05 Optional similar-stack limits remain useful without resizing stock

The mod SHALL retain a separate similar-stack-count limit, initially Off, with choices 1 through 8
as in the source. It SHALL apply per item type across the same linked group. It SHALL count actual
qualifying physical stacks, including each non-stackable item, with all accepted variants of a
type sharing the limit. An oversized existing stack SHALL count once, not as imagined smaller
stacks. Incoming allocations that require distinct new stack positions SHALL also consume that
allowance without counting several deliveries to the same position as several future stacks.
Existing qualifying stacks SHALL still be refillable within other limits when the count limit is
reached. Lowering the limit SHALL NOT remove existing stock. Unavailable new stack space SHALL
block further deliveries without a retry loop and SHALL be explained in the contextual status.

#### Scenario: Variety without prohibiting refill
- **WHEN** the limit is 1, one partial stack of herbal medicine exists and glitterworld medicine is absent
- **THEN** herbal medicine can refill that stack within its other limits and glitterworld medicine can use its own first stack.

#### Scenario: Two workers plan the last new stack
- **WHEN** one additional stack is allowed and a delivery already claims its intended position
- **THEN** another delivery can share remaining capacity in that position but cannot claim a second new stack.

#### Scenario: Stack count makes the fill target unreachable
- **WHEN** Y is 100, the fill slider is 20 and the similar-stack limit is 2
- **THEN** the rule explains that these limits prevent reaching 100, retains the target and does not shuttle items to manufacture space.

#### Scenario: A large stack survives a changed policy
- **WHEN** a 50-unit stack remains after the slider is lowered to 20 and the count limit is 1
- **THEN** it counts as one stack and is neither split nor evicted by the refill feature.

### Requirement: KS-06 Automatic haulers share the remaining demand

Ordinary automatic storage hauling and owned batch/opportunistic automatic hauling SHALL apply
the same refill, stack-fill and stack-count limits before collecting and before delivering cargo.
New allocations SHALL account for qualifying stock and other valid incoming quantities, claim only
the remaining demand and physical space, and never exceed the worker's actual shared carrying
allowance. A worker's own allocation SHALL not be counted against it twice. Partial pickup, partial
delivery, interruption, destination loss and cargo loss SHALL release or adjust claims promptly.
This coordination SHALL NOT be optional for performance reasons.

Native filters, priority, reservations, access, safety and destination capacity SHALL remain in
force. Refill SHALL neither pull items between members merely to balance their individual totals
nor authorize equal/lower-priority transfers by itself. A blocked rule SHALL remain dormant until
relevant conditions change, without repeated pickup/drop loops. A changed destination or external
delivery that consumes demand SHALL cause a safe reduction or ordinary valid rerouting of excess
cargo, never its destruction or an artificial increase in capacity. An item reserved for outgoing
work SHALL count as stored until actually removed; any return SHALL be counted only once.

#### Scenario: Concurrent hauling does not double the request
- **WHEN** an open rule has 12 stored, Y=30 and 10 units already assigned to an incoming hauler
- **THEN** another automatic hauler can allocate at most the remaining 8 units, subject to capacity.

#### Scenario: Batch hauling remains available
- **WHEN** one pawn can legally carry several eligible item types into configured storage
- **THEN** the owned hauling feature can batch their remaining quantities without disabling batching simply because refill controls are enabled.

#### Scenario: Conditions change while cargo is carried
- **WHEN** crafting output fills the target before an automatic hauler arrives
- **THEN** the hauler rechecks demand, delivers only what remains permitted and safely reroutes or normally drops excess without losing it.

#### Scenario: A linked group already contains its stock
- **WHEN** one shelf is empty but other group members satisfy the target
- **THEN** refill does not move items into the empty shelf merely to give every member its own target.

#### Scenario: The only source is inaccessible
- **WHEN** required stock is forbidden or unreachable to available workers
- **THEN** no unsafe job is created and the open cycle can resume when ordinary eligibility permits it.

### Requirement: KS-07 Refill limits are not physical storage bans

The mod SHALL apply these controls to automatic replenishment, not every delivery or storage test.
Explicit player-directed deliveries, production output, trade/gift drops and emergency cargo
recovery SHALL remain able to exceed Y, the automatic stack-fill choice and similar-stack-count
limit when ordinary physical placement permits. A player merely designating an item for automatic
or urgent hauling SHALL still use the refill policy; an explicit directed delivery remains distinct.
Transporter loading, caravan loading, construction ingredients, food/medicine supply to consumers
and other non-storage jobs SHALL NOT acquire refill restrictions. Controls SHALL NOT change an
existing item's otherwise-valid storage classification or cause a removal job solely for exceeding
a refill limit. Extra stock SHALL count normally afterward, pause demand at Y and resume only
through the normal lower-threshold rule. Native stack-merging, item properties and actual capacities
SHALL remain unchanged for all paths.

#### Scenario: Crafted output exceeds the target
- **WHEN** a valid bill output delivery raises a group from 29 to 35 with Y=30
- **THEN** all 35 units remain and automatic refill waits; the production job is not capped at one unit.

#### Scenario: A directed delivery is different from an urgent designation
- **WHEN** a player explicitly orders an otherwise-valid delivery to a satisfied storage
- **THEN** the delivery can exceed the refill limits
- **AND** merely marking loose items for urgent automatic hauling does not bypass them.

#### Scenario: A pawn fetches construction materials
- **WHEN** construction or another non-storage task needs material from the group
- **THEN** refill controls do not prevent the ordinary task from consuming or transporting it.

#### Scenario: Existing stock is above a limit
- **WHEN** an otherwise-valid stored item exceeds Y or a stack control
- **THEN** the refill feature does not reclassify it as misplaced or schedule eviction.

### Requirement: KS-08 Policy and refill state survive storage lifecycle changes

Refill choices SHALL form part of S005's complete editable storage policy. Linking SHALL adopt the
first selected eligible member's policy; editing or pasting through any member SHALL affect the
receiving group. Unlinking SHALL retain independent copies. Copy/paste SHALL copy choices, never
live incoming claims, current stock or another storage's open-cycle state. A newly formed group,
a receiving group after linking, a newly independent member after unlinking or group dissolution,
and a pasted-into scope SHALL reevaluate cycles from its own current stock, opening below X and
waiting otherwise. An existing source group that survives losing members with at least two members
remaining SHALL retain its current cycle; it SHALL still close on reaching Y or start below X
under the ordinary cycle rules. All resulting scopes SHALL discard or reassign stale incoming
allocations before new work.

Saving/reloading SHALL retain policies and whether an unchanged cycle was open, rebuild valid
incoming deliveries and recompute actual stock before more hauling. Renaming SHALL not lose or
cross-wire policy. Resizing or deleting storage SHALL update its actual contents and capacity;
removing one member SHALL not erase surviving policy. Cross-map moves SHALL follow S005's
independent-copy rule. Missing item types SHALL remain visibly unresolved and inactive rather
than bind to another similarly named type. New storage, maps and saves SHALL not inherit stale
state or clipboard choices from an unrelated game. Source KanbanStockpile saves SHALL NOT be
silently converted into the different quantity-based model.

#### Scenario: Reload in the middle of a refill cycle
- **WHEN** X=10, Y=30 and an open cycle at 18 stored is saved and reloaded
- **THEN** it remains open toward 30, with valid incoming work counted once.

#### Scenario: Paste choices into stocked storage
- **WHEN** an open rule at the source is copied into independent storage already between X and Y
- **THEN** the destination receives the choices but starts waiting instead of inheriting the source's cycle or claims.

#### Scenario: Unlink a shelf from a group
- **WHEN** the player unlinks it
- **THEN** departing and surviving scopes retain the applicable policy, newly independent storage reevaluates its own stock, surviving groups keep their cycle and no scope counts deliveries for the wrong group.

#### Scenario: An empty member leaves an active group
- **WHEN** a three-member group with X=10, Y=30 and an open cycle at 18 units loses its empty third member
- **THEN** the remaining two-member group continues refilling from 18 toward 30
- **AND** the newly independent empty member evaluates its copied rule and opens its own cycle.

#### Scenario: A two-member group dissolves
- **WHEN** an open group with X=10, Y=30 holds 12 units in one member and 6 in the other, and unlinking makes both independent
- **THEN** each retains the policy, the 12-unit storage starts waiting and the 6-unit storage starts refilling.

#### Scenario: Rename and switch saves
- **WHEN** storage is renamed, saved and loaded after visiting another save with the same storage label
- **THEN** its original policy and cycle belong to the correct storage and no other save's choices leak into it.

#### Scenario: An item provider is removed
- **WHEN** a configured item type is absent after load
- **THEN** its row is unresolved and inactive, other rows remain usable, and its identity is retained for later recovery.

### Requirement: KS-09 Incompatible contexts fall back coherently

The mod SHALL enable refill controls only where the complete automatic-versus-directed delivery,
quantity, persistence and contextual-control behavior can be supported. Compatible content and
unrelated patches SHALL not by themselves disable it. An active original KanbanStockpile Continued
SHALL suppress the owned refill integration as a whole, preserving its stored policy without
attempting to remove foreign patches or claiming to repair the original. Unsupported optional
storage/hauling contexts SHALL be identified and retain ordinary provider behavior; affected owned
controls SHALL be visibly unavailable instead of promising unenforced limits. Each skipped enabled
patch SHALL receive a bounded startup warning naming the unavailable behavior and fallback.
Supported independent contexts SHALL continue to work when isolation is possible. No failed
admission SHALL destroy items, modify physical stack sizes or silently rewrite source saves.

#### Scenario: Compatible third-party content
- **WHEN** a mod adds an ordinary storable item with supported stack and filter behavior
- **THEN** it can participate without requiring its exact English name or an otherwise-unpatched game.

#### Scenario: Original provider is active
- **WHEN** KanbanStockpile Continued is active alongside the megamod
- **THEN** owned refill enforcement and editing are inactive with an explanation, and original behavior remains foreign-owned.

#### Scenario: Required delivery behavior has changed
- **WHEN** an integration can no longer distinguish automatic refill from production output
- **THEN** the affected refill integration is inactive with a bounded warning and ordinary storage remains available.
