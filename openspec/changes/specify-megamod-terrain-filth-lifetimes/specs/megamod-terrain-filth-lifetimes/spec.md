Owner **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **RimWorld 1.7 beta**.
S105 replacement of Permeable Terrain. Specification only; no gameplay accepted.

## ADDED Requirements

### Requirement: Absorbent surfaces shorten the natural lifetime of liquid filth
The feature SHALL multiply a newly assigned liquid filth lifetime by the receiving surface's
permeability fraction and the Permeation modifier. The ordinary lifetime is the one supplied by the
current game and supported content for that creation, including its normal variation. It is not
replaced with a fixed number of days or rerolled by this feature.

Default surface fractions are **10%** for footprint-taking terrain such as soil and sand, straw
matting and packed dirt, and **50%** for ice. Ice uses its own fraction once even though it also
takes footprints. An explicit supported surface rule takes precedence over these defaults.
The surface currently receiving the filth is used, including the game's exposed floor/foundation
or temporary surface; buried soil is not used through a sealed covering. Roofs and weather do not
change which fraction is selected. Ordinary filth placement restrictions remain in force.

#### Scenario: Blood on soil and a sealed floor
- **WHEN** new blood with an ordinary 40-day lifetime appears on soil and on a sealed floor at default settings
- **THEN** the soil stain receives a 4-day natural age limit and the sealed-floor stain keeps 40 days
- **AND** normal cleaning, rain or other removal can act earlier

#### Scenario: Ice precedence
- **WHEN** new liquid filth with a 40-day ordinary lifetime appears on ice at default settings
- **THEN** its age limit becomes 20 days, without also multiplying by the soil fraction

#### Scenario: Straw matting and packed dirt
- **WHEN** liquid filth with a 10-day ordinary lifetime appears on straw matting or packed dirt
- **THEN** its default age limit is 1 day even if the surface does not use footprints
- **AND** a roof over the same surface does not disable that reduction

#### Scenario: Covered soil and placement restrictions
- **WHEN** a sealed floor covers soil or the ordinary game rejects a filth deposit at a cell
- **THEN** covered soil does not confer permeability through the floor, and this feature does not force a rejected deposit to appear

### Requirement: Classification includes supported content without relying on names
The feature SHALL recognize the game's fluid-cleanup filth family and explicit supported liquid
classification, including inherited classifications. Ordinary examples include blood and blood
smears, insect blood, vomit, slime, amniotic fluid, corpse bile, firefoam and water puddles when present.
The current reference also classifies machine bits and chemfuel puddles in this family; retain that
classification without removing their independent behavior. Other filth uses the separate rule below.

Supported content can declare a specific positive finite surface fraction or explicit liquid
classification without a name-based guess. An explicit surface fraction replaces the inferred
fraction once. Conflicting or invalid declarations leave that affected creation's ordinary lifetime
unchanged with an explanatory diagnostic; unrelated valid content continues. A familiar label alone
does not classify a resource, terrain or filth as liquid or absorbent. Only an actually enabled
footprint property supplies the generic terrain rule; an explicitly disabled property does not.

#### Scenario: Inherited blood and machine debris
- **WHEN** an inherited blood-smear type or ordinary machine bits appears on soil with a finite natural lifetime
- **THEN** it receives the appropriate liquid-family reduction once
- **AND** its appearance, cleaning work and other properties are preserved

#### Scenario: Explicit custom terrain and filth
- **WHEN** supported content declares a liquid stain and a surface fraction of 20%, despite using neither ordinary fluid cleanup nor footprints
- **THEN** a 10-day ordinary lifetime becomes 2 days at the default Permeation modifier
- **AND** no automatic terrain fraction is compounded with that explicit fraction

#### Scenario: Similar names and disabled footprints
- **WHEN** unrelated content is named water, a liquid mixture or soil, or a terrain explicitly disables footprints
- **THEN** those names and the disabled footprint property do not activate a liquid or permeability rule
- **AND** independent supported declarations or ordinary fluid classification can still apply

