# Compact health display

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`). Target: 1.7 beta.
Source: Compact Hediffs. Specification only; health mechanics are unchanged.

## ADDED Requirements

### Requirement: Compact presentation remains in the existing health view

Compact presentation SHALL operate in the pawn's existing health-condition list, preserving the
surrounding health and operation controls. A small bottom-right view button SHALL switch between
compact and ordinary presentation and visibly indicate its state. The initial default SHALL be on.
The current choice SHALL persist per save; saves without a choice SHALL use the effective configured
default. Turning it off SHALL restore ordinary row layout, ordering and tab size together without
changing health, treatment, surgery orders or other gameplay state. No new bottom tab SHALL be added.

#### Scenario: Return to ordinary presentation
- **WHEN** the player switches compact view off while examining an injured pawn
- **THEN** the ordinary list and tab dimensions return with the same injuries and medical orders.

### Requirement: Fold replacements into identifiable body-part labels

With replacement folding enabled, initially on, a replacement part's name SHALL appear with the
affected body part's identity instead of as a redundant condition row. Side and position information
SHALL remain clear, and the replacement's tooltip and information card SHALL remain reachable.
Other conditions on that part SHALL stay visible. With folding off, the replacement SHALL return
to the condition list. Missing-part condition labels SHALL be italicized by default, independently
switchable. Whole-body conditions SHALL be clearly labeled separately from a specific part.

#### Scenario: Injured replacement left leg
- **WHEN** a pawn has a replacement left leg plus another visible condition on that leg
- **THEN** folding identifies both the replacement and left leg while retaining the other condition.
- **AND** the replacement's information can still be opened.

### Requirement: Group repeated conditions without losing individual meaning

Repeated conditions of the same kind on the same body part SHALL share a row with a count when
their identity is equivalent, even if some are tended and others are not. Matching detailed labels
SHALL be retained; differing treatment/stage suffixes SHALL use a shared base label with individual
details available. Different body parts and meaningfully different targets, such as different drug
dependencies, SHALL remain distinguishable rather than becoming an ambiguous shared label.
Grouping SHALL not combine or change actual conditions, severity, treatment or immunity.

#### Scenario: Tended and untreated cuts
- **WHEN** the same arm has three cuts, two tended and one untreated
- **THEN** the list can show one cut row with count three and retain the state and details of all three.

#### Scenario: Two different dependencies
- **WHEN** a pawn has dependencies on different substances
- **THEN** the display identifies each substance without requiring the player to infer it from an
  ambiguous combined dependency label.

### Requirement: Urgent treatment can determine display order

Treatment-priority sorting SHALL be enabled initially. It SHALL place body-part groups with the
most urgent condition eligible for tending apart from its current treatment timer first, using
native body-part order to resolve equal priority, and apply the same treatment priority within each
part. An otherwise eligible treated condition SHALL not lose its display priority solely because
its next tending time has not arrived. Disabling this option SHALL
restore native condition/body-part order while retaining other enabled compact formatting.
Display sorting SHALL not change a pawn's actual medical work priorities.

#### Scenario: A new urgent wound appears
- **WHEN** a pawn gains a more urgently tendable wound on a different part
- **THEN** the displayed order reflects that urgency while sorting is enabled.
- **AND** disabling sorting removes this display reordering for both parts and conditions.

#### Scenario: Severe treated condition and minor untreated wound
- **WHEN** a severe condition is still on its tending cooldown and a less urgent wound is untreated
- **THEN** the severe condition can retain the higher display priority based on native treatment
  urgency, while their individual tending indicators still distinguish the two states.

### Requirement: Individual bleeding and tended-wound indicators remain legible

Each displayed condition SHALL retain its own state indicator. Bleeding wounds SHALL each have a
bleeding icon, ordered by bleeding rate; tended wounds SHALL each retain their tending state, ordered
by quality. Enhanced bleeding and tending icons SHALL initially be on; disabling either enhancement
SHALL use the ordinary corresponding icon rather than hiding that state. Enhanced bleeding icons
SHALL use six increasing bands of blood loss per day: up to 10%, above 10% to below 25%, 25% to below
50%, 50% to below 125%, 125% to below 225%, and 225% or more. Enhanced tending icons SHALL use six
quality bands: up to 1%, above 1% through 25%, above 25% to below 50%, 50% to below 75%, 75% to below
100%, and 100% or more. Enhanced bleeding icons SHALL pulse faster as the wound's bleeding rate
increases while remaining visible throughout the pulse. Crowding SHALL not cover labels or make
individual details inaccessible.

#### Scenario: Quality at a band boundary
- **WHEN** two wounds have tending qualities of 25% and 50%
- **THEN** their indicators use the second and fourth quality bands respectively.

#### Scenario: Wounds bleed at different rates
- **WHEN** two displayed wounds bleed at different rates with enhanced bleeding icons enabled
- **THEN** each icon uses its own severity band and the faster-bleeding wound pulses faster.

#### Scenario: Many wounds in one row
- **WHEN** a body part has more wound indicators than fit comfortably beside its label
- **THEN** the label stays readable and all individual wound states remain accessible without overlap
  with adjacent rows or controls.

### Requirement: Body-part health bars reflect the actual part

Body-part health bars SHALL initially be enabled above the part's entry. A damaged part SHALL show
remaining health relative to its actual maximum, using the game's condition coloring; a part with
no remaining health SHALL be visibly distinct. Whole-body conditions SHALL not invent a body-part
health percentage. Full-health parts SHALL not display a misleading damage bar. Supported altered
body-part health limits SHALL use their actual maximum rather than a standard human assumption.

#### Scenario: Different maximum health
- **WHEN** a supported part has 30 remaining health out of an actual maximum of 60
- **THEN** its bar shows half remaining health regardless of the normal maximum for another pawn.

### Requirement: Severity and immunity bars communicate the correct scale

Visible conditions with meaningful progress SHALL show severity and, where applicable, immunity
without substituting one for the other. Severity SHALL be relative to the condition's applicable
lethal or finite maximum, with pregnancy using its complete gestation scale; a missing meaningful
maximum SHALL not produce an invented percentage. Immunity SHALL be relative to full immunity.
Bars SHALL remain within their allocated area when values reach or exceed the scale, while details
retain the actual value. A condition SHALL not be labeled life-threatening solely because its bar
is full. Ordinary mode SHALL respect the game's condition visibility and hidden-progress rules.

#### Scenario: Immunity leads disease
- **WHEN** a disease has 40% severity and 60% immunity
- **THEN** the display distinguishes both values and shows that immunity leads severity by 20 points.

#### Scenario: No meaningful maximum
- **WHEN** a visible condition has no usable maximum for severity progress
- **THEN** its ordinary label/details remain available without a fabricated severity bar.

### Requirement: Progress presentation offers clear selectable modes

Progress mode SHALL offer Off, Left to right, Edges to middle, and Middle to edges, initially Left
to right. Left-to-right comparison SHALL show the leading severity/immunity extent and distinguish
their difference; split modes SHALL show severity on the left half and immunity on the right half,
progressing in the selected direction. A condition with only one progress value SHALL use both
halves symmetrically in split modes. Bars SHALL be placeable above or below the condition label,
initially above. High contrast and patterns SHALL initially be enabled and separately switchable.
High contrast SHALL distinguish severity in red and immunity/treatment in green; ordinary coloring
SHALL retain condition cues. Patterns SHALL remain recognizable across the full filled extent at
different tab widths. Patterned bars SHALL distinguish malnutrition, blood loss, supported Anomaly
cube conditions and supported Pawnmorpher mutations from ordinary condition progress, while immunity
and cumulative treatment retain distinct treatment cues. Disabling patterns SHALL use solid bars
without changing values or labels. Off SHALL hide severity, immunity and cumulative-treatment bars together.

#### Scenario: Switch bar direction and position
- **WHEN** the player selects Middle to edges and Below
- **THEN** the same values extend outward from the middle below their corresponding condition label.
- **AND** switching to Off removes those progress bars without removing condition details.

#### Scenario: Condition-specific progress cues
- **WHEN** malnutrition and a supported cube or mutation condition are displayed with patterns enabled
- **THEN** their progress bars have distinct condition-specific patterns across the filled extent.
- **AND** disabling patterns replaces those patterns with solid bars while preserving their values.

### Requirement: Cumulative treatment does not reset visually at completion

Cumulative treatment progress SHALL initially be shown for conditions with a defined total
treatment requirement when progress bars are enabled. It SHALL compare accumulated treatment with
that requirement, separately from the latest tend quality and from immunity. Reaching or exceeding
the requirement SHALL display complete progress until the condition disappears, not an empty bar.
Tendable, non-permanent conditions on living pawns SHALL have a pulsing treatment cue, stronger when
their current treatment has expired; permanent conditions and dead pawns SHALL not imply a new
urgent treatment opportunity.

#### Scenario: Treatment exceeds the requirement
- **WHEN** a treatment takes cumulative quality above the required total and the condition remains visible
- **THEN** its treatment bar stays full and details show the actual accumulated treatment.

### Requirement: Blood-loss emphasis reflects severity rather than a fixed species assumption

Special blood-loss presentation SHALL initially be enabled. It SHALL color blood loss from pale
to red over the first 65% of its applicable severity scale and toward dark red/purple from 65% to
100%, with a distinct blood-loss pattern when patterns are enabled. Its progress SHALL use the
pawn's applicable blood-loss scale, including supported species differences. Disabling this option
SHALL restore ordinary condition coloring and progress style without altering blood loss.

#### Scenario: A pawn uses a different blood-loss scale
- **WHEN** supported game content gives a pawn a different blood-loss threshold
- **THEN** the displayed fraction and color reflect that pawn's threshold, not an assumed raw value of one.

### Requirement: Tooltips and native health navigation remain available

Body-part and condition tooltips SHALL preserve individual details, including those of folded
replacements and grouped wounds. Long combined details SHALL remain reachable without being lost
off-screen. Information controls SHALL open the corresponding condition or replacement information.
When condition-driven medical bill filtering is inactive, clicking an entry with an available
native combat-log link SHALL open and highlight that event. When that feature is active, primary
condition clicks SHALL select its recipe filter and a separate labelled Combat log action SHALL
open and highlight the available event without changing the filter. Multiple available events
SHALL remain individually identifiable; an entry without an event SHALL not invent one. Information
and optional care controls SHALL consume their own gestures without also changing the filter or
obscuring the other actions.

#### Scenario: Inspect the cause of a wound
- **WHEN** condition-driven filtering is inactive and the player clicks a wound entry whose combat-log event is still available
- **THEN** the native log opens at and highlights the corresponding event.

#### Scenario: Inspect a wound event alongside medical filtering
- **WHEN** condition-driven filtering is active and the player uses a wound's labelled Combat log action
- **THEN** the corresponding native event opens and is highlighted without changing the recipe filter or adding a bill.

### Requirement: Layout options remain readable and use shared settings

The ordered Health display settings section SHALL contain the initial-view default and presentation
options, using common global defaults and per-save overrides. New/missing overrides SHALL use global.
Use global SHALL remain available in a loaded save; the per-save value editor SHALL be disabled
while inherited. Both SHALL be disabled at the main menu while global editing remains usable.
Changes SHALL affect the next displayed view, except the initial-view default SHALL not overwrite
an already saved operational view choice. A selected-pawn preview SHALL show the effect without
changing the pawn; with no suitable pawn it SHALL explain how to obtain a preview.

Separators between columns, body-part rows and condition rows SHALL initially be on, using dark
separators; gray separators SHALL be selectable. Alternating row shading SHALL initially be off.
Extra tab width SHALL default to 100 UI units with a 0–300 range, and extra height to zero with a
0–200 range. The final view SHALL fit the available screen at the current UI scale, keep scrolling
and click targets usable, and account for every displayed bar and wrapped label.

#### Scenario: Configure globally from the main menu
- **WHEN** the player opens Health display settings without a loaded game
- **THEN** global controls work, save controls are disabled, and the preview explains that a pawn is needed.

#### Scenario: Multiple progress bars and long labels
- **WHEN** a grouped row contains several progress-bearing conditions at a narrow width
- **THEN** all bars and wrapped labels fit their rows and following entries remain separate and clickable.

### Requirement: Diagnostic information and unrestricted edits belong in developer controls

Show hidden health progress, remove condition, set severity and set remaining duration SHALL be
developer functions in the top debug toolbar, unavailable when developer mode is off. Hidden-progress
display SHALL initially be off and SHALL not expose otherwise undiscovered conditions. Edit actions
SHALL identify the selected pawn and exact condition instances, with group removal explicitly showing
which instances will be removed. Severity/duration editing SHALL be offered only where meaningful.
Ordinary health rows SHALL not embed these unrestricted override buttons.

#### Scenario: Normal play after leaving developer mode
- **WHEN** developer mode is turned off after inspecting hidden progress
- **THEN** debug controls and hidden-progress display disappear while ordinary health information remains.

### Requirement: Optional medicine and body-part support fails locally

Supported medicine integrations SHALL preserve their condition-level care choices and indicators in
the compact view. Supported mutation replacements SHALL retain their identity and extra information;
supported body-part health providers SHALL supply the actual maximum. Missing providers SHALL not
leave empty actionable controls. An incompatible adapter SHALL warn once at startup and omit only
its unsupported enhancement, preserving ordinary health information. When native information cannot
be represented reliably, the affected entry SHALL use ordinary presentation rather than misleading data.

#### Scenario: Optional medicine integration is unavailable
- **WHEN** the medicine provider is absent or incompatible
- **THEN** its extra care controls are absent and ordinary condition details and native health actions work.

### Requirement: Source co-installation and failed admission preserve the usable health view

With original Compact Hediffs active, megamod SHALL initially defer compact rendering to it with
an explanatory startup warning rather than replace each row twice. If megamod's required rendering
integration is incompatible, compact mode SHALL be unavailable with a bounded warning and ordinary
health display SHALL remain usable. Compatible unrelated patches SHALL not alone cause failure.
Removing or disabling this display SHALL not remove health conditions or medical orders.

#### Scenario: The native rendering contract changes
- **WHEN** required health-view behavior is no longer safely supported at startup
- **THEN** ordinary health presentation remains usable and the warning identifies the unavailable compact view.
