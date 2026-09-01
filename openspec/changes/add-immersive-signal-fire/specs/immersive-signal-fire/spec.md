## ADDED Requirements

**Owning mod:** Immersive Signal Fire (`fumblesneeze.immersivesignalfire`) at `mods/ImmersiveSignalFire`.

### Requirement: Cold Neolithic signal-fire construction
The mod SHALL add a player-buildable 2×2 signal-fire structure gated by an explicit Neolithic smoke-signalling research project costing 500 research points. The structure SHALL cost 60 WoodLog, require 400 construction work, have 120 base hit points, use an original custom raster asset, and contain no permanent light, heat, fuel, cooking, or fire-overlay component. It SHALL not be Stuff-made or require stone. Its sprite SHALL depict a neat prepared single-use stack with no stones, broad simple forms, a restrained low-gradient palette, and more predominantly light/green fresh wood mixed with fewer brown seasoned pieces. A newly constructed or spawned structure SHALL be visibly unlit.

#### Scenario: Research gate hides the structure
- **WHEN** the smoke-signalling project is incomplete
- **THEN** the signal fire cannot be designated for construction through the native architect path

#### Scenario: Completed research permits a cold structure
- **WHEN** the player completes the Neolithic smoke-signalling project and constructs the signal fire through the native architect and pawn construction workflow
- **THEN** the finished 2×2 structure uses the custom sprite and emits no flame, glow, room heat, or cooking function while idle

### Requirement: Nearby allied pre-industrial contact catalog
For the current colony map, the contact catalog SHALL include a faction exactly once only when it is non-player, non-defeated, allied to the player, has `TechLevel` lower than `Industrial`, and owns at least one spawned world settlement whose approximate world-grid distance from the colony tile is at most 10 tiles inclusive. Results SHALL be ordered by nearest qualifying settlement distance and then localized faction name. Neolithic and Medieval factions qualify; Industrial or higher factions do not.

#### Scenario: Nearby allied tribal and medieval settlements qualify
- **WHEN** allied Neolithic and Medieval factions own settlements 10 or fewer world tiles from the colony
- **THEN** both factions appear once in nearest-first contact order

#### Scenario: Relationship, technology, and distance exclude contacts
- **WHEN** candidate settlements are hostile or neutral, owned by an Industrial-or-higher faction, defeated, or farther than 10 world tiles away
- **THEN** none of those factions appears in the contact catalog

#### Scenario: One faction owns multiple nearby settlements
- **WHEN** an eligible allied faction owns multiple settlements within 10 world tiles
- **THEN** the faction appears once and its ordering distance is its nearest qualifying settlement

### Requirement: Pawn and no-pawn native map contact menus
Right-clicking the signal fire on the map SHALL expose the same top-level contact action with either one selected valid pawn or no selected pawn. Activating it SHALL open a native submenu containing one enabled entry per currently eligible faction. The faction catalog SHALL be revalidated when an entry is chosen. When no faction qualifies, the submenu SHALL contain one disabled entry with exact visible text `no allied neolithic or medieval settlements nearby`.

#### Scenario: Selected pawn opens a faction submenu
- **WHEN** one valid player colonist right-clicks the signal fire and chooses its contact action
- **THEN** the native submenu lists the eligible factions and choosing one opens signal participant setup with that pawn locked as caller

#### Scenario: No selected pawn opens the same submenu
- **WHEN** the player right-clicks the signal fire with no pawn selected and chooses its contact action
- **THEN** the same eligible faction submenu opens and choosing one proceeds to participant setup without requiring a preselected pawn

#### Scenario: No eligible settlements are nearby
- **WHEN** the player opens the contact submenu and no faction satisfies every catalog rule
- **THEN** the submenu shows the exact disabled entry `no allied neolithic or medieval settlements nearby` and cannot start a signal

#### Scenario: Contact becomes stale before selection
- **WHEN** a displayed faction loses alliance, is defeated, or loses its nearby settlement before its submenu entry is chosen
- **THEN** the mod refuses to start, reports that the contact is no longer available, and performs no job or faction mutation

