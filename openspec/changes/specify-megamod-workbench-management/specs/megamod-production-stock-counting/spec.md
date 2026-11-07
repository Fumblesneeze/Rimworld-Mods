# Production stock counting

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`). Target: 1.7 beta.
Origin: replacement. Specification only; no implementation or live acceptance.

## ADDED Requirements

### Requirement: Count additional product types toward the same target
The mod SHALL offer Count additional items and Count when away for target-count production recipes with one ordinary countable product.
The original product always counts; selected additional types contribute individual units to the same
target without becoming recipe outputs or alternative ingredients. The contextual item filter uses
available item categories/types with search; it does not change global settings. No extra types and
Count when away off are the built-in choices. Selecting the primary type again cannot double its
contribution. Confirming an empty additional selection clears it; canceling preserves the old selection.
Uncountable, multi-output, special-output and unsupported custom counting recipes keep their own controls
and do not acquire guessed target behavior. Switching repeat mode preserves dormant extra choices.
The population-production capability extends these controls to its admitted adaptive target modes
with the same output eligibility. Its ingredient-surplus mode is a different trigger and does not
gain product-target controls.

#### Scenario: Different meals satisfy one goal
- **WHEN** a simple-meal bill also counts fine meals and has five qualifying simple meals plus seven fine meals
- **THEN** it displays twelve toward its target but still produces only its ordinary simple-meal recipe.

#### Scenario: Select primary again or cancel
- **WHEN** the additional filter includes the primary product again, or the player cancels an edited filter
- **THEN** the primary stock is counted once and canceling leaves the previous selection unchanged.

#### Scenario: Unsupported output and dormant choices
- **WHEN** a recipe has no supported single output, or a supported bill switches from target mode to Forever and back
- **THEN** the unsupported recipe receives no invented count controls and the supported bill retains its previous extra choices.

### Requirement: Count actual qualifying units once
The mod SHALL count each actual qualifying product unit at most once, irrespective of overlapping categories, containers, carrier lists, linked members or primary/additional selections.
Count stack units, not one per stack; count minified contents by their real multiplicity, never both
the package and its contents. Apply the bill's condition, quality, tainted-apparel and allowed-material
restrictions consistently to every applicable type/location. A non-material item does not satisfy a
material-only restriction. Fogged stock is excluded. Resource products that are no longer fresh do not
count; this rule is consistent across storage, loose stock and carriers. Exclude destroyed/consumed items,
unfinished future products and predicted or reserved output. Installed products count only where the
ordinary product supports counting them; no new Count installed toggle is introduced.

#### Scenario: Stacks and duplicate paths
- **WHEN** thirty matching items are split into stacks of twenty and ten and both are found through overlapping eligible paths
- **THEN** the target count is thirty, not two or sixty.

#### Scenario: Quality and material apply to additional goods
- **WHEN** additional apparel contains low-quality, tainted or wrong-material items excluded by the bill
- **THEN** those items do not satisfy the target merely because their type was selected.

#### Scenario: Packaging, freshness and fog
- **WHEN** a qualifying item is packaged, or resources become rotten or hidden by fog
- **THEN** packaging does not change the number of represented units and disqualified resources cease to count.

### Requirement: Storage scope and explicit carrier additions are distinguishable
The mod SHALL use either the selected storage entity's actual contents or the current map's eligible stock as the base counting scope, with the same rule for primary and additional products.
A selected linked storage group includes all its shelves and stockpiles once. Loose stock and unrelated
storage do not join that group total. On map scope, matching stock in ordinary storage and supported
haul-source containers counts; loose non-resource products retain ordinary eligibility. The preference
Count loose resources has built-in default on; off excludes loose resource products consistently even
when condition/quality filters are changed. This preference does not remove eligible non-resource goods.

On map scope, directly carried products of spawned player-faction pawns count once, including animals
and mechs, as native 1.7 already supports. Count inventories, built-in off, additionally includes local
free-colonist inventories. Include equipped is a per-bill choice retaining native inclusion of local
free-colonist worn gear, equipment and inventories, even with a selected storage scope. Count nonhuman
inventory and equipment, built-in on, extends those enabled inventory/equipped additions to colony
animals, colony mechs and player-controlled colony subhumans. Turning it off does not remove native
direct-carry counts. Inventory preference alone does not broaden a selected storage scope. Direct
carry outside selected storage is excluded there unless it qualifies through Count when away.
Clearly explain carrier/away additions beside the storage scope so a displayed total above its physical
contents is understandable. Enemies and visitors do not count as ordinary colony carriers.
For adaptive targets, population-production explicitly replaces only the Include equipped carrier
selection with that bill's directly matched pawns, including deliberately selected remote or
non-colony pawns. Its explicit selection overrides the nonhuman preference for that addition.
Other base/local-inventory/away paths stay independent; all contributions still deduplicate physical
units. This is direct composed behavior, not a second equipped-count layer.

#### Scenario: Linked warehouse plus equipped gear
- **WHEN** a selected linked storage group holds ten qualifying items and Include equipped adds two on a colonist
- **THEN** the displayed total is twelve with the extra inclusion explained, not just the ten warehouse items.

#### Scenario: Local hauling without inventory option
- **WHEN** a player animal directly carries five matching products on map scope and inventory counting is off
- **THEN** those five still count once through native direct carrying
- **AND** disabling nonhuman inventory/equipment does not remove them.

#### Scenario: Inventory and selected storage
- **WHEN** inventory counting is on, Include equipped is off and a bill counts a particular warehouse
- **THEN** local pawn inventories outside that warehouse do not silently enter its total.

#### Scenario: Consistent loose-resource preference
- **WHEN** loose-resource counting is off and the bill's condition filter changes
- **THEN** loose resources remain excluded, while eligible non-resource goods retain their ordinary scope behavior.

### Requirement: Away stock belongs to its recorded home colony
The mod SHALL let Count when away add matching inventory and directly carried goods of living player-faction pawns associated with the bill's home map while away or temporarily unavailable there.
Record association when a pawn leaves a player home, including entry into an unavailable container;
include supported caravans, transporters and temporary other-map visits. Worn/equipped goods join only
when Include equipped is enabled. This explicit per-bill away choice includes eligible animals/mechs
regardless of the local nonhuman inventory preference and applies in addition to a selected storage
scope. Arrival as an available pawn at any player home clears the away association; a later departure
records that home. Merely visiting a non-home map does not reassign ownership. Pawns with no recorded
home, dead pawns and pawns no longer belonging to the player do not contribute. A lost origin map is
not replaced with the currently viewed map. Goods remain physical and count once during transitions.

#### Scenario: Caravan stock and another home
- **WHEN** a caravan leaves home A carrying eight matching products, visits a non-home map and then arrives at home B
- **THEN** A can count the eight while away if enabled; the temporary visit does not change their origin
- **AND** arrival at B ends A's away contribution and subsequent departure uses B as the new origin.

#### Scenario: Unavailable home pawn
- **WHEN** a living colony pawn enters a supported cryptosleep container on its home map carrying qualifying goods
- **THEN** Count when away includes those goods once until the pawn becomes available again
- **AND** local counting after release does not overlap a lingering away contribution.

#### Scenario: Death or faction change
- **WHEN** an away carrier dies, leaves the player faction or has no resolvable origin map
- **THEN** its belongings cease contributing through this away policy without being moved or deleted.

### Requirement: Counting drives production and display coherently
The mod SHALL use the same current total for the visible target progress and the decision to start, pause or resume production.
Relevant movement, stack changes, inventory transfers, consumption, destruction, quality/condition,
scope and preference edits are reflected before the next displayed evaluation or production decision,
including while paused. Returning from away or transferring between stock and inventory never counts
goods twice. Linked workbenches share one total and hysteresis; do not sum it per bench. Existing work
can finish and ordinary target overshoot remains possible. Counted-away goods can intentionally prevent
local replacement, even while unavailable for local use; explain that effect. Counting never creates
supplies, reserves goods or changes recipe costs, yields, skill requirements or research.

#### Scenario: Move and consume while inspecting
- **WHEN** stock moves from a shelf into an enabled inventory and is later consumed
- **THEN** its first move leaves a single contribution and consumption reduces the next visible/decision count.

#### Scenario: Shared target reaches threshold
- **WHEN** additional or away goods bring a linked bill to its target
- **THEN** all members show the same total and stop new work under the shared pause rule
- **AND** already started physical work retains its ordinary outcome and possible overshoot.

### Requirement: Counting choices persist with clear scope and recovery
The mod SHALL save additional types and Count when away with each production bill or shared logical row, and resolve the three counting preferences through the common global/save settings controls.
Defaults are loose resources on, inventories off, and nonhuman inventory/equipment on. Preference
changes affect the next count evaluation, never mutate physical goods, and obey inheritance, dormant
override, main-menu and save-isolation rules. Bill choices are copied and shared as part of their
production plan. Missing additional types or away associations remain visible as recovery information;
missing types block new affected target work until explicitly removed/replaced rather than silently
increasing production. Loss of a selected storage scope follows whole-list explicit recovery for linked
bills and native invalid-scope handling for ordinary unlinked bills, with the resulting scope visible.

#### Scenario: Save and reload extra products
- **WHEN** a bill counting two extra product types and away stock is saved and reloaded
- **THEN** its choices and valid home associations return without duplicated goods or changed global preferences.

#### Scenario: Removed product type
- **WHEN** a previously selected additional product type is no longer available
- **THEN** the bill retains an unresolved entry and blocks new affected target production until the player resolves it.

### Requirement: Unsafe counting does not silently reinterpret an owned target
The mod SHALL withhold unsafe counting extensions with a bounded startup warning, retaining saved choices and showing when a total is unavailable rather than presenting it as a trustworthy zero.
Affected owned target bills cannot start new work until counting is restored and explicitly resumed,
or the player explicitly clears the unsupported choices and accepts the native fallback. Ordinary
bills without affected owned choices retain native counting. Benign unrelated changes alone do not
reject support. Unsupported custom counters/providers keep their own behavior without guessed additions.
Original-source coexistence withholds our overlapping extension and does not silently transfer its
settings or saved data. Final support claims require actual observed production and scope workflows.

#### Scenario: Failed extension on saved target
- **WHEN** a saved bill relies on additional products but its required counting support fails admission
- **THEN** its total is identified as unavailable, new work is stopped and its choices remain recoverable
- **AND** it does not start manufacturing replacements because the unknown total was treated as zero.

#### Scenario: Restore or deliberately fall back
- **WHEN** valid support returns or the player explicitly clears the unsupported choices
- **THEN** the player can resume with the explained restored/native counting policy without resetting real goods or work.
