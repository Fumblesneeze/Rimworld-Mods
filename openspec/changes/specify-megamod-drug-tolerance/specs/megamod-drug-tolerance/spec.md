## Owner

Make Rimworld Great Again — `fumblesneeze.megamod`, `mods/megamod`; RimWorld 1.7 beta.
Implementation and scoped native acceptance are recorded in the owning tasks with exact tested
builds. The full feature and every remaining scenario still require their stated evidence.

## ADDED Requirements

### Requirement: Tolerance is an ordinary drug-policy choice
Make Rimworld Great Again SHALL provide a tolerance choice for each current addictive drug with
an understood tolerance effect in the native drug-policy window. The choice SHALL be Any or a
numeric percentage from 0% through 100%. Any is distinct from numeric 100%. New or newly restored
drug rows without a surviving saved choice SHALL begin at Any. The control SHALL identify the drug
and explain that it restricts routine scheduled and recreational use, not all consumption or risk.
When neither scheduled nor recreational use is enabled, the choice SHALL remain retained but
inactive with an explanation. Unavailable or inapplicable drugs SHALL not gain invented tolerance.

#### Scenario: Open a new applicable row
- **WHEN** the player opens a newly introduced addictive drug with a recognized tolerance effect
- **THEN** its choice is Any and the player can select a numeric limit in the same policy window.

#### Scenario: Turn off routine uses
- **WHEN** the player turns off both scheduled and recreational use on a row with a chosen limit
- **THEN** that limit remains visible as inactive and returns when either use is enabled again.

#### Scenario: Edit tolerance with the native row slider
- **WHEN** the player adjusts tolerance in the drug-policy row
- **THEN** it uses the same native horizontal slider control, rail, handle, label and vertical alignment as the adjacent mood and recreation sliders, without a separate button, dialog or Apply/Cancel step
- **AND** explicit slider interaction immediately commits a whole percentage from0 through100 or the separate rightmost Any stop; numeric100 and Any remain distinct
- **AND** drawing alone preserves every existing exact numeric value and malformed saved encoding; an explicit valid slider interaction can repair Needs review
- **AND** each commit revalidates the current Game, manager, selected policy, exact row/drug, original choice and effective availability, without retaining edit authority across frames.

### Requirement: Any and numeric thresholds have explicit meanings
With the feature enabled and available, routine use governed by a numeric choice SHALL require
current chemical tolerance to be strictly below that percentage. No current tolerance condition
SHALL count as zero tolerance. Equality SHALL reject use. Numeric zero SHALL reject this use even
when no tolerance condition exists. Any SHALL add no tolerance restriction, including at or above
100% tolerance. All ordinary permissions and other restrictions remain required. The comparison
uses the actual current value rather than a rounded display percentage.

#### Scenario: Boundary and absent tolerance
- **WHEN** an otherwise eligible pawn has exactly 20% tolerance and the row limit is 20%
- **THEN** it does not take that routine dose through this rule
- **AND** a pawn without that tolerance condition is also blocked when the limit is numeric zero.

#### Scenario: Any differs from numeric 100 percent
- **WHEN** an otherwise eligible pawn has 100% tolerance
- **THEN** numeric 100% rejects threshold-governed use while Any adds no rejection.

### Requirement: Scheduled use keeps every existing condition
Scheduled drug use SHALL satisfy ordinary permission, timing, mood, recreation, willingness,
ingestion and risk restrictions as well as the enabled tolerance limit. With S024 potential-mood
checking enabled, both its mood condition and the tolerance condition SHALL hold. A tolerance
choice SHALL not alter dose quantity, schedule interval, chemical effects, inventory targets or
consumption history. This feature SHALL not grant permission or promise addiction safety.

#### Scenario: Mood or interval still rejects
- **WHEN** tolerance is below the chosen limit but potential mood or the ordinary interval rejects a dose
- **THEN** the pawn does not take it through scheduled use.

#### Scenario: Tolerance has fallen
- **WHEN** tolerance falls below the chosen limit and every other scheduled-use condition is met
- **THEN** the pawn can obtain and consume the ordinary permitted dose with its normal effects.

