Owner **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **RimWorld 1.7 beta**.
S104 replacement of No Burn Metal. Specification only; no implementation or native acceptance.

## ADDED Requirements

### Requirement: Selected materials stop contributing ordinary flammability
The feature SHALL remove the flammability coming from a selected construction material in objects
actually made from that material. A standard steel, silver or gold wall therefore has zero
flammability when that material's protection is on. Independent additions to flammability remain
effective; the feature does not promise that every modded object becomes wholly fireproof.
Protection applies to existing and newly made objects, independent of faction, quality or damage.
It does not apply merely because metal appears in an object's ingredient list.

#### Scenario: Existing steel furniture and a newly built wall
- **WHEN** steel protection is on and the player inspects existing steel furniture and builds a standard steel wall
- **THEN** both have zero ordinary material-derived flammability without replacing the furniture
- **AND** their other material properties, costs, work and quality remain unchanged

#### Scenario: Wood and fixed-material machinery
- **WHEN** the player uses a wooden object or a fixed-material machine whose construction costs include steel
- **THEN** enabling steel protection does not remove that object's ordinary flammability
- **AND** a separately selected infrastructure type follows its own protection choice

#### Scenario: Independent flammable component
- **WHEN** a supported modded steel object has a separate positive flammability addition
- **THEN** steel protection removes only the material contribution and the separate addition remains
- **AND** inspection reports the resulting value rather than asserting unconditional fire immunity

#### Scenario: Minified object and material preview
- **WHEN** the player inspects a minified protected object or previews the same object made from that material
- **THEN** its applicable flammability agrees with the corresponding ordinary object in the current configuration
- **AND** inspection does not change the object or create a second physical item

### Requirement: Conduits and power switches have explicit independent choices
The feature SHALL provide separate choices for ordinary power conduits, waterproof conduits and
power switches. When selected, each type's ordinary flammability becomes zero; independent added
effects remain. Hidden conduits that are already nonflammable remain unchanged. Power transmission,
switching, breakdowns, electrical incidents, construction costs and other properties remain ordinary.

#### Scenario: Ordinary infrastructure
- **WHEN** the player builds the three protected infrastructure types with their default choices
- **THEN** their ordinary flammability is zero and they still conduct or switch power normally

#### Scenario: One infrastructure type is disabled
- **WHEN** the player turns off only waterproof conduit protection
- **THEN** waterproof conduits use the current ordinary flammability supplied by the game and other content
- **AND** ordinary conduits and switches keep their independently configured protection

#### Scenario: Hidden conduit and electrical incident
- **WHEN** an already nonflammable hidden conduit is used or a native electrical incident occurs
- **THEN** this feature does not add a redundant hidden-conduit toggle, alter incident eligibility or cancel the incident
- **AND** fires caused on other flammable objects or terrain remain possible

### Requirement: Fire resistance retains its damage and consumption boundaries
The feature SHALL affect ordinary ignition and continued burning through the current resulting
flammability. It SHALL NOT grant general flame, heat, explosion or attack immunity, heal damage,
protect a wearer, prevent deliberate recipe consumption, or change floor and terrain flammability.
Selecting a material concerns objects made from it, not the loose material's separate fire behavior.

#### Scenario: Fire has no remaining fuel
- **WHEN** protection makes the only object supporting an ordinary fire nonflammable and no terrain or other object supports it
- **THEN** the fire ceases through the ordinary fire update, within the native 150 simulated tick reassessment interval
- **AND** existing damage is not repaired and paused play does not advance that interval

#### Scenario: Wooden floor and loose resources
- **WHEN** a protected steel object stands over a burning wooden floor beside a separately flammable loose resource
- **THEN** protection does not extinguish the floor or change the loose resource's own flammability
- **AND** the steel object is no longer selected as ordinary fire fuel when its resulting flammability is zero

#### Scenario: Direct damage and worn apparel
- **WHEN** a protected building receives direct flame damage or protected metal apparel is worn by a burning pawn
- **THEN** native direct-damage rules still apply, including any native minimum building damage contribution
- **AND** the pawn and apparel gain no additional damage immunity from this feature

#### Scenario: Ordinary processing and fuel use
- **WHEN** a material or object is valid for an ordinary burn, smelt, recycle or fuel-consumption action
- **THEN** material fire protection alone does not forbid or refund that action
- **AND** existing recipe and fuel eligibility restrictions remain authoritative

### Requirement: Discoverable choices preserve explicit source defaults
The feature SHALL list eligible active metallic construction materials and the named exceptions
below, grouped by their actual content provider. The following choices start on when their expected
content is present, supported and has a positive contribution to remove; other discovered metallic
materials start off. Availability is not inferred from a familiar name alone.

