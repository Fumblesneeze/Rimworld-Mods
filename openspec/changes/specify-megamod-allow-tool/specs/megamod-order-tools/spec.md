# Order tools

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`). Target: **1.7 beta**.
Specification draft; nothing implemented. Source: Allow Tool, S008 / P1-08.

## ADDED Requirements

### Requirement: Allow and forbid revealed objects
The mod SHALL provide area allow/forbid tools and an allow-everything action under Architect Orders,
with selected-object allow/forbid controls retaining their ordinary action. Area tools affect only
revealed, present objects that support forbidding and whose state changes. Allow everything normally
includes haulable objects and fresh corpses, excludes rotting/desiccated corpses, includes those
remains with Shift, and includes otherwise non-haulable forbiddable objects with Ctrl/Option.
Modifiers combine. Area and map actions report the affected count or that nothing changed.

#### Scenario: Allow a mixed map
- **WHEN** the player uses Allow everything without modifiers on a map containing forbidden meals,
  fresh and rotten corpses, a forbiddable building and an item under fog
- **THEN** the meals and fresh corpses become allowed; the other objects retain their state
- **AND** the feedback counts only objects actually changed

#### Scenario: Deliberately include remains and buildings
- **WHEN** the same action is used with both Shift and Ctrl/Option
- **THEN** revealed forbiddable buildings and rotten/desiccated corpses are included
- **AND** fogged or removed objects are not affected

#### Scenario: Area forbidding and existing controls
- **WHEN** the player drags Forbid across revealed objects and then uses a selected object's Allow toggle
- **THEN** eligible objects in the rectangle become forbidden and the selected object's ordinary toggle works
- **AND** other controls supplied by the game or another mod remain available

### Requirement: Area tools support large bounded selections
The mod SHALL allow its area tools to cover rectangles larger than 50×50 cells, clipped to the
current map, with dimensions and eligible-target highlighting while dragging. Each object is acted
on and counted at most once even if it occupies several cells. Leaving the map view, switching maps
or canceling a drag discards the pending action without applying it to another map.

#### Scenario: Large multi-cell selection
- **WHEN** the player drags an owned area tool across a rectangle wider than 50 cells containing a
  multi-cell object and then completes the drag
- **THEN** the eligible in-map targets are processed once each over the full selected rectangle
- **AND** canceling the drag or changing maps before completion processes none of those pending targets

### Requirement: Select by item type and material
The mod SHALL select similar objects by their actual item type and material, allowing multiple seed
types. A single-cell click starts a new seed selection unless Shift adds to it; dragging adds matching
objects, using the first eligible object as the seed when there is no selection. Map, current-view
and home-area expansion use the existing seeds and prefer matches nearest the camera when limited.
The default total selection limit is 200, adjustable from 50 to 100000 in steps of 50. Alt and
single-cell picking bypass this tool's limit. Partial selection reports that the limit was reached.
Ordinary selection does not reveal fogged objects; a deliberate fog override belongs only in the
developer toolbar and is inactive outside developer mode.
The cursor identifies the active type/material seeds or the need for a seed and indicates when
the limit is reached. It lists up to five seed types, summarizing excess types. A successful similar
selection closes an open Architect panel so the resulting selected-object controls are usable.

#### Scenario: Materials remain distinct
- **WHEN** the player seeds a steel chair and expands selection over a wooden chair, another steel
  chair and an unrelated object with a similar name
- **THEN** only the additional steel chair joins the selection
- **AND** adding the wooden chair as a second seed permits both chair materials

#### Scenario: Limits and scope
- **WHEN** a map expansion would exceed the configured limit
- **THEN** the existing selection is retained, the nearest eligible matches fill the remaining places,
  and feedback explains the partial result
- **AND** choosing home-area or current-view expansion restricts the search to that scope

#### Scenario: No seed and no information leak
- **WHEN** expansion is requested with no seed, or all matching objects are under fog
- **THEN** no arbitrary map-wide selection is made and feedback reports no additional selection

### Requirement: Context actions have explicit scope and preserve native choices
The mod SHALL expose the following extra right-click actions on the relevant order controls, in the
listed order. Existing native or third-party options remain usable; an equivalent native 1.7 action
is reused rather than presented twice. Actions that designate things respect current eligibility,
fog and map scope, and report success counts or no eligible targets.

| Control | Extra actions, in order | Additional restriction |
| --- | --- | --- |
| Cancel | Cancel matching order types across map; cancel all cancelable orders; cancel all blueprints | Matching types are those on selected objects or their cells; clearly name map-wide scope |
| Harvest | All; home area | Native harvest eligibility |
| Chop wood | All; home area | Exclude protected special trees |
| Cut plants | Cut blighted plants | Only blighted plants without that order |
| Harvest fully grown | All; home area | Mature-harvest rules below |
| Finish off | All | Downed-target rules and friendly-target modifier apply |
| Haul | All | Objects requiring an explicit ordinary haul order, such as chunks |
| Haul urgently | All; current view | Bulk actions exclude objects requiring an explicit ordinary haul order; direct urgent selection can include them |
| Hunt | All | Native hunting eligibility |
| Mine | Connected ores; strip-mine tool | Mining capability rules apply |
| Select similar | All; current view; home area | Similar-selection rules apply |
| Strip | All | Exclude player-faction targets and respect native stripping eligibility |
| Allow / Forbid | Current view | Revealed eligible objects within the camera's map rectangle |

#### Scenario: Cancel matching types, not just selected objects
- **WHEN** the player selects an object with a haul order and uses Cancel matching order types across map
- **THEN** cancelable haul orders throughout that map are removed, with the scope stated in the action
- **AND** unrelated order types, planning markers and noncancelable orders remain

#### Scenario: Broad cancellation is explicit
- **WHEN** the player chooses Cancel all cancelable orders
- **THEN** cancelable work designations are removed while plans and noncancelable entries remain
- **AND** blueprints remain until the separate Cancel all blueprints action is used, which performs
  ordinary blueprint cancellation without destroying finished buildings or unfinished frames

#### Scenario: Bulk urgent hauling leaves chunks alone
- **WHEN** the player invokes Haul urgently all with loose meals and rock chunks on the map
- **THEN** eligible meals receive urgent orders and chunks are unchanged
- **AND** explicitly dragging the urgent tool over a chunk can still order its hauling

#### Scenario: Native and modded orders remain available
- **WHEN** a compatible mod adds another order option and the player opens that control's menu
- **THEN** that option remains available alongside the applicable extra actions
- **AND** hidden, ineligible or no-longer-present targets are not acted on from an old menu

### Requirement: Mature harvesting and important-tree protection
The mod SHALL offer Harvest fully grown for harvestable, mature plants, with Shift restricting it
to standard crops, Ctrl/Option to wood-producing plants, and both including their union. Anima and
Gauranlen trees are excluded from that tool and bulk chopping, and from multi-cell cut/chop drags.
Deliberate single-tree native cut/chop remains possible subject to native prohibitions and warnings.
Mature harvesting replaces a conflicting ordinary cut order only; unrelated orders are preserved.
Native restrictions on cutting protected plants and ideology warnings continue to apply.

#### Scenario: Mature crop instead of premature cutting
- **WHEN** a mature harvestable crop has a cut order and an unrelated mod's designation, and the
  player uses Harvest fully grown with Shift
- **THEN** it receives a harvest order, the conflicting cut order is removed, and the unrelated order remains
- **AND** immature crops and wood-only plants are not designated by that action

#### Scenario: Protect special trees without preventing deliberate action
- **WHEN** a cut/chop rectangle contains an Anima or Gauranlen tree alongside ordinary trees
- **THEN** the special tree is excluded
- **AND** selecting it alone and using the native action retains the game's warnings and restrictions

### Requirement: Keyboard and selected-object shortcuts remain contextual
The mod SHALL provide configurable native key bindings: Allow Z, Forbid End, Allow everything Home,
Select similar P, Haul urgently O, and context action Equals. Finish off, mature harvest and strip
mining start unbound. Global tool shortcuts work without opening Orders when enabled, but do not
fire while typing or while a modal window owns input. The context key invokes the first enabled
extra action for the selected tool; with no tool and extended context enabled it uses the first
visible selected-object control with such an action. It does not activate arbitrary native menu
entries. Shift-clicking a supported selected-object order picks up its reusable area tool.

Supported orders are cancel, claim, deconstruct, uninstall, haul, hunt, slaughter, tame, cut, harvest,
chop, mine, strip, open, and the owned allow/forbid, similar, urgent and finish tools where applicable.
Right-click availability and the context-key hint are visible when their display options are enabled.

#### Scenario: Reuse a selected object's order
- **WHEN** the player Shift-clicks a selected building's Deconstruct control
- **THEN** the deconstruction tool is selected for further designation instead of immediately
  deconstructing that building
- **AND** an ordinary click still performs its ordinary selected-object action

#### Scenario: Key respects text entry and hidden tools
- **WHEN** the player types P into a search field or disables the similar-selection tool
- **THEN** typing or the hidden tool's key does not unexpectedly replace the active tool
- **AND** the context key remains independently available when global tool shortcuts are disabled

### Requirement: Common settings and state separation
The mod SHALL put configuration in ordered Mod settings sections: Orders and selection, Urgent
hauling, Finishing and drafted hunting, then Mining plans. Settings use the shared Global default,
Use global and Current save controls; new saves inherit, changing a global value updates inherited
saves, and a dormant override is retained. At the main menu both save controls are disabled; in a
loaded inherited save only the value editor is disabled. Settings apply to the next relevant UI
refresh/action; work-category presentation changes do not require changing another save or restarting.

| Setting | Initial value / range |
| --- | --- |
| Global tool shortcuts; extended context shortcut; Shift pickup of selected-object tools | On |
| Context-menu availability indicator; context-key hint | On |
| Alternative Allow Tool-style icons for allow/forbid | Off; native icons initially |
| Each of the eight tools, each extra context action and each of the four selected-object additions (similar, urgent, finish, mature harvest) | Shown |
| Similar-selection limit | 200; 50–100000, step 50 |
| Separate Haul+ work column; blocked urgent-storage alert | On |
| Separate Finish work column | Off |
| Unforbid nearby objects after finishing | On |
| Drafted-hunting availability; auto-finish prey | On |
| Drafted hunting only designated prey; unforbid corpses from party-hunter kills | Off |
| Initial strip-mine gaps horizontally / vertically | 5 / 5; 1–50 |
| Initial strip-mine alignment follows drag start; show plan editor on release | On |

Key bindings are global input preferences in native keyboard settings, exempt from per-save
inheritance because they describe the player's input setup. Window position is also a global UI
preference. Pawn hunting membership, current selection, work priorities, orders and current mining
geometry are entity/session gameplay state, not global-default/save-override settings. Hiding an
entry point does not silently cancel orders or disable their work. Disabling drafted-hunting
availability ends membership and prevents further automatic hunting; ordinary combat remains native.

#### Scenario: Save overrides and main-menu editing
- **WHEN** one save inherits the selection limit and another overrides it, and the global limit changes
- **THEN** only the inherited save follows the new value
- **AND** returning to the main menu permits global editing but no save override, reset or import mutation

#### Scenario: Hide a tool without erasing orders
- **WHEN** the player hides both finish-order buttons after placing a valid finishing order
- **THEN** those buttons disappear while the existing order remains executable and cancelable
- **AND** disabling drafted-hunting availability instead removes automatic party membership without
  turning ordinary drafted combat off

### Requirement: One provider and understandable unavailable behavior
The mod SHALL leave the original Allow Tool in charge when that provider is active and suppress this
replacement family, with one startup explanation and no duplicate tools, work columns or hunting
actions. If an enabled integration cannot safely operate on the current game/provider, the affected
behavior remains inactive and a bounded startup warning names the failed prerequisite, unavailable
action and fallback. Unrelated compatible mods alone do not make a behavior unavailable. Ordinary
native orders remain usable. No automatic import, removal or rewrite of upstream save data occurs.

#### Scenario: Original provider is installed and active
- **WHEN** both megamod and the original Allow Tool start
- **THEN** the original provides its controls and this replacement family stays inactive with a startup explanation
- **AND** neither mod's stored settings or existing save files are rewritten

#### Scenario: Changed game prevents one enhancement
- **WHEN** a prerequisite for the context-menu enhancement no longer holds
- **THEN** startup logs explain that enhancement's absence and native context actions continue
- **AND** an unrelated compatible patch does not by itself disable the enhancement
