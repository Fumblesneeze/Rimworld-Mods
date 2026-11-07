# Child growth and learning factor

Owner **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **RimWorld 1.7 beta**.
S088 / Growth Speed Makes Sense Now. Observable specification only; no implementation or acceptance.

## ADDED Requirements

### Requirement: Future growth points scale with global learning factor once
The mod SHALL multiply an eligible pawn's otherwise applicable growth-points-per-day rate by that
pawn's effective Global Learning Factor exactly once. A factor of 100% SHALL preserve the rate;
values above and below 100% SHALL proportionally increase or decrease it. The factor SHALL come
from the pawn's ordinary current stat, including supported traits, genes and other stat effects,
without a second owned multiplier or a special colonist-only restriction. Native elapsed-time
accumulation SHALL use that adjusted rate.

#### Scenario: Faster and slower learning
- **WHEN** three otherwise equivalent eligible children have an ordinary rate of 2 points per day and learning factors of 150%,100% and50%
- **THEN** their adjusted daily rates are respectively3,2 and1, with future accumulation using those rates

#### Scenario: Native learning conditions still matter
- **WHEN** reduced learning satisfaction halves a child's otherwise applicable rate while its learning factor stays150%
- **THEN** the adjusted rate also halves; the multiplier does not substitute for learning activities or fill the learning need

#### Scenario: No factor compounding from inspection
- **WHEN** a player repeatedly selects the same child or reopens its growth tooltip
- **THEN** the same current rate is shown without multiplying it again or awarding any points for inspection

### Requirement: Native growth eligibility and growth-vat rules remain authoritative
The mod SHALL retain the ordinary conditions under which a pawn can accumulate growth points,
including pauses pending a growth choice and restrictions on unsupported ages or suspended needs.
An ordinary zero rate SHALL remain zero. A growth-vat rate SHALL receive the same learning-factor
multiplier when ordinary growth-point accumulation uses it; the vat's existing exception to ordinary
suspension SHALL remain. This feature SHALL NOT create growth-point eligibility, restore a missing
learning need, or modify vat power, food, aging or skill-training rules.

#### Scenario: Growth vat
- **WHEN** an eligible child in a growth vat has an ordinary rate of2 points per day and150% Global Learning Factor
- **THEN** its applicable growth-point rate is3 per day, without changing the vat's operation or adding another award of growth points

#### Scenario: Suspended ordinary learning
- **WHEN** a child outside a growth vat has no applicable growth-point gain because ordinary learning is suspended
- **THEN** a high learning factor does not grant growth points through this feature

#### Scenario: Growth choice pending
- **WHEN** the native game pauses further growth-point accumulation while a birthday choice is unresolved
- **THEN** this feature does not bypass that pause or generate another choice; ordinary accumulation resumes only when native rules permit

#### Scenario: Other supported children
- **WHEN** a prisoner, slave or other non-colonist child is otherwise eligible for ordinary growth-point accumulation
- **THEN** the same learning-factor rule applies, while ordinary availability of player controls and rewards remains unchanged

### Requirement: Development and earned history are not rewritten
The mod SHALL change future growth-point rate only. Biological and chronological aging, learning-need
gain/loss, ordinary skill experience, toddler walking/manipulation learning, growth-tier thresholds,
growth-birthday timing and reward options SHALL retain their own rules. Existing earned points and
pending choices SHALL persist unchanged across activation, save/load and stat changes. Native
generation or initialization of historical growth points SHALL remain unscaled; no retrospective
compensation, subtraction or source migration SHALL be performed.

#### Scenario: Existing child starts using the feature
- **WHEN** a child with90 saved growth points and150% learning factor begins using the feature
- **THEN** it still has90 points immediately; only subsequent ordinary accumulation uses the adjusted rate

#### Scenario: Reach a better tier before a birthday
- **WHEN** faster future accumulation reaches an existing tier threshold before the next ordinary growth birthday
- **THEN** the existing progress display reflects that tier, and the ordinary birthday offers that tier's native options at its usual age; no immediate or additional reward is created

#### Scenario: Highest tier
- **WHEN** a child reaches the ordinary highest reward tier
- **THEN** the feature does not add a new tier or extra reward beyond native rules, and the ordinary maximum-tier display remains authoritative

#### Scenario: Generated child and saved history
- **WHEN** the game generates or natively initializes a child's past growth points, or loads a child whose points already exist
- **THEN** that historical result is not multiplied; future eligible accumulation uses the learning-factor rule without replay on reload

