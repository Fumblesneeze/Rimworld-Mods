# Contextual storage presets

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
Origin: replacement, S050 Simple Stockpile Presets (Continued), with broader contextual UI.
Specification only; no gameplay implementation or acceptance.

## ADDED Requirements

### Requirement: SP-01 One compact selector serves the current storage context

The mod SHALL provide one **Presets** selector in the selected storage's normal configuration
surface. It SHALL support ground stockpiles, shelves, S005 linked storage groups and other storage
whose editable filter and intrinsic restrictions are supported. The selector SHALL identify the
individual storage or group it will affect. It SHALL not add one gizmo per preset, a new bottom tab
or storage-specific rows in mod settings. Opening or dismissing the menu SHALL not change any
filter. The selector and preset explanations SHALL use all seven baseline languages.

#### Scenario: Configure a shelf
- **WHEN** the player selects an ordinary shelf and opens its storage configuration
- **THEN** one Presets selector offers the applicable shortcuts without requiring a ground stockpile.

#### Scenario: Dismiss the selector
- **WHEN** the player opens Presets and closes it without choosing
- **THEN** the storage's complete policy remains unchanged.

#### Scenario: Unsupported container
- **WHEN** an object's editable storage policy cannot be safely identified
- **THEN** it has no enabled owned preset action and retains its ordinary provider controls.

### Requirement: SP-02 Offer only presets the storage can meaningfully use

The mod SHALL show a preset only when at least one item type it permits can be accepted by the
storage's intrinsic restrictions in a state permitted by that preset. For linked mixed storage,
acceptance by any current member SHALL make the choice eligible, while actual storage remains
member-specific. Availability SHALL ignore the storage's current editable allow-list, current
physical occupancy and whether the colony currently owns a matching item. A partly applicable
preset SHALL apply only its usable portion and explain that restriction. If none qualify, the
selector SHALL show an explanatory empty state rather than apply an empty preset. Unknown required
acceptance semantics SHALL not be guessed as compatible.

#### Scenario: Wood-only storage
- **WHEN** intrinsic restrictions permit only woody material
- **THEN** a meals-only choice is hidden, and a broader preset appears only if it contains something that storage can actually accept.

#### Scenario: Currently configured for weapons
- **WHEN** a general shelf currently permits only weapons but intrinsically also accepts meals
- **THEN** Meals remains available even when no meals currently exist on the map.

#### Scenario: Mixed shelf and ground group
- **WHEN** a linked group contains a shelf and a ground zone that can accept chunks
- **THEN** Chunks is available for the group, but the shelf does not gain permission to hold chunks it intrinsically rejects.

#### Scenario: Preset's only possible state is forbidden
- **WHEN** a storage intrinsically accepts only clean apparel and rejects all tainted apparel
- **THEN** Tainted apparel is hidden even though the storage accepts apparel item types.

### Requirement: SP-03 Retain the full built-in catalog with explicit contents

The mod SHALL provide the following fifteen presets in this stable order when applicable. Named
categories SHALL include their currently available descendants, including supported modded items;
named exclusions SHALL take precedence if a type belongs to overlapping categories. The contents
describe item-type selection; SP-04 governs retained ranges and unnamed special filters. Each choice
SHALL explain its contents and exceptions before application. Outside-storage naming SHALL not
promise immunity to weather for modded category members.

