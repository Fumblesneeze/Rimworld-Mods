# Population-dependent production

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`). Target: **1.7 beta**.
Origin: replacement, Everybody Gets One - Continued. Specification only; nothing implemented.

## ADDED Requirements

### Requirement: Offer two understandable adaptive targets
The mod SHALL offer Population plus offset and Quantity per match on supported production bills whose products can be counted meaningfully.
For a current matching quantity P and entered quantity A, the targets are P + A and P times A,
respectively, with a minimum effective target of zero. Offset accepts negative whole numbers;
quantity per match accepts nonnegative whole numbers. Display the mode, matching quantity, entered
operand, effective target and current qualifying product total separately. A negative or zero
effective target requests no new products. Ordinary recipe outputs, quantities per batch, costs,
skills, research and work remain unchanged. Unsupported recipes show a reason and retain their
previous mode rather than acquiring a guessed counter.

#### Scenario: Follow a changing colony
- **WHEN** five matching colonists have a bill set to two products per match and a sixth colonist arrives
- **THEN** the target changes from ten to twelve on the next evaluation without the player editing the bill.

#### Scenario: Reserve fewer than the population
- **WHEN** five people match a bill with offset minus two
- **THEN** the target is three; if the matching population falls to two or fewer, no new work is requested.

#### Scenario: Unsupported output
- **WHEN** a player tries an adaptive target on a recipe without supported product counting
- **THEN** the choice is rejected with a reason and the previous bill remains intact.

### Requirement: Keep operands and effective values distinct
The mod SHALL provide consistent whole-number editing in both the overview and bill details, including signed offsets, with a base plus/minus step of one rather than one recipe batch.
Ordinary keyboard adjustment multipliers remain available and explained. Values accepted in one view
are accepted in the other. Offset input range is minus 2,147,483,648 through 2,147,483,647; nonnegative
operands use zero through 2,147,483,647. Invalid text or out-of-range input cannot replace a valid
saved choice. Finite derived totals are shown as finite totals, never mislabeled Infinite or wrapped
to a different quantity; an unrepresentable result is unavailable and stops new work with a reason.

Switching modes retains the existing target and resume operands when valid for the destination,
showing the resulting units/target. A fresh ordinary bill's existing 10 and 5 are therefore retained,
not silently changed to zero or one. Switching a negative offset to a nonnegative mode requires an
explicit valid replacement or cancellation. Target adjustments move the stored resume operand by
the same delta, bounded by its input range; direct resume edits remain possible. Effective resume
clamping is displayed without silently rewriting the entered operand.

#### Scenario: Batch recipe and signed details
- **WHEN** a recipe makes four products per batch and the player decreases its offset from zero
- **THEN** the offset becomes minus one in overview and details, not minus four or zero.

#### Scenario: Explain a mode switch
- **WHEN** a fresh bill with target operand ten switches to Quantity per match for five matches
- **THEN** the preview shows ten per match and a target of fifty before the choice is committed.

#### Scenario: Negative operand cannot silently become a multiplier
- **WHEN** the player switches an offset of minus two to Quantity per match
- **THEN** a valid nonnegative operand is requested in the same interaction or the switch is canceled.

#### Scenario: Large finite value
- **WHEN** the derived target exceeds 999,999 but is representable
- **THEN** the actual finite target remains readable and governs production without an Infinite label.

### Requirement: Apply shared target and resume rules coherently
The mod SHALL start new adaptive production only while not suspended, below the effective target and not paused by Pause when satisfied.
With Pause when satisfied enabled, reaching the target pauses new starts. Transform the entered
resume operand using the same offset/multiplication rule, then clamp its effective value between
zero and one less than the positive target; zero target has zero resume and requests no work.
Paused production resumes when stock is at or below that effective resume level. Disabling Pause
when satisfied clears that pause; explicit Resume is available when stock is below the target.
Changing population alone does not clear a pause unless the new resume condition is met. Display and
work decisions use the same population, stock, target and pause state. Existing physical work and
ready output retain completion/collection under the shared-plan rules; simultaneous ordinary work
can overshoot. New queries do not reserve products or multiply a linked group's target by its benches.

#### Scenario: Multiplication and hysteresis
- **WHEN** five matches use target operand two, resume operand one and Pause when satisfied
- **THEN** stock ten pauses new starts, stock six stays paused and stock five allows resumption.

#### Scenario: Population increases while paused
- **WHEN** a paused bill's target rises but current stock remains above its recomputed resume level
- **THEN** it stays paused and displays why; explicit Resume can restart it while below the target.

#### Scenario: Zero population and completion
- **WHEN** a multiplier bill loses its last match after a batch has already started
- **THEN** no new batch starts, while already committed work and collection can finish once without fabricated output.

### Requirement: Begin with a precise bill-local population
The mod SHALL initialize a newly created adaptive query to spawned player colonists on the bill's map, excluding colony slaves and quest lodgers.
Babies, children and downed colonists count if they meet those conditions; animals, mechs, subhumans,
ordinary visitors, prisoners belonging to another faction and world caravans do not enter that default.
An arrested colonist still belonging to the player remains included while spawned; imprisonment alone
is not a default exclusion. No default age or skill test is implied. Each pawn contributes one.
Birth, death, arrival, departure, faction/status changes
and relevant query edits affect the next evaluation, including inspection while paused. Fog-hidden
results remain excluded from ordinary production queries even when developer or god mode changes.
The default is a bill choice, not a global setting or a permanent restriction on the editor.

#### Scenario: Children and visitors
- **WHEN** a map has four adult colonists, one child colonist, one baby colonist, one slave and one quest lodger
- **THEN** the untouched query counts six, provided those six are spawned and otherwise qualify.

#### Scenario: Pawn leaves the map
- **WHEN** a matching colonist leaves in a caravan or enters a container that removes it from the spawned population
- **THEN** it leaves the default matching count; the default does not silently become a world-population search.

#### Scenario: Developer toggle
- **WHEN** god mode is enabled while a bill's ordinary query is inspected
- **THEN** hidden things do not suddenly increase its target.

#### Scenario: Arrest a colonist
- **WHEN** a matching colonist is arrested on the map and remains a spawned member of the player faction
- **THEN** the default query still counts that colonist, unlike a prisoner of another faction.

### Requirement: Edit the matching rule through the bill
The mod SHALL provide a contextual Edit matches action, a named query and a preview of matching things and their total beside the adaptive bill controls.
The editor supports including/excluding conditions, individually disabling them, nested groups,
all conditions, any condition and at least N conditions. All with no active conditions accepts the
chosen listing; Any with no active conditions matches nothing. N is a positive whole number when
At least N is chosen. Unsupported or unresolved active conditions are errors, not disabled conditions.

Supported ordinary listings include spawned pawns, items, buildings, plants, natural objects, junk,
selectable things, all ordinary things and known contents. Choosing a non-pawn listing is allowed:
P is then matching physical units, including stack quantities, not a misleading count of people.
Overlapping listings/containers count the same physical unit once. A packaged item does not also
count its contents as a second representation of the same unit. The preview identifies the chosen
counting unit. This does not grant access to hidden contents or add debug/raw-object controls here.

The initial inspected condition set includes colonist/free-colonist/prisoner/slave/colony-mech,
quest-lodger, downed/dead status; biological or chronological age ranges; developmental stage;
skill level and passion (a selected skill or any qualifying skill); and trait with degree.
Finite range endpoints are inclusive; No lower limit and No upper limit are explicit choices,
not ordinary finite endpoints. New age conditions start at biological age up to fifty with no lower
limit; new skill conditions start with Animals, unrestricted level and passion ignored. New
developmental-stage conditions start at Adult; new trait conditions start at Pyromaniac and its
first available degree. These are provisional editor choices until Apply. Passion can be ignored,
absent, minor, major or any positive passion. Disabled skills cannot satisfy a skill condition; a pawn without
the relevant trait/skill/age property does not satisfy its positive condition. Exclusion inverts
that match. Each condition first accepts a matching outer thing or its represented inner item/pawn,
then applies inclusion/exclusion to that combined answer. A corpse can therefore match its dead
pawn's age, colonist status, skills or traits in a listing that includes corpses; a minified item can
match its contained item's properties. Keep one represented counting identity, not both wrapper and
contents. A corpse match is not a directly matched living equipment carrier. Further shared predicates
and reusable-library catalogue are specified under S121;
they are not silently substituted with approximate conditions here.

#### Scenario: Select adult crafters
- **WHEN** the player requires adult developmental stage and a chosen skill from ten through twenty
- **THEN** only pawns meeting both conditions enter the preview and target; ten is included and nine is not.

#### Scenario: Exclusion and nested alternatives
- **WHEN** a group accepts either one of two traits but the outer group excludes quest lodgers
- **THEN** a lodger with either trait still fails the outer group and a non-lodger with either qualifies.

#### Scenario: Count item units deliberately
- **WHEN** the player selects item matches containing stacks of twenty and ten, found through overlapping lists
- **THEN** the matching total is thirty units, displayed as units rather than thirty colonists.

#### Scenario: Empty and missing conditions differ
- **WHEN** a player intentionally removes all All conditions, or a saved active trait condition loses its trait
- **THEN** the first accepts the selected listing; the second is unresolved and cannot broaden the query silently.

#### Scenario: Dead colonist inside a corpse
- **WHEN** an All ordinary things query requires Dead and Colonist and a qualifying colonist's corpse is present
- **THEN** the corpse matches through its inner pawn and contributes one represented unit
- **AND** excluding Dead removes it; the default Spawned pawns listing does not include that corpse.

#### Scenario: Explicit open-ended range
- **WHEN** a supported pawn has a skill above twenty and the query selects at least ten with No upper limit
- **THEN** that skill can qualify; explicitly choosing a finite upper limit of twenty excludes it.

### Requirement: Make map scope stable and cancellation complete
The mod SHALL offer Bill map, explicitly selected loaded maps and All loaded maps, with the chosen scope visible in the query and bill summary.
Bill map follows the bill's actual map, never the camera's currently viewed map. Map-relative
conditions evaluate on their declared searched map; a condition requiring unavailable map context
is unresolved. Explicitly selected maps do not silently fall back when lost. All loaded maps follows
the loaded set and does not include off-map caravans. Duplicate map selections do not multiply results.
Preview edits are provisional: Apply commits the whole query, including name/listing/conditions/maps;
Cancel, Escape or outside dismissal discards all provisional changes. Production continues using the
last committed rule. Closing the editor restores the previous game speed when the editor paused it.

#### Scenario: Camera changes colonies
- **WHEN** a Bill map query belongs to colony A and the player views colony B
- **THEN** its population and target still use A.

#### Scenario: Cancel map and filter edits
- **WHEN** the player renames a query, adds another map and changes its filters, then cancels
- **THEN** all three choices revert together and the bill continues with its previous rule.

#### Scenario: Missing chosen map
- **WHEN** an explicitly chosen map is removed
- **THEN** the query retains the unresolved selection and blocks new affected production until explicitly repaired.

### Requirement: Compose matching-pawn equipment with ordinary stock rules
The mod SHALL use the adaptive query's directly matched pawns as the carrier selection for Include equipped, in place of the ordinary local-colonist carrier selection.
This deliberate bill choice can include a selected pawn on another searched map or of another faction;
show that consequence. Count its eligible inventory, equipment and apparel independently when those
holdings exist. Missing apparel or equipment does not discard its other eligible holdings. Non-pawn
matches affect P but do not become equipment carriers. Explicit query selection takes precedence over
the ordinary nonhuman carrier preference for this Include equipped addition.

All other base map/storage, optional local inventory and explicit Count when away paths retain the
production-stock-counting rules. The query does not silently restrict those independent additions.
Explain their scopes separately. Primary/additional products share condition, quality, material,
taint, freshness and unique-unit rules; an item found through multiple paths counts once. Custom
product counters only gain extensions whose semantics are actually supported. Surplus mode does not
acquire these product-target controls.

#### Scenario: Equipment for the selected group
- **WHEN** an adaptive apparel bill matches only two selected pawns and Include equipped is on
- **THEN** qualifying gear on those pawns contributes through Include equipped, while a third unselected pawn's worn gear does not.

#### Scenario: Animal and partial holdings
- **WHEN** the player deliberately matches an animal carrying inventory that cannot wear apparel
- **THEN** its eligible inventory can count through Include equipped despite the absent apparel holdings.

#### Scenario: Independent extra count paths
- **WHEN** an unselected colonist's inventory qualifies through the enabled local inventory preference
- **THEN** those units still count through that explained independent path, once even if another path also finds them.

### Requirement: Keep complete matching choices with the production plan
The mod SHALL save mode, operands, pause choices and complete committed query with each bill or shared logical row, preserving dormant choices when another repeat mode is selected.
S014 whole-list groups share one query, target and pause state per row, including all map/filter edits.
S017 copying captures a complete independent snapshot; later source edits do not change it. Paste
previews show mode/operands/query and maps. Bill map binds to the destination, while explicit map
selections retain their identities; missing references require resolution before the entire paste
commits. Physical work, selected pawn ownership and linked identity are not cloned. Cross-recipe
Paste choices requires meaningful target support and the existing transfer preview. Minification,
movement, reload, member separation and deletion follow shared-plan ownership without orphan queries
or cross-save clipboard leakage. Foreign source saves are not silently adopted as default queries.

#### Scenario: Linked edit
- **WHEN** a player applies a new matching rule through any member of a linked workbench group
- **THEN** every member shows the same rule, current matching quantity, target and pause state.

#### Scenario: Copy between maps
- **WHEN** a Bill map query is copied from A to B, and a separate explicitly A-scoped query is also copied
- **THEN** the first uses B and the second retains A, with both choices shown before paste commits.

#### Scenario: Preserve dormant choice
- **WHEN** a configured adaptive bill switches to Forever, is saved/reloaded and switches back
- **THEN** its matching conditions and valid operands remain available without duplicating physical work.

### Requirement: Exchange complete supported searches without weakening them
The mod SHALL allow copying and applying complete supported queries between its matching editor and its shared search workflows, with preview and explicit application.
An optional supported external search provider can offer the same exchange while the editor is open.
The external provider is not needed for ordinary adaptive production. Transfer copies the query;
it does not share mutable ownership. Imported map-relative scope is shown and explicitly bound to the
destination bill. Unknown conditions, unsupported listing/scoping semantics or missing references
reject the complete import with a reason and leave the previous rule intact. Do not keep only the
recognized subset. Closing an editor removes its receive action; an old destination is not reused
after a save switch. Reusable-library organization beyond this transfer belongs to S121.

#### Scenario: Supported transfer
- **WHEN** a complete supported search is sent to an open bill editor
- **THEN** its proposed rule and destination scope appear for review and affect production only after Apply.

#### Scenario: Unsupported clause
- **WHEN** an imported search contains one unsupported active condition
- **THEN** import is rejected as a whole; that condition is not silently dropped and the old bill rule survives.

### Requirement: Isolate unavailable adaptive behavior visibly
The mod SHALL distinguish a valid zero population from an unavailable query, product count or adaptive mode, and block new affected work while preserving saved choices.
A startup incompatibility produces a bounded warning identifying the skipped behavior and fallback.
Related controls and work decisions are admitted together; benign unrelated patches do not alone
disable support. Original EGO coexistence withholds our overlapping modes. Owned saved affected
plans remain visibly suspended and recoverable, never reinterpreted as an ordinary numeric target.
Returning support requires explicit Resume, or the player can deliberately replace the mode/query
after seeing the changed meaning. Ordinary unaffected bills continue normally. Completion/collection
of existing physical work remains available only through its safely supported native path.

#### Scenario: Query evaluation fails
- **WHEN** one searched map or required condition cannot be evaluated
- **THEN** the displayed result is unavailable rather than a partial population or zero, and no new affected batch starts.

#### Scenario: Failed startup on a saved plan
- **WHEN** the saved adaptive mode fails compatibility checks or the original provider is active
- **THEN** its choices remain visible, startup logs identify the withheld behavior and work waits for explicit recovery.

#### Scenario: Restore support
- **WHEN** valid support returns
- **THEN** the player can explicitly resume the retained rule without resetting actual stock or completed work.
