# Need reserves and overflow presentation

Owner **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **RimWorld 1.7 beta**.
S103 / Need Bar Overflow. Observable draft only; no implementation or gameplay acceptance.

## ADDED Requirements

### Requirement: Extra reserves do not redefine ordinary full
Eligible needs SHALL retain legitimate gains beyond ordinary full up to their configured reserve
limit. Ordinary full SHALL remain the pawn's current ordinary need capacity, including supported
body, life-stage, gene and stat effects. Food, rest, recreation, comfort, beauty, chemical addiction
needs and chemical interest/fascination SHALL default to enabled with a limit of200% ordinary full.
Food SHALL additionally default to allowing at least1 nutrition above ordinary full: its limit is
the larger of twice ordinary capacity and ordinary capacity plus1, not their sum. Gains above the
limit SHALL be discarded without creating items, replaying consumption or refunding its other costs.

#### Scenario: Small body's meal reserve
- **WHEN** an eligible pawn with ordinary food capacity0.5 has0.4 nutrition and actually consumes0.9 nutrition
- **THEN** it holds1.3 nutrition, shown as260% ordinary full; its default limit is1.5 nutrition, not1 or2

#### Scenario: Larger capacity
- **WHEN** an eligible pawn has ordinary capacity2 and receives enough nutrition to reach4.5
- **THEN** its default reserve stops at4, with ordinary full still2 and no additional food created

#### Scenario: Drug gain and other consequences
- **WHEN** a permitted drug provides100percentage points of rest to an eligible pawn at80% rest
- **THEN** rest reaches180% up to its limit, while the drug's ordinary cost, addiction, overdose and other effects still apply

### Requirement: Optional need reserves preserve the need's own rules
The mod SHALL default mood, indoors, outdoors, room size, suppression, deathrest, kill thirst,
mech energy, learning, play, authority, sadism and unrecognized needs to overflow disabled with a
dormant limit of100%. Enabling and increasing a supported limit SHALL permit only gains that the pawn
actually receives; it SHALL NOT create missing needs, new activities or DLC eligibility. Unsupported
need types SHALL remain unavailable with an explanation until their behavior is supported.
Hemogen and psyfocus SHALL not be altered by this feature.

Mood, comfort and beauty SHALL follow their ordinary changing targets and rise/fall rates with an
extended upper limit. Indoors/outdoors SHALL preserve roof, bed, preference, floor and frozen rules.
Chemical needs SHALL preserve category transitions and drug effects. Mech/deathrest behavior SHALL
retain existing charging, sleep, exhaustion and resource rules. Explicit native resets remain valid.
With kill-thirst overflow enabled, an otherwise qualifying kill SHALL add100percentage points
instead of resetting to100%, subject to the selected limit and optional gain slowdown.

#### Scenario: Better furniture
- **WHEN** an eligible pawn uses furniture giving an ordinary comfort target of150% and the supported comfort limit is200%
- **THEN** comfort approaches150% at its ordinary rate; the feature neither jumps immediately to the target nor invents additional comfort

#### Scenario: Environmental reserve
- **WHEN** outdoors overflow is enabled at200% and an eligible pawn gains outdoors satisfaction above100% before returning indoors
- **THEN** the gained reserve is retained and subsequently falls according to the current roof, bed and floor rules

#### Scenario: Qualifying kill
- **WHEN** an eligible pawn with kill-thirst overflow enabled at200%, no gain slowdown and40% satisfaction makes a qualifying melee kill
- **THEN** satisfaction reaches140%; a kill that does not qualify under ordinary kill-thirst rules grants no satisfaction

#### Scenario: Optional content absent or unsupported
- **WHEN** the player has no relevant DLC, the pawn has no such need, or an external need's rules are unsupported
- **THEN** the feature creates no substitute need or gain, explains unavailable configuration where applicable and leaves unrelated needs usable

### Requirement: Ordinary need-seeking and native consequences remain recognizable
Pawns SHALL continue to use ordinary full and their existing schedules, jobs, policies and thresholds
when choosing whether to eat, sleep, recharge, learn or recreate. Overflow SHALL not itself order
extra consumption or prolong those activities to fill the reserve. Legitimate gains from existing
activities, forced actions or binges remain subject to the applicable reserve policy. Food wanted
SHALL never become negative. Above-full mood SHALL not increase ordinary inspiration chance beyond
its100%-mood result or remove an otherwise applicable highest child-mood thought. Valid unrelated
conditions testing for above-full needs SHALL remain functional.

