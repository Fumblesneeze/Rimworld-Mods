## Owner

Make Rimworld Great Again — `fumblesneeze.megamod`, `mods/megamod`; RimWorld 1.7 beta.
The bounded C1 suggestion calculation is specified from 2026-11-11 native/source research.
Independent review, finalized runtime admission, implementation and native acceptance remain due;
no runtime behavior is accepted by this draft.

## ADDED Requirements

### Requirement: Generation begins with an explicit policy-window preview
Make Rimworld Great Again SHALL offer Generate in the native drug-policy window when this feature
is enabled and available in a loaded megamod colony. Generate SHALL open a draft showing every
current available drug, proposed routine-use choices, addiction/medical permission and an explanation
of each suggestion or missing suggestion. The draft SHALL identify its limits as risk-reduction
suggestions, not a guarantee against addiction, overdose or other harm. No policy SHALL be added
until the player chooses Create new policy. Cancel SHALL leave the colony unchanged. No new bottom
tab or policy-creation action in mod settings SHALL be added.

#### Scenario: Inspect and cancel
- **WHEN** the player opens Generate, examines the reasons and cancels
- **THEN** no policy, assignment, default, carried supply or consumption history changes.

#### Scenario: No available drugs
- **WHEN** no drugs are currently available to the colony's policies
- **THEN** the preview explains that there are no rows and does not create a misleading risk-reduction policy.

### Requirement: Only established suggestions enable routine use automatically
The generator SHALL provide an autonomous schedule or recreation suggestion only for drug behavior
covered by an established, documented calculation and its visible assumptions. Unknown or
unsupported behavior SHALL leave both scheduled and recreational suggestions off with a reason.
A negative, nonfinite or otherwise unsupported interval SHALL not be turned into a permissive
schedule. A drug's display name, the presence of Beer, a fixed guessed body size or a universal
frequency cap SHALL not serve as evidence of safety. The preview SHALL explain remaining risks,
including combined drugs sharing a chemical and pawn differences, instead of silently ignoring them.
It SHALL not present unsupported drugs as safe merely because they are omitted from a calculation.

The initial calculation C1 SHALL suggest a schedule based on modeled clearance of a single ordinary
whole dose's tolerance, under the following disclosed assumptions. This is a conservative chosen
objective, not the most frequent usable schedule or a guarantee of actual clearance or harmless
effects. The preview SHALL disclose that native camera-driven batching can skip decay events and
actual tolerance may remain when the suggested interval permits another dose.
It SHALL use current finalized admitted facts rather than the native safe-interval label or a drug
name. C1 SHALL leave autonomous recreation off: recreation does not obey the scheduled interval,
and non-addictiveness alone is not a sufficient positive recreation rule.

C1 SHALL require an ordinary one-item native dose, non-medical routine use with positive joy,
finite addictiveness strictly between zero and one, a positive addiction-tolerance threshold,
an actual chemical/tolerance channel, one deterministic positive tolerance gain and one deterministic
negative decay. Ordinary/random overdose fields SHALL be zero. The gain for the displayed baseline
SHALL be strictly below the addiction threshold; its tolerance SHALL be removable at zero, without
using a maximum-severity cap to make an excessive dose fit. Unknown callbacks/outcomes, nonfinite
values, duplicate givers, random/reversed/gated decay, unproved amplification and unresolved partial
ingestion SHALL receive no computed suggestion. Compatible content-added native shapes can qualify.

The calculation SHALL use the game's binary32 severity arithmetic, 200-tick decay events and
60,000-tick days. If `k` is the first decay event that clears the normalized dose to zero, C1 SHALL
offer **1 + ceiling((200 × k + 415) / 60,000) calendar days**. The 415-tick budget consists of the
modeled 14-tick dispatch allowance, 400 ticks covering both binary32 calendar-boundary
errors over the nonnegative signed-int game clock, and one tick for strict ordering. The additional
calendar day accounts for native day-index scheduling. Negative/rewritten/overflowing clocks and
unproved tick-cadence changes are outside the model. Ordinary camera-driven batching also prevents
this modeled allowance from establishing a universal elapsed-time clearance bound. The design/research define the exact decay
recurrence and failure conditions; no guessed numeric epsilon SHALL shorten the interval.

An automatic result SHALL be representable in the admitted native editor. Its inspected maximum
is 25 days; a greater required interval SHALL be unavailable, never clamped to 25. No additional
delay preference is part of C1. With the researched unmodified Core facts and adult baseline 1,
expected proposals are Beer 3 days, psychite tea 4 days, smokeleaf 4 days and ambrosia 3 days.
These are fact-derived comparator results, not a name-based table or finalized-runtime evidence.

