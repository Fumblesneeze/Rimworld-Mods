# Colony alerts

Owner **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
S084 specification only; no runtime implementation or acceptance.

## ADDED Requirements

### Requirement: Ordinary alerts report current known problems through native controls
Enabled alerts SHALL appear in the ordinary alert readout while their stated condition exists,
subject to normal game alert suppression and initial grace period. Hovering explains the condition
and affected subjects; left click cycles forward and right click backward through valid subjects
using normal map/world navigation. No alert issues a job, changes equipment, assigns beds/masters,
captures, executes, treats or trades on the player's behalf. Navigation is not permission to act.

Reports, counts, names and navigation SHALL agree on the current distinct subjects, including their
map or caravan when needed to distinguish locations. Normal alerts never disclose fogged or otherwise
player-hidden subjects, including through counts, messages, hover text or navigation. Conditions update
on the next ordinary alert refresh; hovering/clicking rechecks current eligibility. A departed, dead,
destroyed, newly hidden or otherwise resolved subject is removed without a stale jump or exception.
Save/load and map switching recompute current conditions without replaying orders or retaining old-save
subjects. These controls add no bottom tab or separate settings window.

Historical activation messages may retain only information and locations already visible when sent;
they must not track a subject's later hidden movement or expose new details. Their navigation uses
the recorded known location without selecting a now-hidden pawn, or becomes unavailable if that
location is no longer valid. This applies to both recent messages and archived message navigation.

Visible enemies and blight use native critical priority/red pulsing. Downed-person/entity alerts use
critical priority with the source's grey pulse. Other ordinary alerts use medium priority. Native
critical activation feedback occurs once on activation, not every refresh; a genuinely resolved and
later renewed problem can activate again. Normal player suppression and alert presentation remain.

#### Scenario: Two maps and a stale target
- **WHEN** an alert has subjects on two maps and one disappears before the player clicks
- **THEN** its explanation identifies the remaining subjects and navigation safely cycles through only valid current targets

#### Scenario: No ordinary hidden-information leak
- **WHEN** a hostile or downed pawn is hidden from the player
- **THEN** current ordinary reports and new activation messages do not reveal it, and older messages disclose no new hidden information

#### Scenario: Enemy hides after an activation message
- **WHEN** a previously visible enemy moves into hiding before the player clicks its recent or archived activation message
- **THEN** the message can locate only its recorded known position or no valid target, never its later hidden position or the hidden pawn itself

#### Scenario: Informational use
- **WHEN** the player hovers or clicks a downed-person, blight or medical alert
- **THEN** it explains or selects the problem without ordering capture, plant cutting or treatment

### Requirement: Enemies and downed outsiders remain distinguishable
The visible-enemies alert SHALL include living spawned pawns hostile to the player that are not
downed and are currently visible, including hostile animals, mechs and humanlikes. It reports the
hostility the game actually establishes, not merely faction membership or a guessed threat level.

Separate downed alerts SHALL cover the following visible living subjects on loaded maps. Subjects
already in any bed, being physically carried or secured in entity containment do not qualify.
An unrelated queued job or a rescue designation alone does not hide a still-grounded subject.

| Alert | Eligible subject |
| --- | --- |
| Entity downed | Anomaly entity, regardless of faction; takes precedence over the humanlike categories |
| Enemy downed | Other humanlike hostile to the player |
| Non-hostile visitor downed | Other non-player-faction humanlike from an allied or neutral faction, excluding prisoners |
| Unaffiliated person downed | Other factionless humanlike not already in the hostile category |

Player-faction people remain covered by ordinary native colony rescue alerts. A subject appears in
only one owned downed category. The non-hostile category's name includes neutral visitors rather than
calling them all allies. Explanations describe available attention, without promising capture/rescue
or execution is legal for every subject. Death, recovery, bed placement, carrying or secure containment
removes the relevant report; an unresolved subject dropped on the ground can return.

