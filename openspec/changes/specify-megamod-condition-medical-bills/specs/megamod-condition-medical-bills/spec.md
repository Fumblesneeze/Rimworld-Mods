# Condition-driven medical bill filtering

Sole owner **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
Origin original; S137. Draft for later implementation. The ancestor-availability extension is a
documented proposal awaiting HBF-Q2 resolution, not a claim of existing game behavior.

## ADDED Requirements

### Requirement: A condition selects a contextual operation filter

The Health tab SHALL provide a searchable medical recipe pane alongside visible conditions when
the current patient permits medical orders. Clicking a condition SHALL select it, show its name
and body-part identity as an active filter, and restrict recipe choices to relevant operations.
Opening the pane from Health overview SHALL retain access to the condition list. Selecting a
condition SHALL neither add a bill nor change health, care priorities, drug policies or work.
Existing queued bills SHALL remain visible through the ordinary Operations controls and SHALL
not be hidden, reordered or cancelled by the recipe filter. No new main tab SHALL be added.

#### Scenario: Find an operation without ordering it
- **WHEN** the player clicks a visible condition while examining a living patient's Health tab
- **THEN** the recipe pane identifies that condition and displays matching choices without changing the patient's health or bill queue.

#### Scenario: The patient cannot receive medical orders
- **WHEN** the player examines a corpse or another health view that disallows medical orders
- **THEN** no orderable recipe pane is offered and the existing information and combat-log controls remain usable.

### Requirement: Selection preserves exact condition identity

Selection SHALL distinguish the actual condition and body part, including left and right instances.
A grouped condition row SHALL select the currently visible conditions represented by that row and
show their count and individual details; its results SHALL be the union of their matches, without
duplicate operation-and-target choices. A folded replacement label SHALL select that replacement,
not every injury on the same part. Info, care and Combat log controls SHALL consume their own
gestures without also changing the filter. With this feature active, a separate labelled Combat
log action SHALL retain navigation to available events; multiple events SHALL be individually
identifiable. When the feature is unavailable, ordinary row-click navigation SHALL remain intact.

#### Scenario: Match the correct leg
- **WHEN** the player selects an infection on the left foot
- **THEN** operations affecting only the right foot or right leg do not appear as matches for that infection.

#### Scenario: Select grouped wounds
- **WHEN** the player clicks a row representing several visible wounds
- **THEN** the filter shows those wounds and offers each matching operation-and-target choice once, with the affected members identifiable.

#### Scenario: Select a folded bionic replacement
- **WHEN** the player clicks a bionic leg's replacement label
- **THEN** the relevant remove-part choice targets that bionic leg, without selecting an unrelated wound on it.

#### Scenario: Inspect a wound event
- **WHEN** the player uses the separate Combat log action for an available wound event
- **THEN** the native log opens and highlights that event without also creating or changing a medical filter.

### Requirement: Known effects determine relevance

Known matches SHALL identify operations whose successful ordinary effect removes or changes a
selected condition, restores its missing part, or removes or replaces its containing body part in
a way that affects it. Matching SHALL account for the entire affected descendant anatomy and for
conditions explicitly retained through part restoration or removal. Sharing a body part alone
SHALL not prove relevance. A whole-body operation SHALL match only the conditions it actually
affects. Explanations SHALL distinguish condition removal, level change, restoration and loss of
the containing part; a destructive or worsening effect SHALL not be described as a recommended cure.
An operation affecting only another identical condition SHALL not be claimed to affect the selected
instance. Existing failure risks and incomplete/conditional outcomes SHALL remain apparent.

#### Scenario: Remove quills
- **WHEN** the player selects a visible porcupine-quill condition and its whole-body removal operation is available
- **THEN** that operation appears once and explains that it removes the matching visible quills, rather than offering a separate bill per quill.

#### Scenario: Replace a part containing a condition
- **WHEN** an available replacement operation restores a part and clears a selected condition on its descendant
- **THEN** it appears with an explanation naming the replacement and affected descendant, while a condition preserved by that operation is not claimed to be cleared.

#### Scenario: A local implant leaves the selected injury intact
- **WHEN** an operation adds an implant on the selected injury's body part but does not affect that injury
- **THEN** it is not a known match merely because the target part is shared.

#### Scenario: Repeated implants have different actual targets
- **WHEN** an operation's successful effect removes the first matching implant elsewhere instead of the selected identical implant
- **THEN** it is not presented as a confirmed removal of the selected instance.

### Requirement: Ancestor amputation has an explicit limited availability extension