### Requirement: Routine eligibility uses the next native selection boundary
The tolerance rule SHALL be evaluated when a native routine route selects an eligible drug or
creates its routine job. A later policy, tolerance or effective-setting change SHALL apply to the
next such selection, without cancelling or adding a second tolerance veto to an already selected
or executing native order. Native interruption, ingestion failure and save/resume behavior SHALL
remain authoritative. Shared ingestion, medical and manual drivers SHALL not acquire a blanket
tolerance veto or a new persisted purpose tag. S024 patient treats, when implemented, SHALL call
the owned rule with the actual recipient at their own treat-selection boundary; absence of S024
SHALL not disable the native scheduled, direct recreation and social-drinking routes.

#### Scenario: Policy changes after a routine job was selected
- **WHEN** a pawn already has a selected routine ingestion order and the player lowers its policy limit
- **THEN** the existing order follows ordinary native execution and interruption rules
- **AND** the new limit governs its next routine eligibility decision.

#### Scenario: Reload preserves an executing dose after a policy edit
- **WHEN** a pawn is consuming a routinely selected dose, the player applies a zero tolerance limit and saves and reloads
- **THEN** the serialized native job and physical dose may continue under ordinary native execution rules
- **AND** a fresh routine selection observes the retained zero limit rather than inheriting permission from the earlier job.

#### Scenario: Tolerance changes while carrying a selected dose
- **WHEN** tolerance reaches the chosen limit after a routine job selected its dose, including across save/load
- **THEN** this feature does not cancel that order or add a final-ingestion veto
- **AND** a subsequent routine selection observes the current tolerance and policy.

#### Scenario: Ordinary settings change around an executing scheduled dose
- **WHEN** native scheduled attempts are withheld by a retained numeric-zero choice and the player disables this feature through its common setting
- **THEN** ordinary native scheduling can select that dose when its other conditions permit
- **AND** re-enabling through the setting while that dose is being consumed does not cancel the existing order or add a final-ingestion veto
- **AND** the retained zero applies to subsequent native selections without rewriting the policy choice or save inheritance.

### Requirement: Recreation uses the recipient and retains native joy exceptions
Ordinary recreational drug use SHALL apply the recipient's tolerance choice in addition to its other recreation permissions.
Native exceptions for a positive drug-desire trait or a mental state SHALL retain their existing
direct self-recreation scope: this extra tolerance veto does not apply to those exceptions on that
route, but they grant no scheduled-use, social-drinking or patient-feeding exception. Social drinks
SHALL require the tolerance choice and ordinary joy permission without importing those exceptions.
When the choice rejects a social drink, otherwise eligible social activity without a drink SHALL
remain available. S024 patient treats SHALL retain required recipient joy-policy permission and add
the numeric tolerance veto without a new drug-desire or mental-state exception. Inventory-sourced
recreation SHALL retain the exceptions of its actual originating route. A visitor's choices SHALL
not grant permission to the patient.

#### Scenario: Visitor could take a drug that the patient cannot
- **WHEN** an eligible visitor selects a recreational drug whose tolerance limit rejects the patient
- **THEN** the visitor does not deliver that drug as a treat and can make an ordinary visit instead.

#### Scenario: Patient trait does not grant a new feeding exception
- **WHEN** a patient has a drug-desire trait but its numeric tolerance limit rejects the offered treat
- **THEN** S024 does not deliver that drug by extending a native self-recreation exception to feeding.

#### Scenario: Joy exception does not become scheduled permission
- **WHEN** a pawn qualifies for an ordinary direct self-recreation exception while its numeric tolerance limit is reached
- **THEN** that exception remains available only on that direct self-recreation route
- **AND** scheduled use remains subject to the tolerance choice.

#### Scenario: Social drink does not inherit a direct recreation exception
- **WHEN** an otherwise eligible social participant has joy permission for a drink, its numeric tolerance limit is reached, and it has a positive drug-desire trait or a mental state
- **THEN** that drink is withheld without importing a direct self-recreation exception
- **AND** otherwise eligible social activity without a drink remains available.