### Requirement: Shared begin-lord-job participant presentation
Choosing an eligible faction SHALL open a Core `Window` that composes RimWorld's ritual/bestowal/childbirth two-column participant, duration, quality-factor, and outcome presentation without inheriting RimWorld 1.6's DLC-guarded `Dialog_BeginLordJob` constructor. It SHALL require exactly one caller and permit zero, one, or two helpers, for a maximum of three participants. Eligible participants SHALL be spawned, living, non-downed, adult, free player colonists on the current map who can reach and reserve a distinct interaction cell at the signal fire. A pawn used to invoke the menu SHALL remain the locked caller; without an invoking pawn, the best eligible signaler SHALL be selected initially and the player SHALL be able to choose another caller. Up to two highest-scoring eligible helpers SHALL be initially selected in either route and the player SHALL be able to remove or replace them.

#### Scenario: Caller signals alone
- **WHEN** the dialog has one eligible caller and no helper selected
- **THEN** it can start and displays a 600-tick expected duration plus the caller's Melee and Social quality contribution

#### Scenario: Two helpers join
- **WHEN** the player selects two additional eligible colonists
- **THEN** all three appear as participants and the displayed quality is recalculated from all three

#### Scenario: A fourth pawn is selected
- **WHEN** three participants are already selected and the player attempts to add another helper
- **THEN** the dialog keeps the three-participant maximum and explains that only two helpers may join

#### Scenario: Start preconditions change
- **WHEN** a selected participant becomes downed, despawned, reserved, or unable to reach a distinct interaction cell before the player accepts
- **THEN** the dialog refuses to start and identifies the blocking participant or path condition

### Requirement: Melee and Social signal quality
The displayed and resolved signal quality SHALL use RimWorld Social as charisma. Each selected participant's individual score SHALL be `(Melee level + Social level) / 40`, with a missing or disabled skill contributing level zero; group quality SHALL be the arithmetic mean of the selected individual scores captured on dialog acceptance.

Outcome bands SHALL be deterministic and use the requested strict thresholds: quality greater than 50% is immediate success; quality greater than 30% and at most 50% is delayed success; quality at least 15% and at most 30% is noticed but misunderstood and yields no aid; quality below 15% is not noticed and yields no aid.

#### Scenario: High-quality signal succeeds immediately
- **WHEN** captured group quality is greater than 50%
- **THEN** completing the performance requests warriors immediately

#### Scenario: Medium-quality signal is delayed
- **WHEN** captured group quality is greater than 30% and at most 50%
- **THEN** completing the performance schedules the warrior request for a saved random delay

#### Scenario: Low-quality signal is misunderstood
- **WHEN** captured group quality is at least 15% and at most 30%
- **THEN** the smoke is reported as noticed but misunderstood and no warrior request occurs

#### Scenario: Very poor signal is unnoticed
- **WHEN** captured group quality is below 15%
- **THEN** the player is told the signal failed to be noticed and no warrior request occurs

### Requirement: Synchronized 600-tick pawn performance
Starting a signal SHALL issue ordinary pawn jobs under one coordinator-owned structure session. Each pawn SHALL reserve a distinct reachable adjacent interaction cell, walk into position, and synchronize the active phase to one shared game tick without competing for one exclusive building reservation. The active phase SHALL last exactly 600 game ticks, equivalent to about 10 real seconds at normal/lowest speed. Participants SHALL face and work at the signal fire until the shared end tick.

#### Scenario: Participants arrive at different times
- **WHEN** selected pawns path to the fire with different travel times
- **THEN** early pawns wait and the 600-tick active phase begins only after every participant is in position

#### Scenario: Performance completes normally
- **WHEN** all selected pawns remain valid through the shared end tick
- **THEN** each completes the synchronized native job and exactly one terminal signal outcome is resolved

#### Scenario: Participant is interrupted
- **WHEN** a participant is drafted, downed, killed, despawned, or loses the job after active smoke begins
- **THEN** the shared signal fails once, all temporary effects stop, no aid is requested, and terminal cleanup still occurs