#### Scenario: Unsupported drug behavior
- **WHEN** a current drug has an unknown tolerance effect or no established suggestion calculation
- **THEN** its preview row remains visible with routine-use suggestions off and an explanation
- **AND** the preview does not label that absence of a schedule as proof the drug is harmless.

#### Scenario: Shared chemical or different pawn needs
- **WHEN** the proposed policy includes drugs sharing a chemical or could be assigned to pawns outside a suggestion's assumptions
- **THEN** the preview identifies the limitation and withholds any autonomous suggestion not covered by the established calculation.

#### Scenario: Measured native whole-dose suggestions
- **WHEN** the admitted finalized inputs match the researched Core whole-dose gains and decay for the displayed adult baseline
- **THEN** Beer and ambrosia propose three calendar days, psychite tea and smokeleaf propose four, all with recreation off and a modeled-clearance explanation including native batching limitations
- **AND** removing or renaming Beer does not change another drug's calculation.

#### Scenario: Native batching does not make a schedule a clearance guarantee
- **WHEN** a player reviews or creates a C1 suggestion
- **THEN** the preview explains that native timing can leave residual tolerance and the interval does not monitor or prevent that state
- **AND** a bounded observed clearance run is never presented as proof for every camera/batching sequence.

#### Scenario: Required interval exceeds native editing range
- **WHEN** admitted slow decay requires more than the native editor's 25-day maximum
- **THEN** the row explains that the required interval cannot be represented, leaves scheduled use off and does not shorten the result to 25 days.

#### Scenario: A zero helper interval is not a recreation recommendation
- **WHEN** the drug is non-addictive but medical, has a risky outcome, or otherwise lacks an established positive recreation model
- **THEN** neither routine-use option is automatically enabled, even when the native safe-interval helper returns zero.

### Requirement: The baseline population and exposure limits remain visible
The preview SHALL state that C1 is for healthy ordinary adult humans whose actual BodySize is at
least the displayed finalized Human-adult baseline (currently 1), with neutral drug metabolism,
no initial relevant tolerance, addiction/dependency or interfering drug effects, ordinary whole
doses and unsuspended normal ticking. It SHALL explain that children/smaller bodies, altered drug
metabolism, extra medical/manual/recreational doses and existing addiction/dependency are outside
that baseline. Weaker gene factors SHALL not shorten the automatic interval. A generated policy
SHALL not imply that it enforces these assumptions for every present or future assignee. It SHALL
not rewrite assignments, clear existing tolerance/history or become dependency treatment.

Rows SHALL share an exposure channel whenever their actual chemical **or** tolerance-Hediff
identity connects them, including transitive connections. When several otherwise eligible candidates
share a channel, all their automatic routine suggestions SHALL remain off with a shared-exposure
reason. Unsupported off rows alone SHALL not suppress an otherwise supported candidate; possible
use of those rows outside the schedule SHALL be disclosed as uncalculated exposure.

Player changes SHALL remain possible. A manually shortened interval, recreation, additional
scheduled drug on the same channel or other departure from the C1 result SHALL be identified as
a manual choice, with combined exposure no longer calculated where applicable. Creation SHALL not
mislabel those edited choices as computed recommendations or silently revert the player's edits.

#### Scenario: Same chemical with different tolerance givers
- **WHEN** two otherwise eligible drug rows share a chemical but use different tolerance givers
- **THEN** both receive the shared-exposure reason and no autonomous schedule; different givers do not bypass chemical grouping.

#### Scenario: Different chemicals share tolerance
- **WHEN** otherwise eligible rows have different chemical names but share the same actual tolerance HediffDef
- **THEN** they are treated as one exposure channel and no independent automatic schedules are proposed.

#### Scenario: Player enables combined use
- **WHEN** the player enables recreation or an additional scheduled row on a computed row's exposure channel
- **THEN** the preview retains the edit, marks the affected combination as manually edited and uncalculated, and does not claim that the displayed interval covers the extra doses.

#### Scenario: Partial drug handling has unresolved effects
- **WHEN** active S085 handling changes a row's quantity/timing or stochastic effects without an admitted contract preserving C1's dose bound
- **THEN** that row has no autonomous C1 suggestion, with a partial-dose explanation, while unaffected whole-dose rows remain eligible.