| Content | Initially selected choices |
| --- | --- |
| Core | Steel, silver, gold; power conduit, waterproof conduit, power switch |
| Anomaly | Bioferrite |
| Alpha Animals | Sky steel |
| Vanilla Factions Expanded - Classical | Bronze |
| Medieval Overhaul | Iron ingot |
| More Useful Materials | Carbon fiber composite and heavy DU-composite steel |
| Cupro's Alloys (Continued) | Plastin, black steel, brass, chrome-plated steel, aurichalcum, sterling silver, rose gold, electrum, plumchalcum, duralumin, zamak, bismuth bronze, bronze, cupronickel, stainless steel, hepatzion, copper, lead, tin, aluminum, nickel, zinc, bismuth and chromium |

Already zero contributions, including ordinary plasteel, jade and uranium, do not need protection
controls. A provider's current labels are displayed, including renamed content. Recognizing a
provider does not enable every metal it adds. Missing or unsupported choices do not become substitutes
for similarly named content; saved choices can remain dormant until the same supported content returns.

#### Scenario: A newly installed metallic material
- **WHEN** a new active provider supplies a supported flammable metallic construction material outside the default list
- **THEN** it appears under that provider with protection off
- **AND** the player can enable it without changing unrelated material properties

#### Scenario: Known provider with additional materials
- **WHEN** More Useful Materials supplies flammable carbon fiber composite and tempered steel
- **THEN** carbon fiber starts selected while tempered steel starts unselected
- **AND** heavy DU-composite steel, if already nonflammable, requires no redundant toggle

#### Scenario: Expanded Materials already supplies nonflammable metals
- **WHEN** Expanded Materials supplies metallic materials whose ordinary material contributions are already zero
- **THEN** those materials are not presented as needing this feature's protection
- **AND** any other positive metallic contribution discovered there starts off unless separately covered by the default list

#### Scenario: Missing or renamed optional material
- **WHEN** an optional provider is absent, renames only a material's visible label, or replaces its identity with different content
- **THEN** absence does not prevent Core protection, a label-only rename retains the same choice, and different content does not inherit the old choice
- **AND** dormant preferences do not cause missing-content errors or mutate another provider's content

### Requirement: Settings use common inheritance and explicit bulk scope
The feature SHALL place configuration in the shared Materials and fire section of mod settings.
Every material and infrastructure choice uses the common Global default, Current save and Use global
controls. New or missing save choices inherit the current global value. Checking Use global retains
a dormant override; unchecking restores it or seeds a first override from the current inherited value.
The per-save editor is inactive while inheriting. At the main menu all save editors, inheritance
changes, bulk save actions and resets are inactive in both appearance and effect.

Bulk actions SHALL explicitly select Global defaults or Current save and either All available
choices or All choices in a named provider, showing the affected count. Search and collapsed groups
do not reduce that advertised scope. Global bulk on/off changes defaults only. Save bulk on/off
creates explicit overrides; save bulk Use global restores inheritance while retaining dormant
overrides. Missing, unsupported and already-zero entries are excluded. No action changes another
save, and a failed bulk commit leaves the selected scope unchanged.

#### Scenario: Global changes and independent save override
- **WHEN** one save inherits steel protection and another has an explicit override, and the global default changes
- **THEN** the inheriting save follows the new global value and the overridden save retains its own value
- **AND** returning to Use global preserves the dormant override for later restoration

#### Scenario: Main-menu configuration
- **WHEN** settings are opened from the main menu
- **THEN** global choices remain editable and every save-scoped control is visibly disabled with an explanation
- **AND** invoking a disabled save action by another input path changes no save

#### Scenario: Bulk action while searching
- **WHEN** a search shows one material and the player selects Global defaults, All available choices, and On
- **THEN** the displayed full target count and scope explain that all available choices are affected, including hidden matches
- **AND** explicit save overrides and dormant or unsupported choices remain unchanged

#### Scenario: Provider save overrides and inheritance restoration
- **WHEN** the player applies Off to all available choices in one provider for Current save, then applies Use global to that same scope
- **THEN** the first action creates only those save overrides and the second restores their inheritance
- **AND** their dormant Off values remain available without changing global defaults or another provider

#### Scenario: Failed bulk update
- **WHEN** a selected bulk update cannot be validated or committed completely
- **THEN** none of its selected choices is changed and the player receives an explanation
- **AND** the UI does not report successful partial protection

### Requirement: Search and presentation expose the actual scope and effect
The settings view SHALL provide case-insensitive search by current item label, stable content
identifier and provider name; Core, expansions and other providers are distinguished. Within those
groups, current labels determine readable ordering. Rows distinguish protection for objects made
from a material from protection for a named infrastructure type. Current effective choice,
inheritance and unavailable behavior are visible separately; Off means use ordinary current rules,
not force an object to burn. Whole-list and per-provider counts show selected versus available
choices for the displayed settings scope, independent of search. Native stat explanations agree
with actual resulting flammability.

Groups can be collapsed individually or together, and search temporarily reveals matching rows
without discarding their previous collapse choices. Unavailable known providers are hidden initially
and can be shown as noninteractive explanatory entries. Hide-unavailable and collapse preferences are
global UI preferences through the common settings path: they describe the user's navigation, not a
colony rule. Search and scrolling are transient. All controls and explanations support English,
German, Spanish, French, ChineseSimplified, Russian and Japanese, including narrow/crowded layouts.
There is no new bottom tab, bottom-right toggle, debug control or entity gizmo for this feature.