### Requirement: Medical and explicit orders retain their own authority
The tolerance preference SHALL not add a restriction to addiction relief, medical administration, S124 condition or pain responses, S134 dependency assistance, explicit manual orders or inventory refill.
Those routes SHALL retain their own permission, medical eligibility, quantity, claims and
actual-consumption rules. A routine-use choice SHALL neither authorize treatment nor create a
second pending dose. Merely carrying a drug SHALL not count as consuming it.
When a scheduled dose is withheld, any extra-stock pickup attached to that dose is withheld as
well; this feature does not promise a separate refill to replace it. Independently permitted refill
SHALL remain available.

#### Scenario: Needed dependency assistance
- **WHEN** routine use is blocked by numeric zero but S134 independently permits dependency assistance
- **THEN** this tolerance choice does not block or duplicate the ordinary permitted treatment.

#### Scenario: Manual order or carried supply
- **WHEN** the player gives an otherwise valid explicit ingestion order or the pawn refills permitted carried stock
- **THEN** this feature does not add a routine tolerance veto to that action or record consumption before it occurs.

#### Scenario: Blocked dose also withholds its attached pickup
- **WHEN** the tolerance choice rejects a scheduled dose that would also collect extra carried stock
- **THEN** that dose and its attached pickup do not occur, without a promised replacement refill trip
- **AND** independently permitted refill remains available under its ordinary rules.

### Requirement: Policy maintenance preserves deliberate choices
Tolerance choices SHALL remain save-local policy gameplay state, not inherited mod configuration.
Copying a policy SHALL copy its choices into independently editable rows. S029 maintenance SHALL
retain each surviving valid row's choice without guessing from its numeric value, policy name or
preset. Renaming, display sorting, default selection and pawn assignment SHALL not reset choices or
dose history. A drug removed from saved policy data and later returned SHALL start at Any; no
hidden restoration store is promised. Choices SHALL survive save/load of megamod colonies and
remain isolated between saves.

#### Scenario: Copy and edit independently
- **WHEN** the player copies a policy, changes one copied tolerance choice, saves and reloads
- **THEN** each policy retains its own choice and pawn assignments remain attached to the intended policy.

#### Scenario: Content repair and return after saved removal
- **WHEN** S029 repairs policies after a content change
- **THEN** surviving valid choices remain and genuinely new rows begin at Any
- **AND** a drug returning after its removal was saved does not recover a hidden old threshold.

### Requirement: Owned serialization failures cannot silently discard choices
Copy and Scribe support SHALL remain installed independently of the ordinary feature switch.
After admitted support encounters an owned serialization or support-generation failure, it SHALL
disable tolerance editing/enforcement, retain a bounded diagnostic and prevent successful native
save/load finalization for that process. Retrying SHALL require a restart, rather than silently
writing missing fields or treating them as Any. Ordinary successful copy/save/load and native
exceptions SHALL retain their existing behavior. A native callback which is skipped or fails
before owned serialization SHALL not commit owned state. Unsupported startup compositions SHALL
report persistence and feature availability honestly; arbitrary removal of every owned observer
or later exception-suppressing foreign code is outside this guarantee.
Once the native UI is available after the failed operation, one localized notice SHALL explain the
blocked save/load state, restart requirement and that native “Saved as” feedback does not prove
success after this error. Repeated attempts SHALL remain blocked without repeating this notice.

#### Scenario: Native row serializer catches an owned error
- **WHEN** the owned Scribe adapter fails and native per-row handling catches that exception
- **THEN** the enclosing save/load still fails before successful finalization, the tolerance controls become unavailable,
  and the diagnostic and one visible notice require a restart before retrying
- **AND** an existing save is not replaced with a supposedly successful file lacking owned choices.

#### Scenario: The first owned error occurs during load finalization
- **WHEN** native cross-reference or post-load initialization catches an owned row error inside load finalization
- **THEN** the native load still fails at its completion boundary and does not report a successfully loaded colony
- **AND** the owned notice requires restart, the source save remains intact, and an existing native exception is not replaced.
- **AND** if native failure handling leaves a non-playable partial game, restart remains required;
  the feature does not resume that game or report successful scene recovery. Native failure
  dialogs can be acknowledged before the owned restart notice is visible.