#### Scenario: No new sleep target
- **WHEN** a pawn reaches its ordinary wake/completion condition with a200% rest limit
- **THEN** the reserve limit alone does not keep it sleeping to200% or create another sleep job

#### Scenario: Full food query
- **WHEN** a pawn holds150% of its ordinary food capacity
- **THEN** it wants zero additional nutrition through the ordinary fill-to-full calculation, not a negative amount or50% toward the reserve limit

#### Scenario: High mood consumers
- **WHEN** an eligible child's mood reaches130% with mood overflow enabled
- **THEN** an otherwise applicable highest child-mood thought still recognizes that happy child, and ordinary inspiration chance is no higher than at100% mood

#### Scenario: Legitimate above-full condition
- **WHEN** supported gameplay deliberately checks whether a need exceeds100%
- **THEN** a true above-full value remains detectable; a separate provider's explicit Never option does not disable every above-full check

### Requirement: Eligibility follows current pawn and content state
By default overflow SHALL be permitted for colony colonists, colony slaves, colony-hosted guests and
prisoners, player-faction animals, player-faction mutants and other player-faction pawns. Other
humans, wild animals, other-faction animals, other mutants and remaining pawns SHALL default to
excluded. These groups SHALL be independently configurable. Mutant classification SHALL use the
actual pawn's state, without requiring an unrelated DLC. No group setting SHALL create a need.

Supported content exclusions SHALL override group permission for the named needs. Configurable
race, worn-apparel, present-condition and active-gene exclusion lists SHALL default to empty and
disabled, and exclude all needs when enabled. Inactive genes and merely carried clothing SHALL not
count. Unavailable saved entries SHALL remain visibly unresolved rather than selecting unrelated
content with the same translated label. A group/content change SHALL apply before the next owned
gain/consequence decision; it SHALL not reuse another pawn's eligibility.

The following source-derived exclusions SHALL be supported when their provider content and behavior
are admitted. Absent content SHALL create no dependency or substitute. If present content cannot be
safely recognized, its affected overflow behavior SHALL remain unavailable with a diagnostic until
that integration is supported; unresolved provider identities do not permit guessing a match.

| Provider content and current pawn condition | Needs excluded from new overflow |
| --- | --- |
| Dust Bunnies: pawn is a Dust Bunny | Food |
| WVC Xenotypes: Dead Stomach gene is active | Food |
| Vanilla Races Expanded — Fungoid: Mind Coalescence gene is active | Mood |
| Vanilla Factions Expanded — Ancients: pawn has Redundant Stomach power | Food |
| Vanilla Factions Expanded — Empire: pawn wears a Techfriar Crown | Rest and recreation |

#### Scenario: Tame and wild animals
- **WHEN** otherwise eligible player-owned and wild animals consume equivalent food under default group settings
- **THEN** only the player-owned animal may newly gain an owned reserve; taming or losing ownership changes eligibility at the next relevant decision

#### Scenario: Clothing and gene exclusions
- **WHEN** a configured exclusion garment is carried but not worn, or an exclusion gene is inactive
- **THEN** that entry alone does not prevent overflow; wearing the garment or activating the gene applies the relevant exclusion

#### Scenario: Pawn and save changes
- **WHEN** the player inspects another pawn or loads a save with different group/exclusion configuration
- **THEN** eligibility reflects that pawn and current save, without a delay inherited from another pawn or the previous game

#### Scenario: Missing saved content
- **WHEN** a saved exclusion's provider is absent
- **THEN** the entry is preserved and identified as unavailable, does not bind to a coincidentally matching label, and can resolve again when its provider returns

#### Scenario: Supported food and mood exclusions
- **WHEN** admitted content makes a pawn a Dust Bunny, gives it active Dead Stomach, gives it Redundant Stomach power, or gives it active Mind Coalescence
- **THEN** the first three conditions prevent new food overflow and the fourth prevents new mood overflow, without disabling unrelated permitted needs

