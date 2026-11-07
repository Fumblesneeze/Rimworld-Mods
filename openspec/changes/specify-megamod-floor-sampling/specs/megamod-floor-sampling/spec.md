# Floor sampling

Owner Make Rimworld Great Again (`fumblesneeze.megamod`, `mods/megamod`),1.7 beta.
S136 replacement; observable draft only, no implementation accepted.

## ADDED Requirements

### Requirement: Sample a visible floor without issuing construction

FPT01. Architect / Floors SHALL offer **Pick floor** when the integration is supported. Selecting it
enters a single-cell sampling mode with a distinct cursor and cell indication. Hovering an in-bounds,
unfogged floor identifies its current terrain name. Clicking an eligible floor selects that exact
terrain's ordinary construction tool; success feedback SHALL indicate selection, not construction.
The sampling press, drag and release SHALL not also place a blueprint, remove terrain, spend
resources, paint, cancel existing work or order a pawn. Placement requires a subsequent independent
ordinary construction input. Right-click or the normal Cancel action exits sampling without work.

#### Scenario: Pick then place
- **WHEN** the player selects Pick floor and clicks a completed wooden floor
- **THEN** the wooden-floor construction tool becomes active with no new blueprint; a later construction click can create an ordinary valid floor blueprint.

#### Scenario: Drag or cancel sampling
- **WHEN** the player drags while sampling, releases after selection, or cancels before selecting
- **THEN** the gesture creates no construction/removal order and cancellation leaves the map unchanged.

### Requirement: Respect current floor and construction availability

FPT02. Sampling SHALL accept only the current exposed terrain in the game's **Floors** construction
category with a currently allowed and visible ordinary construction tool. Core and modded floor
variants meeting that rule are eligible without a hardcoded floor list. Natural ground, concealed
layers, planned blueprints and floors categorized elsewhere are not additional sampling sources.
The selected terrain identity SHALL retain its actual variant, including color when that is a
distinct floor variant. Paint applied to an existing floor is not copied as an extra paint order.

Research, scenario/difficulty rules, discoveries, prerequisites, ideology and current content
exclusions SHALL remain authoritative. An existing ancient or excluded floor does not unlock its
construction. God mode uses its ordinary native availability only while that mode is active; this
tool never enables it. Availability SHALL be rechecked at click and after relevant paused/context
changes through the shared Architect rules. Sampling an otherwise available floor does not require
having its materials now; subsequent construction still needs ordinary materials, work and placement
permission. Sampling cannot waive a destination's support/terrain/obstacle restrictions.
Content exclusions SHALL use S038's applied policy. Pending content edits do not affect sampling
before their specified save/reload application, and the sampler must agree with Architect afterward.

#### Scenario: Missing research or excluded content
- **WHEN** the player samples a floor whose build command is hidden by research or the owned content-selection policy
- **THEN** no build tool is activated and the visible explanation indicates that construction is unavailable.

#### Scenario: Missing supplies and invalid destination
- **WHEN** an available floor is sampled with no stock of its material, then the player points at an invalid construction cell
- **THEN** selection succeeds, but no free resources are created and native placement rejection still applies.

#### Scenario: Exact variant and exposed layer
- **WHEN** a colored floor covers a foundation or a building occupies a floor cell
- **THEN** the current exposed floor terrain is sampled, not the hidden foundation, building or an unrelated variant; painted color adds no automatic painting.

#### Scenario: Availability changes while paused
- **WHEN** a previously valid command becomes unavailable before the sample click, or god mode is turned off
- **THEN** current ordinary eligibility governs the click and no cached permission unlocks construction.

#### Scenario: Pending exclusion and reload
- **WHEN** the player requests a floor exclusion that remains pending in S038, then applies it by save/reload
- **THEN** sampling still follows the prior applied policy before reload and rejects the newly excluded floor afterward, agreeing with Architect in both states.

### Requirement: Explain invalid samples without revealing fogged terrain

FPT03. Hover and click feedback SHALL distinguish a visible floor that cannot currently be built
from a visible non-floor target, without throwing or silently selecting something else. Where an
ordinary reason is available, display it; otherwise say no available construction tool. Fogged or
out-of-bounds cells SHALL disclose no terrain name, category, underlying layer or construction
availability. They SHALL be rejected with a generic non-revealing explanation. A rejected click
keeps the sampler available for another cell and creates no work. The active map and exact cell
SHALL be rechecked; map change, game exit or loss of the sampling surface ends transient sampling.

#### Scenario: Hover fog and then valid ground
- **WHEN** the cursor passes over fogged terrain and then an unfogged floor
- **THEN** the fogged cell reveals no floor information, while the visible floor receives current appropriate feedback.

#### Scenario: Bad sample and map switch
- **WHEN** the player clicks natural soil, then switches maps while sampling
- **THEN** soil is rejected without changing terrain and sampling does not retain a target from the previous map.

### Requirement: Integrate with existing Architect controls

FPT04. The action SHALL remain in existing Architect / Floors and participate in ordinary command
ordering, search and admitted S036 paging. Successful selection SHALL reveal the actual selected
build command/group on its current Architect page once, without placing it or continually overriding
later manual page navigation. Current provider category/visibility rules remain authoritative.
Native colored-floor eyedropper behavior and existing Cancel, removal, painting and deconstruction
commands SHALL remain distinct and intact. Pick floor SHALL have no default keyboard shortcut,
avoiding the source's use of the existing Cancel-designation key; it SHALL not steal or rewrite
another command's binding. Normal cancel-mode input remains usable.

No new bottom main tab, bottom-right operational switch, mod setting or debug-only control SHALL be
added. Active sampling, selected floor and page reveal are transient UI state, not global/save
configuration. Labels, tooltips and unavailable reasons SHALL support all seven baseline languages.

#### Scenario: Paginated floor palette
- **WHEN** the sampled floor belongs to a command group on another Architect page
- **THEN** that group is revealed once with the right floor active; subsequent paging does not place floors or repeatedly snap back.

#### Scenario: Existing Cancel key and removal tools
- **WHEN** the player uses the original Cancel-designation binding or selects Remove floor
- **THEN** that command keeps its normal action; Pick floor has no competing default binding and sampling never acts as removal.

### Requirement: Admit the sampler without disturbing other commands

FPT05. The sampler integration SHALL check its documented semantic requirements at startup and
add its entry once while preserving existing Floors commands and unrelated additions. Compatible
modifications are allowed; no exact game hash or absence of foreign patches is required. Missing,
ambiguous or incompatible requirements SHALL leave the affected sampler inactive with one bounded
startup warning per skipped enabled patch identifying target/spec, reason, unavailable action and
ordinary Architect fallback. No half-working command or per-hover warning loop SHALL remain.

With original Floor Pipette Tool active, the owned overlapping sampler SHALL be withheld with an
explanation, preserving the original provider and native Architect. Unknown optional command
providers SHALL not be bypassed through a newly fabricated build tool. No external package identity,
source settings migration or permanent sampled-floor state is supplied.

#### Scenario: Other commands and repeated startup
- **WHEN** compatible mods add Floors commands or owned initialization repeats
- **THEN** their commands remain and at most one owned sampler is present.

#### Scenario: Unsupported provider or original source
- **WHEN** a required construction-selection rule is unsupported or original Floor Pipette Tool is active
- **THEN** the affected owned sampler is withheld, logs explain why, and remaining Architect controls keep working.