The proposed extension SHALL offer removal of a present ancestor limb for a visible harmful
condition on its descendant when that condition is either not an injury or is a permanent injury,
the ancestor permits suggested amputation, and it is neither the body's core nor an internal organ.
The relevant part-removal operation SHALL already belong to that patient's available medical
repertoire. Only the ordinary requirement that the qualifying condition lie directly on the target
part SHALL be broadened; other patient, content, research, ingredient, permission and provider
restrictions SHALL remain binding. The choice SHALL explicitly identify the entire part lost,
including the affected descendant, and SHALL use normal medical ordering and consequences.
The extension SHALL be selection-independent: the same choice SHALL remain in the unfiltered
catalog while its qualifying condition exists. No missing, unrelated or protected part SHALL be
made removable by this rule. An unsupported extension SHALL leave ordinary matching usable and
clearly report its unavailability rather than claiming the example is supported.

#### Scenario: Infection on a foot makes its containing leg discoverable
- **WHEN** a patient has an infection on a present foot and the containing leg satisfies these conditions
- **THEN** selecting the infection offers amputation of that leg, explicitly explains that the leg and foot are lost, and leaves surgery unordered until the player chooses the operation and completes any confirmation required by its ordinary workflow.

#### Scenario: A temporary bruise does not authorize broader amputation
- **WHEN** a descendant's only condition is a non-permanent injury
- **THEN** the extension does not add a containing-limb amputation for that injury.

#### Scenario: Filtering does not create a permission bypass
- **WHEN** an ancestor is protected by an applicable provider restriction or the patient cannot receive the operation
- **THEN** clicking the descendant condition does not enable that operation.

### Requirement: General matching and researched exceptions share one presentation

The filter SHALL support researched exceptions for effects that general rules cannot describe,
including exclusions of misleading matches. Specific validated corrections SHALL take precedence
over general inference. Where descriptive operation metadata identifies the selected condition
and a compatible target, a bounded heuristic SHALL be allowed. Similar names, a shared part or an
unrelated ingredient alone SHALL NOT qualify. Unsupported operations remain in the unfiltered catalog.
Conflicting equally specific corrections SHALL exclude the ambiguous match and produce a diagnostic,
without changing the operation or claiming that every modded cure can be discovered automatically.

Matches SHALL use one presentation regardless of whether relevance came from a general rule,
a heuristic or a researched exception. There SHALL be no confidence groups, badges, filters or
confidence-based ordering. Ordinary operation descriptions and consequences remain visible.
No matching rule SHALL override ordinary permission or perform a trial treatment to discover effects.

#### Scenario: An unfamiliar operation declares a relevant condition
- **WHEN** an available unfamiliar operation describes removal of the selected condition on the appropriate target and qualifies through the heuristic
- **THEN** it appears in the same list and style as other relevant operations, without a confidence label.

#### Scenario: A researched exception corrects a misleading match
- **WHEN** a supported content-specific correction says an inferred operation leaves the selected condition intact
- **THEN** that false match is excluded and the ordinary unfiltered catalog is unaffected.

#### Scenario: Two corrections disagree
- **WHEN** equally specific applicable corrections disagree about an operation's effect
- **THEN** the ambiguous match is excluded with an internal diagnostic, while the operation stays available in the unfiltered catalog.

### Requirement: Search and clearing have predictable scope

Text search SHALL match displayed operation labels, target labels and content-provider names without
case sensitivity, and SHALL intersect the current condition filter. Selecting another condition
SHALL replace the selection while preserving text search. Clicking the same selected row again or
using Clear condition SHALL remove only the condition filter; clearing search SHALL remove only
the text constraint. Matches SHALL use displayed-label ordering and stable target distinctions,
independent of how relevance was established. No-result text SHALL distinguish no relevant match
from matches hidden by text search and SHALL provide the applicable clear action. Neither state
SHALL assert that the condition is incurable.

#### Scenario: Search narrows relevant operations
- **WHEN** the player selects a condition and enters a search that excludes all its matches
- **THEN** the pane identifies the search restriction and offers to clear search without losing the condition selection.

#### Scenario: Return to the complete catalog
- **WHEN** the player clears the condition filter
- **THEN** all ordinarily available operation choices return, subject only to any remaining text search, and existing bills remain unchanged.

### Requirement: Browsing and ordering use current patient state

The pane SHALL update when patient health, visible conditions, anatomy, recipe availability,
ingredients or effective content policy change, including while paused. Selection SHALL retain
only the originally selected instances that still exist and are visible; it SHALL not silently
select newly appearing conditions with the same name. If none remain, the filter SHALL clear with
an explanation. Changing patient, closing the health view, changing game or loading a save SHALL
clear its transient selection and search. Immediately before an operation is ordered, its current
patient, target, availability and filter relevance SHALL be checked again. Stale choices SHALL
refresh without ordering an operation for another patient or body part.