#### Scenario: Crown worn and removed
- **WHEN** an otherwise eligible pawn wears an admitted Techfriar Crown, then removes it
- **THEN** new rest/recreation overflow is excluded while worn and becomes eligible again after removal; food permission and existing finite reserves are not erased by that transition

### Requirement: Existing reserves survive owned configuration changes
Reducing a cap, disabling overflow or changing eligibility SHALL not itself delete a finite existing
reserve. While the current amount is above the newly permitted limit, further positive gains SHALL
be ignored and ordinary losses SHALL continue without an immediate clamp. Native explicit resets,
removal of the need, actual consumption/loss and supported provider actions retain their own meaning.
Increasing a cap SHALL not fill the added space. Ordinary save/load SHALL preserve current amounts
once, without awarding or discarding reserves just for loading. Owned food consequences SHALL be
removed when disabled, ineligible or no longer applicable, without removing foreign conditions.

#### Scenario: Smaller limit while full
- **WHEN** a pawn has180% rest and the player reduces its limit to120%, then it receives a positive rest gain
- **THEN** changing the setting and the positive gain leave it at180%; subsequent ordinary loss can reduce it through120% and below

#### Scenario: Disable and reload
- **WHEN** the player disables food overflow while a pawn has a reserve and reloads the save
- **THEN** the finite saved reserve remains available for ordinary loss, no new reserve is granted, and disabled owned food consequences do not reappear

#### Scenario: Real reset or removed need
- **WHEN** ordinary gameplay explicitly resets or removes a need
- **THEN** the feature does not resurrect its old reserve or keep an orphaned owned food consequence

### Requirement: Optional overflow rates use actual current satisfaction
Rest, recreation and kill thirst SHALL each provide independent faster-loss and slower-gain options,
both off by default, with a proposed dormant coefficient0.5 and valid coefficients0–10. For current
fraction above ordinary full, faster loss SHALL multiply ordinary loss by one plus excess fraction
times its coefficient; slower gain SHALL divide ordinary gain by the corresponding factor. At or
below ordinary full, disabled options and coefficient0 SHALL leave the ordinary delta unchanged.
Current fraction SHALL be evaluated before each affected gain/loss event, preserving native cadence,
freeze rules and other multipliers. Recreation tolerance SHALL use the actual nonnegative accepted
gain. No rate option SHALL reverse gain/loss or make a gain reduce the need.

An optional rate modifier SHALL apply only while its need's overflow is enabled, admitted and the
pawn is currently eligible. Disabling that need's overflow, disabling the modifier or excluding the
pawn SHALL stop the modifier before the next affected event, even if a finite reserve remains.
Forecasts SHALL immediately follow the same effective policy. This does not bypass other ordinary
or provider rate effects.

#### Scenario: Rate example
- **WHEN** an eligible pawn is at150% rest with coefficient0.5 enabled and its ordinary loss is0.04 or ordinary gain is0.10
- **THEN** the respective adjusted loss is0.05 or adjusted gain0.08 before the applicable reserve limit

#### Scenario: Ordinary satisfaction and disabled slowdown
- **WHEN** a pawn is below100%, or its slower-gain option is off
- **THEN** that option does not reduce the gain, including the100percentage-point gain of an enabled qualifying kill

#### Scenario: Recreation reserve
- **WHEN** a pawn at150% recreation gains tolerated recreation with slowdown enabled
- **THEN** slowdown uses150% actual recreation, and tolerance increases only by recreation actually accepted up to the limit

#### Scenario: Retained reserve after rate deactivation
- **WHEN** an eligible pawn at150% rest has coefficient0.5 faster loss enabled and ordinary loss0.04, then rest overflow is disabled or the pawn becomes excluded
- **THEN** its reserve is not immediately deleted, the next loss is ordinary0.04 rather than0.05, and the rest forecast uses the resulting ordinary rate