#### Scenario: Invalid or ambiguous content declaration
- **WHEN** a selected custom rule has a nonpositive or nonfinite surface fraction, or contradictory equally authoritative declarations
- **THEN** the affected creation retains its ordinary lifetime and a bounded diagnostic identifies the unavailable rule
- **AND** it is not silently reclassified into another lifetime branch

### Requirement: Other filth uses a separate multiplier and safe timing
The feature SHALL use the Other filth lifespan multiplier for any valid combination that is not
liquid filth on a permeable surface, including liquid filth on sealed floors. It is an alternative
branch, not another factor applied to permeated filth. The Permeation modifier defaults to **100%**
and ranges from **25% through 300%**. Other filth lifespan defaults to **100%** and ranges from
**1% through 300%**. Values above 100% lengthen the applicable result; they are not a percentage
removed. For example, 300% Permeation makes the ice age limit 150% of its ordinary value.

An ordinary no-expiration lifetime SHALL remain without age-based expiration. Finite positive
results are rounded down to whole simulated ticks, with a minimum of **1 tick**, so small positive
results never become permanent. Invalid or unrepresentable calculations leave the ordinary lifetime
unchanged with a bounded diagnostic. One game day is **60,000 simulated ticks**; paused play does
not age a stain. Natural age expiration occurs on the applicable ordinary environment check after
the age limit is exceeded, not at an exact wall-clock deadline.

#### Scenario: The two controls do not multiply together
- **WHEN** Permeation is 50%, Other filth lifespan is 200%, and a stain's ordinary lifetime is 10 days
- **THEN** liquid filth on soil receives 0.5 days, liquid filth on a sealed floor receives 20 days, and non-liquid dirt receives 20 days

#### Scenario: Existing no-expiration behavior
- **WHEN** an ordinary blast mark or custom stain has no age-based expiration
- **THEN** neither multiplier invents an expiration date or prevents another ordinary removal mechanism

#### Scenario: Tiny or unrepresentable duration
- **WHEN** a valid positive result would be shorter than one simulated tick
- **THEN** its age limit becomes one tick and it remains eligible for ordinary age-based cleanup
- **AND** a result that cannot be represented safely instead keeps the original lifetime with an explanation

#### Scenario: Paused play and ordinary cleanup scheduling
- **WHEN** the game is paused or a stain has just reached its assigned age limit
- **THEN** pause adds no age and reaching the limit alone does not force immediate deletion
- **AND** ordinary age cleanup removes the stain on an eligible check after the limit is exceeded

### Requirement: Existing stains preserve their assigned lifetime and native aging rules
The feature SHALL apply only when the game assigns a fresh natural lifetime. Loading, transporting
an existing stain on a gravship, inspecting it, changing settings or replacing its underlying floor
does not multiply its stored lifetime again. If an ordinary respawn genuinely assigns a new lifetime,
that new assignment is adjusted once. Adding this feature to a save does not retrospectively change
the lifetimes of already saved stains; removing it does not restore their previous durations.

Adding filth to an existing stain retains the ordinary thickening behavior: its age restarts, its
thickness changes only as normally permitted, and its stored lifetime is retained. Native cleaning,
rain thinning, spread, source attribution, beauty, cleanliness, fire protection and independent
provider aging remain unchanged. An independent removal can therefore act earlier than this
feature's adjusted natural age limit.

#### Scenario: Save reload and later configuration change
- **WHEN** a stain with an adjusted 4-day age limit is saved, reloaded, and the multiplier then changes
- **THEN** that stain keeps its 4-day limit rather than receiving another reduction
- **AND** newly assigned lifetimes use the current effective setting

#### Scenario: Transport and changed surface
- **WHEN** an existing stain travels on a gravship or its floor is replaced with a different surface
- **THEN** this feature preserves the existing stored age limit and does not reinterpret it using the destination surface
- **AND** native transport, age and placement rules remain authoritative