#### Scenario: Enemy becomes downed
- **WHEN** a visible hostile humanlike is downed during combat
- **THEN** it leaves the active-enemies report and appears only in its qualifying downed category

#### Scenario: Neutral visitor and factionless hostile
- **WHEN** a neutral-faction visitor and a hostile factionless humanlike are both downed on the ground
- **THEN** the former appears under non-hostile visitors and the latter under enemies, with no duplicate unaffiliated entry

#### Scenario: Rescue is not finished by an order
- **WHEN** a rescue is ordered for a downed outsider but the rescuer has not picked them up
- **THEN** the alert remains until actual carrying, bed placement or another resolving condition, even if the subject has a ready unrelated queued job

#### Scenario: Entity configuration is independent
- **WHEN** enemy downed is disabled but entity downed is enabled with supported Anomaly content
- **THEN** a visible unsecured downed entity still appears in its own alert

### Requirement: Blight and exposed electrical equipment warn without performing work
The blight alert SHALL identify visible spawned blighted plants on all loaded maps, including
plants outside a growing zone. It persists while blight remains, even if cutting is already designated
or owned automatic blight cutting is enabled. Merely ordering work does not resolve the infection.

The exposed-electrical alert SHALL identify player-owned spawned buildings that can short circuit
in rain and whose relevant exposure cell is unroofed. Use the building's actual supported rain-risk
rule; ordinary native buildings use their position cell. Warn about exposure even before rain and
while temporarily unpowered. Weatherproof equipment, blueprints and non-player buildings are excluded.
Roofing the relevant cell or removing the building resolves the report; this does not create a roof
order or alter power, rain, fire or work behavior.

#### Scenario: Designated blight remains
- **WHEN** a visible blighted plant receives a cutting designation
- **THEN** it remains in the alert until the infection or plant actually disappears

#### Scenario: Dry exposed battery
- **WHEN** an owned rain-vulnerable battery is unroofed on a dry day
- **THEN** exposure is reported, and the report clears after the relevant roof is built

#### Scenario: Weatherproof or enemy equipment
- **WHEN** a weatherproof owned device or an enemy-owned battery is unroofed
- **THEN** it is not included in the colony's exposed-electrical report

### Requirement: Mood alerts follow actual unhappy thoughts
Mood alerts SHALL report living free colonists with the relevant active, non-nullified negative mood
thought, on loaded maps, in caravans or in travelling transporters, excluding suspended pawns.
No mood need or no actual negative thought means no such alert. Report each pawn once per condition;
being immune, approving of the situation or having only a positive/neutral stage does not qualify.

| Alert | Actual unhappy condition |
| --- | --- |
| Bonded animal assignment | The colonist is unhappy about a bonded animal not having its bonded master |
| Tainted apparel | Wearing corpse-tainted apparel causes that colonist a negative thought |
| Human leather apparel | Worn human leather causes that colonist a negative thought |
| Lovers separated | The colonist currently wants to share a bed with a spouse or lover |
| Unhappy bed sharing | Current bed sharing causes the relevant negative thought |
| Ascetic bedroom | An ascetic is actually unhappy that their bedroom is too impressive |

The inspected ascetic baseline starts at an **impressive** room's negative stage (normally **−5 mood**).
Follow the supported current thought's meaning and effect, not a room name guess. Resolving an
assignment, relationship, apparel or room condition clears the alert when the normal thought updates;
do not fabricate thoughts or forcibly expire memories. Relevant gameplay changes remain in native
bed, animal, outfit, pawn and construction controls.

#### Scenario: Tainted or human leather approval
- **WHEN** one wearer is unhappy about tainted/human-leather apparel and another is immune or approves
- **THEN** only the unhappy wearer appears in the corresponding report

#### Scenario: Relationship and bed changes
- **WHEN** the player fixes a lovers-separated or unhappy-sharing assignment
- **THEN** the corresponding report clears once the actual negative thought resolves, without an automatic bed reassignment