### Requirement: Dynamic Effects Forge flame and grouped original dark-smoke puffs
Dynamic Effects Forge (`blues.forge`) SHALL be a hard runtime dependency and SHALL render both the temporary ritual flame and product-owned dark smoke. The 600-tick signal SHALL launch two Morse-like groups of three puffs at exact ticks `0`, `60`, `120`, `360`, `420`, and `480`. At each start, the effect SHALL start exactly one 21-tick emission using the complete original smoke configuration: the compact grayscale particle, `spawnPerTick 0.72`, scale `0.55~1.15`, opacity endpoints `0.88` and `0.82`, movement speed `0.25~0.75`, and travel range `1.7~2.6`. A spacing sketch SHALL guide only the launch cadence and SHALL NOT replace, restyle, or intensify that particle. Every blanket SHALL remain raised for exactly the same inclusive 21 ticks as the owned emitter. The product SHALL explicitly kill its one attached smoke runner at the exclusive 21-tick boundary and on terminal cleanup so Forge cannot reset and loop the emitter. Loading a save during an active puff SHALL reconstruct its transient runner against the original scheduled start and SHALL NOT extend it beyond the original exclusive boundary. It SHALL start no additional emitter before the next scheduled tick and no emitter while blankets are lowered. The steady flame SHALL remain Forge-driven through a product-owned flat white-alpha flame fleck and SHALL exclude stock grayscale fire flecks whose dark lobes read as source smoke during lowered phases. Previously emitted particles MAY continue rising and fading naturally. Each 39-tick inter-puff interval SHALL separate the rising original particles into layered puffs, while the 219-tick break between groups SHALL move the preceding plume visibly clear of the source with blankets lowered. Simple FX: Smoke SHALL NOT be required.

#### Scenario: Active signal shows fire and Morse smoke
- **WHEN** the synchronized phase begins with Dynamic Effects Forge loaded
- **THEN** temporary flame appears, each synchronized 21-tick blanket lift starts one unchanged original dark-smoke puff, rising earlier puffs form a spaced layered plume, every blanket lowers when that emitter stops, the longer group break visibly clears the source, lowered phases start no smoke, and the structure remains cold outside the active performance

#### Scenario: Signal finishes or aborts
- **WHEN** the performance reaches any terminal outcome or interruption
- **THEN** every owned Forge runner is killed, no product smoke or flame continues indefinitely, and no vanilla permanent fire is left behind

#### Scenario: Required Forge dependency is absent
- **WHEN** the player attempts to load the product without `blues.forge`
- **THEN** RimWorld's mod dependency UI reports the missing hard dependency rather than loading a visually incomplete signal system

### Requirement: Optional synchronized smoke blanket
When either the original Show Me Your Tools or its maintained fork is active under canonical package ID `meathax.showmeyourtools` and the expected `JobEffects.JobToolDef` marker is present in that matched package's own assembly list, every participant SHALL display a product-owned blanket-like tool during the active phase. A same-named type from any other loaded assembly SHALL NOT enable the adapter. All blankets SHALL use the one saved shared emitter phase: raised for the same inclusive 21 ticks as each plume runner and lowered after the runner stops. The core signal SHALL remain fully playable when the optional mod is absent. A changed optional shape SHALL disable only blanket rendering and emit at most one bounded diagnostic per game load.

#### Scenario: Supported Show Me Your Tools is active
- **WHEN** the optional package and expected marker shape are loaded and three colonists perform a signal
- **THEN** all three show the blanket tool and raise/lower it in the same ticks as each other and the smoke cadence

#### Scenario: Optional package is absent
- **WHEN** no Show Me Your Tools variant is active
- **THEN** the full contact, dialog, job, smoke, outcome, and cleanup workflow remains available without blanket graphics or optional-assembly errors

#### Scenario: Optional runtime shape changed
- **WHEN** the canonical optional package is active but its expected marker type is missing or incompatible
- **THEN** blanket rendering stays disabled, the signal remains playable, and one concise compatibility diagnostic is recorded

### Requirement: Native allied military aid semantics
An immediate or due delayed success SHALL invoke exactly once a narrow native-equivalent projection of RimWorld's `FactionDialogMaker.CallForAid(Map, Faction)` mutation for the selected faction without opening the comms-console negotiation window. The projection SHALL preserve the native base request of -25 goodwill as processed by Core's `CalculateAdjustedGoodwillChange` natural-goodwill adjustment, the 60,000-tick military-aid availability gate and cooldown stamp, quick military-aid arrival mode, native point range, and `RaidFriendly` incident worker while retaining the worker's Boolean result. Immediately before invocation, the faction SHALL still exist, be non-defeated, remain allied, and be out of military-aid cooldown; otherwise no mutation occurs and the player receives a no-response or wait message. The comms option's sub-industrial refusal SHALL NOT apply because it would contradict this feature's required Neolithic/Medieval contacts.

