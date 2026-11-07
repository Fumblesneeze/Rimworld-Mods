# Floor and order command shortcuts

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **RimWorld 1.7 beta**.
S067; observable draft only, no implementation or gameplay acceptance.

## ADDED Requirements

### Requirement: FOS01 Existing commands in complementary Architect categories
The feature SHALL offer Deconstruct in Floors and Remove Floor in Orders when each addition is supported, while retaining both commands in their original categories.
These are additional entry points to existing actions. Other commands, current availability rules
and related Architect ordering/search behavior remain intact; no fixed screen position is promised.
The feature does not add a second equivalent entry when one is already present.

#### Scenario: Deconstruct while planning floors
- **WHEN** the player opens Floors with its supported shortcut enabled
- **THEN** Deconstruct is available there as the ordinary building-deconstruction tool
- **AND** opening Orders still offers its existing Deconstruct command.

#### Scenario: Remove a floor from general Orders
- **WHEN** the player opens Orders with its supported shortcut enabled
- **THEN** Remove Floor is available there and still available in Floors
- **AND** Remove Foundation and other existing commands remain separate and unchanged.

#### Scenario: Preserve a supplied equivalent and unrelated choices
- **WHEN** the active content already supplies the same native command in the destination category
- **THEN** this feature contributes no additional equivalent entry
- **AND** it preserves unrelated commands and their availability instead of replacing the category.

### Requirement: FOS02 Native actions and consequences
The shortcuts SHALL use the same target eligibility, warnings, designation, cancellation and work consequences as their existing native entry points, including admitted owned removal policies.
Deconstruct targets eligible buildings, not the ground beneath them. Remove Floor targets removable
flooring under current visibility, obstruction and support rules, not an unrestricted terrain eraser.
Normal work retains its current time, costs, refunds and roof/removal sequencing. Native direct
removal exceptions such as god mode, frames and zero-work deconstruction remain; no new exception
is introduced. Merely browsing a category, hovering a command or sampling a floor issues no order.

#### Scenario: Designate and cancel building deconstruction
- **WHEN** the player uses Floors Deconstruct on an ordinary eligible completed building
- **THEN** it receives the same deconstruction order and warnings as the Orders tool
- **AND** cancelling that order before work leaves it intact and removes that pending order.

#### Scenario: Real floor removal preserves support restrictions
- **WHEN** the player drags Orders Remove Floor across revealed eligible and ineligible cells
- **THEN** only cells accepted by the existing floor-removal tool receive orders
- **AND** pawns perform ordinary removal work with its current layered-floor/support consequences;
  hidden cells, non-removable terrain and protected building support are not bypassed.

#### Scenario: Existing exceptions do not grant new powers
- **WHEN** the same target is acted on through either original or added entry point in normal play
  or native god mode
- **THEN** both entry points apply the same native immediate-versus-work behavior
- **AND** browsing, hovering or floor sampling alone never removes a building or floor.

### Requirement: FOS03 Contextual access without an added Orders key claim
The supported keyboard correction SHALL leave the added Orders Remove Floor command mouse-accessible without assigning it a keyboard shortcut, while retaining the original Floors Remove Floor binding and other commands' bindings.
This is a proposed correction to the source behavior: it avoids competing with wood harvesting
for the same key in Orders. Deconstruct retains its native binding. Current user remappings remain;
the feature does not redefine global keys or repair every unrelated native shortcut collision.

#### Scenario: Use the two Remove Floor entry points
- **WHEN** the keyboard correction is supported and the player opens Orders, then Floors
- **THEN** Orders Remove Floor can be selected by mouse and advertises no keyboard shortcut
- **AND** Floors Remove Floor retains its current native shortcut and ordinary keyboard selection.

#### Scenario: Search or reorder does not assign the wood-harvest key to removal
- **WHEN** Architect presentation changes ordering, filtering or pages while the correction is active
- **THEN** Orders Remove Floor does not acquire a keyboard shortcut, even when wood harvesting is hidden
- **AND** other displayed commands retain their own current binding rules.

#### Scenario: Binding correction is unavailable
- **WHEN** startup cannot support the contextual keyboard correction
- **THEN** it leaves native or selected-provider command/key allocation unchanged
- **AND** one bounded warning explains that the correction is unavailable and the native fallback
  can allocate the shared key according to display order; successful menu additions remain usable.

### Requirement: FOS04 Independent compatibility admission and provider coexistence
Each enabled contribution SHALL check its documented target conditions at startup and skip only its unsupported changes with an explanatory bounded warning.
Successful checks accept compatible third-party changes; another patch owner or an unrelated edit
is not itself a failure. Missing or ambiguous required content leaves the affected contribution
unapplied without replacing lists or leaving a partial added structure. Each warning identifies the
patch, failed target condition, unavailable player behavior and remaining fallback, without repeated
per-frame/job messages. Intentional provider selection or existing membership is not a failure.

#### Scenario: One destination is unsupported
- **WHEN** one destination category is missing or has an unsupported shape and the other is valid
- **THEN** the invalid addition is skipped with one explanatory warning and no partial addition
- **AND** the valid independent addition still applies; existing native commands remain where available.

#### Scenario: Compatible third-party content and repeated startup
- **WHEN** unrelated category entries or compatible patches are present and startup is repeated
- **THEN** supported contributions preserve those entries and effects without adding another equivalent command
- **AND** unrelated changes do not produce an incompatibility warning.

#### Scenario: Original source supplies the placement
- **WHEN** the original Add Deconstruct Button To Floor Category provider is active
- **THEN** megamod leaves both placement additions to that provider regardless of their relative load order
- **AND** it independently applies only a supported contextual keyboard correction to the supplied
  native Orders command; it does not remove provider content or impersonate the source.

#### Scenario: Selected original has changed its offered commands
- **WHEN** the selected original provider no longer supplies one of its expected shortcuts
- **THEN** that missing shortcut is reported as unavailable from the selected provider
- **AND** megamod preserves the remaining commands and does not claim the missing behavior was supplied.

### Requirement: FOS05 Contextual presentation and owned feature composition
The feature SHALL remain in existing Architect categories and preserve ordinary game-rule availability, the selected Architect presentation, and admitted removal behavior across reloads.
No new bottom tab, mod-settings row, bottom-right toggle or cheat control is introduced. These
commands and their pending orders are gameplay actions, not inherited settings. Native labels,
icons and tooltips are reused; any new player-facing explanations cover all seven baseline languages.
Removing the shortcut feature does not erase existing ordinary pending removal orders.

#### Scenario: Architect presentation and restrictions compose
- **WHEN** the player uses either native Architect presentation or the owned search/pagination view
- **THEN** supported added commands follow the same current visibility/game rules and keyboard policy
- **AND** activating them retains current owned roof-safe removal/order policies rather than bypassing them.

#### Scenario: Reload or remove the additional navigation
- **WHEN** the player reloads a save or later removes this shortcut feature
- **THEN** categories reflect currently available providers without stale duplicate contributions
- **AND** ordinary pending removal orders retain their native saved behavior.

#### Scenario: Native localization and floor sampling
- **WHEN** the player uses a supported baseline language or separately samples a floor for construction
- **THEN** the shortcuts retain readable native labels/tooltips and translated new explanations
- **AND** sampling does not activate either removal tool or create a removal order.