#### Scenario: Bonded assignment
- **WHEN** an animal/master reassignment resolves the colonist's actual bonded-animal thought
- **THEN** that colonist leaves the report; the alert does not change the animal's training or hunting role

#### Scenario: Ascetic with no negative effect
- **WHEN** an ascetic has a positive, neutral or nullified room thought, or has no mood need
- **THEN** no ascetic-room warning is shown and the alert system continues normally

### Requirement: Animal temperature illness and porcupine quills identify affected subjects
Animal heatstroke and hypothermia alerts SHALL separately report visible spawned player-faction
animals on loaded maps at **minor or worse** illness. The inspected baseline begins at **20% severity**;
initial heat exposure/shivering below minor is excluded. Follow a supported provider's equivalent
illness stage rather than assuming reordered stages have the same meaning. Wild/foreign animals,
caravan animals and animals in suspended containers are outside this source-derived alert scope.
Recovery below minor, loss of player ownership or leaving the map removes the report.

With supported Odyssey content, porcupine-quill alerts SHALL report living free colonists with at
least one remaining embedded porcupine quill, including caravans/travelling transporters but excluding
suspended pawns. Multiple quills do not create duplicate pawn entries. The alert persists while any
quill remains and does not add surgery bills or choose medicine/doctors. Existing medical features
and normal restrictions govern actual treatment.

#### Scenario: Animal severity boundary
- **WHEN** a colony animal progresses from initial exposure to minor hypothermia, then recovers below minor
- **THEN** its alert appears and clears at those actual illness-stage transitions

#### Scenario: Separate heat and cold reports
- **WHEN** owned animals have qualifying heatstroke and hypothermia while a wild animal has the same illness
- **THEN** the owned animals appear in their respective reports and the wild animal is omitted

#### Scenario: Remaining quill in a caravan
- **WHEN** a nonsuspended free colonist in a caravan still has one of several embedded quills
- **THEN** one pawn entry remains and normal world navigation can locate the caravan; removing the last quill clears the report

### Requirement: Unarmed combatants are eligible colonists without an equipped weapon
The unarmed alert SHALL include spawned free colonists on loaded maps who have no primary equipped
weapon, can perform violent work, are not downed and have usable manipulation. By default exclude
juveniles; Include child combatants adds only juveniles meeting those same capability rules. This is
an equipped-weapon warning, not a missing-ranged-hunting-weapon test or a promise that natural attacks
are ineffective. Mechs, animals, prisoners, slaves, caravans and suspended pawns are not added to this
free-colonist scope. Equipping a weapon or losing eligibility clears the report; it never equips one.

#### Scenario: Weapon and capability changes
- **WHEN** an eligible unarmed adult equips a melee weapon, or becomes incapable of violent work
- **THEN** the report removes that colonist without demanding a ranged weapon

#### Scenario: Child preference
- **WHEN** Include child combatants is enabled with the parent alert enabled
- **THEN** eligible unarmed juveniles are included, but downed or incapable children still are not

### Requirement: Trader alerts distinguish real opportunities from unrelated objects
The visiting-trader alert SHALL report visible spawned humanlike traders on loaded maps whose actual
trade state currently permits trading with the colony. Hostile, downed, imprisoned, departed or
otherwise currently unavailable traders are excluded. Ordinary restrictions still apply when a player
chooses a negotiator; the alert does not guarantee every colonist can reach or negotiate with them.

The orbital-trader alert SHALL report actual passing trade ships only on maps with an owned installed
communications terminal capable of contacting those ships under its supported normal rules. A
compatible terminal variant qualifies without requiring the vanilla building's name. Temporary power
loss or a solar flare does not conceal a ship's presence: explain the communication outage instead
of promising that negotiation is ready. An unrelated passing vessel or a console on a different map
does not qualify. Missing a negotiator or trade beacon does not erase a real ship's presence.
Explain the applicable map/ship and allow ordinary navigation to a qualifying terminal; initiating
contact and trade still require normal player commands and eligibility.