#### Scenario: Toddler skills and unrelated growth
- **WHEN** toddler movement/manipulation progress, ordinary skill XP, aging or plant growth advances
- **THEN** this growth-point feature adds no multiplier to those separate systems

### Requirement: Existing displays and future updates use the same current rate
The mod SHALL keep growth progress and rate information on the existing native contextual growth
display, using its ordinary visibility, rounding and maximum-tier behavior. Displays and supported
rate consumers SHALL read the same adjusted value used for future growth-point gain. Changes to the
learning factor SHALL affect subsequent queries with no more than **240 simulated ticks** of
cached-stat staleness, followed by the ordinary accumulation cadence. Merely changing the factor
SHALL NOT award points. No new settings, bottom tab, toggle or per-pawn editor SHALL be added.
Existing localized UI SHALL be retained; any new unavailability text SHALL use all seven languages.

#### Scenario: Native progress tooltip
- **WHEN** the player selects a child whose ordinary visible rate is2 and whose learning factor is150%
- **THEN** its existing rate tooltip reports3 using native formatting, and observed future growth accumulation agrees with that rate under unchanged conditions

#### Scenario: Stat changes during childhood
- **WHEN** an ordinary supported stat change moves the learning factor from100% to150%
- **THEN** subsequent rate queries and normal accumulation adopt the changed factor within the stated cache freshness and ordinary update cadence, without rescaling previously earned points

#### Scenario: Supported rate consumer
- **WHEN** another supported feature uses the child's current daily growth rate to calculate its own ordinary result
- **THEN** it receives the adjusted rate once; this feature neither repeats the multiplier in that consumer nor supplies an additional growth-point award

### Requirement: Invalid rate inputs do not produce an invalid owned rate
The mod SHALL preserve an ordinary zero rate and allow a valid zero learning factor to stop future
gain without reducing earned points. If a learning factor is negative, non-finite or produces an
unrepresentable scaled rate, the owned multiplier SHALL leave a valid ordinary rate unscaled and
emit a bounded diagnostic. If the incoming ordinary rate itself is negative or non-finite, the
owned result SHALL be zero with a bounded diagnostic. This SHALL NOT silently repair or rewrite
already-corrupt saved growth history. These safeguards SHALL NOT cap ordinary valid positive factors
or impose a new reward tier.

#### Scenario: Valid zero factor
- **WHEN** an eligible child has a valid0% learning factor
- **THEN** it gains no future points through this rate while retaining all points already earned

#### Scenario: Invalid factor from a provider
- **WHEN** a provider supplies an invalid factor or an overflowing scaled rate while the ordinary rate is valid
- **THEN** that query retains the ordinary rate, reports the fallback without per-query log spam, and does not subtract points or expose an invalid owned rate

#### Scenario: Invalid ordinary rate
- **WHEN** the incoming ordinary rate is negative or non-finite
- **THEN** the owned result is zero with a bounded diagnostic, without changing existing saved points

### Requirement: Required content and provider admission prevent duplicate scaling
The mod SHALL activate this feature only with Biotech and supported growth-rate semantics. Without
Biotech, it SHALL remain inactive with an explanation while unrelated features remain available.
An active original Growth Speed Makes Sense Now SHALL own the
overlap and suppress the owned multiplier. Compatible stat/rate changes SHALL remain permitted;
missing, ambiguous or already-applied scaling semantics SHALL withhold the affected owned patch
with one bounded startup warning identifying the target, failed requirement, unavailable behavior
and fallback. No source package identity, assembly or saved settings SHALL be impersonated.

#### Scenario: Original source active
- **WHEN** original Growth Speed Makes Sense Now is active
- **THEN** the owned multiplier remains inactive with an explanation and does not turn a150% factor into225% through duplicate scaling

#### Scenario: Biotech absent
- **WHEN** Biotech is not active
- **THEN** this child-growth feature does not activate or require a fictional child-growth system, and unrelated megamod features remain usable

#### Scenario: Compatible provider or changed semantics
- **WHEN** a provider changes the ordinary rate without applying this learning-factor rule, or the required rate semantics have instead become unsupported
- **THEN** a verified compatible rate is scaled once; the unsupported case withholds the affected owned patch and explains its fallback without disabling or unpatching the foreign provider