#### Scenario: Persistence publication becomes unreadable
- **WHEN** required persistence support is removed or its newly published metadata cannot be safely inspected
- **THEN** editing and enforcement remain unavailable and prior serialization authority cannot be reused to report successful save/load
- **AND** transient metadata-monitor contention withholds the current choice-use attempt without permanently faulting otherwise healthy persistence.

#### Scenario: Diagnostic instruction inspection is not a live receipt
- **WHEN** a diagnostic instruction inspection invokes an observer outside native patch publication
- **THEN** the missing compiled generation cannot authorize tolerance choices, even when the previously published metadata identity is unchanged.

### Requirement: Malformed saved choices are visible and conservative
The malformed-data behavior SHALL distinguish an absent choice from an invalid saved
choice. An absent choice defaults to Any. A recognized row containing an unreadable choice, an
unknown choice kind, a nonfinite number or a number outside 0–100% SHALL show Needs review and
withhold routine use governed by the tolerance choice until the player explicitly selects a valid
value. The native joy exceptions above retain their scope; other routes remain unchanged. Loading
SHALL not silently widen an invalid limit to Any, clamp it or rewrite valid neighboring rows.

#### Scenario: Invalid saved number
- **WHEN** a saved row contains an invalid numeric tolerance choice
- **THEN** the policy window identifies that row as needing review, threshold-governed routine use is withheld,
  and selecting Any or a valid number repairs that row without changing other choices.

#### Scenario: Older owned save lacks the new field
- **WHEN** an otherwise valid megamod policy has no saved tolerance choice
- **THEN** it receives Any without being reported as corrupt.

#### Scenario: Unknown saved schema remains repairable
- **WHEN** a saved tolerance choice uses an unknown schema or malformed encoding
- **THEN** its original value is retained without silent conversion, the row needs review,
  and an explicit valid choice repairs only that row.

#### Scenario: Current tolerance cannot be read
- **WHEN** an otherwise applicable tolerance reader cannot supply a valid nonnegative finite value
- **THEN** the feature reports that the extra restriction is unavailable and preserves ordinary
  native permission rather than treating the unknown value as zero or pretending to enforce a limit.
- **AND** the initial adapter gives a localized player message and diagnostic at most once per Game,
  retains the saved choice, and does not retain pawn permission or prevent later valid readings.

### Requirement: Feature configuration does not replace policy state
The feature's built-in enable default SHALL be on. Its enable switch SHALL use common
Global default, Use global and Current save controls in the Drug policies settings section. New
save entries inherit; inherited saves follow current global values, and independent or dormant
overrides remain intact. Main-menu save controls and their actions SHALL be unavailable. Turning
the feature off SHALL remove its extra restriction at subsequent eligibility decisions and disable
its policy editor with an explanation, while retaining policy choices. It SHALL not rewind a dose,
cancel an already executing native order or alter medical behavior. Turning it on SHALL restore
the retained choices at subsequent eligible decisions.

#### Scenario: Disable and re-enable in a loaded save
- **WHEN** the player disables the feature and later enables it
- **THEN** subsequent applicable use decisions follow the effective setting, and previously saved row choices remain.

#### Scenario: A dormant save override survives reload beneath an enabled global default
- **WHEN** the player stores Current save off, restores Use global while Global default is on, and saves and reloads
- **THEN** the inherited global value remains effective and unchecking Use global restores the retained save-off choice
- **AND** subsequent native routine decisions alternate between the retained policy restriction and ordinary native permission as the player changes that inheritance choice, without changing the global default or policy limit.

#### Scenario: Main menu and save switching
- **WHEN** the player is at the main menu or switches between two saves
- **THEN** no current-save control acts on an absent or previous save, and each save retains its own override and policy choices.

### Requirement: Unavailable enforcement and original-provider overlap are honest
The feature SHALL be unavailable if its intended routine-use rules or policy controls cannot be supported.
Report a bounded diagnostic and visible explanation, leaving ordinary
native behavior available rather than claiming protection. Stored choices SHALL remain intact.
When the original tolerance mod is active, startup SHALL issue a bounded warning identifying the
overlap and attempt best-effort suppression through the same ordinary feature-off behavior and
timing. Stored preferences SHALL not be rewritten. An installed but inactive original SHALL not
trigger this behavior. Unrelated features SHALL remain available and the original SHALL not be
changed. The warning SHALL not claim full coexistence safety or rollback of every existing effect.