#### Scenario: Trader leaves the map
- **WHEN** a trader ceases to be available or departs after an alert is displayed
- **THEN** the next report/navigation refresh removes it without opening a stale trade

#### Scenario: Compatible terminal and outage
- **WHEN** a real trade ship is present with a supported non-vanilla terminal that temporarily lacks power
- **THEN** the alert still identifies that ship/map and explains the outage without issuing a contact job

#### Scenario: Unrelated passing vessel
- **WHEN** a map has a communications console but only non-trading passing vessels
- **THEN** no orbital-trader alert is produced

### Requirement: Ordinary alert preferences use common inherited settings
All **19 ordinary alert kinds** SHALL have independent enable rows in structured mod settings under
Alerts / Urgent, Mood, Animals and Other, with Anomaly/Odyssey requirements clearly indicated. Each
built-in default is **On**; Include child combatants defaults **Off**. Hidden-enemy information is a
separate developer diagnostic, not an ordinary alert row or an exception that leaks into gameplay.
Entity downed has its own working row, independent of enemy downed. Missing optional content shows
an unavailable explanation and retains its dormant choice. The child option is inactive while its
parent alert is off, retaining its value.

Each row shows Global default, Current save and Use global. New/missing save entries inherit the
current global dynamically. Checking Use global retains a dormant override; unchecking restores it
or seeds the first override from the effective global value. Disable the save editor while inheriting.
At the main menu disable both save controls and all save mutation/reset callbacks, retaining global
editing. Scope-specific resets never alter another scope. Changes take effect at the next ordinary
alert refresh and never alter world/pawn state or another save. Save switching does not retain old
subjects. All labels, reasons, explanations and controls have the seven baseline languages.

#### Scenario: Independent entity and enemy preferences
- **WHEN** the player changes the entity-down alert preference
- **THEN** enemy-down behavior and its stored preference remain unchanged

#### Scenario: Main menu and two saves
- **WHEN** the global preference changes at the main menu and inherited/overridden saves are loaded
- **THEN** only the inherited save follows that global, and no save editing/reset was possible from the main menu

#### Scenario: Dormant override and child preference
- **WHEN** Use global is checked then unchecked, or the unarmed alert is disabled then enabled
- **THEN** the dormant save override and child choice respectively remain available without an unintended reset

### Requirement: Unsupported alerts fail locally without duplicate ownership
Missing optional DLC SHALL quietly omit its unavailable reports. Present but unsupported required
content or integration disables only the affected report/adapter with a bounded startup explanation;
unrelated alerts continue. Any actual patch failing its documented prerequisites is skipped with the
required per-patch warning and safe fallback. Compatible foreign modifications are not rejected
merely because they exist. No alert invents a safe treatment, rescue, exposure or contact condition
when its required meaning cannot be established.

When original Extra Alerts is active it owns overlapping reports; suppress owned duplicates and
explain the selection once, without editing its settings or removing its code. Ordinary native
colony-rescue and other distinct alerts remain. No source setting/save migration or blanket external
API compatibility is claimed. Obsolete translation-only features (night owl, handler/tameness/rest
and the old paused-caravan alert) are not silently introduced by this change.

#### Scenario: Missing Odyssey
- **WHEN** Odyssey is absent but ordinary alert preferences are enabled
- **THEN** quill reporting is unavailable without errors while supported colony reports continue

#### Scenario: Changed provider illness meaning
- **WHEN** a provider removes the supported illness-stage meaning used by one animal report
- **THEN** that report stays unavailable with an explanation rather than reporting the wrong stage or disabling unrelated alerts

#### Scenario: Original provider active
- **WHEN** Extra Alerts is loaded alongside megamod
- **THEN** only the original supplies overlapping reports and owned settings do not mutate the original configuration