| Preset | Allowed item types | Named state choices and exclusions |
| --- | --- | --- |
| General freezer | Foods; ambrosia, beer, wort, herbal medicine; plant matter; humanlike and animal corpses | Exclude fertilized eggs; allow fresh and disallow rotten |
| Freezer (no meals) | General freezer's item types | Also exclude prepared-meal category; allow fresh and disallow rotten |
| Meals | Prepared-meal category | Allow fresh and disallow rotten |
| General storage | Manufactured goods; raw resources; fertilized eggs; items; weapons; apparel; buildings that can be stored; mechanoid corpses | Exclude ambrosia, beer, wort and plant matter |
| General medical | Medicine, drugs, body parts and neutroamine | Exclude ambrosia and beer |
| Drugs | Drugs and neutroamine | Exclude ambrosia and beer |
| Materials (outside) | Stone blocks, plasteel, silver, steel and uranium | No additional named state change |
| Materials | Raw resources | Exclude plant matter |
| Various others | Manufactured goods; items; weapons; apparel; buildings that can be stored; mechanoid corpses | Exclude medicine, drugs, neutroamine, wort and body parts; allow clean and disallow tainted apparel |
| Shells | Mortar-shell category | No additional named state change |
| Weapons | Weapons | No additional named state change |
| Clean apparel | Apparel | Allow clean and disallow tainted apparel |
| Tainted apparel | Apparel | Allow tainted and disallow clean apparel |
| Chunks | Chunks | No additional named state change |
| Rotting | Apparel; humanlike and animal corpses | Allow rotten and disallow fresh; allow tainted and disallow clean apparel |

Fresh/rotten and clean/tainted SHALL use the game's normal classifications. Rotting therefore
includes eligible decayed/desiccated corpses and tainted clothing; it is not an instruction to spoil
items. Materials SHALL not mean every construction ingredient, Drugs SHALL not mean medicine,
and Shells SHALL not mean every kind of ammunition. There SHALL be no custom-preset authoring,
import or export UI in this source replacement.

#### Scenario: General freezer protects eggs
- **WHEN** General freezer is applied to an unrestricted stockpile
- **THEN** its listed food, plant, medicine and corpse types are selected, fertilized eggs are excluded and rotten items are disallowed.

#### Scenario: Freezer excludes prepared meals
- **WHEN** Freezer (no meals) is applied
- **THEN** the General freezer selection remains except that prepared meals are excluded too.

#### Scenario: Meals selects only the meal category
- **WHEN** Meals is applied
- **THEN** prepared meals are selected while raw meat and unrelated food categories are not, with fresh allowed and rotten disallowed.

#### Scenario: General storage separates freezer goods
- **WHEN** General storage is applied
- **THEN** its listed broad categories and fertilized eggs are selected, with ambrosia, beer, wort and plant matter excluded.

#### Scenario: Medical storage includes treatment supplies
- **WHEN** General medical is applied
- **THEN** medicine, drugs, body parts and neutroamine are selected except ambrosia and beer.

#### Scenario: Drugs is narrower than medical
- **WHEN** Drugs is applied
- **THEN** drug-category items and neutroamine are selected except ambrosia and beer, without adding medicine or body parts merely for medical use.

#### Scenario: Outside materials has a precise list
- **WHEN** Materials (outside) is applied
- **THEN** stone-block types and the four named metals are selected; wood and other raw resources are not added just because they are construction materials.

#### Scenario: Materials follows raw-resource categories
- **WHEN** Materials is applied
- **THEN** current raw-resource types are selected except plant matter, rather than every recipe ingredient in the game.

#### Scenario: Various others keeps medical goods separate
- **WHEN** Various others is applied
- **THEN** its broad listed categories remain with medicine, drugs, neutroamine, wort and body parts excluded, and tainted apparel disallowed.

#### Scenario: Shells includes a modded mortar shell
- **WHEN** Shells is applied with a supported new item in the mortar-shell category
- **THEN** that item is selected alongside existing mortar shells, without also selecting unrelated ammunition.

#### Scenario: Weapons replaces unrelated item choices
- **WHEN** Weapons is applied to storage previously allowing meals
- **THEN** weapons replace the meal selection and ordinary weapon filter restrictions remain subject to SP-04.

#### Scenario: Clean apparel reverses a tainted choice
- **WHEN** Clean apparel is applied after Tainted apparel
- **THEN** apparel remains selected, clean apparel is allowed and tainted apparel is disallowed.

#### Scenario: Tainted apparel reverses a clean choice
- **WHEN** Tainted apparel is applied after Clean apparel
- **THEN** apparel remains selected, tainted apparel is allowed and clean apparel is disallowed.

#### Scenario: Chunks is not stone blocks
- **WHEN** Chunks is applied
- **THEN** chunk-category items are selected without selecting processed stone blocks merely because they are stone.

