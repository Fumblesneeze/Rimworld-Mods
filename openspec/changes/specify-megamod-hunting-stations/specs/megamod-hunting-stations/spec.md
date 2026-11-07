# Hunting spots and raised seats

Owner **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`; target **1.7 beta**.
**Origin: original**, S064/P1-58–59. Specification only; nothing implemented or accepted in game.
Waiting at a spot/seat, range-only detection initially about40 cells, leaving to hunt, and the
map-specific grouped species filter are user decisions. The precise choices below are proposed
owned defaults, retained in HS-Q1–5 for review before implementation rather than invented confirmations.

## ADDED Requirements

### Requirement: Place a physical hunting spot or construct a raised hunting seat

HS01. The player SHALL be able to place a Hunting spot or construct a Hunting seat through Architect, each occupying one map cell and supporting one waiting hunter at a time.
The spot is an instant, material-free marker with no research, hit points, comfort or refund value.
It neither blocks movement nor creates a floor, cover, roof, light or protection. The raised seat
requires ordinary building work before use and grants ordinary seating comfort while occupied.
Neither form attracts animals, changes wildlife spawning, extends weapon range, supplies ammunition,
grants skill experience merely for waiting, or gives an elevation-based accuracy/cover bonus.

Proposed seat baseline: **50 ordinary units of Woody material**, **900 construction work**, no
research or minimum Construction skill, **150 HP**, **mass6**, **comfort0.5**, **beauty0**,
**flammability1**, **fill0.2** and **path cost30**, with normal material/quality/stat adjustments,
repair, deterioration, minification, reinstall and salvage rules. Compatible modded Woody material
is valid. These are provisional measured-comparator choices, not finalized balance acceptance.
Normal construction failure, quality targets and material accounting remain effective.

Placement SHALL use in-bounds cells and the applicable terrain, occupied-cell and construction
rules. The instant spot requires a revealed legal cell. The positive-work seat follows S028's
enabled rock/unexplored-cell planning: its blueprint can await exploration without disclosing hidden
contents or support costs. Full-footprint reveal validation and S063's deferred first plant clearing
apply before legal construction; an unfinished plan supplies no waiting position or detection.
Two stations cannot share a waiting cell. A blocked/unreachable finished station remains visible
but unusable with a reason; placing one does not certify its safety or override allowed areas.

#### Scenario: Place a free spot
- **WHEN** the player places a Hunting spot on a legal revealed cell
- **THEN** a configurable marker appears without consuming material or creating cover, initialized with HS03's species defaults; hunting still requires an eligible hunter and ordinary station acquisition.

#### Scenario: Build the seat
- **WHEN** a builder completes a Hunting seat using an eligible Woody material
- **THEN** ordinary material/work/quality rules produce the actual seat and only then can a hunter sit there to wait.

#### Scenario: Plan a seat before exploration
- **WHEN** S028's rock/unexplored-cell planning is enabled and the player plans a seat in an unexplored cell
- **THEN** only the player's plan is shown, with hidden support needs unknown and no station watching or detection.
- **AND** revealing its footprint triggers ordinary validation, permitted prerequisites and S063's deferred first clearing; only a legally completed seat can be used, while an unfixable plan is canceled with actual supplied inputs recovered.

#### Scenario: A seat is inaccessible or destroyed
- **WHEN** the waiting position becomes blocked, forbidden or destroyed
- **THEN** no hunter teleports into it or continues to use its comfort, and the station's invalidation rules apply.

### Requirement: Offer the map's possible wildlife even when it is absent

HS02. Each station's species chooser SHALL list animal species supported by that map's admitted wildlife-generation and wild-arrival rules, including currently absent species.
Use all relevant map biomes, applicable coastal/pollution/habitat rules, migrations and supported
rare wild arrivals such as thrumbos. Seasonal absence, a currently full wildlife population, lack of
a current specimen or an incident's temporary cooldown SHALL NOT alone remove a possible species.
The list SHALL distinguish possible presence from current availability; it is not a spawn promise.
Permanent map/content restrictions still apply. Imported pets, traders' animals or hypothetical
arbitrary quest spawns do not make every loaded animal a possible local wild species by themselves.

Revealed eligible wild animals actually present through another source SHALL also make their species
selectable. Hidden individuals, undiscovered quest contents and animals on another map SHALL NOT
be exposed by that observation. Species with several animal variants are listed once per species;
distinct species remain distinct. DLC and compatible modded species participate under their actual
active rules without becoming mandatory dependencies.
An observed species remains known on that map after its last specimen leaves, labeled as observed
rather than guaranteed to return; removing its required content still makes it unavailable.

Opening, searching or refreshing the chooser SHALL NOT spawn animals, alter incident chances or
advance random gameplay outcomes. A newly discovered/introduced species starts disallowed. Saved
choices for temporarily inapplicable or missing species are retained and labeled unavailable, never
silently reassigned to a similarly named species. Restored species recover their own previous choice.
Unsupported provider rules SHALL have an explicit coverage limitation; the UI must not claim that a
partial catalog exhausts all possible provider-specific arrivals.

#### Scenario: No deer are currently present
- **WHEN** deer are allowed by this map's wildlife rules but none are spawned
- **THEN** deer remain selectable and can be enabled before they arrive.

#### Scenario: Seasons and coast differ
- **WHEN** a species is seasonally absent on a suitable map and another species requires an unavailable coast
- **THEN** the seasonal species remains listed while the inapplicable coastal rule does not populate an inland map's possible-wildlife list.

#### Scenario: Rare arrival and ordinary colony livestock
- **WHEN** a map permits a future thrumbo visit and a farm-animal arrival only grants owned livestock
- **THEN** the thrumbo can be selected while that livestock arrival alone does not authorize hunting colony animals or add every pet species as local wildlife.

#### Scenario: A provider introduces a revealed wild species
- **WHEN** an eligible previously unlisted wild animal becomes revealed on this map
- **THEN** its species becomes selectable but starts disallowed, without exposing hidden animals or another map's population.

#### Scenario: Species disappears across saves or content changes
- **WHEN** a saved selected species is unavailable after load and later becomes available again
- **THEN** its unavailable choice is retained and restored to that same species without selecting a replacement or newly added species.

### Requirement: Configure each station through a grouped contextual animal filter

HS03. Selecting a station SHALL expose a storage-style searchable species filter with individual checkboxes and predator/non-predator and small-game/big-game groupings.
The two classifications combine, so players can choose, for example, small non-predators. Predator
classification follows the game's species behavior. Small/big classification uses the species'
normal adult body-size value, not an individual animal's age, injuries or temporary modifiers.
The proposed common cutoff is **1.0**: below it is small game, equal or above is big game. A grouping
is a selection aid, not a guarantee that an animal is safe, edible, calm or easy to kill. Species
information and ordinary retaliation/death-hazard information SHALL be available from the chooser.

A new station SHALL start enabled with the currently listed map-possible species whose native
retaliation-on-hunting-damage chance is exactly **0%** selected. Unknown chances and positive chances
rounded to0% SHALL NOT qualify. This is an initial choice, not a guarantee against all dangers or
permission for hidden targets. Player edits SHALL remain authoritative through reload and later
catalog/stat changes; newly introduced species remain unselected. An empty resulting selection
takes no worker until configured.
Group/Allow all/Disallow all actions change only the applicable currently listed species; new
species do not inherit blanket authorization later. Searching only narrows what is displayed and
does not silently change existing choices. A species choice applies to all its ages and variants.
Changing the size cutoff regroups the UI without changing explicit species permissions.

The station's enabled state and species choices are saved entity gameplay state, not mod settings
or a pawn allowed area. Each station remains independent. Contextual controls SHALL show its range,
enabled/paused state, current hunter/target when applicable, and reasons such as unconfigured,
unavailable worker, blocked position or unsupported integration. No new bottom main tab is added.

#### Scenario: New station default
- **WHEN** the map-possible list contains species with0%, positive and unknown retaliation chances
- **THEN** only those with exactly0% start selected; the player may change any selection, which survives reload and later chance changes.

#### Scenario: Select small non-predators
- **WHEN** the player enables the small non-predator group
- **THEN** the corresponding listed species become allowed, predators and big game retain their own choices, and subsequently introduced species remain disallowed.

#### Scenario: Baby of a large species
- **WHEN** a juvenile of a species whose adult body size is at least1.0 enters range
- **THEN** it uses that species' big-game permission rather than changing categories because it is young.

#### Scenario: Search and regroup without changing authorization
- **WHEN** the player searches for a species or changes the common size cutoff
- **THEN** the display changes but previously selected species remain selected and no new species is authorized automatically.

#### Scenario: Two independent stations
- **WHEN** one station permits predators and another permits only non-predators
- **THEN** each displays and uses its own choices after save/load without an implicit link or a mod-settings row for those entity choices.

### Requirement: Eligible hunters physically wait under ordinary work priorities

HS04. An undrafted colony worker already eligible for ordinary Hunting work SHALL be able to use an enabled configured station through that work type, without a new work column or forced assignment.
Work/capacity/violence restrictions, usable non-explosive ranged hunting weapon, ranged-blocking
equipment restrictions, permitted access and normal urgent needs remain effective. A station does
not equip a weapon, recruit an animal as the primary hunter or permit an otherwise ineligible worker.
Currently actionable explicit manual Hunt work takes precedence over station waiting within Hunting;
other work types retain the player's priorities. Without a usable station, ordinary hunting remains.

Choose a nearest reachable, legal and unclaimed station by ordinary route distance, with a stable
tie choice. The worker SHALL walk to its actual waiting cell and wait there; traveling toward it
does not already count as watching. A seat is occupied for waiting, while the free spot grants no
seating comfort. No prey is required to be present for waiting, but an empty/unavailable species
selection supplies no waiting job. Waiting can occupy Hunting work time ahead of lower-priority work;
it is not promised to be spare-time recreation.

One station has at most one active hunter assignment, retained through that station's hunt and
ordinary return. Different stations can supply other hunters. Waiting SHALL reconsider ordinary
needs and work at least every **250 game ticks**, and immediately yield to native forced/emergency
interruptions when those take control. It grants no free rest, recreation or hunting experience.

#### Scenario: Hunter arrives before game does
- **WHEN** Hunting is the worker's next permitted work and a configured station has no prey in range
- **THEN** the worker walks to it and waits there rather than roaming the map to find unmarked game.

#### Scenario: Manual hunt or missing weapon
- **WHEN** an actionable manual Hunt order exists, or the worker lacks a valid hunting weapon
- **THEN** the manual order takes normal Hunting precedence in the first case and the station does not grant a hunting job in the second.

#### Scenario: Two hunters and one station
- **WHEN** two workers try to use the same free station
- **THEN** only one takes its assignment, including while that hunter is away hunting, and the other uses other eligible work or another station.

#### Scenario: Need or higher-priority work interrupts watching
- **WHEN** normal needs or higher-priority work require the waiting hunter
- **THEN** ordinary reassessment releases the assignment without trapping the pawn in an endless waiting job or overriding an explicit player command.

### Requirement: Detect by range and choose one presently authorized prey

HS05. While the hunter is physically waiting, the station SHALL consider eligible animals at a straight-line horizontal distance of at most the configured radius from its waiting cell, initially **40 cells**.
Line of sight from the station is not required. The full circle applies regardless of seat facing;
exactly40 cells is inside the default boundary. This range is only the initial detection boundary,
not a weapon range, firing-through-walls permission or limit on an already admitted pursuit.
Revealed prey can be noticed behind a wall; fogged/undiscovered prey cannot be located through this
feature. The circle display SHALL not reveal hidden terrain or targets.

New automatic prey must be a living, spawned, factionless wild animal of an allowed species on the
same map. Exclude wild humans, colony/visitor-owned animals, active taming or other admitted
conflicting protective orders, initially aggressive/manhunting animals, forbidden targets and
targets claimed by another incompatible worker/action. Current worker-specific hunting willingness,
access and a legal ordinary hunting approach/firing position must also permit the target. No direct
line of sight from the seat is imposed as a substitute for those actual hunting checks.

Choose the nearest eligible animal to the station, with a stable tie choice. Only one target is
claimed for that hunt. Revalidate current circumstances before leaving; a lost claim selects another
eligible target or continues waiting. Stable eligible prey remaining in range SHALL be reconsidered
within **60 game ticks** while watching. Failure to find prey or a legal exit must yield bounded
waiting with a reason, not an immediate retry loop. Detection creates no map-wide hunting marks
that authorize unrelated workers to bypass the station workflow.

#### Scenario: Behind a wall but approachable
- **WHEN** allowed revealed prey is within40 cells behind a wall and an ordinary permitted approach exists
- **THEN** the waiting hunter can select it without seeing it from the station, then must use that approach and ordinary firing rules.

#### Scenario: Range boundary and hidden prey
- **WHEN** otherwise eligible prey is exactly40 cells away, another is beyond40 and another is fogged within40
- **THEN** only the first qualifies under the default detection rule; hidden prey is not identified by the UI.

#### Scenario: Tame-marked, owned and aggressive animals
- **WHEN** nearby animals include a colony pet, tame-marked wildlife, an already manhunting animal and an ordinary permitted wild animal
- **THEN** only the ordinary eligible animal is considered for a new station hunt.

#### Scenario: Two stations notice the same prey
- **WHEN** hunters at overlapping stations choose the same unclaimed animal
- **THEN** one acquires the target and the other re-evaluates without duplicate pursuit or finishing authority.

#### Scenario: No exit or firing route
- **WHEN** a hunter in a one-cell enclosed waiting position cannot legally leave to hunt
- **THEN** it waits or yields to other work with a blocked reason instead of attacking through the enclosure, teleporting or continuously restarting a job.

### Requirement: Leave the station and perform ordinary hunting and corpse handling

HS06. On acquiring prey, the hunter SHALL end watching and physically step off the spot or out of the seat before beginning the ordinary hunt; the station is not a firing emplacement.
Hunting retains normal walking speed, legal positions, weapon minimum/maximum range, line of fire,
ammo/provider restrictions, attack timing, accuracy, damage, animal flight/retaliation and ideological
consequences. Prey moving outside the detection circle after acquisition does not alone cancel the
hunt. The ordinary pursuit timeout remains effective; the inspected native baseline is **5000 game
ticks**, not an endlessly refreshed allowance. Further targets require another waiting/acquisition
cycle rather than automatic roaming or chaining away from the station.

Without a separate admitted companion handoff, the hunter SHALL use ordinary hunting completion:
approach and finish safely finishable downed prey with the normal preparation, currently **180 game
ticks**, or continue the ordinary ranged handling for a prey whose death is dangerous in melee.
Do not substitute S008's distinct60-tick Finish off behavior merely because it is also implemented.
Recovery, death, despawn or invalid authorization must prevent a stale finishing blow. A revenge
reaction caused during this admitted hunt follows ordinary active-hunt/self-preservation behavior;
the initial aggressive-animal exclusion is not a rule to erase retaliation or freeze the target.

Actual corpses, ordinary meat/leather/body condition and normal hunting corpse-unforbidding apply.
The hunter SHALL physically haul the corpse when a valid destination and the composed carrying rules
permit; otherwise it remains physically on the map for later handling. No storage vacancy, capacity
or extra inventory is invented, and this feature does not butcher or duplicate the corpse. Owned
batch hauling and storage/refill admission remain authoritative for any composed transfer.

#### Scenario: Prey already within weapon range
- **WHEN** the station selects prey already within the hunter's weapon range
- **THEN** the hunter still leaves the waiting position before hunting rather than shooting indefinitely from the seat.

#### Scenario: Chase crosses the detection circle
- **WHEN** admitted prey runs beyond40 cells before being downed
- **THEN** ordinary pursuit can continue with its existing timeout and permissions, without extending weapon range or acquiring a second target along the way.

#### Scenario: Downed explosive prey
- **WHEN** the prey's ordinary death behavior is dangerous at melee distance
- **THEN** the station does not force a close finishing action and retains the ordinary ranged hunting completion rules.

#### Scenario: Prey recovers during finishing
- **WHEN** the animal becomes mobile or otherwise loses finishing authorization before the blow
- **THEN** the stale finishing action does not kill it; ordinary valid hunting can reconsider its current state.

#### Scenario: Corpse cannot be stored or carried
- **WHEN** no permissible destination or carrying route/capacity exists for the real corpse
- **THEN** it remains available for ordinary later handling without a duplicate, fabricated storage acceptance or invisible carried copy.

### Requirement: Return and retry remain subordinate to ordinary work

HS07. After hunting and any valid corpse handling finish, the worker SHALL reconsider its ordinary needs, orders and work before returning to its original station.
An admitted S065 handoff completes the human's responsibility for that exact prey, permitting this
reassessment before the companion finishes/delivers it. The companion retains its original station's
current permissions; a new hunter watch/target does not erase that delegated job or its cancellation rules.
If watching remains the next permitted activity and that station is still enabled, configured,
reachable and available to the same assignment, the hunter walks back and begins a new watch.
Otherwise it releases the assignment and takes ordinary work. No delayed return may override a
player command, drafting, needs, changed map or an invalid station. Selecting a new station later
uses a fresh ordinary assignment; it is not a permanent pawn ownership binding.

An unsuccessful pursuit SHALL not immediately launch the same failed chase repeatedly. The proposed
retry delay is **1 in-game hour** for that station/prey pair, adjustable in common settings; other
eligible prey can be selected. The delay persists across save/load and does not apply to an explicit
manual Hunt order. Zero delay permits a later normal acquisition check, never an unbounded immediate
retry. A timeout cannot be bypassed by resetting the same active hunt's elapsed time on reload or
by repeatedly changing stations without ending that active attempt.

#### Scenario: Finish and return
- **WHEN** the hunter has completed corpse delivery and still has station hunting as its next permitted work
- **THEN** it physically returns to the original valid station before choosing another automatic prey.

#### Scenario: New player command during the hunt
- **WHEN** the player drafts the hunter or orders another action
- **THEN** no station return queued earlier later pulls it away from that command.

#### Scenario: Unsuccessful pursuit
- **WHEN** a station hunt times out and its prey remains alive
- **THEN** that station/prey pair waits the configured retry delay, can choose other prey, and preserves the delay through reload.

### Requirement: Cancellation and persistence preserve authorization and physical state

HS08. Pausing, destroying, removing or relocating a station SHALL stop its new watching/acquisition and revoke its not-yet-completed automatic attacks/finishing and return intent.
Removing a species from the filter, taming/ownership change, a conflicting protective order or new
forbidden/access restriction or loss of revealed target information SHALL similarly invalidate
affected outstanding authority. Revalidate
before each new attack or finishing action; a projectile already fired retains ordinary consequences.
Unrelated manual orders and other stations remain unchanged. Already killed prey is not resurrected.

If a worker already carries a corpse when station authority ends, the actual load SHALL follow
ordinary lawful delivery or safe physical placement/recovery; canceling the station must not erase
or duplicate it. A minified/reinstalled seat keeps its own filter and enabled state but no active
hunter/prey claim; its destination map supplies the new applicable species catalog. Removing a free
spot removes that station; a separately placed spot starts with new defaults.

Save/load SHALL preserve entity choices and enough active phase/target/timeout/retry information
to resume only still-valid work once. Revalidate worker, station, prey/corpse, map and claims on load.
Missing targets, unloaded maps or rejected integration release stale claims and leave actual animals,
corpses and cargo intact. Switching games SHALL not retain another game's assignments or choices.

#### Scenario: Pause or deselect prey before a shot
- **WHEN** the station is paused or the current prey's species is disallowed before the next attack
- **THEN** no new station-authorized attack or finishing blow occurs, while already fired projectiles keep their normal effect.

#### Scenario: Taming wins before the next attack
- **WHEN** the selected prey becomes owned or receives an applicable protective order
- **THEN** the station's current authority is revoked without removing that order or continuing to attack the protected animal.

#### Scenario: Remove the station during corpse transport
- **WHEN** a hunter is carrying the real corpse and its station is removed
- **THEN** transport completes or recovers through ordinary lawful handling and there is no stale return, deleted cargo or duplicate corpse.

#### Scenario: Reinstall on another map
- **WHEN** a hunting seat is uninstalled and reinstalled on another map
- **THEN** its species choices persist as applicable/unavailable choices, its old claims are gone and no hunter returns to the old coordinates.

#### Scenario: Save while pursuing or carrying
- **WHEN** a game is saved during pursuit or corpse handling and reloaded
- **THEN** only the still-valid current phase resumes, with no repeated kill, duplicate haul, reset active-hunt timeout or leaked assignment from another game.

### Requirement: Tuning uses the common inherited settings model

HS09. Hunting station tuning SHALL appear in an ordered Hunting / Stations section of common mod settings with Detection radius default40 cells, Big-game adult-size cutoff default1.0, and Failed-hunt retry delay default1 in-game hour.
Radius accepts positive whole cells, cutoff accepts a positive finite size, and delay accepts finite
nonnegative hours. Invalid input leaves the previous value effective with an explanation. A radius
larger than the map only includes actual map cells and never another map. Radius changes govern the
next acquisition, cutoff changes only classification, and delay changes govern newly failed attempts;
existing timed attempts/delays are not refreshed by editing settings.

Each row SHALL show Global default, Current save and Use global through the common settings path.
New/missing save entries inherit current globals; save overrides never change global values or another
save. Use global disables the save editor and preserves a dormant override; unchecking restores it
or seeds a first override from the current effective global value. Main-menu save controls and all
their mutation callbacks SHALL be disabled. Station filters/enablement remain contextual entity state.
All controls, species group labels, explanations and status SHALL support the seven baseline languages.

#### Scenario: Two saves use different radii
- **WHEN** one save overrides detection radius and another inherits a changed global radius
- **THEN** each save's next acquisition uses its own effective value, without altering an already admitted pursuit's weapon range or timeout.

#### Scenario: Main menu and dormant override
- **WHEN** settings are opened at the main menu or a loaded save temporarily switches to Use global
- **THEN** main-menu save mutations are disabled and a loaded save's dormant override can later be restored without changing globals.

#### Scenario: Invalid radius or regrouping preference
- **WHEN** a negative radius or non-finite cutoff is entered
- **THEN** the previous valid value remains with an explanation; a valid cutoff change only regroups the filter and does not authorize species.

### Requirement: Compose shared hunting and hauling rules with explained fallback

HS10. Station hunting SHALL share actual prey/corpse claims, physical quantities and current permissions with ordinary hunting and admitted owned companion/hauling features, without duplicate finishing or transport.
S065's bonded-companion behavior and S066's directional wandering remain separately owned feature
changes within megamod; they must compose by direct owned decisions. A companion handoff, when
implemented and admitted, has one current finisher/carrier and prevents a concurrent hunter action
on the same target. It cannot multiply capacity or delete a failed carrier's physical load.
Without that feature, ordinary hunter completion remains. S008 drafted party hunting keeps its own
visibility, range and finishing rules; drafting ends the station assignment rather than combining
two automatic attack loops. Shared candidate information SHALL never grant stale permission.

Compatible installed animal/weapon/map changes SHALL remain usable when their required behavior is
supported; another patch's presence alone is not incompatibility. When required behavior cannot be
established, the affected station integration SHALL remain inactive with a visible reason and a
bounded startup warning naming the skipped patch/target requirement, reason, unavailable behavior
and fallback. Do not unpatch another provider or impersonate its package. Unsupported custom paths
retain ordinary manual work; no cross-map targeting, uncertain automatic attack or replay of held
owned attacks is permitted after failed admission. Newly added XML content/patches must preserve
unrelated entries and explain unavailable required content instead of leaving half-working stations.

#### Scenario: Draft into party hunting
- **WHEN** a station hunter is drafted and enables the separately specified party-hunting mode
- **THEN** the station assignment ends and only the party-hunting contract controls its new automatic attacks.

#### Scenario: Companion claims a downed prey
- **WHEN** an admitted future companion handoff gives one companion the current finishing/retrieval claim
- **THEN** the hunter does not concurrently finish or carry that prey, and interruption leaves one recoverable physical target/load.

#### Scenario: Compatible wildlife versus unsupported provider behavior
- **WHEN** compatible modded species join ordinary map wildlife, or a provider changes a required attack/map behavior incompatibly
- **THEN** compatible species can participate under current filters and the unsupported path is disabled with the specified explanation rather than silently guessing or affecting another map.

#### Scenario: Load held work with failed admission
- **WHEN** a save contains an active station hunt whose required integration is now unavailable
- **THEN** its automatic attack/return authority is not replayed, claims are released safely and existing prey, corpse and carried load remain real and recoverable.
