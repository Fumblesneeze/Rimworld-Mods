# Developer query inspection

Owner **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`; **1.7 beta**.
S121 TD Find Lib - Continued. Specification only.

## ADDED Requirements

### Requirement: Keep developer inspection separate from ordinary play
Developer queries SHALL be entered through a labeled top-debug-toolbar control only while developer
mode is enabled. They have their own workspace and saved collection, clearly marked as potentially
revealing hidden information. Default knowledge remains ordinary; a separate explicit Reveal hidden
information choice applies only to this developer workspace. Turning developer mode off disables
inspection/evaluation and hides the entry point; ordinary queries and production counts remain
unchanged. Debug definitions cannot be sent to ordinary search, production, hauling or another
ordinary consumer, even if a particular snapshot happens to reveal nothing hidden. No bottom tab,
ordinary custom-expression option or silent elevation through imported content is supplied.

#### Scenario: Toggle god mode with both workspaces present
- **WHEN** god mode changes while an ordinary search and developer query exist
- **THEN** ordinary visibility/counts remain unchanged and developer disclosure still follows its explicitly chosen scope.

### Requirement: Provide bounded non-mutating inspection
Developer inspection SHALL offer selectable status, runtime class, drawing category, fogged held
cell and a custom chain of supported readable values. Class choices match the selected class and
its subclasses, initially the ordinary Thing class. Other exact drawing-class choices require
selection. Custom chains support readable fields, explicitly admitted read-only properties,
component selection, type narrowing, admitted read-only helper observations and Any-member traversal
of known finite collections. Wrong-type or null intermediate paths do not match; a final explicit
null comparison can match a null value. Cycles or traversal limits produce a visible incomplete
evaluation, not a partial authoritative result or an unresponsive game.

No custom query can assign a field, invoke a setter, issue game actions or execute an unreviewed
getter/helper merely because its name is selectable or imported. Unsupported paths remain visible
as unavailable with the original definition retained. Private observations require an additional
explicit private-inspection choice and the same non-mutating admission; god mode alone does not
grant execution. An error or exhausted bound stops the affected evaluation without continuing as
if the failing clause were disabled. Imported expressions do not execute on preview or load.

#### Scenario: Inspect a cyclic holder graph
- **WHEN** a collection path cycles or exceeds the admitted inspection bound
- **THEN** evaluation stops with an incomplete reason and no authoritative count, without hanging the game or silently returning its partial matches.

#### Scenario: Import a named property with unknown side effects
- **WHEN** a custom expression names a property whose read-only behavior is not admitted
- **THEN** it remains unavailable and the property is not invoked, including during suggestion generation.

### Requirement: Preserve explicit final comparisons
Custom comparisons SHALL offer Boolean true/false, inclusive integer/decimal ranges, literal
case-sensitive substring or exact text, enum choice, required/prohibited flags, exact content identity
and null/not-null. Initial Boolean comparison is true, numeric interval 10–15, text empty substring;
enum/content choices require selection. Required flags must all be present and prohibited flags
absent. Outer exclusion negates the completed predicate, not an unavailable expression. Each chain,
comparison, unit and operand survives debug copy/save/reload without becoming an ordinary query.

#### Scenario: Compare literal text
- **WHEN** the debug comparison searches for the literal text Ab using case-sensitive substring
- **THEN** xAby matches and xaby does not; switching to exact text matches only Ab.

### Requirement: Provide the six source inspection examples honestly
The debug example importer SHALL offer the following explicitly labeled, editable templates under
the same independent-copy, collision and atomic persistence rules as the ordinary library. All use
Current map. Each remains unavailable until every observation it uses is admitted for the active
game/provider; the template itself is not a compatibility guarantee. No template changes state.

| Example | Complete observation |
| --- | --- |
| Low battery and fuel fractions | Building with battery stored-energy fraction 0–20% AND refuelable target-fuel fraction 0–20%; both required, not alternatives |
| Very low-output generators | Building with a power-plant observation and current power output 0–1 |
| Cooking bill currently worked | Pawn's current job uses a bill whose recipe's work skill is Cooking; other cooking-related jobs do not match automatically |
| Ideology reform available | Pawn's ideology currently reports reform availability |
| CE loadout includes a weapon | Pawn's Combat Extended loadout has a slot for a weapon type; not proof the pawn carries it |
| HAR non-Human pawn type | Pawn has the supported Humanoid Alien Races type and is not exact Human; not every modded humanoid |

The CE template is deferred and unavailable throughout initial support; HAR remains unavailable without separately admitted provider observations;
this change does not claim CE/HAR compatibility or automatically execute their helper functions.
Deleting examples does not cause their recreation. Example names and comparisons remain inspectable
even when their provider is missing.

#### Scenario: Low power without a fuel component
- **WHEN** a building has a low battery fraction but no refuelable observation
- **THEN** the combined Low battery and fuel fractions example does not match; its label does not suggest an OR test.

### Requirement: Keep developer observations observational
Executing or refreshing a debug query SHALL not mutate game state, advance time, spend resources,
change pawn jobs or bypass native selection/action restrictions. Observed values are current to
the completed evaluation; results disclose when evaluation did not complete. Debug caches and
temporary references are cleared on map/game teardown and cannot affect ordinary shared facts.
Admission failures follow the foundation's bounded startup warnings and local failure isolation.
No inspected or imported code is trusted because it came from a saved query or a Workshop package.

#### Scenario: Inspect without side effects
- **WHEN** an admitted debug query observes job, fuel and ideology values while the game is paused
- **THEN** those values can be inspected without creating a job, consuming fuel, reforming an ideology or advancing simulation time.
