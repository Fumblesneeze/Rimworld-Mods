# Expansion and optional-provider queries

Owner **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`; **1.7 beta**.
S121 TD Find Lib - Continued. Specification only.

## ADDED Requirements

### Requirement: Keep unavailable content distinct from an ordinary nonmatch
Expansion and optional-provider criteria SHALL follow the ordinary catalog's ranges, knowledge,
copy fidelity, physical identity and unavailable-query rules. Inactive content is not offered as a
working ordinary criterion. Existing saved clauses remain intact and visibly unavailable when their
content/provider disappears. Turning on developer mode does not activate absent expansion behavior.
Unsupported optional observations disable only their adapter and dependent queries. Base queries,
saved data and unrelated consumers remain usable. A compatible provider change is not rejected just
because its version, whole file or other patches differ.

#### Scenario: Remove a required expansion
- **WHEN** a saved gene query is opened with Biotech inactive
- **THEN** the gene clause and its selections remain visible for repair, the query has no authoritative result, and ordinary non-gene searches still work.

### Requirement: Query Royalty titles obligations and psychic resources
Royalty SHALL add these disclosed observations. Each exact faction/title/permit choice preserves
its identity rather than its label; unavailable concrete choices require explicit selection.

| Criterion | Meaning and initial choice |
| --- | --- |
| Title | Exact title, any or none; None initially; a pawn must have the relevant royalty state |
| Title range | Highest title seniority for selected faction or any faction between chosen titles; Any faction and unrestricted seniority initially; no title is a nonmatch |
| Unmet title requirement | Any or selected missing throne, undignified throne room, missing personal bedroom, undignified bedroom, no acceptable available food, missing required apparel or insufficient apparel quality; Any initially; honor native suspended/grace-period requirements |
| Honor | Current favor with exact faction or any recorded faction, initially Any and unrestricted; missing exact-faction honor can be zero, while Any does not invent unrecorded faction entries |
| Permit | Exact or any permit, exact or any faction and Only ready switch; Trade settlement if available, otherwise Any; Any faction and Only ready on initially |
| Permit points | Sum of unspent points for disclosed factions, initially one or more |
| Psyfocus | Current psyfocus of a pawn with psylink, unrestricted initially |
| Neural heat | Current heat of a pawn with psylink, unrestricted initially |

Food availability uses the pawn's ordinary food restrictions and access rules without reserving or
ordering a meal. A pawn with no applicable title cannot acquire an unmet-apparel-quality match or
error merely from an empty title set. Ready permit means off cooldown, not guaranteed execution at
every possible target. Permit faction and readiness survive all copy/save/transfer paths.

#### Scenario: Keep a permit query intact
- **WHEN** an Empire permit query with Only ready off is copied, saved and reopened
- **THEN** it still includes cooldown permits from that faction and does not revert to any faction or ready-only.

#### Scenario: Observe suspended obligations
- **WHEN** the game suspends a pawn's room requirement or it is within its grace period
- **THEN** the corresponding Unmet title requirement follows that native exemption without creating a false violation.

### Requirement: Query Ideology identity beliefs and visible roles
Ideology SHALL add the following criteria.

| Criterion | Meaning and initial choice |
| --- | --- |
| Style | Exact, any, none or style category; Any initially; Other category means an actual uncategorized style, not no style |
| Ideoligion | Exact ideology, player's primary or another ideology; Player's primary initially; a pawn with no ideology does not match these choices |
| Meme | Pawn's ideology includes selected non-structural meme, initially Darkness |
| Precept | Pawn's ideology includes selected ordinary disclosed precept, initially Meat eating: abhorrent; presentation issue groups do not change its identity |
| Role | Exact, any, single-holder or multiple-holder role; currently held versus requirements met, plus no role and unmet role apparel; Leader currently held initially; eligibility does not appoint the pawn |
| Xenotype preference | Preferred, nonpreferred or no opinion under pawn's, player's or selected ideology; Pawn's ideology initially, preference mode explicitly chosen; requires Ideology and Biotech; No opinion means no preferred default or custom xenotypes |
| Venerated animal | Species venerated by owner's faction-primary, player-primary or selected ideology; Owner's faction primary initially; absent owner ideology does not manufacture veneration |
| Weapon class | Ground weapon or pawn's primary weapon belongs to selected class, initially Ultratech |
| Weapon disposition | Ground weapon under player's primary ideology, or primary weapon under its wielder's ideology, has selected disposition; Noble initially |
| Gauranlen | Connection, exact production mode, current/desired strength, current/maximum dryads or time until next dryad; Connection initially; mode Gaumaker, strength 50–100%, dryads 1–2, time 0–1 day when those modes are selected |

All preference modes, role eligibility/held modes and current/desired tree choices survive copy and
reload. No criterion converts a pawn, appoints a role, changes tree mode or applies a style.

#### Scenario: Distinguish absent and uncategorized style
- **WHEN** one object has no style and another has a style without a category
- **THEN** Style none matches the first and Style Other matches the second.

#### Scenario: Preserve xenotype preference mode
- **WHEN** a query asks for a nonpreferred xenotype under a selected ideology and is saved/reloaded
- **THEN** it still asks for nonpreferred under that ideology rather than silently selecting Preferred.

### Requirement: Query mechanitor relationships and resources
Biotech SHALL offer total, used or available mechanitor bandwidth, initially Total and unrestricted;
mechanoid weight class, requiring an explicit choice; mechanoid bandwidth cost, unrestricted;
current mech work mode, initially Work; and uncontrolled player mechanoids with an inclusive range
of time until their feral check, unrestricted initially and displayed in hours. Uncontrolled does
not imply already feral. A non-mechanoid does not match merely because a generic stat has a value.

Mech/overseer relationship offers an overseer matching a nested query or a mechanitor's Any/All
controlled mechs matching it, initially Any controlled mech. All requires a nonempty controlled set,
consistent with gear and social relationships. Uncontrolled or unavailable relationships do not
invent an overseer. Shortcuts add visible editable clauses for Mechlink implant, Work mechanoid
type category and Mech energy need; they are not alternate eligibility or resource rules.

#### Scenario: No controlled mechs
- **WHEN** a mechanitor controls no mechs
- **THEN** All controlled mechs match is false, with no fabricated matching mech or bandwidth consumption.

#### Scenario: Follow a changed overseer
- **WHEN** a mech's actual overseer changes and the query refreshes
- **THEN** the nested relationship uses the current overseer and no stale relationship survives in the result.

### Requirement: Query xenotypes and known gene collections explicitly
Xenotype SHALL offer an exact standard xenotype (Baseliner initially), common unnamed, unique,
hybrid or an exact custom xenotype. Custom names are usable for relevant pawns, gene assemblers
and xenogerms; ambiguous same-name choices must be distinguished rather than silently substituted.

Gene SHALL offer exact gene, category, any or none, combined with either origin, endogene or
xenogene, and Has, Active or Inactive; initially Inbred, either origin, Has. A No xenogenes shortcut
adds the visible None/xenogene/Has clause; it is not a search for every xenogerm item. For pawns,
origin filters the actual gene list and Active means not overridden in that gene set. None/Has
accepts an empty eligible list; None/Active and None/Inactive require at least one gene of the
chosen origin and no genes satisfying the requested activity. Embryos supply endogenes only.
Genepacks, xenogerms, known gene containers and an assembler's accessible packs supply xenogenes
only; unpowered but connected banks still contribute when their contents are ordinarily known.

For these stored gene sets, Active/Inactive refers to overrides within that known set, not readiness
for implantation. A container without any eligible gene-holding contents does not match None as if
it contained a known empty set. With eligible holders, None means none of their eligible genes
match the selected test. All matches use current disclosed contents; unknown caskets/banks are not
opened by the query. Unsupported provider-specific gene meaning makes that clause unavailable.

#### Scenario: Separate endogenes from stored xenogenes
- **WHEN** an embryo and a known genepack each contain the selected gene
- **THEN** Endogene can match the embryo and Xenogene can match the genepack; switching origin does not treat the two collections as interchangeable.

#### Scenario: Distinguish an empty container from no xenogenes
- **WHEN** a pawn has no xenogenes and a container has no gene-holding contents
- **THEN** No xenogenes can match the pawn but not that empty container.

### Requirement: Query Odyssey objects without changing gravship state
Odyssey SHALL add Unique weapon, Gravship fuel, Gravship supported area and Gravship cooldown.
Unique weapon identifies a weapon with the game's unique-weapon feature without an extra quality
threshold. Fuel is the pilot console's connected engine fuel as a proportion of capacity. Supported
area is connected substructure count as a proportion of support capacity, not cargo capacity.
Their ranges initially are unrestricted. Cooldown matches while launch cooldown remains in the
future. Missing required engine/support information or a nonpositive denominator is unavailable
measurement for that otherwise applicable console, never zero or a division error. Observing these
values does not fuel, launch, recharge or reshape the gravship.

#### Scenario: Inspect a disconnected console
- **WHEN** a fuel query encounters a console without its required engine/capacity information
- **THEN** the query explains that unavailable observation instead of reporting the ship as empty or full.

### Requirement: Observe supported external visibility and colony groups narrowly
When a supported CAI visibility provider is active, an explicit CAI-visible criterion SHALL mean
its fog feature is enabled and the held cell is disclosed by that provider. With provider fog
disabled this explicit criterion does not match; ordinary search can still use ordinary known
visibility. When provider fog is enabled but its state cannot be determined, dependent ordinary
queries are unavailable rather than treating a missing visibility grid as visible. Enabling a
debug view never broadens ordinary search or production counts.

A supported LTO colony-group criterion SHALL match membership in an exact selected pawn group,
or None for a pawn in no such groups. None requires reliable membership information, not merely
a failed lookup. Group identity survives renaming; missing or ambiguous bindings require repair.
These observations do not replace either provider, change group membership or promise support for
uninspected versions. Real Fog of War's dormant source filter has no working adapter in this scope;
queries containing it remain unavailable until explicitly converted or supported.

#### Scenario: Lose external visibility data
- **WHEN** provider fog is enabled but the current map's visibility data cannot be read
- **THEN** ordinary dependent searches expose no hidden results and explain the unavailable observation; unrelated queries with a proven independent knowledge path remain usable.

#### Scenario: Rename a colony group
- **WHEN** the bound group is renamed
- **THEN** the exact-group query follows that same group and does not choose another group with the old name.