### Requirement: Optional food consequences are explicit and reversible
Food overflow SHALL provide separate hunger, natural-healing, Moving, Eating and vomiting effects,
all disabled by default. Enabled effects SHALL use adjusted fullness: ordinary100% plus the excess
food fraction multiplied by a scale, default100% for ordinary humans,25% for nonhumans, or25% for
human Gourmands. The two quarter scales SHALL not multiply together. Scale0 SHALL produce no owned
food consequence. Consequences SHALL apply only above ordinary full, with positive adjusted excess
and eligible food overflow. Current effects SHALL update no later than the next ordinary food
update, at most150 simulated ticks, and be derived from the current amount rather than an older
pre-loss amount. Configuration changes do not advance simulation while paused.

Eight configurable stages SHALL use the highest reached adjusted-fullness threshold, without
interpolation, with the following dormant defaults:

| Adjusted fullness |100% |120% |140% |160% |180% |200% |300% |500% |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Hunger multiplier |1 |1.05 |1.1 |1.2 |1.3 |1.5 |2 |5 |
| Natural injury-healing multiplier |1.1 |1.2 |1.2 |1.2 |1.2 |1.2 |1.2 |1.2 |
| Moving reduction, percentage points |1 |2 |5 |10 |15 |20 |25 |30 |
| Eating reduction, percentage points |5 |10 |20 |30 |40 |50 |60 |70 |
| Vomiting average events/day |0 |0 |0 |0 |0.25 |2 |5 |6 |

Only enabled columns SHALL affect the pawn, once each, alongside ordinary health modifiers. Healing
SHALL affect ordinary naturally healing injuries, not cure diseases, grant immunity or restore
parts. Capacity reductions SHALL be additive percentage-point reductions and retain ordinary health
consequences, including possible incapacitation. Vomiting SHALL be stochastic at the configured
average frequency, use ordinary eligibility/effects and produce no events for frequency0. The
feature SHALL not bypass native vomiting conditions or guarantee exact daily counts.

The contextual health explanation SHALL appear when adjusted fullness reaches the configurable
visibility threshold, default120%, and hide again below it. Active but hidden effects SHALL still
be explained in the food tooltip. Effects SHALL disappear at/below ordinary full or when disabled;
they SHALL not become permanent because they once exceeded the visibility threshold.

#### Scenario: Default reserves have no added penalties
- **WHEN** a pawn gains a default food reserve while all consequence options remain off
- **THEN** this feature adds no hunger/healing change, capacity penalty, vomiting event or food-overflow health condition

#### Scenario: Fullness and scaled effect stage
- **WHEN** ordinary and quarter-scale pawns hold180% food with Moving reduction enabled
- **THEN** their adjusted fullness values are180% and120%, giving15 and2percentage-point reductions respectively before ordinary health composition

#### Scenario: Healing scope
- **WHEN** natural healing is enabled for an eligible pawn at140% adjusted fullness
- **THEN** ordinary naturally healing injury gain receives the1.2 multiplier once, without curing unrelated disease or restoring missing body parts

#### Scenario: Stage falls and display clears
- **WHEN** ordinary nutrition loss moves a pawn below a stage or display threshold, then to ordinary full
- **THEN** the next food update uses the new lower stage, the condition hides below its visibility threshold, and all owned consequences cease at ordinary full

#### Scenario: Vomiting setting and disabled column
- **WHEN** an eligible pawn is at200% adjusted fullness with vomiting enabled at the default stage value
- **THEN** ordinary eligible vomiting has an average frequency of2events/day, while disabled hunger, healing and capacity columns add no effects

#### Scenario: Zero consequence scale
- **WHEN** the applicable consequence scale is0 while a pawn has surplus food
- **THEN** no owned food consequence applies, and the reserve itself remains available

### Requirement: Further eating controls preserve medical and actor-specific rules
An optional further-eating restriction SHALL default to enabled at100% ordinary fullness. It SHALL
prevent ordinary automatic inventory snacking and player-ordered ordinary food ingestion for an
eligible pawn at/above the threshold, showing a full-food explanation for an unavailable order.
Hungry pawns under the ordinary hunger categories SHALL remain able to eat even if a configured
lower threshold is reached. The threshold SHALL be configurable from50% to1000%. Drugs, hemogen-pack
consumption and existing medical/feeding routes outside these two controls SHALL retain their own
permissions and needs. Each actor SHALL be evaluated separately; selecting a full pawn SHALL not
disable another hungry pawn's valid action. Removing this restriction SHALL not itself order eating.