### Requirement: Medical permission and ordinary policy defaults are explicit
Generation SHALL start from S029's applicable native policy defaults: addiction permission on only
for addictive drugs, recreation and scheduled use off, frequency one day, mood and recreation
thresholds 100%, and carried amount zero. S138 tolerance begins at Any. A displayed frequency on an
off schedule SHALL not imply a recommendation. Established suggestions and the player's deliberate
draft edits can change those fields, but generation eligibility SHALL not silently clear or grant
addiction/medical permission. That permission SHALL be visible and independently editable where
applicable. Omitted suggestions SHALL not remove the drug's policy row.

#### Scenario: Addictive drug without a suggestion
- **WHEN** an addictive drug has no supported routine-use suggestion
- **THEN** it still has a visible policy row, routine-use suggestions off and its applicable addiction permission on.

#### Scenario: Suggested schedule retains separate medical choice
- **WHEN** an established suggestion enables scheduled use for an addictive drug
- **THEN** the player can separately see and change addiction permission before creation
- **AND** enabling the schedule does not silently disable that permission.

### Requirement: Draft editing and reset have complete meanings
The player SHALL be able to edit a draft's name and applicable policy choices before creation.
Edits SHALL remain confined to the draft. Numeric mood/recreation 100% SHALL remove that extra
threshold, and S138 Any SHALL remove its additional tolerance restriction; returning to those values
SHALL not retain an earlier lower limit. Reset draft SHALL discard draft edits and regenerate the
whole preview from the current catalog, established suggestion rules and current effective generation
preferences, showing that existing edits will be lost before the player confirms reset. Reset SHALL
not change an existing policy. Invalid draft input SHALL be explained and block creation.

#### Scenario: Lower a limit and restore its unrestricted value
- **WHEN** the player lowers a draft mood limit and returns it to 100%, or chooses numeric tolerance and returns it to Any
- **THEN** the preview and newly created policy use the final unrestricted choice rather than the earlier limit.

#### Scenario: Reset an edited draft
- **WHEN** the player confirms Reset draft after seeing which edits will be discarded
- **THEN** the complete draft is rebuilt from current inputs and no old partial draft values remain.

### Requirement: Only a current complete preview can be created
Create new policy SHALL require that the preview still belongs to the same loaded colony and
matches its current available drug list, applicable drug properties, established suggestion rules
and effective generation preferences. If any of those inputs change, the preview SHALL visibly
become out of date and creation SHALL be refused without mutation. Reopening or explicitly resetting
the preview SHALL show the new inputs. Changing maps within the same colony SHALL not alone change
policy identity. When S138 is off or unavailable, creation SHALL require a refreshed preview whose
tolerance choices are all Any, with numeric tolerance editing unavailable and an explanation.
The created policy SHALL store Any for those rows, without a hidden numeric choice that could
activate later. Stored generation preferences SHALL remain unchanged. Enabling S138 later SHALL
not replace those created Any choices; the retained preference applies only to new/refreshed drafts.

#### Scenario: Drug content changes while preview is open
- **WHEN** a drug is removed or its relevant behavior changes after the preview was prepared
- **THEN** Create new policy refuses the stale draft and explains that it must be refreshed.

#### Scenario: Save changes or tolerance support disappears
- **WHEN** the player switches colonies or required tolerance support becomes unavailable
- **THEN** the old preview cannot create a policy against the new or changed state.

#### Scenario: Numeric preference while tolerance support is off
- **WHEN** the stored generation preference is numeric but S138 is off or unavailable and the player refreshes the preview and creates a policy
- **THEN** the preview explains that tolerance support is unavailable, its tolerance choices and the created rows use Any, and the stored generation preference remains unchanged
- **AND** enabling S138 later leaves those created rows at Any rather than activating a hidden numeric limit.

### Requirement: Creation adds one complete independent policy or changes nothing
After confirmation, generation SHALL add exactly one complete new policy with the final displayed
choices and select it for inspection in the policy window. Every prior policy, its identity, choices,
order, assignments and default status SHALL remain unchanged. One confirmation SHALL not create
duplicates when its input is delivered repeatedly. A later deliberate new Generate/Create workflow
can create another independent policy. Failure at any stage SHALL remove only the failed owned
creation, restore all prior owned state and show an accurate failure instead of a success message.
Creation SHALL not save the game automatically.

#### Scenario: Successful creation and repeated confirmation
- **WHEN** the player confirms a current valid preview and the same confirmation arrives twice
- **THEN** one independent policy with the displayed choices appears, and prior policies and assignments remain unchanged.

#### Scenario: Creation fails after work has begun
- **WHEN** creation cannot finish or verify the complete intended result
- **THEN** no partial new policy remains, previous state is restored and the player receives a failure explanation.

