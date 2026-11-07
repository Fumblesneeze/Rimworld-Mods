# Ingredient preferences

Owner **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`; target **1.7 beta**.
Origin replacement S059/P1-53, Best Mix (Continued). Observable draft only; no implementation.
The corrections and composition choices are proposals retained under MX-Q1–4 for final review.

## ADDED Requirements

### Requirement: Workbench defaults and bill overrides are contextual choices

MX01. Each supported player production workbench SHALL have an ingredient preference, initially
**Default**, and each new production bill SHALL initially **Use workbench preference**. Unchecking
that choice SHALL restore a dormant bill override or seed its first override from the currently
effective workbench choice. Rechecking SHALL retain the dormant override while following later bench
changes. The view SHALL show both inheritance and the currently effective choice. These are saved
entity choices, not global mod settings. Pawn bills, surgery and unsupported bill-giver workflows
SHALL retain their ordinary selection.

Use a contextual workbench action and bill ingredient controls, with the same selector reachable
from the integrated bill list. No new bottom tab or separate ingredient-management screen is needed.
Canceling a menu SHALL change nothing. Menus SHALL distinguish **Default**, **Nearest** and
**Use workbench preference**; none is labeled ambiguously as doing nothing. The workbench menu SHALL
offer criteria meaningful to at least one of its available production recipes; the bill menu SHALL
identify applicability to its recipe. Retain a saved unavailable choice with its explanation.

#### Scenario: Bench change follows inheritance
- **WHEN** a bench changes from Default to Random while one bill inherits and another explicitly uses cheapest
- **THEN** the first bill uses Random for new work and the second retains cheapest.

#### Scenario: Restore a dormant bill choice
- **WHEN** a bill returns to inheritance and later unchecks it
- **THEN** its former explicit choice returns without changing the bench or another bill.

#### Scenario: Cancel and inspect
- **WHEN** the player opens and cancels either selector
- **THEN** no choice changes and the displayed effective preference still agrees with the saved state.

#### Scenario: Medical order
- **WHEN** a pawn's operation selects medicine or another required ingredient
- **THEN** this feature does not reorder that selection or supply a misleading active ingredient preference.

### Requirement: Criteria explain the ingredient property being compared

MX02. The feature SHALL retain the following choices with visible direction and units. They compare
the actual ingredient or its material property, not a forecast of finished-product quality or a
promise of combat effectiveness. Mass, market value and nutrition are **per item**, not stack totals.
Beauty uses the ingredient's current beauty without adding an extra material multiplier or offset.
Output-product material comparison remains the separate Use best by choice.

| Choice | Preferred ingredient |
| --- | --- |
| Default | Ordinary selection with the other enabled owned preferences; no extra ingredient criterion |
| Nearest | Shorter horizontal distance from the work location |
| Expiry / spoilage | Shorter time until spoilage at its current temperature, as specified below |
| Damaged / Robust | Lower / higher remaining hit-point fraction |
| Cheapest / Most expensive | Lower / higher current value per item |
| Warmest / Coldest | Higher / lower current ambient temperature |
| Random | A random order of eligible stacks for the current attempt |
| Least-filled stack | Lower item count divided by that item's maximum stack size |
| Stock most / Stock least | Higher / lower counted units of that item type under the stock options |
| Prettiest / Ugliest | Higher / lower ingredient beauty |
| Heaviest / Lightest | Higher / lower ingredient mass per item |
| Most flammable | Higher ingredient flammability |
| Blunt / Sharp / Heat protection | Higher corresponding material armor property |
| Heat / Cold insulation | Higher corresponding material insulation property |
| Sharp / Blunt damage | Higher corresponding material damage multiplier |
| Lowest / Highest nutrition | Lower / higher ingredient nutrition per item |
| Electric protection | Deferred with the source CE adapter; unavailable in initial support |
| Softness | Higher textile softness from a supported active provider |

Supported zero values SHALL remain real values. Items for which a criterion is inherently
inapplicable, such as no hit points for Damaged, SHALL rank after comparable items and tie with one
another under the remaining ordering. If every candidate is inherently inapplicable, retain that
remaining ordering and explain that the criterion makes no distinction. This differs from an active
choice whose required provider/property has disappeared, which follows MX12 recovery.

#### Scenario: Item value does not mean stack value
- **WHEN** cheapest compares ten items worth two each with one item worth five
- **THEN** the two-per-item ingredient ranks first without pretending the entire stack costs less.

#### Scenario: Stack fraction accounts for different limits
- **WHEN** eligible stacks contain 10 of 20 and 20 of 100 items
- **THEN** Least-filled stack prefers the second stack because 20% is below 50%.

#### Scenario: Ingredient beauty is not product beauty
- **WHEN** Prettiest ranks ingredients with different beauty and material modifiers
- **THEN** it compares their current ingredient beauty once and does not add a separate material factor.

#### Scenario: Other numeric directions
- **WHEN** the player selects either direction of value, temperature, mass or nutrition, or a listed protection/insulation/damage property
- **THEN** permitted candidates follow the direction and per-item/material meaning stated in the table, without changing the actual property or recipe output.

### Requirement: Condition and expiry use current physical circumstances

MX03. Damaged and Robust SHALL compare remaining hit points divided by maximum hit points, without
treating a no-hit-point item as destroyed. Expiry SHALL compare the game's current-temperature
prediction of time until rot begins. Already-rotting permitted ingredients with an active finite
prediction have no remaining time;
currently frozen, nonrotting or inactive-rotting ingredients with no finite prediction SHALL follow
all finite-expiry ingredients. This does not make rotten or otherwise forbidden food usable.
Temperature, damage and storage changes SHALL be reflected at the next fresh selection. A prediction
does not promise the temperature will stay unchanged during travel or work.

#### Scenario: Relative condition
- **WHEN** Damaged compares an item at 20 of 100 HP with one at 50 of 1000 HP
- **THEN** the 5%-remaining item ranks first; Robust prefers the 20%-remaining item.

#### Scenario: Frozen old food versus expiring warm food
- **WHEN** old frozen food has no current finite spoilage time and warm permitted food will spoil soon
- **THEN** Expiry prefers the warm food regardless of which has less intrinsic rot progress remaining.

#### Scenario: No hit points
- **WHEN** Damaged encounters a no-HP ingredient and a comparable damaged ingredient
- **THEN** the comparable item ranks first and the no-HP item is not described as completely damaged.

#### Scenario: Freezer warms up
- **WHEN** storage conditions change before a new iteration
- **THEN** that new selection uses the updated spoilage prediction and ambient temperature.

### Requirement: Stock totals are an information signal rather than permission

MX04. Stock most/least SHALL count physical units of each item type, combining its Stuff, quality and
condition variants. By default count spawned items strictly inside the bill's ingredient radius on
the same map and only in valid storage. The two stock options SHALL allow whole-map counting and
counting outside valid storage independently. Exclude forbidden, burning and fogged stock. Do not
count pawn inventories, off-map items or container-only contents absent from the spawned map stock.
Count each physical unit once, including when storages are linked.

Totals SHALL NOT claim that all counted stock is reachable, unreserved, allowed by this bill or
available to this worker. These permissions are checked separately when choosing actual ingredients.
Changing counting scope SHALL NOT enlarge pickup radius or allow otherwise inaccessible items.
An ingredient can remain eligible for consumption even when it is excluded from the counting scope;
its type's total can therefore be zero. Haul-source contents discovered by ordinary ingredient
search remain eligible under normal rules even though container-only units are absent from this
source-like stock statistic. Help SHALL explain this distinction. Stock changes affect the next
selection without requiring a save reload.

#### Scenario: More units rather than more stacks
- **WHEN** Stock most compares a type with two stacks of 40 against a type with five stacks of ten
- **THEN** the 80-unit type ranks ahead of the 50-unit type.

#### Scenario: Whole-map counting does not expand pickup
- **WHEN** whole-map counting gives one type a large total outside the ingredient radius
- **THEN** it influences that type's rank but only actually permitted in-range discovered supplies can be consumed.

#### Scenario: Counting storage is not a consumption filter
- **WHEN** stored-stock counting excludes a loose eligible candidate and no stored units of its type exist
- **THEN** its signal is zero and it can still be consumed if the recipe and current action permit it.

#### Scenario: Linked storage and physical transfer
- **WHEN** stock moves or splits between linked shelves, into a pawn inventory or into a container
- **THEN** the next stock signal follows the stated physical scope without double counting the old and new locations.

### Requirement: Preference preserves complete physical recipe requirements

MX05. Preferences SHALL order only supplies discovered by the ordinary ingredient search plus the
already specified enabled linked-storage addition. They SHALL NOT promise the best ingredient in
the entire radius or map, bypass a normal early success, unlock recipes or change filters, explicit
material locks, fixed ingredients, worker access, reservations or permitted containers.

Mixing recipes SHALL use their ordinary ingredient value and rounding rules while following the
selected preference among valid choices. Non-mixing recipes SHALL use a sufficient permitted type
for each requirement, combining its actual split stacks; an insufficient preferred type SHALL NOT
prevent using a sufficient lower-ranked type. Within a type, use its preferred eligible stacks.
Across requirements, a physical unit SHALL count at most once. Whole-stack recipes retain their
ordinary whole-stack meaning. If no complete permitted selection exists, wait normally; do not
invent ingredients, change required nutrition or substitute an unauthorized type.

An explicit ingredient preference can change resource overage caused by whole-item rounding. It
SHALL NOT alter recipe work, research, quality generation, ingredient effects or output quantities.
Actual chosen ingredients determine the normal output material and food provenance.

#### Scenario: Mixing preserves required nutrition
- **WHEN** a mixing recipe prefers a food with a different nutrition value per item
- **THEN** the ordinary rounded quantity still supplies the full required nutrition and output remains unchanged.

#### Scenario: Insufficient preferred no-mix type
- **WHEN** preferred material is short but another allowed material can satisfy the requirement
- **THEN** the lower-ranked sufficient material can be used without mixing otherwise nonmixable types.

#### Scenario: Overlapping ingredient requirements
- **WHEN** two ingredient requirements can use the same stack
- **THEN** selected quantities across both never exceed the actual available units; an incomplete selection waits.

#### Scenario: Better distant stock is not discovered
- **WHEN** the ordinary search succeeds before reaching a better stockpile outside any enabled linked-group addition
- **THEN** the discovered permitted choice can be used and the UI does not promise a map-wide optimum.

### Requirement: Compose ingredient and output preferences in a single order

MX06. For fresh selections, the feature SHALL use this precedence: applicable filters and material
lock; enabled Use best by for the output-material role; explicit ingredient preference; enabled
Common Sense spoilage among remaining ties; ordinary distance and discovery-order ties. Other
ingredient roles skip output-material comparison. Default contributes no ingredient criterion.
Explicit Nearest compares distance at its stated position in this order; Random supplies a random
order within the higher-priority constraints. A lower-priority preference SHALL NOT overturn an
earlier decision. Medical selection remains excluded.

When ingredient preferences are disabled, unavailable by meal-only scope, or set to Default, other
enabled owned preferences and ordinary native mixing/value rules SHALL remain. The eligible linked-
storage candidate addition specified by Common Sense SHALL remain available when enabled even if
an explicit ingredient criterion outranks spoilage. Apply each contribution once, without a later
independent sort replacing the result.

#### Scenario: Output material wins over cheaper ingredients
- **WHEN** Use best by prefers one sufficient output material and Cheapest would choose another
- **THEN** the output-material preference wins and Cheapest applies only to remaining choices and secondary ingredients.

#### Scenario: Explicit nearest versus spoilage
- **WHEN** eligible stacks tie on higher-priority rules and the bill explicitly chooses Nearest
- **THEN** the nearer stack wins even if a farther one spoils sooner; Default would retain the enabled ordinary spoilage preference.

#### Scenario: Explicit expiry differs from Common Sense
- **WHEN** Expiry and intrinsic spoilage-hours preference disagree because one ingredient is frozen
- **THEN** current-temperature Expiry decides first and intrinsic spoilage breaks only remaining ties.

#### Scenario: Genuine inactive path
- **WHEN** ingredient preferences are disabled or Default applies with no other explicit preference
- **THEN** native ingredient-value ordering for mixing remains, including any other enabled owned behavior, instead of being forcibly replaced with distance alone.

#### Scenario: Linked-group candidate addition survives
- **WHEN** the enabled linked-storage rule discovers more eligible supplies and the bill selects an explicit ingredient mode
- **THEN** those supplies participate under the combined order without widening radius or access.

### Requirement: Random is consistent within an attempt and conserves state

MX07. Random SHALL give each discovered eligible stack an equal opportunity in the randomized
ordering, subject to higher-priority rules and complete recipe requirements. It is stack-based,
not equal probability per species, material type, nutrition unit or individual item. A current
selection attempt SHALL retain a consistent random order as candidates are considered; opening
the menu or redrawing a bill SHALL NOT reroll it. Committed ingredient portions SHALL remain those
chosen through collection, route changes, cleanup and save/reload. A later genuinely new attempt
can choose differently. Randomness SHALL NOT consume unrelated gameplay random outcomes merely
because the player views the preference UI.

#### Scenario: Several stacks of one ingredient
- **WHEN** three eligible stacks of one food and one stack of another enter a random choice
- **THEN** each stack has equal opportunity, without promising equal probability for the two food types.

#### Scenario: Redraw and additional search candidates
- **WHEN** the UI redraws or more candidates enter the same selection attempt
- **THEN** already considered candidates retain a consistent relative random order and no invalid comparator reshuffle occurs.

#### Scenario: Save after collection starts
- **WHEN** a random iteration has committed real ingredient portions and is saved mid-collection
- **THEN** loading preserves that iteration and its portions rather than rerolling a different recipe input.

### Requirement: Copies links and edits preserve the full choice

MX08. Bill duplication and supported copy/paste SHALL preserve inheritance, explicit and dormant
choices independently. An inherited copied bill SHALL use its destination bench's current default;
the paste preview SHALL identify that effective result. An explicit copied choice SHALL remain
explicit. Unsupported active choices SHALL reject the complete paste before mutation under the
existing snapshot rules.

Linked workbenches SHALL share one ingredient default along with their shared logical bill list.
The existing link operation's chosen source supplies that default and all bill choices; the preview
SHALL disclose destination changes and reject an unsupported group atomically. Editing the default
through any member updates the group. Unlinking produces independent retained copies of the current
default and bill choices. These choices SHALL survive load, minification/reinstallation where the
native bench identity survives, and save switching without leaking between unrelated benches.

Edits apply to new iterations only. Started work SHALL retain its committed ingredient policy,
actual portions, material and quantities through additional trips and reload, while current physical
access and safety continue to apply. Deletion, cancellation or provider loss SHALL preserve actual
cargo and work through the existing recovery path, not create another iteration or erase supplies.

#### Scenario: Copy inheritance to a different bench
- **WHEN** an inheriting bill is pasted to a bench with a different default
- **THEN** the preview shows that destination default and the copied bill remains inheriting rather than silently copying the source's effective mode as an override.

#### Scenario: Linked default and independent unlink
- **WHEN** a linked group's default changes and a member is then unlinked
- **THEN** all linked members received the change and the unlinked member retains an independent copy thereafter.

#### Scenario: Preference edited between pickup trips
- **WHEN** a player switches a started bill from Cheapest to Random before its next ingredient trip
- **THEN** existing work keeps its committed portions/material and new work uses Random without extra quota or duplicated supplies.

#### Scenario: Unsupported destination
- **WHEN** a copy or link cannot represent an active choice on every affected destination
- **THEN** it explains the incompatibility and changes no list, default, bill choice or physical work.

### Requirement: Configuration uses shared global and save controls

MX09. Configuration SHALL use the common mod-settings loading, storage and rendering path, with
these built-in defaults in a clearly ordered Ingredient preferences section:

| Configuration | Default |
| --- | --- |
| Enable ingredient preferences | On |
| Limit preferences to meal-producing workbenches | Off |
| Count stock across the whole map | Off |
| Count only stock in valid storage | On |
| Extend production bill capacity to 125 | On |

Each row SHALL show Global default, Current save and Use global. New/missing save values inherit
the current global value. Use global disables the save editor and retains its dormant override;
unchecking restores it or seeds a first override. At the main menu, both save editor and Use global
are disabled, including mutation actions; globals remain editable. Changes apply to new selections
or add/paste actions and the next UI refresh, preserving started work and existing bill rows.

Meal-only scope uses the game's meal-source classification, including supported modded benches,
and does not delete choices on excluded benches. Turning preferences off preserves stored entity
choices and other owned selection behavior. The capacity option is independent of ingredient-mode
enablement. Dead source radius/debug/position controls are not exposed as working features.

#### Scenario: Global change and saved override
- **WHEN** one save inherits whole-map stock counting and another overrides it
- **THEN** changing the global default updates the inherited save's future choices and leaves the overridden save unchanged.

#### Scenario: Main menu is not an implicit save
- **WHEN** settings open without an active game
- **THEN** only global values can change and no previous save's settings are mutated.

#### Scenario: Meal-only restriction
- **WHEN** meal-only scope is enabled with a configured stove and smithy
- **THEN** the stove can use preferences, the smithy retains its dormant choices with ordinary remaining selection, and its configuration explains the exclusion.

### Requirement: The production bill limit is coherent and non-destructive

MX10. With the capacity extension enabled and supported, production workbench lists SHALL allow
up to **125** bills through every supported Add, quick-add, duplicate, paste and linked-list path.
With it disabled, the ordinary **15**-bill limit SHALL apply unless another explicitly supported
capacity provider governs those paths. This feature SHALL NOT change medical/pawn-bill limits.
The limit controls additions, not how many existing bills can be seen, reordered, edited, suspended,
executed or deleted. Lowering a limit SHALL never delete rows or cancel work.

An operation adding multiple rows SHALL validate the complete resulting list and add all or none.
At or above the active limit, explain why additions are unavailable; do not trap access to existing
rows. Incompatible external capacity providers SHALL require one admitted common policy rather than
silently applying different limits in different views.

#### Scenario: Add beyond ordinary fifteen
- **WHEN** the extension is enabled and a supported bench already has 15 production bills
- **THEN** a further permitted bill can be added through the native or owned Add workflow.

#### Scenario: Whole paste would exceed capacity
- **WHEN** a bench with 124 bills receives a two-bill paste under the 125-bill limit
- **THEN** neither bill is added and the capacity explanation identifies the problem.

#### Scenario: Lower the limit on an existing list
- **WHEN** the extension is disabled for a bench already holding 80 bills
- **THEN** all 80 remain usable and additions are blocked until allowed by the current limit, without truncation or canceled work.

### Requirement: Supported external controls use the same choice

MX11. A supported external bill button SHALL show the current bill's ingredient preference and open
the same contextual selector as owned controls. Reading its icon SHALL NOT mutate choices or roll
random selection. Unsupported/null/nonproduction targets SHALL safely yield no enhancement. The
proven Nice Bill Tab Expansion button interaction SHALL remain callable without impersonating the
source package. This narrow compatibility does not promise the source's other public types, saves
or startup behavior. Owned recipe-list controls SHALL call the shared behavior directly.

Initial support is single-player without Combat Extended. Multiplayer synchronization and CE-only
properties SHALL remain outside initial implementation and acceptance; source adapter research
SHALL NOT be represented as owned compatibility.

#### Scenario: External icon and click
- **WHEN** the supported external production-bill UI reads the icon and then the player clicks it
- **THEN** reading changes nothing and clicking opens the same owned selector with the same inherited/explicit choice.

#### Scenario: Unsupported external target
- **WHEN** the external view requests a null, surgery or unsupported bill target
- **THEN** the enhancement safely supplies no actionable button without breaking the surrounding view.

### Requirement: Unsupported preferences preserve intent and allow explicit recovery

MX12. An active saved choice whose provider/property or selection integration disappears SHALL
remain stored and visibly unavailable. New affected iterations SHALL wait rather than silently
consume different ingredients. The player SHALL retain an independently safe route to choose
Default, restore inheritance to a supported default, or disable preferences. Bills without affected
active choices SHALL continue ordinary remaining work; committed work follows existing safe recovery.

Each enabled integration SHALL check its documented requirements at startup; failed requirements
leave the affected integration inactive with one bounded warning identifying the skipped behavior,
reason and fallback. Benign unrelated changes and compatible other mods SHALL NOT alone disable it.
Original Best Mix or its continuation active SHALL suppress owned overlapping modes/capacity with
an explicit source-control indication, retaining owned choices and holding incompatible active new
work until explicitly recovered. Do not remove/reconfigure the external provider or imply it reads
owned state. The narrow external button facade SHALL delegate to the admitted original control when
source ownership applies, or safely decline with an explanation if that delegation is unavailable.

No automatic import of source settings/string records, source package spoof or general safe-removal
guarantee is selected. A failed capacity enhancement SHALL preserve all existing rows and explain
the remaining limit. Optional stat absence before any choice is normal, not an error spam source.

#### Scenario: Selected optional stat disappears
- **WHEN** an active Softness choice loses its supporting property
- **THEN** it remains recorded as unavailable, new affected work waits, and explicitly choosing Default permits ordinary work without rewriting existing items or other bills.

#### Scenario: Selection patch cannot be admitted
- **WHEN** the game's ingredient-selection behavior no longer satisfies the required contract
- **THEN** startup logs explain the skipped behavior, active affected new work waits safely, and unaffected/default work can continue.

#### Scenario: Source is co-installed
- **WHEN** original or continued Best Mix is active
- **THEN** owned overlap stays inactive, source ownership is visible, owned state remains intact and supported external buttons use the admitted source action without a second owned selection pass.

#### Scenario: Capacity or optional property alone fails
- **WHEN** only one independent enhancement is unsupported
- **THEN** supported remaining choices continue, existing bills remain intact and the unavailable enhancement is explained without repeated per-frame logging.