#### Scenario: Rotting includes tainted clothing
- **WHEN** Rotting is applied
- **THEN** its apparel and humanlike/animal corpse selection excludes fresh items and clean apparel, while permitting the normal rotten and tainted classifications.

### Requirement: SP-04 Replace the intended filter choices and preserve the rest

Choosing a preset SHALL intentionally replace the editable item-type allow-list with that preset's
applicable contents, rather than union it with the old selection. Only special-filter choices named
by the preset SHALL be changed; unnamed configurable and nonconfigurable restrictions SHALL remain
unchanged. A named allow choice SHALL not override an intrinsic provider restriction. Quality,
condition, mental-break-risk ranges, priority, names, linking, physical capacity and S049 refill
quantities/stack controls SHALL remain unchanged. The menu SHALL state that item choices will be
replaced and that other restrictions remain. It SHALL explain retained restrictions that prevent
otherwise selected items from being accepted; applying a preset SHALL not silently reset them.

#### Scenario: Quality and condition survive
- **WHEN** storage is Normal-or-better, at least 50% condition and Critical priority, and the player chooses Weapons
- **THEN** the weapon item selection changes while all three restrictions retain their prior values.

#### Scenario: An unnamed special filter survives
- **WHEN** a storage disallows smeltable weapons and Weapons is applied
- **THEN** that restriction remains and the UI explains that the preset did not reset it.

#### Scenario: A retained restriction blocks the chosen contents
- **WHEN** the storage disallows both fresh and rotten items and a preset without a named freshness change selects a rottable item type
- **THEN** the freshness choices remain, the limitation is explained, and the preset does not silently enable either state.

#### Scenario: Refill choices survive a preset
- **WHEN** storage has configured X/Y quantities, a finite stack-fill choice and a stack-count limit
- **THEN** applying a filter preset preserves those choices and only changes which actual items qualify under the resulting filter.

### Requirement: SP-05 Apply a validated choice to the intended storage atomically

Before applying a choice, the mod SHALL recheck the intended storage, its current group and the
preset's current applicability. If selection switches to another policy scope, the original scope
is deleted, or group membership changes while the menu is open, the stale action SHALL leave all
policies unchanged and explain that the menu must be reopened. Other intervening policy edits
SHALL be preserved for fields outside the preset's stated scope. A successful application SHALL
publish the complete resulting filter at once and verify that the selected scope has that result.
Validation or application failure SHALL restore the prior complete filter, preserve unrelated
policy and report failure without leaving an empty or partially applied preset. Cancellation SHALL
never apply. Reapplying the same preset SHALL not duplicate choices or alter unrelated state.

#### Scenario: Selection changes while the menu is open
- **WHEN** the player opens Presets for A, selects unrelated storage B and then activates an old choice
- **THEN** neither A nor B changes and the old action requests a fresh menu.

#### Scenario: A group changes before application
- **WHEN** a member leaves or joins the target group after the menu was opened
- **THEN** the stale choice does not unexpectedly change the new membership's policy.

#### Scenario: Failure after preparation
- **WHEN** the preset cannot be applied completely
- **THEN** the previous filter remains or is restored, unrelated choices remain intact, and the player sees a failure explanation.

#### Scenario: Unrelated policy edits are newer than the menu
- **WHEN** priority or quality is edited after opening the preset menu but before its valid choice is applied
- **THEN** the latest priority and quality survive the item-choice replacement.

### Requirement: SP-06 Linked storage and native persistence share one result

Applying a preset through any S005 linked member or the selected group SHALL edit the effective
shared filter once and expose the same result through all members. Each member SHALL retain its
own intrinsic acceptance and physical properties. Unrelated groups SHALL remain unchanged. Native
manual edits and complete-policy copy/paste SHALL remain available afterward. The applied filter
SHALL persist through native save/reload, rename, link/unlink and copy/paste under those operations'
existing contracts; it SHALL not require a remembered preset name or continuous reapplication.
Later mod/catalog changes SHALL not silently reapply a preset to an already edited storage.