### Requirement: Created policy choices are ordinary persistent gameplay state
Generated policies SHALL behave as ordinary native policies for editing, copying, renaming,
assignment, deletion, default selection and saving. Generation SHALL not itself assign pawns,
choose a default or alter actual-consumption history. Loading, copying, renaming, repairing or
enabling the generator SHALL not regenerate a saved policy or overwrite deliberate edits. S029
content maintenance SHALL preserve surviving choices and give newly introduced rows its ordinary
defaults with S138 Any; generation suggestions SHALL not be silently applied to those later rows.

#### Scenario: Save and reload an edited generated policy
- **WHEN** the player creates a policy, changes a choice, assigns it, saves and reloads
- **THEN** the edited choice and deliberate assignment persist without reapplying the generator.

#### Scenario: Add content after creation
- **WHEN** S029 adds a new drug row to an existing generated policy
- **THEN** that row receives ordinary new-row defaults, while existing rows and consumption history remain unchanged.

### Requirement: Generation preferences use common configuration without owning policy state
Proposed reusable generation preferences SHALL be feature enabled on, initial mood threshold 100%,
initial recreation threshold 100% and initial tolerance Any. The numeric threshold preferences SHALL
allow 0–100%; tolerance SHALL also distinguish Any from numeric 100%. These are proposed preview
defaults, not validated safety calculations. They SHALL use the common Drug policies → Generation
settings section with Global default, Use global and Current save controls. New save entries SHALL
inherit current globals; overrides and dormant values SHALL remain independent. Main-menu save
controls and callbacks SHALL be unavailable. Changes SHALL affect new/refreshed previews and make
an open preview stale; they SHALL never rewrite existing policies. A draft-specific edit SHALL not
write a global or save preference. C1 adds no additional-delay preference.

#### Scenario: Global preference changes for inherited and overridden saves
- **WHEN** the global initial mood threshold changes
- **THEN** new previews in an inheriting save use it, independently overridden saves retain their value,
  and existing policies remain unchanged.

#### Scenario: Preview edit is not a preference edit
- **WHEN** the player changes a limit in one preview then starts a separate Generate workflow
- **THEN** the new preview uses effective generation preferences rather than the previous draft's edit.

#### Scenario: Main menu and disabled feature
- **WHEN** no colony is loaded or the generator is effectively off
- **THEN** no policy can be created through this feature, save-setting callbacks have no absent/previous-save target,
  and existing generated policies remain ordinary usable policies.

### Requirement: Suggestions retain existing routine and medical authority
Generated choices SHALL be ordinary policy inputs, not a new consumption scheduler. S024 potential
mood, recipient checks and inventory purpose SHALL retain their full meaning. S138 SHALL apply the
saved routine tolerance preference, including Any/zero and native joy exceptions, without a second
generator-specific veto. S124/S134 SHALL retain their distinct treatment reasons, addiction/medical
permission, pending-dose coordination and actual-consumption history. Generation SHALL not queue
treatment, acquire supplies or mark a dose taken. It SHALL explain that satisfying a policy interval
alone does not ensure the pawn will or should consume the drug.

#### Scenario: Generated interval is due but another rule rejects
- **WHEN** a generated policy's interval has elapsed but S024, S138 or an ordinary eligibility condition rejects use
- **THEN** the generator does not force a dose or reset the interval to make one happen.

#### Scenario: Medical assistance remains independently eligible
- **WHEN** S134 considers an eligible dependent patient with a generated policy
- **THEN** its visible addiction permission and existing medical rules govern assistance, with no extra dose or history entry created by generation.

### Requirement: Unavailable generation and original-provider overlap are honest
If safe complete creation or the required native window cannot be supported, Generate SHALL be
unavailable with a reason and ordinary policy management SHALL remain usable. Active original
Safe Drug Policy Generator SHALL produce a bounded startup warning and a best-effort attempt to
disable this owned generator through its ordinary effective-off path. It SHALL not rewrite stored
preferences, remove already created policies or change the original provider. Installed but inactive
originals SHALL not trigger this behavior. Unrelated features SHALL remain available. The warning
SHALL describe limited best-effort disablement, not compatibility or rollback of all earlier effects.

#### Scenario: Original generator is active
- **WHEN** startup finds the original generator active
- **THEN** it reports the overlap and best-effort ordinary disablement without deleting saved policies or promising coexistence safety.

#### Scenario: Creation support is unavailable
- **WHEN** the game cannot support complete verified policy creation
- **THEN** Generate explains its unavailability and native policy editing remains usable.
