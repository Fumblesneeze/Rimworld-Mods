# Scattered chunks and rocky debris

Owner **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`; target **1.7 beta**.
Origin replacement **S112/P2-41**. Draft only. Source-derived values and proposed corrections are
explicit choices for later implementation, not newly confirmed user preferences or accepted visuals.

## ADDED Requirements

### Requirement: SS01 Ground chunks have stable visual variation
Supported stone chunks lying on the ground SHALL have varied rotation, scale and horizontal offset
while remaining recognizable as their actual stone type. At default settings, scale ranges from
82% to 128% of ordinary size, rotation covers a full turn, and each horizontal offset ranges from
-0.27 to +0.27 cells independently. Different chunks can have different appearances even when
occupying the same cell. A given surviving chunk's variation SHALL remain stable through redraw,
camera/zoom changes, hauling and save reload at unchanged settings. Settings adjust the extent of
that variation, not repeatedly randomize it while viewed.

Use the current supported chunk texture and tint. This presentation SHALL NOT change the physical
cell, stack count/limit, mass, path cost, selection target, hit points, hauling eligibility, recipe
ingredients, stonecutting output or any other material property. Steel slag and finished blocks
are not stone chunks for this feature. Ordinary compatible modded stone chunks participate; a
custom presentation with no supported integration retains its own rendering under SS08.

#### Scenario: Natural cluster
- **WHEN** several supported stone chunks lie on the ground at default settings
- **THEN** they display stable varied scale, full-turn rotation and independently varied offsets within the stated bounds
- **AND** the player can still select and haul the real chunks from their actual cells

#### Scenario: Redraw and reload
- **WHEN** the same surviving chunk is viewed at another zoom, redrawn or saved and reloaded without changing settings
- **THEN** its selected variation does not visibly reroll

#### Scenario: Haul a scattered chunk
- **WHEN** a pawn hauls the chunk to another ground cell
- **THEN** its variation moves relative to that new cell without changing the physical job, count or destination

#### Scenario: Texture replacement and unchanged yield
- **WHEN** a supported texture replacement changes a stone chunk and a pawn later cuts that chunk into blocks
- **THEN** scattering uses the current appearance and ordinary stonecutting consumes and produces the normal quantities

### Requirement: SS02 Storage keeps its own item layout
Ground scattering SHALL yield to an actual storage building's item placement and to an admitted
storage provider's layout. It SHALL NOT force offset, scale or rotation that conflicts with that
layout. Ordinary ground stockpile cells still use ground scattering. Leaving building storage
restores the same chunk's ground variation; temporary centering in storage SHALL NOT permanently
erase its offset. Carried items, inventory icons and selection information retain their native
presentation unless their existing owner explicitly supports this feature.

#### Scenario: Stockpile versus shelf
- **WHEN** one chunk is stored in a ground stockpile and another in a shelf or storage building
- **THEN** the stockpile chunk retains ground scattering and the building's layout governs its stored chunk

#### Scenario: Haul out of storage
- **WHEN** a previously scattered chunk is placed in a storage building and later hauled back onto the ground
- **THEN** its ground variation returns without requiring a reload or settings change

#### Scenario: Custom storage without an adapter
- **WHEN** a provider owns a storage display that has no admitted scattering integration
- **THEN** that display remains unchanged rather than drawing a second displaced or oversized copy

### Requirement: SS03 Initial debris represents existing natural rock
On the feature's first admitted initialization of a map, it SHALL add rocky debris beneath eligible
stone chunks outside any storage and beneath visible exposed natural-rock cliff cells. Ore/resource
rocks, map-edge cliff cells, fully buried cliff cells and cliffs hidden by unexplored fog do not
qualify. A cliff is exposed when its cell has at least one in-bounds adjacent cardinal cell from
which ordinary cleaning can touch it; one blocked side or nearby water alone does not exclude it.
Current terrain and the Allow debris on water choice still govern placement.

There SHALL be at most one owned debris patch of one thickness per cell, even with multiple rocks.
New debris matches the depositing chunk/cliff's stone tint and appears as small low debris around
its base, preserving the legibility of chunks, cliffs, nearby items and fog. When initial rocks
share a cell, use one stable contributing rock for color. A later genuine deposit can update that
patch's tint and age; incidental redraw does neither. Ordinary ground debris remains after a chunk
is hauled away, until cleaned, aged out or otherwise validly removed.

Initialization applies to a newly generated map and to an existing map first using this feature.
Reloading, changing settings or hauling a chunk onto another cell SHALL NOT reseed the map, restore
already-cleaned debris or turn storage into a recurring debris source. There is no automatic
redecoration of every newly revealed cliff face after the initial pass.

#### Scenario: Existing map first uses the feature
- **WHEN** a map first initializes with loose chunks, stored chunks, visible exposed cliffs and unexplored cliffs
- **THEN** eligible loose chunks and visible exposed cliffs receive debris once, while stored chunks and hidden cliffs do not

#### Scenario: Several rocks in a cell
- **WHEN** more than one eligible chunk occupies one cell
- **THEN** the cell receives one stable-colored patch and one patch's cleaning and room effects, not duplicate layers

#### Scenario: Nearby water versus water under the debris
- **WHEN** a cliff has an accessible dry side and water on another side, or the debris cell itself is water
- **THEN** nearby water alone does not reject the cliff, while the actual debris cell obeys the water setting and ordinary placement rules

#### Scenario: Clean, reload and haul
- **WHEN** initial debris is cleaned, the map is reloaded and a chunk is later hauled through the same area
- **THEN** those actions do not recreate the cleaned initial patch or leave a new trail of debris

#### Scenario: Chunk removed from an existing patch
- **WHEN** a pawn hauls the last chunk off a debris patch
- **THEN** the patch remains available for ordinary cleaning and aging rather than disappearing with the hauled item

### Requirement: SS04 Dirty mining follows actual stone yield
With **Dirty mining** enabled, actual stone-chunk yield SHALL leave one owned debris patch at each
eligible destination where the completed native event places a yielded chunk. This
includes an ordinary rock-destruction yield using the same process, not just a pawn's mining job.
Failed yield, ore-only yield and unrelated preexisting chunks SHALL NOT create new debris. Near-cell
placement uses the actual yielded destination, and water/storage/placement exclusions are consistent
with SS03. The feature SHALL NOT alter drop chance, yield count, forbidden state or ownership.

Successful mining of a cell that already has owned rocky debris SHALL refresh that patch's age
without increasing thickness or resampling its lifetime, even when Dirty mining is off. Dirty
mining off prevents new yield debris; it does not erase existing patches. Repeated notifications
for one event do not create extra patches or repeatedly refresh unrelated debris.

#### Scenario: Yield placed beside the mined cell
- **WHEN** mining yields a stone chunk which the native action places in a neighboring eligible cell
- **THEN** the new debris appears at that actual destination without changing the yielded chunk or count

#### Scenario: No new stone yield
- **WHEN** a yield fails or produces only ore while another chunk already lies nearby
- **THEN** that old chunk is not evidence of a new deposit and no new debris is created by the event

#### Scenario: Dirty mining disabled
- **WHEN** a rock cell with existing debris is successfully mined while Dirty mining is off
- **THEN** existing debris's age restarts with its stored lifetime, but no new yield debris is added

### Requirement: SS05 Rocky debris is real cleanable filth
Each owned patch SHALL have one thickness, **240 base cleaning work**, **-10 beauty**, **0 outdoor
beauty** and **-2 cleanliness**. Ordinary cleaning speed and terrain cleaning-time effects apply.
It SHALL NOT attach to pawns, provide a haulable/craftable resource, alter movement cost or wash away
through ordinary rain thinning. Chunks and cliffs keep their own separate properties.

At creation, the base natural lifetime is sampled from **60–90 in-game days**. Enabled S105
terrain-sensitive filth lifetime handling treats this as non-liquid debris and applies its Other
filth lifespan adjustment once. Age, thickness, tint and the assigned lifetime survive save/load
and supported native transport. A later settings change does not resample or rescale an existing
patch's lifetime. Genuine thickening/deposit refresh restarts age while retaining that lifetime.

By default, natural aging can remove debris even while rock covers the cell. With **Keep debris
while covered by rock** enabled, reaching natural expiry while a qualifying stone chunk or natural
non-resource cliff remains on that cell restarts the age using the same assigned lifetime. Without
such support, ordinary expiry proceeds. Removing support does not instantly remove debris or
invent another lifetime. This option protects only natural aging: cleaning, terrain removal,
construction and other legitimate removal SHALL still succeed. It SHALL NOT revive cleaned filth.

#### Scenario: Cleaning cost and room effect
- **WHEN** one debris patch is present inside a room and a pawn cleans it
- **THEN** it contributes -10 beauty and -2 cleanliness until removed, and requires 240 base work adjusted by ordinary cleaning conditions
- **AND** it produces no stone item or extra layer when cleaning completes

#### Scenario: Default aging and rain
- **WHEN** a default patch remains under a chunk through rain and beyond its assigned natural lifetime
- **THEN** rain does not thin it, but ordinary natural aging can remove it

#### Scenario: Covered persistence and later removal of support
- **WHEN** covered persistence is enabled and a supported patch reaches natural expiry, then its supporting chunk is hauled away
- **THEN** the supported expiry restarts its age with the existing lifetime, and after support is gone it can expire normally without an immediate forced removal

#### Scenario: Cleaning an expired supported patch
- **WHEN** a pawn cleans a supported patch whose age is already due for natural expiry and covered persistence is enabled
- **THEN** cleaning still removes it rather than restarting its age or trapping the pawn in repeated cleaning

#### Scenario: Terrain lifetime composition
- **WHEN** an 80-day base patch is created with Other filth lifespan set to 50%, then saved, reloaded and refreshed by mining
- **THEN** it keeps a 40-day lifetime through those later events, with no second scaling or fresh lifetime draw

#### Scenario: Terrain or construction removes debris
- **WHEN** native terrain/flood behavior or construction legitimately removes a patch
- **THEN** covered persistence does not veto that removal, regardless of its current age or nearby rock

### Requirement: SS06 Construction and rendering do not create stuck work
Supported rock placement SHALL preserve owned debris that belongs beneath that rock while keeping
other ordinary replacement rules intact. When construction fully buries owned debris beneath an
obstacle with no adjacent cleaning access, the now-unusable patch SHALL be discarded locally,
without removing unrelated filth or useful nearby patches. Mere pawn restrictions, a temporarily
reserved approach or a distant lack of access do not count as fully buried geometry.

Invalid, missing or changed rocks/maps SHALL stop the affected local update safely. Creation,
cleaning and drawing SHALL NOT leave repeated failing jobs, multiply patches, reveal unexplored
rock, change unrelated gameplay outcomes or require a full map redraw every frame.

#### Scenario: Rock placed over existing debris
- **WHEN** a supported rock is placed over owned debris
- **THEN** the debris can remain beneath it and unrelated items/filth keep their ordinary replacement behavior

#### Scenario: Construction seals the final approach
- **WHEN** a completed building removes the last adjacent cleaning access to owned debris beneath an obstacle
- **THEN** that now-buried owned patch is removed without deleting unrelated filth or nearby reachable debris

#### Scenario: Temporarily unavailable cleaner
- **WHEN** a patch's geometry remains accessible but pawns are restricted or its approach is reserved
- **THEN** the feature does not erase the patch merely to bypass those ordinary work constraints

### Requirement: SS07 Settings have truthful scope and timing
All six configuration choices SHALL use the common Environment and cleaning settings, with an
ordered Scattered stones section separating appearance from debris behavior:

| Setting | Built-in default | Range / effect |
| --- | --- | --- |
| Minimum size modifier | 100% | 50–200%, in 1% steps; multiplies the ordinary 82% minimum |
| Maximum size modifier | 100% | 50–200%, in 1% steps; multiplies the ordinary 128% maximum; the effective upper bound is never below the effective lower bound |
| Offset modifier | 100% | 0–300%, in 1% steps; multiplies each 0.27-cell offset bound; zero centers ground chunks |
| Allow debris on water | On | Affects future deposits; ordinary terrain support/removal rules still apply |
| Keep debris while covered by rock | Off | Affects future natural-expiry decisions, never blocks cleaning or other removal |
| Dirty mining | On | Affects future yield deposits; does not delete existing debris |

Each row SHALL show Global default, Current save and Use global. New/missing save choices follow
the current global value. An override affects only that save. Checking Use global retains a dormant
override; unchecking restores it or seeds a first override from the current global value. Save
editors are inactive while inheriting; all save controls and callbacks are inactive at the main
menu, with global controls usable. Invalid edits are rejected; invalid stored values use that row's
built-in default with a bounded diagnostic, preserving unrelated valid entries.

Appearance changes SHALL update already visible supported chunks on the active game's maps without
needing an unrelated construction action or reload. They do not reseed debris or change gameplay
properties. Settings and help in all seven baseline languages SHALL disclose the scope above,
including the lifetime option's narrower meaning. No bottom tab or separate operational toggle is added.

#### Scenario: Crossed size modifiers
- **WHEN** minimum is 200% and maximum is 50%
- **THEN** the effective minimum and maximum both become 164% of ordinary size, without an inverted range or invalid graphics

#### Scenario: Zero offset and live refresh
- **WHEN** the player sets Offset modifier to zero during play
- **THEN** current supported ground chunks become centered without a reload, while their physical cells and quantities remain unchanged

#### Scenario: Global and save values
- **WHEN** the global value changes while one save inherits and another has an override
- **THEN** only the inherited effective value follows the change, and switching saves or Use global preserves the correct dormant choices

#### Scenario: Main-menu controls and invalid values
- **WHEN** settings are opened at the main menu or one stored entry is invalid
- **THEN** no save can be edited implicitly and the invalid entry alone uses its built-in default without breaking the settings window

#### Scenario: Creation choices do not rewrite existing debris
- **WHEN** water deposits or Dirty mining are disabled after debris already exists
- **THEN** new qualifying events follow the new choice while existing patches retain their saved state and normal removal paths

### Requirement: SS08 Compatibility and saved state remain explicit
Required content and behavior SHALL be admitted before the corresponding owned change applies.
A failed or unknown prerequisite leaves the affected change inactive, with one bounded startup
warning identifying it, the reason and fallback. Compatible unrelated modifications alone do not
disqualify it. Unadmitted chunk rendering preserves the prior graphic; unsafe debris creation is
disabled rather than creating uncleanable or partly configured content. Existing owned debris
remains cleanable and follows ordinary aging when enhanced persistence is unavailable.

An active original Scattered Stones provider SHALL own the overlap: owned scattering, new deposits
and enhanced persistence remain inactive with a warning, without editing the provider's settings,
graphics or saved objects. Missing optional providers are quiet. A failed requested adapter preserves
the provider's ordinary behavior and explains the missing enhancement. Reinitialization, map/save
switches and later incompatibility SHALL NOT duplicate effects or reuse another game's presentation.

Owned tint, lifetime and initialization progress SHALL persist accurately without saving a transient
chunk-rendering cache. Legacy source debris and its map state SHALL NOT be presented as automatically
converted unless a separately specified, verified migration exists. No source identity is spoofed.
Disabling future owned generation SHALL leave already-created owned debris cleanable; this is not
a promise that removing the whole megamod from a save containing its content is lossless.

#### Scenario: Unsupported graphic with supported physical chunk
- **WHEN** a stone type's custom graphic lacks a supported integration
- **THEN** its prior rendering and physical behavior remain, and the skipped owned enhancement is identified without replacing that foreign graphic

#### Scenario: Partial debris capability is unavailable
- **WHEN** required debris creation/cleanup prerequisites fail or optional enhanced persistence cannot be admitted
- **THEN** unsafe new creation stays inactive, while existing owned debris can still be cleaned and uses ordinary aging when persistence is unavailable

#### Scenario: Original provider active
- **WHEN** a source Scattered Stones version is active under its shared package ID
- **THEN** owned overlap is inactive with a warning and the source's code, settings and objects are left untouched

#### Scenario: Optional absence or compatible foreign change
- **WHEN** an optional provider is absent or a benign other patch leaves required semantics intact
- **THEN** absence is quiet and the benign patch alone does not disable supported owned behavior

#### Scenario: Reload, repeated initialization and later incompatibility
- **WHEN** a map is reloaded, initialization repeats or an admitted effective prerequisite later becomes false
- **THEN** no extra initial debris pass, doubled transformation or stale cross-save rendering occurs, and only the affected owned enhancement stops