#### Scenario: Choose through a linked shelf
- **WHEN** Meals is chosen from one member of a mixed shelf/stockpile group
- **THEN** every member shows the shared meal selection, while another independent group is unchanged.

#### Scenario: Manually refine then reload
- **WHEN** the player applies Weapons, manually removes one weapon type, saves and reloads
- **THEN** that manual exclusion remains and Weapons is not automatically applied again.

#### Scenario: Copy and unlink after using a preset
- **WHEN** the effective policy is copied or a member is unlinked
- **THEN** the ordinary full-policy copy/unlink rules apply, without making the preset a new linkage or overriding S049's lifecycle rules.

### Requirement: SP-07 Presets change selection policy rather than physical stock or work rules

The mod SHALL leave storage creation, zone painting/expansion, ordinary manual filter editing and
native loading usable. Presets SHALL not teleport, delete, spoil, repair or otherwise mutate stored
items, change the category definitions themselves or enlarge storage. After successful application,
ordinary hauling SHALL observe the new policy with its existing priorities, access and reservations.
S049 SHALL recompute qualifying stock without resetting its preserved X/Y choices or overwriting
its cycle rules. Bill-output validation SHALL observe the final filter using S005/S014's existing
distinction between ordinary native fallback and whole-list linked-bill unresolved-scope recovery.

#### Scenario: Place a new stockpile before using presets
- **WHEN** the player paints a normal new stockpile and later opens its configuration
- **THEN** placement completes normally and presets are a separate optional action on that storage.

#### Scenario: A hauler responds to the changed filter
- **WHEN** Meals replaces Weapons on a valid high-priority shelf with reachable supplies
- **THEN** ordinary hauling can bring eligible meals and existing weapons follow ordinary changed-filter handling without teleportation or deletion.

#### Scenario: A linked workbench loses its permitted output destination
- **WHEN** a preset makes that workbench's selected storage unsuitable for its output
- **THEN** the linked row retains the unresolved choice and stops new starts until resolved, while ordinary unlinked bills follow their existing native handling.

### Requirement: SP-08 Missing content and incompatible hooks fail coherently

The mod SHALL admit each required UI/edit integration and catalog meaning before enabling it.
Known absent optional items or categories SHALL contribute no contents; presets with no usable
contents SHALL be hidden. If a missing or changed special-filter behavior prevents a preset's
advertised meaning, that preset SHALL be unavailable rather than silently broadened. Unknown
required content/ownership semantics SHALL fail the affected context closed. Compatible unrelated
patches and valid category additions SHALL remain allowed. A skipped enabled patch SHALL log one
bounded startup warning describing the failure, unavailable behavior and fallback; unavailable
catalog meaning SHALL also have a bounded diagnostic and visible explanation where relevant.

When original `Mlie.SimpleStockpilePresets` is active, the owned preset integration SHALL be
inactive as a whole to avoid presenting competing preset semantics. Original behavior remains
foreign-owned; the mod SHALL not remove its patches or promise to repair it. Ordinary native filter
editing SHALL remain available when owned preset integration is inactive. No source-specific save
migration, custom preset import or package/assembly identity replacement SHALL occur.

#### Scenario: An included item was removed by content policy
- **WHEN** one optional item in a broad preset is absent but the remaining recipe meaning is supported
- **THEN** that item is omitted without reintroducing it, and the usable remaining preset is available.

#### Scenario: Tainted filtering cannot be supported
- **WHEN** the required clean/tainted distinction is missing or changed incompatibly
- **THEN** the affected presets are unavailable with an explanation and do not accept both states as a substitute.

#### Scenario: Original provider is present
- **WHEN** Simple Stockpile Presets (Continued) is active
- **THEN** the owned selector is inactive with a diagnostic and does not interfere with the original's controls.

#### Scenario: Core contextual hook changes
- **WHEN** the required storage editing path can no longer preserve the intended target and complete filter
- **THEN** owned preset application is inactive with a bounded warning, while otherwise-working manual storage editing remains available.
