# Most skilled production workers

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`). Target: **1.7 beta**.
Origin: replacement, Most Skilled Bill Restriction. Specification only; nothing implemented.

## ADDED Requirements

### Requirement: Choose a dynamic worker restriction in bill details
The mod SHALL offer Most skilled alongside the ordinary worker choices on supported production bills with a relevant work skill.
Selecting it replaces a named-worker, slave-only, mech-only or non-mech choice; selecting an ordinary
worker choice disables it. Include away and Ignore if downed both begin off and remain dormant when
Most skilled is disabled. Newly created individual bills retain ordinary worker defaults unless the
destination's explicit workbench policy supplies this choice. Recipes without a meaningful work skill
show why Most skilled is unavailable and retain their previous assignment. These are contextual bill
choices, not mod settings. The allowed skill range remains visible/editable, initially the native 0–20;
it is never a hidden leftover restriction underneath the Most skilled controls.

#### Scenario: Select and leave Most skilled
- **WHEN** a bill assigned to one pawn switches to Most skilled and later to Any worker
- **THEN** the named assignment is replaced, then the dynamic restriction is disabled
- **AND** the two optional choices remain available if Most skilled is selected again.

#### Scenario: No work skill
- **WHEN** the player inspects a production recipe with no relevant work skill
- **THEN** Most skilled is unavailable with a reason rather than appearing active but doing nothing.

#### Scenario: Existing skill range
- **WHEN** a bill naming a level-fifteen worker retains a zero-through-ten range and switches to Most skilled
- **THEN** that range stays visible and the benchmark is drawn from eligible workers within it.

### Requirement: Rank suitable workers by current skill level
The mod SHALL use the highest current level of the recipe's work skill among its benchmark workers, allowing all tied workers and rejecting new starts by workers below that level.
The local benchmark includes living spawned player free colonists, colony slaves and supported
controlled colony mechs on the bill's map. Use ordinary current skill level, or a work-capable mech's
fixed skill level. Animals, prisoners, subhumans and non-player visitors are not silently added to this worker
population. A candidate must fall within the visible allowed range, be capable of the recipe and its
skill requirements, and have the relevant work enabled. A work assignment that merely shares the
same skill but cannot perform this recipe does not qualify. A mech must actually support that work;
its fixed level is not general permission to perform every recipe.

Busy, sleeping, drafted or temporarily unreachable otherwise qualified workers still set the
benchmark; this protects the selected skill standard rather than choosing the fastest free worker.
Ignore if downed and Include away refine it as specified below. Ranking is not by passion, fractional
experience, inspiration, predicted quality or work speed. A valid empty benchmark shows No eligible
benchmark and adds no skill minimum; all ordinary start restrictions still apply. Manually ordering
an otherwise eligible higher-skilled worker remains possible even if their work was not enabled for
automatic selection; forcing a below-benchmark worker does not bypass Most skilled.

#### Scenario: Tied experts
- **WHEN** eligible workers have relevant skill levels fourteen, fourteen and ten
- **THEN** either level-fourteen worker can start, while the level-ten worker receives the Most skilled rejection.

#### Scenario: Different work shares a skill
- **WHEN** a higher-skilled pawn is assigned only to a different work type that cannot perform the recipe
- **THEN** that pawn does not raise this recipe's benchmark merely because both jobs use the same skill.

#### Scenario: Qualified mech and ordinary gates
- **WHEN** a supported colony mech can perform the recipe with fixed skill ten and a suitable colonist has skill twelve
- **THEN** the benchmark is twelve and the mech cannot start through Most skilled
- **AND** a mech incapable of the recipe never raises the benchmark through its fixed level.

#### Scenario: Busy expert and manual order
- **WHEN** the benchmark expert is sleeping and a lower-skilled pawn is manually ordered to work
- **THEN** the lower-skilled start is rejected; the rule does not silently relax for immediate throughput.

### Requirement: Make downed-worker behavior explicit
The mod SHALL retain otherwise qualified downed workers in the benchmark while Ignore if downed is off, and exclude them while it is on.
This option changes only the benchmark, not a downed pawn's ability to work. A worker's downing or
recovery changes the next displayed benchmark and new-start decision, including inspection while
paused. Mental state, sleep, travel and imprisonment are not synonyms for downed; their own scope
and eligibility rules apply. Show the current benchmark and why a nonworking expert still determines it.

#### Scenario: Preserve the default standard
- **WHEN** a level-sixteen expert is downed, another qualified worker has level-twelve skill and Ignore if downed is off
- **THEN** the benchmark remains sixteen and new level-twelve work waits.

#### Scenario: Enable fallback and recover
- **WHEN** Ignore if downed is enabled in that situation and the expert later recovers
- **THEN** the benchmark first falls to twelve and later returns to sixteen for new work.

### Requirement: Away experts belong to their home colony
The mod SHALL let Include away add otherwise qualified living player workers associated with the bill's home map while away or temporarily unavailable there.
Use the shared home-association rules: leaving a player home records it; an available arrival at any
player home clears the away association; a non-home visit does not reassign it. Include supported
caravans, transporters, temporary other-map visits and unavailable home containers. A worker with no
known home cannot raise an unrelated colony's benchmark. Death or faction loss removes its
contribution. A lost origin is unresolved, not replaced with the camera's viewed map.

Remote/contained candidates still need the same recipe capability, work assignment and skill-range
qualification, ignoring only their location/unavailability for this benchmark. Supported controlled
mechs retain an explicitly known owner and actual work capability while away; an ordinary animal or
uncontrolled mech is not made a worker. Ignore if downed also applies to away candidates. Explain
that including a better absent expert can intentionally stop local production until return or an
explicit choice change. Include away does not schedule remote work or move a pawn.

#### Scenario: Expert travels
- **WHEN** a home's level-sixteen crafter leaves in a caravan and its best local qualified crafter is level twelve
- **THEN** Include away off uses twelve; on retains sixteen and can block the local worker.

#### Scenario: Visit and change home
- **WHEN** that expert visits a non-home map and later arrives available at another player home
- **THEN** the visit retains the original association, while arrival ends its old home's away contribution.

#### Scenario: Contained or unknown origin
- **WHEN** a qualified expert enters a supported unavailable container at home, or an unrelated remote pawn has no recorded home
- **THEN** Include away can retain the contained expert for its home but never guesses the unrelated pawn's origin.

### Requirement: Preserve native eligibility and committed work
The mod SHALL apply Most skilled as an additional restriction on new work, never as permission to bypass ordinary recipe, worker, DLC, mechanitor, access or reservation requirements.
Changing skill, work assignment, range, away/downed choices or benchmark membership affects the next
new-start decision and visible explanation together. An unavailable benchmark is distinct from a
valid empty benchmark and blocks new affected work. Existing committed work keeps its actual worker,
materials and progress under ordinary continuation rules. An expert's arrival or skill increase cannot
strand a lower-skilled creator's already committed unfinished item or prevent collection of completed
autonomous output solely because the new-start benchmark rose. No free output, replacement worker,
skill gain, quality guarantee or recipe cost/yield change is introduced.

#### Scenario: Skill changes while inspecting
- **WHEN** another eligible worker improves their relevant skill while the bill is being inspected
- **THEN** the next benchmark display and next new-start decision use the same updated maximum.

#### Scenario: Existing unfinished work
- **WHEN** a newly arrived expert raises the benchmark after another worker has committed an unfinished product
- **THEN** the original eligible creator can finish that physical work under native rules, but future starts use the higher benchmark.

#### Scenario: Native requirement still fails
- **WHEN** a highest-skilled pawn lacks a recipe's required mechanitor capability or cannot access its work
- **THEN** Most skilled does not override that ordinary rejection.

### Requirement: Share the complete worker policy across the intended plan
The mod SHALL carry Most skilled, Include away, Ignore if downed and the allowed range as a complete worker choice in S014 linked lists and S017 workbench policy/snapshots.
Each linked logical row has one choice and benchmark, not one independently selected worker per bench.
An enabled Restrict workbench policy applies the choice to all supported rows and new additions;
each recipe still uses its own relevant work skill and eligible benchmark. Destination policy takes
precedence on paste and is shown in its preview. Disabling bench-wide propagation retains each row's
effective choice. Linked rows continue to synchronize completely. Unsupported families or missing
required references reject the complete affected edit/paste instead of silently dropping the rule.
Copy one/all captures the flags at copy time, with no stale values from an earlier copy, source
edits, another save or a different paste route. Copies do not clone physical work or home ownership.

#### Scenario: Different skills under one bench policy
- **WHEN** a restricted linked production plan contains recipes using different work skills
- **THEN** all rows inherit Most skilled and its options, but each uses the highest eligible level of its own skill.

#### Scenario: Copy all and destination override
- **WHEN** a list with different Most skilled choices is copied and pasted into a plan with its own enabled worker policy
- **THEN** the preview explains that policy's override and commits the complete resulting list atomically.

#### Scenario: Snapshot remains independent
- **WHEN** the source bill's options change after copying
- **THEN** the stored snapshot remains the copy-time choice until explicitly copied again.

### Requirement: Scope the controls to the actual bill
The mod SHALL render the worker choice, visible skill range, two options and benchmark explanation only in the supported bill's contextual controls.
Changing game selection while a dialog remains open cannot redirect edits to another bench. Other
dropdowns, Assign-tab policy painting, unrelated ranges and native actions retain their behavior.
No global mod setting, bottom tab or bottom-right toggle is added. All labels, scopes, rejection and
recovery messages use the required language support and remain usable at supported UI scales.

#### Scenario: Unrelated policy painting
- **WHEN** the player drag-applies apparel, food or drug policies in the Assign tab
- **THEN** the ordinary policy actions and labels work without invoking Most skilled behavior.

#### Scenario: Selection changes during editing
- **WHEN** a bill dialog remains open while another building is selected
- **THEN** its worker edits still apply to the original bill and its declared linked/bench scope.

### Requirement: Persist and recover without silently broadening workers
The mod SHALL preserve owned choices across save/load, supported minification/reinstallation, copying and linked membership transitions, with no state leakage across saves.
A move requiring home/map rebinding uses explicit recovery before new work. If required support is
incompatible or the original source is active, withhold our overlapping behavior, retain saved choices
and suspend affected owned new work. Startup logs identify skipped enabled patches and unavailable
behavior with a bounded warning; benign unrelated patches alone do not reject support. Returning
support requires explicit Resume, or the player can deliberately replace Most skilled with an
explained ordinary choice. Missing references or failed evaluations are never a valid zero benchmark.
Unrelated ordinary bills continue normally. No automatic foreign save conversion is implied.

#### Scenario: Restore a saved restriction
- **WHEN** a supported owned bill is saved, reloaded and reopened after reinstallation on its map
- **THEN** its worker choice/options/range return and its benchmark is recomputed from current workers.

#### Scenario: Failed support at startup
- **WHEN** an owned saved restriction cannot be supported safely at startup
- **THEN** it remains visible and recoverable, no unrestricted new work starts, and the startup log identifies the reason.

#### Scenario: Explicit recovery
- **WHEN** support returns or the player deliberately selects an ordinary worker choice
- **THEN** work can resume with the visible intended policy without resetting stock, skills or physical progress.
