# Individual care and medical supplies

Owner **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`; **1.7 beta**.
S062, replacement of Smart Medicine - Continued. Observable draft only; no behavior delivered.
Values and corrections below are explicit proposed defaults pending SM-Q1, not confirmed balance.

## ADDED Requirements

### Requirement: Conditions have individual care choices without changing general policy
MC01. An ordinary Health condition menu SHALL offer Default care, No care, No medicine, Herbal or
worse, Normal or worse, and Best care for conditions that can be tended, including those waiting
for their next tending interval. Default SHALL follow the patient's current general care advice;
an explicit choice SHALL replace that advice only for the selected conditions. No care excludes
their treatment, No medicine permits treatment without medicine, and other categories are ceilings
on allowed medicine, not guarantees that the requested medicine exists. Ordinary treatment
eligibility, self-tend permission and other independent restrictions remain.

Grouped-row actions SHALL identify their affected conditions and apply to every current member.
The player SHALL also be able to choose one specific condition. Default care SHALL reset the entire
chosen set. Mixed groups SHALL visibly report mixed choices rather than present the highest as
everyone's choice. General care SHALL never change merely because a row is inspected, medicine is
counted, a doctor starts/ends work or a condition-specific exception is used.

#### Scenario: One expensive condition
- **WHEN** a herbal-care patient has one condition set to Best and another left at Default
- **THEN** the exception can use better medicine for its own treatment while general care remains herbal and the other condition retains that limit.

#### Scenario: Group reset and individual exception
- **WHEN** a player resets a mixed three-condition group to Default and then changes only one member
- **THEN** all three initially follow general care, only the selected member changes afterward, and the group indicates mixed choices.

#### Scenario: No care and the next interval
- **WHEN** a condition is set to No care or is still waiting for its next valid tending interval
- **THEN** it is not treated now; its control remains accessible so the player can revise the choice without inventing immediate eligibility.

#### Scenario: Player edits general care during treatment
- **WHEN** a patient-specific exception is in use and the player changes general care
- **THEN** that new general choice survives completion, interruption, save/load and later condition removal.

### Requirement: Bed-rest exceptions affect voluntary convalescence only
MC02. The same condition controls SHALL allow excluding selected conditions from voluntary
post-treatment bed rest, with a visible per-condition indicator. Group actions SHALL apply to all
selected members. This SHALL NOT suppress pending treatment, downing, labor, an operation, another
non-excluded reason to rest, sleep needs, genuine incapacity or ordinary treatment effects. It SHALL
NOT alter infection, immunity, healing rates or the condition itself. Choices SHALL persist with the
condition and disappear when that condition is removed, without transferring to a later similar wound.

#### Scenario: Work after a minor treated wound
- **WHEN** an otherwise capable pawn's only medical-rest reason is a treated healing wound marked to ignore bed rest
- **THEN** that wound no longer makes the pawn choose convalescence, while ordinary work and sleep rules still apply.

#### Scenario: Another medical reason remains
- **WHEN** the pawn also needs tending, has another non-excluded illness, is downed or is in labor
- **THEN** excluding the first wound does not remove the other medical-rest reason or make the pawn capable of work.

#### Scenario: New wound after recovery
- **WHEN** an excluded wound heals and a later wound of the same kind appears
- **THEN** the new wound starts without the old exception and medical progression was never disabled.

### Requirement: Count and perform the same permitted treatment batches
MC03. For one chosen patient, treatment SHALL first consider urgent eligible conditions, then
non-urgent ones; within each class, higher care ceilings precede lower ones and ordinary tending
priority resolves ties. For this feature, any tendable non-injury, bleeding injury or injury with
infection potential is urgent. Unknown classifications SHALL be treated as urgent. This order
does not change S058's choice between different patients or interrupt explicit valid player work.

Conditions SHALL share one treatment only when the actual medicine, individual care choices and
non-urgent economy mode allow it for every member and the game's ordinary batching rules permit it.
In None mode, non-urgent conditions cannot join a medicated urgent treatment. In Lowest available
mode, they can join only if that medicine also satisfies their lowest-available choice; otherwise
they wait for their separate ordinary treatment. Counting, fetching and treatment
SHALL agree on that batch; a premium treatment cannot also tend a No-care or lower-ceiling condition.
Native time, skill, quality caps, injury batching limits and real medicine consumption remain.
Permissions and remaining need SHALL be reconsidered before the next treatment. No elapsed wait
alone grants a successful treatment or consumes an extra dose.

#### Scenario: Urgent restricted wound and premium minor wound
- **WHEN** a bleeding herbal-care wound and a non-urgent Best-care wound need treatment
- **THEN** the bleeding wound is considered first using its permitted medicine, and the minor wound cannot raise that batch's ceiling.

#### Scenario: Mixed permissions in an ordinary batch
- **WHEN** ordinary treatment would group injuries that have incompatible care choices
- **THEN** treatment separates them, preserves ordinary batching within each compatible set and spends only the doses actually used.

#### Scenario: Urgent and minor wounds share a care ceiling
- **WHEN** a bleeding wound and a minor bruise both allow Best care and would normally share one medicated treatment, but non-urgent mode is None
- **THEN** the medicated treatment handles the bleeding wound only, and the bruise receives a separate permitted unmedicated treatment.

#### Scenario: Lowest available limits actual batching
- **WHEN** an urgent wound uses industrial medicine but a minor wound's Lowest available choice is herbal
- **THEN** the minor wound does not join the industrial treatment; if both treatments select the same permitted medicine, ordinary compatible batching remains possible.

#### Scenario: Policy changes before the next dose
- **WHEN** the player lowers care or disables self-tending while medicine is being fetched
- **THEN** the next treatment honors the new permission and unused supplies remain real items available for normal recovery.

### Requirement: Non-urgent economy has explicit mutually exclusive behavior
MC04. Non-urgent medicine preference SHALL offer Ordinary, Lowest available and None, initially
Ordinary. Lowest available SHALL choose the lowest-potency eligible available medicine for
non-urgent treatments; None SHALL perform them without medicine. Urgent treatment SHALL retain
ordinary highest-potency choice within its care ceiling. An explicit No-care or No-medicine
condition choice remains decisive. Lower potency does not imply equal effectiveness.

With no eligible medicine, otherwise permitted tending without medicine SHALL remain possible;
an unavailable premium stock SHALL NOT make the pawn wait indefinitely. The feature SHALL NOT
promise a general doctor/bed-dependent cheapest-equally-effective calculation. Anesthesia-only
operations under Lowest available SHALL prefer their lowest eligible medicine, while other
operations retain ordinary surgical medicine preference and required ingredients.

#### Scenario: Save better medicine for urgent care
- **WHEN** Lowest available is selected and separate urgent and minor treatments can use herbal and industrial medicine
- **THEN** the urgent treatment prefers industrial and the minor treatment prefers herbal, subject to each condition's ceiling.

#### Scenario: No medicine does not waive surgery ingredients
- **WHEN** None is selected and the patient has minor tending plus an operation requiring medicine
- **THEN** the minor tending can be unmedicated and the operation still requires its real ingredients.

#### Scenario: Lowest anesthesia medicine
- **WHEN** Lowest available is selected and an anesthesia-only bill accepts two medicine strengths
- **THEN** the lower-potency eligible medicine is preferred without changing the bill's quantity or effects.

### Requirement: Medicine sources respect independent roles and current permissions
MC05. Selection SHALL consider reachable permitted ground medicine plus enabled sources from the
doctor, the patient, other free colonists and colony animals. Each inventory role SHALL have its
own switch, initially on; a doctor or patient uses that specific role rather than being removed by
the other-colonist/animal switch. A non-player patient can supply its own medicine only when the
ordinary treatment and inventory-transfer permissions allow that use. Unrelated hostile inventories,
other maps, sealed inaccessible contents and unsupported special-patient supplies are not donors.

Each source SHALL satisfy the current care ceiling, patient compatibility, access, forbidden state,
reservation, transfer and shared carrying-capacity rules. A donor's personal stock target does not
prevent a permitted medical donation, but its remaining quantity SHALL update immediately. An item
already allocated to another active medical/bill action SHALL NOT be promised again. Inventory-only
actions SHALL preserve their native scope: if only the doctor's inventory is allowed, other donors
and ground stock are excluded.

#### Scenario: Animal switch does not remove a human patient
- **WHEN** animal donations are disabled and patient-inventory use is enabled for an eligible human patient
- **THEN** the patient's permitted medicine remains a candidate.

#### Scenario: Reserved or forbidden medicine
- **WHEN** the best medicine is forbidden, reserved for another action or no longer transferable
- **THEN** it is excluded without being unforbidden, duplicated or taken remotely; another eligible source or permitted unmedicated tending remains possible.

#### Scenario: Inventory-only drafted treatment
- **WHEN** a native treatment command permits only the doctor's carried inventory
- **THEN** choosing it does not silently fetch another pawn's medicine or ground supplies.

### Requirement: Rank useful medicine and collect only useful physical quantities
MC06. Normal medicine selection SHALL prefer higher potency within the applicable care ceiling,
then the shorter currently traversable journey from doctor through supply to patient, then inventory
on a complete tie. Lowest available reverses only potency preference. Other donors SHALL initially
be limited to a **12-cell detour**, configurable 0–99; Unlimited donors removes that range limit,
not permissions or emergency timing. A cell detour is half the added orthogonal map distance via
the supply compared with going directly to the patient; walls still affect actual reachability and
journey ranking. Doctor and patient supplies are not rejected by the other-donor range switch.

Prefer equal ground medicine SHALL initially be on with a **6-cell detour**, configurable 0–99.
It permits equal-potency ground stock within that detour to replace inventory medicine. This is
not a maximum search radius when no inventory supply exists. After the primary choice, collect
additional equally suitable supplies within **8 orthogonal cells of the primary location**, only
up to the remaining permitted treatment need and shared physical capacity. Distinct item types
remain distinct; equal potency does not make them mergeable. Every physical unit SHALL be counted
once. Shortage or partial reservations SHALL reduce actual pickup rather than create phantom cargo.

#### Scenario: Nearby ground supply preserves a personal stock
- **WHEN** equal-potency ground and inventory medicine are eligible and ground falls inside the configured ground detour
- **THEN** ground medicine is preferred while a higher-potency permitted supply still outranks both in Ordinary mode.

#### Scenario: Wall or distant donor
- **WHEN** a donor is geometrically nearby but unreachable, or outside the enabled donor detour
- **THEN** their medicine is excluded; Unlimited removes only the second restriction.

#### Scenario: Two small stacks and a third incompatible type
- **WHEN** several nearby equal-potency stacks are useful for a patient's permitted treatments
- **THEN** the doctor can physically collect useful quantities without counting a stack twice, merging incompatible types or exceeding shared capacity.

#### Scenario: Source splits or disappears
- **WHEN** another action splits, consumes or moves a selected stack before pickup
- **THEN** the doctor rechecks the real available stack, adjusts or chooses again, and neither loses medicine nor follows an obsolete split reference.

### Requirement: Surgery preferences relax only the patient's routine medicine ceiling
MC07. A per-pawn Surgery uses best choice SHALL appear in Health and the existing Assign medical
controls where applicable. Initially off for newly initialized eligible pawns, it SHALL allow an
operation to use the best recipe-eligible medicine independently of the patient's ordinary tending
ceiling. It SHALL NOT enable doctor work, create a bill, waive required ingredients, authorize
unavailable surgery, change the ordinary success formula or guarantee success. The actual selected
medicine SHALL retain its ordinary effect on surgical outcomes. Existing individual choices SHALL survive global
default edits and ordinary medical-policy resets. The shared setting controls only new choices.

The surgeon's eligible inventory SHALL participate alongside ordinary operation ingredients;
the doctor-inventory switch still applies. Other donors are not added to surgery by this feature.
Bill filters, selected patient, operation type, actual quantities and ordinary collection remain.
S024 ingredient handling and S059 material preferences SHALL not replace this medicine-specific
choice, duplicate its collection or reorder an explicitly selected material outside its rules.

#### Scenario: Cheap routine care and best surgery medicine
- **WHEN** a herbal-care patient has Surgery uses best enabled and a valid operation accepts industrial medicine
- **THEN** the operation can consume industrial medicine while ordinary tending retains herbal care.

#### Scenario: Operation still lacks another ingredient
- **WHEN** the surgery preference is on but a required implant or permitted medicine is absent
- **THEN** the preference neither starts an impossible operation nor substitutes an unapproved ingredient.

#### Scenario: Better medicine retains its ordinary surgical effect
- **WHEN** the preference causes an eligible operation to use better medicine
- **THEN** the operation uses that medicine's ordinary effect on success, without a separate success bonus or guaranteed outcome.

#### Scenario: New-pawn default changes
- **WHEN** the default is changed after existing pawns have individual choices
- **THEN** those choices remain and newly initialized eligible pawns receive the new default once.

### Requirement: Medical configuration uses the common global and save interface
MC08. Actual configuration SHALL use ordered Medical care, Field care and Personal supplies
sections in mod settings, using the common load/store/resolve/render behavior. Each row SHALL show
Global default, Use global and Current save. Inheritance follows the current global value; save
overrides do not modify globals or another save. Inherited editors are inactive, and both current-save
controls are inactive at the main menu, including mutation callbacks. No separate medical-default
settings window or source cache-tuning option SHALL be added.

| Preference | Built-in value / allowed range |
| --- | --- |
| Doctor, patient, other-colonist and colony-animal inventory medicine | Each on |
| Prefer equal ground medicine; ground detour | On;6 cells, integer0–99 |
| Other donor detour; Unlimited donors | 12 cells, integer0–99;off |
| Non-urgent medicine | Ordinary / Lowest available / None; Ordinary initially |
| New-pawn Surgery uses best | Off |
| Emergency field care; emergency travel allowance | On;2 game hours,0–6 hours |
| Ordinary field care | Off / No suitable bed / Always; Off initially |
| Personal stocking; colony supply threshold | On;150%,0–500%,0 disables scarcity gate |
| Return personal stock under scarcity | Off |
| Show extended personal-supply catalogue | Off |

Committed edits SHALL govern the next decision/treatment/pickup and refresh the displayed status;
they SHALL NOT retroactively consume supplies, cancel valid completed work, reset individual orders
or silently import upstream settings. Pawn/condition/stock targets and clipboard contents are not
inherited settings. All ordinary controls SHALL work in both ordinary and owned compact views,
remain readable in the seven required languages, and add no bottom main tab or debug-only gameplay.

#### Scenario: Global edit and overridden save
- **WHEN** a global medicine preference changes while one save inherits and another overrides it
- **THEN** the inherited save uses the new value at its next decision and the overridden save retains its value.

#### Scenario: Main-menu settings
- **WHEN** settings open without an active save
- **THEN** global controls work and all current-save editing/reset paths are disabled without affecting a previous save.

#### Scenario: Compact and ordinary controls
- **WHEN** the player switches between owned compact and ordinary Health views or changes language
- **THEN** the same condition choices and current-language meanings remain available without stale labels or duplicate actions.

### Requirement: Unsupported medical integration has an explicit limited fallback
MC09. Every enabled integration SHALL be admitted at startup against its documented requirements.
Compatible changes SHALL be allowed; another mod merely touching the same game method is not
failure. A failed integration SHALL stay inactive with a bounded warning naming the failed patch,
target requirement, unavailable behavior and fallback. Dependent care selection, counting and
actual treatment SHALL fail together when they cannot honor one policy; independent stock controls
or display enhancements can remain when safe. Inactive choices SHALL remain saved and visibly
unavailable, not appear enforceable or silently revert to a different medical promise.

With original or Continued Smart Medicine active, owned S062 overlap SHALL be inactive with a
startup explanation, leaving the provider untouched. Optional display failure SHALL preserve a
reachable ordinary Health control path. Supported Pharmacist advice can supply Default care;
explicit condition choices still win. Unknown extended care categories or special-patient supply
rules SHALL leave that affected owned decision to the remaining game/provider with a warning,
not be guessed as Best or No care. No source package identity or assembly compatibility is promised.

#### Scenario: Counting is supported but consumption is not
- **WHEN** actual treatment cannot be guaranteed to obey the same individual care choices
- **THEN** the dependent care enhancement stays inactive, stored choices are labeled unavailable, and startup logs identify the fallback.

#### Scenario: Optional health display fails
- **WHEN** an external health display changes incompatibly
- **THEN** only its adapter is disabled and the player retains access to supported ordinary care controls.

#### Scenario: Source provider or extended medical categories
- **WHEN** a source provider is active, or an unsupported provider changes the relevant care categories
- **THEN** the corresponding owned overlap does not run simultaneously or guess meanings, and logs explain which behavior remains available.

#### Scenario: Save loaded with unresolved conditions
- **WHEN** an owned save contains removed conditions or unresolved optional supply definitions
- **THEN** valid individual choices remain, removed conditions lose only their obsolete choices, and unsupported saved selections are shown unavailable without treating or consuming anything on load.