#### Scenario: New lifetime during an ordinary respawn
- **WHEN** an ordinary respawn actually initializes a fresh 40-day lifetime on soil
- **THEN** the new lifetime becomes 4 days at defaults, once
- **AND** the previous adjusted value is not used as the new baseline

#### Scenario: Thickening and cleaning
- **WHEN** additional filth thickens a stain with a 4-day limit and a pawn subsequently cleans it
- **THEN** native thickening restarts its age without changing the 4-day limit
- **AND** cleaning consumes the ordinary work and removes thickness normally, with no new deposit or artificial refund

#### Scenario: Independent puddle or fuel behavior
- **WHEN** supported puddle behavior extends a stain's lifetime in rain or a chemfuel puddle dries through its separate native mechanism
- **THEN** that behavior remains effective without repeated multiplication of the extension or suppression of the earlier removal

### Requirement: Configuration uses common scope and communicates future-only effects
The feature SHALL put its two gameplay controls in the shared Environment and cleaning settings
section. Each uses Global default, Current save and Use global. Missing save choices inherit the
current global default; explicit overrides affect only their save. Checking Use global retains a
dormant override, and unchecking restores it or seeds a first override from the current inherited
value. Inherited save editors are inactive. Main-menu save editors, inheritance controls and resets
are disabled in both appearance and mutation behavior.

Controls SHALL display consistent percentages, their defaults/ranges, examples for soil and ice,
and that existing stains retain their assigned lifetime. Invalid interactive edits preserve the
last valid value. An invalid stored entry uses the built-in 100% default for that entry with an
explanation, preserving other valid entries. Save switching resolves the new context before any new
lifetime is assigned; main-menu configuration uses globals without altering a previous save.
There is no bottom tab, entity gizmo or bottom-right toggle for this feature.

#### Scenario: Inheriting and overridden saves
- **WHEN** a global multiplier changes while one save inherits it and another has an explicit override
- **THEN** subsequent fresh lifetimes use the new global value only in the inheriting save
- **AND** old stains and the other save's override remain unchanged

#### Scenario: Main menu and dormant override
- **WHEN** settings are opened from the main menu, or a save switches its choice to Use global and back
- **THEN** main-menu save actions cannot mutate a save, and the save can restore its dormant override when a game is active

#### Scenario: Invalid saved value and percentage help
- **WHEN** a stored multiplier is invalid and the settings page is opened
- **THEN** only that entry falls back to 100%, the issue is explained, and other valid choices remain intact
- **AND** the page clearly distinguishes a multiplier of 250% from 25% and shows their different future lifetime effects

### Requirement: Optional content retains its own lifetime and rain choices
The feature SHALL compose with current supported provider lifetimes rather than replacing them
with historical fixed ranges. In particular, ReGrowth and FSF filth changes retain their current
base aging and rain rules before this feature's one applicable multiplier. No provider's rain
behavior, cleaning sound or unrelated terrain/filth properties are rewritten merely to classify it.

Supported ReGrowth water puddles and water spatters SHALL receive explicit liquid classification
when their actual content is present and compatible, without renaming them or changing their cleaning
sound. Other supported modded filth with the ordinary fluid classification works automatically.
Dubs Bad Hygiene's inspected urine, faeces and raw sewage do not acquire liquid classification merely
from their names; without a separate supported declaration they follow Other filth lifespan.
Missing content does not block Core behavior. Legacy external declarations requiring the original
package are not silently enabled by pretending that package is installed.

#### Scenario: Existing provider reduction
- **WHEN** FSF or ReGrowth supplies a 15-day ordinary lifetime for a supported liquid stain on soil
- **THEN** default settings produce a 1.5-day age limit from that existing value
- **AND** no automatic 250% compensation is imposed and the provider's rain rule remains intact