#### Scenario: Original is active
- **WHEN** startup finds the original tolerance mod active
- **THEN** the player receives the bounded best-effort disablement warning, retained preferences remain intact,
  and no original-mod compatibility guarantee is presented.

#### Scenario: Required behavior is unavailable
- **WHEN** the game cannot support this feature's required enforcement or editor
- **THEN** the feature explains that its extra protection is unavailable and does not pretend that a displayed limit is enforced.

#### Scenario: Retained choices are evaluated before the first policy-window paint
- **WHEN** a supported save loads with retained tolerance choices and routine eligibility runs before the player opens its policy window
- **THEN** the atomic feature uses a proved native allocation and a current main-thread measurement of translated labels and viewport to establish editor availability
- **AND** it does not wait for a first paint to silently begin enforcing those choices.

#### Scenario: An upstream native geometry producer changes after admission
- **WHEN** a required effective initial-size, margin, window, sidebar, header, row or scroll producer
  changes so the concrete consumed-width proof is no longer current
- **THEN** the entire tolerance editor/enforcement capability becomes unavailable before adding a
  subsequent routine-use veto, leaves native behavior and saved choices intact, and reports a bounded reason
- **AND** a fresh supported publication may recover availability; an unrelated observer or drawing
  change does not become unsupported merely because it belongs to another patch owner.
- **AND** this initial native-manager slice uses physical-screen initial sizing, conservative native
  deductions and actual logical-GUI fit checks; unproved virtual geometry overrides remain unavailable.

#### Scenario: Actual native layout cannot fit the contextual controls
- **WHEN** a header or row supplies a rectangle that no longer supports the admitted native allocation and owned controls
- **THEN** that reservation leaves the native rectangle intact, the full feature becomes visibly unavailable, and subsequent routine eligibility adds no tolerance veto
- **AND** preferences and policy choices remain intact, existing selected jobs keep native execution, and recovery requires a fresh supported geometry assessment rather than elapsed time alone.
- **AND** the initial allocation requires calculator input widths from600 through1300 GUI units, at least600 native-column units and at least120 owned-column units or wider measured translated text; unsupported fits remain unavailable until a supported assessment succeeds.

#### Scenario: Native physical sizing exceeds the scaled logical viewport
- **WHEN** the native manager's prospective dimensions exceed the current logical GUI viewport, even though its local columns would fit
- **THEN** tolerance editing and extra routine restrictions are unavailable before the first manager paint, with retained choices and native behavior preserved
- **AND** while the exact native manager is open, its actual window rectangle must also fit wholly within that logical viewport before reserving an owned column or adding a subsequent tolerance veto
- **AND** a fresh supported viewport and current window allocation can restore availability; virtual scrolling of policy rows does not require every row to be on screen simultaneously.

#### Scenario: Another row fits after one row failed
- **WHEN** one current policy row fails its actual allocation and another row or the header later fits
- **THEN** the failed row still keeps the atomic feature unavailable
- **AND** only that same allocation succeeding, removal/replacement of its current row/policy, or closing the manager clears that row's failure.

#### Scenario: An open manager has unproved virtual producers
- **WHEN** the policy workflow uses a subclass of the native manager whose geometry/drawing producers have not been admitted
- **THEN** editing and extra restrictions remain unavailable while it is open; the initial implementation supports the exact native manager type only.

#### Scenario: Policy workflow explains missing tolerance controls
- **WHEN** the player opens a manager while tolerance controls are effectively off or unavailable, or its actual layout becomes unsupported
- **THEN** the current workflow gives a localized explanation at most once per reason for that open window
- **AND** an intentional off state produces no failed-enabled-patch diagnostic; no global repeated message is emitted while no manager is open.

#### Scenario: Native drug recreation inherits the admitted decision
- **WHEN** a drug recreation worker inherits the admitted native decision without declaring another implementation
- **THEN** looking up that same declaration through the worker does not make tolerance editing or enforcement unavailable
- **AND** a separate override or hidden implementation still needs its own proved semantics; sharing a base definition alone does not authorize it.
