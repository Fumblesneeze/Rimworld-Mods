# Reusable query library

Owner **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`; **1.7 beta**.
S121 TD Find Lib - Continued. Specification only.

## ADDED Requirements

### Requirement: Open one reusable library from the relevant workflow
The mod SHALL provide library controls from the ordinary search workspace and population-query
editor, plus a Manage query library entry in the Search section of mod settings. It adds no bottom
tab. The library stores reusable user-authored templates and groups globally, not as inherited
per-save configuration. A query attached to a bill remains that bill's gameplay state; editing a
global template does not retroactively modify any bill or working search. The library makes this
scope distinction clear. Debug templates have a separate collection, entered only through the top
debug toolbar; ordinary consumers cannot execute them.

At the main menu, global template organization and content-based criteria editing remain available.
Preview/evaluation and map/save-specific binding writes are disabled, including their callbacks,
with a reason. A previous game's objects must never supply an implicit context. In a loaded game's
world view, explicit/all-loaded-map previews can run; Current map still requires a viewed map.
All labels, choice descriptions, errors and controls support the seven baseline languages; user
names remain the user's text. Long labels and normal supported UI scales retain usable controls.

#### Scenario: Reuse a template for a bill
- **WHEN** a player opens a global pawn template for a bill and confirms the bill-map binding
- **THEN** the bill receives its own query copy; later template edits leave that bill unchanged.

#### Scenario: Organize from the main menu
- **WHEN** the player renames a global group without a loaded game
- **THEN** the rename works while current-save/map controls and their write paths remain unavailable.

### Requirement: Edit and save library entries deliberately
The library SHALL allow creating, renaming, deleting, copying and reordering groups and queries;
moving queries between groups; and copying instead of moving with an explicit Copy action or
Ctrl-drag. Copies are independent and retain every operand, group mode, disabled/included state,
listing and binding intent. Each group/query has identity separate from its editable display name.
Names are nonempty. Group names are unique within the collection and query names within their group;
case-insensitive collisions require an explicit Replace, Save as copy or Cancel decision. A generated
copy name is unique, including after repeated copying. Replacing does not merge unrelated clauses.

New and existing query editors use a working copy. Save commits the complete valid or explicitly
unresolved definition; Cancel discards edits. Closing a dirty editor asks Save/Discard/Keep editing.
Invalid input cannot destroy the previous saved entry. A new entry is not stored before Save.
Group/query deletion always confirms the actual target and contained count; Shift cannot bypass it.
Reorder/move/copy must not lose or duplicate neighboring entries or allow a nested group to become
its own ancestor. A locked editor blocks every mutating path, including imports, drag/drop, clear,
rename and delayed callbacks; inspection, copy and refresh remain available. S033's explicit New
search shortcut still creates a new working query without changing its locked source.

#### Scenario: Cancel a new query
- **WHEN** the player creates a query, changes criteria and discards it
- **THEN** the library has no new entry and the current search/bill and existing templates are unchanged.

#### Scenario: Duplicate a duplicate
- **WHEN** a query and its first copy already exist and Copy is used again
- **THEN** an independently editable copy with a distinct name appears without replacing either existing query.

#### Scenario: Import while locked
- **WHEN** a menu command or previously opened chooser attempts to replace a now-locked query
- **THEN** replacement is refused with an explanation and all existing operands remain unchanged.

### Requirement: Preserve complete definitions and bind actual identities
Save, reload, rename, copy and supported exchange SHALL retain all criterion choices, including
permit faction/readiness, ideology preference mode, relationship direction/gender, schedule hours,
unit/range bounds, required/prohibited masks and nested groups. Display-language changes do not
change meanings. Exact maps, zones, areas, policies, ideologies, factions and provider groups retain
their actual identity within a save; duplicate labels never select a different entity.

A reusable global template carries explicit placeholders for save-specific bindings rather than
live links to a different save. Opening it presents the required bindings before use. Current map
and All loaded maps preserve their declared meaning. Chosen maps with no valid binding are
unavailable, never automatically Current map. Retargeting a whole query to a bill's map is explicit
and cannot silently remove an exact-zone/area restriction. Removed targets, missing content and
unsupported clauses remain visible and repairable; they are not dropped or auto-disabled.

Loading a damaged collection preserves the original data and reports the affected entries; it must
not overwrite the only copy with an empty/default library. Saving/importing prepares and validates
the complete replacement, retains a recoverable previous version, and commits atomically. Failure
restores the previous collection and reports failure. Explicitly saving an unresolved definition
is allowed as data, but never makes its evaluation authoritative.

#### Scenario: Reuse an exact-area query in another save
- **WHEN** a template refers to an area from save A and is opened in save B with an identically named area
- **THEN** it requests deliberate binding and does not silently select B's area.

#### Scenario: Remove a required policy
- **WHEN** a saved query's selected policy disappears
- **THEN** its choice remains visible as unresolved, no authoritative count is shown, and other library entries remain intact.

### Requirement: Transfer complete supported queries atomically
The library SHALL support explicit copy/paste, export/import and admitted provider transfer for a
single query, group or whole collection. Transfers preserve names/order, all operands and scope
intent, and use independent copies. A preview states destination, replacements, required bindings
and unsupported content before a mutation. All collisions in the selected transfer are resolved
before committing; Cancel or any rejected entry leaves the entire selected destination unchanged.
Focused text-field copy/paste remains text editing; whole-query commands are distinct.

Malformed, oversized, unsupported or unresolved executable transfers are rejected with a bounded
useful explanation, without running imported instructions, interrupting a game save/load, exposing
whole imported content in logs or partially importing the accepted subset. Importing an explicitly
unresolved definition for storage is a separate deliberate action; it cannot be mistaken for a
working search or be sent to an executing consumer. Source-formatted queries require supported
conversion of every clause and binding. Exact imported numeric values are never silently rounded
to friendlier sample thresholds. Automatic foreign-settings/save migration is outside this scope.

#### Scenario: Reject one unsupported clause in a group
- **WHEN** a three-query group import includes one unsupported criterion and the player requested usable queries
- **THEN** the group import reports that criterion and changes none of the destination's queries.

#### Scenario: Preserve an unsupported definition deliberately
- **WHEN** the player explicitly chooses Store unresolved after inspecting the problem
- **THEN** the complete definition is retained as unavailable data, with no result actions or automatic consumer execution.

### Requirement: Compose owned consumers and narrow external exchange
Owned search and population production SHALL use the same catalog and library meaning directly.
The production destination preserves S018's initial pawn population, deliberate non-pawn physical-unit counts and explicit map-scope rules and refuses
queries it cannot represent without loss. Sending a bill query to search opens the independent
locked copy specified by S033. Library templates are not background tasks; merely opening,
importing or saving them never schedules repeated evaluation or changes jobs, bills or alerts.
S033's manual refresh and temporary off-by-default 60-tick refresh remain authoritative. A library
view of an existing consumer's query identifies that consumer and opens its own UI; it does not
create a second scheduler or keep a closed consumer alive.

Supported external exchange uses the actual optional TD library and actual receiving consumer.
Only compatible transfer kinds and presently available recipients are offered; unavailable or
same-source feedback destinations are excluded. Sender and receiver retain independent state.
A recipient that disappears between preview and delivery causes visible failure without changing
the owned source. Unsupported external representations are rejected rather than approximated.
Original TD library presence does not disable independent owned queries; original Ctrl F and
Everybody Gets One overlap follows their already specified single-provider policies. No claim is
made that removing TD Find Lib satisfies another mod's package or assembly dependency.

#### Scenario: Receiver closes before delivery
- **WHEN** an external population editor closes after its transfer destination was selected
- **THEN** delivery is declined and the owned library/working query remains unchanged.

#### Scenario: Open a monitoring example
- **WHEN** the player opens or saves an example describing hungry pawns
- **THEN** it remains a query only, without enabling an alert, drug response, meal job or recurring task.

### Requirement: Supply explicit ordinary examples without hidden orders
The library SHALL initially contain an empty Saved queries group and offer an explicit Add examples
action. Examples are independently editable copies; they are never reinserted at startup after
deletion. All provided examples display their actual criteria and map scope before use. Adding
examples uses ordinary collision/atomicity rules. Missing content or map binding leaves the affected
example visibly unavailable; unsupported criteria are not omitted. Monitoring names describe matches
without claiming an actual alert, treatment or job exists. The following source-derived ordinary
examples are supplied, with labels translated and numeric thresholds shown as editable choices.

Unless specified below, scope is All loaded maps; pawn examples use Pawns, item examples Items,
plant examples Plants and building examples Buildings. Held-item examples use Known contents.
Production examples require an explicit destination map. Thresholds below are inclusive, with
“at least”/“at most” denoting the stated unbounded opposite end.

| Example | Complete selection |
| --- | --- |
| Humans without pants in possessions | Exact Human pawn; not recursively holding Pants |
| Passionate skilled pawns | At least three of Shooting, Melee, Construction, Cooking, Crafting, Intellectual at level 8 or above with any passion |
| Unpartnered humanlikes | Humanlike; no disclosed spouse, fiancé or lover of any gender |
| Hunters without ranged weapons in possessions | Hunting work enabled; no recursively held ranged weapon |
| Hostiles in Home | Hostile-faction pawn inside Home |
| Hostile possessions | Item held by hostile holder; Current map |
| Doors blocked by objects | Door blocked open; same cell contains an item or Other-kind object |
| Very hungry pawns | Disclosed Food fraction at most 0.103195697 (10.3195697%) |
| Forbidden animal corpses | Animal-corpse category and forbidden |
| Damaged player pawns and buildings | Player faction; health/HP fraction at most 0.995464921 (99.5464921%); Pawns plus Buildings |
| Allowed deteriorating items | Currently deteriorating and not forbidden |
| Harvestable plants outside growing zones | Harvestable, product not Wood logs, outside any growing zone |
| Undesignated non-steel ore | Resource mineable, not compacted steel, no Mine designation; Natural objects |
| Potential useful tame targets | Nonplayer animal; wildness at most 50%; pack animal OR annual unfertilized eggs 0–60 OR milk 0–840 OR wool 0–600, requiring that producer/product to exist |
| Valuable hostile weapons | Weapon-category possession of hostile holder; market value at least 300 |
| Valuable hostile possessions | Hostile-held item; Excellent or better OR market value at least 500 |
| Skilled downed hostiles | Recruitable hostile pawn in the native downed waiting activity; passionate Shooting or Melee at least 10 AND at least one passionate Construction/Mining/Cooking/Plants/Animals/Crafting at least 7 |
| Downed hostiles with known advanced implants | Hostile pawn in downed waiting activity; disclosed bionic or archotech implant category |
| Prisoners on Maintain only | Prisoner with Maintain only interaction |
| Selected mood or night-owl schedule issues | Any non-nullified mood thought: Clothed nudist, Jealous, Greedy stages 0–4, Ascetic 0–6, Prosthophobe unhappy 0–5, Not bonded animal master 0–1; OR Night owl whose schedule differs from Sleep in any hour 11–17; stages bind to the named thought's actual effects, never display positions |
| Blighted plants | Blighted; Current map |
| Melee population | Colonist, not colony slave/quest lodger; Melee at least 6 OR any Melee passion; explicitly chosen destination map |
| Shooting population | Same population restrictions; Shooting at least 6 OR any Shooting passion; destination map |
| Mechanitor population | Same population restrictions; Mechlink implant; destination map, Biotech |
| Slave population | Colony slave; destination map |
| Child population | Player-faction Child developmental stage; destination map |
| Royal options available | At least one unspent permit point OR any off-cooldown permit; Royalty |
| Uncomfortable titled pawns | Any title; not within comfortable temperature range; Royalty |
| Mining specialists with work disabled | Holds Mining specialist role; Mining work not enabled; Ideology |
| Dead pawns with mechlinks | Dead pawn with disclosed Mechlink implant; Map things, Biotech; corpse is one physical result |
| Spare mechanitor bandwidth | At least one available bandwidth; Map things, Biotech |
| Animal aptitude with work disabled | Active strong/remarkable Animals aptitude gene; Handling work disabled; Biotech |
| Gauranlen strength below example thresholds | Current dryads at least 4 AND strength at most 76.9230783% (fraction 0.769230783), OR at least 3 AND at most 51.9230783% (0.519230783), OR at least 2 AND at most 26.9230813% (0.269230813); Current map, Ideology |
| Player pawns of another ideology | Player faction; ideology differs from player's primary; Current map, Ideology |

Labels that refer to particular source thought stages show the associated effect text when added;
if the required effect cannot be identified in the current content, that example is unresolved.
The six source custom-inspection examples belong to the debug capability, never this ordinary pack.
These examples deliberately retain source-specific thresholds where stated; they are examples,
not global balance settings, universal danger assessments or promises that a problem is fixable.

#### Scenario: Add examples twice
- **WHEN** the player has edited an example and invokes Add examples again
- **THEN** the existing edit is preserved unless the player explicitly chooses Replace after the collision preview; deletion does not cause later startup restoration.

#### Scenario: View the skilled-hostile example
- **WHEN** a recruitable downed hostile satisfies only the combat-skill half of that example
- **THEN** it does not match until the required passionate work-skill condition also holds; no capture order is issued.

### Requirement: Use the common settings path for suggestion preferences
Only available choices SHALL be an actual mod setting under Search → Query editor, with built-in
global default true and the common Global default, Current save and Use global controls. New/missing
save entries inherit; switching to Use global retains a dormant override and switching back restores
it or seeds a first override from the effective value. Apply changes on the next chooser redraw.
Main-menu save controls and callbacks are disabled, while the global control remains editable.
Saved templates, selected operands, locks and group order are user/query data, not inherited settings.
There is no reflection-prewarm setting; that implementation detail does not require a player choice.

#### Scenario: Override suggestions in one save
- **WHEN** a save overrides Only available choices to false and the global value remains true
- **THEN** its chooser offers all admitted choices, inherited saves retain filtering, and neither operation rewrites any saved query.

### Requirement: Admit integrations without sacrificing saved data
Every required patch or external adapter SHALL meet its documented semantic target before activation.
False, unknown or failed checks disable only that patch/declared atomic feature group, preserve
definitions, and produce one bounded startup warning naming the target, failed condition, unavailable
behavior and fallback. Compatible foreign patches alone do not fail admission. Repeated initialization
and supported later revalidation do not duplicate registration; failed installation rolls back only
newly installed owned work. A safe current native fallback is allowed when it preserves the complete
query contract; stale/partial counts are not a fallback. No integration mutates another provider's
settings, removes it or changes the normal active mod list.

#### Scenario: Optional exchange fails admission
- **WHEN** the actual external library lacks a required transfer operation
- **THEN** external transfer is unavailable with a bounded reason, while owned catalog/library/search operations and all saved definitions remain intact.