#### Scenario: Immediate success sends warriors
- **WHEN** a greater-than-50% signal completes for a still-valid allied faction
- **THEN** the native aid mutation runs once, goodwill/cooldown change through RimWorld's code, and friendly warriors arrive through the native incident path without a comms dialog

#### Scenario: Delayed success survives save and load
- **WHEN** a greater-than-30%-and-at-most-50% signal samples a delay between 600 and 3,600 ticks and the game is saved and loaded before it expires
- **THEN** the same faction and due tick are retained, no delay is resampled, and the native aid mutation runs once when that tick is reached

#### Scenario: Faction ceases to be an ally while delayed
- **WHEN** a delayed signal becomes due after the selected faction is defeated or no longer allied
- **THEN** no goodwill, cooldown, or incident mutation occurs and the player receives a no-response message

#### Scenario: Military-aid cooldown has not elapsed
- **WHEN** an immediate or delayed response becomes due before 60,000 ticks have elapsed since that faction's previous military-aid request
- **THEN** no goodwill, cooldown, or incident mutation occurs and the player receives a wait message

#### Scenario: Native incident cannot produce warriors
- **WHEN** the native friendly-raid worker cannot execute after the native aid mutation begins
- **THEN** the mod does not synthesize pawns or roll back partial native faction mutations and reports that no warriors could answer

### Requirement: Single-use destruction and substantial soot cleanup
Every completed, failed, or interrupted active performance SHALL stop the temporary effects, place exactly 16 thickness units of ordinary cleanable ash on valid walkable cells in or near the occupied rectangle, preferring nearer cells within radius one, and destroy the spent signal-fire building. Outcome, cleanup, and destruction SHALL be idempotent across multiple pawn completion callbacks, external destruction, and save/load. Cancellation while participants are still gathering and before the active flame/smoke phase begins SHALL leave the unused structure in place.

#### Scenario: Signal completes
- **WHEN** any quality band reaches its terminal end
- **THEN** the temporary effects stop, the spent building is gone, and exactly 16 ash thickness units are available for ordinary cleaning work
- **THEN** the exact ash delta is verified after the spent building's ordinary despawn side effects, so dense prior soot cannot make the final cleanable result smaller than 16

#### Scenario: Gathering is cancelled before use
- **WHEN** a participant becomes unavailable before the synchronized active phase begins
- **THEN** the gathering jobs stop without aid or soot and the still-cold prepared signal fire remains available

#### Scenario: Multiple jobs finish on one tick
- **WHEN** two or three participant job drivers report completion during the terminal tick
- **THEN** aid or failure resolves once, the building is destroyed once, and soot is not duplicated

#### Scenario: Player deconstructs an active signal fire
- **WHEN** the player applies the native deconstruction order and an ordinary Construction pawn removes the fire before its 600-tick terminal result
- **THEN** the active signal is interrupted without aid, the building remains destroyed, and `PostDeSpawn` adds exactly one bounded 16-thickness ash cleanup without recursive destruction

### Requirement: Honest packaging, compatibility, and evidence
The released package SHALL contain only allowlisted Immersive Signal Fire files, strict 8-bit RGBA product assets, and the product assembly. It SHALL declare Harmony and Dynamic Effects Forge as hard dependencies, Show Me Your Tools as optional compatibility, and SHALL NOT bundle or reference Workshop assemblies, Simple FX: Smoke, another repository product mod, or RimWorld Dev Gateway. Runtime strings SHALL be localized through keyed or Def-injected language data.

Final acceptance SHALL use a fresh minimized isolated RimWorld process on the reviewed build and retain the exact build/package identity, ordered mod list, native right-click action, participant dialog interaction, before/active/after screenshots, observed warriors or explicit failure result, burnout/soot state, relevant logs, configuration hashes, and exact-process cleanup. Static tests, direct state mutation, and Gateway request success alone SHALL NOT accept gameplay.

#### Scenario: Package is validated
- **WHEN** the Release product package is built and inspected
- **THEN** it contains no dependency assembly, test assembly, Dev Gateway reference, source-only art candidate, or non-allowlisted file

#### Scenario: Reviewed live workflow is accepted
- **WHEN** the player opens the contact submenu, selects a faction and participants, starts the signal, observes the synchronized effects, and waits for the terminal result on the reviewed build
- **THEN** retained evidence causally connects that native action to the observed aid/failure, spent-building destruction, and soot in the exact isolated process