#### Scenario: Renamed bronze and search
- **WHEN** another provider changes bronze's visible name and the player searches by its stable identifier or provider
- **THEN** the same material choice remains findable with its current name and correct source attribution
- **AND** the search reveals it inside a collapsed group without permanently expanding that group
- **AND** whole-list and provider counts retain their stated full scope rather than counting only visible search results

#### Scenario: No matches or inactive provider
- **WHEN** a search has no matches or the player reveals a known inactive provider
- **THEN** the view explains the empty or unavailable state without offering a usable content toggle
- **AND** inactive providers are excluded from bulk affected counts

#### Scenario: Truthful effects and wide translations
- **WHEN** a player inspects a protected object's flammability and opens settings with a long localized material name
- **THEN** the displayed effect agrees with the current game value, preserving any independent positive contribution
- **AND** the label, scope, state and controls remain readable and do not overlap or alter neighboring UI state

### Requirement: Changes are current and reversible without cross-save leakage
The feature SHALL apply a committed effective choice to the next relevant inspection or fire
evaluation for both existing and future objects, without restart or rebuilding. It SHALL preserve
ordinary scheduled fire updates rather than scanning or deleting every fire on a settings change.
Loading another save or returning to the main menu changes to that context's effective choices
before they are used. Turning protection off reveals current ordinary behavior, including legitimate
changes from other content, rather than restoring a stale startup snapshot.

#### Scenario: Protection is turned off after another content change
- **WHEN** another supported source has legitimately changed ordinary steel flammability and the player turns steel protection off
- **THEN** current inspection and subsequent fire evaluation use that source's current ordinary value
- **AND** an old captured value is not restored over it

#### Scenario: Save switching and returning to the menu
- **WHEN** the player switches between saves with different overrides and then returns to the main menu
- **THEN** each save uses only its effective choices, and menu previews use global defaults
- **AND** no cached protection from the previous save changes the next context's results

#### Scenario: Removal and re-enablement
- **WHEN** this feature is disabled or later re-enabled with supported content
- **THEN** existing objects remain usable and their ordinary identities, health and other properties remain intact
- **AND** saved protection preferences can resume without reconstructing objects or importing the original mod's settings

### Requirement: Compatibility failures leave coherent ordinary behavior
The feature SHALL activate only where its documented compatibility requirements hold. Compatible
unrelated changes remain supported. If an enabled protection path cannot be applied safely, its
affected behavior remains ordinary, its settings show that it is unavailable, and startup logs give
one bounded warning per skipped enabled patch naming the failed requirement, unavailable behavior
and fallback. Other supported choices continue when they do not depend on the failed path.
When original No Burn Metal is active, it owns this entire overlapping feature and our configured
choices remain dormant with an explanation. No foreign behavior is removed or falsely reported off.

#### Scenario: A compatible unrelated change
- **WHEN** another mod changes an unrelated stat or adds compatible behavior around a relevant method
- **THEN** supported protection remains available without requiring an untouched method or exclusive patch ownership
- **AND** unrelated values and foreign behavior are preserved

#### Scenario: Changed material behavior
- **WHEN** the game or another mod removes or ambiguously changes the required material-flammability behavior
- **THEN** that enabled protection path is skipped with a bounded startup warning and an unavailable settings state
- **AND** ordinary behavior remains coherent, without a misleading zero-value display or partially active owned path

#### Scenario: Original provider is present
- **WHEN** original No Burn Metal is active alongside the megamod
- **THEN** our overlapping protection is inactive and its saved choices are retained
- **AND** settings identify the original provider as authoritative instead of claiming our Off choice overrides it

#### Scenario: Optional content changes while other content remains supported
- **WHEN** one advertised optional material is missing or fails its required compatibility shape
- **THEN** that material does not receive unsafe protection and supported Core or other-provider choices remain usable
- **AND** unselected absent content does not generate failed-enabled-patch warnings

### Requirement: Owned features use one resulting fire policy
The feature SHALL compose directly with owned material previews, structural damage, fire response,
corpse burning and item recipe orders. All use current effective flammability and their own ordinary
eligibility rules. This feature does not change a material's categories, widen fuel filters, create
new content or repeat another feature's action.

#### Scenario: Combined damage and fire response
- **WHEN** structural damage and pawn fire-response features are enabled during a fire near protected metal infrastructure
- **THEN** protection changes only its specified flammability contribution, damage scaling occurs once under the relevant damage rules, and pawns retain ordinary fire-response eligibility
- **AND** corpse burning does not acquire metal protection merely because a corpse carries metal apparel

#### Scenario: Bills and material comparisons
- **WHEN** the player compares materials or issues an owned processing order for a protected object
- **THEN** comparisons use current effects and the processing action follows its ordinary recipe contract exactly once
- **AND** no material category, allowed fuel list or unrelated stat is removed or replaced by this feature