#### Scenario: Full ordinary food order
- **WHEN** an eligible pawn at110% food is offered an ordinary food-consumption order with default restriction enabled
- **THEN** the order is unavailable with a full-food explanation and consumes nothing

#### Scenario: Mixed selection and hungry exemption
- **WHEN** selected pawns include one full pawn and one hungry pawn, including a hungry pawn above a configured50% restriction threshold
- **THEN** the hungry pawn can still receive its otherwise valid food order independently of the full pawn

#### Scenario: Required drug or blood resource
- **WHEN** a full pawn otherwise has permission and reason to consume a drug or hemogen pack
- **THEN** this ordinary-food restriction does not block it or change its other eligibility, dose and side effects

### Requirement: Need presentation shows reserves without overflowing the layout
Existing need displays SHALL distinguish ordinary full from actual current reserves, retain ordinary
labels, descriptions, thresholds, arrows and relevant native contextual information, and keep all
graphics inside their allotted area. A detailed bar SHALL represent current amount and any supported
instant target on a consistent scale, including a distinguishable reserve segment and ordinary-full
boundary. Compact surfaces SHALL remain bounded and expose actual above-full values in their
associated readout or tooltip; no out-of-bounds bar is an acceptable indication of a reserve.
Food SHALL show actual nutrition alongside ordinary capacity. Inaccessible/invalid values SHALL be
shown as unavailable rather than a plausible invented percentage. Displaying any bar SHALL not
change needs or health, grow marker counts with reserve magnitude, or alter unrelated UI.

#### Scenario: Detailed reserve and higher target
- **WHEN** current comfort is120% and its supported instant target is170%
- **THEN** the detailed display distinguishes current amount, ordinary100% and target170% within its bounds rather than drawing both current and target at the same right edge

#### Scenario: Compact bar and tooltip
- **WHEN** food, mood, learning, suppression or mech energy is above ordinary full on a supported compact surface
- **THEN** the bar remains within its allotted area and its actual value is discoverable, while unrelated windows retain their ordinary geometry

#### Scenario: Large reserve and ordinary information
- **WHEN** an admitted finite reserve is very large or ordinary food capacity is small
- **THEN** the display remains responsive and readable with bounded markers, truthful amounts and preserved tooltips, native learning icons and tutorial highlighting

### Requirement: Forecasts and other owned features use one coherent need result
Owned forecasts SHALL include actual reserves and current effective hunger/rate stages once, while
retaining ordinary-full reference points and conditional/unavailable states. They SHALL not show a
fixed-rate exact duration when a supported stage/rate model is unavailable. Owned mood bars and
future RimHUD displays SHALL consume the same need result without changing simulation.
Partial ingestion SHALL grant only the need gain corresponding to the actual consumed portion;
enabling overflow SHALL not consume an additional portion by itself.

Optional learning overflow SHALL retain ordinary dependence of future growth points on actual
learning satisfaction. The separate Global Learning Factor feature SHALL multiply that ordinary
rate once. This feature SHALL not add growth tiers, rewrite historical points, change ages, grant
toddler skill experience or bypass native growth/vat eligibility. All source features implemented
inside the megamod SHALL compose as one behavior without duplicate effects.

#### Scenario: Changing rate forecast
- **WHEN** a food reserve crosses an enabled hunger-effect stage or rest crosses an optional faster-loss region
- **THEN** the corresponding forecast accounts for the successive effective rates and actual reserve once, or clearly reports an unavailable model

#### Scenario: Interrupted portion
- **WHEN** a supported partially eaten meal has delivered0.2 nutrition before interruption
- **THEN** overflow receives at most that0.2 gain up to its cap, and the remaining meal retains the uneaten amount without a second nutrition award

#### Scenario: Growth composition
- **WHEN** a child's ordinary rate at100% learning would be2points/day, actual learning is150% under an enabled reserve, and Global Learning Factor is150%
- **THEN** its corresponding ordinary rate is3points/day and the combined adjusted rate is4.5, with existing eligibility, birthday rewards and accumulated history unchanged