#### Scenario: ReGrowth content mapping
- **WHEN** admitted ReGrowth water or water spatter receives a fresh lifetime on soil
- **THEN** its lifetime receives the liquid branch once and its original cleaning sound and independent rain behavior remain intact
- **AND** already-present lifetime data is neither duplicated nor replaced with a historical range

#### Scenario: Unclassified hygiene content
- **WHEN** the inspected Dubs Bad Hygiene urine, faeces or raw sewage appears without another supported liquid declaration
- **THEN** Other filth lifespan applies, rather than an assumed liquid rule based on the visible name

#### Scenario: Missing provider or legacy package-gated declaration
- **WHEN** an optional provider is absent or an external declaration is conditional on the original Permeable Terrain package
- **THEN** ordinary Core behavior remains available and no missing-content error is introduced
- **AND** the legacy package condition is not spoofed; any unsupported mapping is stated explicitly

### Requirement: Diagnostics belong to the developer toolbar and remain bounded
The feature SHALL provide its terrain classification report and optional creation trace through
the top debug toolbar, available only in developer mode. A report identifies current supported
surfaces, fractions and unavailable mappings without changing filth. An explicitly started trace
records the next **20** relevant fresh lifetime assignments at most, with original and adjusted
durations, surface and selected rule, then stops. Trace state is transient, off by default, cleared
on developer-mode exit or game teardown, and never stored as a colony setting. Durations retain
useful fractions rather than truncating all sub-day lifetimes to zero days.
Settings, help and diagnostics support all seven baseline languages without clipped controls.

#### Scenario: Requested report and finite trace
- **WHEN** a developer requests a report and starts a creation trace
- **THEN** the report describes current classification without mutating stains, and no more than 20 subsequent qualifying assignments are logged
- **AND** logging stops at that limit or on developer-mode exit, without per-tick trace messages

#### Scenario: Ordinary play and localization
- **WHEN** developer mode is off and a player opens settings in any supported language
- **THEN** debug actions and trace controls are absent, while gameplay settings and future-only explanations remain readable
- **AND** no diagnostic switch is added to the gameplay settings page

### Requirement: Failed admission and original-provider authority avoid duplicate effects
The feature SHALL apply only when its documented target requirements hold, allowing compatible
other patches and unrelated code changes. A changed or ambiguous lifetime-assignment path leaves
new lifetimes ordinary and displays unavailability; startup logs give one bounded warning per skipped
enabled patch naming the failed requirement, affected behavior and fallback. Partial classification
failure must not silently send affected content through the wrong multiplier branch. Independent
supported content continues when its required paths remain valid.

When original or continued Permeable Terrain is active under its shared package, it SHALL own the
overlap and our lifetime adjustments remain dormant with an explanation. Existing stored durations
and foreign behavior are never undone. Owned cleaning, construction, need/health and fire features
use this same lifetime rule through their ordinary filth creation paths, without duplicate scaling.

#### Scenario: Compatible patches and changed lifetime initialization
- **WHEN** a compatible unrelated patch is present, or the required lifetime assignment is removed or ambiguously changed
- **THEN** compatible behavior remains enabled, while the changed required path is skipped with a bounded warning and truthful unavailable state
- **AND** an existing saved stain is not rewritten as fallback

#### Scenario: Original or continued provider is active
- **WHEN** either original or continued Permeable Terrain is active with the megamod
- **THEN** only that provider performs the overlapping lifetime adjustment and our saved settings remain dormant
- **AND** no foreign patch or stored stain duration is removed to force our preferred result

#### Scenario: Owned features create or remove filth
- **WHEN** an owned feature produces vomit or cleaning/construction removes filth through the ordinary game workflow
- **THEN** each genuinely new lifetime receives at most one owned adjustment and removal retains its original eligibility and consequences
- **AND** enabling multiple owned features does not introduce duplicate aging or a second cleaning job