#### Scenario: Treatment removes the selected condition
- **WHEN** the selected condition disappears while the pane remains open
- **THEN** the filter clears and explains the change without selecting a new same-named condition.

#### Scenario: A grouped selection loses one member
- **WHEN** one selected wound disappears and another remains
- **THEN** the selection count and matches update for the remaining selected wound without absorbing new wounds.

#### Scenario: Availability changes while paused
- **WHEN** an ingredient or permission change invalidates a previously displayed option before it is clicked
- **THEN** the click refreshes or rejects that option with its current explanation and creates no stale bill.

### Requirement: Explicit choices retain ordinary medical consequences and ownership

Selecting a recipe SHALL use its normal confirmation, secondary-choice and bill-creation workflow,
including warnings about skills, beds, care restrictions and faction consequences. A visible
disabled option SHALL retain its ordinary reason. Filtering SHALL not reveal recipes ordinarily
hidden by research, content policy or missing required ingredients, except for the explicitly
specified ancestor-target extension. A warning that normally permits queuing SHALL not become
a new prohibition merely because no doctor or bed is currently ready. Work, materials, failure,
anesthesia, health effects and automatic medical-order ownership SHALL remain unchanged.
An explicit player bill SHALL use the common medical-order policy and SHALL not acquire duplicate
automatic drug or quill orders because it came from this pane. Selecting a condition alone SHALL
never create an automatic dose request, cancellation hold or care override.

For whole-body quill removal, an existing matching bill, including a suspended bill, SHALL make
the recipe choice show **Already queued** and navigate to that bill instead of adding another.
Navigation SHALL preserve its position, suspension, ownership and automatic origin. Editing or
reordering an automatic bill through its ordinary controls SHALL transfer it to player control
while retaining automatic origin. Subsequent deletion or suspension SHALL preserve S134's patient
hold until explicit Resume quill removal. If no matching bill exists, the ordinary action SHALL
add one player bill without automatic origin; equivalent automatic scheduling SHALL then be blocked
by that existing bill. This pane SHALL not resume a suspended bill or clear an automation hold.

#### Scenario: Confirmation is cancelled
- **WHEN** the player selects a relevant operation and cancels its normal confirmation
- **THEN** no bill is added and the selected condition remains only a browsing filter.

#### Scenario: A warning permits ordinary queuing
- **WHEN** the ordinary operation can be queued but warns that no suitable doctor is currently available
- **THEN** the same warning and queuing behavior are retained, with no doctor or work created by the filter.

#### Scenario: An automatic quill order already exists
- **WHEN** one automatic quill-removal bill exists and the player clicks Already queued in the filtered pane
- **THEN** the queue still contains that one bill with unchanged position, ownership and origin, and its ordinary controls are brought into view.

#### Scenario: Take control of an existing automatic quill order
- **WHEN** the player edits that automatic bill through the displayed controls and later deletes it
- **THEN** editing makes it player-controlled with automatic origin retained, and deletion leaves no matching bill and sets the persistent S134 quill-automation hold until explicit Resume.

#### Scenario: Queue quill removal manually
- **WHEN** no matching quill bill exists and the player chooses its available removal operation
- **THEN** one ordinary player bill is added without automatic origin, and S134 does not add an equivalent automatic bill while it exists.

### Requirement: Unavailable integrations leave usable health controls

Compatible unrelated modifications SHALL not disable the feature solely because they are present.
When a required health-view or operation contract is missing or incompatible, the affected feature
group SHALL remain inactive and startup logs SHALL identify the skipped behavior, reason and
fallback once per enabled skipped patch. An unsupported effect mapping SHALL affect only that
mapping. An unverified overlapping external health renderer or recipe pane SHALL retain control
while the owned overlap is withheld with an explanation. No partial click interception, double
ordering or forced removal of another mod SHALL occur. All added controls, reasons and operation
text SHALL be readable in all seven baseline languages and at supported UI sizes. Selection is
temporary patient-view state, not a global or per-save setting.

#### Scenario: An unrelated compatible patch is installed
- **WHEN** the required health and ordering behavior remains available alongside that patch
- **THEN** condition filtering remains usable rather than being rejected because another mod patched the same method.

#### Scenario: The owned pane cannot safely integrate
- **WHEN** an incompatible renderer prevents safe condition selection and native ordering
- **THEN** startup explains the withheld filter and the existing health, information, combat-log and ordinary operation paths remain usable.

#### Scenario: Optional quill content is absent
- **WHEN** the content supplying quills and their operation is absent
- **THEN** no quill-specific choice is invented and other condition filtering remains usable.