### Requirement: Common settings validate whole configurations and scope
Configuration SHALL use common Needs → Overflow sections ordered as need limits, pawn/content
eligibility, food consequences and eating restriction, optional rates, and compatibility information.
Every applicable configuration value SHALL have Global default, Current save and Use global controls.
New/missing save entries SHALL inherit current globals; an override SHALL affect only its save.
Use global SHALL disable the save editor and retain its dormant value. At the main menu, save
editors, inheritance changes and save resets SHALL be disabled in both presentation and behavior.
Committed valid changes SHALL apply before the next owned decision without altering another save.
Invalid edits SHALL retain the prior effective configuration and display their reason.

Need limits SHALL accept finite percentages at least100% or an explicit Unlimited choice; food
extra reserve SHALL accept nonnegative finite nutrition or Unlimited. Unlimited SHALL mean no owned
configured ceiling, never an infinite actual amount. Food scales SHALL be0–100%, visibility threshold
at least100%, and stage thresholds finite, strictly ascending with the first fixed at100%. Hunger
factors SHALL be finite and at least1; healing factors1–10; capacity reductions0–100percentage
points; vomiting0–24events/day. Rate coefficients and eating thresholds SHALL obey their specified
ranges. Whole effective configurations SHALL be checked across inherited/overridden values.
Invalid, non-finite or unrepresentable owned arithmetic SHALL not write corrupt need or health state:
withhold that owned adjustment, retain the last valid owned state and report the reason without spam.
Native behavior remains the fallback when no valid owned state is available.

#### Scenario: Save override and main menu
- **WHEN** two saves inherit a global food limit and one chooses a different override
- **THEN** later global changes affect only the inheriting save; the main menu permits global editing but no save mutation, and switching saves restores the proper effective configuration

#### Scenario: Invalid stages or amount
- **WHEN** an edit makes stage thresholds unordered, supplies a non-finite value or produces an unrepresentable owned amount
- **THEN** no corrupt result is applied, the last valid configuration/state is retained where available, and the UI or bounded diagnostic explains the fallback

#### Scenario: Unlimited is a ceiling choice
- **WHEN** Unlimited is selected and a valid finite gain is received
- **THEN** the need remains a finite amount resulting from that gain; selecting Unlimited alone grants nothing

### Requirement: Developer actions and compatibility remain explicit
Unrestricted owned need adjustments SHALL be available only in the top developer toolbar with
developer mode enabled. The selected pawn's actions SHALL offer ±1,±10 and±100percentage points of
ordinary full, using its admitted need policy; no normal need-row cheat buttons, new bottom tab or
ordinary behavior-toggle button SHALL be added. All new player-facing labels, help, scope controls,
disabled explanations and diagnostics SHALL be supported in the seven baseline languages.

An active original Need Bar Overflow SHALL suppress the owned
overlap with an explanation. Supported optional content SHALL preserve need-specific exclusions
without disabling unrelated needs. Required gain, loss, display or consequence assumptions that are
missing, changed or unknown SHALL withhold the affected owned feature group with one bounded startup
warning per skipped enabled patch, naming the failed requirement, unavailable behavior and fallback.
Compatible other modifications SHALL remain permitted. No foreign patch, setting, condition or
content list SHALL be removed. Foreign settings/health-condition migration and unsupported providers
SHALL not be presented as supported. Other megamod features SHALL continue where independently safe.

#### Scenario: Developer-only adjustment
- **WHEN** developer mode is disabled, then enabled and a selected pawn receives a toolbar+10percentage-point rest action
- **THEN** the action is hidden while disabled and applies only the admitted finite gain when enabled; ordinary need rows gain no unrestricted editor

#### Scenario: Source provider owns the overlap
- **WHEN** original Need Bar Overflow is active
- **THEN** owned overflow and its duplicated consequences remain inactive with an explanation; no provider is unpatched or settings silently imported

#### Scenario: Scoped exclusion and missing adapter
- **WHEN** supported worn content excludes rest and recreation, or its required behavior has changed and cannot be established
- **THEN** the known exclusion affects only those needs; the unknown affected adapter/feature is withheld with the required warning rather than assuming safety or disabling unrelated features

#### Scenario: Benign other modification
- **WHEN** another modification changes an unrelated part of a required game method while the consumed behavior remains supported
- **THEN** the owned feature remains eligible; the presence of another patch alone is not a reason to skip it
